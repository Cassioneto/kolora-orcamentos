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
using Kolora.Orcamentos.Helpers;

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
    public void Limpar() { Selecionado = null; Nome = ""; TipoCalculo = TipoCalculo.M2; PrecoCusto = "0"; Margem = "0.40"; Ativo = true; }

    [RelayCommand]
    public void Editar(Produto p)
    {
        Selecionado = p; Nome = p.Nome; TipoCalculo = p.TipoCalculo;
        PrecoCusto = p.PrecoCustoBase.ToString("0.##"); Margem = p.MargemPadrao.ToString("0.##"); Ativo = p.Ativo;
    }

    [RelayCommand]
    public async Task SalvarAsync()
    {
        try
        {
            Log.Information("Iniciando SalvarAsync para produto: {Nome}", Nome);
            
            if (string.IsNullOrWhiteSpace(Nome)) 
            {
                Log.Warning("Tentativa de salvar produto sem nome");
                return;
            }
            
            Log.Debug("Valores antes da conversão: PrecoCusto='{PrecoCusto}', Margem='{Margem}'", PrecoCusto, Margem);
            
            if (!decimal.TryParse(PrecoCusto.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var preco)) 
            {
                preco = 0;
                Log.Warning("Não foi possível converter PrecoCusto '{PrecoCusto}', usando valor padrão 0", PrecoCusto);
            }
            
            if (!decimal.TryParse(Margem.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var margem)) 
            {
                margem = 0.40m;
                Log.Warning("Não foi possível converter Margem '{Margem}', usando valor padrão 0.40", Margem);
            }
            
            Log.Debug("Valores convertidos: preco={Preco}, margem={Margem}", preco, margem);
            
            var dbPath = _grafica.DbPath;
            Log.Debug("Usando banco de dados em: {DbPath}", dbPath);
            
            using var db = new KoloraDbContext(_grafica.DbPath);
            using var tx = await db.Database.BeginTransactionAsync();
            
            Produto p;
            string op;
            if (Selecionado == null)
            {
                p = new Produto { 
                    Id = Guid.NewGuid(), 
                    GraficaId = _grafica.GraficaId, 
                    Nome = Nome.Trim(), 
                    TipoCalculo = TipoCalculo, 
                    PrecoCustoBase = preco, 
                    MargemPadrao = margem, 
                    Ativo = Ativo, 
                    AtualizadoEm = DateTime.UtcNow 
                };
                db.Produtos.Add(p); 
                op = "INSERT";
                Log.Debug("Criando novo produto: {Produto}", p.Nome);
            }
            else
            {
                p = await db.Produtos.FindAsync(Selecionado.Id) ?? Selecionado;
                p.Nome = Nome.Trim(); 
                p.TipoCalculo = TipoCalculo; 
                p.PrecoCustoBase = preco; 
                p.MargemPadrao = margem; 
                p.Ativo = Ativo; 
                p.AtualizadoEm = DateTime.UtcNow;
                db.Produtos.Update(p); 
                op = "UPDATE";
                Log.Debug("Atualizando produto existente: {Produto} (ID: {Id})", p.Nome, p.Id);
            }
            
            var outbox = new OutboxEvent { 
                Id = Guid.NewGuid(), 
                Entidade = "Produto", 
                EntidadeId = p.Id, 
                TipoOperacao = op == "INSERT" ? TipoOperacaoOutbox.INSERT : TipoOperacaoOutbox.UPDATE, 
                PayloadJson = OutboxJson.Serialize(p), 
                CriadoEm = DateTime.UtcNow, 
                StatusSync = "Pendente" 
            };
            db.OutboxEvents.Add(outbox);
            
            Log.Debug("Salvando alterações no banco de dados...");
            var saved = await db.SaveChangesAsync();
            Log.Debug("SaveChanges retornou: {Saved} registros afetados", saved);
            
            await tx.CommitAsync();
            Log.Information("Produto salvo com sucesso: {Nome} {Op} (ID: {Id})", p.Nome, op, p.Id);
            
            Log.Debug("Recarregando lista de produtos...");
            await LoadAsync();
            
            Log.Debug("Limpando campos com Limpar()...");
            Limpar();
            
            Log.Information("Operação SalvarAsync concluída com sucesso");
        } 
        catch (Exception ex) 
        { 
            Log.Error(ex, "Erro ao salvar produto: {Message}", ex.Message);
            // Tentar fazer rollback se a transação ainda existir
            try { /* Rollback é feito automaticamente quando a transação é descartada */ } 
            catch { }
        }
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
            var outbox = new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Produto", EntidadeId = p.Id, TipoOperacao = TipoOperacaoOutbox.DELETE, PayloadJson = OutboxJson.Serialize(ent), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" };
            db.OutboxEvents.Add(outbox);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            await LoadAsync();
        } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Excluir produto"); }
    }
}
