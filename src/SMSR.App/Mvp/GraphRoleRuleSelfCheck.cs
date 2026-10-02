namespace SMSR.App.Mvp;

internal static class GraphRoleRuleSelfCheck
{
    internal static void Run(GraphRoleContext context)
    {
        using var schema = System.Text.Json.JsonDocument.Parse(GraphRoleOutputSchema.Create(context));
        var ids = schema.RootElement.GetProperty("properties").GetProperty("claims").GetProperty("items")
            .GetProperty("properties").GetProperty("evidenceIds");
        if (ids.GetProperty("minItems").GetInt32() != 1 || ids.GetProperty("maxItems").GetInt32() != 4 || !ids.GetProperty("items").GetProperty("enum")
            .EnumerateArray().Select(v => v.GetString()).SequenceEqual(context.Evidence.Select(e => e.Id)))
            throw new Exception("Role output evidence ID constraint mismatch");
        var request = GraphRoleSelfCheck.Request(context);
        var marker = "private-provider-output-not-for-storage";
        var invalid = request with { Claims = [request.Claims[0] with { SupportingQuote = marker }] };
        Expect(invalid, "QUOTE_NOT_FOUND", 1);
        Expect(request with { Model = "" }, "REPORT_MODEL", null);
        Expect(request with { Claims = [] }, "REPORT_CLAIM_COUNT", null);
        Expect(request with { Claims = [request.Claims[0] with { EvidenceIds = [] }] }, "CLAIM_EVIDENCE_COUNT", 1);
        Expect(request with { Claims = [request.Claims[0] with { EvidenceIds = ["e0", "e0", "e0", "e0", "e0"] }] }, "CLAIM_EVIDENCE_COUNT", 1);
        Expect(request with { Claims = [request.Claims[0] with { Kind = "BEHAVIOR" }, request.Claims[0]] }, "REPORT_ROLE_FIRST", 1);
        var limitation = request.Claims[0] with { Kind = "LIMITATION", Confidence = "INFERRED", Text = "이 예제 근거 밖의 실행 결과는 확인하지 못했습니다." };
        GraphRoleValidation.Validate(request with { Claims = [request.Claims[0], limitation] }, context);
        Expect(request with { Claims = [request.Claims[0], limitation with { Confidence = "EXTRACTED" }] }, "LIMITATION_CONFIDENCE", 2);
        Expect(request with { Claims = [request.Claims[0], request.Claims[0] with { Kind = "RELATION" }] }, "RELATION_EVIDENCE", 2);
        var helper = context.Evidence[0] with { Id = "helper", Path = "other.py" };
        var scope = context with { Evidence = [.. context.Evidence, helper] };
        Expect(request with { Claims = [request.Claims[0] with { EvidenceIds = ["helper"] }] }, "ROLE_SOURCE", 1, scope);
        foreach (var (claim, rule) in new[] {
            (request.Claims[0] with { Kind = "bad" }, "CLAIM_KIND"),
            (request.Claims[0] with { Confidence = "bad" }, "CLAIM_CONFIDENCE"),
            (request.Claims[0] with { Text = "" }, "CLAIM_TEXT_LENGTH"),
            (request.Claims[0] with { SupportingQuote = "" }, "CLAIM_QUOTE_LENGTH"),
            (request.Claims[0] with { EvidenceIds = ["not-an-evidence"] }, "CLAIM_EVIDENCE_IDS") })
            Expect(request with { Claims = [request.Claims[0], claim] }, rule, 2);
        void Expect(GraphRoleRequest value, string rule, int? index, GraphRoleContext? target = null)
        {
            try { GraphRoleValidation.Validate(value, target ?? context); }
            catch (GraphRoleRuleException error)
            {
                if (error.Rule != rule || error.ClaimIndex != index || error.Message.Contains(marker))
                    throw new Exception("Role rule diagnostic leaked content or lost claim position");
                return;
            }
            throw new Exception("Invalid role accepted without a rule diagnostic");
        }
    }
}
