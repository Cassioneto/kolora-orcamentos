namespace Kolora.Orcamentos.Models.Entities;

public class Material
{
    public Guid Id { get; set; }
    public Guid GraficaId { get; set; }
    public required string Nome { get; set; }
    public string Unidade { get; set; } = "unidade";
    public decimal StockAtual { get; set; }
    public decimal StockMinimo { get; set; }
    public DateTime AtualizadoEm { get; set; }
}