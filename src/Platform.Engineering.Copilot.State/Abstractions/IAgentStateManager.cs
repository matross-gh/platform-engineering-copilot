using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.State.Abstractions;

/// <summary>
/// Manages agent-specific state (per agent type, per conversation).
/// </summary>
public interface IAgentStateManager
{
    /// <summary>
    /// Get agent state for a specific agent in a conversation.
    /// </summary>
    Task<AgentState> GetAgentStateAsync(string conversationId, string agentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save agent state.
    /// </summary>
    Task SaveAgentStateAsync(string conversationId, string agentType, AgentState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear agent state.
    /// </summary>
    Task ClearAgentStateAsync(string conversationId, string agentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all agent states for a conversation.
    /// </summary>
    Task<Dictionary<string, AgentState>> GetAllAgentStatesAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a tool result in agent state.
    /// </summary>
    Task SetToolResultAsync(string conversationId, string agentType, string toolName, object result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a tool result from agent state.
    /// </summary>
    Task<T?> GetToolResultAsync<T>(string conversationId, string agentType, string toolName, CancellationToken cancellationToken = default);
}
