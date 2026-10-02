namespace SMSR.App.Mvp;

internal static class GraphSemanticFixture
{
    internal static GraphSemanticRequest Request(GraphDocument doc)
    {
        string Location(string quote) => doc.Blocks.Single(b => b.Text.Contains(quote)).Location;
        return new("documents", "plan.md", doc.SourceHash, doc.Revision, "fixture-host", "semantic-v1",
            [new("events", "concept", "이벤트 이력", Location("이벤트 이력을"), "이벤트 이력을 저장한다."),
             new("checks", "requirement", "검증 근거", Location("검증 근거가"), "검증 근거가 필요하다."),
             new("preserve", "rationale", "실패 보존", Location("오류 시"), "오류 시 이전 결과를 유지한다.")],
            [new("events", "file:src/service.py", "REFERENCES", Location("src/service.py"), "src/service.py는 이벤트 저장을 담당한다.")],
            [new("history", "검증 가능한 이력", Location("오류 시"), "오류 시 이전 결과를 유지한다.", [new("events"),new("checks"),new("preserve")])]);
    }
}
