using Microsoft.Extensions.Configuration;

namespace Kolora.Orcamentos.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _config;
    private readonly IGraficaIdService _graficaId;
    public ConfigurationService(IConfiguration config, IGraficaIdService graficaId)
    {
        _config = config;
        _graficaId = graficaId;
    }
    public string SupabaseUrl => _config["Supabase:Url"] ?? "";
    public string SupabaseAnonKey => _config["Supabase:AnonKey"] ?? "";
    public Guid GraficaId => _graficaId.GraficaId;
}
