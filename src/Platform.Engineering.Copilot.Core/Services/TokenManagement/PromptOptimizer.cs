using Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;
using Platform.Engineering.Copilot.Core.Models.TokenManagement;

namespace Platform.Engineering.Copilot.Core.Services.TokenManagement;

/// <summary>
/// Service for optimizing prompts to fit within token limits
/// </summary>
public class PromptOptimizer : IPromptOptimizer
{
    private readonly ITokenCounter _tokenCounter;

    public PromptOptimizer(ITokenCounter tokenCounter)
    {
        _tokenCounter = tokenCounter;
    }

    /// <inheritdoc/>
    public OptimizedPrompt OptimizePrompt(
        string systemPrompt,
        string userMessage,
        List<string> ragContext,
        List<string> conversationHistory,
        PromptOptimizationOptions options)
    {
        // Get original estimate
        var originalEstimate = _tokenCounter.EstimateTokens(
            systemPrompt,
            userMessage,
            ragContext ?? new List<string>(),
            conversationHistory ?? new List<string>(),
            options.ModelName);

        originalEstimate.ReservedCompletionTokens = options.ReservedCompletionTokens;

        // Check if optimization is needed
        var targetTokens = options.TargetTokenCount > 0
            ? options.TargetTokenCount
            : options.MaxContextWindow - options.ReservedCompletionTokens;

        // Apply safety buffer
        var safetyBuffer = (int)(targetTokens * options.SafetyBufferPercentage / 100.0);
        targetTokens -= safetyBuffer;

        if (originalEstimate.TotalInputTokens <= targetTokens)
        {
            // No optimization needed
            return new OptimizedPrompt
            {
                SystemPrompt = systemPrompt,
                UserMessage = userMessage,
                RagContext = ragContext ?? new List<string>(),
                ConversationHistory = conversationHistory ?? new List<string>(),
                OriginalEstimate = originalEstimate,
                OptimizedEstimate = originalEstimate,
                WasOptimized = false,
                OptimizationStrategy = "None - within token limits"
            };
        }

        // Calculate token distribution
        var distribution = CalculateTokenDistribution(originalEstimate, options);

        // Optimize each component
        var optimizedResult = new OptimizedPrompt
        {
            SystemPrompt = systemPrompt,
            UserMessage = userMessage,
            WasOptimized = true
        };

        // System prompt - only truncate if absolutely necessary
        if (originalEstimate.SystemPromptTokens > distribution["SystemPrompt"])
        {
            optimizedResult.SystemPrompt = TruncateToTokenLimit(
                systemPrompt,
                distribution["SystemPrompt"],
                options.ModelName,
                "... [System prompt truncated to fit token limits]");
            optimizedResult.Warnings.Add("System prompt was truncated - may affect agent behavior");
        }

        // User message - keep unchanged (highest priority)
        optimizedResult.UserMessage = userMessage;

        // RAG context - remove lowest ranked items first
        optimizedResult.RagContext = OptimizeRagContext(
            ragContext ?? new List<string>(),
            distribution["RagContext"],
            options,
            out var ragItemsRemoved);
        optimizedResult.RagContextItemsRemoved = ragItemsRemoved;

        if (ragItemsRemoved > 0)
        {
            optimizedResult.Warnings.Add($"Removed {ragItemsRemoved} RAG context items to fit token limits");
        }

        // Conversation history - keep most recent messages
        optimizedResult.ConversationHistory = OptimizeConversationHistory(
            conversationHistory ?? new List<string>(),
            distribution["ConversationHistory"],
            options,
            out var historyMessagesRemoved);
        optimizedResult.ConversationHistoryMessagesRemoved = historyMessagesRemoved;

        if (historyMessagesRemoved > 0)
        {
            optimizedResult.Warnings.Add($"Removed {historyMessagesRemoved} conversation history messages to fit token limits");
        }

        // Calculate optimized estimate
        optimizedResult.OptimizedEstimate = _tokenCounter.EstimateTokens(
            optimizedResult.SystemPrompt,
            optimizedResult.UserMessage,
            optimizedResult.RagContext,
            optimizedResult.ConversationHistory,
            options.ModelName);

        optimizedResult.OptimizedEstimate.ReservedCompletionTokens = options.ReservedCompletionTokens;

        // Determine optimization strategy
        optimizedResult.OptimizationStrategy = GetOptimizationStrategy(
            ragItemsRemoved,
            historyMessagesRemoved,
            originalEstimate.SystemPromptTokens != optimizedResult.OptimizedEstimate.SystemPromptTokens);

        return optimizedResult;
    }

    /// <inheritdoc/>
    public bool NeedsOptimization(
        string systemPrompt,
        string userMessage,
        List<string> ragContext,
        List<string> conversationHistory,
        string modelName = "gpt-4o",
        int reservedCompletionTokens = 4000)
    {
        var estimate = _tokenCounter.EstimateTokens(
            systemPrompt,
            userMessage,
            ragContext ?? new List<string>(),
            conversationHistory ?? new List<string>(),
            modelName);

        estimate.ReservedCompletionTokens = reservedCompletionTokens;

        return estimate.ExceedsLimit;
    }

