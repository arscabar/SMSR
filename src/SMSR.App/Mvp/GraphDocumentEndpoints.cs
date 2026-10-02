using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class GraphDocumentEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/graph/document-page-image",async(string projectId,string path,int page,GraphDocumentService service,CancellationToken ct)=>
        {
            try{return Results.File(await service.PreviewAsync(projectId,path,page,ct),"image/jpeg");}
            catch(Exception e)when(e is ArgumentException or InvalidOperationException or KeyNotFoundException or System.IO.IOException)
            {return Results.BadRequest("PDF 페이지를 읽지 못했습니다. 색인·페이지·모델·제한을 확인하세요.");}
        });
        app.MapGet("/graph/document", async (string projectId, string path, string? location, string? workflowId,
            GraphDocumentService service, CancellationToken ct) =>
        {
            try { return Results.Content(GraphDocumentPage.Render(projectId, path,
                await service.ReadAsync(projectId, path, ct), location, workflowId), "text/html; charset=utf-8"); }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or System.IO.IOException)
            { return Results.BadRequest("문서 근거를 읽지 못했습니다. 색인·형식·지문·실행 제한을 확인하세요."); }
        });
        app.MapGet("/api/graph/document", (string projectId, string path, GraphDocumentService service, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => service.ReadAsync(projectId, path, ct)));
        app.MapGet("/api/graph/overview", (string projectId, int? offset, int? limit,string? q, GraphOverviewService service, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => service.PageAsync(projectId, offset ?? 0, limit ?? 12, ct,q)));
        app.MapGet("/api/graph/group-members", (string projectId, int groupId, int revision, int? offset, int? limit,
            GraphOverviewService service, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => service.MembersAsync(projectId, groupId, revision, offset ?? 0, limit ?? 50, ct)));
        app.MapGet("/api/graph/document-semantic", (string projectId, string path, GraphSemanticService service, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => service.ReadAsync(projectId, path, ct)));
        app.MapPost("/api/graph/document-semantic", (GraphSemanticRequest request, HttpRequest http, GraphSemanticService service, CancellationToken ct)
            => !LocalServerEndpoints.SameOrigin(http) ? Task.FromResult<IResult>(Results.Unauthorized())
                : GraphExplorerEndpoints.Reply(() => service.SubmitAsync(request, ct)));
    }
}
