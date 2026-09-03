namespace Kolora.Orcamentos.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;

public class EfClienteRepository : IClienteRepository
{
    private readonly KoloraDbContext _context;

    public EfClienteRepository(KoloraDbContext context) => _context = context;

    public async Task<List<Cliente>> GetAllAsync() => await _context.Clientes.AsNoTracking().ToListAsync();
    public async Task<Cliente?> GetByIdAsync(Guid id) => await _context.Clientes.FindAsync(id);
    public Task AddAsync(Cliente cliente) { _context.Clientes.Add(cliente); return Task.CompletedTask; }
    public Task UpdateAsync(Cliente cliente) { _context.Clientes.Update(cliente); return Task.CompletedTask; }
    public async Task DeleteAsync(Guid id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente != null) _context.Clientes.Remove(cliente);
    }
}