using Platform.Engineering.Copilot.Core.Models.IntelligentChat;

namespace Platform.Engineering.Copilot.Core.Models.Chat;

/// <summary>
/// Response from RAG-powered chat completion
/// Contains the generated response and detailed token usage metrics
/// </summary>
public class ChatCompletionResponse
{
    /// <summary>
    /// The generated response content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the completion was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if completion failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed token usage breakdown
    /// </summary>
    public TokenUsageMetrics TokenUsage { get; set; } = new();

    /// <summary>
    /// Model used for completion
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Timestamp of completion
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Conversation ID (if applicable)
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// RAG search results that were included in the prompt
    /// </summary>
    public List<string> IncludedRagResults { get; set; } = new();

    /// <summary>
    /// Conversation history that was included
    /// </summary>
    public ChatHistoryResult? IncludedHistory { get; set; }

    /// <summary>
    /// Whether RAG context was included
    /// </summary>
    public bool UsedRagContext { get; set; }

    /// <summary>
    /// Whether conversation history was included
    /// </summary>
    public bool UsedConversationHistory { get; set; }

    /// <summary>
    /// Finish reason from the model (e.g., "stop", "length", "content_filter")
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Additional metadata from the completion
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Get compact summary for logging
    /// </summary>
    public string GetSummary()
    {
        var status = Success ? "✅ Success" : $"❌ Failed: {ErrorMessage}";
        return $@"{status}
Model: {ModelUsed}
Time: {ProcessingTimeMs}ms
{TokenUsage.GetCompactSummary()}
RAG: {(UsedRagContext ? $"{IncludedRagResults.Count} results" : "None")}
History: {(UsedConversationHistory ? $"{IncludedHistory?.MessageCount ?? 0} messages" : "None")}";
    }

    /// <summary>
    /// Get detailed summary for debugging
    /// </summary>
    public string GetDetailedSummary()
    {
        return $@"Chat Completion Response
==========================================
Status: {(Success ? "✅ Success" : $"❌ Failed: {ErrorMessage}")}
Model: {ModelUsed}
Processing Time: {ProcessingTimeMs}ms
Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}
Conversation ID: {ConversationId ?? "N/A"}
Finish Reason: {FinishReason ?? "N/A"}

{TokenUsage.GetSummary()}

RAG Context: {(UsedRagContext ? $"✅ {IncludedRagResults.Count} results included" : "❌ Not used")}
Conversation History: {(UsedConversationHistory ? $"✅ {IncludedHistory?.MessageCount ?? 0} messages, {IncludedHistory?.TokenCount ?? 0} tokens" : "❌ Not used")}

Response Length: {Content.Length:N0} characters
Response Preview: {(Content.Length > 200 ? Content.Substring(0, 200) + "..." : Content)}";
    }

    /// <summary>
    /// Check if response is empty
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Get response lines
    /// </summary>
    public string[] GetLines() => Content.Split('\n');

    /// <summary>
    /// Calculate cost efficiency (tokens per dollar)
    /// </summary>
    public double GetCostEfficiency()
    {
        if (TokenUsage.EstimatedCost > 0)
        {
            return TokenUsage.TotalTokens / TokenUsage.EstimatedCost;
        }
        return 0;
    }

    /// <summary>
    /// Get token breakdown percentages
    /// </summary>
    public Dictionary<string, double> GetTokenBreakdownPercentages()
    {
        if (TokenUsage.TotalPromptTokens == 0)
        {
            return new Dictionary<string, double>();
        }

        return new Dictionary<string, double>
        {
            ["SystemPrompt"] = (double)TokenUsage.SystemPromptTokens / TokenUsage.TotalPromptTokens * 100,
            ["RagContext"] = (double)TokenUsage.RagContextTokens / TokenUsage.TotalPromptTokens * 100,
            ["ConversationHistory"] = (double)TokenUsage.ConversationHistoryTokens / TokenUsage.TotalPromptTokens * 100,
            ["UserPrompt"] = (double)TokenUsage.UserPromptTokens / TokenUsage.TotalPromptTokens * 100
        };
    }
}
