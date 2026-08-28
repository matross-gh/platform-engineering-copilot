using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using OpenTelemetry;
using OpenTelemetry.Resources;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Platform.Engineering.Copilot.Chat.App.Data;
using Platform.Engineering.Copilot.Chat.App.Hubs;
using Platform.Engineering.Copilot.Chat.App.Services;
using Platform.Engineering.Copilot.Agents.Extensions;
using Platform.Engineering.Copilot.Core.Extensions;
using Platform.Engineering.Copilot.Core.Data.Context;
using Platform.Engineering.Copilot.Core.Data.Extensions;
using Platform.Engineering.Copilot.Core.Interfaces.GitHub;
using Azure.Identity;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault for secure secret management
var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"];
if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                // Prioritize Managed Identity for Azure deployments
                ManagedIdentityClientId = builder.Configuration["KeyVault:ManagedIdentityClientId"],
                // Exclude IDE credentials for production security
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true
            }));
        
        Log.Logger?.Information("✅ Azure Key Vault configured: {KeyVaultEndpoint}", keyVaultEndpoint);
    }
    catch (Exception ex)
    {
        Log.Logger?.Warning(ex, "⚠️  Failed to configure Azure Key Vault. Using local configuration only.");
    }
}
else
{
    Log.Logger?.Warning("⚠️  Key Vault not configured. Using local secrets only. Set 'KeyVault:Endpoint' in appsettings.json for production.");
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information) // Enable request logging
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Information) // Enable routing logs
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/chat-app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

try
{

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure Azure Monitor (Application Insights) - reads APPLICATIONINSIGHTS_CONNECTION_STRING,
// which ACI sets as an env var (see main.bicep). Without this, that env var is inert: nothing
// in the app was actually sending telemetry, so Live Metrics/Application Map had no data.
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("platform-copilot-chat"))
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
        });
    Log.Information("✅ Azure Monitor configured for Application Insights telemetry");
}
else
{
    Log.Warning("⚠️  APPLICATIONINSIGHTS_CONNECTION_STRING not set. No telemetry will be sent to Application Insights.");
}

// Add Entity Framework - Chat DB (Cosmos DB)
var chatCosmosEndpoint = builder.Configuration["CosmosDb:Endpoint"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Cosmos DB endpoint not found. Set 'CosmosDb:Endpoint' or connection string 'DefaultConnection'.");
var chatCosmosKey = builder.Configuration["CosmosDb:Key"] ?? string.Empty;
var chatCosmosDatabaseName = builder.Configuration["CosmosDb:ChatDatabaseName"] ?? builder.Configuration["CosmosDb:DatabaseName"] ?? "PlatformEngineeringCopilotChat";
Console.WriteLine($"[Chat] Using Cosmos DB for Chat database: {chatCosmosEndpoint}");
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseCosmosWithKeyOrEntraId(chatCosmosEndpoint, chatCosmosKey, chatCosmosDatabaseName));

// Add Entity Framework - Platform Management DB (required by agents) (Cosmos DB)
var platformCosmosEndpoint = builder.Configuration["CosmosDb:Endpoint"]
    ?? builder.Configuration.GetConnectionString("SqlServerConnection")
    ?? throw new InvalidOperationException("Cosmos DB endpoint not found. Set 'CosmosDb:Endpoint' or connection string 'SqlServerConnection'.");
var platformCosmosKey = builder.Configuration["CosmosDb:Key"] ?? string.Empty;
var platformCosmosDatabaseName = builder.Configuration["CosmosDb:DatabaseName"] ?? "PlatformEngineeringCopilot";
Console.WriteLine($"[Chat] Using Cosmos DB for Platform database: {platformCosmosEndpoint}");
builder.Services.AddDbContext<PlatformEngineeringCopilotContext>(options =>
    options.UseCosmosWithKeyOrEntraId(platformCosmosEndpoint, platformCosmosKey, platformCosmosDatabaseName));

// Add HttpClient for API integration
builder.Services.AddHttpClient();

// Add SignalR with minimal configuration
builder.Services.AddSignalR();

// Add CORS - allow all origins in production for ACI deployment
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:5001")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            // In production, allow any origin (for ACI deployment)
            // For tighter security, specify exact origins
            policy
                .SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// Register services
builder.Services.AddScoped<IChatService, ChatService>();

// Add required services for agents
builder.Services.AddScoped<IGitHubServices, Platform.Engineering.Copilot.Core.Services.GitHubGatewayService>();

// Register the Azure client factory (required by AzureResourceService, StigValidationService,
// AtoComplianceEngine, InfrastructureProvisioningService, and various Agent Framework tools -
// missing this caused a DI validation failure at startup in Development environments, since
// ASP.NET Core validates the whole container on Build() when running in Development).
builder.Services.AddAzureClientFactory();

// Register Platform.Engineering.Copilot.Core services (includes ConfigurationPlugin, OrchestratorAgent, SemanticKernelService, etc.)
builder.Services.AddPlatformEngineeringCopilotCore(builder.Configuration);

// Configure agent options from nested AgentConfiguration sections
// Add new Agent Framework (all agents registered via consolidated Agents project)
builder.Services.AddAgentFramework(builder.Configuration);

Log.Information("🚀 Agent Framework loaded with all agents");

// Add SPA services
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "wwwroot";
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Add request logging middleware to see all incoming requests
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseCors();

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseSpaStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

// Configure SPA - but exclude API routes from SPA proxy
app.MapWhen(context => !context.Request.Path.StartsWithSegments("/api") && 
                      !context.Request.Path.StartsWithSegments("/chathub"), 
    subApp =>
    {
        subApp.UseSpa(spa =>
        {
            spa.Options.SourcePath = "wwwroot";
            spa.Options.DefaultPage = "/index.html";

            if (app.Environment.IsDevelopment())
            {
                spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
            }
        });
    });

// Initialize databases (non-fatal: log and continue if the DB can't be created,
// e.g. SQLite fallback path isn't writable in a container - matches the MCP
// server's defensive pattern so a DB issue doesn't crash the whole app)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var chatContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await chatContext.Database.EnsureCreatedAsync();
        Log.Information("✅ Chat database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Failed to initialize Chat database");
    }

    try
    {
        var platformContext = scope.ServiceProvider.GetRequiredService<PlatformEngineeringCopilotContext>();
        await platformContext.Database.EnsureCreatedAsync();
        Log.Information("✅ Platform database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Failed to initialize Platform database");
    }
}

Log.Information("🚀 Enhanced Chat Application starting on {Environment}", app.Environment.EnvironmentName);

app.Run();
}
catch (Exception ex)
{
    // Catch-all so a startup failure is logged (and flushed) instead of the
    // process aborting silently with no output - this previously showed up
    // as an ACI CrashLoopBackOff with ExitCode 134 and zero captured logs.
    Log.Fatal(ex, "Chat application terminated unexpectedly");
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit
    Log.CloseAndFlush();
}