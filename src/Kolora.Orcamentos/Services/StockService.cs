using System.Text.Json;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Kolora.Orcamentos.Services;

public class StockService
{
    private readonly GraficaIdService _graficaId;
    public StockService(GraficaIdService graficaId) => _graficaId = graficaId;

    public async Task AbaterStockAsync(Guid produtoId, decimal quantidade)
    {
        using var db = new KoloraDbContext(_graficaId.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var consumos = await db.ProdutoMateriais.Where(pm => pm.ProdutoId == produtoId).ToListAsync();
            foreach (var pm in consumos)
            {
                var mat = await db.Materiais.FindAsync(pm.MaterialId);
                if (mat == null) continue;
                mat.StockAtual -= pm.ConsumoPorUnidade * quantidade;
                mat.AtualizadoEm = DateTime.UtcNow;
                db.Materiais.Update(mat);
                var outbox = new OutboxEvent
                {
                    Id = Guid.NewGuid(), Entidade = "Material", EntidadeId = mat.Id,
                    TipoOperacao = Models.Enums.TipoOperacaoOutbox.UPDATE,
                    PayloadJson = JsonSerializer.Serialize(mat), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente"
                };
                db.OutboxEvents.Add(outbox);
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Log.Error(ex, "Erro ao abater stock produto {ProdutoId}", produtoId);
            throw;
        }
    }

    public async Task<List<Material>> GetAbaixoDoMinimoAsync()
    {
        using var db = new KoloraDbContext(_graficaId.DbPath);
        return await db.Materiais.Where(m => m.GraficaId == _graficaId.GraficaId && m.StockAtual <= m.StockMinimo).ToListAsync();
    }
}
