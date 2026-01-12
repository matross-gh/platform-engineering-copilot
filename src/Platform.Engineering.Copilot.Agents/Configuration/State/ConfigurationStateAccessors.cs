using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.Agents.Configuration.State;

/// <summary>
/// State accessors for Configuration Agent, providing typed access to configuration state.
/// Tracks subscription settings, environment preferences, and user configuration.
/// </summary>
public class ConfigurationStateAccessors
{
    private readonly IAgentStateManager _stateManager;
    private readonly ISharedMemory _sharedMemory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationStateAccessors> _logger;
    private readonly ConfigService _configService;

    private const string AgentType = "configuration";
    private const string CurrentSubscriptionKey = "current_subscription";
    private const string ConfigurationHistoryKey = "configuration_history";

    public ConfigurationStateAccessors(
        IAgentStateManager stateManager,
        ISharedMemory sharedMemory,
        IMemoryCache cache,
        ILogger<ConfigurationStateAccessors> logger,
        ConfigService configService)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _sharedMemory = sharedMemory ?? throw new ArgumentNullException(nameof(sharedMemory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <summary>
    /// Get the current subscription ID from persisted config or conversation state.
    /// </summary>
    public async Task<string?> GetCurrentSubscriptionAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        // 1. First check conversation state
        var subscription = await _sharedMemory.GetAsync<SubscriptionContext>(
            conversationId, CurrentSubscriptionKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(subscription?.SubscriptionId))
        {
            return subscription.SubscriptionId;
        }
        
        // 2. Fall back to persisted config (survives across sessions)
        var persistedSub = _configService.GetDefaultSubscription();
        if (!string.IsNullOrEmpty(persistedSub))
        {
            _logger.LogDebug("Using persisted subscription from config: {SubscriptionId}", persistedSub);
            // Store in conversation state for efficiency in subsequent calls
            await SetCurrentSubscriptionAsync(conversationId, persistedSub, null, cancellationToken);
            return persistedSub;
        }
        
        return null;
    }

    /// <summary>
    /// Set the current subscription ID in both conversation state and persisted config.
    /// </summary>
    public async Task SetCurrentSubscriptionAsync(
        string conversationId,
        string subscriptionId,
        string? subscriptionName = null,
        CancellationToken cancellationToken = default)
    {
        var context = new SubscriptionContext
        {
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            SetAt = DateTime.UtcNow
        };

        // Store in conversation state
        await _sharedMemory.SetAsync(
            conversationId, CurrentSubscriptionKey, context, cancellationToken);

        // Persist to config file for cross-session persistence
        _configService.SetDefaultSubscription(subscriptionId);

        _logger.LogInformation("📋 Subscription configured: {SubscriptionId} (persisted)", subscriptionId);

        // Publish event for other agents
        await PublishConfigurationEventAsync(conversationId, "subscription_configured", new
        {
            subscriptionId,
            subscriptionName,
            source = "ConfigurationAgent"
        }, cancellationToken);
    }

    /// <summary>
    /// Clear the current subscription from both conversation state and persisted config.
    /// </summary>
    public async Task ClearCurrentSubscriptionAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.RemoveAsync(conversationId, CurrentSubscriptionKey, cancellationToken);
        _configService.ClearDefaultSubscription();

        _logger.LogInformation("🗑️ Subscription configuration cleared");

        await PublishConfigurationEventAsync(conversationId, "subscription_cleared", new
        {
            source = "ConfigurationAgent"
        }, cancellationToken);
    }

    /// <summary>
    /// Record a configuration change in history.
    /// </summary>
    public async Task RecordConfigurationChangeAsync(
        string conversationId,
        string settingName,
        object? oldValue,
        object? newValue,
        CancellationToken cancellationToken = default)
    {
        var history = await _sharedMemory.GetAsync<List<ConfigurationChange>>(
            conversationId, ConfigurationHistoryKey, cancellationToken) ?? new List<ConfigurationChange>();

        history.Add(new ConfigurationChange
        {
            SettingName = settingName,
            OldValue = oldValue?.ToString(),
            NewValue = newValue?.ToString(),
            ChangedAt = DateTime.UtcNow
        });

        // Keep last 50 changes
        if (history.Count > 50)
        {
            history = history.Skip(history.Count - 50).ToList();
        }

        await _sharedMemory.SetAsync(
            conversationId, ConfigurationHistoryKey, history, cancellationToken);
    }

    /// <summary>
    /// Get recent configuration changes.
    /// </summary>
    public async Task<List<ConfigurationChange>> GetConfigurationHistoryAsync(
        string conversationId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var history = await _sharedMemory.GetAsync<List<ConfigurationChange>>(
            conversationId, ConfigurationHistoryKey, cancellationToken) ?? new List<ConfigurationChange>();

        return history.TakeLast(limit).ToList();
    }

    /// <summary>
    /// Publish a configuration event to shared memory for other agents.
    /// </summary>
    private async Task PublishConfigurationEventAsync(
        string conversationId,
        string eventType,
        object data,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sharedMemory.PublishEventAsync(conversationId, eventType, data, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to publish configuration event (non-critical): {Error}", ex.Message);
        }
    }
}

/// <summary>
/// Subscription context stored in shared memory.
/// </summary>
public class SubscriptionContext
{
    public string? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public DateTime SetAt { get; set; }
}

/// <summary>
/// Record of a configuration change.
/// </summary>
public class ConfigurationChange
{
    public string SettingName { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
}
