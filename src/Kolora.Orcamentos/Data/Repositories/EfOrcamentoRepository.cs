namespace Kolora.Orcamentos.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;

public class EfOrcamentoRepository : IOrcamentoRepository
{
    private readonly KoloraDbContext _context;

    public EfOrcamentoRepository(KoloraDbContext context) => _context = context;

    public async Task<List<Orcamento>> GetAllAsync() => await _context.Orcamentos.AsNoTracking().ToListAsync();
    public async Task<Orcamento?> GetByIdAsync(Guid id) => await _context.Orcamentos.FindAsync(id);
    public Task AddAsync(Orcamento orcamento) { _context.Orcamentos.Add(orcamento); return Task.CompletedTask; }
    public Task UpdateAsync(Orcamento orcamento) { _context.Orcamentos.Update(orcamento); return Task.CompletedTask; }
    public async Task DeleteAsync(Guid id)
    {
        var orcamento = await _context.Orcamentos.FindAsync(id);
        if (orcamento != null) _context.Orcamentos.Remove(orcamento);
    }
}