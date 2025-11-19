using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Platform.Engineering.Copilot.Core.Extensions;
using Platform.Engineering.Copilot.Core.Data.Context;
using Platform.Engineering.Copilot.Mcp.Server;
using Platform.Engineering.Copilot.Mcp.Tools;
using Platform.Engineering.Copilot.Mcp.Middleware;
using Platform.Engineering.Copilot.Compliance.Core.Extensions;
using Platform.Engineering.Copilot.Infrastructure.Core.Extensions;
using Platform.Engineering.Copilot.CostManagement.Core.Extensions;
using Platform.Engineering.Copilot.Environment.Core.Extensions;
using Platform.Engineering.Copilot.Discovery.Core.Extensions;
using Serilog;
using Platform.Engineering.Copilot.Security.Agent.Extensions;
using Platform.Engineering.Copilot.KnowledgeBase.Agent.Extensions;

namespace Platform.Engineering.Copilot.Mcp;

/// <summary>
/// MCP server host service (stdio mode for GitHub Copilot/Claude)
/// </summary>
public class McpStdioService : BackgroundService
{
    private readonly McpServer _mcpServer;
    private readonly ILogger<McpStdioService> _logger;

    public McpStdioService(McpServer mcpServer, ILogger<McpStdioService> logger)
    {
        _mcpServer = mcpServer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("📡 MCP stdio server starting...");
            await _mcpServer.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP stdio server failed");
            throw;
        }
    }
}

