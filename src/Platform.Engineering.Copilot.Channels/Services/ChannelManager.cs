using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Channels.Abstractions;

namespace Platform.Engineering.Copilot.Channels.Services;

/// <summary>
/// Default implementation of IChannelManager.
/// </summary>
public class ChannelManager : IChannelManager
{
    private readonly IChannel _channel;
    private readonly ILogger<ChannelManager> _logger;
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _conversationConnections = new();
    private readonly object _lock = new();

    public ChannelManager(IChannel channel, ILogger<ChannelManager> logger)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ConnectionInfo> RegisterConnectionAsync(string connectionId, string? userId = null, CancellationToken cancellationToken = default)
    {
        var connectionInfo = new ConnectionInfo
        {
            ConnectionId = connectionId,
            UserId = userId,
            ConnectedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _connections[connectionId] = connectionInfo;
        _logger.LogInformation("Connection registered: {ConnectionId}, User: {UserId}", connectionId, userId);

        return Task.FromResult(connectionInfo);
    }

    public Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (_connections.TryRemove(connectionId, out var connectionInfo))
        {
            // Remove from all conversations
            foreach (var conversationId in connectionInfo.ConversationIds.ToList())
            {
                RemoveFromConversation(connectionId, conversationId);
            }

            _logger.LogInformation("Connection unregistered: {ConnectionId}", connectionId);
        }

        return Task.CompletedTask;
    }

    public Task JoinConversationAsync(string connectionId, string conversationId, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            lock (_lock)
            {
                if (!connectionInfo.ConversationIds.Contains(conversationId))
                {
                    connectionInfo.ConversationIds.Add(conversationId);
                }

                _conversationConnections.AddOrUpdate(
                    conversationId,
                    _ => new HashSet<string> { connectionId },
                    (_, set) =>
                    {
                        set.Add(connectionId);
                        return set;
                    });
            }

            connectionInfo.LastActivityAt = DateTime.UtcNow;
            _logger.LogDebug("Connection {ConnectionId} joined conversation {ConversationId}", connectionId, conversationId);
        }

        return Task.CompletedTask;
    }

    public Task LeaveConversationAsync(string connectionId, string conversationId, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            RemoveFromConversation(connectionId, conversationId);
            connectionInfo.ConversationIds.Remove(conversationId);
            connectionInfo.LastActivityAt = DateTime.UtcNow;
            _logger.LogDebug("Connection {ConnectionId} left conversation {ConversationId}", connectionId, conversationId);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetConversationConnectionsAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (_conversationConnections.TryGetValue(conversationId, out var connections))
        {
            return Task.FromResult<IEnumerable<string>>(connections.ToList());
        }

        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    public Task<ConnectionInfo?> GetConnectionInfoAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        _connections.TryGetValue(connectionId, out var connectionInfo);
        return Task.FromResult(connectionInfo);
    }

    public IChannel GetChannel() => _channel;

    public async Task SendToConversationAsync(string conversationId, ChannelMessage message, CancellationToken cancellationToken = default)
    {
        await _channel.SendToConversationAsync(conversationId, message, cancellationToken);
    }

    public async Task SendToConnectionAsync(string connectionId, ChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(connectionId, out var connectionInfo))
        {
            connectionInfo.LastActivityAt = DateTime.UtcNow;
        }

        await _channel.SendAsync(connectionId, message, cancellationToken);
    }

    private void RemoveFromConversation(string connectionId, string conversationId)
    {
        lock (_lock)
        {
            if (_conversationConnections.TryGetValue(conversationId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    _conversationConnections.TryRemove(conversationId, out _);
                }
            }
        }
    }
}
