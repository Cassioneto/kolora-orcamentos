using System.Collections.ObjectModel;
using System.IO;
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
public record ItemOrcamentoVm(string Nome, string Tipo, decimal Qtd, decimal Largura, decimal Altura, decimal Custo, decimal Preco);

public partial class OrcamentosViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    private readonly CalculadoraService _calc;
    private readonly PdfService _pdf;
    private readonly StockService _stock;
    [ObservableProperty] private ObservableCollection<Orcamento> _orcamentos = new();
    [ObservableProperty] private ObservableCollection<Cliente> _clientes = new();
    [ObservableProperty] private ObservableCollection<Produto> _produtos = new();
    [ObservableProperty] private Cliente? _clienteSelecionado;
    [ObservableProperty] private Produto? _produtoSelecionado;
    [ObservableProperty] private string _largura = "1";
    [ObservableProperty] private string _altura = "1";
    [ObservableProperty] private string _quantidade = "1";
    [ObservableProperty] private ObservableCollection<ItemOrcamentoVm> _itens = new();
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private string _mensagem = "";

    public OrcamentosViewModel(GraficaIdService grafica, CalculadoraService calc, PdfService pdf, StockService stock)
    { _grafica = grafica; _calc = calc; _pdf = pdf; _stock = stock; _ = LoadAsync(); }

    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        Orcamentos = new ObservableCollection<Orcamento>(await db.Orcamentos.Where(o => o.GraficaId == _grafica.GraficaId).OrderByDescending(o => o.Data).ToListAsync());
        Clientes = new ObservableCollection<Cliente>(await db.Clientes.Where(c => c.GraficaId == _grafica.GraficaId).OrderBy(c => c.Nome).ToListAsync());
        Produtos = new ObservableCollection<Produto>(await db.Produtos.Where(p => p.GraficaId == _grafica.GraficaId && p.Ativo).OrderBy(p => p.Nome).ToListAsync());
        if (Produtos.Count > 0 && ProdutoSelecionado == null) ProdutoSelecionado = Produtos[0];
        // Cliente fica sem seleção: placeholder "Selecione o cliente" evita orçamento sujo no histórico
    }

    [RelayCommand] public void AdicionarItem()
    {
        if (ProdutoSelecionado == null) { Mensagem = "Selecione um produto."; return; }
        if (!decimal.TryParse(Largura.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var l)) l = 1;
        if (!decimal.TryParse(Altura.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var a)) a = 1;
        if (!decimal.TryParse(Quantidade.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var q)) q = 1;
        var calc = _calc.Calcular(ProdutoSelecionado, l, a, q);
        Itens.Add(new ItemOrcamentoVm(calc.ProdutoNome, calc.Tipo.ToString(), q, l, a, calc.CustoTotal, calc.PrecoFinal));
        Total = Itens.Sum(i => i.Preco);
        Mensagem = $"{calc.ProdutoNome} adicionado. Total: {Kz.Format(Total)}";
    }

    [RelayCommand] public void RemoverItem(ItemOrcamentoVm item) { Itens.Remove(item); Total = Itens.Sum(i => i.Preco); }

    [RelayCommand] public void LimparItens() { Itens.Clear(); Total = 0; Mensagem = ""; }

    [RelayCommand] public async Task SalvarOrcamentoAsync()
    {
        if (ClienteSelecionado == null) { Mensagem = "Selecione um cliente."; return; }
        if (Itens.Count == 0) { Mensagem = "Adicione pelo menos um item."; return; }
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var grafica = await db.Graficas.FindAsync(_grafica.GraficaId);
            var config = await db.ConfiguracoesGrafica.FirstOrDefaultAsync(c => c.GraficaId == _grafica.GraficaId);
            if (grafica == null || config == null) { Mensagem = "Gráfica não configurada."; return; }
            var itensJson = OutboxJson.Serialize(Itens);
            var orc = new Orcamento { Id = Guid.NewGuid(), GraficaId = _grafica.GraficaId, ClienteId = ClienteSelecionado.Id, Data = DateTime.UtcNow, Total = Total, ItensJson = itensJson, Validade = DateTime.UtcNow.AddDays(config.ValidadePadraoDias), AtualizadoEm = DateTime.UtcNow };
            // Gerar PDF
            var cliente = ClienteSelecionado;
            var itensPdf = Itens.Select(i => new ItemOrcamentoPdf(i.Nome, i.Tipo, i.Qtd, i.Largura, i.Altura, i.Preco)).ToList();
            var pdfModel = new OrcamentoPdfModel(grafica, config, cliente, itensPdf, Total, orc.Validade);
            var pdfPath = await _pdf.GerarAsync(pdfModel, orc.Id);
            orc.CaminhoPdf = pdfPath;
            db.Orcamentos.Add(orc);
            db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Orcamento", EntidadeId = orc.Id, TipoOperacao = TipoOperacaoOutbox.INSERT, PayloadJson = OutboxJson.Serialize(orc), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" });
            // Abate stock por produto (quantidade)
            var consumoMap = await db.ProdutoMateriais.Where(pm => Produtos.Select(p => p.Id).Contains(pm.ProdutoId)).ToListAsync();
            // Para cada item, abater proporcional
            foreach (var it in Itens)
            {
                var prod = Produtos.FirstOrDefault(p => p.Nome == it.Nome);
                if (prod == null) continue;
                var consumos = consumoMap.Where(c => c.ProdutoId == prod.Id).ToList();
                foreach (var pm in consumos)
                {
                    var mat = await db.Materiais.FindAsync(pm.MaterialId);
                    if (mat == null) continue;
                    mat.StockAtual -= pm.ConsumoPorUnidade * it.Qtd;
                    mat.AtualizadoEm = DateTime.UtcNow;
                    db.Materiais.Update(mat);
                    db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Material", EntidadeId = mat.Id, TipoOperacao = TipoOperacaoOutbox.UPDATE, PayloadJson = OutboxJson.Serialize(mat), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" });
                }
            }
            await db.SaveChangesAsync(); await tx.CommitAsync();
            Mensagem = $"Orçamento salvo! PDF: {Path.GetFileName(pdfPath)}";
            Log.Information("Orcamento {Id} salvo total {Total}", orc.Id, Total);
            // Abrir PDF
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfPath) { UseShellExecute = true }); } catch {}
            await LoadAsync(); LimparItens();
            ClienteSelecionado = null; // volta ao placeholder
        } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Salvar orcamento"); Mensagem = "Erro: " + ex.Message; }
    }

    [RelayCommand]
    public void AbrirPasta(Orcamento? o)
    {
        try
        {
            // Sem orçamento: abre a pasta base; com orçamento: abre o Explorer já com o PDF selecionado
            string alvo;
            if (o?.CaminhoPdf is string p && !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                alvo = $"/select,\"{p}\"";
            else
                alvo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Kolora", "Orcamentos");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", alvo) { UseShellExecute = true });
        }
        catch (Exception ex) { Mensagem = ex.Message; }
    }

    [RelayCommand] public async Task ExcluirAsync(Orcamento o)
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try { var e = await db.Orcamentos.FindAsync(o.Id); if (e == null) return; db.Orcamentos.Remove(e); db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Orcamento", EntidadeId = o.Id, TipoOperacao = TipoOperacaoOutbox.DELETE, PayloadJson = OutboxJson.Serialize(e), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" }); await db.SaveChangesAsync(); await tx.CommitAsync(); await LoadAsync(); } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Excluir orcamento"); }
    }
}
