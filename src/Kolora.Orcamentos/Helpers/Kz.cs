namespace Kolora.Orcamentos.Helpers;

/// <summary>
/// Formato de moeda de Angola: "Kz 7,272.50".
/// Usar em vez de StringFormat=C (que depende da cultura do Windows e pode sair $).
/// </summary>
public static class Kz
{
    public static string Format(decimal v)
        => "Kz " + v.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
}
