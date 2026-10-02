namespace SMSR.App.Mvp;

internal sealed class GraphRoleRuleException(string rule, int? claimIndex = null)
    : ArgumentException("설명 검증 실패 · " + rule + (claimIndex is null ? "" : " · 문장 " + claimIndex))
{
    internal string Rule { get; } = rule;
    internal int? ClaimIndex { get; } = claimIndex;
}
