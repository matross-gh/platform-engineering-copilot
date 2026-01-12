using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.State.Abstractions;
using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.Agents.Compliance.State;

/// <summary>
/// State accessors for the Compliance Agent.
/// Provides access to shared memory and caching for compliance operations.
/// </summary>
public class ComplianceStateAccessors
{
    private readonly ISharedMemory _sharedMemory;
    private readonly IAgentStateManager _agentStateManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ComplianceStateAccessors> _logger;
    private readonly ConfigService? _configService;

    // State keys
    private const string CurrentSubscriptionKey = "compliance:current_subscription";
    private const string AssessmentResultsCachePrefix = "compliance:assessment";
    private const string RemediationStatusKey = "compliance:remediation_status";
    private const string ComplianceHistoryKey = "compliance:history";
    private const string ControlFamilyCachePrefix = "compliance:control_family";
    private const string EvidenceCachePrefix = "compliance:evidence";

    public ComplianceStateAccessors(
        ISharedMemory sharedMemory,
        IAgentStateManager agentStateManager,
        IMemoryCache cache,
        ILogger<ComplianceStateAccessors> logger,
        ConfigService? configService = null)
    {
        _sharedMemory = sharedMemory ?? throw new ArgumentNullException(nameof(sharedMemory));
        _agentStateManager = agentStateManager ?? throw new ArgumentNullException(nameof(agentStateManager));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService;
    }

    #region Subscription Context

    /// <summary>
    /// Get the current subscription ID from conversation state.
    /// Falls back to persisted config (~/.platform-copilot/config.json) if not in conversation state.
    /// </summary>
    public async Task<string?> GetCurrentSubscriptionAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        // 1. First check conversation state
        var context = await _sharedMemory.GetAsync<SubscriptionContext>(
            conversationId, CurrentSubscriptionKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(context?.SubscriptionId))
        {
            return context.SubscriptionId;
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
        _logger.LogDebug("Set compliance subscription for {ConversationId}: {SubscriptionId}",
            conversationId, subscriptionId);
    }

    #endregion

    #region Assessment Results

    /// <summary>
    /// Get cached assessment results for a subscription.
    /// </summary>
    public AssessmentResult? GetCachedAssessment(string conversationId, string subscriptionId, string framework)
    {
        var cacheKey = $"{AssessmentResultsCachePrefix}:{conversationId}:{subscriptionId}:{framework}";
        if (_cache.TryGetValue<AssessmentResult>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for assessment: {Framework} on {SubscriptionId}", framework, subscriptionId);
            return cached;
        }
        return null;
    }

