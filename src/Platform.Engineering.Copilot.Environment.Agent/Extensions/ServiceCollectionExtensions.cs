using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Core.Interfaces.Agents;
using Platform.Engineering.Copilot.Core.Interfaces.Deployment;
using Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;
using Platform.Engineering.Copilot.Environment.Agent.Services.Deployment;
using Platform.Engineering.Copilot.Infrastructure.Core.Services;

namespace Platform.Engineering.Copilot.Environment.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEnvironmentCore(this IServiceCollection services)
    {
        // Register Environment Agent and Plugin
        services.AddScoped<EnvironmentAgent>();
        services.AddScoped<ISpecializedAgent, EnvironmentAgent>(sp => sp.GetRequiredService<EnvironmentAgent>());
        services.AddScoped<EnvironmentManagementPlugin>();

        // Register Environment Management Services
        services.AddScoped<IEnvironmentManagementEngine, EnvironmentManagementEngine>();
        
        // Register Environment Storage Service - Scoped (requires DbContext)
        services.AddScoped<EnvironmentStorageService>();
        
        // Register deployment orchestration service - Singleton (no DbContext dependency)
        services.AddSingleton<IDeploymentOrchestrationService, DeploymentOrchestrationService>();

        
        return services;
    }
}
