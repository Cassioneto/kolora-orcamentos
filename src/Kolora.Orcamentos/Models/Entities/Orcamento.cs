namespace Kolora.Orcamentos.Models.Entities;

public class Orcamento
{
    public Guid Id { get; set; }
    public Guid GraficaId { get; set; }
    public Guid ClienteId { get; set; }
    public DateTime Data { get; set; }
    public decimal Total { get; set; }
    public required string ItensJson { get; set; }
    public DateTime Validade { get; set; }
    public string? CaminhoPdf { get; set; }
    public DateTime AtualizadoEm { get; set; }
}