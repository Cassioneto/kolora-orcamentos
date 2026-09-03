namespace Kolora.Orcamentos.Services;
public interface INetworkMonitorService
{
    bool IsOnline { get; }
    event EventHandler<bool>? ConnectivityChanged;
    Task<bool> CheckConnectivityAsync();
}
