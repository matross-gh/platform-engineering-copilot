namespace Platform.Engineering.Copilot.Core.Models.Agents;

/// <summary>
/// Final orchestrated response from the multi-agent system
/// </summary>
public class OrchestratedResponse
{
    /// <summary>
    /// The synthesized final response to the user
    /// </summary>
    public string FinalResponse { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary intent that was processed
    /// </summary>
    public string PrimaryIntent { get; set; } = string.Empty;
    
    /// <summary>
    /// List of agents that were invoked
    /// </summary>
    public List<AgentType> AgentsInvoked { get; set; } = new();
    
    /// <summary>
    /// Execution pattern that was used
    /// </summary>
    public ExecutionPattern ExecutionPattern { get; set; }
    
    /// <summary>
    /// Total number of agent calls made
    /// </summary>
    public int TotalAgentCalls { get; set; }
    
    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public double ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Whether the overall execution was successful
    /// </summary>
    public bool Success { get; set; } = true;
    
    /// <summary>
    /// Whether a follow-up question is needed
    /// </summary>
    public bool RequiresFollowUp { get; set; }
    
    /// <summary>
    /// Follow-up prompt if needed
    /// </summary>
    public string? FollowUpPrompt { get; set; }
    
    /// <summary>
    /// Fields that are missing from the request
    /// </summary>
    public List<string> MissingFields { get; set; } = new();
    
    /// <summary>
    /// Quick reply suggestions
    /// </summary>
    public List<string> QuickReplies { get; set; } = new();
    
    /// <summary>
    /// Combined metadata from all agents
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Any errors from agent execution
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
