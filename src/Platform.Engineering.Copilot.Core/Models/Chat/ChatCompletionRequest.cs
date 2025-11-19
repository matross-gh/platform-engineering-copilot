using Platform.Engineering.Copilot.Core.Models.IntelligentChat;

namespace Platform.Engineering.Copilot.Core.Models.Chat;

/// <summary>
/// Request model for RAG-powered chat completions
/// Combines system prompt, RAG context, conversation history, and user prompt
/// </summary>
public class ChatCompletionRequest
{
    /// <summary>
    /// Base system prompt (agent-specific instructions)
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// User's current question or prompt
    /// </summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// RAG search results from knowledge base (vector search results)
    /// </summary>
    public List<string> RagResults { get; set; } = new();

    /// <summary>
    /// Conversation context with message history
    /// </summary>
    public ConversationContext? ConversationContext { get; set; }

    /// <summary>
    /// Model name to use (e.g., "gpt-4o", "gpt-4-turbo")
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o";

    /// <summary>
    /// Temperature for response generation (0.0 to 2.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Maximum tokens for completion response
    /// </summary>
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Maximum messages to include from conversation history
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 10;

    /// <summary>
    /// Maximum tokens to allocate for conversation history
    /// </summary>
    public int MaxHistoryTokens { get; set; } = 2000;

    /// <summary>
    /// Whether to include RAG context in system prompt
    /// </summary>
    public bool IncludeRagContext { get; set; } = true;

    /// <summary>
    /// Whether to include conversation history
    /// </summary>
    public bool IncludeConversationHistory { get; set; } = true;

    /// <summary>
    /// Additional metadata for request tracking
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Get total number of RAG results
    /// </summary>
    public int RagResultCount => RagResults?.Count ?? 0;

    /// <summary>
    /// Get estimated RAG context size in characters
    /// </summary>
    public int EstimatedRagContextSize => RagResults?.Sum(r => r?.Length ?? 0) ?? 0;

    /// <summary>
    /// Validate the request
    /// </summary>
    public bool IsValid(out string validationError)
    {
        if (string.IsNullOrWhiteSpace(UserPrompt))
        {
            validationError = "UserPrompt is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SystemPrompt))
        {
            validationError = "SystemPrompt is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ModelName))
        {
            validationError = "ModelName is required";
            return false;
        }

        if (Temperature < 0 || Temperature > 2.0)
        {
            validationError = "Temperature must be between 0.0 and 2.0";
            return false;
        }

        if (MaxTokens < 1)
        {
            validationError = "MaxTokens must be greater than 0";
            return false;
        }

        validationError = string.Empty;
        return true;
    }
}
