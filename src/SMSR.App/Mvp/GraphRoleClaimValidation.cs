namespace SMSR.App.Mvp;

internal static class GraphRoleClaimValidation
{
    internal static void Validate(GraphRoleClaim claim, int index,
        IReadOnlyDictionary<string, GraphRoleEvidence> evidence, string selectedPath)
    {
        void Require(bool valid, string rule) { if (!valid) throw new GraphRoleRuleException(rule, index); }
        Require(claim is not null, "CLAIM_NULL");
        Require(claim!.Kind is "ROLE" or "BEHAVIOR" or "RELATION" or "RATIONALE" or "LIMITATION", "CLAIM_KIND");
        Require(claim.Confidence is "EXTRACTED" or "INFERRED", "CLAIM_CONFIDENCE");
        Require(!string.IsNullOrWhiteSpace(claim.Text) && claim.Text.Length <= 700, "CLAIM_TEXT_LENGTH");
        Require(!string.IsNullOrWhiteSpace(claim.SupportingQuote) && claim.SupportingQuote.Length <= 800, "CLAIM_QUOTE_LENGTH");
        Require(!GraphDocumentRedaction.Excluded(claim.Text) && !GraphDocumentRedaction.Excluded(claim.SupportingQuote), "CLAIM_SENSITIVE");
        Require(claim.EvidenceIds is { Count: >= 1 and <= 4 }, "CLAIM_EVIDENCE_COUNT");
        Require(claim.EvidenceIds!.All(id => id is not null && evidence.ContainsKey(id)), "CLAIM_EVIDENCE_IDS");
        var sources = claim.EvidenceIds!.Select(id => evidence[id]).ToArray();
        Require(sources.Any(e => e.Quote.Contains(claim.SupportingQuote, StringComparison.Ordinal)), "QUOTE_NOT_FOUND");
        if (claim.Kind == "ROLE")
            Require(sources.Any(e => e.Edge is null && e.Path == selectedPath
                && e.Quote.Contains(claim.SupportingQuote, StringComparison.Ordinal)), "ROLE_SOURCE");
        if (claim.Kind == "RELATION")
            Require(sources.Any(e => e.Edge is not null && e.Quote.Contains(claim.SupportingQuote, StringComparison.Ordinal))
                && (claim.Confidence != "EXTRACTED" || !sources.Any(e => e.Edge is { } edge
                    && (edge.Confidence != "EXTRACTED" || edge.Resolution != "RESOLVED"))), "RELATION_EVIDENCE");
        if (claim.Kind == "RATIONALE") Require(claim.Confidence == "EXTRACTED", "RATIONALE_CONFIDENCE");
        if (claim.Kind == "LIMITATION") Require(claim.Confidence == "INFERRED", "LIMITATION_CONFIDENCE");
    }
}
