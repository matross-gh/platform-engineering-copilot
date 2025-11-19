namespace Platform.Engineering.Copilot.Core.Models.TokenManagement;

/// <summary>
/// Detailed token usage estimate for a complete prompt
/// </summary>
public class TokenEstimate
{
    /// <summary>
    /// Tokens used by system prompt
    /// </summary>
    public int SystemPromptTokens { get; set; }

    /// <summary>
    /// Tokens used by user message
    /// </summary>
    public int UserMessageTokens { get; set; }

    /// <summary>
    /// Tokens used by RAG context
    /// </summary>
    public int RagContextTokens { get; set; }

    /// <summary>
    /// Tokens used by conversation history
    /// </summary>
    public int ConversationHistoryTokens { get; set; }

    /// <summary>
    /// Total input tokens
    /// </summary>
    public int TotalInputTokens => SystemPromptTokens + UserMessageTokens + RagContextTokens + ConversationHistoryTokens;

    /// <summary>
    /// Reserved tokens for completion
    /// </summary>
    public int ReservedCompletionTokens { get; set; }

    /// <summary>
    /// Total tokens (input + reserved completion)
    /// </summary>
    public int TotalTokens => TotalInputTokens + ReservedCompletionTokens;

    /// <summary>
    /// Maximum tokens allowed for this model
    /// </summary>
    public int MaxContextWindow { get; set; }

    /// <summary>
    /// Remaining tokens available
    /// </summary>
    public int RemainingTokens => MaxContextWindow - TotalTokens;

    /// <summary>
    /// Percentage of context window used
    /// </summary>
    public double UtilizationPercentage => (double)TotalTokens / MaxContextWindow * 100;

    /// <summary>
    /// Whether the estimate exceeds the model's limits
    /// </summary>
    public bool ExceedsLimit => TotalTokens > MaxContextWindow;

    /// <summary>
    /// Model name used for estimation
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Breakdown of RAG context tokens per item
    /// </summary>
    public List<int> RagContextItemTokens { get; set; } = new();

    /// <summary>
    /// Breakdown of conversation history tokens per message
    /// </summary>
    public List<int> ConversationHistoryItemTokens { get; set; } = new();

    /// <summary>
    /// Get a human-readable summary of the token estimate
    /// </summary>
    public string GetSummary()
    {
        return $"Token Usage ({ModelName}):\n" +
               $"  System: {SystemPromptTokens:N0}\n" +
               $"  User: {UserMessageTokens:N0}\n" +
               $"  RAG Context: {RagContextTokens:N0} ({RagContextItemTokens.Count} items)\n" +
               $"  History: {ConversationHistoryTokens:N0} ({ConversationHistoryItemTokens.Count} messages)\n" +
               $"  Total Input: {TotalInputTokens:N0}\n" +
               $"  Reserved Completion: {ReservedCompletionTokens:N0}\n" +
               $"  Total: {TotalTokens:N0} / {MaxContextWindow:N0} ({UtilizationPercentage:F1}%)\n" +
               $"  Status: {(ExceedsLimit ? "EXCEEDS LIMIT ⚠️" : "Within limits ✓")}";
    }
}
