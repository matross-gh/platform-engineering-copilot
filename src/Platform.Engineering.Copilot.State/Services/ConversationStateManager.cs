using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.State.Abstractions;
using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.State.Services;

/// <summary>
/// Default implementation of IConversationStateManager.
/// </summary>
public class ConversationStateManager : IConversationStateManager
{
    private readonly IStateStore _stateStore;
    private readonly ILogger<ConversationStateManager> _logger;
    private const string KeyPrefix = "conversation:";
    private const int MaxHistoryMessages = 100;

    public ConversationStateManager(IStateStore stateStore, ILogger<ConversationStateManager> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConversationState> GetOrCreateAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId);
        var state = await _stateStore.GetAsync<ConversationState>(key, cancellationToken);

        if (state == null)
        {
            state = new ConversationState
            {
                ConversationId = conversationId,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                Status = ConversationStatus.Active
            };

            await SaveAsync(state, cancellationToken);
            _logger.LogDebug("Created new conversation state for {ConversationId}", conversationId);
        }

        return state;
    }

    public async Task SaveAsync(ConversationState state, CancellationToken cancellationToken = default)
    {
        state.Touch();
        var key = GetKey(state.ConversationId);
        await _stateStore.SetAsync(key, state, cancellationToken: cancellationToken);
        _logger.LogDebug("Saved conversation state for {ConversationId}", state.ConversationId);
    }

    public async Task DeleteAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(conversationId);
        await _stateStore.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("Deleted conversation state for {ConversationId}", conversationId);
    }

    public async Task AddMessageAsync(string conversationId, ConversationMessage message, CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateAsync(conversationId, cancellationToken);
        state.Messages.Add(message);

        // Trim history if too long
        if (state.Messages.Count > MaxHistoryMessages)
        {
            state.Messages = state.Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(MaxHistoryMessages)
                .OrderBy(m => m.Timestamp)
                .ToList();
        }

        await SaveAsync(state, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync(string conversationId, int? maxMessages = null, CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateAsync(conversationId, cancellationToken);
        var messages = state.Messages.OrderBy(m => m.Timestamp).ToList();

        if (maxMessages.HasValue && messages.Count > maxMessages.Value)
        {
            messages = messages.TakeLast(maxMessages.Value).ToList();
        }

        return messages.AsReadOnly();
    }

    public async Task SetVariableAsync(string conversationId, string key, object value, CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateAsync(conversationId, cancellationToken);
        state.Variables[key] = value;
        await SaveAsync(state, cancellationToken);
    }

    public async Task<T?> GetVariableAsync<T>(string conversationId, string key, CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateAsync(conversationId, cancellationToken);
        if (state.Variables.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public async Task<IEnumerable<ConversationSummary>> ListActiveConversationsAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _stateStore.GetKeysAsync($"{KeyPrefix}*", cancellationToken);
        var summaries = new List<ConversationSummary>();

        foreach (var key in keys)
        {
            var state = await _stateStore.GetAsync<ConversationState>(key, cancellationToken);
            if (state != null && state.Status == ConversationStatus.Active)
            {
                summaries.Add(new ConversationSummary
                {
                    ConversationId = state.ConversationId,
                    UserId = state.UserId,
                    CreatedAt = state.CreatedAt,
                    LastActivityAt = state.LastActivityAt,
                    Status = state.Status,
                    MessageCount = state.Messages.Count,
                    LastAgentType = state.ActiveAgentType
                });
            }
        }

        return summaries.OrderByDescending(s => s.LastActivityAt);
    }

    private static string GetKey(string conversationId) => $"{KeyPrefix}{conversationId}";
}
