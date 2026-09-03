namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface IClienteRepository
{
    Task<List<Cliente>> GetAllAsync();
    Task<Cliente?> GetByIdAsync(Guid id);
    Task AddAsync(Cliente cliente);
    Task UpdateAsync(Cliente cliente);
    Task DeleteAsync(Guid id);
}