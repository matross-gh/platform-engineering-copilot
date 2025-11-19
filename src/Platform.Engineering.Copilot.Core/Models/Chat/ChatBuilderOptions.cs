namespace Platform.Engineering.Copilot.Core.Models.Chat;

/// <summary>
/// Configuration options for building chat history
/// </summary>
public class ChatBuilderOptions
{
    /// <summary>
    /// Maximum tokens for conversation history
    /// </summary>
    public int MaxTokens { get; set; } = 5000;

    /// <summary>
    /// Model name for token counting (gpt-4o, gpt-4, gpt-3.5-turbo)
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o";

    /// <summary>
    /// Tokens to reserve for other parts of the prompt (system message, user input, completion)
    /// </summary>
    public int ReservedTokens { get; set; } = 0;

    /// <summary>
    /// Include system messages in the history
    /// </summary>
    public bool IncludeSystemMessages { get; set; } = false;

    /// <summary>
    /// Minimum number of messages to keep even if they exceed token limit
    /// </summary>
    public int MinimumMessages { get; set; } = 2;

    /// <summary>
    /// Separator between messages in formatted output
    /// </summary>
    public string MessageSeparator { get; set; } = "\n";

    /// <summary>
    /// Format template for each message. Placeholders: {role}, {content}, {timestamp}
    /// </summary>
    public string FormatTemplate { get; set; } = "{role}: {content}";

    /// <summary>
    /// Include timestamps in formatted output
    /// </summary>
    public bool IncludeTimestamps { get; set; } = false;

    /// <summary>
    /// Timestamp format (only used if IncludeTimestamps is true)
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Whether to reverse the order (oldest first instead of newest first)
    /// </summary>
    public bool OldestFirst { get; set; } = false;

    /// <summary>
    /// Create default options from token management configuration
    /// </summary>
    public static ChatBuilderOptions CreateDefault(string modelName = "gpt-4o")
    {
        return new ChatBuilderOptions
        {
            MaxTokens = 5000,
            ModelName = modelName,
            ReservedTokens = 0,
            IncludeSystemMessages = false,
            MinimumMessages = 2,
            MessageSeparator = "\n",
            FormatTemplate = "{role}: {content}",
            IncludeTimestamps = false
        };
    }

    /// <summary>
    /// Create options optimized for vector search context
    /// </summary>
    public static ChatBuilderOptions CreateForVectorSearch(int maxMessages = 5, string modelName = "gpt-4o")
    {
        return new ChatBuilderOptions
        {
            MaxTokens = 2000, // Smaller for search context
            ModelName = modelName,
            ReservedTokens = 0,
            IncludeSystemMessages = false,
            MinimumMessages = 1,
            MessageSeparator = "\n",
            FormatTemplate = "{role}: {content}",
            IncludeTimestamps = false
        };
    }

    /// <summary>
    /// Create options for debugging (includes timestamps, readable format)
    /// </summary>
    public static ChatBuilderOptions CreateForDebugging(string modelName = "gpt-4o")
    {
        return new ChatBuilderOptions
        {
            MaxTokens = 10000,
            ModelName = modelName,
            ReservedTokens = 0,
            IncludeSystemMessages = true,
            MinimumMessages = 1,
            MessageSeparator = "\n",
            FormatTemplate = "[{timestamp}] {role}: {content}",
            IncludeTimestamps = true,
            TimestampFormat = "yyyy-MM-dd HH:mm:ss"
        };
    }
}
