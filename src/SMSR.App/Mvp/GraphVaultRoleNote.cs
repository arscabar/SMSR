namespace SMSR.App.Mvp;

internal static class GraphVaultRoleNote
{
    internal static string Render(string project, string address, GraphRoleReport report)
    {
        var body = ""; var proof = "";
        foreach (var group in report.Claims.GroupBy(c => c.Kind))
        {
            body += "### " + (group.Key switch { "ROLE" => "핵심 역할", "BEHAVIOR" => "주요 동작",
                "RELATION" => "관련 구성요소", "RATIONALE" => "기록된 설계 이유", _ => "분석 범위와 한계" }) + "\n\n";
            foreach (var claim in group)
            {
                var text = claim.Kind == "ROLE" && claim.Text.StartsWith("ROLE:") ? claim.Text[5..].TrimStart() : claim.Text;
                body += "- " + GraphVaultNote.Text(text) + (claim.Confidence == "INFERRED" ? " · 해석·추정" : "") + "\n";
                foreach (var e in report.Evidence.Where(e => claim.EvidenceIds.Contains(e.Id)))
                    if (GraphRoleCitation.Line(e, claim.SupportingQuote) is { } line)
                    {
                        var link = address + "/graph/source?projectId=" + Uri.EscapeDataString(project)
                            + "&path=" + Uri.EscapeDataString(e.Path) + "&line=" + line;
                        proof += $"- 근거: `{e.Path.Replace("`", "")}`:{line} · [원문]({link})\n  - {GraphVaultNote.Text(claim.SupportingQuote).Replace("\n", " ")}\n";
                    }
            }
            body += "\n";
        }
        return body + "<details><summary>설명 근거 펼치기</summary>\n\n" + proof + "\n</details>\n\n"
            + $"분석 모델: {GraphVaultNote.Text(report.Model)} · 분석 리비전: {report.Revision}\n\n";
    }
}
