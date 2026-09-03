using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Serilog;
using Kolora.Orcamentos.Helpers;

namespace Kolora.Orcamentos.Services;

public class SupabaseService : ISupabaseService
{
    private readonly HttpClient _http;
    private readonly IConfigurationService _cfg;

    public SupabaseService(IConfigurationService cfg)
    {
        _cfg = cfg;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        // Headers estáveis: apikey/x-grafica-id só quando configurado
        // A anon key é aplicada por-request (ApplyAuth) porque o cofre DPAPI pode
        // ser atualizado em runtime pela aba Configuracoes sem reiniciar o app.
    }

    /// <summary>Só tenta falar com o Supabase se URL+AnonKey configuradas. Caso contrário o app segue 100% offline.</summary>
    private bool Ready => _cfg.IsSupabaseConfigured;

    /// <summary>Headers frescos por-request: anon key pode entrar/sair do cofre DPAPI em runtime.</summary>
    private void ApplyAuth(HttpRequestMessage req)
    {
        req.Headers.Add("apikey", _cfg.SupabaseAnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.SupabaseAnonKey);
        req.Headers.Add("x-grafica-id", _cfg.GraficaId.ToString());
        req.Headers.Add("Prefer", "resolution=merge-duplicates");
    }

    public async Task<bool> PushOutboxAsync(Guid graficaId, string entidade, string payloadJson, string operacao)
    {
        if (!Ready) return false;
        try
        {
            var table = MapEntidade(entidade);
            var url = _cfg.SupabaseUrl.TrimEnd('/') + "/rest/v1/" + table;
            var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");
            var id = ExtrairId(payloadJson);
            HttpResponseMessage resp;
            if (operacao == "INSERT")
            {
                using var post = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                ApplyAuth(post);
                resp = await _http.SendAsync(post);
            }
            else if (operacao == "UPDATE")
            {
                using var patch = new HttpRequestMessage(new HttpMethod("PATCH"), url + "?id=eq." + id) { Content = content };
                ApplyAuth(patch);
                resp = await _http.SendAsync(patch);
            }
            else
            {
                using var del = new HttpRequestMessage(HttpMethod.Delete, url + "?id=eq." + id);
                ApplyAuth(del);
                resp = await _http.SendAsync(del);
            }
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                // Erros de config (42501 sem GRANT / PGRST205 sem tabela) sao logados 1x por evento
                // com hint acionavel; falhas repetidas nao spamam o log.
                var code = TryGetJsonCode(body);
                if ((int)resp.StatusCode is 401 or 403 or 404)
                {
                    if (code == "42501")
                        Log.Warning("Push {Entidade}: sem GRANT para anon — rode supabase/migrations/003_grants_anon.sql no SQL Editor. Body: {Body}", entidade, body);
                    else if (code == "PGRST205")
                        Log.Warning("Push {Entidade}: tabela nao existe no schema — rode supabase/migrations/001_initial_schema.sql. Body: {Body}", entidade, body);
                    else
                        Log.Warning("Push {Entidade} falhou {Status}: {Body}", entidade, resp.StatusCode, body);
                }
                else
                    Log.Debug("Push {Entidade} falhou {Status}: {Body}", entidade, resp.StatusCode, body);
            }
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) { Log.Debug("PushOutbox offline/erro {Entidade}: {Msg}", entidade, ex.Message); return false; }
    }

    public async Task<List<T>> PullAsync<T>(string table, DateTime? since, Guid graficaId) where T : class
    {
        if (!Ready) return new();
        try
        {
            var url = _cfg.SupabaseUrl.TrimEnd('/') + "/rest/v1/" + table + "?grafica_id=eq." + graficaId;
            if (since.HasValue) url += "&atualizado_em=gt." + Uri.EscapeDataString(since.Value.ToString("o"));
            url += "&order=atualizado_em.asc";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyAuth(req);
            using var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var list = await resp.Content.ReadFromJsonAsync<List<T>>(OutboxJson.Options);
            return list ?? new();
        }
        catch (Exception ex) { Log.Debug("Pull {Table} offline/erro: {Msg}", table, ex.Message); return new(); }
    }

    public async Task<List<Models.Entities.PedidoOrcamento>> PullPedidosNovosAsync(Guid graficaId)
    {
        if (!Ready) return new();
        try
        {
            var url = _cfg.SupabaseUrl.TrimEnd('/') + "/rest/v1/pedidos_orcamento?grafica_id=eq." + graficaId + "&status=eq.Novo&order=criado_em_origem.asc";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyAuth(req);
            using var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var list = await resp.Content.ReadFromJsonAsync<List<Models.Entities.PedidoOrcamento>>(OutboxJson.Options);
            return list ?? new();
        }
        catch (Exception ex) { Log.Debug("Pull pedidos offline/erro: {Msg}", ex.Message); return new(); }
    }

    private static string? TryGetJsonCode(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("code", out var c) ? c.GetString() : null;
        }
        catch { return null; }
    }

    private static string? ExtrairId(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("id", out var idSnake)) return idSnake.GetString();
        if (root.TryGetProperty("Id", out var idPascal)) return idPascal.GetString();
        return null;
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
