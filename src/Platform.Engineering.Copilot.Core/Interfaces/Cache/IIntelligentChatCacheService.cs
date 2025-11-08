namespace Platform.Engineering.Copilot.Core.Interfaces.Cache;

/// <summary>
/// Caching service for intelligent chat responses to reduce AI API calls and rate limiting
/// </summary>
public interface IIntelligentChatCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    string GenerateCacheKey(string prefix, params string[] values);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}