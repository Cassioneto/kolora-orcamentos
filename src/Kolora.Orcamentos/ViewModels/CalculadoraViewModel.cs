using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
using Kolora.Orcamentos.Services;

namespace Kolora.Orcamentos.ViewModels;
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
        Detalhe = $"{item.ProdutoNome} | {item.Tipo} | {q} x {l:0.##}x{a:0.##} | Custo: {item.CustoTotal:C} | Margem: {item.Margem:P0} → Preço: {item.PrecoFinal:C}";
    }

    [RelayCommand] public void Limpar() { Largura = "1"; Altura = "1"; Quantidade = "1"; Margem = ProdutoSelecionado?.MargemPadrao.ToString("0.##") ?? "0.40"; Recalcular(); }
}
