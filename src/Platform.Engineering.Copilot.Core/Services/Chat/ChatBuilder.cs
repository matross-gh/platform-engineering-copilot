using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Core.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;
using Platform.Engineering.Copilot.Core.Models.Chat;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;

namespace Platform.Engineering.Copilot.Core.Services.Chat;

/// <summary>
/// Service for building conversation history with token awareness.
/// Gathers the most conversation history up to token limits, formats it as a string,
/// and returns token count for use in prompt construction.
/// </summary>
public class ChatBuilder
{
    private readonly ITokenCounter _tokenCounter;
    private readonly TokenManagementOptions _options;
    private readonly ILogger<ChatBuilder> _logger;

    public ChatBuilder(
        ITokenCounter tokenCounter,
        IOptions<TokenManagementOptions> options,
        ILogger<ChatBuilder> logger)
    {
        _tokenCounter = tokenCounter;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Build conversation history from ConversationContext within token limits
    /// </summary>
    public ChatHistoryResult BuildHistory(
        ConversationContext context,
        ChatBuilderOptions? options = null)
    {
        if (context?.MessageHistory == null || !context.MessageHistory.Any())
        {
            return CreateEmptyResult(options);
        }

        return BuildHistory(context.MessageHistory, options);
    }

    /// <summary>
    /// Build conversation history from explicit message list within token limits
    /// </summary>
    public ChatHistoryResult BuildHistory(
        List<MessageSnapshot> messages,
        ChatBuilderOptions? options = null)
    {
        options ??= ChatBuilderOptions.CreateDefault(_options.DefaultModelName);

        if (messages == null || !messages.Any())
        {
            return CreateEmptyResult(options);
        }

        // Calculate available tokens
        var availableTokens = options.MaxTokens - options.ReservedTokens;
        if (availableTokens <= 0)
        {
            _logger.LogWarning("No tokens available for chat history (MaxTokens: {Max}, Reserved: {Reserved})",
                options.MaxTokens, options.ReservedTokens);
            return CreateEmptyResult(options);
        }

        // Filter messages based on options
        var filteredMessages = FilterMessages(messages, options);

        // Build history from newest to oldest
        var includedMessages = new List<MessageSnapshot>();
        var currentTokens = 0;
        var messagesProcessed = 0;

        // Reverse to process from newest to oldest
        var messagesToProcess = options.OldestFirst
            ? filteredMessages.ToList()
            : filteredMessages.AsEnumerable().Reverse().ToList();

        foreach (var message in messagesToProcess)
        {
            messagesProcessed++;

            // Format message
            var formattedMessage = FormatMessage(message, options);
            var messageTokens = _tokenCounter.CountTokens(formattedMessage, options.ModelName);

            // Check if we can add this message
            var wouldExceedLimit = currentTokens + messageTokens > availableTokens;
            var hasMinimum = includedMessages.Count >= options.MinimumMessages;

            if (!wouldExceedLimit || !hasMinimum)
            {
                // Add message (insert at beginning to maintain chronological order)
                if (options.OldestFirst)
                {
                    includedMessages.Add(message);
                }
                else
                {
                    includedMessages.Insert(0, message);
                }
                currentTokens += messageTokens;
            }
            else
            {
                // Stop - we've reached the token limit
                break;
            }
        }

        // Build formatted history string
        var formattedHistory = BuildFormattedString(includedMessages, options);

        // Calculate final token count (re-count the complete string for accuracy)
        var finalTokenCount = _tokenCounter.CountTokens(formattedHistory, options.ModelName);

        var result = new ChatHistoryResult
        {
            FormattedHistory = formattedHistory,
            TokenCount = finalTokenCount,
            MessageCount = includedMessages.Count,
            TruncatedMessageCount = filteredMessages.Count - includedMessages.Count,
            MaxTokens = options.MaxTokens,
            ModelName = options.ModelName,
            Messages = includedMessages,
            UtilizationPercentage = options.MaxTokens > 0
                ? (double)finalTokenCount / options.MaxTokens * 100
                : 0
        };

        if (result.WasTruncated)
        {
            _logger.LogInformation(
                "Chat history truncated: {Included}/{Total} messages, {Tokens} tokens",
                result.MessageCount,
                filteredMessages.Count,
                result.TokenCount);
        }

        return result;
    }

    /// <summary>
    /// Get formatted conversation context for vector search (limited messages, optimized for search)
    /// </summary>
    public string GetSearchContext(
        ConversationContext context,
        int maxMessages = 5)
    {
        if (context?.MessageHistory == null || !context.MessageHistory.Any())
        {
            return string.Empty;
        }

        var options = ChatBuilderOptions.CreateForVectorSearch(maxMessages, _options.DefaultModelName);
        var result = BuildHistory(context, options);

        return result.FormattedHistory;
    }

    /// <summary>
    /// Append a new message to existing history and rebuild if needed
    /// </summary>
    public ChatHistoryResult AppendMessage(
        ChatHistoryResult existingHistory,
        MessageSnapshot newMessage,
        ChatBuilderOptions? options = null)
    {
        options ??= ChatBuilderOptions.CreateDefault(_options.DefaultModelName);

        // Create new message list with appended message
        var allMessages = new List<MessageSnapshot>(existingHistory.Messages)
        {
            newMessage
        };

        // Rebuild history with new message
        return BuildHistory(allMessages, options);
    }

    /// <summary>
    /// Get conversation summary for context (last N exchanges)
    /// </summary>
    public string GetConversationSummary(
        ConversationContext context,
        int exchangeCount = 3)
    {
        if (context?.MessageHistory == null || !context.MessageHistory.Any())
        {
            return "No conversation history.";
        }

        // Take last N exchanges (each exchange = user + assistant message)
        var recentMessages = context.MessageHistory
            .TakeLast(exchangeCount * 2)
            .ToList();

        if (!recentMessages.Any())
        {
            return "No recent messages.";
        }

        var summary = new List<string>();
        for (int i = 0; i < recentMessages.Count; i += 2)
        {
            if (i + 1 < recentMessages.Count)
            {
                var userMsg = recentMessages[i];
                var assistantMsg = recentMessages[i + 1];

                // Truncate long messages
                var userContent = TruncateContent(userMsg.Content, 100);
                var assistantContent = TruncateContent(assistantMsg.Content, 150);

                summary.Add($"User asked about {userContent}");
                summary.Add($"Assistant responded with {assistantContent}");
            }
        }

        return string.Join("\n", summary);
    }

    // ========== PRIVATE HELPER METHODS ==========

    private ChatHistoryResult CreateEmptyResult(ChatBuilderOptions? options)
    {
        options ??= ChatBuilderOptions.CreateDefault(_options.DefaultModelName);

        return new ChatHistoryResult
        {
            FormattedHistory = string.Empty,
            TokenCount = 0,
            MessageCount = 0,
            TruncatedMessageCount = 0,
            MaxTokens = options.MaxTokens,
            ModelName = options.ModelName,
            Messages = new List<MessageSnapshot>(),
            UtilizationPercentage = 0
        };
    }

    private List<MessageSnapshot> FilterMessages(List<MessageSnapshot> messages, ChatBuilderOptions options)
    {
        var filtered = messages.AsEnumerable();

        // Filter out system messages if not included
        if (!options.IncludeSystemMessages)
        {
            filtered = filtered.Where(m => m.Role?.ToLowerInvariant() != "system");
        }

        return filtered.ToList();
    }

    private string FormatMessage(MessageSnapshot message, ChatBuilderOptions options)
    {
        var formatted = options.FormatTemplate;

        // Replace placeholders
        formatted = formatted.Replace("{role}", message.Role ?? "unknown");
        formatted = formatted.Replace("{content}", message.Content ?? string.Empty);

        if (options.IncludeTimestamps && message.Timestamp != default)
        {
            formatted = formatted.Replace("{timestamp}", message.Timestamp.ToString(options.TimestampFormat));
        }
        else
        {
            // Remove timestamp placeholder if not using timestamps
            formatted = formatted.Replace("[{timestamp}] ", "");
            formatted = formatted.Replace("{timestamp}", "");
        }

        return formatted;
    }

    private string BuildFormattedString(List<MessageSnapshot> messages, ChatBuilderOptions options)
    {
        if (!messages.Any())
        {
            return string.Empty;
        }

        var formattedMessages = messages
            .Select(m => FormatMessage(m, options))
            .Where(m => !string.IsNullOrWhiteSpace(m));

        return string.Join(options.MessageSeparator, formattedMessages);
    }

    private string TruncateContent(string? content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "...";
        }

        if (content.Length <= maxLength)
        {
            return content;
        }

        return content.Substring(0, maxLength) + "...";
    }
}
