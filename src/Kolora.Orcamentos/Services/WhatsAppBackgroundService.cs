using System.IO;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Helpers;
using Kolora.Orcamentos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Kolora.Orcamentos.Services;

/// <summary>
/// Drena FilaEnvioWhatsApp em segundo plano (offline-first):
/// só envia com internet + sessão WhatsApp ligada. Falhas voltam para a fila
/// com backoff (30s → 2min → 10min → 1h). Nunca descarta, nunca bloqueia a UI.
/// </summary>
public class WhatsAppBackgroundService : BackgroundService
{
    private readonly INetworkMonitorService _net;
    private readonly IWhatsAppService _wpp;
    private readonly GraficaIdService _grafica;

    public WhatsAppBackgroundService(
        INetworkMonitorService net, IWhatsAppService wpp, GraficaIdService grafica)
    {
        _net = net;
        _wpp = wpp;
        _grafica = grafica;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Auto-liga em fundo se já houver sessão pareada (creds.json existe)
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(8), ct);
                if (File.Exists(Path.Combine(_grafica.AppDataPath, "whatsapp", "session", "creds.json")))
                {
                    Log.Information("WhatsApp: sessão anterior encontrada, a religar...");
                    await _wpp.LigarAsync();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Log.Debug("WhatsApp auto-ligar: {Msg}", ex.Message); }
        }, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                if (_net.IsOnline && _wpp.Ligado) await DrenarAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Log.Debug("WhatsApp worker: {Msg}", ex.Message); }
        }
    }

    private async Task DrenarAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var itens = await db.FilaEnvioWhatsApp
            .Where(f => f.GraficaId == _grafica.GraficaId && f.Status == "Pendente")
            .OrderBy(f => f.CriadoEm)
            .Take(10)
            .ToListAsync();

        foreach (var item in itens)
        {
            var backoff = item.Tentativas switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(30),
                2 => TimeSpan.FromMinutes(2),
                3 => TimeSpan.FromMinutes(10),
                _ => TimeSpan.FromHours(1)
            };
            var ultima = ParseUltima(item.UltimoErro) ?? item.CriadoEm;
            if (DateTime.UtcNow - ultima < backoff) continue;

            try
            {
                await _wpp.EnviarPdfAsync(item.JidDestino, item.PdfPath, item.Legenda, Path.GetFileName(item.PdfPath));
                item.Status = "Enviado";
                item.EnviadoEm = DateTime.UtcNow;
                item.UltimoErro = null;
                Log.Information("WhatsApp: item {Id} enviado na drenagem", item.Id);
            }
            catch (Exception ex)
            {
                item.Tentativas++;
                item.UltimoErro = $"@{DateTime.UtcNow:O} {ex.Message}";
                Log.Debug("WhatsApp: item {Id} volta pra fila ({Msg})", item.Id, ex.Message);
                if (!_wpp.Ligado) break; // caiu a sessão no meio — tenta no próximo ciclo
            }
            item.AtualizadoEm = DateTime.UtcNow;
            db.FilaEnvioWhatsApp.Update(item);
            await db.SaveChangesAsync();
        }
    }

    private static DateTime? ParseUltima(string? ultimoErro)
    {
        if (string.IsNullOrWhiteSpace(ultimoErro) || !ultimoErro.StartsWith('@')) return null;
        var fim = ultimoErro.IndexOf(' ', 1);
        var iso = fim > 1 ? ultimoErro[1..fim] : ultimoErro[1..];
        return DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
    }
}
