using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.State.Services;

/// <summary>
/// Default implementation of ISharedMemory.
/// </summary>
public class SharedMemory : ISharedMemory
{
    private readonly IStateStore _stateStore;
    private readonly ILogger<SharedMemory> _logger;
    private const string DataKeyPrefix = "shared:";
    private const string EventKeyPrefix = "events:";
    private const int MaxEvents = 100;

    public SharedMemory(IStateStore stateStore, ILogger<SharedMemory> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SetAsync<T>(string conversationId, string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        var storeKey = GetDataKey(conversationId, key);
        await _stateStore.SetAsync(storeKey, value, cancellationToken: cancellationToken);
        _logger.LogDebug("Set shared memory {Key} for conversation {ConversationId}", key, conversationId);
    }

    public async Task<T?> GetAsync<T>(string conversationId, string key, CancellationToken cancellationToken = default) where T : class
    {
        var storeKey = GetDataKey(conversationId, key);
        return await _stateStore.GetAsync<T>(storeKey, cancellationToken);
    }

    public async Task RemoveAsync(string conversationId, string key, CancellationToken cancellationToken = default)
    {
        var storeKey = GetDataKey(conversationId, key);
        await _stateStore.RemoveAsync(storeKey, cancellationToken);
        _logger.LogDebug("Removed shared memory {Key} for conversation {ConversationId}", key, conversationId);
    }

    public async Task<bool> ExistsAsync(string conversationId, string key, CancellationToken cancellationToken = default)
    {
        var storeKey = GetDataKey(conversationId, key);
        return await _stateStore.ExistsAsync(storeKey, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var pattern = $"{DataKeyPrefix}{conversationId}:*";
        var keys = await _stateStore.GetKeysAsync(pattern, cancellationToken);
        
        // Strip the prefix to return just the key names
        var prefix = $"{DataKeyPrefix}{conversationId}:";
        return keys.Select(k => k.StartsWith(prefix) ? k[prefix.Length..] : k);
    }

    public async Task ClearAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var dataPattern = $"{DataKeyPrefix}{conversationId}:*";
        var eventPattern = $"{EventKeyPrefix}{conversationId}";

        await _stateStore.ClearAsync(dataPattern, cancellationToken);
        await _stateStore.RemoveAsync(eventPattern, cancellationToken);

        _logger.LogDebug("Cleared all shared memory for conversation {ConversationId}", conversationId);
    }

    public async Task PublishEventAsync(string conversationId, string eventType, object data, CancellationToken cancellationToken = default)
    {
        var eventKey = GetEventKey(conversationId);
        var events = await _stateStore.GetAsync<List<SharedMemoryEvent>>(eventKey, cancellationToken) 
                     ?? new List<SharedMemoryEvent>();

        var newEvent = new SharedMemoryEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        events.Add(newEvent);

        // Keep only the most recent events
        if (events.Count > MaxEvents)
        {
            events = events.OrderByDescending(e => e.Timestamp).Take(MaxEvents).ToList();
        }

        await _stateStore.SetAsync(eventKey, events, cancellationToken: cancellationToken);
        _logger.LogDebug("Published event {EventType} for conversation {ConversationId}", eventType, conversationId);
    }

    public async Task<IEnumerable<SharedMemoryEvent>> GetEventsAsync(string conversationId, int maxEvents = 50, CancellationToken cancellationToken = default)
    {
        var eventKey = GetEventKey(conversationId);
        var events = await _stateStore.GetAsync<List<SharedMemoryEvent>>(eventKey, cancellationToken) 
                     ?? new List<SharedMemoryEvent>();

        return events.OrderByDescending(e => e.Timestamp).Take(maxEvents);
    }

    private static string GetDataKey(string conversationId, string key) => $"{DataKeyPrefix}{conversationId}:{key}";
    private static string GetEventKey(string conversationId) => $"{EventKeyPrefix}{conversationId}";
}
