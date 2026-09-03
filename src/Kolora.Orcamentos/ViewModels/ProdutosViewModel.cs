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
public partial class ProdutosViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    [ObservableProperty] private ObservableCollection<Produto> _produtos = new();
    [ObservableProperty] private Produto? _selecionado;
    [ObservableProperty] private string _nome = "";
    [ObservableProperty] private TipoCalculo _tipoCalculo = TipoCalculo.M2;
    [ObservableProperty] private string _precoCusto = "0";
    [ObservableProperty] private string _margem = "0.40";
    [ObservableProperty] private bool _ativo = true;
    [ObservableProperty] private string _filtro = "";

    public Array Tipos => Enum.GetValues(typeof(TipoCalculo));

    public ProdutosViewModel(GraficaIdService grafica) { _grafica = grafica; _ = LoadAsync(); }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            using var db = new KoloraDbContext(_grafica.DbPath);
            var list = await db.Produtos.Where(p => p.GraficaId == _grafica.GraficaId).OrderBy(p => p.Nome).ToListAsync();
            Produtos = new ObservableCollection<Produto>(list);
        } catch (Exception ex) { Log.Error(ex, "Load produtos"); }
    }

    [RelayCommand]
    public void Novo() { Selecionado = null; Nome = ""; TipoCalculo = TipoCalculo.M2; PrecoCusto = "0"; Margem = "0.40"; Ativo = true; }

    [RelayCommand]
    public void Editar(Produto p)
    {
        Selecionado = p; Nome = p.Nome; TipoCalculo = p.TipoCalculo;
        PrecoCusto = p.PrecoCustoBase.ToString("0.##"); Margem = p.MargemPadrao.ToString("0.##"); Ativo = p.Ativo;
    }

    [RelayCommand]
    public async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome)) return;
        if (!decimal.TryParse(PrecoCusto.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var preco)) preco = 0;
        if (!decimal.TryParse(Margem.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var margem)) margem = 0.40m;
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            Produto p;
            string op;
            if (Selecionado == null)
            {
                p = new Produto { Id = Guid.NewGuid(), GraficaId = _grafica.GraficaId, Nome = Nome.Trim(), TipoCalculo = TipoCalculo, PrecoCustoBase = preco, MargemPadrao = margem, Ativo = Ativo, AtualizadoEm = DateTime.UtcNow };
                db.Produtos.Add(p); op = "INSERT";
            }
            else
            {
                p = await db.Produtos.FindAsync(Selecionado.Id) ?? Selecionado;
                p.Nome = Nome.Trim(); p.TipoCalculo = TipoCalculo; p.PrecoCustoBase = preco; p.MargemPadrao = margem; p.Ativo = Ativo; p.AtualizadoEm = DateTime.UtcNow;
                db.Produtos.Update(p); op = "UPDATE";
            }
            var outbox = new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Produto", EntidadeId = p.Id, TipoOperacao = op == "INSERT" ? TipoOperacaoOutbox.INSERT : TipoOperacaoOutbox.UPDATE, PayloadJson = JsonSerializer.Serialize(p), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" };
            db.OutboxEvents.Add(outbox);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            Log.Information("Produto salvo {Nome} {Op}", p.Nome, op);
            await LoadAsync(); Novo();
        } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Salvar produto"); }
    }

    [RelayCommand]
    public async Task ExcluirAsync(Produto p)
    {
        if (p == null) return;
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var ent = await db.Produtos.FindAsync(p.Id); if (ent == null) return;
            db.Produtos.Remove(ent);
            var outbox = new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Produto", EntidadeId = p.Id, TipoOperacao = TipoOperacaoOutbox.DELETE, PayloadJson = JsonSerializer.Serialize(ent), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" };
            db.OutboxEvents.Add(outbox);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            await LoadAsync();
        } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Excluir produto"); }
    }
}
