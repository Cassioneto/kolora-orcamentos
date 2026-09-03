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
    [ObservableProperty] private string _nomeGrafica = "";
    [ObservableProperty] private string _telefone = "";
    [ObservableProperty] private string _localizacao = "";
    [ObservableProperty] private string _logoPath = "";
    [ObservableProperty] private string _nomePdf = "KOLORA";
    [ObservableProperty] private string _mensagemRodape = "";
    [ObservableProperty] private string _margemGlobal = "0.40";
    [ObservableProperty] private string _validadeDias = "3";
    [ObservableProperty] private string _mensagem = "";
    public ConfiguracoesViewModel(GraficaIdService grafica, BackupService backup) { _grafica = grafica; _backup = backup; _ = LoadAsync(); }
    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var g = await db.Graficas.FindAsync(_grafica.GraficaId);
        var c = await db.ConfiguracoesGrafica.FirstOrDefaultAsync(x => x.GraficaId == _grafica.GraficaId);
        if (g != null) { NomeGrafica = g.Nome; Telefone = g.Telefone ?? ""; Localizacao = g.Localizacao ?? ""; LogoPath = g.LogoPath ?? ""; }
        if (c != null) { NomePdf = c.NomeExibicaoPdf ?? "KOLORA"; MensagemRodape = c.MensagemRodapePdf ?? ""; MargemGlobal = c.MargemPadraoGlobal.ToString("0.##"); ValidadeDias = c.ValidadePadraoDias.ToString(); LogoPath = c.LogoPath ?? LogoPath; }
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
            if (c == null) { c = new ConfiguracoesGrafica { Id = Guid.NewGuid(), GraficaId = _grafica.GraficaId, LogoPath = LogoPath, NomeExibicaoPdf = NomePdf, MensagemRodapePdf = MensagemRodape, MargemPadraoGlobal = margem, ValidadePadraoDias = dias, AtualizadoEm = DateTime.UtcNow }; db.ConfiguracoesGrafica.Add(c); }
            else { c.LogoPath = LogoPath; c.NomeExibicaoPdf = NomePdf; c.MensagemRodapePdf = MensagemRodape; c.MargemPadraoGlobal = margem; c.ValidadePadraoDias = dias; c.AtualizadoEm = DateTime.UtcNow; db.ConfiguracoesGrafica.Update(c); }
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
}
