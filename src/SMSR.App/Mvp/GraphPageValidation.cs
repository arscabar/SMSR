using System.Text;

namespace SMSR.App.Mvp;

internal static partial class GraphPageSections
{
    public static string Validation(GraphValidationGaps? gaps)
    {
        if (gaps is null) return "";
        var html = new StringBuilder("<section class=\"panel\"><h2>검증 기록 점검</h2>");
        html.Append($"<p class=\"muted\">색인 파일 근거 {gaps.FileEvidenceCount}건 · 미해소 산출물 {gaps.UnresolvedEvidenceCount}건 · 구조화된 검증 {gaps.Verifications.Count}건</p>");
        html.Append(gaps.VerificationUnrecorded
            ? "<p class=\"error\">산출물은 있지만 구조화된 검증 기록이 없습니다. 실제 테스트 미실행을 뜻하지는 않습니다.</p>"
            : "<p class=\"muted\">검증 기록 여부만 확인합니다. 파일별 검증 적합성은 판정하지 않습니다.</p>");
        if (gaps.EvidenceTruncated) html.Append("<p class=\"muted\">산출물 최근 200건 기준입니다.</p>");
        foreach (var verification in gaps.Verifications)
            html.Append($"<p>· {GraphPage.Encode(verification)}</p>");
        return html.Append("</section>").ToString();
    }
}
