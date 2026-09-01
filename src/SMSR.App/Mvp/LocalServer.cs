using System.Net;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

public sealed class LocalServer(WebApplication app, EventStore events, WorkflowSummaryService summaries,
    WorkflowExportService exports, WorkflowEventNotifier notifier, LocalOAuthStore oauth,
    McpConnectionTracker connections) : IAsyncDisposable
{
    public const int Port = 49783;
    public string Address => app.Urls.Single();
    public bool HasAuthorizedCodex => oauth.HasActiveAuthorization;
    public bool HasActiveMcpClient => connections.IsConnected;
    public DateTimeOffset? LastMcpActivityAt => connections.LastActivityAt;
    public event EventHandler? AuthorizationChanged
    {
        add => oauth.AuthorizationChanged += value;
        remove => oauth.AuthorizationChanged -= value;
    }
    public event EventHandler? ConnectionChanged
    {
        add => connections.Changed += value;
        remove => connections.Changed -= value;
    }
    public event EventHandler<WorkflowChangedEventArgs>? WorkflowChanged
    {
        add => notifier.Changed += value;
        remove => notifier.Changed -= value;
    }

    public static async Task<LocalServer> StartAsync(string? dataPath = null, int port = Port,
        Func<string>? dashboardTheme = null)
    {
        dataPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
        Directory.CreateDirectory(dataPath);
        var store = new EventStore(Path.Combine(dataPath, "smsr.db"));
        await store.InitializeAsync();
        var oauth = new LocalOAuthStore(Path.Combine(dataPath, "oauth-state.bin"));
        var bridgeToken = new McpBridgeToken(dataPath);
        var flows = new OAuthFlowStore();
        var oauthAudit = new OAuthAuditLog(Path.Combine(dataPath, "logs"));
        var notifier = new WorkflowEventNotifier();
        var activity = new ActivityJsonlStore(dataPath);
        var activityToken = new ActivityHookToken(dataPath);
        var connections = new McpConnectionTracker();
        var summaries = new WorkflowSummaryService(store);
        var exports = new WorkflowExportService(store, activity, Path.Combine(dataPath, "exports"), dashboardTheme);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(notifier);
        builder.Services.AddSingleton(summaries);
        builder.Services.AddSingleton(exports);
        builder.Services.AddSingleton(connections);
        builder.Services.AddMcpServer(options => options.ServerInstructions = SmsrMcpInstructions.Text)
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<WorkflowTools>()
            .WithTools<PlanTools>()
            .WithTools<AgentTools>();
        var app = builder.Build();
        LocalServerEndpoints.Map(app, oauth, bridgeToken, flows, oauthAudit, connections, notifier, activity, activityToken, dashboardTheme);
        await app.StartAsync();
        return new(app, store, summaries, exports, notifier, oauth, connections);
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

}
