using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;

namespace Kolora.Orcamentos.Services;

public record ItemCalculado(string ProdutoNome, TipoCalculo Tipo, decimal Quantidade, decimal Largura, decimal Altura, decimal CustoUnitario, decimal Margem, decimal PrecoFinal, decimal CustoTotal);

public class CalculadoraService
{
    public decimal CalcularArea(decimal largura, decimal altura) => largura * altura;

    public decimal CalcularCusto(Produto produto, decimal largura, decimal altura, decimal quantidade)
    {
        return produto.TipoCalculo switch
        {
            TipoCalculo.M2 => produto.PrecoCustoBase * largura * altura * quantidade,
            TipoCalculo.MetroLinear => produto.PrecoCustoBase * largura * quantidade,
            _ => produto.PrecoCustoBase * quantidade
        };
    }

    public decimal CalcularPreco(decimal custo, decimal margem) => custo * (1 + margem);

    public ItemCalculado Calcular(Produto produto, decimal largura, decimal altura, decimal qtd, decimal? margemOverride = null)
    {
        var margem = margemOverride ?? produto.MargemPadrao;
        var custo = CalcularCusto(produto, largura, altura, qtd);
        var preco = CalcularPreco(custo, margem);
        return new ItemCalculado(produto.Nome, produto.TipoCalculo, qtd, largura, altura, produto.PrecoCustoBase, margem, preco, custo);
    }
}
