namespace Kolora.Orcamentos.Models.Entities;

public class Cliente
{
    public Guid Id { get; set; }
    public Guid GraficaId { get; set; }
    public required string Nome { get; set; }
    public string? Telefone { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}