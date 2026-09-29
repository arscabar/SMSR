namespace SMSR.App.Mvp;

internal static class GraphCrossRepoSelfCheck
{
    public static async Task RunAsync(EventStore store)
    {
        var projects = await store.GetGraphProjectsAsync();
        var expected = projects.ToDictionary(p => p.ProjectId, p => p.Revision);
        var project = projects.Single(p => p.ProjectId == "sample");
        var before = await store.GetCrossRepoEdgesAsync(project.ProjectId);
        if (!await store.IsCrossRepoCurrentAsync(project.ProjectId, project.Revision) || before.Count == 0)
            throw new Exception("저장소 간 연결 버전 근거 누락");
        expected[project.ProjectId]--;
        try { await store.ReplaceCrossRepoEdgesAsync([], expected); throw new Exception("오래된 연결 쓰기 허용"); }
        catch (InvalidOperationException) { }
        var after = await store.GetCrossRepoEdgesAsync(project.ProjectId);
        if (!before.SequenceEqual(after)) throw new Exception("거부된 연결 갱신이 이전 관계 삭제");
        var oldEdges = await store.GetGraphEdgePairsAsync(project.ProjectId, 1000, revision: 1);
        if (oldEdges.Count == 0) throw new Exception("과거 관계 조회 공백");
        var ids = await store.GetGraphNodeIdsAsync(project.ProjectId, 1000, revision: 1);
        if (oldEdges.Any(e => !ids.Contains(e.Source) || !ids.Contains(e.Target)))
            throw new Exception("커뮤니티 리비전 격리 실패");
        if (!GraphPageCrossRepo.Render("sample", null, "", new(project.Revision, before, false, true)).Contains("오래되었거나"))
            throw new Exception("저장소 간 오래됨 표시 실패");
    }
}
