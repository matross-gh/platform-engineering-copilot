using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.State.Abstractions;

/// <summary>
/// Manages conversation state across agent interactions.
/// </summary>
public interface IConversationStateManager
{
    /// <summary>
    /// Get or create a conversation state.
    /// </summary>
    Task<ConversationState> GetOrCreateAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save conversation state.
    /// </summary>
    Task SaveAsync(ConversationState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete conversation state.
    /// </summary>
    Task DeleteAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a message to conversation history.
    /// </summary>
    Task AddMessageAsync(string conversationId, ConversationMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversation history.
    /// </summary>
    Task<IReadOnlyList<ConversationMessage>> GetHistoryAsync(string conversationId, int? maxMessages = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a conversation variable.
    /// </summary>
    Task SetVariableAsync(string conversationId, string key, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a conversation variable.
    /// </summary>
    Task<T?> GetVariableAsync<T>(string conversationId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// List active conversations.
    /// </summary>
    Task<IEnumerable<ConversationSummary>> ListActiveConversationsAsync(CancellationToken cancellationToken = default);
}
