using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Services;
using Serilog;
using System.IO;
using Microsoft.Win32;

namespace Kolora.Orcamentos.ViewModels;
public partial class ConfiguracoesViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    private readonly BackupService _backup;
    private readonly ConfigurationService _cfg;
    private readonly SupabaseKeyVault _vault;
    [ObservableProperty] private string _nomeGrafica = "";
    [ObservableProperty] private string _telefone = "";
    [ObservableProperty] private string _localizacao = "";
    [ObservableProperty] private string _logoPath = "";
    [ObservableProperty] private string _nomePdf = "KOLORA";
    [ObservableProperty] private string _mensagemRodape = "";
    [ObservableProperty] private string _margemGlobal = "0.40";
    [ObservableProperty] private string _validadeDias = "3";
    [ObservableProperty] private string _iban = "";
    [ObservableProperty] private string _multicaixaExpress = "";
    [ObservableProperty] private string _supabaseUrl = "";
    [ObservableProperty] private string _supabaseKey = "";
    [ObservableProperty] private bool _syncAtivo;
    [ObservableProperty] private string _mensagem = "";
    public ConfiguracoesViewModel(GraficaIdService grafica, BackupService backup, ConfigurationService cfg, SupabaseKeyVault vault)
    { _grafica = grafica; _backup = backup; _cfg = cfg; _vault = vault; _ = LoadAsync(); }
    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var g = await db.Graficas.FindAsync(_grafica.GraficaId);
        var c = await db.ConfiguracoesGrafica.FirstOrDefaultAsync(x => x.GraficaId == _grafica.GraficaId);
        if (g != null) { NomeGrafica = g.Nome; Telefone = g.Telefone ?? ""; Localizacao = g.Localizacao ?? ""; LogoPath = g.LogoPath ?? ""; }
        if (c != null) { NomePdf = c.NomeExibicaoPdf ?? "KOLORA"; MensagemRodape = c.MensagemRodapePdf ?? ""; MargemGlobal = c.MargemPadraoGlobal.ToString("0.##"); ValidadeDias = c.ValidadePadraoDias.ToString(); LogoPath = c.LogoPath ?? LogoPath; Iban = c.Iban ?? ""; MulticaixaExpress = c.MulticaixaExpressNumero ?? ""; }
        // Supabase: URL do appsettings.json, key do cofre DPAPI (mascarada)
        SupabaseUrl = _cfg.SupabaseUrl;
        SupabaseKey = string.IsNullOrWhiteSpace(_cfg.SupabaseAnonKey) ? "" : new string('•', 12);
        SyncAtivo = _cfg.IsSupabaseConfigured;
    }
    [RelayCommand] public void EscolherLogo()
    {
        var dlg = new OpenFileDialog { Filter = "Imagens|*.png;*.jpg;*.jpeg", Title = "Escolher logo" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var destDir = Path.Combine(_grafica.AppDataPath, "assets"); Directory.CreateDirectory(destDir);
                var dest = Path.Combine(destDir, "logo.png");
                File.Copy(dlg.FileName, dest, true); LogoPath = dest; Mensagem = "Logo copiado para " + dest;
            } catch (Exception ex) { Mensagem = ex.Message; }
        }
    }
    [RelayCommand] public async Task SalvarAsync()
    {
        decimal.TryParse(MargemGlobal.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var margem);
        int.TryParse(ValidadeDias, out var dias); if (dias <= 0) dias = 3;
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var g = await db.Graficas.FindAsync(_grafica.GraficaId); if (g != null) { g.Nome = NomeGrafica; g.Telefone = Telefone; g.Localizacao = Localizacao; g.LogoPath = LogoPath; g.AtualizadoEm = DateTime.UtcNow; db.Graficas.Update(g); }
            var c = await db.ConfiguracoesGrafica.FirstOrDefaultAsync(x => x.GraficaId == _grafica.GraficaId);
            if (c == null) { c = new ConfiguracoesGrafica { Id = Guid.NewGuid(), GraficaId = _grafica.GraficaId, LogoPath = LogoPath, NomeExibicaoPdf = NomePdf, MensagemRodapePdf = MensagemRodape, MargemPadraoGlobal = margem, ValidadePadraoDias = dias, Iban = Iban.Trim(), MulticaixaExpressNumero = MulticaixaExpress.Trim(), AtualizadoEm = DateTime.UtcNow }; db.ConfiguracoesGrafica.Add(c); }
            else { c.LogoPath = LogoPath; c.NomeExibicaoPdf = NomePdf; c.MensagemRodapePdf = MensagemRodape; c.MargemPadraoGlobal = margem; c.ValidadePadraoDias = dias; c.Iban = Iban.Trim(); c.MulticaixaExpressNumero = MulticaixaExpress.Trim(); c.AtualizadoEm = DateTime.UtcNow; db.ConfiguracoesGrafica.Update(c); }
            await db.SaveChangesAsync(); await tx.CommitAsync(); Mensagem = "Configurações salvas!";
        } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Salvar config"); Mensagem = ex.Message; }
    }
    [RelayCommand] public async Task BackupAsync()
    {
        try { var path = await _backup.ExportarAsync(); Mensagem = "Backup: " + path; try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.GetDirectoryName(path)!) { UseShellExecute = true }); } catch {} } catch (Exception ex) { Mensagem = ex.Message; }
    }
    [RelayCommand] public void AbrirLogs()
    {
        var dir = Path.Combine(_grafica.AppDataPath, "logs");
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true }); } catch (Exception ex) { Mensagem = ex.Message; }
    }
    [RelayCommand] public void AbrirPastaDados()
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_grafica.AppDataPath) { UseShellExecute = true }); } catch {}
    }

    // ---- Supabase: key cifrada com DPAPI (so abre na conta Windows desta grafica) ----

    [RelayCommand]
    public void GuardarSupabaseAsync()
    {
        try
        {
            // URL grava no appsettings.json ao lado do exe (nao e segredo)
            SalvarUrlNoAppSettings(SupabaseUrl.Trim());
            // Key grava cifrada no cofre DPAPI — nunca em ficheiro legivel
            var key = SupabaseKey.Contains('•') ? null : SupabaseKey; // bullets = nao alterou
            if (!string.IsNullOrWhiteSpace(key)) _vault.SaveAnonKey(key);
            _cfg.InvalidateCache(); // aplica na hora, sem reiniciar
            SyncAtivo = _cfg.IsSupabaseConfigured;
            Mensagem = _cfg.IsSupabaseConfigured
                ? "Supabase configurado! Key cifrada (DPAPI). Sync arranca em segundos."
                : "Preencha URL e key para ativar o sync (opcional — o app funciona 100% offline).";
        }
        catch (Exception ex) { Mensagem = "Erro ao guardar key: " + ex.Message; }
    }

    [RelayCommand]
    public void RemoverSupabase()
    {
        try
        {
            _vault.SaveAnonKey(null); // apaga o .bin
            SalvarUrlNoAppSettings("");
            _cfg.InvalidateCache();
            SupabaseUrl = ""; SupabaseKey = ""; SyncAtivo = false;
            Mensagem = "Supabase removido. App segue 100% offline.";
        }
        catch (Exception ex) { Mensagem = ex.Message; }
    }

    private static void SalvarUrlNoAppSettings(string url)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var json = File.Exists(path) ? File.ReadAllText(path) : "{}";
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            using var file = File.Open(path, FileMode.Create, FileAccess.Write);
            using var writer = new System.Text.Json.Utf8JsonWriter(file, new System.Text.Json.JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            // Supabase: so a URL aqui; AnonKey vazia por defeito (a key real vive cifrada no cofre DPAPI)
            writer.WritePropertyName("Supabase");
            writer.WriteStartObject();
            writer.WriteString("Url", url);
            writer.WriteString("AnonKey", "");
            writer.WriteEndObject();
            if (root.TryGetProperty("Serilog", out _))
            {
                writer.WritePropertyName("Serilog");
                writer.WriteRawValue(root.GetProperty("Serilog").GetRawText());
            }
            writer.WriteEndObject();
            writer.Flush();
        }
        catch (Exception ex) { Log.Error(ex, "Salvar appsettings URL"); }
    }
}
