using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Helpers;

namespace Kolora.Orcamentos.Services;

/// <summary>
/// Motor de sincronização em segundo plano (spec.md §7).
/// REGRA: nunca bloqueia a UI e nunca marca eventos como "Erro" por falta de rede —
/// offline os eventos ficam "Pendente" para sempre e sincronizam quando houver internet.
/// Só tenta falar com o Supabase se URL+AnonKey estiverem configuradas (app 100% funcional sem).
/// </summary>
public class SyncBackgroundService : BackgroundService, ISyncService
{
    private readonly IServiceProvider _sp;
    private readonly INetworkMonitorService _net;
    private readonly IConfigurationService _cfg;
    private readonly GraficaIdService _graficaId;
    private string _status = "Offline";
    private int _pendentes;
    public string StatusText => _status;
    public int Pendentes => _pendentes;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<int>? NovosPedidos;

    public SyncBackgroundService(IServiceProvider sp, INetworkMonitorService net, IConfigurationService cfg, GraficaIdService graficaId)
    {
        _sp = sp; _net = net; _cfg = cfg; _graficaId = graficaId;
        _net.ConnectivityChanged += (_, online) => { if (online) _ = TriggerSyncAsync(); };
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Atualiza contadores de imediato (rodapé), sem rede
        _ = UpdatePendentesAsync();
        var loopPush = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct);
                    if (_cfg.IsSupabaseConfigured && _net.IsOnline) await DoSyncAsync();
                    else await UpdatePendentesAsync();
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Log.Debug("Sync loop: {Msg}", ex.Message); }
            }
        }, ct);

        // Pull de pedidos com intervalo mais curto (spec §7.5: 15-30s)
        var loopPedidos = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), ct);
                    if (_cfg.IsSupabaseConfigured && _net.IsOnline)
                    {
                        await PullPedidosAsync();
                        await ExpirePedidosAsync();
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Log.Debug("Pedidos loop: {Msg}", ex.Message); }
            }
        }, ct);

        await Task.WhenAll(loopPush, loopPedidos);
    }

    public async Task TriggerSyncAsync()
    {
        if (!_cfg.IsSupabaseConfigured)
        {
            await UpdatePendentesAsync();
            return;
        }
        if (!_net.IsOnline) { await UpdatePendentesAsync(); return; }
        await DoSyncAsync();
    }

    private async Task DoSyncAsync()
    {
        try
        {
            await PushOutboxAsync();
            await PullCatalogoAsync();
            await PullPedidosAsync();
            await ExpirePedidosAsync();
        }
        finally { await UpdatePendentesAsync(); }
    }

    private async Task PushOutboxAsync()
    {
        using var scope = _sp.CreateScope();
        var supabase = scope.ServiceProvider.GetRequiredService<ISupabaseService>();
        using var db = new KoloraDbContext(_graficaId.DbPath);
        var pendentes = await db.OutboxEvents
            .Where(o => o.StatusSync == "Pendente" || o.StatusSync == "Erro")
            .OrderBy(o => o.CriadoEm)
            .Take(50)
            .ToListAsync();

        foreach (var ev in pendentes)
        {
            // Backoff exponencial com base na ULTIMA tentativa (spec §7.3: 30s, 2min, 10min, 1h).
            // CriadoEm e fixo — usar so ele fazia eventos velhos tentarem a cada ciclo (spam no log).
            var ultimaTentativa = ParseUltimaTentativa(ev.UltimoErro) ?? ev.CriadoEm;
            var backoff = ev.TentativasEnvio switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(30),
                2 => TimeSpan.FromMinutes(2),
                3 => TimeSpan.FromMinutes(10),
                _ => TimeSpan.FromHours(1)
            };
            if (DateTime.UtcNow - ultimaTentativa < backoff) continue;

            var ok = await supabase.PushOutboxAsync(_graficaId.GraficaId, ev.Entidade, ev.PayloadJson, ev.TipoOperacao.ToString());
            if (ok)
            {
                ev.StatusSync = "Enviado";
                ev.UltimoErro = null;
            }
            else
            {
                // Offline/falha de rede: mantém Pendente (nunca descarta), incrementa só para backoff
                ev.TentativasEnvio++;
                // Guarda o timestamp da tentativa dentro do UltimoErro para o backoff funcionar
                ev.UltimoErro = $"@{DateTime.UtcNow:O} sem rede ou Supabase indisponível";
                if (ev.StatusSync != "Erro") ev.StatusSync = "Pendente";
            }
            db.OutboxEvents.Update(ev);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Extrai o timestamp gravado em UltimoErro no formato "@{O} mensagem".</summary>
    private static DateTime? ParseUltimaTentativa(string? ultimoErro)
    {
        if (string.IsNullOrWhiteSpace(ultimoErro) || !ultimoErro.StartsWith('@')) return null;
        var fim = ultimoErro.IndexOf(' ', 1);
        var iso = fim > 1 ? ultimoErro[1..fim] : ultimoErro[1..];
        return DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
    }

    private async Task PullCatalogoAsync()
    {
        // Pull last-write-wins por atualizado_em (spec §7.4) — estrutura pronta
        using var scope = _sp.CreateScope();
        var supabase = scope.ServiceProvider.GetRequiredService<ISupabaseService>();
        using var db = new KoloraDbContext(_graficaId.DbPath);
        var state = await db.SyncState.FirstOrDefaultAsync();
        var since = state?.UltimoPullCatalogo;

        var remotos = await supabase.PullAsync<Produto>("produtos", since, _graficaId.GraficaId);
        foreach (var r in remotos)
        {
            var local = await db.Produtos.FindAsync(r.Id);
            if (local == null) db.Produtos.Add(r);
            else if (r.AtualizadoEm > local.AtualizadoEm) { db.Produtos.Remove(local); db.Produtos.Add(r); }
        }
        if (remotos.Count > 0) await db.SaveChangesAsync();
        if (state != null) { state.UltimoPullCatalogo = DateTime.UtcNow; await db.SaveChangesAsync(); }
    }

    private async Task PullPedidosAsync()
    {
        using var scope = _sp.CreateScope();
        var supabase = scope.ServiceProvider.GetRequiredService<ISupabaseService>();
        var pedidos = await supabase.PullPedidosNovosAsync(_graficaId.GraficaId);
        if (pedidos.Count == 0) return;
        using var db = new KoloraDbContext(_graficaId.DbPath);
        var novos = 0;
        foreach (var p in pedidos)
        {
            if (await db.PedidosOrcamento.AnyAsync(x => x.Id == p.Id)) continue;
            p.RecebidoLocalEm = DateTime.UtcNow;
            p.AtualizadoEm = DateTime.UtcNow;
            db.PedidosOrcamento.Add(p);
            novos++;
        }
        if (novos > 0)
        {
            await db.SaveChangesAsync();
            Log.Information("Pull {Count} pedidos novos", novos);
            NovosPedidos?.Invoke(this, novos);
        }
    }

    private async Task ExpirePedidosAsync()
    {
        using var db = new KoloraDbContext(_graficaId.DbPath);
        var limite = DateTime.UtcNow.AddDays(-3);
        var expirados = await db.PedidosOrcamento
            .Where(p => p.Status == Models.Enums.StatusPedido.Novo && p.CriadoEmOrigem < limite)
            .ToListAsync();
        foreach (var p in expirados) { p.Status = Models.Enums.StatusPedido.Expirado; p.AtualizadoEm = DateTime.UtcNow; }
        if (expirados.Count > 0) await db.SaveChangesAsync();
    }

    private async Task UpdatePendentesAsync()
    {
        try
        {
            using var db = new KoloraDbContext(_graficaId.DbPath);
            _pendentes = await db.OutboxEvents.CountAsync(o => o.StatusSync != "Enviado");
            if (!_cfg.IsSupabaseConfigured)
                _status = _pendentes == 0 ? "Offline (sem Supabase)" : $"Offline (sem Supabase) — {_pendentes} na fila";
            else if (!_net.IsOnline)
                _status = _pendentes == 0 ? "Offline" : $"Offline — {_pendentes} pendentes";
            else
            {
                _status = _pendentes == 0 ? "Sincronizado" : $"{_pendentes} pendentes";
                if (_pendentes > 500) _status += " (atenção: >500)";
            }
            StatusChanged?.Invoke(this, _status);
        }
        catch { /* nunca quebra a UI por causa do rodapé */ }
    }
}
