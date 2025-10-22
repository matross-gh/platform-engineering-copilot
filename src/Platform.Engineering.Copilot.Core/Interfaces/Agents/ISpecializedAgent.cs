using Platform.Engineering.Copilot.Core.Models.Agents;
using Platform.Engineering.Copilot.Core.Services.Agents;

namespace Platform.Engineering.Copilot.Core.Interfaces.Agents;

/// <summary>
/// Interface for specialized agents in the multi-agent system
/// </summary>
public interface ISpecializedAgent
{
    /// <summary>
    /// The type of agent
    /// </summary>
    AgentType AgentType { get; }
    
    /// <summary>
    /// Process a task assigned to this agent
    /// </summary>
    /// <param name="task">The task to process</param>
    /// <param name="memory">Shared memory for agent communication</param>
    /// <returns>Agent response with results</returns>
    Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory);
}
