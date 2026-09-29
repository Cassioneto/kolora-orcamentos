namespace Kolora.Orcamentos.Models.Entities;

/// <summary>
/// Fila de envios de orçamento via WhatsApp (offline-first).
/// O clique "Enviar" NUNCA depende de rede: grava Pendente local e o worker
/// drena quando houver internet + sessão WhatsApp ligada. Nunca descarta.
/// </summary>
public class FilaEnvioWhatsApp
{
    public Guid Id { get; set; }
    public Guid GraficaId { get; set; }
    public Guid OrcamentoId { get; set; }
    public string TelefoneDestino { get; set; } = "";   // como o usuário digitou
    public string JidDestino { get; set; } = "";        // normalizado: 2449XXXXXXXX@s.whatsapp.net
    public string PdfPath { get; set; } = "";           // snapshot do caminho do PDF na hora do clique
    public string Legenda { get; set; } = "";           // texto que acompanha o PDF
    public string Status { get; set; } = "Pendente";    // Pendente | Enviado | Erro
    public int Tentativas { get; set; }
    public string? UltimoErro { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
