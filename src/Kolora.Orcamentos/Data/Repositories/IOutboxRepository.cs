namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface IOutboxRepository
{
    Task<List<OutboxEvent>> GetPendentesAsync(int limit = 50);
    Task AddAsync(OutboxEvent outboxEvent);
    Task UpdateAsync(OutboxEvent outboxEvent);
    Task MarcarComoEnviadosAsync(List<Guid> ids);
    Task MarcarComoErroAsync(Guid id, string erro);
    Task IncrementarTentativasAsync(Guid id);
    Task<int> GetCountPendentesAsync();
}