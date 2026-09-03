namespace Kolora.Orcamentos.Models.Entities;

public class Grafica
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public string? Telefone { get; set; }
    public string? Localizacao { get; set; }
    public string? LogoPath { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}