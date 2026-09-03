namespace Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
public class PedidoOrcamento
{
    public Guid Id { get; set; }
    public Guid GraficaId { get; set; }
    public string? ClienteNome { get; set; }
    public string? ClienteTelefone { get; set; }
    public string? TextoOriginalCliente { get; set; }
    public string? ProdutoDesejado { get; set; }
    public string? DescricaoPedido { get; set; }
    public decimal? Quantidade { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.Novo;
    public Guid? OrcamentoId { get; set; }
    public DateTime CriadoEmOrigem { get; set; }
    public DateTime RecebidoLocalEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
