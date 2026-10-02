using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphIndexSelfCheck
{
    public static async Task RunAsync()
    {
        foreach (var view in new[] { "find", "relations", "flow" })
        {
            var page = GraphExplorerPage.Render("sample", null, view, null);
            if (page.Contains("id=\"edge-list\"", StringComparison.Ordinal)
                || page.Contains("id=\"graph-note\"", StringComparison.Ordinal)
                || page.Contains("코드 흐름 보기", StringComparison.Ordinal)
                || page.Contains("로컬 프로젝트 탐색", StringComparison.Ordinal)
                || page.Contains("data-kind=\"heading\"", StringComparison.Ordinal)
                || !page.Contains("data-kind=\"audio\"", StringComparison.Ordinal)
                || !page.Contains("data-kind=\"video\"", StringComparison.Ordinal)
                || !page.Contains("id=\"relation-next\"", StringComparison.Ordinal))
                throw new InvalidOperationException("단일 찾기 화면 정리 실패");
        }
        if (!WebNavigation.Render("sample", null, "find").Contains("sessionStorage", StringComparison.Ordinal)
            || !WebNavigation.Render("sample", "work-1", "find").Contains("workflowId=work-1", StringComparison.Ordinal))
            throw new InvalidOperationException("작업 현황 복귀 경로 보존 실패");
        GraphMarkdownSelfCheck.Run();
        await GraphImageSelfCheck.RunAsync();
        GraphRouteSelfCheck.Run();
        await GraphBoundarySelfCheck.RunAsync();
        var root = Path.Combine(Path.GetTempPath(), "smsr-graph-한글-" + Guid.NewGuid().ToString("N"));
        var otherRoot = root + "-other";
        Directory.CreateDirectory(root);
        try
        {
            await GitAsync(root, "init", "-q");
            var legacyGraphPath = Path.Combine(root, "legacy-graph.db");
            await using (var legacy = new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = legacyGraphPath, Pooling = false }.ToString()))
            {
                await legacy.OpenAsync();
                using var command = legacy.CreateCommand();
                command.CommandText = "CREATE TABLE graph_issues(project_id TEXT,owner_path TEXT,source_line INTEGER,relation TEXT,reason TEXT);";
                await command.ExecuteNonQueryAsync();
            }
            await new EventStore(legacyGraphPath).InitializeAsync();
            await using (var upgraded = new SqliteConnection(new SqliteConnectionStringBuilder
                { DataSource = legacyGraphPath, Pooling = false }.ToString()))
            {
                await upgraded.OpenAsync();
                using var column = upgraded.CreateCommand();
                column.CommandText = "SELECT 1 FROM pragma_table_info('graph_issues') WHERE name='candidates_json';";
                if (await column.ExecuteScalarAsync() is null
                    || Directory.GetFiles(root, "legacy-graph.db.bak-p1-*").Length != 1)
                    throw new InvalidOperationException("기존 관계 DB 백업·후보 컬럼 이전 검증 실패");
            }
            Directory.CreateDirectory(Path.Combine(root, "docs"));
            Directory.CreateDirectory(Path.Combine(root, "src"));
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "ignored.md\n");
            await File.WriteAllTextAsync(Path.Combine(root, "ignored.md"), "# ignored");
            await File.WriteAllTextAsync(Path.Combine(root, ".env"), "TOKEN=do-not-index");
            await File.WriteAllTextAsync(Path.Combine(root, "src", "code.cs"), "class Widget {}\n");
            var doc = Path.Combine(root, "docs", "a.md");
            await File.WriteAllTextAsync(doc, "# Alpha <img src=x onerror=alert(1)>\n[code](../src/code.cs)\n`../src/code.cs::Widget`\n[escape](../../outside.cs)\n[root](/src/code.cs)\n[self](a.md)\n[same](../src/code.cs) [same](../src/code.cs)\n# token smds_notstored\n");
            var store = new EventStore(Path.Combine(root, "test.db"));
            await store.InitializeAsync();
            var index = new GraphIndexService(store);
            var first = await index.IndexAsync("sample", root);
            if ((await index.CheckFreshnessAsync("sample")).IsStale)
                throw new InvalidOperationException("변경 없는 색인의 최신성 검증 실패");
            var query = new GraphQueryService(store);
            var found = await query.PathAsync("sample", "file:docs/a.md", "file:src/code.cs");
            var firstEdge = found.Edges[0];
            var feedbackService = new GraphFeedbackService(store);
            await feedbackService.RecordAsync(new("sample", firstEdge.SourceId, firstEdge.TargetId,
                firstEdge.Relation, firstEdge.OwnerPath, firstEdge.SourceLine, "USEFUL"));
            if ((await feedbackService.GetAsync("sample", firstEdge.SourceId)).Single().Stale)
                throw new InvalidOperationException("새 관계 피드백의 최신성 검증 실패");
            var impact = await query.ImpactAsync("sample", "file:src/code.cs");
            await feedbackService.RecordAsync(new("sample", firstEdge.SourceId, firstEdge.TargetId,
                firstEdge.Relation, firstEdge.OwnerPath, firstEdge.SourceLine, "ERROR"));
            await feedbackService.RecordAsync(new("sample", firstEdge.SourceId, firstEdge.TargetId,
                firstEdge.Relation, firstEdge.OwnerPath, firstEdge.SourceLine, "CORRECTED"));
            if (!(await feedbackService.GetAsync("sample", firstEdge.SourceId)).Any(item => item.Verdict == "CORRECTED" && !item.Stale))
                throw new InvalidOperationException("수정됨 피드백 기록 실패");
            var firstHealth = await store.GetGraphHealthAsync("sample");
            var source = new GraphSourceService(store);
            var firstSource = await source.GetAsync("sample", "docs/a.md", 1);
            var firstSourceHtml = GraphSourcePage.Render("sample", firstSource, null);
            var issuePage = GraphPage.Render("sample", null, "", firstHealth,
                await query.SearchAsync("sample", ""), null, null, null, null, null);
            if (first.FileCount != 2 || first.EdgeCount != 7 || !found.Found || found.Edges.Count != 2
                || !impact.Nodes.Any(node => node.NodeId == "file:docs/a.md")
                || firstHealth?.Issues.Count != 3 || firstHealth.Info.UnresolvedEdges != 2
                || firstHealth.SelfLoops != 1 || firstHealth.DuplicateReferences != 1
                || !issuePage.Contains("미해소 참조", StringComparison.Ordinal)
                || !issuePage.Contains("Alpha &lt;img", StringComparison.Ordinal)
                || issuePage.Contains("<strong>Alpha <img", StringComparison.Ordinal)
                || !firstSourceHtml.Contains("Alpha &lt;img", StringComparison.Ordinal)
                || firstSourceHtml.Contains("Alpha <img", StringComparison.Ordinal)
                || (await query.SearchAsync("sample", "smds_notstored")).Nodes.Count != 0
                || (await store.GetGraphNodeAsync("sample", "file:ignored.md")) is not null
                || (await store.GetGraphNodeAsync("sample", "file:.env")) is not null)
                throw new InvalidOperationException("첫 색인·경로·영향·비밀 제외 검증 실패");
            try
            {
                await source.GetAsync("sample", ".env", 1);
                throw new InvalidOperationException("색인 제외 파일 미리보기 차단 실패");
            }
            catch (KeyNotFoundException) { }
            if (!(await index.IndexAsync("sample", root)).Unchanged)
                throw new InvalidOperationException("변경 없는 증분 색인 검증 실패");
            var alphaId = (await query.SearchAsync("sample", "Alpha")).Nodes.Single(node => node.Kind == "heading").NodeId;
            Directory.CreateDirectory(Path.Combine(root, "other"));
            await File.WriteAllTextAsync(Path.Combine(root, "other", "code.cs"), "class Other {}\n");
            await File.WriteAllTextAsync(doc, "# Prelude\n" + await File.ReadAllTextAsync(doc)
                + "[missing](wrong/code.cs)\n[web](https://example.com)\n");
            var stale = await index.CheckFreshnessAsync("sample");
            try
            {
                await source.GetAsync("sample", "docs/a.md", 1);
                throw new InvalidOperationException("변경된 원문 미리보기 차단 실패");
            }
            catch (InvalidOperationException error) when (error.Message.Contains("색인 이후", StringComparison.Ordinal)) { }
            if (!stale.IsStale || stale.ChangedCount != 1 || stale.AddedCount != 1)
                throw new InvalidOperationException("수정된 문서의 색인 오래됨 검증 실패");
            await index.IndexAsync("sample", root);
            if ((await feedbackService.GetAsync("sample", firstEdge.SourceId)).Any(item => !item.Stale))
                throw new InvalidOperationException("재색인된 근거의 피드백 오래됨 검증 실패");
            var revisionDiff = await query.DiffAsync("sample", 1, 2);
            if (revisionDiff.NodeChangeCount < 2 || revisionDiff.EdgeChangeCount < 1
                || !revisionDiff.Nodes.Any(item => item.Change == "ADDED" && item.After?.NodeId == "file:other/code.cs"))
                throw new InvalidOperationException("리비전별 노드·관계 비교 검증 실패");
            if ((await store.GetGraphNodeAsync("sample", alphaId))?.Label.StartsWith("Alpha", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("제목 앞 삽입 후 안정 키 검증 실패");
            var candidates = await store.GetGraphHealthAsync("sample");
            var candidatePage = GraphPage.Render("sample", null, "", candidates,
                await query.SearchAsync("sample", ""), null, null, null, null, null);
            if (candidates?.Issues.Count != 4 || candidates.Issues.Single(item => item.CandidatePaths.Count > 0).CandidatePaths.Count != 2
                || !candidatePage.Contains("확인할 후보", StringComparison.Ordinal)
                || !candidatePage.Contains("심벌 구현은 확인하지 않았습니다", StringComparison.Ordinal)
                || (await query.PathAsync("sample", "file:docs/a.md", "file:other/code.cs")).Found)
                throw new InvalidOperationException("모호한 파일 후보와 외부 링크 제외 검증 실패");
            await File.WriteAllTextAsync(doc, "# Renamed\n[code](../src/code.cs)\n");
            var changed = await index.IndexAsync("sample", root);
            if (changed.ChangedFiles != 1 || (await query.SearchAsync("sample", "Alpha")).Nodes.Count != 0
                || (await store.GetGraphHealthAsync("sample"))?.Issues.Count != 0
                || !(await query.PathAsync("sample", "file:docs/a.md", "file:src/code.cs")).Found)
                throw new InvalidOperationException("문서 변경·기존 관계 교체 검증 실패");
            File.Delete(Path.Combine(root, "src", "code.cs"));
            if ((await index.CheckFreshnessAsync("sample")).RemovedCount != 1)
                throw new InvalidOperationException("삭제된 파일의 색인 오래됨 검증 실패");
            var removed = await index.IndexAsync("sample", root);
            var health = await store.GetGraphHealthAsync("sample");
            if (removed.RemovedFiles != 1 || health?.DanglingEdges != 0 || health.Info.EdgeCount != 2
                || (await store.GetGraphNodeAsync("sample", "file:src/code.cs")) is not null)
                throw new InvalidOperationException("삭제·고아 관계 정리 검증 실패");
            var many = Path.Combine(root, "docs", "many.md");
            await File.WriteAllTextAsync(many, string.Join("\n", Enumerable.Range(1, 25).Select(i => "# Heading " + i)));
            await index.IndexAsync("sample", root);
            var beforeReduction = await store.GetGraphInfoAsync("sample");
            await File.WriteAllTextAsync(many, "# One\n");
            try
            {
                await index.IndexAsync("sample", root);
                throw new InvalidOperationException("대량 축소 보호가 작동하지 않았습니다.");
            }
            catch (InvalidOperationException error) when (error.Message.Contains("절반 이상", StringComparison.Ordinal)) { }
            if ((await store.GetGraphInfoAsync("sample"))?.Revision != beforeReduction?.Revision)
                throw new InvalidOperationException("거부된 색인이 기존 리비전을 변경했습니다.");
            await index.IndexAsync("sample", root, allowLargeReduction: true);
            var noCycles = await query.CyclesAsync("sample");
            if (noCycles.TotalGroups != 0 || noCycles.Truncated)
                throw new InvalidOperationException("단방향 관계의 순환 오탐 검증 실패");
            await File.WriteAllTextAsync(Path.Combine(root, "docs", "b.md"), "# Beta\n[to-a](a.md)\n");
            await File.AppendAllTextAsync(doc, "[to-b](b.md)\n");
            await index.IndexAsync("sample", root);
            var cycles = await query.CyclesAsync("sample");
            if (cycles.TotalGroups != 1 || !cycles.Groups[0].NodeIds.Contains("file:docs/a.md")
                || !cycles.Groups[0].NodeIds.Contains("file:docs/b.md"))
                throw new InvalidOperationException("다중 파일 순환 관계 검증 실패");
            var scope = await query.ExportScopeAsync("sample", "file:docs/a.md", 2);
            if (scope.Nodes.Count < 2 || scope.Edges.Count == 0 || scope.Truncated
                || scope.Edges.Count != scope.Edges.Distinct().Count()
                || scope.Edges.Any(edge => edge.OwnerPath.Length == 0))
                throw new InvalidOperationException("선택 범위 내보내기 검증 실패");
            var sampleCommunities = GraphCommunityDetector.Find(
                ["a", "b", "c", "d", "isolated"],
                [("a", "b"), ("b", "c"), ("c", "a"), ("c", "d")], 1);
            if (sampleCommunities.ScannedNodes != 5 || sampleCommunities.IsolatedNodes != 1
                || sampleCommunities.TotalGroups == 0)
                throw new InvalidOperationException("커뮤니티 축약 계산 검증 실패");
            await File.WriteAllTextAsync(Path.Combine(root, "src", "routes.cs"),
                "app.MapGet(\"/api/health\", Handler);\n[McpServerTool(Name = \"safe_tool\")]\n[McpServerTool(Name = \"smds_notstored\")]\n");
            await index.IndexAsync("sample", root);
            var routeMap = await query.RoutesAsync("sample");
            if (routeMap.Routes.Count != 2 || routeMap.Routes.Any(item => item.Label.Contains("smds_", StringComparison.Ordinal))
                || !routeMap.Routes.Any(item => item.Label == "GET /api/health")
                || !routeMap.Routes.Any(item => item.Label == "MCP safe_tool"))
                throw new InvalidOperationException("비밀 제외 API/MCP 경로 색인 검증 실패");
            await store.RecordAsync(new RecordEventRequest("graph-evidence-1", "sample", "work-1", "step",
                "agent", "NODE_STATUS_CHANGED", "SUCCESS", "done", null, null,
                ["docs/a.md", "missing.md"]));
            var evidence = await new GraphEvidenceService(store).GetAsync("sample", "work-1");
            if (evidence.Matches.Count != 2 || evidence.Matches.Count(item => item.FileNode is not null) != 1)
                throw new InvalidOperationException("작업 근거와 색인 파일 연결 검증 실패");
            var validation = new GraphValidationService(store, new GraphEvidenceService(store));
            if (!(await validation.GetAsync("sample", "work-1")).VerificationUnrecorded)
                throw new InvalidOperationException("산출물의 검증 기록 누락 판정 실패");
            await store.RecordDailyActivityAsync(new("activity-1", "sample", "task-1", "작업", "완료",
                Verifications: ["자체검사 통과"], WorkflowId: "work-1"));
            if ((await validation.GetAsync("sample", "work-1")).VerificationUnrecorded)
                throw new InvalidOperationException("구조화된 검증 기록 인식 실패");
            health = await store.GetGraphHealthAsync("sample");
            var html = GraphPage.Render("sample", null, "", health,
                await query.SearchAsync("sample", ""), null, null, null, null, null);
            var linkedHtml = GraphPage.Render("sample", "work-1", "", health,
                await query.SearchAsync("sample", ""), null, null, null, evidence, null);
            if (!html.Contains("코드·문서 관계") || !html.Contains("전체·폴더별 색인 선택")
                || !html.Contains("현재 소스 확인")
                || !linkedHtml.Contains("파일 확인") || !linkedHtml.Contains("미해소"))
                throw new InvalidOperationException("관계 화면 검증 실패");
            var serverData = Path.Combine(root, "server");
            await using (var host = await LocalServer.StartAsync(serverData, 0))
            {
                using var client = new HttpClient();
                if (!(await client.GetStringAsync(host.Address + "/assets/graph-explorer-evidence.js")).Contains("showEvidence"))
                    throw new InvalidOperationException("관계 근거 모듈 제공 검증 실패");
                using var denied = await client.PostAsJsonAsync(host.Address + "/api/graph/index",
                    new GraphIndexRequest("sample", root));
                var gateway = new McpHttpGateway(host.Address, serverData);
                var indexed = await gateway.CallAsync("index_project_graph", new { projectId = "sample", rootPath = root });
                var indexOptions = await client.GetStringAsync(host.Address + "/api/graph/index-options?projectId=sample");
                await File.WriteAllTextAsync(Path.Combine(root, "docs", "c.md"), "# Gamma\n");
                await gateway.CallAsync("index_project_graph", new { projectId = "sample", rootPath = root });
                var revisionTool = await gateway.CallAsync("compare_graph_revisions", new { projectId = "sample", fromRevision = 1, toRevision = 2 });
                var revisionJson = await client.GetStringAsync(host.Address + "/api/graph/diff?projectId=sample&fromRevision=1&toRevision=2");
                var reviewContext = await client.GetFromJsonAsync<GraphContext>(host.Address + "/api/graph/context?projectId=sample&nodeId=file%3Adocs%2Fc.md");
                var reviewEdge = reviewContext!.Outgoing[0].Edge;
                var reviewRequest = new GraphFeedbackRequest("sample", reviewEdge.SourceId, reviewEdge.TargetId,
                    reviewEdge.Relation, reviewEdge.OwnerPath, reviewEdge.SourceLine, "ERROR");
                using var deniedReview = await client.PostAsJsonAsync(host.Address + "/api/graph/feedback", reviewRequest);
                var reviewTool = await gateway.CallAsync("record_graph_feedback", new { projectId = "sample",
                    sourceId = reviewEdge.SourceId, targetId = reviewEdge.TargetId, relation = reviewEdge.Relation,
                    ownerPath = reviewEdge.OwnerPath, sourceLine = reviewEdge.SourceLine, verdict = "CORRECTED" });
                var reviewList = await gateway.CallAsync("get_graph_feedback", new { projectId = "sample", sourceId = reviewEdge.SourceId });
                if (!reviewList.Contains("CORRECTED", StringComparison.Ordinal))
                    throw new InvalidOperationException("수정됨 피드백 MCP 연결 실패");
                var routeTool = await gateway.CallAsync("get_graph_route_map", new { projectId = "sample" });
                var routeJson = await client.GetStringAsync(host.Address + "/api/graph/routes?projectId=sample");
                var validationTool = await gateway.CallAsync("get_workflow_validation_gaps", new { projectId = "sample", workflowId = "work-2" });
                var validationJson = await client.GetStringAsync(host.Address + "/api/graph/validation?projectId=sample&workflowId=work-2");
                var scopeTool = await gateway.CallAsync("export_graph_scope", new { projectId = "sample", nodeId = "file:docs/a.md", depth = 2, direction = "both" });
                var scopeJson = await client.GetStringAsync(host.Address + "/api/graph/scope?projectId=sample&nodeId=file%3Adocs%2Fa.md");
                var communityTool = await gateway.CallAsync("get_graph_communities", new { projectId = "sample" });
                var communityJson = await client.GetStringAsync(host.Address + "/api/graph/communities?projectId=sample");
                await GraphExplorerHttpSelfCheck.RunAsync(client, host.Address, gateway);
                using var download = await client.GetAsync(host.Address + "/api/graph/export?projectId=sample&nodeId=file%3Adocs%2Fa.md");
                var searched = await gateway.CallAsync("search_project_graph", new { projectId = "sample", query = "Renamed" });
                var documentSearch = await client.GetFromJsonAsync<GraphSearch>(host.Address + "/api/graph/search?projectId=sample&q=&kind=document");
                var fileSearch = await client.GetFromJsonAsync<GraphSearch>(host.Address + "/api/graph/search?projectId=sample&q=&kind=files");
                var firstSearchPage = await client.GetFromJsonAsync<GraphSearch>(host.Address + "/api/graph/search?projectId=sample&q=&limit=1&offset=0");
                var secondSearchPage = await client.GetFromJsonAsync<GraphSearch>(host.Address + "/api/graph/search?projectId=sample&q=&limit=1&offset=1");
                using var invalidSearchPage = await client.GetAsync(host.Address + "/api/graph/search?projectId=sample&q=&offset=-1");
                var freshness = await gateway.CallAsync("check_graph_freshness", new { projectId = "sample" });
                var cycleTool = await gateway.CallAsync("get_graph_cycles", new { projectId = "sample" });
                var healthJson = await client.GetStringAsync(host.Address + "/api/graph/health?projectId=sample");
                var cyclesJson = await client.GetStringAsync(host.Address + "/api/graph/cycles?projectId=sample");
                var freshnessJson = await client.GetStringAsync(host.Address + "/api/graph/freshness?projectId=sample");
                var page = await client.GetStringAsync(host.Address + "/graph?projectId=sample");
                var explorer = await client.GetStringAsync(host.Address + "/graph/explore?projectId=sample");
                var legacyExplorer = await client.GetStringAsync(host.Address + "/graph/explore?projectId=sample&view=flow");
                var explorerScript = await client.GetStringAsync(host.Address + "/assets/graph-explorer.js");
                var explorerLabels = await client.GetStringAsync(host.Address + "/assets/graph-explorer-labels.js");
                var cyclePage = await client.GetStringAsync(host.Address + "/graph?projectId=sample&cycles=true");
                var revisionPage = await client.GetStringAsync(host.Address + "/graph?projectId=sample&fromRevision=1&toRevision=2");
                var routePage = await client.GetStringAsync(host.Address + "/graph?projectId=sample&routes=true");
                var communityPage = await client.GetStringAsync(host.Address + "/graph?projectId=sample&communities=true");
                using var sourceResponse = await client.GetAsync(host.Address + "/graph/source?projectId=sample&path=docs%2Fa.md&line=1");
                var sourcePage = await sourceResponse.Content.ReadAsStringAsync();
                var sourceWithWorkflow = await client.GetStringAsync(host.Address + "/graph/source?projectId=sample&path=docs%2Fa.md&line=1&workflowId=work-1");
                using var deniedSource = await client.GetAsync(host.Address + "/graph/source?projectId=sample&path=.env&line=1");
                using var traversalSource = await client.GetAsync(host.Address + "/graph/source?projectId=sample&path=..%2F.env&line=1");
                if (!explorer.Contains("무엇을 찾고 있나요?", StringComparison.Ordinal)
                    || !legacyExplorer.Contains("무엇을 찾고 있나요?", StringComparison.Ordinal)
                    || explorer.Contains("코드 흐름 보기", StringComparison.Ordinal)
                    || !sourceWithWorkflow.Contains("workflowId=work-1", StringComparison.Ordinal)
                    || !explorer.Contains("/assets/graph-explorer.js", StringComparison.Ordinal)
                    || !explorer.Contains("id=\"index-form\"", StringComparison.Ordinal)
                    || !explorer.Contains("data-kind=\"document\" aria-pressed=\"false\"", StringComparison.Ordinal)
                    || !explorer.Contains("id=\"search-pages\"", StringComparison.Ordinal)
                    || !explorerScript.Contains("get('/api/graph/files'", StringComparison.Ordinal)
                    || !explorerScript.Contains("...(selectedKind?{kind:selectedKind}:{})", StringComparison.Ordinal)
                    || !explorerScript.Contains("symbolTree(node,details", StringComparison.Ordinal)
                    || !explorerScript.Contains("graph-explorer-relations.js", StringComparison.Ordinal)
                    || documentSearch?.Nodes.Count == 0 || documentSearch?.Nodes.Any(node => node.Kind != "document") != false
                    || fileSearch?.Nodes.Count == 0 || fileSearch?.Nodes.Any(node => node.Kind == "heading") != false
                    || firstSearchPage?.Truncated != true || secondSearchPage?.Nodes.Count != 1
                    || firstSearchPage.Nodes[0].NodeId == secondSearchPage.Nodes[0].NodeId
                    || invalidSearchPage.StatusCode != System.Net.HttpStatusCode.BadRequest
                    || !indexOptions.Contains("\"indexed\":true", StringComparison.Ordinal)
                    || !indexOptions.Contains("\"docs\"", StringComparison.Ordinal)
                    || !explorerScript.Contains("relationControl.select(node)", StringComparison.Ordinal)
                    || !explorerLabels.Contains("labelLines", StringComparison.Ordinal)
                    || denied.StatusCode != System.Net.HttpStatusCode.Unauthorized
                    || !indexed.Contains("ProjectId", StringComparison.Ordinal)
                    || !searched.Contains("Renamed", StringComparison.Ordinal)
                    || !freshness.Contains("IsStale", StringComparison.Ordinal)
                    || !freshnessJson.Contains("\"isStale\":false", StringComparison.Ordinal)
                    || !healthJson.Contains("edgeCount", StringComparison.OrdinalIgnoreCase)
                    || !cyclesJson.Contains("file:docs/b.md", StringComparison.Ordinal)
                    || !cycleTool.Contains("file:docs/b.md", StringComparison.Ordinal)
                    || !cyclePage.Contains("순환 연결 그룹 1개", StringComparison.Ordinal)
                    || !revisionTool.Contains("file:docs/c.md", StringComparison.Ordinal)
                    || !revisionJson.Contains("file:docs/c.md", StringComparison.Ordinal)
                    || !revisionPage.Contains("변경 전후 그래프 차이", StringComparison.Ordinal)
                    || deniedReview.StatusCode != System.Net.HttpStatusCode.Unauthorized
                    || !reviewTool.Contains("CORRECTED", StringComparison.Ordinal)
                    || !reviewList.Contains("CORRECTED", StringComparison.Ordinal)
                    || !routeTool.Contains("GET /api/health", StringComparison.Ordinal)
                    || !routeJson.Contains("MCP safe_tool", StringComparison.Ordinal)
                    || !routePage.Contains("GET /api/health", StringComparison.Ordinal)
                    || !validationTool.Contains("VerificationUnrecorded", StringComparison.Ordinal)
                    || !validationJson.Contains("verificationUnrecorded", StringComparison.Ordinal)
                    || !scopeTool.Contains("file:docs/a.md", StringComparison.Ordinal)
                    || !scopeJson.Contains("ownerPath", StringComparison.Ordinal)
                    || download.Content.Headers.ContentDisposition?.FileName != "smsr-graph-scope.json"
                    || !communityTool.Contains("ScannedNodes", StringComparison.Ordinal)
                    || !communityJson.Contains("scannedNodes", StringComparison.Ordinal)
                    || !communityPage.Contains("커뮤니티 축약", StringComparison.Ordinal)
                    || sourceResponse.StatusCode != System.Net.HttpStatusCode.OK
                    || !sourcePage.Contains("Renamed", StringComparison.Ordinal)
                    || !sourceResponse.Headers.TryGetValues("Content-Security-Policy", out _)
                    || deniedSource.StatusCode != System.Net.HttpStatusCode.NotFound
                    || traversalSource.StatusCode != System.Net.HttpStatusCode.NotFound
                    || !page.Contains("Renamed", StringComparison.Ordinal))
                    throw new InvalidOperationException("HTTP/MCP 색인·조회·화면 연결 검증 실패");
                Directory.CreateDirectory(otherRoot);
                await GitAsync(otherRoot, "init", "-q");
                await File.WriteAllTextAsync(Path.Combine(otherRoot, "target.md"), "# Target\n");
                await File.WriteAllTextAsync(Path.Combine(otherRoot, "target.csproj"), "<Project/>\n");
                await gateway.CallAsync("index_project_graph", new { projectId = "other", rootPath = otherRoot });
                var otherName = Path.GetFileName(otherRoot);
                await File.AppendAllTextAsync(doc, $"\n[other](../../{otherName}/target.md)\n");
                await File.WriteAllTextAsync(Path.Combine(root, "src", "cross.csproj"),
                    $"<Project><ItemGroup><ProjectReference Include=\"../../{otherName}/target.csproj\" /></ItemGroup></Project>\n");
                await gateway.CallAsync("index_project_graph", new { projectId = "sample", rootPath = root });
                var crossTool = await gateway.CallAsync("get_cross_repo_graph", new { projectId = "sample" });
                var crossJson = await client.GetStringAsync(host.Address + "/api/graph/cross-repo?projectId=sample");
                var crossPage = await client.GetStringAsync(host.Address + "/graph?projectId=sample&crossRepo=true");
                if (!crossTool.Contains("file:target.md", StringComparison.Ordinal)
                    || !crossTool.Contains("file:target.csproj", StringComparison.Ordinal)
                    || !crossJson.Contains("targetProjectId", StringComparison.Ordinal)
                    || !crossPage.Contains("저장소 간 명시적 연결", StringComparison.Ordinal)
                    || !crossPage.Contains("target.csproj", StringComparison.Ordinal))
                    throw new InvalidOperationException("명시적 저장소 간 참조 연결 검증 실패");
                await GraphCrossRepoSelfCheck.RunAsync(new EventStore(Path.Combine(serverData, "smsr.db")));
                await GraphCrossRepoRetrySelfCheck.RunAsync(new EventStore(Path.Combine(serverData, "smsr.db")), root, otherRoot);
                File.Delete(Path.Combine(otherRoot, "target.md"));
                await gateway.CallAsync("index_project_graph", new { projectId = "other", rootPath = otherRoot });
                var pruned = await gateway.CallAsync("get_cross_repo_graph", new { projectId = "sample" });
                if (pruned.Contains("file:target.md", StringComparison.Ordinal)
                    || !pruned.Contains("file:target.csproj", StringComparison.Ordinal))
                    throw new InvalidOperationException("대상 파일 삭제 후 저장소 간 관계 갱신 실패");
                await new EventStore(Path.Combine(serverData, "smsr.db")).DeleteProjectAsync("other");
                if ((await client.GetStringAsync(host.Address + "/api/graph/cross-repo?projectId=sample"))
                    .Contains("target.csproj", StringComparison.Ordinal))
                    throw new InvalidOperationException("프로젝트 삭제 후 저장소 간 관계 정리 실패");
                client.DefaultRequestHeaders.Add("Origin", host.Address);
                using var scopedResponse = await client.PostAsJsonAsync(host.Address + "/api/graph/index",
                    new GraphIndexRequest("sample", AllowLargeReduction: true, Folders: ["docs"]));
                if (!scopedResponse.IsSuccessStatusCode
                    || !(await client.GetStringAsync(host.Address + "/api/graph/index-options?projectId=sample"))
                        .Contains("\"selectedFolders\":[\"docs\"]", StringComparison.Ordinal))
                    throw new InvalidOperationException("경로 입력 없는 폴더 색인 API 검증 실패");
                using var fullResponse = await client.PostAsJsonAsync(host.Address + "/api/graph/index",
                    new GraphIndexRequest("sample", AllowLargeReduction: true));
                if (!fullResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException("경로 입력 없는 전체 색인 API 검증 실패");
            }
            var folders = await GraphFileScanner.FoldersAsync(root, CancellationToken.None);
            if (!folders.Contains("docs") || !folders.Contains("src"))
                throw new InvalidOperationException("색인 폴더 목록 검증 실패");
            await index.IndexAsync("sample", root, ["docs"], allowLargeReduction: true);
            if ((await store.GetGraphScopeAsync("sample")).Single() != "docs"
                || (await store.GetGraphNodeAsync("sample", "file:src/cross.csproj")) is not null
                || (await index.CheckFreshnessAsync("sample")).IsStale)
                throw new InvalidOperationException("선택 폴더 색인·최신성 검증 실패");
            await index.IndexAsync("sample", root, [], allowLargeReduction: true);
            if ((await store.GetGraphScopeAsync("sample")).Count != 0
                || (await store.GetGraphNodeAsync("sample", "file:src/cross.csproj")) is null)
                throw new InvalidOperationException("전체 색인 복원 검증 실패");
            try { await index.IndexAsync("sample", root, ["../src"]); throw new InvalidOperationException("폴더 경로 검증 실패"); }
            catch (ArgumentException) { }
        }
        finally
        {
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (var path in new[] { root, otherRoot })
            {
                var full = Path.GetFullPath(path);
                if (full.StartsWith(temp, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(full).StartsWith("smsr-graph-", StringComparison.Ordinal)
                    && Directory.Exists(full)) Directory.Delete(full, true);
            }
        }
    }

    private static async Task GitAsync(string root, params string[] args)
    {
        var start = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add("-C"); start.ArgumentList.Add(root);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Git 실행 실패");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException(await process.StandardError.ReadToEndAsync());
    }
}
