using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.State.Stores;

/// <summary>
/// In-memory implementation of IStateStore using IMemoryCache.
/// Suitable for single-instance deployments or development.
/// </summary>
public class MemoryStateStore : IStateStore
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryStateStore> _logger;
    private readonly ConcurrentDictionary<string, byte> _keys = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(4);

    public MemoryStateStore(IMemoryCache cache, ILogger<MemoryStateStore> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached is T typedValue)
            {
                return Task.FromResult<T?>(typedValue);
            }

            // Handle serialized JSON
            if (cached is string json)
            {
                try
                {
                    var deserialized = JsonSerializer.Deserialize<T>(json);
                    return Task.FromResult(deserialized);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize state for key {Key}", key);
                }
            }
        }

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration ?? _defaultExpiration
        };

        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _keys.TryRemove(evictedKey.ToString()!, out _);
        });

        _cache.Set(key, value, options);
        _keys.TryAdd(key, 0);

        _logger.LogDebug("Stored state for key {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _cache.Remove(key);
        _keys.TryRemove(key, out _);

        _logger.LogDebug("Removed state for key {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

        var matchingKeys = _keys.Keys.Where(k => regex.IsMatch(k));
        return Task.FromResult(matchingKeys);
    }

    public async Task ClearAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var keys = await GetKeysAsync(pattern, cancellationToken);
        foreach (var key in keys)
        {
            await RemoveAsync(key, cancellationToken);
        }

        _logger.LogDebug("Cleared state matching pattern {Pattern}", pattern);
    }
}
