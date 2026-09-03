using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
using Kolora.Orcamentos.Services;
using Serilog;

namespace Kolora.Orcamentos.ViewModels;
public partial class MateriaisViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    [ObservableProperty] private ObservableCollection<Material> _materiais = new();
    [ObservableProperty] private Material? _selecionado;
    [ObservableProperty] private string _nome = "";
    [ObservableProperty] private string _unidade = "unidade";
    [ObservableProperty] private string _stockAtual = "0";
    [ObservableProperty] private string _stockMinimo = "5";
    public string[] Unidades => new[] { "m", "m2", "litro", "unidade", "kg", "rolo" };
    public MateriaisViewModel(GraficaIdService grafica) { _grafica = grafica; _ = LoadAsync(); }
    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var list = await db.Materiais.Where(m => m.GraficaId == _grafica.GraficaId).OrderBy(m => m.Nome).ToListAsync();
        Materiais = new ObservableCollection<Material>(list);
    }
    [RelayCommand] public void Novo() { Selecionado = null; Nome = ""; Unidade = "unidade"; StockAtual = "0"; StockMinimo = "5"; }
    [RelayCommand] public void Editar(Material m) { Selecionado = m; Nome = m.Nome; Unidade = m.Unidade; StockAtual = m.StockAtual.ToString("0.##"); StockMinimo = m.StockMinimo.ToString("0.##"); }
    [RelayCommand] public async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome)) return;
        decimal.TryParse(StockAtual.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sa);
        decimal.TryParse(StockMinimo.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sm);
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            Material mat; string op;
            if (Selecionado == null) { mat = new Material { Id = Guid.NewGuid(), GraficaId = _grafica.GraficaId, Nome = Nome.Trim(), Unidade = Unidade, StockAtual = sa, StockMinimo = sm, AtualizadoEm = DateTime.UtcNow }; db.Materiais.Add(mat); op = "INSERT"; }
            else { mat = await db.Materiais.FindAsync(Selecionado.Id) ?? Selecionado; mat.Nome = Nome.Trim(); mat.Unidade = Unidade; mat.StockAtual = sa; mat.StockMinimo = sm; mat.AtualizadoEm = DateTime.UtcNow; db.Materiais.Update(mat); op = "UPDATE"; }
            db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Material", EntidadeId = mat.Id, TipoOperacao = op == "INSERT" ? TipoOperacaoOutbox.INSERT : TipoOperacaoOutbox.UPDATE, PayloadJson = JsonSerializer.Serialize(mat), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" });
            await db.SaveChangesAsync(); await tx.CommitAsync(); await LoadAsync(); Novo();
        } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Salvar material"); }
    }
    [RelayCommand] public async Task ExcluirAsync(Material m)
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try { var e = await db.Materiais.FindAsync(m.Id); if (e == null) return; db.Materiais.Remove(e); db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Material", EntidadeId = m.Id, TipoOperacao = TipoOperacaoOutbox.DELETE, PayloadJson = JsonSerializer.Serialize(e), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" }); await db.SaveChangesAsync(); await tx.CommitAsync(); await LoadAsync(); } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Excluir material"); }
    }
}
