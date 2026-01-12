using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.Agents.CostManagement.State;

/// <summary>
/// State accessors for Cost Management Agent, providing typed access to cost-related state.
/// Tracks subscription context, cost data cache, optimization recommendations, and budget alerts.
/// </summary>
public class CostManagementStateAccessors
{
    private readonly IAgentStateManager _stateManager;
    private readonly ISharedMemory _sharedMemory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CostManagementStateAccessors> _logger;
    private readonly ConfigService? _configService;

    private const string AgentType = "cost-management";
    private const string CurrentSubscriptionKey = "current_subscription";
    private const string CostDashboardCachePrefix = "cost_dashboard";
    private const string OptimizationResultsKey = "optimization_results";
    private const string BudgetAlertsKey = "budget_alerts";
    private const string ForecastResultsKey = "forecast_results";
    private const string LastAnalysisResultsKey = "last_cost_analysis";

    public CostManagementStateAccessors(
        IAgentStateManager stateManager,
        ISharedMemory sharedMemory,
        IMemoryCache cache,
        ILogger<CostManagementStateAccessors> logger,
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

    #region Cost Dashboard Caching

    /// <summary>
    /// Get cached cost dashboard for a subscription.
    /// </summary>
    public CostDashboardCache? GetCachedDashboard(string subscriptionId, int lookbackDays)
    {
        var cacheKey = $"{CostDashboardCachePrefix}:{subscriptionId}:{lookbackDays}";
        if (_cache.TryGetValue<CostDashboardCache>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for cost dashboard: {SubscriptionId}", subscriptionId);
            return cached;
        }
        return null;
    }

    /// <summary>
    /// Cache cost dashboard for a subscription.
    /// </summary>
    public void CacheDashboard(
        string subscriptionId,
        int lookbackDays,
        CostDashboardCache dashboard,
        TimeSpan? expiration = null)
    {
        var cacheKey = $"{CostDashboardCachePrefix}:{subscriptionId}:{lookbackDays}";
        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(60);
        _cache.Set(cacheKey, dashboard, cacheExpiration);
        _logger.LogDebug("Cached cost dashboard for subscription {SubscriptionId}, expires in {Minutes} minutes",
            subscriptionId, cacheExpiration.TotalMinutes);
    }

    /// <summary>
    /// Invalidate cached dashboard for a subscription.
    /// </summary>
    public void InvalidateDashboardCache(string subscriptionId)
    {
        // Invalidate common lookback periods
        foreach (var days in new[] { 7, 14, 30, 60, 90 })
        {
            var cacheKey = $"{CostDashboardCachePrefix}:{subscriptionId}:{days}";
            _cache.Remove(cacheKey);
        }
        _logger.LogDebug("Invalidated dashboard cache for subscription {SubscriptionId}", subscriptionId);
    }

    #endregion

    #region Optimization Results

    /// <summary>
    /// Store optimization results in shared memory for cross-agent access.
    /// </summary>
    public async Task ShareOptimizationResultsAsync(
        string conversationId,
        OptimizationResultSummary summary,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, OptimizationResultsKey, summary, cancellationToken);
        await _sharedMemory.PublishEventAsync(conversationId, "cost_optimization_completed", summary, cancellationToken);
        _logger.LogDebug("Shared optimization results for {ConversationId}: {RecommendationCount} recommendations, ${Savings} potential savings",
            conversationId, summary.RecommendationCount, summary.TotalPotentialSavings);
    }

