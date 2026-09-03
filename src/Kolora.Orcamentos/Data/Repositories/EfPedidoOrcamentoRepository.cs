namespace Kolora.Orcamentos.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;

public class EfPedidoOrcamentoRepository : IPedidoOrcamentoRepository
{
    private readonly KoloraDbContext _context;

    public EfPedidoOrcamentoRepository(KoloraDbContext context) => _context = context;

    public async Task<List<PedidoOrcamento>> GetAllAsync() => await _context.PedidosOrcamento.AsNoTracking().ToListAsync();
    public async Task<PedidoOrcamento?> GetByIdAsync(Guid id) => await _context.PedidosOrcamento.FindAsync(id);
    public async Task<List<PedidoOrcamento>> GetNovosAsync() => await _context.PedidosOrcamento.Where(p => p.Status == Kolora.Orcamentos.Models.Enums.StatusPedido.Novo).AsNoTracking().ToListAsync();
    public Task AddAsync(PedidoOrcamento pedido) { _context.PedidosOrcamento.Add(pedido); return Task.CompletedTask; }
    public Task UpdateAsync(PedidoOrcamento pedido) { _context.PedidosOrcamento.Update(pedido); return Task.CompletedTask; }
    public async Task DeleteAsync(Guid id)
    {
        var pedido = await _context.PedidosOrcamento.FindAsync(id);
        if (pedido != null) _context.PedidosOrcamento.Remove(pedido);
    }
}