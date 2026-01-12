namespace Platform.Engineering.Copilot.State.Models;

/// <summary>
/// Represents the state of a conversation.
/// </summary>
public class ConversationState
{
    /// <summary>
    /// Unique conversation identifier.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// User identifier associated with this conversation.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Timestamp when the conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of the last activity.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Conversation status.
    /// </summary>
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;

    /// <summary>
    /// Message history.
    /// </summary>
    public List<ConversationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Conversation-level variables.
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Active Azure context.
    /// </summary>
    public AzureContext? AzureContext { get; set; }

    /// <summary>
    /// Currently active agent type.
    /// </summary>
    public string? ActiveAgentType { get; set; }

    /// <summary>
    /// Conversation metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Update last activity timestamp.
    /// </summary>
    public void Touch()
    {
        LastActivityAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Conversation status.
/// </summary>
public enum ConversationStatus
{
    Active,
    Paused,
    Completed,
    Expired,
    Error
}

/// <summary>
/// A message in the conversation.
/// </summary>
public class ConversationMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? AgentType { get; set; }
    public string? ToolName { get; set; }
    public object? ToolResult { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Convenience property - true if Role is User.
    /// </summary>
    public bool IsUser
    {
        get => Role == MessageRole.User;
        set => Role = value ? MessageRole.User : MessageRole.Assistant;
    }

    /// <summary>
    /// Alias for AgentType for compatibility.
    /// </summary>
    public string? AgentName
    {
        get => AgentType;
        set => AgentType = value;
    }
}

/// <summary>
/// Message role.
/// </summary>
public enum MessageRole
{
    User,
    Assistant,
    System,
    Tool
}

/// <summary>
/// Azure context for a conversation.
/// </summary>
public class AzureContext
{
    public string? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public string? TenantId { get; set; }
    public string? ResourceGroup { get; set; }
    public string? Region { get; set; }
    public string? CloudEnvironment { get; set; } = "AzureUSGovernment";
}

/// <summary>
/// Summary of a conversation for listing.
/// </summary>
public class ConversationSummary
{
    public string ConversationId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public ConversationStatus Status { get; set; }
    public int MessageCount { get; set; }
    public string? LastAgentType { get; set; }
}
