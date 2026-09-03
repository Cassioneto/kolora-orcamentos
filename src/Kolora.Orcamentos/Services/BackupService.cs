using System.IO;
using System.IO.Compression;
using Serilog;

namespace Kolora.Orcamentos.Services;

public class BackupService
{
    private readonly GraficaIdService _graficaId;
    public BackupService(GraficaIdService graficaId) => _graficaId = graficaId;

    public async Task<string> ExportarAsync()
    {
        var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Kolora", "Backups");
        Directory.CreateDirectory(backupDir);
        var fileName = "kolora-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip";
        var dest = Path.Combine(backupDir, fileName);
        var appData = _graficaId.AppDataPath;
        using var zip = ZipFile.Open(dest, ZipArchiveMode.Create);
        var dbPath = _graficaId.DbPath;
        if (File.Exists(dbPath)) zip.CreateEntryFromFile(dbPath, "kolora.db");
        var assets = Path.Combine(appData, "assets");
        if (Directory.Exists(assets))
            foreach (var f in Directory.GetFiles(assets))
                zip.CreateEntryFromFile(f, "assets/" + Path.GetFileName(f));
        if (File.Exists(_graficaId.ConfigPath)) zip.CreateEntryFromFile(_graficaId.ConfigPath, "config.json");
        Log.Information("Backup criado: {Dest}", dest);
        await Task.CompletedTask;
        return dest;
    }
}
