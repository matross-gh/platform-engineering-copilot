using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Channels.Abstractions;

namespace Platform.Engineering.Copilot.Channels.Services;

/// <summary>
/// In-memory channel implementation (for single-instance or testing).
/// </summary>
public class InMemoryChannel : IChannel
{
    private readonly ILogger<InMemoryChannel> _logger;
    private readonly Dictionary<string, HashSet<string>> _conversationConnections = new();
    private readonly HashSet<string> _activeConnections = new();
    private readonly object _lock = new();

    // Event for testing/debugging - allows subscribing to messages
    public event Func<string, ChannelMessage, Task>? OnMessageSent;

    public string ChannelId { get; } = Guid.NewGuid().ToString();
    public ChannelType Type => ChannelType.LongPolling;

    public InMemoryChannel(ILogger<InMemoryChannel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendAsync(string connectionId, ChannelMessage message, CancellationToken cancellationToken = default)
    {
        if (!_activeConnections.Contains(connectionId))
        {
            _logger.LogWarning("Attempted to send to inactive connection {ConnectionId}", connectionId);
            return;
        }

        _logger.LogDebug("Sending message to connection {ConnectionId}: {MessageType}", connectionId, message.Type);
        
        if (OnMessageSent != null)
        {
            await OnMessageSent.Invoke(connectionId, message);
        }
    }

    public async Task SendToConversationAsync(string conversationId, ChannelMessage message, CancellationToken cancellationToken = default)
    {
        List<string> connections;
        lock (_lock)
        {
            if (!_conversationConnections.TryGetValue(conversationId, out var set))
            {
                _logger.LogDebug("No connections for conversation {ConversationId}", conversationId);
                return;
            }
            connections = set.ToList();
        }

        foreach (var connectionId in connections)
        {
            await SendAsync(connectionId, message, cancellationToken);
        }
    }

    public async Task BroadcastAsync(ChannelMessage message, CancellationToken cancellationToken = default)
    {
        List<string> connections;
        lock (_lock)
        {
            connections = _activeConnections.ToList();
        }

        foreach (var connectionId in connections)
        {
            await SendAsync(connectionId, message, cancellationToken);
        }
    }

    public Task<bool> IsConnectedAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_activeConnections.Contains(connectionId));
    }

    /// <summary>
    /// Register a connection (for testing).
    /// </summary>
    public void RegisterConnection(string connectionId)
    {
        lock (_lock)
        {
            _activeConnections.Add(connectionId);
        }
    }

    /// <summary>
    /// Unregister a connection (for testing).
    /// </summary>
    public void UnregisterConnection(string connectionId)
    {
        lock (_lock)
        {
            _activeConnections.Remove(connectionId);
            foreach (var connections in _conversationConnections.Values)
            {
                connections.Remove(connectionId);
            }
        }
    }

    /// <summary>
    /// Add connection to conversation (for testing).
    /// </summary>
    public void JoinConversation(string connectionId, string conversationId)
    {
        lock (_lock)
        {
            if (!_conversationConnections.TryGetValue(conversationId, out var set))
            {
                set = new HashSet<string>();
                _conversationConnections[conversationId] = set;
            }
            set.Add(connectionId);
        }
    }
}
