namespace Kolora.Orcamentos.Data.Repositories;

using Kolora.Orcamentos.Models.Entities;

public interface IGraficaRepository
{
    Task<Grafica?> GetByGraficaIdAsync(Guid id);
    Task<Grafica?> GetFirstAsync();
    Task AddAsync(Grafica grafica);
    Task UpdateAsync(Grafica grafica);
}