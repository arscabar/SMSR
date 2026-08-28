using System.IO;

namespace SMSR.App.Mvp;

internal static class TrackingContractSelfCheck
{
    public static async Task RunAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"smsr-tracking-{Guid.NewGuid():N}.db");
        try
        {
            var store = new EventStore(path);
            await store.InitializeAsync();
            await store.SavePlanAsync("SMSR", "task-1",
            [
                new("implementation", "구현", 1, null, null, "lead", "coordinator", "모든 하위 작업 완료"),
                new("contract", "계약 확장", 2, null, "implementation", "worker-1", "implementer", "계약 검사 통과")
            ]);
            var request = new RecordEventRequest(
                "event-1", "SMSR", "task-1", "contract", "worker-1", "NODE_STATUS_CHANGED", "IN_PROGRESS",
                "계약 구현", null, null, ["src/SMSR.App/Mvp/Contracts.cs"], "implementer", 60, 2, "테스트 실행");
            if (!await store.RecordAsync(request)) Fail("이벤트 기록");
            await store.RecordHeartbeatAsync(new("SMSR", "task-1", "reviewer-1", "reviewer", "ACTIVE", "contract", "계약 검토", 0));

            var plan = await store.GetPlanAsync("SMSR", "task-1");
            var state = await store.GetStateAsync("SMSR", "task-1");
            var recent = await store.GetRecentEventsAsync("SMSR", "task-1");
            var node = state.Nodes.Single();
            if (plan.Nodes.Single(item => item.NodeId == "contract").ParentNodeId != "implementation"
                || node.AgentRole != "implementer" || node.ProgressPercentage != 60 || node.RetryCount != 2
                || node.Artifacts?.Single() != "src/SMSR.App/Mvp/Contracts.cs" || state.Agents?.Count != 2
                || recent.Single().RetryCount != 2)
                Fail("확장 계약 조회");

            var root = DashboardPage.Render(state, plan, recent);
            var child = DashboardPage.Render(state, plan, recent, null, "implementation", "contract");
            if (!root.Contains("하위 작업 1개") || !root.Contains("parentNodeId=implementation") || !root.Contains("implementation · lead · IN_PROGRESS")
                || !child.Contains("계약 검사 통과") || !child.Contains("src/SMSR.App/Mvp/Contracts.cs"))
                Fail("계층 드릴다운 렌더링");
        }
        finally
        {
            foreach (var file in new[] { path, $"{path}-shm", $"{path}-wal" })
                if (File.Exists(file)) File.Delete(file);
        }
    }

    private static void Fail(string step) => throw new InvalidOperationException($"{step} 검증이 실패했습니다.");
}
