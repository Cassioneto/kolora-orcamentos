using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;

namespace Kolora.Orcamentos.Services;

public class SyncBackgroundService : BackgroundService, ISyncService
{
    private readonly IServiceProvider _sp;
    private readonly INetworkMonitorService _net;
    private readonly GraficaIdService _graficaId;
    private string _status = "Sincronizado";
    private int _pendentes;
    public string StatusText => _status;
    public int Pendentes => _pendentes;
    public event EventHandler<string>? StatusChanged;

    public SyncBackgroundService(IServiceProvider sp, INetworkMonitorService net, GraficaIdService graficaId)
    {
        _sp = sp; _net = net; _graficaId = graficaId;
        _net.ConnectivityChanged += (_, online) => { if (online) _ = TriggerSyncAsync(); };
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                if (_net.IsOnline) await DoSyncAsync();
                else await UpdatePendentesAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Log.Error(ex, "Sync loop erro"); }
        }
    }

    public async Task TriggerSyncAsync() => await DoSyncAsync();

    private async Task DoSyncAsync()
    {
        await PushOutboxAsync();
        await PullCatalogoAsync();
        await PullPedidosAsync();
        await UpdatePendentesAsync();
        await ExpirePedidosAsync();
    }

    private async Task PushOutboxAsync()
    {
        using var scope = _sp.CreateScope();
        var dbPath = _graficaId.DbPath;
        using var db = new KoloraDbContext(dbPath);
        var supabase = scope.ServiceProvider.GetService(typeof(ISupabaseService)) as ISupabaseService;
        if (supabase == null) return;
        var pendentes = await db.OutboxEvents.Where(o => o.StatusSync == "Pendente").OrderBy(o => o.CriadoEm).Take(50).ToListAsync();
        foreach (var ev in pendentes)
        {
            var backoff = ev.TentativasEnvio switch { 0 => 0, 1 => 30, 2 => 120, 3 => 600, _ => 3600 };
            if ((DateTime.UtcNow - ev.CriadoEm).TotalSeconds < backoff) continue;
            var ok = await supabase.PushOutboxAsync(_graficaId.GraficaId, ev.Entidade, ev.PayloadJson, ev.TipoOperacao.ToString());
            if (ok) { ev.StatusSync = "Enviado"; }
            else { ev.TentativasEnvio++; ev.UltimoErro = "Falha push"; ev.StatusSync = ev.TentativasEnvio > 5 ? "Erro" : "Pendente"; }
            db.OutboxEvents.Update(ev);
            await db.SaveChangesAsync();
        }
    }

    private async Task PullCatalogoAsync()
    {
        // last-write-wins placeholder - real pull via SupabaseService.PullAsync
        await Task.CompletedTask;
    }

    private async Task PullPedidosAsync()
    {
        using var scope = _sp.CreateScope();
        var supabase = scope.ServiceProvider.GetService(typeof(ISupabaseService)) as ISupabaseService;
        if (supabase == null) return;
        var pedidos = await supabase.PullPedidosNovosAsync(_graficaId.GraficaId);
        if (pedidos.Count == 0) return;
        using var db = new KoloraDbContext(_graficaId.DbPath);
        foreach (var p in pedidos)
        {
            if (await db.PedidosOrcamento.AnyAsync(x => x.Id == p.Id)) continue;
            p.RecebidoLocalEm = DateTime.UtcNow;
            p.AtualizadoEm = DateTime.UtcNow;
            db.PedidosOrcamento.Add(p);
        }
        await db.SaveChangesAsync();
        Log.Information("Pull {Count} pedidos novos", pedidos.Count);
    }

    private async Task ExpirePedidosAsync()
    {
        using var db = new KoloraDbContext(_graficaId.DbPath);
        var limite = DateTime.UtcNow.AddDays(-3);
        var expirados = await db.PedidosOrcamento.Where(p => p.Status == Models.Enums.StatusPedido.Novo && p.CriadoEmOrigem < limite).ToListAsync();
        foreach (var p in expirados) { p.Status = Models.Enums.StatusPedido.Expirado; p.AtualizadoEm = DateTime.UtcNow; }
        if (expirados.Count > 0) await db.SaveChangesAsync();
    }

    private async Task UpdatePendentesAsync()
    {
        try
        {
            using var db = new KoloraDbContext(_graficaId.DbPath);
            _pendentes = await db.OutboxEvents.CountAsync(o => o.StatusSync == "Pendente");
            _status = _pendentes == 0 ? "Sincronizado" : _pendentes + " pendentes";
            if (_pendentes > 500) _status += " (atencao: >500)";
            if (!_net.IsOnline) _status = "Offline - " + _status;
            StatusChanged?.Invoke(this, _status);
        }
        catch { }
    }
}
