using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.State.Abstractions;
using Platform.Engineering.Copilot.State.Configuration;
using Platform.Engineering.Copilot.State.Services;
using Platform.Engineering.Copilot.State.Stores;

namespace Platform.Engineering.Copilot.State.Extensions;

/// <summary>
/// Extension methods for registering state management services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add state management services.
    /// </summary>
    public static IServiceCollection AddStateManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var section = configuration.GetSection(StateOptions.SectionName);
        services.AddOptions<StateOptions>()
            .Bind(section);
        
        var options = new StateOptions();
        section.Bind(options);

        // Add memory cache (always needed)
        services.AddMemoryCache();

        // Add state store based on provider
        switch (options.Provider)
        {
            case StateProvider.Redis:
                if (string.IsNullOrEmpty(options.RedisConnectionString))
                {
                    throw new InvalidOperationException("Redis connection string is required when using Redis provider");
                }
                
                services.AddStackExchangeRedisCache(redisOptions =>
                {
                    redisOptions.Configuration = options.RedisConnectionString;
                    redisOptions.InstanceName = options.RedisInstanceName;
                });
                services.AddSingleton<IStateStore, RedisStateStore>();
                break;

            case StateProvider.Memory:
            default:
                services.AddSingleton<IStateStore, MemoryStateStore>();
                break;
        }

        // Add state managers
        services.AddScoped<IConversationStateManager, ConversationStateManager>();
        services.AddScoped<IAgentStateManager, AgentStateManager>();
        services.AddScoped<ISharedMemory, SharedMemory>();

        return services;
    }

    /// <summary>
    /// Add state management with in-memory store (for development/testing).
    /// </summary>
    public static IServiceCollection AddInMemoryStateManagement(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IStateStore, MemoryStateStore>();
        services.AddScoped<IConversationStateManager, ConversationStateManager>();
        services.AddScoped<IAgentStateManager, AgentStateManager>();
        services.AddScoped<ISharedMemory, SharedMemory>();

        return services;
    }
}
