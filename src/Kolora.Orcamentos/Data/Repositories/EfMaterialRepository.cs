namespace Kolora.Orcamentos.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;

public class EfMaterialRepository : IMaterialRepository
{
    private readonly KoloraDbContext _context;

    public EfMaterialRepository(KoloraDbContext context) => _context = context;

    public async Task<List<Material>> GetAllAsync() => await _context.Materiais.AsNoTracking().ToListAsync();
    public async Task<Material?> GetByIdAsync(Guid id) => await _context.Materiais.FindAsync(id);
    public async Task AddAsync(Material material) => await _context.Materiais.AddAsync(material);
    public async Task UpdateAsync(Material material) => _context.Materiais.Update(material);
    public async Task DeleteAsync(Guid id)
    {
        var material = await _context.Materiais.FindAsync(id);
        if (material != null) _context.Materiais.Remove(material);
    }
    public async Task<List<Material>> GetAbaixoDoMinimoAsync() => await _context.Materiais.Where(m => m.StockAtual <= m.StockMinimo).AsNoTracking().ToListAsync();
}