    /// <summary>
    /// Get shared optimization results from this or another agent's operation.
    /// </summary>
    public async Task<OptimizationResultSummary?> GetSharedOptimizationResultsAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<OptimizationResultSummary>(
            conversationId, OptimizationResultsKey, cancellationToken);
    }

    #endregion

    #region Budget Alerts

    /// <summary>
    /// Store budget alerts in shared memory.
    /// </summary>
    public async Task ShareBudgetAlertsAsync(
        string conversationId,
        List<BudgetAlertSummary> alerts,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, BudgetAlertsKey, alerts, cancellationToken);
        
        var criticalAlerts = alerts.Where(a => a.Severity == "Critical").ToList();
        if (criticalAlerts.Any())
        {
            await _sharedMemory.PublishEventAsync(conversationId, "budget_alert_critical", criticalAlerts, cancellationToken);
        }
    }

    /// <summary>
    /// Get shared budget alerts.
    /// </summary>
    public async Task<List<BudgetAlertSummary>?> GetSharedBudgetAlertsAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<List<BudgetAlertSummary>>(
            conversationId, BudgetAlertsKey, cancellationToken);
    }

    #endregion

    #region Forecast Results

    /// <summary>
    /// Store forecast results in shared memory.
    /// </summary>
    public async Task ShareForecastResultsAsync(
        string conversationId,
        ForecastResultSummary summary,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, ForecastResultsKey, summary, cancellationToken);
        await _sharedMemory.PublishEventAsync(conversationId, "cost_forecast_completed", summary, cancellationToken);
    }

    /// <summary>
    /// Get shared forecast results.
    /// </summary>
    public async Task<ForecastResultSummary?> GetSharedForecastResultsAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<ForecastResultSummary>(
            conversationId, ForecastResultsKey, cancellationToken);
    }

    #endregion

    #region Agent State Tracking

    /// <summary>
    /// Track cost analysis operation in agent state.
    /// </summary>
    public async Task TrackCostAnalysisOperationAsync(
        string conversationId,
        string operationType,
        string subscriptionId,
        decimal totalCost,
        decimal potentialSavings,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var agentState = await _stateManager.GetAgentStateAsync(conversationId, AgentType, cancellationToken);

        agentState.SetData("lastOperationType", operationType);
        agentState.SetData("lastSubscriptionId", subscriptionId);
        agentState.SetData("lastTotalCost", totalCost);
        agentState.SetData("lastPotentialSavings", potentialSavings);
        agentState.SetData("lastOperationTime", DateTime.UtcNow);
        agentState.SetData("lastOperationDurationMs", (int)duration.TotalMilliseconds);

        // Track operation count
        var opCount = agentState.GetData<int>("operationCount");
        agentState.SetData("operationCount", opCount + 1);

        // Track cumulative analyzed spend
        var cumulativeAnalyzed = agentState.GetData<decimal>("cumulativeAnalyzedSpend");
        agentState.SetData("cumulativeAnalyzedSpend", cumulativeAnalyzed + totalCost);

        await _stateManager.SaveAgentStateAsync(conversationId, AgentType, agentState, cancellationToken);
    }

    /// <summary>
    /// Store last analysis results for context continuity.
    /// </summary>
    public async Task StoreLastAnalysisAsync(
        string conversationId,
        CostAnalysisSummary summary,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, LastAnalysisResultsKey, summary, cancellationToken);
    }

    /// <summary>
    /// Get last analysis results for follow-up queries.
    /// </summary>
    public async Task<CostAnalysisSummary?> GetLastAnalysisAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _sharedMemory.GetAsync<CostAnalysisSummary>(
            conversationId, LastAnalysisResultsKey, cancellationToken);
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
/// Cached cost dashboard data.
/// </summary>
public class CostDashboardCache
{
    public string SubscriptionId { get; set; } = string.Empty;
    public int LookbackDays { get; set; }
    public decimal CurrentMonthSpend { get; set; }
    public decimal PreviousMonthSpend { get; set; }
    public decimal ProjectedMonthlySpend { get; set; }
    public decimal AverageDailyCost { get; set; }
    public decimal YearToDateSpend { get; set; }
    public decimal PotentialSavings { get; set; }
    public int OptimizationOpportunities { get; set; }
    public List<ServiceCostBreakdown> ServiceBreakdown { get; set; } = new();
    public List<BudgetAlertSummary> BudgetAlerts { get; set; } = new();
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// Service cost breakdown item.
/// </summary>
public class ServiceCostBreakdown
{
    public string ServiceName { get; set; } = string.Empty;
    public decimal MonthlyCost { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public int ResourceCount { get; set; }
}

/// <summary>
/// Optimization result summary for cross-agent sharing.
/// </summary>
public class OptimizationResultSummary
{
    public string SubscriptionId { get; set; } = string.Empty;
    public decimal TotalMonthlyCost { get; set; }
    public decimal TotalPotentialSavings { get; set; }
    public int RecommendationCount { get; set; }
    public List<OptimizationRecommendation> TopRecommendations { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// Single optimization recommendation.
/// </summary>
public class OptimizationRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialMonthlySavings { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Complexity { get; set; } = "Moderate";
    public string AffectedResourceId { get; set; } = string.Empty;
    public string AffectedResourceType { get; set; } = string.Empty;
}

/// <summary>
/// Budget alert summary.
/// </summary>
public class BudgetAlertSummary
{
    public string BudgetName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal CurrentSpend { get; set; }
    public decimal CurrentPercentage { get; set; }
    public string Severity { get; set; } = "Info";
    public DateTime? AlertTriggeredAt { get; set; }
}

/// <summary>
/// Forecast result summary for cross-agent sharing.
/// </summary>
public class ForecastResultSummary
{
    public string SubscriptionId { get; set; } = string.Empty;
    public int ForecastDays { get; set; }
    public decimal ProjectedTotalCost { get; set; }
    public decimal ProjectedDailyCost { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public string TrendDirection { get; set; } = "Stable";
    public decimal TrendPercentage { get; set; }
    public bool SeasonalityDetected { get; set; }
    public DateTime ForecastGeneratedAt { get; set; }
}

/// <summary>
/// Cost analysis summary for context continuity.
/// </summary>
public class CostAnalysisSummary
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string AnalysisType { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PotentialSavings { get; set; }
    public int ResourcesAnalyzed { get; set; }
    public Dictionary<string, decimal> CostByService { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

#endregion
