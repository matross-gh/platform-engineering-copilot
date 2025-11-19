using Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;
using Platform.Engineering.Copilot.Core.Models.TokenManagement;

namespace Platform.Engineering.Copilot.Core.Services.TokenManagement;

/// <summary>
/// Service for optimizing RAG context based on semantic relevance and token limits
/// </summary>
public class RagContextOptimizer : IRagContextOptimizer
{
    private readonly ITokenCounter _tokenCounter;

    public RagContextOptimizer(ITokenCounter tokenCounter)
    {
        _tokenCounter = tokenCounter;
    }

    /// <inheritdoc/>
    public OptimizedRagContext OptimizeContext(
        List<RankedSearchResult> searchResults,
        RagOptimizationOptions options)
    {
        if (searchResults == null || !searchResults.Any())
        {
            return new OptimizedRagContext
            {
                Results = new List<RankedSearchResult>(),
                TotalTokens = 0,
                OriginalResultCount = 0,
                WasOptimized = false,
                OptimizationStrategy = "No results to optimize"
            };
        }

        var result = new OptimizedRagContext
        {
            OriginalResultCount = searchResults.Count
        };

        // Step 1: Rank and filter by relevance
        var filtered = RankAndFilter(searchResults, options.MinRelevanceScore, options.MaxResults);

        // Step 2: Calculate token counts for each result
        foreach (var item in filtered)
        {
            item.TokenCount = _tokenCounter.CountTokens(item.Content, options.ModelName);
        }

        // Step 3: Trim large results if enabled
        if (options.TrimLargeResults)
        {
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].TokenCount > options.MaxTokensPerResult)
                {
                    filtered[i] = TrimResult(filtered[i], options.MaxTokensPerResult, options.ModelName);
                    result.ResultsTrimmed++;
                }
            }
        }

        // Step 4: Keep adding results until we hit token limit
        var optimizedResults = new List<RankedSearchResult>();
        var currentTokens = 0;
        var minResultsMet = false;

        foreach (var item in filtered)
        {
            var wouldExceedLimit = currentTokens + item.TokenCount > options.MaxRagTokens;

            // Always keep minimum results even if it exceeds limit
            if (!minResultsMet && optimizedResults.Count < options.MinResults)
            {
                optimizedResults.Add(item);
                currentTokens += item.TokenCount;
                minResultsMet = optimizedResults.Count >= options.MinResults;
            }
            else if (!wouldExceedLimit)
            {
                optimizedResults.Add(item);
                currentTokens += item.TokenCount;
            }
            else
            {
                result.ResultsRemoved++;
            }
        }

        // Step 5: Calculate statistics
        result.Results = optimizedResults;
        result.TotalTokens = currentTokens;
        result.ResultsRemoved += filtered.Count - optimizedResults.Count;
        result.WasOptimized = result.ResultsRemoved > 0 || result.ResultsTrimmed > 0;

        if (optimizedResults.Any())
        {
            result.AverageRelevanceScore = optimizedResults.Average(r => r.RelevanceScore);
            result.LowestRelevanceScore = optimizedResults.Min(r => r.RelevanceScore);
        }

        // Step 6: Determine optimization strategy
        var strategies = new List<string>();
        if (result.ResultsRemoved > 0)
            strategies.Add($"Removed {result.ResultsRemoved} low-ranked results");
        if (result.ResultsTrimmed > 0)
            strategies.Add($"Trimmed {result.ResultsTrimmed} large results");
        if (!strategies.Any())
            strategies.Add("No optimization needed");

        result.OptimizationStrategy = string.Join(", ", strategies);

        return result;
    }

    /// <inheritdoc/>
    public List<RankedSearchResult> RankAndFilter(
        List<RankedSearchResult> searchResults,
        double minRelevanceScore = 0.3,
        int maxResults = 10)
    {
        if (searchResults == null || !searchResults.Any())
            return new List<RankedSearchResult>();

        return searchResults
            .Where(r => r.RelevanceScore >= minRelevanceScore)
            .OrderByDescending(r => r.RelevanceScore)
            .Take(maxResults)
            .ToList();
    }

    /// <inheritdoc/>
    public RankedSearchResult TrimResult(
        RankedSearchResult result,
        int maxTokens,
        string modelName = "gpt-4o")
    {
        if (result.TokenCount <= maxTokens)
            return result;

        // Encode content
        var tokens = _tokenCounter.EncodeText(result.Content, modelName);

        // Calculate how many tokens we can keep (accounting for truncation notice)
        var truncationNotice = "\n\n[Content truncated to fit token limits]";
        var truncationTokens = _tokenCounter.CountTokens(truncationNotice, modelName);
        var keepTokens = maxTokens - truncationTokens;

        if (keepTokens <= 0)
        {
            return new RankedSearchResult
            {
                Content = truncationNotice,
                RelevanceScore = result.RelevanceScore,
                Source = result.Source,
                TokenCount = truncationTokens,
                Metadata = result.Metadata
            };
        }

        // Take first N tokens and decode
        var truncatedTokens = tokens.Take(keepTokens).ToList();
        var truncatedContent = _tokenCounter.DecodeTokens(truncatedTokens, modelName);

        return new RankedSearchResult
        {
            Content = truncatedContent + truncationNotice,
            RelevanceScore = result.RelevanceScore,
            Source = result.Source,
            TokenCount = _tokenCounter.CountTokens(truncatedContent + truncationNotice, modelName),
            Metadata = result.Metadata
        };
    }

    /// <inheritdoc/>
    public List<RankedSearchResult> CreateRankedResults(
        List<string> contents,
        double defaultScore = 0.5,
        string modelName = "gpt-4o")
    {
        if (contents == null || !contents.Any())
            return new List<RankedSearchResult>();

        return contents.Select((content, index) => new RankedSearchResult
        {
            Content = content,
            RelevanceScore = defaultScore,
            TokenCount = _tokenCounter.CountTokens(content, modelName),
            Source = $"Result {index + 1}"
        }).ToList();
    }
}
