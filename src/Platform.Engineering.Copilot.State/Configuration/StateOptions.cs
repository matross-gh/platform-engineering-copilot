namespace Platform.Engineering.Copilot.State.Configuration;

/// <summary>
/// Configuration options for state management.
/// </summary>
public class StateOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "StateManagement";

    /// <summary>
    /// State store provider: Memory, Redis, Sql
    /// </summary>
    public StateProvider Provider { get; set; } = StateProvider.Memory;

    /// <summary>
    /// Redis connection string (when Provider is Redis).
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Redis instance name (when Provider is Redis).
    /// </summary>
    public string RedisInstanceName { get; set; } = "platform-copilot:";

    /// <summary>
    /// Default expiration for state entries.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Maximum messages to keep in conversation history.
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 100;

    /// <summary>
    /// Maximum events to keep in shared memory.
    /// </summary>
    public int MaxSharedMemoryEvents { get; set; } = 100;

    /// <summary>
    /// Enable state persistence to database.
    /// </summary>
    public bool EnablePersistence { get; set; } = false;
}

/// <summary>
/// State storage provider.
/// </summary>
public enum StateProvider
{
    /// <summary>
    /// In-memory state (single instance only)
    /// </summary>
    Memory,

    /// <summary>
    /// Redis distributed cache
    /// </summary>
    Redis,

    /// <summary>
    /// SQL database persistence
    /// </summary>
    Sql
}
