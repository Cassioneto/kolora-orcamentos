namespace Kolora.Orcamentos.Services;

/// <summary>Estado da sessão WhatsApp (Baileys) — exibido na aba Configurações.</summary>
public enum WhatsAppStatus
{
    Desligado,
    Ligando,
    AguardandoQR,
    Ligado,
    Erro
}

public interface IWhatsAppService
{
    WhatsAppStatus Status { get; }
    bool Ligado { get; }
    /// <summary>QR atual em PNG (para exibir na tela de pareamento). Null quando não há QR pendente.</summary>
    byte[]? QrPng { get; }
    string UltimoErro { get; }
    int PendentesFila { get; }
    event EventHandler? EstadoMudou;
    Task LigarAsync();
    Task DesligarAsync(bool desemparelhar = false);
    /// <summary>Envia PDF + legenda. Lança exceção se falhar (o chamador enfileira).</summary>
    Task EnviarPdfAsync(string jid, string pdfPath, string legenda, string nomeFicheiro);
}