    /// <inheritdoc/>
    public Dictionary<string, int> CalculateTokenDistribution(
        TokenEstimate estimate,
        PromptOptimizationOptions options)
    {
        // Calculate available tokens
        var availableTokens = options.MaxContextWindow - options.ReservedCompletionTokens;
        var safetyBuffer = (int)(availableTokens * options.SafetyBufferPercentage / 100.0);
        availableTokens -= safetyBuffer;

        // Calculate total priority
        var totalPriority = options.SystemPromptPriority +
                           options.UserMessagePriority +
                           options.RagContextPriority +
                           options.ConversationHistoryPriority;

        // Calculate proportional allocation
        var distribution = new Dictionary<string, int>
        {
            ["SystemPrompt"] = Math.Min(
                estimate.SystemPromptTokens,
                (int)(availableTokens * options.SystemPromptPriority / (double)totalPriority)),
            
            ["UserMessage"] = Math.Min(
                estimate.UserMessageTokens,
                (int)(availableTokens * options.UserMessagePriority / (double)totalPriority)),
            
            ["RagContext"] = Math.Min(
                estimate.RagContextTokens,
                (int)(availableTokens * options.RagContextPriority / (double)totalPriority)),
            
            ["ConversationHistory"] = Math.Min(
                estimate.ConversationHistoryTokens,
                (int)(availableTokens * options.ConversationHistoryPriority / (double)totalPriority))
        };

        // Redistribute unused tokens
        var usedTokens = distribution.Values.Sum();
        var unusedTokens = availableTokens - usedTokens;

        if (unusedTokens > 0)
        {
            // Give unused tokens to components that were truncated (in priority order)
            if (estimate.RagContextTokens > distribution["RagContext"])
            {
                var additional = Math.Min(unusedTokens, estimate.RagContextTokens - distribution["RagContext"]);
                distribution["RagContext"] += additional;
                unusedTokens -= additional;
            }

            if (unusedTokens > 0 && estimate.ConversationHistoryTokens > distribution["ConversationHistory"])
            {
                var additional = Math.Min(unusedTokens, estimate.ConversationHistoryTokens - distribution["ConversationHistory"]);
                distribution["ConversationHistory"] += additional;
                unusedTokens -= additional;
            }

            if (unusedTokens > 0 && estimate.SystemPromptTokens > distribution["SystemPrompt"])
            {
                var additional = Math.Min(unusedTokens, estimate.SystemPromptTokens - distribution["SystemPrompt"]);
                distribution["SystemPrompt"] += additional;
            }
        }

        return distribution;
    }

    /// <summary>
    /// Optimize RAG context by removing lowest ranked items
    /// </summary>
    private List<string> OptimizeRagContext(
        List<string> ragContext,
        int targetTokens,
        PromptOptimizationOptions options,
        out int itemsRemoved)
    {
        itemsRemoved = 0;

        if (ragContext == null || !ragContext.Any())
            return new List<string>();

        // Calculate tokens for each item
        var itemsWithTokens = ragContext.Select(item => new
        {
            Item = item,
            Tokens = _tokenCounter.CountTokens(item, options.ModelName)
        }).ToList();

        // Keep items until we reach the target (keeping minimum items)
        var optimized = new List<string>();
        var currentTokens = 0;

        foreach (var item in itemsWithTokens)
        {
            if (optimized.Count < options.MinRagContextItems ||
                (currentTokens + item.Tokens <= targetTokens))
            {
                optimized.Add(item.Item);
                currentTokens += item.Tokens;
            }
            else
            {
                itemsRemoved++;
            }
        }

        return optimized;
    }

    /// <summary>
    /// Optimize conversation history by keeping most recent messages
    /// </summary>
    private List<string> OptimizeConversationHistory(
        List<string> conversationHistory,
        int targetTokens,
        PromptOptimizationOptions options,
        out int messagesRemoved)
    {
        messagesRemoved = 0;

        if (conversationHistory == null || !conversationHistory.Any())
            return new List<string>();

        // Calculate tokens for each message
        var messagesWithTokens = conversationHistory.Select(msg => new
        {
            Message = msg,
            Tokens = _tokenCounter.CountTokens(msg, options.ModelName)
        }).ToList();

        // Keep most recent messages (reverse order)
        var optimized = new List<string>();
        var currentTokens = 0;

        // Start from the end (most recent)
        for (int i = messagesWithTokens.Count - 1; i >= 0; i--)
        {
            var msg = messagesWithTokens[i];

            if (optimized.Count < options.MinConversationHistoryMessages ||
                (currentTokens + msg.Tokens <= targetTokens))
            {
                optimized.Insert(0, msg.Message); // Insert at beginning to maintain order
                currentTokens += msg.Tokens;
            }
            else
            {
                messagesRemoved++;
            }
        }

        return optimized;
    }

    /// <summary>
    /// Truncate text to fit within token limit
    /// </summary>
    private string TruncateToTokenLimit(string text, int maxTokens, string modelName, string truncationSuffix = "...")
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var currentTokens = _tokenCounter.CountTokens(text, modelName);
        if (currentTokens <= maxTokens)
            return text;

        // Encode text
        var tokens = _tokenCounter.EncodeText(text, modelName);

        // Calculate how many tokens we can keep (accounting for suffix)
        var suffixTokens = _tokenCounter.CountTokens(truncationSuffix, modelName);
        var keepTokens = maxTokens - suffixTokens;

        if (keepTokens <= 0)
            return truncationSuffix;

        // Take first N tokens and decode
        var truncatedTokens = tokens.Take(keepTokens).ToList();
        var truncatedText = _tokenCounter.DecodeTokens(truncatedTokens, modelName);

        return truncatedText + truncationSuffix;
    }

    /// <summary>
    /// Determine the optimization strategy used
    /// </summary>
    private string GetOptimizationStrategy(int ragItemsRemoved, int historyMessagesRemoved, bool systemTruncated)
    {
        var strategies = new List<string>();

        if (ragItemsRemoved > 0)
            strategies.Add($"RAG context reduction ({ragItemsRemoved} items)");

        if (historyMessagesRemoved > 0)
            strategies.Add($"History pruning ({historyMessagesRemoved} messages)");

        if (systemTruncated)
            strategies.Add("System prompt truncation");

        return strategies.Any()
            ? string.Join(", ", strategies)
            : "Unknown";
    }
}
