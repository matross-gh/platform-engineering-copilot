namespace Platform.Engineering.Copilot.Channels.Abstractions;

/// <summary>
/// Handles incoming messages from channels.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Handle an incoming message.
    /// </summary>
    Task<ChannelMessage> HandleAsync(IncomingMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Incoming message from a channel.
/// </summary>
public class IncomingMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string ConnectionId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Optional: Specify which agent should handle this message.
    /// </summary>
    public string? TargetAgentType { get; set; }
    
    /// <summary>
    /// Optional: Attachments (file paths, URLs, etc.)
    /// </summary>
    public List<MessageAttachment> Attachments { get; set; } = new();
}

/// <summary>
/// Attachment to a message.
/// </summary>
public class MessageAttachment
{
    public string AttachmentId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? Url { get; set; }
    public byte[]? Data { get; set; }
    public long Size { get; set; }
}
