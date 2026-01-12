namespace Platform.Engineering.Copilot.Channels.Abstractions;

/// <summary>
/// Abstraction for a communication channel.
/// Channels handle message routing between clients and agents.
/// </summary>
public interface IChannel
{
    /// <summary>
    /// Channel identifier.
    /// </summary>
    string ChannelId { get; }

    /// <summary>
    /// Channel type (e.g., SignalR, WebSocket, Polling).
    /// </summary>
    ChannelType Type { get; }

    /// <summary>
    /// Send a message to a specific connection.
    /// </summary>
    Task SendAsync(string connectionId, ChannelMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to all connections in a conversation.
    /// </summary>
    Task SendToConversationAsync(string conversationId, ChannelMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast a message to all connected clients.
    /// </summary>
    Task BroadcastAsync(ChannelMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a connection is active.
    /// </summary>
    Task<bool> IsConnectedAsync(string connectionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Channel types.
/// </summary>
public enum ChannelType
{
    SignalR,
    WebSocket,
    LongPolling,
    ServerSentEvents
}

/// <summary>
/// Message sent through a channel.
/// </summary>
public class ChannelMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.AgentResponse;
    public string Content { get; set; } = string.Empty;
    public string? AgentType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// For streaming responses - indicates if more chunks are coming.
    /// </summary>
    public bool IsStreaming { get; set; }
    
    /// <summary>
    /// For streaming responses - indicates this is the final chunk.
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// Message types.
/// </summary>
public enum MessageType
{
    UserMessage,
    AgentResponse,
    AgentThinking,
    ToolExecution,
    ToolResult,
    Error,
    SystemNotification,
    ProgressUpdate,
    ConfirmationRequest,
    StreamChunk
}
