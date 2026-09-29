using System.IO;
using System.Net.Http;
using BaileysCSharp.Core.Events;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Logging;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Sending.Media;
using BaileysCSharp.Core.Models.Sending.NonMedia;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Sockets;
using BaileysCSharp.Core.Types;
using BaileysCSharp.Exceptions;
using QRCoder;
using Serilog;

namespace Kolora.Orcamentos.Services;

/// <summary>
/// Sessão WhatsApp via BaileysCSharp (protocolo WhatsApp Web, sem API oficial).
/// Offline-first: a sessão e a fila vivem no disco; sem internet ou sem pareamento,
/// os envios ficam em FilaEnvioWhatsApp e o worker drena sozinho depois.
/// Sessão em %AppData%/Kolora/whatsapp (creds.json + keys/).
/// </summary>
public class WhatsAppService : IWhatsAppService, IDisposable
{
    private readonly GraficaIdService _grafica;
    private readonly INetworkMonitorService _net;
    private readonly object _lock = new();
    private WASocket? _socket;
    private bool _desejaLigar;
    private bool _ligando;
    private bool _disposed;
    private int _tentativaSeq;
    private int _falhasSeguidas;
    private static readonly HttpClient _httpVersao = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static uint[]? _versaoWaWeb; // buscada 1x por execução (sw.js muda várias vezes ao dia)

    public WhatsAppStatus Status { get; private set; } = WhatsAppStatus.Desligado;
    public bool Ligado => Status == WhatsAppStatus.Ligado && _socket != null;
    public byte[]? QrPng { get; private set; }
    public string UltimoErro { get; private set; } = "";
    public int PendentesFila { get; private set; }
    public event EventHandler? EstadoMudou;

    private string SessionDir => Path.Combine(_grafica.AppDataPath, "whatsapp", "session");
    private string KeysDir => Path.Combine(SessionDir, "keys");
    private string CredsPath => Path.Combine(SessionDir, "creds.json");

    public WhatsAppService(GraficaIdService grafica, INetworkMonitorService net)
    {
        _grafica = grafica;
        _net = net;
        _net.ConnectivityChanged += (_, online) =>
        {
            if (online && _desejaLigar && !Ligado) _ = LigarAsync();
        };
    }

    private void MudarEstado(WhatsAppStatus s, string erro = "")
    {
        Status = s;
        UltimoErro = erro;
        if (s != WhatsAppStatus.AguardandoQR) QrPng = null;
        EstadoMudou?.Invoke(this, EventArgs.Empty);
    }

    public Task LigarAsync()
    {
        lock (_lock)
        {
            if (_ligando || Ligado) return Task.CompletedTask;
            _desejaLigar = true;
            _ligando = true;
            _tentativaSeq++;
        }
        MudarEstado(WhatsAppStatus.Ligando);
        Log.Information("WhatsApp: a ligar...");
        return Task.Run(async () => await ConectarInterno());
    }

    /// <summary>
    /// A versão WA Web vem AO VIVO de web.whatsapp.com/sw.js (client_revision).
    /// A lib traz [2,3000,1023680336] hardcoded (~2024) e o servidor rejeita
    /// versões velhas com 405 client_too_old — sem isto, o QR nunca chega.
    /// Fallback: última versão boa conhecida (atualizar quando o pareamento falhar).
    /// </summary>
    private static readonly uint[] VersaoFallback = [2, 3000, 1048745628]; // 2026-09-29

