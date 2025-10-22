namespace Platform.Engineering.Copilot.Core.Models.Agents;

/// <summary>
/// Defines how agents should be executed
/// </summary>
public enum ExecutionPattern
{
    /// <summary>
    /// Execute agents one after another (tasks have dependencies)
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Execute agents simultaneously (independent tasks)
    /// </summary>
    Parallel,
    
    /// <summary>
    /// Agents collaborate and iterate (refinement needed)
    /// </summary>
    Collaborative
}
