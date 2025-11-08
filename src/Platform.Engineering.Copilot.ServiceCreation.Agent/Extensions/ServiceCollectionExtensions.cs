using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Core.Interfaces.Agents;

namespace Platform.Engineering.Copilot.ServiceCreation.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceCreationCore(this IServiceCollection services)
    {
        // Register Service Creation Agent and Plugin
        services.AddScoped<ServiceCreationAgent>();
        services.AddScoped<ISpecializedAgent, ServiceCreationAgent>(sp => sp.GetRequiredService<ServiceCreationAgent>());
        services.AddScoped<ServiceCreationPlugin>();
        
        // TODO: Fix type mappings for ServiceCreationRequest, ServiceCreationValidationResult, etc.
        // These services need proper model types defined
        // services.AddScoped<IServiceCreationService, FlankspeedServiceCreationService>();
        // services.AddScoped<FlankspeedGenericServiceCreationAdapter>();
        
        return services;
    }
}
