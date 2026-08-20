using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;
using Platform.Engineering.Copilot.Core.Interfaces.Audits;
using Platform.Engineering.Copilot.Core.Interfaces.Notifications;
using Platform.Engineering.Copilot.Core.Interfaces.GitHub;
using Platform.Engineering.Copilot.Core.Interfaces.Jobs;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.Core.Services.Jobs;
using Platform.Engineering.Copilot.Core.Services.Azure;
using Platform.Engineering.Copilot.Core.Services.Azure.ResourceHealth;
using Platform.Engineering.Copilot.Core.Services.Azure.Security;
using Platform.Engineering.Copilot.Core.Services.Audits;
using Platform.Engineering.Copilot.Core.Services.Notifications;
using Platform.Engineering.Copilot.Core.Configuration;
using Platform.Engineering.Copilot.Core.Data.Repositories;

namespace Platform.Engineering.Copilot.Core.Extensions;

/// <summary>
/// Extension methods for registering Platform.Engineering.Copilot.Core services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all Platform.Engineering.Copilot.Core services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddPlatformEngineeringCopilotCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Azure Gateway configuration options
        services.AddOptions<AzureGatewayOptions>()
            .BindConfiguration(AzureGatewayOptions.SectionName);
        
        // Register caching services
        services.AddMemoryCache(); // Required for IMemoryCache
        
        // Register configuration service for persistent subscription storage
        services.AddSingleton<ConfigService>();
        
        // Note: ConfigurationPlugin has been moved to Infrastructure Agent
        // services.AddTransient<Plugins.ConfigurationPlugin>();
        
        // Note: AI chat capabilities are now provided via IChatClient from Microsoft.Extensions.AI,
        // registered in AddAzureOpenAIChatClient() in the Agents project.
        
        // Register Azure resource service - Singleton (no DbContext dependency)
        services.AddSingleton<IAzureResourceService, AzureResourceService>();
        services.AddSingleton<IAzureResourceService, AzureResourceService>();
        
        // Register Azure resource health service - Singleton (no DbContext dependency)
        services.AddSingleton<IAzureResourceHealthService, AzureResourceHealthService>();
                
        // Register audit logging service - Singleton (no DbContext dependency, uses in-memory store)
        services.AddSingleton<IAuditLoggingService, AuditLoggingService>();
        
        services.AddScoped<IAzureSecurityConfigurationService, AzureSecurityConfigurationService>();
        
        // Register Repository services (required by Storage services)
        services.AddScoped<IEnvironmentTemplateRepository, EnvironmentTemplateRepository>();
        services.AddScoped<IEnvironmentDeploymentRepository, EnvironmentDeploymentRepository>();
        services.AddScoped<IComplianceAssessmentRepository, ComplianceAssessmentRepository>();
        
        // Register GitHub Services - Singleton (no DbContext dependency)
        services.AddSingleton<IGitHubServices, GitHubGatewayService>();
        
        // Register Notification Services - Singleton (no DbContext dependency)
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<ISlackService, SlackService>();
        services.AddSingleton<ITeamsNotificationService, TeamsNotificationService>();

        // ========================================
        // MULTI-AGENT SYSTEM REGISTRATION
        // ========================================
        
        // Note: Agents and orchestration are now registered in Platform.Engineering.Copilot.Agents project
        // via PlatformAgentGroupChat, BaseAgent, and domain-specific agents.
        // AI chat capabilities are provided via IChatClient from Microsoft.Extensions.AI,
        // registered in AddAzureOpenAIChatClient() in the Agents project.
        
        // Register Background Job Service for long-running operations
        services.AddSingleton<IBackgroundJobService, BackgroundJobService>();
        
        // Register Job Cleanup Background Service
        services.AddHostedService<JobCleanupBackgroundService>();

        // Register Azure MCP Client (Microsoft's official Azure MCP Server integration)
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var gatewayOptions = new GatewayOptions();
            config.GetSection(GatewayOptions.SectionName).Bind(gatewayOptions);

            return new AzureMcpConfiguration
            {
                ReadOnly = config.GetValue("AzureMcp:ReadOnly", false),
                Debug = config.GetValue("AzureMcp:Debug", false),
                DisableUserConfirmation = config.GetValue("AzureMcp:DisableUserConfirmation", false),
                Namespaces = config.GetSection("AzureMcp:Namespaces").Get<string[]>(),

                // Set subscription and tenant from Gateway configuration or environment variables
                SubscriptionId = gatewayOptions.Azure.SubscriptionId ?? Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID"),
                TenantId = gatewayOptions.Azure.TenantId ?? Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
                AuthenticationMethod = "credential" // Use Azure Identity SDK (Service Principal, Managed Identity, or Azure CLI)
            };
        });
        services.AddSingleton<AzureMcpClient>();

        return services;
    }

    /// <summary>
    /// Add semantic processing services with custom configuration
    /// </summary>
    public static IServiceCollection AddSemanticProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddPlatformEngineeringCopilotCore(configuration);
    }
}
