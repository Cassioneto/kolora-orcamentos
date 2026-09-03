namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface IOrcamentoRepository
{
    Task<List<Orcamento>> GetAllAsync();
    Task<Orcamento?> GetByIdAsync(Guid id);
    Task AddAsync(Orcamento orcamento);
    Task UpdateAsync(Orcamento orcamento);
    Task DeleteAsync(Guid id);
}