    private static async Task<uint[]> ObterVersaoWaWebAsync()
    {
        if (_versaoWaWeb != null) return _versaoWaWeb;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://web.whatsapp.com/sw.js");
            req.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            req.Headers.TryAddWithoutValidation("Referer", "https://web.whatsapp.com/");
            using var resp = await _httpVersao.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var sw = await resp.Content.ReadAsStringAsync();
            var m = System.Text.RegularExpressions.Regex.Match(sw, @"client_revision\D{0,5}(\d{6,})");
            if (m.Success && uint.TryParse(m.Groups[1].Value, out var rev))
            {
                _versaoWaWeb = [2, 3000, rev];
                Log.Information("WhatsApp: versão WA Web ao vivo 2.3000.{Rev}", rev);
                return _versaoWaWeb;
            }
            Log.Warning("WhatsApp: client_revision não achado no sw.js, usando fallback");
        }
        catch (Exception ex) { Log.Warning("WhatsApp: falha ao buscar versão ({Msg}), usando fallback", ex.Message); }
        _versaoWaWeb = VersaoFallback;
        return _versaoWaWeb;
    }

    private async Task ConectarInterno()
    {
        try
        {
            var versao = await ObterVersaoWaWebAsync();
            Directory.CreateDirectory(SessionDir);
            Directory.CreateDirectory(KeysDir);

            // Liberta socket anterior (se houver) ANTES de abrir o novo:
            // o LiteDatabase interno segura store.db com lock exclusivo.
            DescartarSocket();

            AuthenticationCreds? creds = null;
            if (File.Exists(CredsPath))
            {
                try { creds = AuthenticationCreds.Deserialize(File.ReadAllText(CredsPath)); }
                catch (Exception ex) { Log.Warning(ex, "WhatsApp: creds.json inválido, nova sessão"); }
            }
            creds ??= AuthenticationUtils.InitAuthCreds();

            var config = new SocketConfig
            {
                // Caminho absoluto: CacheRoot = Combine(Root, SessionName) devolve o absoluto
                SessionName = SessionDir,
                Version = versao, // AO VIVO (sw.js) — hardcoded da lib é rejeitado pelo servidor
                Auth = new AuthenticationState { Creds = creds, Keys = new FileKeyStore(KeysDir) },
            };
            config.Logger.Level = LogLevel.Info; // handshake e erros da lib → Serilog (via Forward)
            DefaultLogger.Forward = json => Log.Debug("Baileys: {Json}", json);
            config.MarkOnlineOnConnect = false;   // não mostra "online" o tempo todo
            config.SyncFullHistory = false;       // não baixa histórico (poupa dados)
            config.FireInitQueries = false;

            var socket = new WASocket(config);
            socket.EV.Auth.Update += (_, c) =>
            {
                try { File.WriteAllText(CredsPath, AuthenticationCreds.Serialize(c)); }
                catch (Exception ex) { Log.Warning(ex, "WhatsApp: falha ao persistir creds"); }
            };
            socket.EV.Connection.Update += ConexaoMudou;

            lock (_lock) _socket = socket;
            socket.MakeSocket(); // dispara conexão em thread da lib; eventos disparam o resto
            Log.Information("WhatsApp: socket criado, a aguardar resposta do servidor...");
            IniciarWatchdog();
        }
        catch (IOException io) when (io.Message.Contains("being used by another process"))
        {
            // store.db bloqueado: outra janela do KOLORA aberta OU socket anterior não libertado.
            // Não reagenda sozinho (evita spam no log) — usuário fecha a outra janela e clica Ligar.
            lock (_lock) { _ligando = false; _desejaLigar = false; }
            MudarEstado(WhatsAppStatus.Erro, "Outra janela do KOLORA parece estar aberta. Feche-a e clique Ligar de novo.");
            Log.Warning("WhatsApp: store.db bloqueado — provável segunda instância aberta");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WhatsApp: falha ao ligar");
            lock (_lock) _ligando = false;
            MudarEstado(WhatsAppStatus.Erro, ex.Message);
            AgendarReligar();
        }
    }

    /// <summary>
    /// Desconecta e liberta o socket atual, fechando o LiteDatabase (store.db).
    /// Sem isto, cada nova tentativa falha com "used by another process" para sempre.
    /// </summary>
    private void DescartarSocket()
    {
        WASocket? velho;
        lock (_lock) { velho = _socket; _socket = null; }
        if (velho == null) return;
        try { velho.WSDisconnect(); } catch { }
        try { velho.Dispose(); } catch (Exception ex) { Log.Debug("WhatsApp: dispose socket ({Msg})", ex.Message); }
    }

    private void ConexaoMudou(object? sender, ConnectionState e)
    {
        try
        {
            if (!string.IsNullOrEmpty(e.QR))
            {
                using var gen = new QRCodeGenerator();
                var data = gen.CreateQrCode(e.QR, QRCodeGenerator.ECCLevel.Q);
                QrPng = new PngByteQRCode(data).GetGraphic(8);
                MudarEstado(WhatsAppStatus.AguardandoQR);
                Log.Information("WhatsApp: QR gerado — escaneie no telemóvel");
                return;
            }

            if (e.Connection == WAConnectionState.Open)
            {
                lock (_lock) { _ligando = false; _falhasSeguidas = 0; _tentativaSeq++; }
                MudarEstado(WhatsAppStatus.Ligado);
                Log.Information("WhatsApp: sessão ligada");
                return;
            }

            if (e.Connection is WAConnectionState.Close or WAConnectionState.Closed)
            {
                // BUGFIX: descartar (Dispose) antes de anular — senão o LiteDatabase
                // segura store.db e toda reconexão falha com "used by another process".
                DescartarSocket();
                lock (_lock) _ligando = false;
                var boom = e.LastDisconnect?.Error as Boom;
                var code = boom?.Data?.StatusCode as int?;
                if (code == (int)DisconnectReason.LoggedOut)
                {
                    ApagarSessao();
                    MudarEstado(WhatsAppStatus.Desligado, "Sessão terminada no telemóvel — ligue de novo.");
                    Log.Warning("WhatsApp: logged out, sessão apagada");
                    return;
                }
                if (_desejaLigar)
                {
                    MudarEstado(WhatsAppStatus.Ligando, "A religar...");
                    AgendarReligar();
                }
                else MudarEstado(WhatsAppStatus.Desligado);
            }
        }
        catch (Exception ex) { Log.Error(ex, "WhatsApp: erro no handler de conexão"); }
    }

    /// <summary>
    /// A lib não tem timeout no handshake (ConnectAsync sem CancellationToken) e o
    /// evento de desconexão está comentado no código dela — sem este watchdog, uma
    /// rede que trave o handshake deixa o botão "morto" para sempre, sem nenhum log.
    /// Se em 45s não chegar QR/Open/Close, descarta o socket e tenta com backoff.
    /// </summary>
    private void IniciarWatchdog()
    {
        int seq;
        lock (_lock) seq = _tentativaSeq;
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(45));
            bool travado;
            lock (_lock) travado = seq == _tentativaSeq && Status == WhatsAppStatus.Ligando && !_disposed;
            if (!travado) return;
            Log.Warning("WhatsApp: sem resposta do servidor em 45s (internet/firewall?) — a tentar de novo");
            DescartarSocket();
            lock (_lock) _ligando = false;
            MudarEstado(WhatsAppStatus.Erro, "Sem resposta dos servidores WhatsApp. Verifique a internet e clique Ligar de novo.");
            AgendarReligar();
        });
    }

    private void AgendarReligar()
    {
        int n;
        lock (_lock) n = _falhasSeguidas++;
        var espera = TimeSpan.FromSeconds(Math.Min(10 * Math.Pow(2, n), 300)); // 10s, 20s, 40s... max 5min
        Task.Run(async () =>
        {
            await Task.Delay(espera);
            if (_desejaLigar && !Ligado && !_disposed) await LigarAsync();
        });
    }

    public async Task DesligarAsync(bool desemparelhar = false)
    {
        _desejaLigar = false;
        lock (_lock) _ligando = false;
        await Task.Run(DescartarSocket);
        if (desemparelhar) ApagarSessao();
        MudarEstado(WhatsAppStatus.Desligado);
        Log.Information("WhatsApp: desligado" + (desemparelhar ? " (sessão apagada)" : ""));
    }

    private void ApagarSessao()
    {
        try
        {
            if (File.Exists(CredsPath)) File.Delete(CredsPath);
            if (Directory.Exists(KeysDir)) Directory.Delete(KeysDir, true);
            // store.db do LiteDB interno: só apaga com socket já descartado (sem lock)
            var store = Path.Combine(SessionDir, "store.db");
            if (File.Exists(store)) File.Delete(store);
            foreach (var j in new[] { store + "-journal", store + "-wal", store + "-shm" })
                try { if (File.Exists(j)) File.Delete(j); } catch { }
        }
        catch (Exception ex) { Log.Warning(ex, "WhatsApp: falha ao apagar sessão"); }
    }

    public async Task EnviarPdfAsync(string jid, string pdfPath, string legenda, string nomeFicheiro)
    {
        var socket = _socket;
        if (!Ligado || socket == null)
            throw new InvalidOperationException("WhatsApp desligado");
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("PDF não encontrado", pdfPath);

        if (!string.IsNullOrWhiteSpace(legenda))
            await socket.SendMessage(jid, new TextMessageContent { Text = legenda });

        using var fs = File.OpenRead(pdfPath);
        var doc = await socket.SendMessage(jid, new DocumentMessageContent
        {
            Document = fs,
            Mimetype = "application/pdf",
            FileName = nomeFicheiro,
        });
        if (doc == null) throw new InvalidOperationException("WhatsApp não confirmou o envio");
        Log.Information("WhatsApp: PDF enviado para {Jid}", jid);
    }

    public void Dispose()
    {
        _disposed = true;
        _desejaLigar = false;
        try { _socket?.Dispose(); } catch { }
    }
}
