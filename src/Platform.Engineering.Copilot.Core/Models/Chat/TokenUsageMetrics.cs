namespace Platform.Engineering.Copilot.Core.Models.Chat;

/// <summary>
/// Detailed token usage metrics for chat completions
/// Provides breakdown of token usage by component for accurate cost tracking
/// </summary>
public class TokenUsageMetrics
{
    /// <summary>
    /// Tokens used for base system prompt
    /// </summary>
    public int SystemPromptTokens { get; set; }

    /// <summary>
    /// Tokens used for RAG context (knowledge base search results)
    /// </summary>
    public int RagContextTokens { get; set; }

    /// <summary>
    /// Tokens used for conversation history
    /// </summary>
    public int ConversationHistoryTokens { get; set; }

    /// <summary>
    /// Tokens used for user's current prompt/question
    /// </summary>
    public int UserPromptTokens { get; set; }

    /// <summary>
    /// Total tokens in the prompt (input)
    /// Sum of: System + RAG + History + User
    /// </summary>
    public int TotalPromptTokens { get; set; }

    /// <summary>
    /// Tokens used in the completion (output/response)
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used (prompt + completion)
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Model name used for token counting
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Estimated cost based on token usage (USD)
    /// </summary>
    public double EstimatedCost { get; set; }

    /// <summary>
    /// Percentage of context window used
    /// </summary>
    public double ContextWindowUtilization { get; set; }

    /// <summary>
    /// Maximum context window for the model
    /// </summary>
    public int MaxContextWindow { get; set; }

    /// <summary>
    /// Number of RAG results included
    /// </summary>
    public int RagResultCount { get; set; }

    /// <summary>
    /// Number of conversation messages included
    /// </summary>
    public int ConversationMessageCount { get; set; }

    /// <summary>
    /// Whether token limits were exceeded and truncation occurred
    /// </summary>
    public bool WasTruncated { get; set; }

    /// <summary>
    /// Additional metadata for tracking
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Calculate estimated cost based on model pricing
    /// </summary>
    public void CalculateEstimatedCost()
    {
        // Pricing per 1K tokens (as of 2024)
        var pricing = GetModelPricing(ModelName);
        
        var promptCost = (TotalPromptTokens / 1000.0) * pricing.promptPer1k;
        var completionCost = (CompletionTokens / 1000.0) * pricing.completionPer1k;
        
        EstimatedCost = promptCost + completionCost;
    }

    /// <summary>
    /// Calculate context window utilization percentage
    /// </summary>
    public void CalculateUtilization()
    {
        if (MaxContextWindow > 0)
        {
            ContextWindowUtilization = (double)TotalTokens / MaxContextWindow * 100;
        }
    }

    /// <summary>
    /// Get formatted summary of token usage
    /// </summary>
    public string GetSummary()
    {
        return $@"Token Usage Summary ({ModelName})
==========================================
System Prompt:        {SystemPromptTokens:N0} tokens
RAG Context:          {RagContextTokens:N0} tokens ({RagResultCount} results)
Conversation History: {ConversationHistoryTokens:N0} tokens ({ConversationMessageCount} messages)
User Prompt:          {UserPromptTokens:N0} tokens
------------------------------------------
Total Prompt:         {TotalPromptTokens:N0} tokens
Completion:           {CompletionTokens:N0} tokens
------------------------------------------
TOTAL:                {TotalTokens:N0} tokens
Utilization:          {ContextWindowUtilization:F1}% of {MaxContextWindow:N0}
Estimated Cost:       ${EstimatedCost:F4}
{(WasTruncated ? "⚠️  Content was truncated to fit token limits" : "")}";
    }

    /// <summary>
    /// Get compact single-line summary
    /// </summary>
    public string GetCompactSummary()
    {
        return $"Tokens: {TotalTokens:N0} (S:{SystemPromptTokens} R:{RagContextTokens} H:{ConversationHistoryTokens} U:{UserPromptTokens} C:{CompletionTokens}) ${EstimatedCost:F4}";
    }

    /// <summary>
    /// Get model pricing information
    /// </summary>
    private (double promptPer1k, double completionPer1k) GetModelPricing(string modelName)
    {
        // Pricing as of November 2024 (USD per 1K tokens)
        return modelName.ToLowerInvariant() switch
        {
            "gpt-4o" => (0.0025, 0.010),              // GPT-4o
            "gpt-4o-2024-08-06" => (0.0025, 0.010),   // GPT-4o (latest)
            "gpt-4-turbo" => (0.01, 0.03),            // GPT-4 Turbo
            "gpt-4-turbo-2024-04-09" => (0.01, 0.03), // GPT-4 Turbo
            "gpt-4" => (0.03, 0.06),                  // GPT-4
            "gpt-4-32k" => (0.06, 0.12),              // GPT-4 32K
            "gpt-3.5-turbo" => (0.0005, 0.0015),      // GPT-3.5 Turbo
            "gpt-3.5-turbo-16k" => (0.003, 0.004),    // GPT-3.5 Turbo 16K
            _ => (0.0025, 0.010)                       // Default to GPT-4o pricing
        };
    }
}
