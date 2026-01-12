using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Mcp.Server;

namespace Platform.Engineering.Copilot.Mcp.Server;

/// <summary>
/// MCP server host service (stdio mode) using domain-specific tool architecture
/// </summary>
public class McpStdioService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<McpStdioService> _logger;

    public McpStdioService(IServiceProvider serviceProvider, ILogger<McpStdioService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("📡 MCP stdio server starting (domain-specific tools)...");

            // Create a scope for scoped services
            using var scope = _serviceProvider.CreateScope();
            var mcpServer = scope.ServiceProvider.GetRequiredService<McpServer>();

            await mcpServer.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP stdio server failed");
            throw;
        }
    }
}
