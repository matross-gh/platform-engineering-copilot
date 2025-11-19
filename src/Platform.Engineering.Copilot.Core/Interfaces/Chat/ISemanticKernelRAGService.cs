using Platform.Engineering.Copilot.Core.Models.Chat;

namespace Platform.Engineering.Copilot.Core.Interfaces.Chat;

/// <summary>
/// Service interface for generating RAG-powered chat completions with detailed token tracking
/// </summary>
public interface ISemanticKernelRAGService
{
    /// <summary>
    /// Generate a chat completion with RAG context and conversation history
    /// Provides detailed token tracking for each component:
    /// - System prompt tokens
    /// - RAG context tokens
    /// - Conversation history tokens
    /// - User prompt tokens
    /// - Completion tokens
    /// </summary>
    /// <param name="request">Chat completion request containing:
    /// - System prompt (base agent instructions)
    /// - User prompt (current question)
    /// - RAG results (vector search results for context)
    /// - Conversation context (optional chat history)
    /// - Model configuration (temperature, max tokens, etc.)
    /// </param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Chat completion response with:
    /// - Generated content
    /// - Detailed token breakdown by component
    /// - Cost estimation
    /// - Performance metrics
    /// - RAG results included in response
    /// - Conversation history included
    /// </returns>
    Task<ChatCompletionResponse> GetResponseAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}
