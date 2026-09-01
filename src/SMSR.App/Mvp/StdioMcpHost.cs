using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

public static class StdioMcpHost
{
    public static async Task RunAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<McpHttpGateway>();
        services.AddMcpServer(options => options.ServerInstructions = SmsrMcpInstructions.Text)
            .WithStdioServerTransport()
            .WithTools<StdioWorkflowTools>()
            .WithTools<StdioPlanTools>()
            .WithTools<StdioAgentTools>();
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<McpServer>().RunAsync();
    }
}
