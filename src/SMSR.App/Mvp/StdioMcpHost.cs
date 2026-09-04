using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

public static class StdioMcpHost
{
    public static async Task RunAsync()
    {
        await ConnectDashboardAsync();
        var services = new ServiceCollection();
        services.AddSingleton<McpHttpGateway>();
        services.AddMcpServer(options => options.ServerInstructions = SmsrMcpInstructions.Text)
            .WithStdioServerTransport()
            .WithTools<StdioWorkflowTools>()
            .WithTools<StdioPlanTools>()
            .WithTools<StdioAgentTools>()
            .WithTools<StdioDailyActivityTools>()
            .WithTools<StdioDailySummaryTools>();
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<McpServer>().RunAsync();
    }

    private static async Task ConnectDashboardAsync()
    {
        try
        {
            if (await DashboardProcessLauncher.EnsureStartedAsync())
                await McpBridgeConnection.NotifyAsync();
        }
        catch
        {
            // Keep the stdio transport alive; the gateway returns a bounded server error on use.
        }
    }
}
