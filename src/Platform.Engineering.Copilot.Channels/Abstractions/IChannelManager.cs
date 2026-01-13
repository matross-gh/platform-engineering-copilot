namespace Platform.Engineering.Copilot.Channels.Abstractions;

/// <summary>
/// Manages channel routing and connection tracking.
/// </summary>
public interface IChannelManager
{
    /// <summary>
    /// Register a new connection.
    /// </summary>
    Task<ConnectionInfo> RegisterConnectionAsync(string connectionId, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregister a connection.
    /// </summary>
    Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Associate a connection with a conversation.
    /// </summary>
    Task JoinConversationAsync(string connectionId, string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disassociate a connection from a conversation.
    /// </summary>
    Task LeaveConversationAsync(string connectionId, string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connections for a conversation.
    /// </summary>
    Task<IEnumerable<string>> GetConversationConnectionsAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connection info.
    /// </summary>
    Task<ConnectionInfo?> GetConnectionInfoAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the channel for sending messages.
    /// </summary>
    IChannel GetChannel();

    /// <summary>
    /// Send a message to a conversation.
    /// </summary>
    Task SendToConversationAsync(string conversationId, ChannelMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to a specific connection.
    /// </summary>
    Task SendToConnectionAsync(string connectionId, ChannelMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Connection information.
/// </summary>
public class ConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public List<string> ConversationIds { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
