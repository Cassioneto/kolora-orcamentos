using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace Kolora.Orcamentos.Data.Repositories;
public class EfGraficaRepository : IGraficaRepository
{
    private readonly KoloraDbContext _ctx;
    public EfGraficaRepository(KoloraDbContext ctx) => _ctx = ctx;
    public async Task<Grafica?> GetByGraficaIdAsync(Guid id) => await _ctx.Graficas.FindAsync(id);
    public async Task<Grafica?> GetFirstAsync() => await _ctx.Graficas.FirstOrDefaultAsync();
    public Task AddAsync(Grafica g) { _ctx.Graficas.Add(g); return Task.CompletedTask; }
    public Task UpdateAsync(Grafica g) { _ctx.Graficas.Update(g); return Task.CompletedTask; }
}