    /// <summary>
    /// Cache assessment results.
    /// </summary>
    public void CacheAssessment(
        string conversationId,
        string subscriptionId,
        string framework,
        AssessmentResult result,
        TimeSpan? expiration = null)
    {
        var cacheKey = $"{AssessmentResultsCachePrefix}:{conversationId}:{subscriptionId}:{framework}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(4)
        };
        _cache.Set(cacheKey, result, options);
        _logger.LogDebug("Cached assessment for {Framework} on {SubscriptionId}", framework, subscriptionId);
    }

    /// <summary>
    /// Share assessment summary with other agents via shared memory.
    /// </summary>
    public async Task ShareAssessmentSummaryAsync(
        string conversationId,
        AssessmentSummary summary,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, $"{AssessmentResultsCachePrefix}:summary", summary, cancellationToken);
        _logger.LogDebug("Shared assessment summary for {ConversationId}", conversationId);
    }

    /// <summary>
    /// Get assessment summary from shared memory.
    /// </summary>
    public async Task<AssessmentSummary?> GetAssessmentSummaryAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<AssessmentSummary>(
            conversationId, $"{AssessmentResultsCachePrefix}:summary", cancellationToken);
    }

    #endregion

    #region Remediation Status

    /// <summary>
    /// Update remediation status in shared memory.
    /// </summary>
    public async Task UpdateRemediationStatusAsync(
        string conversationId,
        RemediationStatus status,
        CancellationToken cancellationToken = default)
    {
        var key = $"{RemediationStatusKey}:{status.RemediationId}";
        await _sharedMemory.SetAsync(conversationId, key, status, cancellationToken);
        _logger.LogDebug("Updated remediation status: {RemediationId} -> {Status}",
            status.RemediationId, status.Status);
    }

    /// <summary>
    /// Get remediation status from shared memory.
    /// </summary>
    public async Task<RemediationStatus?> GetRemediationStatusAsync(
        string conversationId,
        string remediationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{RemediationStatusKey}:{remediationId}";
        return await _sharedMemory.GetAsync<RemediationStatus>(conversationId, key, cancellationToken);
    }

    /// <summary>
    /// Add remediation to history.
    /// </summary>
    public async Task AddRemediationToHistoryAsync(
        string conversationId,
        RemediationRecord record,
        CancellationToken cancellationToken = default)
    {
        var history = await _sharedMemory.GetAsync<List<RemediationRecord>>(
            conversationId, ComplianceHistoryKey, cancellationToken) ?? new List<RemediationRecord>();
        
        history.Insert(0, record);
        if (history.Count > 50) history = history.Take(50).ToList();
        
        await _sharedMemory.SetAsync(conversationId, ComplianceHistoryKey, history, cancellationToken);
    }

    /// <summary>
    /// Get remediation history.
    /// </summary>
    public async Task<List<RemediationRecord>> GetRemediationHistoryAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<List<RemediationRecord>>(
            conversationId, ComplianceHistoryKey, cancellationToken) ?? new List<RemediationRecord>();
    }

    #endregion

    #region Control Family Cache

    /// <summary>
    /// Get cached control family details.
    /// </summary>
    public ControlFamilyDetails? GetCachedControlFamily(string family)
    {
        var cacheKey = $"{ControlFamilyCachePrefix}:{family}";
        if (_cache.TryGetValue<ControlFamilyDetails>(cacheKey, out var cached))
        {
            return cached;
        }
        return null;
    }

    /// <summary>
    /// Cache control family details.
    /// </summary>
    public void CacheControlFamily(string family, ControlFamilyDetails details)
    {
        var cacheKey = $"{ControlFamilyCachePrefix}:{family}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Controls don't change often
        };
        _cache.Set(cacheKey, details, options);
    }

    #endregion

    #region Evidence Cache

    /// <summary>
    /// Cache collected evidence.
    /// </summary>
    public void CacheEvidence(string conversationId, string controlId, EvidenceItem evidence)
    {
        var cacheKey = $"{EvidenceCachePrefix}:{conversationId}:{controlId}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
        };
        _cache.Set(cacheKey, evidence, options);
    }

    /// <summary>
    /// Get cached evidence.
    /// </summary>
    public EvidenceItem? GetCachedEvidence(string conversationId, string controlId)
    {
        var cacheKey = $"{EvidenceCachePrefix}:{conversationId}:{controlId}";
        if (_cache.TryGetValue<EvidenceItem>(cacheKey, out var cached))
        {
            return cached;
        }
        return null;
    }

    #endregion

    #region Agent State Tracking

    /// <summary>
    /// Track compliance operation in agent state.
    /// </summary>
    public async Task TrackComplianceOperationAsync(
        string conversationId,
        string operationType,
        string framework,
        string subscriptionId,
        bool success,
        int findingsCount,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var agentState = await _agentStateManager.GetAgentStateAsync(conversationId, "compliance", cancellationToken);
        var state = agentState?.Data ?? new Dictionary<string, object>();

        var operationKey = $"last_{operationType}";
        state[operationKey] = new
        {
            timestamp = DateTime.UtcNow,
            framework,
            subscriptionId,
            success,
            findingsCount,
            durationMs = (int)duration.TotalMilliseconds
        };

        // Track operation counts
        var countKey = $"{operationType}_count";
        state[countKey] = state.TryGetValue(countKey, out var count) ? (int)count + 1 : 1;

        var newState = agentState ?? new AgentState { AgentType = "compliance", ConversationId = conversationId };
        newState.Data = state;
        await _agentStateManager.SaveAgentStateAsync(conversationId, "compliance", newState, cancellationToken);
    }

    /// <summary>
    /// Get cached assessment by assessment ID.
    /// </summary>
    public async Task<AssessmentResult?> GetCachedAssessmentAsync(
        string conversationId,
        string assessmentId,
        CancellationToken cancellationToken = default)
    {
        // Try to get from shared memory first
        var result = await _sharedMemory.GetAsync<AssessmentResult>(
            conversationId, $"{AssessmentResultsCachePrefix}:{assessmentId}", cancellationToken);
        return result;
    }

    #endregion
}

#region State Models

/// <summary>
/// Subscription context for compliance operations.
/// </summary>
public class SubscriptionContext
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string? SubscriptionName { get; set; }
    public DateTime SetAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Assessment result from compliance scanning.
/// </summary>
public class AssessmentResult
{
    public string AssessmentId { get; set; } = Guid.NewGuid().ToString();
    public string Framework { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public int TotalControls { get; set; }
    public int PassedControls { get; set; }
    public int FailedControls { get; set; }
    public int NotApplicableControls { get; set; }
    public double CompliancePercentage { get; set; }
    public List<ComplianceFinding> Findings { get; set; } = new();
    public Dictionary<string, int> FindingsByControlFamily { get; set; } = new();
    public Dictionary<string, int> FindingsBySeverity { get; set; } = new();
}

/// <summary>
/// Summary of assessment for cross-agent sharing.
/// </summary>
public class AssessmentSummary
{
    public string AssessmentId { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; }
    public double CompliancePercentage { get; set; }
    public int CriticalFindings { get; set; }
    public int HighFindings { get; set; }
    public int MediumFindings { get; set; }
    public int LowFindings { get; set; }
    public List<string> TopControlFamiliesWithIssues { get; set; } = new();
}

/// <summary>
/// Individual compliance finding.
/// </summary>
public class ComplianceFinding
{
    public string FindingId { get; set; } = Guid.NewGuid().ToString();
    public string ControlId { get; set; } = string.Empty;
    public string ControlFamily { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public bool CanAutoRemediate { get; set; }
    public string? RemediationGuidance { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Remediation operation status.
/// </summary>
public class RemediationStatus
{
    public string RemediationId { get; set; } = Guid.NewGuid().ToString();
    public string FindingId { get; set; } = string.Empty;
    public string ControlId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed, RolledBack
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public bool DryRun { get; set; }
    public Dictionary<string, string> Actions { get; set; } = new();
}

/// <summary>
/// Record of a remediation operation for history.
/// </summary>
public class RemediationRecord
{
    public string RemediationId { get; set; } = string.Empty;
    public string ControlId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool DryRun { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Control family details.
/// </summary>
public class ControlFamilyDetails
{
    public string Family { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalControls { get; set; }
    public List<string> ControlIds { get; set; } = new();
}

/// <summary>
/// Evidence item for compliance.
/// </summary>
public class EvidenceItem
{
    public string EvidenceId { get; set; } = Guid.NewGuid().ToString();
    public string ControlId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public string? StorageUri { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

#endregion
