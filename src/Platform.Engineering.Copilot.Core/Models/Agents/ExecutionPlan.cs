namespace Platform.Engineering.Copilot.Core.Models.Agents;

/// <summary>
/// Execution plan created by the orchestrator
/// </summary>
public class ExecutionPlan
{
    /// <summary>
    /// Primary intent determined from user message
    /// </summary>
    public string PrimaryIntent { get; set; } = string.Empty;
    
    /// <summary>
    /// List of tasks to execute
    /// </summary>
    public List<AgentTask> Tasks { get; set; } = new();
    
    /// <summary>
    /// How to execute the tasks
    /// </summary>
    public ExecutionPattern ExecutionPattern { get; set; } = ExecutionPattern.Sequential;
    
    /// <summary>
    /// Estimated execution time in seconds
    /// </summary>
    public int EstimatedTimeSeconds { get; set; }
    
    /// <summary>
    /// Additional notes about the plan
    /// </summary>
    public string? Notes { get; set; }
}
