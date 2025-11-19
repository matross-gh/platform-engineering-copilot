namespace Platform.Engineering.Copilot.Core.Configuration;

/// <summary>
/// Configuration options for token management features
/// </summary>
public class TokenManagementOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "TokenManagement";

    /// <summary>
    /// Enable token management optimization
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable detailed token usage logging
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Log warning when token usage exceeds this percentage of context window
    /// </summary>
    public int WarningThresholdPercentage { get; set; } = 80;

    /// <summary>
    /// Default model name for token counting
    /// </summary>
    public string DefaultModelName { get; set; } = "gpt-4o";

    /// <summary>
    /// Reserved tokens for LLM completion
    /// </summary>
    public int ReservedCompletionTokens { get; set; } = 4000;

    /// <summary>
    /// Safety buffer percentage to prevent edge case token limit violations
    /// </summary>
    public int SafetyBufferPercentage { get; set; } = 5;

    /// <summary>
    /// Prompt optimization settings
    /// </summary>
    public PromptOptimizationSettings PromptOptimization { get; set; } = new();

    /// <summary>
    /// RAG context optimization settings
    /// </summary>
    public RagContextSettings RagContext { get; set; } = new();

    /// <summary>
    /// Conversation history management settings
    /// </summary>
    public ConversationHistorySettings ConversationHistory { get; set; } = new();
}

/// <summary>
/// Prompt optimization settings
/// </summary>
public class PromptOptimizationSettings
{
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
}

/// <summary>
/// RAG context optimization settings
/// </summary>
public class RagContextSettings
{
    /// <summary>
    /// Maximum tokens for RAG context
    /// </summary>
    public int MaxTokens { get; set; } = 10000;

    /// <summary>
    /// Minimum relevance score to keep search results (0.0 to 1.0)
    /// </summary>
    public double MinRelevanceScore { get; set; } = 0.3;

    /// <summary>
    /// Minimum number of search results to keep
    /// </summary>
    public int MinResults { get; set; } = 3;

    /// <summary>
    /// Maximum number of search results to keep
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Trim individual results that exceed token limits
    /// </summary>
    public bool TrimLargeResults { get; set; } = true;

    /// <summary>
    /// Maximum tokens for a single result before trimming
    /// </summary>
    public int MaxTokensPerResult { get; set; } = 2000;
}

/// <summary>
/// Conversation history management settings
/// </summary>
public class ConversationHistorySettings
{
    /// <summary>
    /// Maximum number of messages to keep in memory
    /// </summary>
    public int MaxMessages { get; set; } = 20;

    /// <summary>
    /// Maximum tokens for conversation history
    /// </summary>
    public int MaxTokens { get; set; } = 5000;

    /// <summary>
    /// Use summarization for old messages instead of removing
    /// </summary>
    public bool UseSummarization { get; set; } = false;

    /// <summary>
    /// Number of messages to keep before summarizing older ones
    /// </summary>
    public int SummarizationThreshold { get; set; } = 10;

    /// <summary>
    /// ChatBuilder default options
    /// </summary>
    public ChatBuilderSettings ChatBuilder { get; set; } = new();
}

/// <summary>
/// ChatBuilder configuration settings
/// </summary>
public class ChatBuilderSettings
{
    /// <summary>
    /// Default maximum tokens for chat history
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 5000;

    /// <summary>
    /// Reserved tokens for completion
    /// </summary>
    public int ReservedTokens { get; set; } = 0;

    /// <summary>
    /// Include system messages in history
    /// </summary>
    public bool IncludeSystemMessages { get; set; } = false;

    /// <summary>
    /// Minimum messages to keep even if exceeds token limit
    /// </summary>
    public int MinimumMessages { get; set; } = 2;

    /// <summary>
    /// Message separator in formatted output
    /// </summary>
    public string MessageSeparator { get; set; } = "\n";

    /// <summary>
    /// Format template for messages
    /// </summary>
    public string FormatTemplate { get; set; } = "{role}: {content}";

    /// <summary>
    /// Include timestamps in formatted output
    /// </summary>
    public bool IncludeTimestamps { get; set; } = false;

    /// <summary>
    /// Timestamp format string
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Process messages from oldest to newest (vs newest to oldest)
    /// </summary>
    public bool OldestFirst { get; set; } = false;
}
