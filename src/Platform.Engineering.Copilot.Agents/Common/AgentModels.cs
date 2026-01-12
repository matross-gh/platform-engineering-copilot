using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.Agents.Common;

/// <summary>
/// Agent response model
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// Unique identifier of the agent
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the agent that produced this response
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Tools that were executed
    /// </summary>
    public List<ToolExecutionResult> ToolsExecuted { get; set; } = new();

    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Error messages if any
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether this agent needs to hand off to another agent
    /// </summary>
    public bool RequiresHandoff { get; set; }

    /// <summary>
    /// Name of agent to hand off to (if RequiresHandoff is true)
    /// </summary>
    public string? HandoffTarget { get; set; }

    /// <summary>
    /// Reason for handoff
    /// </summary>
    public string? HandoffReason { get; set; }
}

/// <summary>
/// Result of a tool execution
/// </summary>
public class ToolExecutionResult
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public double ExecutionTimeMs { get; set; }
}

/// <summary>
/// Conversation context passed between agents
/// </summary>
public class AgentConversationContext
{
    /// <summary>
    /// Unique conversation identifier
    /// </summary>
    public string ConversationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Current Azure subscription ID (if set)
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// User identifier
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Conversation message history
    /// </summary>
    public List<ConversationMessage> MessageHistory { get; set; } = new();

    /// <summary>
    /// Workflow state dictionary
    /// </summary>
    public Dictionary<string, object> WorkflowState { get; set; } = new();

    /// <summary>
    /// Previous agent responses in this conversation
    /// </summary>
    public List<AgentResponse> PreviousResponses { get; set; } = new();

    /// <summary>
    /// Add a message to history
    /// </summary>
    public void AddMessage(string content, bool isUser, string? agentName = null)
    {
        MessageHistory.Add(new ConversationMessage
        {
            Content = content,
            IsUser = isUser,
            AgentName = agentName,
            Timestamp = DateTime.UtcNow
        });
    }
}
