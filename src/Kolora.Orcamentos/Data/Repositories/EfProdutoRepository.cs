namespace Kolora.Orcamentos.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;

public class EfProdutoRepository : IProdutoRepository
{
    private readonly KoloraDbContext _context;

    public EfProdutoRepository(KoloraDbContext context) => _context = context;

    public async Task<List<Produto>> GetAllAsync() => await _context.Produtos.AsNoTracking().ToListAsync();
    public async Task<Produto?> GetByIdAsync(Guid id) => await _context.Produtos.FindAsync(id);
    public async Task<List<Produto>> GetAtivosAsync() => await _context.Produtos.Where(p => p.Ativo).AsNoTracking().ToListAsync();
    public Task AddAsync(Produto produto) { _context.Produtos.Add(produto); return Task.CompletedTask; }
    public Task UpdateAsync(Produto produto) { _context.Produtos.Update(produto); return Task.CompletedTask; }
    public async Task DeleteAsync(Guid id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto != null) _context.Produtos.Remove(produto);
    }
}