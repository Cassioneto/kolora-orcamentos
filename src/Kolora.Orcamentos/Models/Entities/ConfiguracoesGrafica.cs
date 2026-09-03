namespace Kolora.Orcamentos.Models.Entities;

public class ConfiguracoesGrafica
{
    public Guid Id { get; set; }
    public Guid GraficaId { get; set; }
    public string? LogoPath { get; set; }
    public string? NomeExibicaoPdf { get; set; }
    public string? MensagemRodapePdf { get; set; }
    public decimal MargemPadraoGlobal { get; set; }
    public int ValidadePadraoDias { get; set; } = 3;
    public string? Iban { get; set; }
    public string? MulticaixaExpressNumero { get; set; }
    public DateTime AtualizadoEm { get; set; }
}