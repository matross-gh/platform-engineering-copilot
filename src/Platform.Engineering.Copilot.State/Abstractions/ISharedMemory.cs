namespace Platform.Engineering.Copilot.State.Abstractions;

/// <summary>
/// Shared memory for cross-agent communication within a conversation.
/// Provides a way for agents to share data without direct coupling.
/// </summary>
public interface ISharedMemory
{
    /// <summary>
    /// Store a value in shared memory.
    /// </summary>
    Task SetAsync<T>(string conversationId, string key, T value, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Get a value from shared memory.
    /// </summary>
    Task<T?> GetAsync<T>(string conversationId, string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove a value from shared memory.
    /// </summary>
    Task RemoveAsync(string conversationId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists in shared memory.
    /// </summary>
    Task<bool> ExistsAsync(string conversationId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all keys for a conversation.
    /// </summary>
    Task<IEnumerable<string>> GetKeysAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all shared memory for a conversation.
    /// </summary>
    Task ClearAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish an event to shared memory (for agent coordination).
    /// </summary>
    Task PublishEventAsync(string conversationId, string eventType, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent events for a conversation.
    /// </summary>
    Task<IEnumerable<SharedMemoryEvent>> GetEventsAsync(string conversationId, int maxEvents = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event stored in shared memory.
/// </summary>
public class SharedMemoryEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? SourceAgent { get; set; }
}
