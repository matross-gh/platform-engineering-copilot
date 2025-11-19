using Platform.Engineering.Copilot.Core.Models.TokenManagement;

namespace Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;

/// <summary>
/// Service for optimizing RAG context based on semantic relevance and token limits
/// </summary>
public interface IRagContextOptimizer
{
    /// <summary>
    /// Optimize RAG search results based on relevance scores and token limits
    /// </summary>
    /// <param name="searchResults">Search results with relevance scores</param>
    /// <param name="options">Optimization options</param>
    /// <returns>Optimized RAG context</returns>
    OptimizedRagContext OptimizeContext(
        List<RankedSearchResult> searchResults,
        RagOptimizationOptions options);

    /// <summary>
    /// Rank and filter search results by relevance
    /// </summary>
    /// <param name="searchResults">Search results to rank</param>
    /// <param name="minRelevanceScore">Minimum relevance score threshold</param>
    /// <param name="maxResults">Maximum number of results to keep</param>
    /// <returns>Ranked and filtered results</returns>
    List<RankedSearchResult> RankAndFilter(
        List<RankedSearchResult> searchResults,
        double minRelevanceScore = 0.3,
        int maxResults = 10);

    /// <summary>
    /// Trim large results to fit within token limits
    /// </summary>
    /// <param name="result">Result to trim</param>
    /// <param name="maxTokens">Maximum tokens allowed</param>
    /// <param name="modelName">Model name for token counting</param>
    /// <returns>Trimmed result</returns>
    RankedSearchResult TrimResult(
        RankedSearchResult result,
        int maxTokens,
        string modelName = "gpt-4o");

    /// <summary>
    /// Convert generic search results to ranked results
    /// </summary>
    /// <param name="contents">Search result contents</param>
    /// <param name="defaultScore">Default relevance score if not specified</param>
    /// <param name="modelName">Model name for token counting</param>
    /// <returns>Ranked search results</returns>
    List<RankedSearchResult> CreateRankedResults(
        List<string> contents,
        double defaultScore = 0.5,
        string modelName = "gpt-4o");
}
