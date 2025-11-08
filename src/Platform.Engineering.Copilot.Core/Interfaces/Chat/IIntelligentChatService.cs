using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;

namespace Platform.Engineering.Copilot.Core.Interfaces.Chat;

/// <summary>
/// Pure Multi-Agent Intelligent Chat Service
/// Uses OrchestratorAgent to coordinate specialized agents for all requests.
/// NO LEGACY SINGLE-AGENT CODE - This is a pure multi-agent system.
/// </summary>
public interface IIntelligentChatService
{
    /// <summary>
    /// Process a user message using pure multi-agent orchestration
    /// All intelligence, planning, and execution handled by OrchestratorAgent
    /// </summary>
    Task<IntelligentChatResponse> ProcessMessageAsync(
        string message,
        string conversationId,
        ConversationContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate proactive suggestions based on conversation context
    /// </summary>
    Task<List<ProactiveSuggestion>> GenerateProactiveSuggestionsAsync(
        string conversationId,
        ConversationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create conversation context
    /// </summary>
    Task<ConversationContext> GetOrCreateContextAsync(
        string conversationId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update conversation context with new message
    /// </summary>
    Task UpdateContextAsync(
        ConversationContext context,
        MessageSnapshot message,
        CancellationToken cancellationToken = default);
}
