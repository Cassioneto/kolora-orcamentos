namespace Kolora.Orcamentos.Services;
public interface ISupabaseService
{
    Task<bool> PushOutboxAsync(Guid graficaId, string entidade, string payloadJson, string operacao);
    Task<List<T>> PullAsync<T>(string table, DateTime? since, Guid graficaId) where T : class;
    Task<List<Models.Entities.PedidoOrcamento>> PullPedidosNovosAsync(Guid graficaId);
}
