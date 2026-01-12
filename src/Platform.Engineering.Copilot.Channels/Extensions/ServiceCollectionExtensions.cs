using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Engineering.Copilot.Channels.Abstractions;
using Platform.Engineering.Copilot.Channels.Configuration;
using Platform.Engineering.Copilot.Channels.Services;

namespace Platform.Engineering.Copilot.Channels.Extensions;

/// <summary>
/// Extension methods for registering channel services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add channel services with configuration.
    /// </summary>
    public static IServiceCollection AddChannels(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var section = configuration.GetSection(ChannelOptions.SectionName);
        services.AddOptions<ChannelOptions>()
            .Bind(section);

        // Add core channel services
        services.AddSingleton<IChannel, InMemoryChannel>();
        services.AddSingleton<IChannelManager, ChannelManager>();
        services.AddScoped<IStreamingHandler, StreamingHandler>();
        services.AddScoped<IMessageHandler, DefaultMessageHandler>();

        return services;
    }

    /// <summary>
    /// Add channel services with in-memory channel (for development/testing).
    /// </summary>
    public static IServiceCollection AddInMemoryChannels(this IServiceCollection services)
    {
        services.AddSingleton<IChannel, InMemoryChannel>();
        services.AddSingleton<IChannelManager, ChannelManager>();
        services.AddScoped<IStreamingHandler, StreamingHandler>();
        services.AddScoped<IMessageHandler, DefaultMessageHandler>();

        return services;
    }
}
