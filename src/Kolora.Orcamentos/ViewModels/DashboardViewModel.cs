using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Services;
using Serilog;

namespace Kolora.Orcamentos.ViewModels;
public partial class DashboardViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    [ObservableProperty] private int _totalProdutos;
    [ObservableProperty] private int _totalClientes;
    [ObservableProperty] private int _totalOrcamentos;
    [ObservableProperty] private decimal _faturacaoMes;
    [ObservableProperty] private int _stockCritico;
    [ObservableProperty] private int _pendentesSync;
    [ObservableProperty] private int _pedidosNovos;
    [ObservableProperty] private string _graficaNome = "";

    public DashboardViewModel(GraficaIdService grafica) { _grafica = grafica; _ = LoadAsync(); }

    [RelayCommand] public async Task LoadAsync()
    {
        try
        {
            using var db = new KoloraDbContext(_grafica.DbPath);
            var gid = _grafica.GraficaId;
            GraficaNome = (await db.Graficas.FindAsync(gid))?.Nome ?? "Minha Gráfica";
            TotalProdutos = await db.Produtos.CountAsync(p => p.GraficaId == gid && p.Ativo);
            TotalClientes = await db.Clientes.CountAsync(c => c.GraficaId == gid);
            TotalOrcamentos = await db.Orcamentos.CountAsync(o => o.GraficaId == gid);
            var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            FaturacaoMes = await db.Orcamentos.Where(o => o.GraficaId == gid && o.Data >= inicioMes).SumAsync(o => (decimal?)o.Total) ?? 0;
            StockCritico = await db.Materiais.CountAsync(m => m.GraficaId == gid && m.StockAtual <= m.StockMinimo);
            PendentesSync = await db.OutboxEvents.CountAsync(o => o.StatusSync == "Pendente");
            PedidosNovos = await db.PedidosOrcamento.CountAsync(p => p.GraficaId == gid && p.Status == Models.Enums.StatusPedido.Novo);
        } catch (Exception ex) { Log.Error(ex, "Dashboard load"); }
    }
}
