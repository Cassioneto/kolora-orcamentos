using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Kolora.Orcamentos.Services;

/// <summary>
/// Cofre de credenciais com DPAPI do Windows (CurrentUser).
/// A anon key do Supabase fica cifrada e só abre na conta Windows do dono da gráfica —
/// copiada para pendrive/outro PC é ilegível. O appsettings.json distribui vazio.
/// </summary>
public class SupabaseKeyVault
{
    private readonly string _binPath;

    public SupabaseKeyVault(GraficaIdService grafica)
    {
        _binPath = Path.Combine(grafica.AppDataPath, "supabase.bin");
    }

    public string? LoadAnonKey()
    {
        try
        {
            if (!File.Exists(_binPath)) return null;
            var encrypted = File.ReadAllBytes(_binPath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(decrypted));
            return doc.RootElement.TryGetProperty("anon_key", out var k) ? k.GetString() : null;
        }
        catch
        {
            // .bin corrompido ou conta Windows diferente — ignora e segue offline
            return null;
        }
    }

    public void SaveAnonKey(string? anonKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(anonKey)) { File.Delete(_binPath); return; }
            var json = JsonSerializer.Serialize(new { anon_key = anonKey.Trim() });
            var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_binPath, encrypted);
        }
        catch (CryptographicException)
        {
            // DPAPI indisponível — não guarda em claro por segurança
            throw;
        }
    }
}
