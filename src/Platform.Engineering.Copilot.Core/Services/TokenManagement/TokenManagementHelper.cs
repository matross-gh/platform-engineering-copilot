using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Platform.Engineering.Copilot.Core.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.Chat;
using Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;
using Platform.Engineering.Copilot.Core.Models.Chat;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;
using Platform.Engineering.Copilot.Core.Models.TokenManagement;
using Platform.Engineering.Copilot.Core.Services.Chat;

namespace Platform.Engineering.Copilot.Core.Services.TokenManagement;

/// <summary>
/// Helper service for integrating token management into chat services
/// </summary>
public class TokenManagementHelper
{
    private readonly ITokenCounter _tokenCounter;
    private readonly IPromptOptimizer _promptOptimizer;
    private readonly IRagContextOptimizer _ragContextOptimizer;
    private readonly ChatBuilder _chatBuilder;
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly TokenManagementOptions _options;
    private readonly ILogger<TokenManagementHelper> _logger;

    public TokenManagementHelper(
        ITokenCounter tokenCounter,
        IPromptOptimizer promptOptimizer,
        IRagContextOptimizer ragContextOptimizer,
        ChatBuilder chatBuilder,
        ISemanticKernelService semanticKernelService,
        IOptions<TokenManagementOptions> options,
        ILogger<TokenManagementHelper> logger)
    {
        _tokenCounter = tokenCounter;
        _promptOptimizer = promptOptimizer;
        _ragContextOptimizer = ragContextOptimizer;
        _chatBuilder = chatBuilder;
        _semanticKernelService = semanticKernelService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Optimize chat history to fit within token limits
    /// </summary>
    public ChatHistory OptimizeChatHistory(
        ChatHistory chatHistory,
        string modelName,
        int? maxTokens = null)
    {
        if (!_options.Enabled)
            return chatHistory;

        var targetMaxTokens = maxTokens ?? _options.ConversationHistory.MaxTokens;
        var messages = chatHistory.ToList();
        var optimizedHistory = new ChatHistory();

        // Always keep system message if present
        var systemMessage = messages.FirstOrDefault(m => m.Role.Label == "system");
        if (systemMessage != null)
        {
            optimizedHistory.Add(systemMessage);
            messages = messages.Where(m => m.Role.Label != "system").ToList();
        }

        // Count tokens for remaining messages
        var messageTokens = messages.Select(m => new
        {
            Message = m,
            Tokens = _tokenCounter.CountTokens(m.Content ?? string.Empty, modelName)
        }).ToList();

        // Keep most recent messages within token limit
        var currentTokens = 0;
        var keptMessages = new List<Microsoft.SemanticKernel.ChatMessageContent>();

        // Iterate from most recent to oldest
        for (int i = messageTokens.Count - 1; i >= 0; i--)
        {
            var msgWithTokens = messageTokens[i];

            if (currentTokens + msgWithTokens.Tokens <= targetMaxTokens ||
                keptMessages.Count < _options.ConversationHistory.MaxMessages)
            {
                keptMessages.Insert(0, msgWithTokens.Message);
                currentTokens += msgWithTokens.Tokens;
            }
            else
            {
                break;
            }
        }

        // Add kept messages to optimized history
        foreach (var msg in keptMessages)
        {
            optimizedHistory.Add(msg);
        }

        if (_options.EnableLogging && keptMessages.Count < messages.Count)
        {
            _logger.LogInformation(
                "💾 Optimized chat history: {Original} → {Optimized} messages ({Tokens:N0} tokens)",
                messages.Count,
                keptMessages.Count,
                currentTokens);
        }

        return optimizedHistory;
    }

    /// <summary>
    /// Optimize a prompt with RAG context and conversation history
    /// </summary>
    public OptimizedPrompt OptimizePrompt(
        string systemPrompt,
        string userMessage,
        List<string>? ragContext = null,
        ConversationContext? conversationContext = null,
        string? modelName = null)
    {
        if (!_options.Enabled)
        {
            return new OptimizedPrompt
            {
                SystemPrompt = systemPrompt,
                UserMessage = userMessage,
                RagContext = ragContext ?? new List<string>(),
                ConversationHistory = conversationContext?.MessageHistory
                    .Select(m => $"{m.Role}: {m.Content}")
                    .ToList() ?? new List<string>(),
                WasOptimized = false,
                OptimizationStrategy = "Token management disabled"
            };
        }

        var model = modelName ?? _options.DefaultModelName;
        var conversationHistory = conversationContext?.MessageHistory
            .Select(m => $"{m.Role}: {m.Content}")
            .ToList() ?? new List<string>();

        // Create optimization options
        var optimizationOptions = new PromptOptimizationOptions
        {
            ModelName = model,
            MaxContextWindow = _tokenCounter.GetMaxContextWindow(model),
            ReservedCompletionTokens = _options.ReservedCompletionTokens,
            SafetyBufferPercentage = _options.SafetyBufferPercentage,
            SystemPromptPriority = _options.PromptOptimization.SystemPromptPriority,
            UserMessagePriority = _options.PromptOptimization.UserMessagePriority,
            RagContextPriority = _options.PromptOptimization.RagContextPriority,
            ConversationHistoryPriority = _options.PromptOptimization.ConversationHistoryPriority,
            MinRagContextItems = _options.PromptOptimization.MinRagContextItems,
            MinConversationHistoryMessages = _options.PromptOptimization.MinConversationHistoryMessages
        };

        // Optimize the prompt
        var result = _promptOptimizer.OptimizePrompt(
            systemPrompt,
            userMessage,
            ragContext ?? new List<string>(),
            conversationHistory,
            optimizationOptions);

        // Log optimization if enabled
        if (_options.EnableLogging && result.WasOptimized)
        {
            _logger.LogWarning("⚠️ Prompt optimization required:\n{Summary}", result.GetSummary());
        }
        else if (_options.EnableLogging)
        {
            var estimate = result.OptimizedEstimate ?? result.OriginalEstimate;
            if (estimate != null && estimate.UtilizationPercentage > _options.WarningThresholdPercentage)
            {
                _logger.LogWarning(
                    "⚠️ High token usage: {Utilization:F1}% ({Total:N0} / {Max:N0} tokens)",
                    estimate.UtilizationPercentage,
                    estimate.TotalTokens,
                    estimate.MaxContextWindow);
            }
        }

        return result;
    }

    /// <summary>
    /// Optimize RAG search results
    /// </summary>
    public OptimizedRagContext OptimizeRagContext(
        List<string> searchResults,
        string? modelName = null)
    {
        if (!_options.Enabled || searchResults == null || !searchResults.Any())
        {
            return new OptimizedRagContext
            {
                Results = searchResults?.Select((content, index) => new RankedSearchResult
                {
                    Content = content,
                    RelevanceScore = 0.5,
                    Source = $"Result {index + 1}"
                }).ToList() ?? new List<RankedSearchResult>(),
                WasOptimized = false
            };
        }

        var model = modelName ?? _options.DefaultModelName;

        // Convert to ranked results (with default scores since we don't have actual scores)
        var rankedResults = _ragContextOptimizer.CreateRankedResults(
            searchResults,
            defaultScore: 0.7, // Assume reasonable relevance
            modelName: model);

        // Create optimization options from configuration
        var options = new RagOptimizationOptions
        {
            MaxRagTokens = _options.RagContext.MaxTokens,
            MinRelevanceScore = _options.RagContext.MinRelevanceScore,
            MinResults = _options.RagContext.MinResults,
            MaxResults = _options.RagContext.MaxResults,
            TrimLargeResults = _options.RagContext.TrimLargeResults,
            MaxTokensPerResult = _options.RagContext.MaxTokensPerResult,
            ModelName = model
        };

        // Optimize
        var result = _ragContextOptimizer.OptimizeContext(rankedResults, options);

        // Log if enabled
        if (_options.EnableLogging && result.WasOptimized)
        {
            _logger.LogInformation("🔍 RAG context optimized:\n{Summary}", result.GetSummary());
        }

        return result;
    }

    /// <summary>
    /// Estimate tokens for a complete prompt
    /// </summary>
    public TokenEstimate EstimateTokens(
        string systemPrompt,
        string userMessage,
        List<string>? ragContext = null,
        ConversationContext? conversationContext = null,
        string? modelName = null)
    {
        var model = modelName ?? _options.DefaultModelName;
        var conversationHistory = conversationContext?.MessageHistory
            .Select(m => $"{m.Role}: {m.Content}")
            .ToList() ?? new List<string>();

        var estimate = _tokenCounter.EstimateTokens(
            systemPrompt,
            userMessage,
            ragContext ?? new List<string>(),
            conversationHistory,
            model);

        estimate.ReservedCompletionTokens = _options.ReservedCompletionTokens;

        return estimate;
    }

    /// <summary>
    /// Check if token management is enabled
    /// </summary>
    public bool IsEnabled => _options.Enabled;

    /// <summary>
    /// Get token management options
    /// </summary>
    public TokenManagementOptions Options => _options;

    /// <summary>
    /// Build conversation history with token awareness
    /// </summary>
    public ChatHistoryResult BuildChatHistory(
        ConversationContext context,
        ChatBuilderOptions? options = null)
    {
        return _chatBuilder.BuildHistory(context, options);
    }

    /// <summary>
    /// Build conversation history from explicit message list
    /// </summary>
    public ChatHistoryResult BuildChatHistory(
        List<MessageSnapshot> messages,
        ChatBuilderOptions? options = null)
    {
        return _chatBuilder.BuildHistory(messages, options);
    }

    /// <summary>
    /// Get conversation context for vector search
    /// </summary>
    public string GetSearchContext(ConversationContext context, int maxMessages = 5)
    {
        return _chatBuilder.GetSearchContext(context, maxMessages);
    }

    /// <summary>
    /// Generate chat completion with RAG context and conversation history
    /// </summary>
    /// <param name="request">Chat completion request with RAG results and conversation context</param>
    /// <returns>Chat completion response with detailed token breakdown</returns>
    public async Task<ChatCompletionResponse> GetRagCompletionAsync(ChatCompletionRequest request)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var ragLogger = loggerFactory.CreateLogger<SemanticKernelRAGService>();
        var ragService = new SemanticKernelRAGService(_semanticKernelService, _tokenCounter, _chatBuilder, ragLogger);
        return await ragService.GetResponseAsync(request);
    }
}
