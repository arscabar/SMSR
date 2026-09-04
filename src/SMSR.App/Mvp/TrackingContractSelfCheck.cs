using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class TrackingContractSelfCheck
{
    public static async Task RunAsync()
    {
        if (!SmsrMcpInstructions.Text.Contains("record_daily_activity", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("계산, 짧은 검색", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("workflowId를 생략", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("즉시 record_event", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("list_workflows", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("범위를 닫", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("일일 활동만 기록", StringComparison.Ordinal))
            Fail("요청형 그래프 지침");

        var path = Path.Combine(Path.GetTempPath(), $"smsr-tracking-{Guid.NewGuid():N}.db");
        try
        {
            var store = new EventStore(path);
            await store.InitializeAsync();
            var dailyNotifier = new DailyActivityNotifier();
            var dailyChanges = 0;
            dailyNotifier.Changed += (_, _) => dailyChanges++;
            var dailyTools = new DailyActivityTools(store, dailyNotifier);
            var dailyResult = await dailyTools.RecordDailyActivity("daily-1", "daily-project", "task-1",
                "설정 문구 수정", "한 파일의 안내 문구를 수정했습니다.", files: ["README.md"],
                verifications: ["문서 확인"]);
            await dailyTools.RecordDailyActivity("daily-1", "daily-project", "task-1",
                "설정 문구 수정", "검증에서 문제를 발견했습니다.", status: "FAILED",
                files: ["README.md"], verifications: ["문서 검사 실패"]);
            var now = DateTimeOffset.UtcNow;
            var daily = await store.GetDailyActivitiesAsync(now.AddMinutes(-1), now.AddMinutes(1));
            if (dailyResult.Contains("error", StringComparison.OrdinalIgnoreCase) || dailyChanges != 2
                || daily.Single().Files.Single() != "README.md" || daily.Single().Status != "FAILED"
                || !(await store.GetProjectIdsAsync()).Contains("daily-project")
                || DailyActivityValidation.Validate(new("", "project", "task", "title", "summary")) is null)
                Fail("일일 작업 기록 계약");
            await store.DeleteProjectAsync("daily-project");
            if ((await store.GetProjectIdsAsync()).Contains("daily-project")) Fail("일일 작업 삭제 계약");
            var generatedId = WorkflowIdGenerator.Create("Apple Game", "점심 추천 웹서버",
                new DateTimeOffset(2026, 8, 31, 15, 42, 7, TimeSpan.FromHours(9)).AddMilliseconds(123));
            if (generatedId != "20260831-154207123__Apple-Game__점심-추천-웹서버") Fail("workflow ID 자동 생성");
            var opaqueResult = await new PlanTools(store, new()).SavePlan("Apple Game",
                [new("root", "점심 추천 웹서버")], "01a05b48-d586-7052-bb7c-eb258bf3f06d");
            using var opaqueDocument = JsonDocument.Parse(opaqueResult);
            var opaqueRoot = opaqueDocument.RootElement;
            var opaqueWorkflowId = opaqueRoot.GetProperty("workflowId").GetString();
            if (!opaqueRoot.GetProperty("replacedOpaqueId").GetBoolean()
                || opaqueWorkflowId == "01a05b48-d586-7052-bb7c-eb258bf3f06d"
                || opaqueWorkflowId is null
                || !opaqueWorkflowId.EndsWith("Apple-Game__점심-추천-웹서버", StringComparison.Ordinal))
                Fail("불투명 session ID 교체");
            await store.SavePlanAsync("SMSR", "task-1",
            [
                new("implementation", "구현", 1, null, null, "lead", "coordinator", "모든 하위 작업 완료"),
                new("contract", "계약 확장", 2, null, "implementation", "worker-1", "implementer", "계약 검사 통과"),
                new("obsolete", "제외 예정", 1, null, "implementation")
            ]);
            var request = new RecordEventRequest(
                "event-1", "SMSR", "task-1", "contract", "worker-1", "NODE_STATUS_CHANGED", "IN_PROGRESS",
                "계약 구현", null, null, ["src/SMSR.App/Mvp/Contracts.cs"], "implementer", 60, 2, "테스트 실행");
            if (!await store.RecordAsync(request)) Fail("이벤트 기록");
            if (!await store.RecordAsync(request with { EventId = "obsolete-event", NodeId = "obsolete" }))
                Fail("제거 예정 노드 기록");
            await store.RecordHeartbeatAsync(new("SMSR", "task-1", "reviewer-1", "reviewer", "ACTIVE", "contract", "계약 검토", 0));

            var notifier = new WorkflowEventNotifier();
            var version = notifier.Version("SMSR", "task-1");
            var planTools = new PlanTools(store, notifier);
            var updatedResult = await planTools.SavePlan("SMSR",
            [
                new("implementation", "구현", 1, null, null, "lead", "coordinator", "모든 하위 작업 완료"),
                new("review", "검토", 1, ["contract"], "implementation", "reviewer-1", "reviewer", "검토 통과"),
                new("contract", "계약 확장", 2, null, "implementation", "worker-1", "implementer", "계약 검사 통과")
            ], "task-1");
            var updatedPlan = await store.GetPlanAsync("SMSR", "task-1");
            var updatedState = await store.GetStateAsync("SMSR", "task-1");
            if (updatedResult.Contains("error", StringComparison.OrdinalIgnoreCase)
                || notifier.Version("SMSR", "task-1") <= version
                || !updatedPlan.Nodes.Select(node => node.NodeId).SequenceEqual(["implementation", "review", "contract"])
                || updatedState.Nodes.Single(node => node.NodeId == "contract").ProgressPercentage != 60
                || updatedState.Nodes.Any(node => node.NodeId == "obsolete")
                || updatedPlan.Nodes.Single(node => node.NodeId == "review").Status != "PENDING")
                Fail("작업 중 계획 순서·노드 추가");

            var plan = await store.GetPlanAsync("SMSR", "task-1");
            var state = await store.GetStateAsync("SMSR", "task-1");
            var recent = await store.GetRecentEventsAsync("SMSR", "task-1");
            var workflows = await store.GetWorkflowCatalogAsync("SMSR");
            var node = state.Nodes.Single();
            if (plan.Nodes.Single(item => item.NodeId == "contract").ParentNodeId != "implementation"
                || node.AgentRole != "implementer" || node.ProgressPercentage != 60 || node.RetryCount != 2
                || node.Artifacts?.Single() != "src/SMSR.App/Mvp/Contracts.cs" || state.Agents?.Count != 2
                || recent.Single(item => item.NodeId == "contract").RetryCount != 2 || workflows.Single().WorkflowId != "task-1"
                || workflows.Single().Title != "구현" || workflows.Single().NodeCount != 3
                || workflows.Single().Status != "ACTIVE")
                Fail("확장 계약 조회");

            foreach (var nodeId in new[] { "contract", "review", "implementation" })
                await store.RecordAsync(request with { EventId = $"done-{nodeId}", NodeId = nodeId, Status = "SUCCESS", ProgressPercentage = 100 });
            var completedPlan = await store.GetPlanAsync("SMSR", "task-1");
            var completedDefinitions = completedPlan.Nodes.Select(node => new PlanNodeDefinition(node.NodeId,
                node.Title, node.Weight, node.DependsOn, node.ParentNodeId, node.AssignedAgentId,
                node.AgentRole, node.CompletionCriteria)).ToArray();
            var childUpdate = await planTools.SavePlan("SMSR",
                [.. completedDefinitions, new("late-child", "완료 노드 하위 추가", ParentNodeId: "contract")], "task-1");
            var disconnected = await planTools.SavePlan("SMSR",
                [.. completedDefinitions, new("unrelated", "별도 작업")], "task-1");
            var lateUpdate = await planTools.SavePlan("SMSR",
                [.. completedDefinitions, new("late", "관련 후속 작업", DependsOn: ["implementation"])], "task-1");
            var followUpPlan = await store.GetPlanAsync("SMSR", "task-1");
            var followUpCatalog = (await store.GetWorkflowCatalogAsync("SMSR")).Single(item => item.WorkflowId == "task-1");
            var lateRequest = request with { EventId = "late-start", NodeId = "late", Status = "IN_PROGRESS", ProgressPercentage = 5 };
            if (!ReadError(childUpdate).Contains("완료된 노드 아래", StringComparison.Ordinal)
                || !ReadError(disconnected).Contains("별도 작업", StringComparison.Ordinal)
                || lateUpdate.Contains("error", StringComparison.OrdinalIgnoreCase)
                || followUpPlan.Nodes.Count != completedPlan.Nodes.Count + 1
                || followUpPlan.Nodes.Single(node => node.NodeId == "late").Status != "PENDING"
                || followUpCatalog.Status != "ACTIVE"
                || WorkflowDependencyGate.Validate(lateRequest, followUpPlan) is not null
                || WorkflowDependencyGate.Validate(request with { EventId = "reopen", Status = "IN_PROGRESS" }, followUpPlan) is null)
                Fail("완료 이력 보호·관련 후속 노드 추가");
            if (!await store.RecordAsync(lateRequest)) Fail("후속 노드 시작 기록");
            var followUpState = await store.GetStateAsync("SMSR", "task-1");
            var followUpPage = DashboardPage.Render(followUpState, followUpPlan,
                await store.GetRecentEventsAsync("SMSR", "task-1"));
            if (followUpState.Nodes.Single(node => node.NodeId == "late").Status != "IN_PROGRESS"
                || followUpPage.Contains("전체 진행률 100%", StringComparison.Ordinal)
                || !followUpPage.Contains("완료 3 / 4", StringComparison.Ordinal))
                Fail("완료 그래프 후속 진행 표시");

            var root = DashboardPage.Render(state, plan, recent);
            var child = DashboardPage.Render(state, plan, recent, null, "implementation", "contract");
            if (!root.Contains("하위 작업 2개") || !root.Contains("parentNodeId=implementation") || !root.Contains("implementation · lead · IN_PROGRESS")
                || !root.Contains("toggle-status-cards") || !root.Contains("status-card")
                || !root.Contains("smsr-status-cards")
                || !root.Contains("<span class=\"status-card-title\">계약 확장</span>", StringComparison.Ordinal)
                || !child.Contains("계약 검사 통과") || !child.Contains("src/SMSR.App/Mvp/Contracts.cs"))
                Fail("계층 드릴다운 렌더링");
        }
        finally
        {
            foreach (var file in new[] { path, $"{path}-shm", $"{path}-wal" })
                if (File.Exists(file)) File.Delete(file);
        }
    }

    private static string ReadError(string json)
        => JsonDocument.Parse(json).RootElement.GetProperty("error").GetString() ?? string.Empty;

    private static void Fail(string step) => throw new InvalidOperationException($"{step} 검증이 실패했습니다.");
}
