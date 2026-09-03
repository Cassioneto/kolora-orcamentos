using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace Kolora.Orcamentos.Data.Repositories;
public class EfOutboxRepository : IOutboxRepository
{
    private readonly KoloraDbContext _ctx;
    public EfOutboxRepository(KoloraDbContext ctx) => _ctx = ctx;
    public async Task<List<OutboxEvent>> GetPendentesAsync(int limit=50) => await _ctx.OutboxEvents.Where(o=>o.StatusSync=="Pendente").OrderBy(o=>o.CriadoEm).Take(limit).ToListAsync();
    public Task AddAsync(OutboxEvent e) { _ctx.OutboxEvents.Add(e); return Task.CompletedTask; }
    public Task UpdateAsync(OutboxEvent e) { _ctx.OutboxEvents.Update(e); return Task.CompletedTask; }
    public async Task MarcarComoEnviadosAsync(List<Guid> ids){ foreach(var id in ids){ var ev=await _ctx.OutboxEvents.FindAsync(id); if(ev!=null){ ev.StatusSync="Enviado"; _ctx.OutboxEvents.Update(ev);} } }
    public async Task MarcarComoErroAsync(Guid id,string erro){ var ev=await _ctx.OutboxEvents.FindAsync(id); if(ev!=null){ ev.StatusSync="Erro"; ev.UltimoErro=erro; _ctx.OutboxEvents.Update(ev);} }
    public async Task IncrementarTentativasAsync(Guid id){ var ev=await _ctx.OutboxEvents.FindAsync(id); if(ev!=null){ ev.TentativasEnvio++; _ctx.OutboxEvents.Update(ev);} }
    public async Task<int> GetCountPendentesAsync()=> await _ctx.OutboxEvents.CountAsync(o=>o.StatusSync=="Pendente");
}
