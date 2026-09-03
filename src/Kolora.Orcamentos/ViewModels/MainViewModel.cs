using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Services;
namespace Kolora.Orcamentos.ViewModels;
public partial class MainViewModel : BaseViewModel
{
    private readonly ISyncService _sync;
    private readonly INetworkMonitorService _net;
    [ObservableProperty] private string _syncStatus = "Sincronizado";
    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private int _pedidosBadge;
    public MainViewModel(ISyncService sync, INetworkMonitorService net, IServiceProvider sp)
    {
        _sync = sync; _net = net;
        _sync.StatusChanged += (_, s) => App.Current.Dispatcher.Invoke(() => SyncStatus = s);
        CurrentView = sp.GetService(typeof(DashboardViewModel)) as BaseViewModel;
        _ = RefreshBadgeAsync(sp);
    }
    private async Task RefreshBadgeAsync(IServiceProvider sp)
    {
        try { using var db = new Data.KoloraDbContext((sp.GetService(typeof(GraficaIdService)) as GraficaIdService)!.DbPath); PedidosBadge = await db.PedidosOrcamento.CountAsync(p=>p.Status==Models.Enums.StatusPedido.Novo); } catch {}
    }
    [RelayCommand] private void Navigate(string view) { }
}
