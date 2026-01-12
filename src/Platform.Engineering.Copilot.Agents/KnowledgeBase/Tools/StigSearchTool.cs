using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;
using Platform.Engineering.Copilot.Core.Models.KnowledgeBase;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for searching STIG controls by keyword, severity, or category.
/// </summary>
public class StigSearchTool : BaseTool
{
    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly IStigKnowledgeService _stigService;
    private readonly KnowledgeBaseAgentOptions _options;

    public override string Name => "search_stigs";

    public override string Description =>
        "Search for STIG controls by keyword, severity, or category. " +
        "Use to find STIGs related to a specific technology or security concern. " +
        "Can filter by severity (CAT I, CAT II, CAT III) and service type.";

    public StigSearchTool(
        ILogger<StigSearchTool> logger,
        KnowledgeBaseStateAccessors stateAccessors,
        IStigKnowledgeService stigService,
        IOptions<KnowledgeBaseAgentOptions> options) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _stigService = stigService ?? throw new ArgumentNullException(nameof(stigService));
        _options = options?.Value ?? new KnowledgeBaseAgentOptions();

        Parameters.Add(new ToolParameter(
            name: "search_term",
            description: "Keyword or phrase to search for in STIG controls",
            required: true));

        Parameters.Add(new ToolParameter(
            name: "severity",
            description: "Filter by severity: 'high' (CAT I), 'medium' (CAT II), or 'low' (CAT III)",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "max_results",
            description: "Maximum number of results to return (default: 10)",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetRequiredString(arguments, "search_term");
        var severityStr = GetOptionalString(arguments, "severity")?.ToLowerInvariant();
        var maxResults = GetOptionalInt(arguments, "max_results") ?? 10;

        Logger.LogInformation("Searching STIGs for '{SearchTerm}' (severity: {Severity})", 
            searchTerm, severityStr ?? "all");

        try
        {
            // Track the query
            await _stateAccessors.SetLastQueryAsync("system", searchTerm, "stig_search", cancellationToken);

            // Search for STIGs
            var results = await _stigService.SearchStigsAsync(searchTerm, cancellationToken);

            // Filter by severity if specified
            if (!string.IsNullOrEmpty(severityStr))
            {
                var severity = severityStr switch
                {
                    "high" or "cat1" or "cati" => StigSeverity.High,
                    "medium" or "cat2" or "catii" => StigSeverity.Medium,
                    "low" or "cat3" or "catiii" => StigSeverity.Low,
                    _ => (StigSeverity?)null
                };

                if (severity.HasValue)
                {
                    results = results.Where(s => s.Severity == severity.Value).ToList();
                }
            }

            // Limit results
            var limitedResults = results.Take(maxResults).ToList();

            if (!limitedResults.Any())
            {
                return ToJson(new
                {
                    success = true,
                    message = $"No STIG controls found matching '{searchTerm}'.",
                    suggestions = new[]
                    {
                        "Try broader search terms",
                        "Remove severity filter",
                        "Use 'explain_stig' if you know the STIG ID"
                    }
                });
            }

            var stigSummaries = limitedResults.Select(s => new
            {
                id = s.StigId,
                title = s.Title,
                severity = s.Severity.ToString(),
                category = s.Category,
                nistControls = s.NistControls
            }).ToList();

            return ToJson(new
            {
                success = true,
                searchTerm,
                severity = severityStr ?? "all",
                totalFound = results.Count(),
                resultsReturned = limitedResults.Count,
                stigs = stigSummaries
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching STIGs for '{SearchTerm}'", searchTerm);
            return ToJson(new
            {
                success = false,
                error = $"Error searching STIGs: {ex.Message}"
            });
        }
    }
}
