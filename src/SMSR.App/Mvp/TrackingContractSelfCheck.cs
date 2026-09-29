using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class TrackingContractSelfCheck
{
    public static async Task RunAsync()
    {
        if (!SmsrMcpInstructions.Text.Contains("record_daily_activity", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("계산, 짧은 검색", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("workflowId를 생략", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("즉시 record_event", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("operatorInstruction", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("사용자 요청 요약:", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("프롬프트 원문·비밀·개인정보", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("끝난 작업의 진행 노드를 남겨두지 마세요", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("CANCELLED(중단)", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("list_workflows", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("범위를 닫", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("두 개 이상의 실행 결과", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("포괄적인 A를 리프로 남기지", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("기간만 길다는 이유", StringComparison.Ordinal)
            || !SmsrMcpInstructions.Text.Contains("일일 활동만 기록", StringComparison.Ordinal))
            Fail("요청형 그래프 지침");

        var path = Path.Combine(Path.GetTempPath(), $"smsr-tracking-{Guid.NewGuid():N}.db");
        var legacyPath = path + ".legacy";
        try
        {
            var store = new EventStore(path);
            await store.InitializeAsync();
            await using (var legacy = new SqliteConnection($"Data Source={legacyPath};Pooling=False"))
            {
                await legacy.OpenAsync();
                var command = legacy.CreateCommand();
                command.CommandText = "CREATE TABLE events(event_id TEXT PRIMARY KEY, project_id TEXT NOT NULL, workflow_id TEXT NOT NULL, node_id TEXT NOT NULL, agent_id TEXT NOT NULL, event_type TEXT NOT NULL, status TEXT NOT NULL, summary TEXT, error TEXT, payload_json TEXT NOT NULL, created_at_utc TEXT NOT NULL);";
                await command.ExecuteNonQueryAsync();
                command.CommandText = "INSERT INTO events VALUES ('legacy-event','SMSR','legacy','node','agent','NODE_STATUS_CHANGED','SUCCESS','완료',NULL,$payload,$createdAt);";
                command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(new RecordEventRequest(
                    "legacy-event", "SMSR", "legacy", "node", "agent", "NODE_STATUS_CHANGED", "SUCCESS", "완료", null, null, ["legacy-proof.txt"])));
                command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));
                await command.ExecuteNonQueryAsync();
            }
            var migrated = new EventStore(legacyPath);
            await migrated.InitializeAsync();
            await migrated.InitializeAsync();
            if (Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(legacyPath) + ".bak-p0-*").Length != 1
                || Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(legacyPath) + ".bak-p1-*").Length != 1
                || (await migrated.GetEvidenceAsync("SMSR", "legacy")).Single().Reference != "legacy-proof.txt"
                || (await migrated.GetPlanRevisionsAsync("SMSR", "legacy")).Count != 0
                || !DashboardTimeline.Render([], await migrated.GetTimelineEventsAsync("SMSR", "legacy"),
                    await migrated.GetEvidenceAsync("SMSR", "legacy"), 1,
                    await migrated.GetPlanAsync("SMSR", "legacy")).Contains("버전 이력 없음"))
                Fail("기존 DB 백업·증거 이관·계획 공백 보존");
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
                || DailyActivityValidation.Validate(new("", "project", "task", "title", "summary")) is null
                || DailyActivityValidation.Validate(new("cancelled", "project", "task", "title", "summary", "CANCELLED")) is not null)
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
            if (EventValidation.Validate(request with { Status = "SUCCESS", Summary = null, Error = null }) is null)
                Fail("종료 결과 근거 필수");
            if (!await store.RecordAsync(request)) Fail("이벤트 기록");
            if (!await store.RecordAsync(request with { EventId = "obsolete-event", NodeId = "obsolete" }))
                Fail("제거 예정 노드 기록");
            var evidence = await store.GetEvidenceAsync("SMSR", "task-1");
            var timeline = await store.GetTimelineEventsAsync("SMSR", "task-1");
            if (evidence.Count != 2 || evidence[0].EventId != "event-1"
                || evidence[0].Reference != "src/SMSR.App/Mvp/Contracts.cs"
                || !timeline.Select(item => item.EventId).SequenceEqual(["event-1", "obsolete-event"])
                || await store.GetWorkflowEventCountAsync("SMSR", "task-1") != 2)
                Fail("시간순 이벤트·산출물 증거 저장");
            await store.RecordHeartbeatAsync(new("SMSR", "task-1", "reviewer-1", "reviewer", "ACTIVE", "contract", "계약 검토", 0));

            var notifier = new WorkflowEventNotifier();
            var version = notifier.Version("SMSR", "task-1");
            var planTools = new PlanTools(store, notifier);
            var updatedResult = await planTools.SavePlan("SMSR",
            [
                new("implementation", "구현", 1, null, null, "lead", "coordinator", "모든 하위 작업 완료"),
                new("review", "검토", 1, ["contract"], "implementation", "reviewer-1", "reviewer", "검토 통과"),
                new("contract", "계약 확장", 2, null, "implementation", "worker-1", "implementer", "계약 검사 통과")
            ], "task-1", changeReason: "검토 단계 추가");
            var updatedPlan = await store.GetPlanAsync("SMSR", "task-1");
            var revisions = await store.GetPlanRevisionsAsync("SMSR", "task-1");
            var updatedState = await store.GetStateAsync("SMSR", "task-1");
            if (updatedResult.Contains("error", StringComparison.OrdinalIgnoreCase)
                || notifier.Version("SMSR", "task-1") <= version
                || revisions.Count != 2 || revisions[0].Nodes.Any(node => node.NodeId == "review")
                || revisions[1].ChangeReason != "검토 단계 추가"
                || !revisions[1].Nodes.Select(node => node.NodeId).SequenceEqual(["implementation", "review", "contract"])
                || !updatedPlan.Nodes.Select(node => node.NodeId).SequenceEqual(["implementation", "review", "contract"])
                || updatedState.Nodes.Single(node => node.NodeId == "contract").ProgressPercentage != 60
                || updatedState.Nodes.Any(node => node.NodeId == "obsolete")
                || updatedPlan.Nodes.Single(node => node.NodeId == "review").Status != "PENDING")
                Fail("작업 중 계획 순서·노드 추가");

            var nestedResult = await planTools.SavePlan("SMSR",
            [
                new("broad", "포괄 작업", Children:
                [
                    new("broad-1", "환경 확인"),
                    new("broad-2", "구현", DependsOn: ["broad-1"])
                ])
            ], "nested-task", reason: "추적 이유", approach: "계획과 이벤트로 진행");
            var nestedPlan = await store.GetPlanAsync("SMSR", "nested-task");
            if (nestedResult.Contains("error", StringComparison.OrdinalIgnoreCase)
                || nestedPlan.Nodes.Count != 3
                || nestedPlan.Nodes.Where(node => node.NodeId.StartsWith("broad-", StringComparison.Ordinal))
                    .Any(node => node.ParentNodeId != "broad"))
                Fail("하위 계획 자동 전개");
            if (!DashboardPage.Render(new("SMSR", "nested-task", []), nestedPlan, [])
                .Contains("종료 후 결과가 기록됩니다", StringComparison.Ordinal))
                Fail("진행 중 결과 안내");

            await store.SaveWorkflowContextAsync(new("SMSR", "nested-task", null, null, "검증 완료", DateTimeOffset.UtcNow));
            var context = await store.GetWorkflowContextAsync("SMSR", "nested-task");
            var contextPage = DashboardPage.Render(new("SMSR", "nested-task", []), nestedPlan, [], context: context);
            if (context?.Reason != "추적 이유" || context.Approach != "계획과 이벤트로 진행"
                || context.Result != "검증 완료" || !contextPage.Contains("작업 배경") || !contextPage.Contains("최종 결과"))
                Fail("워크플로우 설명 저장·표시");

            var plan = await store.GetPlanAsync("SMSR", "task-1");
            var state = await store.GetStateAsync("SMSR", "task-1");
            var recent = await store.GetRecentEventsAsync("SMSR", "task-1");
            var workflows = await store.GetWorkflowCatalogAsync("SMSR");
            var node = state.Nodes.Single();
            if (plan.Nodes.Single(item => item.NodeId == "contract").ParentNodeId != "implementation"
                || node.AgentRole != "implementer" || node.ProgressPercentage != 60 || node.RetryCount != 2
                || node.Artifacts?.Single() != "src/SMSR.App/Mvp/Contracts.cs" || state.Agents?.Count != 2
                || recent.Single(item => item.NodeId == "contract").RetryCount != 2 || workflows.Single(item => item.WorkflowId == "task-1").Title != "구현"
                || workflows.Single(item => item.WorkflowId == "task-1").NodeCount != 3
                || workflows.Single(item => item.WorkflowId == "task-1").Status != "ACTIVE")
                Fail("확장 계약 조회");

            foreach (var nodeId in new[] { "contract", "review", "implementation" })
                await store.RecordAsync(request with { EventId = $"done-{nodeId}", NodeId = nodeId, Status = "SUCCESS", ProgressPercentage = 100 });
            var automaticResult = await store.GetWorkflowContextAsync("SMSR", "task-1");
            if (automaticResult?.Result?.StartsWith("자동 요약(완료 기록): 계약 구현", StringComparison.Ordinal) != true)
                Fail("완료 그래프 결과 자동 기록");
            await store.SaveWorkflowContextAsync(new("SMSR", "task-1", null, null, "사용자가 작성한 결과", DateTimeOffset.UtcNow));
            await store.RecordAsync(request with { EventId = "after-manual-result", NodeId = "implementation",
                Status = "SUCCESS", Summary = "자동 기록이 덮어쓰면 안 됨", ProgressPercentage = 100 });
            if ((await store.GetWorkflowContextAsync("SMSR", "task-1"))?.Result != "사용자가 작성한 결과")
                Fail("명시적 결과 보호");
            await using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                await connection.OpenAsync();
                var clear = connection.CreateCommand();
                clear.CommandText = "UPDATE workflow_context SET result=NULL WHERE project_id='SMSR' AND workflow_id='task-1';";
                await clear.ExecuteNonQueryAsync();
                await EventStoreWorkflowResults.FillMissingAsync(connection, null, null, null, CancellationToken.None);
            }
            if ((await store.GetWorkflowContextAsync("SMSR", "task-1"))?.Result?.StartsWith("자동 요약(완료 기록):", StringComparison.Ordinal) != true)
                Fail("기존 완료 그래프 결과 보완");
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
                || followUpPage.Contains("완료 3 / 4", StringComparison.Ordinal))
                Fail("완료 그래프 후속 진행 표시");

            var root = DashboardPage.Render(state, plan, recent);
            var child = DashboardPage.Render(state, plan, recent, null, "implementation", "contract");
            var timelinePage = DashboardPage.Render(state, plan, recent, revisions: revisions,
                timelineEvents: timeline, evidence: evidence, timelineEventCount: 2);
            if (!root.Contains("하위 작업 2개") || !root.Contains("parentNodeId=implementation") || !root.Contains("implementation · lead · IN_PROGRESS")
                || !root.Contains("toggle-status-cards") || !root.Contains("status-card")
                || !root.Contains("smsr-status-cards")
                || !root.Contains("<span class=\"status-card-title\">계약 확장</span>", StringComparison.Ordinal)
                || !child.Contains("계약 검사 통과") || !child.Contains("src/SMSR.App/Mvp/Contracts.cs")
                || !child.Contains("작업 중인 Codex에 요청") || child.Contains("codex://threads/new?prompt="))
                Fail("계층 드릴다운 렌더링");
            if (!timelinePage.Contains("계획·실행 순서") || !timelinePage.Contains("id=\"timeline-step\"")
                || !timelinePage.Contains("기록 선택") || !timelinePage.Contains("<li hidden")
                || !timelinePage.Contains("class=\"context-field\"")
                || !timelinePage.Contains("계획 v2") || !timelinePage.Contains("#event-event-1")
                || !timelinePage.Contains("검토 단계 추가"))
                Fail("계획·실행 타임라인 화면");
        }
        finally
        {
            foreach (var file in new[] { path, $"{path}-shm", $"{path}-wal" })
                if (File.Exists(file)) File.Delete(file);
            foreach (var file in new[] { legacyPath, $"{legacyPath}-shm", $"{legacyPath}-wal" }
                .Concat(Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(legacyPath) + ".bak-p0-*"))
                .Concat(Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(legacyPath) + ".bak-p1-*")))
                if (File.Exists(file)) File.Delete(file);
        }
    }

    private static string ReadError(string json)
        => JsonDocument.Parse(json).RootElement.GetProperty("error").GetString() ?? string.Empty;

    private static void Fail(string step) => throw new InvalidOperationException($"{step} 검증이 실패했습니다.");
}
