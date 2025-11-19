using Platform.Engineering.Copilot.Core.Models.TokenManagement;

namespace Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;

/// <summary>
/// Service for counting tokens in text using model-specific tokenizers
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Count the number of tokens in the provided text
    /// </summary>
    /// <param name="text">Text to count tokens for</param>
    /// <param name="modelName">Model name (gpt-4o, gpt-4, gpt-3.5-turbo)</param>
    /// <returns>Number of tokens</returns>
    int CountTokens(string text, string modelName = "gpt-4o");

    /// <summary>
    /// Encode text into token array
    /// </summary>
    /// <param name="text">Text to encode</param>
    /// <param name="modelName">Model name</param>
    /// <returns>Array of token IDs</returns>
    List<int> EncodeText(string text, string modelName = "gpt-4o");

    /// <summary>
    /// Decode token array back into text
    /// </summary>
    /// <param name="tokens">Token IDs to decode</param>
    /// <param name="modelName">Model name</param>
    /// <returns>Decoded text</returns>
    string DecodeTokens(List<int> tokens, string modelName = "gpt-4o");

    /// <summary>
    /// Estimate total tokens for a complete prompt including system message, user message, RAG context, and conversation history
    /// </summary>
    /// <param name="systemPrompt">System prompt text</param>
    /// <param name="userMessage">User message text</param>
    /// <param name="ragContext">RAG context items</param>
    /// <param name="conversationHistory">Previous conversation messages</param>
    /// <param name="modelName">Model name</param>
    /// <returns>Detailed token estimate</returns>
    TokenEstimate EstimateTokens(
        string systemPrompt,
        string userMessage,
        List<string> ragContext,
        List<string> conversationHistory,
        string modelName = "gpt-4o");

    /// <summary>
    /// Get the maximum context window size for a given model
    /// </summary>
    /// <param name="modelName">Model name</param>
    /// <returns>Maximum tokens supported</returns>
    int GetMaxContextWindow(string modelName);

    /// <summary>
    /// Get the maximum completion tokens for a given model
    /// </summary>
    /// <param name="modelName">Model name</param>
    /// <returns>Maximum completion tokens</returns>
    int GetMaxCompletionTokens(string modelName);
}
