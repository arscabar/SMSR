using System.Net;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

public sealed class LocalServer(WebApplication app, string token, EventStore events, WorkflowSummaryService summaries, WorkflowExportService exports) : IAsyncDisposable
{
    public string Token { get; } = token;
    public string Address => app.Urls.Single();

    public static async Task<LocalServer> StartAsync(string? dataPath = null)
    {
        dataPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
        Directory.CreateDirectory(dataPath);
        var store = new EventStore(Path.Combine(dataPath, "smsr.db"));
        await store.InitializeAsync();
        var token = new LocalTokenStore(Path.Combine(dataPath, "mcp-token.bin")).GetOrCreate();
        var notifier = new WorkflowEventNotifier();
        var summaries = new WorkflowSummaryService(store);
        var exports = new WorkflowExportService(store, Path.Combine(dataPath, "exports"));
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(notifier);
        builder.Services.AddSingleton(summaries);
        builder.Services.AddSingleton(exports);
        builder.Services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<WorkflowTools>();
        var app = builder.Build();
        LocalServerEndpoints.Map(app, token);
        await app.StartAsync();
        return new(app, token, store, summaries, exports);
    }

    public Task<IReadOnlyList<string>> GetProjectIdsAsync(CancellationToken cancellationToken = default)
        => events.GetProjectIdsAsync(cancellationToken);

    public Task<IReadOnlyList<string>> GetWorkflowIdsAsync(string projectId, CancellationToken cancellationToken = default)
        => events.GetWorkflowIdsAsync(projectId, cancellationToken);

    public Task<WorkflowState> GetStateAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
        => events.GetStateAsync(projectId, workflowId, cancellationToken);

    public Task<IReadOnlyList<RecentEvent>> GetRecentEventsAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
        => events.GetRecentEventsAsync(projectId, workflowId, cancellationToken);

    public Task<WorkflowSummary?> GetLatestSummaryAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
        => events.GetLatestSummaryAsync(projectId, workflowId, cancellationToken);

    public Task<WorkflowSummary> GenerateSummaryAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
        => summaries.GenerateAsync(projectId, workflowId, cancellationToken);

    public Task<ExportResult> ExportAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
        => exports.ExportAsync(projectId, workflowId, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync().ConfigureAwait(false);
        await app.DisposeAsync().ConfigureAwait(false);
    }

    internal static bool IsAuthorized(HttpRequest request, string token)
        => LocalServerEndpoints.IsAuthorized(request, token);
}
