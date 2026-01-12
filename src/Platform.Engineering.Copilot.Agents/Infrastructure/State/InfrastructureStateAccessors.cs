using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.State;

/// <summary>
/// State accessors for Infrastructure Agent, providing typed access to infrastructure-related state.
/// Tracks template generation, provisioning operations, and deployment state.
/// </summary>
public class InfrastructureStateAccessors
{
    private readonly IAgentStateManager _stateManager;
    private readonly ISharedMemory _sharedMemory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<InfrastructureStateAccessors> _logger;
    private readonly ConfigService? _configService;

    private const string AgentType = "infrastructure";
    private const string CurrentSubscriptionKey = "current_subscription";
    private const string GeneratedTemplatesCachePrefix = "generated_templates";
    private const string LastTemplateResultKey = "last_template_result";
    private const string ProvisioningStatusKey = "provisioning_status";
    private const string DeploymentHistoryKey = "deployment_history";

    public InfrastructureStateAccessors(
        IAgentStateManager stateManager,
        ISharedMemory sharedMemory,
        IMemoryCache cache,
        ILogger<InfrastructureStateAccessors> logger,
        ConfigService? configService = null)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _sharedMemory = sharedMemory ?? throw new ArgumentNullException(nameof(sharedMemory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService;
    }

    #region Subscription Management

    /// <summary>
    /// Get the current subscription ID from conversation state.
    /// Falls back to persisted config (~/.platform-copilot/config.json) if not in conversation state.
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
        var persistedSub = _configService?.GetDefaultSubscription();
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
    /// Set the current subscription ID in conversation state.
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

        await _sharedMemory.SetAsync(conversationId, CurrentSubscriptionKey, context, cancellationToken);
        _logger.LogDebug("Set current subscription for {ConversationId}: {SubscriptionId}",
            conversationId, subscriptionId);
    }

    #endregion

    #region Template Management

    /// <summary>
    /// Get cached template for a resource type.
    /// </summary>
    public TemplateCache? GetCachedTemplate(string conversationId, string resourceType, string format)
    {
        var cacheKey = $"{GeneratedTemplatesCachePrefix}:{conversationId}:{resourceType}:{format}";
        if (_cache.TryGetValue<TemplateCache>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for template: {ResourceType}/{Format}", resourceType, format);
            return cached;
        }
        return null;
    }

    /// <summary>
    /// Cache generated template.
    /// </summary>
    public void CacheTemplate(
        string conversationId,
        string resourceType,
        string format,
        TemplateCache template,
        TimeSpan? expiration = null)
    {
        var cacheKey = $"{GeneratedTemplatesCachePrefix}:{conversationId}:{resourceType}:{format}";
        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(60);
        _cache.Set(cacheKey, template, cacheExpiration);
        _logger.LogDebug("Cached template for {ResourceType}/{Format}, expires in {Minutes} minutes",
            resourceType, format, cacheExpiration.TotalMinutes);
    }

