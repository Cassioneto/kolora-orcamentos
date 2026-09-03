namespace Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
public class Produto
{
    public Guid Id { get; set; }
    public Guid GraficaId { get; set; }
    public required string Nome { get; set; }
    public TipoCalculo TipoCalculo { get; set; }
    public decimal PrecoCustoBase { get; set; }
    public decimal MargemPadrao { get; set; } = 0.40m;
    public bool Ativo { get; set; } = true;
    public DateTime AtualizadoEm { get; set; }
}
