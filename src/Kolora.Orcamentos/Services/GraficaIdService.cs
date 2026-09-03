using System.IO;
using System.Text.Json;
using Serilog;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kolora.Orcamentos.Services;

public class GraficaIdService : IGraficaIdService
{
    private readonly string _configPath;
    private readonly string _dbPath;
    public Guid GraficaId { get; private set; }

    public GraficaIdService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kolora");
        Directory.CreateDirectory(appData);
        Directory.CreateDirectory(Path.Combine(appData, "assets"));
        Directory.CreateDirectory(Path.Combine(appData, "logs"));
        _configPath = Path.Combine(appData, "config.json");
        _dbPath = Path.Combine(appData, "kolora.db");
    }

    public string DbPath => _dbPath;
    public string ConfigPath => _configPath;
    public string AppDataPath => Path.GetDirectoryName(_configPath)!;

    public async Task EnsureInitializedAsync()
    {
        if (File.Exists(_configPath))
        {
            var json = await File.ReadAllTextAsync(_configPath);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("grafica_id", out var idProp) && Guid.TryParse(idProp.GetString(), out var gid))
            {
                GraficaId = gid;
                Log.Information("GraficaId carregado: {GraficaId}", GraficaId);
                return;
            }
        }
        GraficaId = Guid.NewGuid();
        var config = new { grafica_id = GraficaId.ToString(), criado_em = DateTime.UtcNow, versao_app = "1.0.0" };
        await File.WriteAllTextAsync(_configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        Log.Information("Novo GraficaId gerado: {GraficaId}", GraficaId);

        // Ensure Grafica row exists
        try
        {
            using var db = new KoloraDbContext(_dbPath);
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
            if (!await db.Graficas.AnyAsync(g => g.Id == GraficaId))
            {
                var grafica = new Grafica { Id = GraficaId, Nome = "Minha Grafica", CriadoEm = DateTime.UtcNow, AtualizadoEm = DateTime.UtcNow };
                db.Graficas.Add(grafica);
                var outbox = new OutboxEvent
                {
                    Id = Guid.NewGuid(), Entidade = "Grafica", EntidadeId = GraficaId,
                    TipoOperacao = Models.Enums.TipoOperacaoOutbox.INSERT,
                    PayloadJson = JsonSerializer.Serialize(grafica), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente"
                };
                db.OutboxEvents.Add(outbox);
                await db.SaveChangesAsync();
            }
            if (!await db.ConfiguracoesGrafica.AnyAsync(c => c.GraficaId == GraficaId))
            {
                db.ConfiguracoesGrafica.Add(new ConfiguracoesGrafica
                {
                    Id = Guid.NewGuid(), GraficaId = GraficaId, NomeExibicaoPdf = "KOLORA",
                    MensagemRodapePdf = "Orcamento valido por 3 dias. Obrigado pela preferencia!",
                    MargemPadraoGlobal = 0.40m, ValidadePadraoDias = 3, AtualizadoEm = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex) { Log.Error(ex, "Erro ao inicializar GraficaId no banco"); }
    }
}
