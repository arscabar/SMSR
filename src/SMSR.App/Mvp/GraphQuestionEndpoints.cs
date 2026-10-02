using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
namespace SMSR.App.Mvp;
internal static class GraphQuestionEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/api/graph/media-analysis",(string projectId,string path,bool? retry,GraphMediaAnalysisService service,Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime)
            =>GraphExplorerEndpoints.Reply(()=>GraphMediaPolling.StateAsync(service,projectId,path,lifetime.ApplicationStopping,retry??false)));
        app.MapGet("/api/graph/watch",(string projectId,GraphWatchService service)=>Results.Ok(service.Get(projectId)));
        app.MapPost("/api/graph/watch",(GraphWatchRequest request,HttpRequest http,GraphWatchService service,CancellationToken ct)
            =>!LocalServerEndpoints.SameOrigin(http)?Task.FromResult<IResult>(Results.Unauthorized()):GraphExplorerEndpoints.Reply(()=>service.SetAsync(request.ProjectId,request.Enabled,ct)));
        app.MapGet("/api/graph/report",(string projectId,GraphReportService service,Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime)
            =>GraphExplorerEndpoints.Reply(()=>service.GetAsync(projectId,lifetime.ApplicationStopping)));
        app.MapGet("/api/graph/vocabulary",(string projectId,int? offset,int? limit,GraphQuestionService service,CancellationToken ct)
            =>GraphExplorerEndpoints.Reply(()=>service.VocabularyAsync(projectId,offset??0,limit??100,ct)));
        // POST keeps the raw question out of request URL/access logs; no request body logging.
        app.MapPost("/api/graph/question-search",(GraphQuestionRequest request,HttpRequest http,GraphQuestionService service,CancellationToken ct)
            =>!LocalServerEndpoints.SameOrigin(http)?Task.FromResult<IResult>(Results.Unauthorized()):GraphExplorerEndpoints.Reply(()=>service.SearchAsync(request,ct)));
        app.MapPost("/api/graph/question-evidence",(GraphQuestionRequest request,HttpRequest http,GraphQuestionService service,CancellationToken ct)
            =>!LocalServerEndpoints.SameOrigin(http)?Task.FromResult<IResult>(Results.Unauthorized()):GraphExplorerEndpoints.Reply(()=>service.EvidenceAsync(request,ct)));
        app.MapGet("/api/graph/feedback-lessons",(string projectId,string sourceId,GraphQuestionService service,CancellationToken ct)
            =>GraphExplorerEndpoints.Reply(()=>service.LessonsAsync(projectId,sourceId,ct)));
    }
}
internal sealed record GraphWatchRequest(string ProjectId,bool Enabled);
