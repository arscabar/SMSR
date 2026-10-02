using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal static class DashboardEvidenceSelfCheck
{
    public static void Run()
    {
        var now = DateTimeOffset.UtcNow;
        var plan = new WorkflowPlan("ui", "evidence", [new("node", "확인 작업", 1, [], "SUCCESS", null, null, now)]);
        WorkflowEvent[] events = [new("evt", "node", "agent", "NODE_STATUS_CHANGED", "SUCCESS", "확인", null, now)];
        WorkflowEvidenceLink[] evidence = [new("node", "evt", "docs/result.md", now),
            new("node", "older", "<img src=x onerror=alert(1)>", now)];
        var markup = DashboardEvidence.Render(evidence, events, plan);
        Check(markup.StartsWith("<details class=\"evidence-disclosure\" data-disclosure=\"evidence\">"), "기본 닫힘");
        Check(markup.Contains("2건") && markup.Contains("#event-evt")
            && markup.Contains("실행 기록 보기") && markup.Contains("확인 작업"), "근거 링크·개수");
        Check(markup.Contains("&lt;img") && !markup.Contains("<img")
            && !markup.Contains("#event-older"), "인코딩·이전 기록");
        Check(DashboardEvidence.Render([], [], plan).Contains("아직 등록된"), "빈 상태");
        var many = Enumerable.Range(0, 101).Select(i => new WorkflowEvidenceLink("node", "evt", $"file-{i}", now)).ToArray();
        var limited = DashboardEvidence.Render(many, events, plan);
        Check(Regex.Matches(limited, "class=\"evidence-card\"").Count == 100
            && limited.Contains("101건") && limited.Contains("최근 100/101건"), "표시 제한");
        Check(!DashboardTimeline.Render([], events, evidence, 1, plan).Contains("evidence-links"), "타임라인 분리");
        var page = DashboardPage.Render(new("ui", "evidence", []), plan, [], timelineEvents: events, evidence: evidence);
        var activity = page.IndexOf("data-disclosure=\"activity\"", StringComparison.Ordinal);
        var artifacts = page.IndexOf("data-disclosure=\"evidence\"", StringComparison.Ordinal);
        Check(activity >= 0 && artifacts > activity && page.Contains("smsr-dashboard-disclosures.js"), "하단 배치");
        Check(page.Contains("data-disclosure=\"context\" data-default-open=\"true\" open")
            && page.Contains("data-disclosure=\"detail\" data-default-open=\"true\" open")
            && page.Contains("panel-disclosure-body"), "이력·상세 기본 펼침과 카드 통일");
        Check(DashboardStyles.For("Light").Contains("color-scheme:light")
            && DashboardStyles.For(null).Contains(".evidence-reference:focus-visible"), "테마·키보드");
    }

    private static void Check(bool value, string step)
    {
        if (!value) throw new InvalidOperationException("근거 UI 검사 실패: " + step);
    }
}
