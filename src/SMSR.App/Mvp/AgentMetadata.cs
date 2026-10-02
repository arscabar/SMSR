using System.Text.Json;
using System.Text.Json.Serialization;

namespace SMSR.App.Mvp;

internal sealed record AgentMetadata(string? Model = null, string? ReasoningEffort = null,
    string? ParentAgentId = null)
{
    public string ToJson() => JsonSerializer.Serialize(this,
        new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

    public static AgentMetadata Parse(string json)
    {
        try { return JsonSerializer.Deserialize<AgentMetadata>(json) ?? new(); }
        catch (JsonException) { return new(); }
    }
}
