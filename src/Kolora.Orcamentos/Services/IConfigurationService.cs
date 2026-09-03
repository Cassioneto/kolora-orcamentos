namespace Kolora.Orcamentos.Services;
public interface IConfigurationService
{
    string SupabaseUrl { get; }
    string SupabaseAnonKey { get; }
    Guid GraficaId { get; }
}
