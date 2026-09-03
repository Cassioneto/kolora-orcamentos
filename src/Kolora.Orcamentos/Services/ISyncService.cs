namespace Kolora.Orcamentos.Services;
public interface ISyncService
{
    string StatusText { get; }
    int Pendentes { get; }
    event EventHandler<string>? StatusChanged;
    Task TriggerSyncAsync();
}
