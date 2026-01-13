using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.State.Stores;

/// <summary>
/// Redis implementation of IStateStore using IDistributedCache.
/// Suitable for multi-instance deployments.
/// </summary>
public class RedisStateStore : IStateStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisStateStore> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(4);
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisStateStore(IDistributedCache cache, ILogger<RedisStateStore> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting state for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = expiration ?? _defaultExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
            _logger.LogDebug("Stored state for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting state for key {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed state for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing state for key {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return !string.IsNullOrEmpty(value);
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based key retrieval requires direct Redis connection
        // For IDistributedCache, we would need to track keys separately or use StackExchange.Redis directly
        _logger.LogWarning("GetKeysAsync with pattern is not fully supported in Redis distributed cache mode");
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task ClearAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based clearing requires direct Redis connection
        _logger.LogWarning("ClearAsync with pattern is not fully supported in Redis distributed cache mode");
        return Task.CompletedTask;
    }
}
