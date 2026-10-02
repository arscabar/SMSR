namespace SMSR.App.Mvp;

internal static class GraphRoleValidation
{
    internal static void Validate(GraphRoleRequest request, GraphRoleContext context)
    {
        if (request.ExpectedRevision != context.Revision || request.Fingerprint != context.Fingerprint)
            throw new InvalidOperationException("설명 근거가 변경되었습니다. 다시 읽으세요.");
        if (string.IsNullOrWhiteSpace(request.Model) || request.Model.Length > 100
            || GraphDocumentRedaction.Excluded(request.Model)) throw new GraphRoleRuleException("REPORT_MODEL");
        if (request.Claims is null || request.Claims.Count is < 1 or > 12)
            throw new GraphRoleRuleException("REPORT_CLAIM_COUNT");
        if (!request.Claims.Any(c => c?.Kind == "ROLE")) throw new GraphRoleRuleException("REPORT_ROLE_REQUIRED");
        if (request.Claims[0]?.Kind != "ROLE") throw new GraphRoleRuleException("REPORT_ROLE_FIRST", 1);
        var evidence = context.Evidence.ToDictionary(e => e.Id);
        for (var index = 0; index < request.Claims.Count; index++)
            GraphRoleClaimValidation.Validate(request.Claims[index], index + 1, evidence, context.Node.SourcePath);
    }
}
