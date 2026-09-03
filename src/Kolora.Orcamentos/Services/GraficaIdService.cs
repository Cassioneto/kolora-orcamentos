using System.IO;
using System.Text.Json;
using Serilog;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Helpers;

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
        // 1. Resolve o GraficaId do config.json (nunca regenera se já existir — spec §6)
        if (File.Exists(_configPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_configPath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("grafica_id", out var idProp) && Guid.TryParse(idProp.GetString(), out var gid))
                    GraficaId = gid;
            }
            catch (Exception ex) { Log.Warning(ex, "config.json ilegível, gerando novo GraficaId"); }
        }
        if (GraficaId == Guid.Empty)
        {
            GraficaId = Guid.NewGuid();
            var config = new { grafica_id = GraficaId.ToString(), criado_em = DateTime.UtcNow, versao_app = "1.0.0" };
            await File.WriteAllTextAsync(_configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
            Log.Information("Novo GraficaId gerado: {GraficaId}", GraficaId);
        }
        else
        {
            Log.Information("GraficaId carregado: {GraficaId}", GraficaId);
        }

        // 2. Garante schema + WAL + auto-recuperação das linhas da gráfica
        //    (mesmo com config.json existente: se o DB for novo/apagado, recria Graficas/Configuracoes)
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
                    PayloadJson = OutboxJson.Serialize(grafica), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente"
                };
                db.OutboxEvents.Add(outbox);
                await db.SaveChangesAsync();
                Log.Information("Grafica recriada no banco local (auto-recuperacao)");
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
