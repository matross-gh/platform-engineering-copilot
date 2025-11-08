using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Core.Interfaces.Agents;

namespace Platform.Engineering.Copilot.Discovery.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscoveryCore(this IServiceCollection services)
    {
        // Register Discovery Agent and Plugin
        services.AddScoped<DiscoveryAgent>();
        services.AddScoped<ISpecializedAgent, DiscoveryAgent>(sp => sp.GetRequiredService<DiscoveryAgent>());
        services.AddScoped<ResourceDiscoveryPlugin>();
        
        return services;
    }
}
