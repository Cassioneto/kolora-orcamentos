using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Serilog;

namespace Kolora.Orcamentos.Services;

public class SupabaseService : ISupabaseService
{
    private readonly HttpClient _http;
    private readonly IConfigurationService _cfg;

    public SupabaseService(IConfigurationService cfg)
    {
        _cfg = cfg;
        _http = new HttpClient();
        if (!string.IsNullOrWhiteSpace(cfg.SupabaseAnonKey))
        {
            _http.DefaultRequestHeaders.Add("apikey", cfg.SupabaseAnonKey);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cfg.SupabaseAnonKey);
        }
        if (!string.IsNullOrWhiteSpace(cfg.GraficaId.ToString()))
            _http.DefaultRequestHeaders.Add("x-grafica-id", cfg.GraficaId.ToString());
        _http.DefaultRequestHeaders.Add("Prefer", "resolution=merge-duplicates");
    }

    public async Task<bool> PushOutboxAsync(Guid graficaId, string entidade, string payloadJson, string operacao)
    {
        if (string.IsNullOrWhiteSpace(_cfg.SupabaseUrl) || string.IsNullOrWhiteSpace(_cfg.SupabaseAnonKey))
            return false;
        try
        {
            var table = MapEntidade(entidade);
            var url = _cfg.SupabaseUrl.TrimEnd('/') + "/rest/v1/" + table;
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage resp;
            if (operacao == "INSERT")
                resp = await _http.PostAsync(url, content);
            else if (operacao == "UPDATE")
            {
                var id = JsonDocument.Parse(payloadJson).RootElement.GetProperty("Id").GetString();
                resp = await _http.PatchAsync(url + "?id=eq." + id, content);
            }
            else
            {
                var id = JsonDocument.Parse(payloadJson).RootElement.GetProperty("Id").GetString();
                resp = await _http.DeleteAsync(url + "?id=eq." + id);
            }
            if (!resp.IsSuccessStatusCode)
                Log.Warning("Push {Entidade} falhou: {Status} {Body}", entidade, resp.StatusCode, await resp.Content.ReadAsStringAsync());
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) { Log.Error(ex, "PushOutbox erro {Entidade}", entidade); return false; }
    }

    public async Task<List<T>> PullAsync<T>(string table, DateTime? since, Guid graficaId) where T : class
    {
        if (string.IsNullOrWhiteSpace(_cfg.SupabaseUrl)) return new();
        try
        {
            var url = _cfg.SupabaseUrl.TrimEnd('/') + "/rest/v1/" + table + "?grafica_id=eq." + graficaId;
            if (since.HasValue) url += "&atualizado_em=gt." + Uri.EscapeDataString(since.Value.ToString("o"));
            url += "&order=atualizado_em.asc";
            var list = await _http.GetFromJsonAsync<List<T>>(url);
            return list ?? new();
        }
        catch (Exception ex) { Log.Error(ex, "Pull {Table} erro", table); return new(); }
    }

    public async Task<List<Models.Entities.PedidoOrcamento>> PullPedidosNovosAsync(Guid graficaId)
    {
        if (string.IsNullOrWhiteSpace(_cfg.SupabaseUrl)) return new();
        try
        {
            var url = _cfg.SupabaseUrl.TrimEnd('/') + "/rest/v1/pedidos_orcamento?grafica_id=eq." + graficaId + "&status=eq.Novo&order=criado_em_origem.asc";
            var list = await _http.GetFromJsonAsync<List<Models.Entities.PedidoOrcamento>>(url);
            return list ?? new();
        }
        catch (Exception ex) { Log.Error(ex, "Pull pedidos erro"); return new(); }
    }

    private static string MapEntidade(string e) => e.ToLower() switch
    {
        "grafica" => "graficas",
        "produto" => "produtos",
        "material" => "materiais",
        "cliente" => "clientes",
        "orcamento" => "orcamentos",
        "pedidoorcamento" or "pedidosorcamento" => "pedidos_orcamento",
        _ => e.ToLower() + "s"
    };
}
