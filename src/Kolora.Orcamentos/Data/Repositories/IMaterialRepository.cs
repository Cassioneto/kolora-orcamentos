namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface IMaterialRepository
{
    Task<List<Material>> GetAllAsync();
    Task<Material?> GetByIdAsync(Guid id);
    Task AddAsync(Material material);
    Task UpdateAsync(Material material);
    Task DeleteAsync(Guid id);
    Task<List<Material>> GetAbaixoDoMinimoAsync();
}