namespace Platform.Engineering.Copilot.Mcp.Models;

/// <summary>
/// MCP chat response model for HTTP bridge responses.
/// Maps from AgentResponse to MCP-compatible format.
/// </summary>
public class McpChatResponse
{
    /// <summary>
    /// Whether the request was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response content from the agent(s)
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Conversation ID for maintaining context
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the primary agent that handled the request
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public double ProcessingTimeMs { get; set; }

    /// <summary>
    /// Tools that were executed during processing
    /// </summary>
    public List<ToolExecution> ToolsExecuted { get; set; } = new();

    /// <summary>
    /// Error messages if any
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Follow-up suggestions for the user
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Whether additional follow-up is required
    /// </summary>
    public bool RequiresFollowUp { get; set; }

    /// <summary>
    /// Additional metadata from agent processing
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Tool execution information
/// </summary>
public class ToolExecution
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public double ExecutionTimeMs { get; set; }
}
