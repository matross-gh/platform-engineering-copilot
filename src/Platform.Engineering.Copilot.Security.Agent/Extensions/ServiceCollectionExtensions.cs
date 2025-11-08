using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Security.Agent.Plugins;

namespace Platform.Engineering.Copilot.Security.Agent.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityAgent(this IServiceCollection services)
    {
        // Register Security Plugin
        services.AddScoped<SecurityPlugin>();
        
        return services;
    }
}
