using System.Text.Json;
using System.Text.Json.Nodes;
namespace SMSR.App.Mvp;

internal static class GraphRoleOutputSchema
{
    internal static string Create(GraphRoleContext context)
    {
        var schema = JsonNode.Parse(Json)!;
        var ids = schema["properties"]!["claims"]!["items"]!["properties"]!["evidenceIds"]!;
        ids["minItems"] = 1;
        ids["maxItems"] = 4;
        ids["items"]!["enum"] = JsonSerializer.SerializeToNode(context.Evidence.Select(e => e.Id).ToArray());
        return schema.ToJsonString();
    }
    internal const string Json = """
        {"type":"object","properties":{"claims":{"type":"array","minItems":1,"maxItems":12,
        "items":{"type":"object","properties":{
        "kind":{"type":"string","enum":["ROLE","BEHAVIOR","RELATION","RATIONALE","LIMITATION"]},
        "text":{"type":"string"},"confidence":{"type":"string","enum":["EXTRACTED","INFERRED"]},
        "evidenceIds":{"type":"array","items":{"type":"string"}},"supportingQuote":{"type":"string"}},
        "required":["kind","text","confidence","evidenceIds","supportingQuote"],"additionalProperties":false}}},
        "required":["claims"],"additionalProperties":false}
        """;
}
