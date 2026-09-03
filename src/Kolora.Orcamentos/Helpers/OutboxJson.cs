using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kolora.Orcamentos.Helpers;

/// <summary>
/// Serialização padrão dos payloads do Outbox — snake_case + enums como string,
/// compatível com as colunas do Postgres/Supabase (id, grafica_id, atualizado_em...).
/// </summary>
public static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
