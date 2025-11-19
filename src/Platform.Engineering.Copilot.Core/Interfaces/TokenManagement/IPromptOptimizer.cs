using Platform.Engineering.Copilot.Core.Models.TokenManagement;

namespace Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;

/// <summary>
/// Service for optimizing prompts to fit within token limits
/// </summary>
public interface IPromptOptimizer
{
    /// <summary>
    /// Optimize a prompt to fit within the specified token limit
    /// </summary>
    /// <param name="systemPrompt">System prompt text</param>
    /// <param name="userMessage">User message text</param>
    /// <param name="ragContext">RAG context items</param>
    /// <param name="conversationHistory">Previous conversation messages</param>
    /// <param name="options">Optimization options</param>
    /// <returns>Optimized prompt components</returns>
    OptimizedPrompt OptimizePrompt(
        string systemPrompt,
        string userMessage,
        List<string> ragContext,
        List<string> conversationHistory,
        PromptOptimizationOptions options);

    /// <summary>
    /// Check if a prompt needs optimization
    /// </summary>
    /// <param name="systemPrompt">System prompt text</param>
    /// <param name="userMessage">User message text</param>
    /// <param name="ragContext">RAG context items</param>
    /// <param name="conversationHistory">Previous conversation messages</param>
    /// <param name="modelName">Model name</param>
    /// <param name="reservedCompletionTokens">Reserved tokens for completion</param>
    /// <returns>True if optimization is needed</returns>
    bool NeedsOptimization(
        string systemPrompt,
        string userMessage,
        List<string> ragContext,
        List<string> conversationHistory,
        string modelName = "gpt-4o",
        int reservedCompletionTokens = 4000);

    /// <summary>
    /// Calculate optimal distribution of tokens across prompt components
    /// </summary>
    /// <param name="estimate">Current token estimate</param>
    /// <param name="options">Optimization options</param>
    /// <returns>Dictionary with target tokens for each component</returns>
    Dictionary<string, int> CalculateTokenDistribution(
        TokenEstimate estimate,
        PromptOptimizationOptions options);
}
