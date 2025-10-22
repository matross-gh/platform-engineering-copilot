namespace Platform.Engineering.Copilot.Core.Models.Agents;

/// <summary>
/// Represents the response from a specialized agent
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// The task ID this response is for
    /// </summary>
    public string TaskId { get; set; } = string.Empty;
    
    /// <summary>
    /// The agent type that generated this response
    /// </summary>
    public AgentType AgentType { get; set; }
    
    /// <summary>
    /// The main content/result from the agent
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the agent successfully completed the task
    /// </summary>
    public bool Success { get; set; } = true;
    
    /// <summary>
    /// Additional metadata from the agent execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Any errors encountered during execution
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Any warnings generated during execution
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// Agent execution time in milliseconds
    /// </summary>
    public double ExecutionTimeMs { get; set; }
    
    // Agent-specific properties
    
    /// <summary>
    /// For compliance agent: whether the compliance check passed
    /// </summary>
    public bool? IsApproved { get; set; }
    
    /// <summary>
    /// For cost agent: estimated monthly cost
    /// </summary>
    public decimal? EstimatedCost { get; set; }
    
    /// <summary>
    /// For cost agent: whether cost is within budget
    /// </summary>
    public bool? IsWithinBudget { get; set; }
    
    /// <summary>
    /// For compliance agent: overall compliance score (0-100)
    /// </summary>
    public int? ComplianceScore { get; set; }
    
    /// <summary>
    /// For infrastructure agent: Azure resource ID created/modified
    /// </summary>
    public string? ResourceId { get; set; }
    
    /// <summary>
    /// For deployment agent: deployment ID
    /// </summary>
    public string? DeploymentId { get; set; }
    
    /// <summary>
    /// Tools/functions that were invoked by the agent
    /// </summary>
    public List<string> ToolsInvoked { get; set; } = new();
}
