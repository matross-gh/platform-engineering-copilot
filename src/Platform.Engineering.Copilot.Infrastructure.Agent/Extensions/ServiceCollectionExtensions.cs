using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Core.Interfaces.Agents;
using Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;
using Platform.Engineering.Copilot.Infrastructure.Core.Services;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.Core.Services.ServiceCreation;
using Platform.Engineering.Copilot.Core.Services.Generators.Documentation;
using Platform.Engineering.Copilot.Infrastructure.Agent.Plugins;

namespace Platform.Engineering.Copilot.Infrastructure.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureAgent(this IServiceCollection services)
    {
        // Register Infrastructure Agent and Plugin
        services.AddScoped<InfrastructureAgent>();
        services.AddScoped<ISpecializedAgent, InfrastructureAgent>(sp => sp.GetRequiredService<InfrastructureAgent>());
        services.AddScoped<InfrastructurePlugin>();
        
        // Register Infrastructure Services
        services.AddScoped<NetworkTopologyDesignService>();
        services.AddScoped<INetworkTopologyDesignService, NetworkTopologyDesignService>();
        services.AddScoped<PredictiveScalingEngine>();
        services.AddScoped<InfrastructureProvisioningService>();
        services.AddScoped<IInfrastructureProvisioningService, InfrastructureProvisioningService>();

        // Register Azure Services
        services.AddScoped<IPredictiveScalingEngine, PredictiveScalingEngine>();
        
        // Register Template Generation Services
        services.AddScoped<DynamicTemplateGeneratorService>();
        services.AddScoped<IDynamicTemplateGenerator, DynamicTemplateGeneratorService>();
        
        // Register Service Wizard Services
        services.AddScoped<ServiceWizardStateManager>();
        services.AddScoped<WizardPromptEngine>();
        services.AddScoped<DoDMetadataValidator>();
        services.AddScoped<DoDDocumentationGenerator>();
        services.AddScoped<ServiceWizardPlugin>();
        
        return services;
    }
}
