namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface ISyncStateRepository
{
    Task<SyncState?> GetAsync();
    Task UpdateAsync(SyncState syncState);
}