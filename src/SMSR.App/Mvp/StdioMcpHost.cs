using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

public static class StdioMcpHost
{
    public static async Task RunAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<McpHttpGateway>();
        services.AddMcpServer().WithStdioServerTransport().WithTools<StdioWorkflowTools>().WithTools<StdioPlanTools>();
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<McpServer>().RunAsync();
    }
}
