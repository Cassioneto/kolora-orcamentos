namespace Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
public class OutboxEvent
{
    public Guid Id { get; set; }
    public required string Entidade { get; set; }
    public Guid EntidadeId { get; set; }
    public TipoOperacaoOutbox TipoOperacao { get; set; }
    public required string PayloadJson { get; set; }
    public DateTime CriadoEm { get; set; }
    public int TentativasEnvio { get; set; }
    public required string StatusSync { get; set; }
    public string? UltimoErro { get; set; }
}
