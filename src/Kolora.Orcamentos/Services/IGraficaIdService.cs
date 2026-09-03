namespace Kolora.Orcamentos.Services;
public interface IGraficaIdService
{
    Guid GraficaId { get; }
    Task EnsureInitializedAsync();
}
