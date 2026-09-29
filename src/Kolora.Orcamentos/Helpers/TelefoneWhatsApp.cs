namespace Kolora.Orcamentos.Helpers;

/// <summary>
/// Normalização de telefone de Angola para JID do WhatsApp.
/// Aceita: 923456789, +244 923..., 00244..., 244923... → 244923456789@s.whatsapp.net
/// </summary>
public static class TelefoneWhatsApp
{
    public static bool TentarNormalizar(string? telefone, out string digitos)
    {
        digitos = "";
        if (string.IsNullOrWhiteSpace(telefone)) return false;

        var d = new string(telefone.Where(char.IsDigit).ToArray());
        if (d.StartsWith("00")) d = d[2..];          // 00244... -> 244...
        if (d.Length == 9 && d.StartsWith('9')) d = "244" + d;   // 923... -> 244923...
        if (d.Length != 12 || !d.StartsWith("244")) return false;

        digitos = d;
        return true;
    }

    public static string ParaJid(string digitos) => $"{digitos}@s.whatsapp.net";
}
