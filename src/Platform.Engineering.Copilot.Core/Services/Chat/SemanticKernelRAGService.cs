using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Platform.Engineering.Copilot.Core.Interfaces.Chat;
using Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;
using Platform.Engineering.Copilot.Core.Models.Chat;
using System.Diagnostics;
using System.Text;

namespace Platform.Engineering.Copilot.Core.Services.Chat;

/// <summary>
/// Service for generating RAG-powered chat completions with detailed token tracking
/// Combines system prompts, RAG context, conversation history, and user prompts
/// Provides accurate token counting for each component separately
/// </summary>
public class SemanticKernelRAGService : ISemanticKernelRAGService
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly ITokenCounter _tokenCounter;
    private readonly ChatBuilder _chatBuilder;
    private readonly ILogger<SemanticKernelRAGService> _logger;

    public SemanticKernelRAGService(
        ISemanticKernelService semanticKernelService,
        ITokenCounter tokenCounter,
        ChatBuilder chatBuilder,
        ILogger<SemanticKernelRAGService> logger)
    {
        _semanticKernelService = semanticKernelService;
        _tokenCounter = tokenCounter;
        _chatBuilder = chatBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Generate a chat completion with RAG context and conversation history
    /// Provides detailed token tracking for each component
    /// </summary>
    public async Task<ChatCompletionResponse> GetResponseAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ChatCompletionResponse
        {
            ConversationId = request.ConversationContext?.ConversationId,
            ModelUsed = request.ModelName
        };

        try
        {
            // Validate request
            if (!request.IsValid(out var validationError))
            {
                return CreateErrorResponse(validationError, stopwatch.ElapsedMilliseconds);
            }

            _logger.LogInformation("🤖 Generating RAG completion for conversation {ConversationId}", 
                request.ConversationContext?.ConversationId ?? "N/A");

            // Step 1: Build conversation history using ChatBuilder (if requested)
            ChatHistoryResult? historyResult = null;
            if (request.IncludeConversationHistory && request.ConversationContext != null)
            {
                var chatBuilderOptions = new ChatBuilderOptions
                {
                    MaxTokens = request.MaxHistoryTokens,
                    ModelName = request.ModelName,
                    MinimumMessages = 2,
                    IncludeSystemMessages = false
                };

                historyResult = _chatBuilder.BuildHistory(
                    request.ConversationContext,
                    chatBuilderOptions);

                _logger.LogInformation("📜 Built conversation history: {MessageCount} messages, {TokenCount} tokens",
                    historyResult.MessageCount, historyResult.TokenCount);
            }

            // Step 2: Build complete system prompt with RAG context
            var systemPromptWithRag = BuildSystemPromptWithRAG(
                request.SystemPrompt,
                request.RagResults,
                request.IncludeRagContext);

            // Step 3: Build user message with conversation history
            var userMessageWithHistory = BuildUserMessageWithHistory(
                request.UserPrompt,
                historyResult);

            // Step 4: Count tokens for each component SEPARATELY (critical for cost tracking)
            var tokenMetrics = CountTokensByComponent(
                request.SystemPrompt,
                request.RagResults,
                historyResult,
                request.UserPrompt,
                request.ModelName);

            _logger.LogInformation("📊 Token breakdown - System: {System}, RAG: {Rag}, History: {History}, User: {User}",
                tokenMetrics.SystemPromptTokens,
                tokenMetrics.RagContextTokens,
                tokenMetrics.ConversationHistoryTokens,
                tokenMetrics.UserPromptTokens);

            // Step 5: Create kernel and get chat completion service
            var kernel = _semanticKernelService.CreateSpecializedKernel(
                Core.Models.Agents.AgentType.Orchestrator);

            IChatCompletionService? chatCompletion;
            try
            {
                chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get chat completion service");
                return CreateErrorResponse("Chat completion service not available. Configure Azure OpenAI or OpenAI.", 
                    stopwatch.ElapsedMilliseconds);
            }

            // Step 6: Build chat history for completion
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddSystemMessage(systemPromptWithRag);
            chatHistory.AddUserMessage(userMessageWithHistory);

            // Step 7: Configure execution settings
            var executionSettings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
            {
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens
            };

            // Step 8: Execute chat completion
            _logger.LogInformation("🚀 Executing chat completion with {ModelName}...", request.ModelName);
            
            var result = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken);

            // Step 9: Extract completion metadata
            var completionTokens = ExtractCompletionTokens(result);

            // Step 10: Build response with complete token metrics
            response.Success = true;
            response.Content = result.Content ?? string.Empty;
            response.FinishReason = result.Metadata?.GetValueOrDefault("FinishReason")?.ToString();
            
            // Set token usage
            tokenMetrics.CompletionTokens = completionTokens;
            tokenMetrics.TotalTokens = tokenMetrics.TotalPromptTokens + completionTokens;
            tokenMetrics.MaxContextWindow = _tokenCounter.GetMaxContextWindow(request.ModelName);
            tokenMetrics.RagResultCount = request.RagResults.Count;
            tokenMetrics.ConversationMessageCount = historyResult?.MessageCount ?? 0;
            
            tokenMetrics.CalculateUtilization();
            tokenMetrics.CalculateEstimatedCost();
            
            response.TokenUsage = tokenMetrics;
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            response.UsedRagContext = request.IncludeRagContext && request.RagResults.Any();
            response.UsedConversationHistory = request.IncludeConversationHistory && historyResult != null;
            response.IncludedRagResults = request.RagResults;
            response.IncludedHistory = historyResult;

            _logger.LogInformation("✅ Completion successful - {CompactSummary}",
                response.TokenUsage.GetCompactSummary());

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RAG completion");
            return CreateErrorResponse($"Completion failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Build system prompt with RAG context embedded
    /// </summary>
    private string BuildSystemPromptWithRAG(
        string baseSystemPrompt,
        List<string> ragResults,
        bool includeRagContext)
    {
        if (!includeRagContext || ragResults == null || !ragResults.Any())
        {
            return baseSystemPrompt;
        }

        var sb = new StringBuilder();
        sb.AppendLine(baseSystemPrompt);
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine("KNOWLEDGE BASE CONTEXT (Retrieved Documentation)");
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("Use the following retrieved documentation to answer the user's question accurately.");
        sb.AppendLine("Cite specific sources when making claims. If the context doesn't contain relevant information,");
        sb.AppendLine("acknowledge this and use your general knowledge appropriately.");
        sb.AppendLine();

        for (int i = 0; i < ragResults.Count; i++)
        {
            sb.AppendLine($"[Source {i + 1}]");
            sb.AppendLine("-----------------------------------------------------------");
            sb.AppendLine(ragResults[i]);
            sb.AppendLine();
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine("END KNOWLEDGE BASE CONTEXT");
        sb.AppendLine("═══════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    /// <summary>
    /// Build user message with conversation history embedded
    /// </summary>
    private string BuildUserMessageWithHistory(
        string userPrompt,
        ChatHistoryResult? historyResult)
    {
        if (historyResult == null || historyResult.IsEmpty)
        {
            return userPrompt;
        }

        var sb = new StringBuilder();
        sb.AppendLine("RECENT CONVERSATION CONTEXT:");
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine(historyResult.FormattedHistory);
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("CURRENT QUESTION:");
        sb.AppendLine(userPrompt);

        return sb.ToString();
    }

    /// <summary>
    /// Count tokens for each component separately
    /// This is CRITICAL for accurate cost tracking and optimization
    /// </summary>
    private TokenUsageMetrics CountTokensByComponent(
        string baseSystemPrompt,
        List<string> ragResults,
        ChatHistoryResult? historyResult,
        string userPrompt,
        string modelName)
    {
        var metrics = new TokenUsageMetrics
        {
            ModelName = modelName
        };

        // Count base system prompt
        metrics.SystemPromptTokens = _tokenCounter.CountTokens(baseSystemPrompt, modelName);

        // Count RAG context (if any)
        if (ragResults != null && ragResults.Any())
        {
            var ragContextText = string.Join("\n", ragResults);
            metrics.RagContextTokens = _tokenCounter.CountTokens(ragContextText, modelName);
        }

        // Count conversation history (if any)
        if (historyResult != null && !historyResult.IsEmpty)
        {
            metrics.ConversationHistoryTokens = historyResult.TokenCount;
        }

        // Count user prompt
        metrics.UserPromptTokens = _tokenCounter.CountTokens(userPrompt, modelName);

        // Calculate total prompt tokens
        metrics.TotalPromptTokens = 
            metrics.SystemPromptTokens +
            metrics.RagContextTokens +
            metrics.ConversationHistoryTokens +
            metrics.UserPromptTokens;

        // Add overhead for formatting (separators, labels, etc.) - approximately 5%
        var formattingOverhead = (int)(metrics.TotalPromptTokens * 0.05);
        metrics.TotalPromptTokens += formattingOverhead;

        return metrics;
    }

    /// <summary>
    /// Extract completion token count from result metadata
    /// </summary>
    private int ExtractCompletionTokens(Microsoft.SemanticKernel.ChatMessageContent result)
    {
        try
        {
            // Try to get from metadata
            if (result.Metadata != null && result.Metadata.TryGetValue("Usage", out var usage))
            {
                var usageDict = usage as Dictionary<string, object>;
                if (usageDict?.TryGetValue("CompletionTokens", out var completionTokensObj) == true)
                {
                    if (completionTokensObj is int completionTokens)
                    {
                        return completionTokens;
                    }
                }
            }

            // Fallback: estimate from content
            if (!string.IsNullOrEmpty(result.Content))
            {
                return _tokenCounter.CountTokens(result.Content, result.ModelId ?? "gpt-4o");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract completion tokens from result");
            return 0;
        }
    }

    /// <summary>
    /// Create error response
    /// </summary>
    private ChatCompletionResponse CreateErrorResponse(string errorMessage, long elapsedMs)
    {
        return new ChatCompletionResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            ProcessingTimeMs = elapsedMs,
            Content = string.Empty
        };
    }
}
