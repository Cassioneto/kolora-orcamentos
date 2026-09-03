namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface IPedidoOrcamentoRepository
{
    Task<List<PedidoOrcamento>> GetAllAsync();
    Task<PedidoOrcamento?> GetByIdAsync(Guid id);
    Task<List<PedidoOrcamento>> GetNovosAsync();
    Task AddAsync(PedidoOrcamento pedido);
    Task UpdateAsync(PedidoOrcamento pedido);
    Task DeleteAsync(Guid id);
}