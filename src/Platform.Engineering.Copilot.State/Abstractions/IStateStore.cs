namespace Platform.Engineering.Copilot.State.Abstractions;

/// <summary>
/// Abstraction for state storage operations.
/// Supports multiple backends (Memory, Redis, SQL).
/// </summary>
public interface IStateStore
{
    /// <summary>
    /// Get a value from the state store.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set a value in the state store.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove a value from the state store.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists in the state store.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all keys matching a pattern.
    /// </summary>
    Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all keys matching a pattern.
    /// </summary>
    Task ClearAsync(string pattern, CancellationToken cancellationToken = default);
}
