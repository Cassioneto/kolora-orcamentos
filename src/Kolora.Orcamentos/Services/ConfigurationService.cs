using Microsoft.Extensions.Configuration;
using Serilog;

namespace Kolora.Orcamentos.Services;

/// <summary>
/// Fonte de configuração do Supabase:
/// 1. Cofre DPAPI (%AppData%/Kolora/supabase.bin) — prioridade, key cifrada por conta Windows
/// 2. appsettings.json — fallback legado (se alguém já preencheu) e fonte da URL
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _config;
    private readonly IGraficaIdService _graficaId;
    private readonly SupabaseKeyVault _vault;
    private string? _cachedKey;
    private bool _keyLoaded;

    public ConfigurationService(IConfiguration config, IGraficaIdService graficaId, SupabaseKeyVault vault)
    {
        _config = config;
        _graficaId = graficaId;
        _vault = vault;
    }

    public string SupabaseUrl => _config["Supabase:Url"]?.Trim() ?? "";
    public Guid GraficaId => _graficaId.GraficaId;

    /// <summary>Key resolvida 1x e cacheada (o cofre DPAPI deixa de ser lido a cada header).</summary>
    public string SupabaseAnonKey
    {
        get
        {
            if (!_keyLoaded)
            {
                _cachedKey = _vault.LoadAnonKey();
                if (string.IsNullOrWhiteSpace(_cachedKey))
                {
                    var legacy = _config["Supabase:AnonKey"]?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(legacy))
                    {
                        _cachedKey = legacy;
                        Log.Information("Anon key em uso do appsettings.json — guarde-a em Configuracoes para cifrar com DPAPI");
                    }
                }
                _keyLoaded = true;
            }
            return _cachedKey ?? "";
        }
    }

    /// <summary>Chamado ao guardar/remover a key em Configuracoes (cache invalidate, sem reiniciar).</summary>
    public void InvalidateCache() { _keyLoaded = false; _cachedKey = null; }

    public bool IsSupabaseConfigured =>
        !string.IsNullOrWhiteSpace(SupabaseUrl) && !string.IsNullOrWhiteSpace(SupabaseAnonKey);
}
