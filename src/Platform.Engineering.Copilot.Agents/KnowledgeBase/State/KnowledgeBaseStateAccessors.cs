using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.State.Abstractions;
using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.State;

/// <summary>
/// State accessors for Knowledge Base Agent, providing typed access to knowledge-related state.
/// Tracks knowledge base queries, cached results, and conversation context.
/// </summary>
public class KnowledgeBaseStateAccessors
{
    private readonly IAgentStateManager _stateManager;
    private readonly ISharedMemory _sharedMemory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KnowledgeBaseStateAccessors> _logger;

    private const string AgentType = "knowledgebase";
    private const string LastQueryKey = "last_query";
    private const string QueryHistoryKey = "query_history";
    private const string CachedControlsPrefix = "cached_control";
    private const string CachedStigsPrefix = "cached_stig";

    public KnowledgeBaseStateAccessors(
        IAgentStateManager stateManager,
        ISharedMemory sharedMemory,
        IMemoryCache cache,
        ILogger<KnowledgeBaseStateAccessors> logger)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _sharedMemory = sharedMemory ?? throw new ArgumentNullException(nameof(sharedMemory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the last knowledge base query for a conversation.
    /// </summary>
    public async Task<string?> GetLastQueryAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        var query = await _sharedMemory.GetAsync<QueryContext>(
            conversationId, LastQueryKey, cancellationToken);
        return query?.Query;
    }

    /// <summary>
    /// Set the last knowledge base query for a conversation.
    /// </summary>
    public async Task SetLastQueryAsync(
        string conversationId,
        string query,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var context = new QueryContext
        {
            Query = query,
            Category = category,
            QueriedAt = DateTime.UtcNow
        };

        await _sharedMemory.SetAsync(conversationId, LastQueryKey, context, cancellationToken);
        _logger.LogDebug("Set last query for {ConversationId}: {Query}", conversationId, query);
    }

    /// <summary>
    /// Get cached NIST control information.
    /// </summary>
    public CachedControlInfo? GetCachedControl(string controlId)
    {
        var cacheKey = $"{CachedControlsPrefix}:{controlId.ToUpperInvariant()}";
        if (_cache.TryGetValue<CachedControlInfo>(cacheKey, out var cached))
        {
            return cached;
        }
        return null;
    }

    /// <summary>
    /// Cache NIST control information.
    /// </summary>
    public void CacheControl(string controlId, string explanation, TimeSpan? expiration = null)
    {
        var cacheKey = $"{CachedControlsPrefix}:{controlId.ToUpperInvariant()}";
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(24)
        };

        var cached = new CachedControlInfo
        {
            ControlId = controlId.ToUpperInvariant(),
            Explanation = explanation,
            CachedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, cached, cacheOptions);
        _logger.LogDebug("Cached control {ControlId}", controlId);
    }

    /// <summary>
    /// Get cached STIG control information.
    /// </summary>
    public CachedStigInfo? GetCachedStig(string stigId)
    {
        var cacheKey = $"{CachedStigsPrefix}:{stigId.ToUpperInvariant()}";
        if (_cache.TryGetValue<CachedStigInfo>(cacheKey, out var cached))
        {
            return cached;
        }
        return null;
    }

    /// <summary>
    /// Cache STIG control information.
    /// </summary>
    public void CacheStig(string stigId, string explanation, TimeSpan? expiration = null)
    {
        var cacheKey = $"{CachedStigsPrefix}:{stigId.ToUpperInvariant()}";
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(24)
        };

        var cached = new CachedStigInfo
        {
            StigId = stigId.ToUpperInvariant(),
            Explanation = explanation,
            CachedAt = DateTime.UtcNow
        };

        _cache.Set(cacheKey, cached, cacheOptions);
        _logger.LogDebug("Cached STIG {StigId}", stigId);
    }

    /// <summary>
    /// Track a knowledge base operation in state.
    /// </summary>
    public async Task TrackKnowledgeBaseOperationAsync(
        string conversationId,
        string operationType,
        string query,
        bool success,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var state = await _stateManager.GetAgentStateAsync(conversationId, AgentType, cancellationToken);

        // Track operation in metadata
        state.Metadata["last_operation"] = operationType;
        state.Metadata["last_operation_at"] = DateTime.UtcNow.ToString("O");
        var currentCount = state.Metadata.TryGetValue("operation_count", out var countStr) && int.TryParse(countStr, out var count) ? count : 0;
        state.Metadata["operation_count"] = (currentCount + 1).ToString();
        state.Metadata["last_query"] = query;
        state.Metadata["last_query_success"] = success.ToString();
        state.Metadata["last_query_duration_ms"] = ((int)duration.TotalMilliseconds).ToString();

        await _stateManager.SaveAgentStateAsync(conversationId, AgentType, state, cancellationToken);
        _logger.LogDebug("Tracked knowledge base operation: {OperationType} for {ConversationId}",
            operationType, conversationId);
    }

    /// <summary>
    /// Share knowledge base findings with other agents.
    /// </summary>
    public async Task ShareKnowledgeAsync(
        string conversationId,
        string key,
        object value,
        CancellationToken cancellationToken = default)
    {
        await _sharedMemory.SetAsync(conversationId, $"kb_{key}", value, cancellationToken);
        _logger.LogDebug("Shared knowledge {Key} for {ConversationId}", key, conversationId);
    }
}

/// <summary>
/// Query context for tracking knowledge base queries.
/// </summary>
public class QueryContext
{
    public string Query { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime QueriedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cached NIST control information.
/// </summary>
public class CachedControlInfo
{
    public string ControlId { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cached STIG control information.
/// </summary>
public class CachedStigInfo
{
    public string StigId { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
