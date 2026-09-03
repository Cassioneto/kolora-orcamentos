namespace Kolora.Orcamentos.Services;
public interface IConfigurationService
{
    string SupabaseUrl { get; }
    string SupabaseAnonKey { get; }
    Guid GraficaId { get; }
    /// <summary>True apenas se URL + AnonKey preenchidas. Sem isso o app roda 100% offline.</summary>
    bool IsSupabaseConfigured { get; }
    /// <summary>Invalida o cache da key (chamado ao guardar/remover em Configuracoes).</summary>
    void InvalidateCache();
}
