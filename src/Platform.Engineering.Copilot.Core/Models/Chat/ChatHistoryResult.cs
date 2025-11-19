namespace Platform.Engineering.Copilot.Core.Models.Chat;

/// <summary>
/// Result of building conversation history with token awareness
/// </summary>
public class ChatHistoryResult
{
    /// <summary>
    /// Formatted conversation history as a string (newline-separated)
    /// </summary>
    public string FormattedHistory { get; set; } = string.Empty;

    /// <summary>
    /// Total tokens used in the conversation history
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Number of messages included in the history
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Number of messages that were truncated/removed due to token limits
    /// </summary>
    public int TruncatedMessageCount { get; set; }

    /// <summary>
    /// Percentage of available tokens used by the history
    /// </summary>
    public double UtilizationPercentage { get; set; }

    /// <summary>
    /// List of messages included in the history (most recent first)
    /// </summary>
    public List<Platform.Engineering.Copilot.Core.Models.IntelligentChat.MessageSnapshot> Messages { get; set; } = new();

    /// <summary>
    /// Maximum tokens allowed for this history
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Model name used for token counting
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the history was truncated
    /// </summary>
    public bool WasTruncated => TruncatedMessageCount > 0;

    /// <summary>
    /// Get a human-readable debug summary
    /// </summary>
    public string GetDebugSummary()
    {
        return $@"Chat History Summary:
  Messages: {MessageCount} included, {TruncatedMessageCount} truncated
  Tokens: {TokenCount:N0} / {MaxTokens:N0} ({UtilizationPercentage:F1}%)
  Model: {ModelName}
  Status: {(WasTruncated ? "⚠️ Truncated" : "✓ Complete")}
  Length: {FormattedHistory.Length:N0} characters";
    }

    /// <summary>
    /// Get lines of formatted history for easy processing
    /// </summary>
    public string[] GetLines()
    {
        return FormattedHistory.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Check if history is empty
    /// </summary>
    public bool IsEmpty => MessageCount == 0 || string.IsNullOrWhiteSpace(FormattedHistory);
}
