namespace Kolora.Orcamentos.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;

public class EfSyncStateRepository : ISyncStateRepository
{
    private readonly KoloraDbContext _context;

    public EfSyncStateRepository(KoloraDbContext context) => _context = context;

    public async Task<SyncState?> GetAsync() => await _context.SyncState.FirstOrDefaultAsync();
    public Task UpdateAsync(SyncState syncState) { _context.SyncState.Update(syncState); return Task.CompletedTask; }
}