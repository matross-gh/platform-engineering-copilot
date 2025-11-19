namespace Platform.Engineering.Copilot.Core.Models.TokenManagement;

/// <summary>
/// RAG search result with relevance score
/// </summary>
public class RankedSearchResult
{
    /// <summary>
    /// Original content from search result
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score (0.0 to 1.0)
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Source document or location
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Token count for this result
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Metadata from search engine
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Options for RAG context optimization
/// </summary>
public class RagOptimizationOptions
{
    /// <summary>
    /// Target maximum tokens for RAG context
    /// </summary>
    public int MaxRagTokens { get; set; } = 10000;

    /// <summary>
    /// Minimum relevance score to keep (0.0 to 1.0)
    /// </summary>
    public double MinRelevanceScore { get; set; } = 0.3;

    /// <summary>
    /// Minimum number of results to keep regardless of score
    /// </summary>
    public int MinResults { get; set; } = 3;

    /// <summary>
    /// Maximum number of results to keep
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Model name for token counting
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o";

    /// <summary>
    /// Whether to trim individual results that exceed token limits
    /// </summary>
    public bool TrimLargeResults { get; set; } = true;

    /// <summary>
    /// Maximum tokens for a single result before trimming
    /// </summary>
    public int MaxTokensPerResult { get; set; } = 2000;

    /// <summary>
    /// Whether to prioritize diversity in results
    /// </summary>
    public bool PrioritizeDiversity { get; set; } = false;
}

/// <summary>
/// Result of RAG context optimization
/// </summary>
public class OptimizedRagContext
{
    /// <summary>
    /// Optimized search results
    /// </summary>
    public List<RankedSearchResult> Results { get; set; } = new();

    /// <summary>
    /// Total tokens in optimized context
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Number of results removed
    /// </summary>
    public int ResultsRemoved { get; set; }

    /// <summary>
    /// Number of results trimmed
    /// </summary>
    public int ResultsTrimmed { get; set; }

    /// <summary>
    /// Original number of results
    /// </summary>
    public int OriginalResultCount { get; set; }

    /// <summary>
    /// Average relevance score of kept results
    /// </summary>
    public double AverageRelevanceScore { get; set; }

    /// <summary>
    /// Lowest relevance score in kept results
    /// </summary>
    public double LowestRelevanceScore { get; set; }

    /// <summary>
    /// Whether optimization was applied
    /// </summary>
    public bool WasOptimized { get; set; }

    /// <summary>
    /// Optimization strategy used
    /// </summary>
    public string OptimizationStrategy { get; set; } = string.Empty;

    /// <summary>
    /// Get combined text content for LLM context
    /// </summary>
    public string GetCombinedContext(string separator = "\n\n---\n\n")
    {
        return string.Join(separator, Results.Select(r => r.Content));
    }

    /// <summary>
    /// Get summary of optimization
    /// </summary>
    public string GetSummary()
    {
        if (!WasOptimized)
        {
            return $"No optimization required - {Results.Count} results within token limits ({TotalTokens:N0} tokens).";
        }

        return $"RAG Context Optimization Summary:\n" +
               $"  Strategy: {OptimizationStrategy}\n" +
               $"  Original Results: {OriginalResultCount}\n" +
               $"  Kept Results: {Results.Count} ({ResultsRemoved} removed)\n" +
               $"  Trimmed Results: {ResultsTrimmed}\n" +
               $"  Total Tokens: {TotalTokens:N0}\n" +
               $"  Avg Relevance: {AverageRelevanceScore:F3}\n" +
               $"  Min Relevance: {LowestRelevanceScore:F3}";
    }
}
