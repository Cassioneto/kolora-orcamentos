using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
using Kolora.Orcamentos.Services;
using Serilog;
using Kolora.Orcamentos.Helpers;

namespace Kolora.Orcamentos.ViewModels;
public partial class PedidosRecebidosViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    [ObservableProperty] private ObservableCollection<PedidoOrcamento> _pedidos = new();
    [ObservableProperty] private PedidoOrcamento? _selecionado;
    public PedidosRecebidosViewModel(GraficaIdService grafica) { _grafica = grafica; _ = LoadAsync(); }
    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        Pedidos = new ObservableCollection<PedidoOrcamento>(await db.PedidosOrcamento.Where(p => p.GraficaId == _grafica.GraficaId).OrderByDescending(p => p.CriadoEmOrigem).ToListAsync());
    }
    [RelayCommand] public async Task MarcarVistoAsync(PedidoOrcamento p)
    {
        if (p.Status != StatusPedido.Novo) return;
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try { var e = await db.PedidosOrcamento.FindAsync(p.Id); if (e == null) return; e.Status = StatusPedido.Visto; e.AtualizadoEm = DateTime.UtcNow; db.PedidosOrcamento.Update(e); db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "PedidosOrcamento", EntidadeId = e.Id, TipoOperacao = TipoOperacaoOutbox.UPDATE, PayloadJson = OutboxJson.Serialize(e), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" }); await db.SaveChangesAsync(); await tx.CommitAsync(); await LoadAsync(); } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Marcar visto"); }
    }
    [RelayCommand] public async Task ExpirarAsync(PedidoOrcamento p)
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var e = await db.PedidosOrcamento.FindAsync(p.Id); if (e == null) return; e.Status = StatusPedido.Expirado; e.AtualizadoEm = DateTime.UtcNow; db.PedidosOrcamento.Update(e); await db.SaveChangesAsync(); await LoadAsync();
    }
}
