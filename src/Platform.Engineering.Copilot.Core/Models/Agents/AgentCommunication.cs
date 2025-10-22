namespace Platform.Engineering.Copilot.Core.Models.Agents;

/// <summary>
/// Communication between agents in the multi-agent system
/// </summary>
public class AgentCommunication
{
    /// <summary>
    /// Timestamp of the communication
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The agent sending the message
    /// </summary>
    public AgentType FromAgent { get; set; }
    
    /// <summary>
    /// The agent receiving the message (can be null for broadcast)
    /// </summary>
    public AgentType? ToAgent { get; set; }
    
    /// <summary>
    /// The communication message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional data payload
    /// </summary>
    public object? Data { get; set; }
}
