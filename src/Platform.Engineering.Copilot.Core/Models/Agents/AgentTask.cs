namespace Platform.Engineering.Copilot.Core.Models.Agents;

/// <summary>
/// Represents a task to be processed by a specialized agent
/// </summary>
public class AgentTask
{
    /// <summary>
    /// Unique identifier for the task
    /// </summary>
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The agent type that should process this task
    /// </summary>
    public AgentType AgentType { get; set; }
    
    /// <summary>
    /// Description of the task to be performed
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional parameters for task execution
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// Whether this task is critical for the overall execution
    /// </summary>
    public bool IsCritical { get; set; } = false;
    
    /// <summary>
    /// Priority level (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Conversation ID for context tracking
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
}
