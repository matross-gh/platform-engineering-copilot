namespace Platform.Engineering.Copilot.Core.Models.TokenManagement;

/// <summary>
/// Options for prompt optimization
/// </summary>
public class PromptOptimizationOptions
{
    /// <summary>
    /// Target token count for the optimized prompt
    /// </summary>
    public int TargetTokenCount { get; set; }

    /// <summary>
    /// Model name to use for token counting
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o";

    /// <summary>
    /// Maximum context window for the model
    /// </summary>
    public int MaxContextWindow { get; set; }

    /// <summary>
    /// Reserved tokens for completion
    /// </summary>
    public int ReservedCompletionTokens { get; set; } = 4000;

    /// <summary>
    /// Priority for system prompt (higher = keep more content)
    /// </summary>
    public int SystemPromptPriority { get; set; } = 100;

    /// <summary>
    /// Priority for user message (higher = keep more content)
    /// </summary>
    public int UserMessagePriority { get; set; } = 100;

    /// <summary>
    /// Priority for RAG context (higher = keep more content)
    /// </summary>
    public int RagContextPriority { get; set; } = 80;

    /// <summary>
    /// Priority for conversation history (higher = keep more content)
    /// </summary>
    public int ConversationHistoryPriority { get; set; } = 60;

    /// <summary>
    /// Minimum number of RAG context items to keep
    /// </summary>
    public int MinRagContextItems { get; set; } = 3;

    /// <summary>
    /// Minimum number of conversation history messages to keep
    /// </summary>
    public int MinConversationHistoryMessages { get; set; } = 2;

    /// <summary>
    /// Whether to use summarization for truncated content
    /// </summary>
    public bool UseSummarization { get; set; } = false;

    /// <summary>
    /// Safety buffer percentage (0-100) to avoid edge cases
    /// </summary>
    public int SafetyBufferPercentage { get; set; } = 5;
}