    /// <summary>
    /// Store last template result in shared memory for cross-agent access.
    /// </summary>
    public async Task ShareTemplateResultAsync(
        string conversationId,
        TemplateResultSummary summary,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, LastTemplateResultKey, summary, cancellationToken);
        await _sharedMemory.PublishEventAsync(conversationId, "template_generated", summary, cancellationToken);
        _logger.LogDebug("Shared template result for {ConversationId}: {TemplateName}",
            conversationId, summary.TemplateName);
    }

    /// <summary>
    /// Get shared template result.
    /// </summary>
    public async Task<TemplateResultSummary?> GetSharedTemplateResultAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<TemplateResultSummary>(
            conversationId, LastTemplateResultKey, cancellationToken);
    }

    #endregion

    #region Provisioning Status

    /// <summary>
    /// Update provisioning status in shared memory.
    /// </summary>
    public async Task UpdateProvisioningStatusAsync(
        string conversationId,
        ProvisioningStatus status,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, ProvisioningStatusKey, status, cancellationToken);

        // Publish event based on status
        var eventName = status.Status switch
        {
            "InProgress" => "provisioning_started",
            "Succeeded" => "provisioning_completed",
            "Failed" => "provisioning_failed",
            _ => "provisioning_updated"
        };
        await _sharedMemory.PublishEventAsync(conversationId, eventName, status, cancellationToken);
    }

    /// <summary>
    /// Get current provisioning status.
    /// </summary>
    public async Task<ProvisioningStatus?> GetProvisioningStatusAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<ProvisioningStatus>(
            conversationId, ProvisioningStatusKey, cancellationToken);
    }

    #endregion

    #region Deployment History

    /// <summary>
    /// Add deployment to history.
    /// </summary>
    public async Task AddDeploymentToHistoryAsync(
        string conversationId,
        DeploymentRecord record,
        CancellationToken cancellationToken = default)
    {
        var history = await _sharedMemory.GetAsync<List<DeploymentRecord>>(
            conversationId, DeploymentHistoryKey, cancellationToken) ?? new List<DeploymentRecord>();

        history.Add(record);

        // Keep only last 50 deployments
        if (history.Count > 50)
        {
            history = history.OrderByDescending(d => d.DeployedAt).Take(50).ToList();
        }

        await _sharedMemory.SetAsync(conversationId, DeploymentHistoryKey, history, cancellationToken);
    }

    /// <summary>
    /// Get deployment history.
    /// </summary>
    public async Task<List<DeploymentRecord>> GetDeploymentHistoryAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<List<DeploymentRecord>>(
            conversationId, DeploymentHistoryKey, cancellationToken) ?? new List<DeploymentRecord>();
    }

    #endregion

    #region Agent State Tracking

    /// <summary>
    /// Track infrastructure operation in agent state.
    /// </summary>
    public async Task TrackInfrastructureOperationAsync(
        string conversationId,
        string operationType,
        string resourceType,
        string subscriptionId,
        bool success,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var agentState = await _stateManager.GetAgentStateAsync(conversationId, AgentType, cancellationToken);

        agentState.SetData("lastOperationType", operationType);
        agentState.SetData("lastResourceType", resourceType);
        agentState.SetData("lastSubscriptionId", subscriptionId);
        agentState.SetData("lastOperationSuccess", success);
        agentState.SetData("lastOperationTime", DateTime.UtcNow);
        agentState.SetData("lastOperationDurationMs", (int)duration.TotalMilliseconds);

        // Track operation counts
        var opCount = agentState.GetData<int>("operationCount");
        agentState.SetData("operationCount", opCount + 1);

        if (success)
        {
            var successCount = agentState.GetData<int>("successfulOperations");
            agentState.SetData("successfulOperations", successCount + 1);
        }
        else
        {
            var failedCount = agentState.GetData<int>("failedOperations");
            agentState.SetData("failedOperations", failedCount + 1);
        }

        await _stateManager.SaveAgentStateAsync(conversationId, AgentType, agentState, cancellationToken);
    }

    #endregion
}

#region State Models

/// <summary>
/// Subscription context stored in shared memory.
/// </summary>
public class SubscriptionContext
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string? SubscriptionName { get; set; }
    public DateTime SetAt { get; set; }
}

/// <summary>
/// Cached template data.
/// </summary>
public class TemplateCache
{
    public string TemplateName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public Dictionary<string, string> Files { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Template result summary for cross-agent sharing.
/// </summary>
public class TemplateResultSummary
{
    public string TemplateName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public List<string> FileNames { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public bool ComplianceEnhanced { get; set; }
    public string? ComplianceFramework { get; set; }
}

/// <summary>
/// Provisioning status.
/// </summary>
public class ProvisioningStatus
{
    public string DeploymentId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public string? ResourceId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, string> Outputs { get; set; } = new();
}

/// <summary>
/// Deployment record for history tracking.
/// </summary>
public class DeploymentRecord
{
    public string DeploymentId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime DeployedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, string> Resources { get; set; } = new();
}

#endregion
