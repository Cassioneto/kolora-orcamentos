namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface IProdutoRepository
{
    Task<List<Produto>> GetAllAsync();
    Task<Produto?> GetByIdAsync(Guid id);
    Task<List<Produto>> GetAtivosAsync();
    Task AddAsync(Produto produto);
    Task UpdateAsync(Produto produto);
    Task DeleteAsync(Guid id);
}