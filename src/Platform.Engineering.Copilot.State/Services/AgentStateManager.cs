using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.State.Abstractions;
using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.State.Services;

/// <summary>
/// Default implementation of IAgentStateManager.
/// </summary>
public class AgentStateManager : IAgentStateManager
{
    private readonly IStateStore _stateStore;
    private readonly ILogger<AgentStateManager> _logger;
    private const string KeyPrefix = "agent:";

    public AgentStateManager(IStateStore stateStore, ILogger<AgentStateManager> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentState> GetAgentStateAsync(string conversationId, string agentType, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId, agentType);
        var state = await _stateStore.GetAsync<AgentState>(key, cancellationToken);

        if (state == null)
        {
            state = new AgentState
            {
                ConversationId = conversationId,
                AgentType = agentType,
                LastActivityAt = DateTime.UtcNow
            };
        }

        return state;
    }

    public async Task SaveAgentStateAsync(string conversationId, string agentType, AgentState state, CancellationToken cancellationToken = default)
    {
        state.LastActivityAt = DateTime.UtcNow;
        var key = GetKey(conversationId, agentType);
        await _stateStore.SetAsync(key, state, cancellationToken: cancellationToken);
        _logger.LogDebug("Saved agent state for {AgentType} in conversation {ConversationId}", agentType, conversationId);
    }

    public async Task ClearAgentStateAsync(string conversationId, string agentType, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId, agentType);
        await _stateStore.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("Cleared agent state for {AgentType} in conversation {ConversationId}", agentType, conversationId);
    }

    public async Task<Dictionary<string, AgentState>> GetAllAgentStatesAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var pattern = $"{KeyPrefix}{conversationId}:*";
        var keys = await _stateStore.GetKeysAsync(pattern, cancellationToken);
        var states = new Dictionary<string, AgentState>();

        foreach (var key in keys)
        {
            var state = await _stateStore.GetAsync<AgentState>(key, cancellationToken);
            if (state != null)
            {
                states[state.AgentType] = state;
            }
        }

        return states;
    }

    public async Task SetToolResultAsync(string conversationId, string agentType, string toolName, object result, CancellationToken cancellationToken = default)
    {
        var state = await GetAgentStateAsync(conversationId, agentType, cancellationToken);
        
        state.ToolResults[toolName] = new ToolExecutionResult
        {
            ToolName = toolName,
            ExecutedAt = DateTime.UtcNow,
            Success = true,
            Result = result
        };

        await SaveAgentStateAsync(conversationId, agentType, state, cancellationToken);
    }

    public async Task<T?> GetToolResultAsync<T>(string conversationId, string agentType, string toolName, CancellationToken cancellationToken = default)
    {
        var state = await GetAgentStateAsync(conversationId, agentType, cancellationToken);
        
        if (state.ToolResults.TryGetValue(toolName, out var result) && result.Result is T typedResult)
        {
            return typedResult;
        }

        return default;
    }

    private static string GetKey(string conversationId, string agentType) => $"{KeyPrefix}{conversationId}:{agentType}";
}
