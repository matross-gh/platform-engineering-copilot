namespace Platform.Engineering.Copilot.Core.Models.TokenManagement;

/// <summary>
/// Result of prompt optimization
/// </summary>
public class OptimizedPrompt
{
    /// <summary>
    /// Optimized system prompt
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Optimized user message (usually unchanged)
    /// </summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// Optimized RAG context items
    /// </summary>
    public List<string> RagContext { get; set; } = new();

    /// <summary>
    /// Optimized conversation history
    /// </summary>
    public List<string> ConversationHistory { get; set; } = new();

    /// <summary>
    /// Token estimate before optimization
    /// </summary>
    public TokenEstimate? OriginalEstimate { get; set; }

    /// <summary>
    /// Token estimate after optimization
    /// </summary>
    public TokenEstimate? OptimizedEstimate { get; set; }

    /// <summary>
    /// Number of RAG context items removed
    /// </summary>
    public int RagContextItemsRemoved { get; set; }

    /// <summary>
    /// Number of conversation history messages removed
    /// </summary>
    public int ConversationHistoryMessagesRemoved { get; set; }

    /// <summary>
    /// Tokens saved through optimization
    /// </summary>
    public int TokensSaved => (OriginalEstimate?.TotalTokens ?? 0) - (OptimizedEstimate?.TotalTokens ?? 0);

    /// <summary>
    /// Whether optimization was necessary
    /// </summary>
    public bool WasOptimized { get; set; }

    /// <summary>
    /// Optimization strategy used
    /// </summary>
    public string OptimizationStrategy { get; set; } = string.Empty;

    /// <summary>
    /// Warnings or notes about the optimization
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Get a human-readable summary of the optimization
    /// </summary>
    public string GetSummary()
    {
        if (!WasOptimized)
        {
            return "No optimization required - prompt within token limits.";
        }

        return $"Prompt Optimization Summary:\n" +
               $"  Strategy: {OptimizationStrategy}\n" +
               $"  Original Tokens: {OriginalEstimate?.TotalTokens:N0}\n" +
               $"  Optimized Tokens: {OptimizedEstimate?.TotalTokens:N0}\n" +
               $"  Tokens Saved: {TokensSaved:N0}\n" +
               $"  RAG Items: {OriginalEstimate?.RagContextItemTokens.Count} → {RagContext.Count} ({RagContextItemsRemoved} removed)\n" +
               $"  History Messages: {OriginalEstimate?.ConversationHistoryItemTokens.Count} → {ConversationHistory.Count} ({ConversationHistoryMessagesRemoved} removed)\n" +
               $"  Utilization: {OptimizedEstimate?.UtilizationPercentage:F1}%\n" +
               (Warnings.Any() ? $"  Warnings: {string.Join(", ", Warnings)}\n" : "");
    }
}