/// <summary>
/// Program entry point - Dual-mode MCP server
/// Supports BOTH stdio (for GitHub Copilot/Claude) and HTTP (for Chat web app)
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Check if running in HTTP mode (--http flag)
        var httpMode = args.Contains("--http");
        var httpPort = GetHttpPort(args);

        // Configure Serilog for MCP server (write to stderr to avoid interfering with stdout)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();

        try
        {
            if (httpMode)
            {
                await RunHttpModeAsync(httpPort);
            }
            else
            {
                await RunStdioModeAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "MCP server terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Run in stdio mode for external AI tools (GitHub Copilot, Claude Desktop, Cline)
    /// </summary>
    static async Task RunStdioModeAsync()
    {
        Log.Information("🚀 Starting MCP server in STDIO mode (for GitHub Copilot/Claude)");

        var builder = Host.CreateApplicationBuilder();

        // Configure services
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog();
        });

        // Register database context (SQLite)
        var dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..", "platform_engineering_copilot_management.db"));
        var connectionString = $"Data Source={dbPath}";
        builder.Services.AddDbContext<PlatformEngineeringCopilotContext>(options =>
            options.UseSqlite(connectionString));

        // Register HttpClient for services that need it (like NistControlsService)
        builder.Services.AddHttpClient();

        // Add Core services (Multi-Agent Orchestrator, Plugins, etc.)
        builder.Services.AddPlatformEngineeringCopilotCore();

        // Register MCP Chat Tool - Scoped to match IIntelligentChatService
        builder.Services.AddScoped<PlatformEngineeringCopilotTools>();

        // Register MCP server for stdio
        builder.Services.AddSingleton<McpServer>();
        builder.Services.AddHostedService<McpStdioService>();

        var host = builder.Build();
        await host.RunAsync();
    }

    /// <summary>
    /// Run in HTTP mode for web apps (Chat client)
    /// </summary>
    static async Task RunHttpModeAsync(int port)
    {
        Log.Information("🌐 Starting MCP server in HTTP mode on port {Port} (for Chat web app)", port);

        var builder = WebApplication.CreateBuilder();

        // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog();
        });

        // Register database context (SQLite) - use SQL Server connection from config
        var configuration = builder.Configuration;
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration.GetConnectionString("SqlServerConnection");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            builder.Services.AddDbContext<PlatformEngineeringCopilotContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            // Fallback to SQLite
            var dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..", "platform_engineering_copilot_management.db"));
            var sqliteConnectionString = $"Data Source={dbPath}";
            builder.Services.AddDbContext<PlatformEngineeringCopilotContext>(options =>
                options.UseSqlite(sqliteConnectionString));
        }

        // Register HttpClient for services that need it (like NistControlsService)
        builder.Services.AddHttpClient();

        // Add Core services (Multi-Agent Orchestrator, Plugins, etc.)
        builder.Services.AddPlatformEngineeringCopilotCore();
        
        // Configure which agents are enabled
        builder.Services.Configure<Platform.Engineering.Copilot.Core.Configuration.AgentConfiguration>(
            builder.Configuration.GetSection(Platform.Engineering.Copilot.Core.Configuration.AgentConfiguration.SectionName));
        
        // Configure individual agent options from nested sections
        builder.Services.Configure<Platform.Engineering.Copilot.Infrastructure.Agent.Configuration.InfrastructureAgentOptions>(
            builder.Configuration.GetSection("AgentConfiguration:InfrastructureAgent"));
        builder.Services.Configure<Platform.Engineering.Copilot.Compliance.Core.Configuration.ComplianceAgentOptions>(
            builder.Configuration.GetSection("AgentConfiguration:ComplianceAgent"));
        builder.Services.Configure<Platform.Engineering.Copilot.CostManagement.Core.Configuration.CostManagementAgentOptions>(
            builder.Configuration.GetSection("AgentConfiguration:CostManagementAgent"));
        builder.Services.Configure<Platform.Engineering.Copilot.Discovery.Core.Configuration.DiscoveryAgentOptions>(
            builder.Configuration.GetSection("AgentConfiguration:DiscoveryAgent"));
        
        // Add domain-specific agents and plugins based on configuration
        var agentConfigSection = builder.Configuration.GetSection(Platform.Engineering.Copilot.Core.Configuration.AgentConfiguration.SectionName);
        var agentConfig = new Platform.Engineering.Copilot.Core.Configuration.AgentConfiguration();
        agentConfig.SetConfiguration(agentConfigSection);

        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("AgentLoader");
        logger.LogInformation("🔧 Loading agents based on configuration...");

        int enabledCount = 0;

        if (agentConfig.IsAgentEnabled("Compliance"))
        {
            logger.LogInformation("✅ Compliance agent enabled");
            enabledCount++;
        }
        
        if (agentConfig.IsAgentEnabled("Infrastructure"))
        {
            logger.LogInformation("✅ Infrastructure agent enabled");
            enabledCount++;
        }

        if (agentConfig.IsAgentEnabled("CostManagement"))
        {
            logger.LogInformation("✅ CostManagement agent enabled");
            enabledCount++;
        }

        if (agentConfig.IsAgentEnabled("Environment"))
        {
            builder.Services.AddEnvironmentAgent();
            logger.LogInformation("✅ Environment agent enabled");
            enabledCount++;
        }

        if (agentConfig.IsAgentEnabled("Discovery"))
        {
            logger.LogInformation("✅ Discovery agent enabled");
            enabledCount++;
        }

        if (agentConfig.IsAgentEnabled("Security"))
        {
            builder.Services.AddSecurityAgent();
            logger.LogInformation("✅ Security agent enabled");
            enabledCount++;
        }

        if (agentConfig.IsAgentEnabled("KnowledgeBase"))
        {
            var knowledgeBaseConfig = builder.Configuration.GetSection("AgentConfiguration:KnowledgeBaseAgent");
            builder.Services.AddKnowledgeBaseAgent(knowledgeBaseConfig);
            logger.LogInformation("✅ Knowledge Base agent enabled");
            enabledCount++;
        }

        logger.LogInformation($"🚀 Loaded {enabledCount} agents");

        // Register MCP Chat Tool - Scoped to match IIntelligentChatService
        builder.Services.AddScoped<PlatformEngineeringCopilotTools>();

        // Register HTTP bridge - Singleton (resolves scoped services per request)
        builder.Services.AddSingleton<McpHttpBridge>();

        var app = builder.Build();

        // Apply database migrations automatically
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<PlatformEngineeringCopilotContext>();
                logger.LogInformation("🔄 Ensuring database is created...");
                context.Database.EnsureCreated();
                logger.LogInformation("✅ Database created/verified successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to create/verify database");
            }
        }

        // Configure URLs
        app.Urls.Add($"http://0.0.0.0:{port}");

        // Add audit logging middleware for HTTP requests
        app.UseMiddleware<AuditLoggingMiddleware>();

        // Map HTTP endpoints
        var httpBridge = app.Services.GetRequiredService<McpHttpBridge>();
        httpBridge.MapHttpEndpoints(app);

        Log.Information("✅ MCP HTTP server ready on http://localhost:{Port}", port);
        await app.RunAsync();
    }

    /// <summary>
    /// Get HTTP port from args (default: 5100)
    /// </summary>
    static int GetHttpPort(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port" && int.TryParse(args[i + 1], out int port))
            {
                return port;
            }
        }
        return 5100; // Default port
    }
}