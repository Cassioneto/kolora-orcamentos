using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
using Kolora.Orcamentos.Services;
using Kolora.Orcamentos.Helpers;

namespace Kolora.Orcamentos.ViewModels;

/// <summary>Linha da tabela de sugestões de preço por margem de venda desejada.</summary>
public record MargemSugestaoVm(string MargemVenda, string MarkupNecessario, string Multiplicador, string PrecoSugerido, string Lucro);

public partial class CalculadoraViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    private readonly CalculadoraService _calc;
    [ObservableProperty] private ObservableCollection<Produto> _produtos = new();
    [ObservableProperty] private Produto? _produtoSelecionado;
    [ObservableProperty] private string _largura = "1";
    [ObservableProperty] private string _altura = "1";
    [ObservableProperty] private string _quantidade = "1";
    [ObservableProperty] private string _margem = "";
    [ObservableProperty] private decimal _custoCalculado;
    [ObservableProperty] private decimal _precoCalculado;
    [ObservableProperty] private string _detalhe = "Selecione um produto para calcular.";

    // ---- Aba Margem: calculadora reversa custo + preço -> margem real ----
    [ObservableProperty] private string _margemCustoInput = "";
    [ObservableProperty] private string _margemPrecoInput = "";
    [ObservableProperty] private decimal _margemLucro;
    [ObservableProperty] private decimal _margemVendaPct;
    [ObservableProperty] private decimal _markupCustoPct;
    [ObservableProperty] private decimal _multiplicador;
    [ObservableProperty] private string _margemExplicacao = "Introduza o custo e o preço para ver a margem real.";
    [ObservableProperty] private ObservableCollection<MargemSugestaoVm> _sugestoes = new();

    public CalculadoraViewModel(GraficaIdService grafica, CalculadoraService calc) { _grafica = grafica; _calc = calc; _ = LoadAsync(); }

    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var list = await db.Produtos.Where(p => p.GraficaId == _grafica.GraficaId && p.Ativo).OrderBy(p => p.Nome).ToListAsync();
        Produtos = new ObservableCollection<Produto>(list);
        if (Produtos.Count > 0 && ProdutoSelecionado == null) ProdutoSelecionado = Produtos[0];
        Recalcular();
    }

    partial void OnProdutoSelecionadoChanged(Produto? value) { if (value != null && string.IsNullOrWhiteSpace(Margem)) Margem = value.MargemPadrao.ToString("0.##"); Recalcular(); }
    partial void OnLarguraChanged(string value) => Recalcular();
    partial void OnAlturaChanged(string value) => Recalcular();
    partial void OnQuantidadeChanged(string value) => Recalcular();
    partial void OnMargemChanged(string value) => Recalcular();
    partial void OnMargemCustoInputChanged(string value) => RecalcMargem();
    partial void OnMargemPrecoInputChanged(string value) => RecalcMargem();

    private void Recalcular()
    {
        if (ProdutoSelecionado == null) { Detalhe = "Selecione um produto."; return; }
        if (!decimal.TryParse(Largura.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var l)) l = 0;
        if (!decimal.TryParse(Altura.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var a)) a = 0;
        if (!decimal.TryParse(Quantidade.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var q)) q = 1;
        decimal.TryParse(Margem.Replace(",","."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var m);
        if (m == 0) m = ProdutoSelecionado.MargemPadrao;
        var item = _calc.Calcular(ProdutoSelecionado, l, a, q, m);
        CustoCalculado = item.CustoTotal; PrecoCalculado = item.PrecoFinal;
        Detalhe = $"{item.ProdutoNome} | {item.Tipo} | {q} x {l:0.##}x{a:0.##} | Custo: {Kz.Format(item.CustoTotal)} | Margem: {item.Margem:P0} → Preço: {Kz.Format(item.PrecoFinal)}";
    }

    [RelayCommand] public void Limpar() { Largura = "1"; Altura = "1"; Quantidade = "1"; Margem = ProdutoSelecionado?.MargemPadrao.ToString("0.##") ?? "0.40"; Recalcular(); }

    // ---------------- Aba Margem ----------------

    private static bool ParseNum(string? s, out decimal v) =>
        decimal.TryParse((s ?? "").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v);

    private void RecalcMargem()
    {
        if (!ParseNum(MargemCustoInput, out var custo) || custo <= 0 ||
            !ParseNum(MargemPrecoInput, out var preco) || preco <= 0)
        {
            MargemLucro = 0; MargemVendaPct = 0; MarkupCustoPct = 0; Multiplicador = 0;
            Sugestoes = new();
            MargemExplicacao = "Introduza o custo e o preço para ver a margem real.";
            return;
        }

        MargemLucro = preco - custo;
        MargemVendaPct = MargemLucro / preco;          // margem sobre VENDA (o que sobra de cada venda)
        MarkupCustoPct = MargemLucro / custo;          // markup sobre CUSTO (o que o app usa nos produtos)
        Multiplicador = preco / custo;                  // ex: 1.75x

        // Sugestões: preço para margens de venda 20/30/40/50% => custo / (1 - m)
        var linhas = new List<MargemSugestaoVm>();
        foreach (var m in new[] { 0.20m, 0.30m, 0.40m, 0.50m })
        {
            var pSug = decimal.Round(custo / (1 - m), 2);
            linhas.Add(new MargemSugestaoVm(
                $"{m:P0}", $"{(m / (1 - m)):P0}", $"{(1 / (1 - m)):0.##}x", Kz.Format(pSug), Kz.Format(pSug - custo)));
        }
        Sugestoes = new ObservableCollection<MargemSugestaoVm>(linhas);

        MargemExplicacao =
            $"CUSTO {Kz.Format(custo)} → PREÇO {Kz.Format(preco)} = LUCRO {Kz.Format(MargemLucro)}\n" +
            $"• Margem sobre VENDA: {MargemVendaPct:P1} — de cada {Kz.Format(preco)} vendidos, {Kz.Format(MargemLucro)} ficam para você. É esta a 'margem' que o dono da gráfica pensa.\n" +
            $"• Markup sobre CUSTO: {MarkupCustoPct:P1} — você cobrou {Multiplicador:0.##}x o custo. É este o número que o campo 'Margem' dos produtos usa (Preço = Custo × (1 + Margem)).\n" +
            (MargemVendaPct <= 0
                ? "⚠ ATENÇÃO: preço igual ou abaixo do custo — você está a perder dinheiro em cada venda."
                : MargemVendaPct < 0.15m
                    ? "⚠ Margem apertada (menos de 15% de venda). Cubra água, luz, renda e o seu tempo antes de aceitar."
                    : $"✓ Exemplo: para ter {MargemVendaPct:P0} de margem de venda, sempre multiplique o custo por {Multiplicador:0.##}.");
    }

    [RelayCommand] public void LimparMargem() { MargemCustoInput = ""; MargemPrecoInput = ""; RecalcMargem(); }
}
