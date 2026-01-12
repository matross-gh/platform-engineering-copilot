using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for searching NIST 800-53 controls by topic, keyword, or family.
/// Provides discovery and exploration of the NIST control catalog.
/// </summary>
public class NistControlSearchTool : BaseTool
{
    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly INistControlsService _nistControlsService;
    private readonly KnowledgeBaseAgentOptions _options;

    public override string Name => "search_nist_controls";

    public override string Description =>
        "Search for NIST 800-53 controls by topic, keyword, or control family. " +
        "Use to find controls related to a specific security concern (e.g., 'encryption', 'authentication', 'logging'). " +
        "Returns matching controls with brief descriptions.";

    public NistControlSearchTool(
        ILogger<NistControlSearchTool> logger,
        KnowledgeBaseStateAccessors stateAccessors,
        INistControlsService nistControlsService,
        IOptions<KnowledgeBaseAgentOptions> options) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _nistControlsService = nistControlsService ?? throw new ArgumentNullException(nameof(nistControlsService));
        _options = options?.Value ?? new KnowledgeBaseAgentOptions();

        Parameters.Add(new ToolParameter(
            name: "search_term",
            description: "Topic, keyword, or phrase to search for (e.g., 'encryption', 'multi-factor authentication', 'audit logging')",
            required: true));

        Parameters.Add(new ToolParameter(
            name: "family",
            description: "Optional control family to filter by (e.g., 'AC', 'AU', 'IA', 'SC', 'CM')",
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
        var family = GetOptionalString(arguments, "family")?.ToUpperInvariant();
        var maxResults = GetOptionalInt(arguments, "max_results") ?? 10;

        Logger.LogInformation("Searching NIST controls for '{SearchTerm}' (family: {Family})", searchTerm, family ?? "all");

        try
        {
            // Track the query
            await _stateAccessors.SetLastQueryAsync("system", searchTerm, "nist_search", cancellationToken);

            // Search for controls
            var results = await _nistControlsService.SearchControlsAsync(searchTerm, cancellationToken);

            // Filter by family if specified
            if (!string.IsNullOrEmpty(family))
            {
                results = results.Where(c => c.Id?.StartsWith(family, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            // Limit results
            var limitedResults = results.Take(maxResults).ToList();

            if (!limitedResults.Any())
            {
                return ToJson(new
                {
                    success = true,
                    message = $"No NIST 800-53 controls found matching '{searchTerm}'.",
                    suggestions = new[]
                    {
                        "Try broader search terms",
                        "Search by control family (AC, AU, IA, SC, CM, SI)",
                        "Use 'explain_nist_control' if you know the control ID"
                    }
                });
            }

            var controlSummaries = limitedResults.Select(c => new
            {
                id = c.Id,
                title = c.Title,
                family = c.Id?.Split('-')[0],
                briefDescription = GetBriefDescription(c)
            }).ToList();

            return ToJson(new
            {
                success = true,
                searchTerm,
                family = family ?? "all",
                totalFound = results.Count(),
                resultsReturned = limitedResults.Count,
                controls = controlSummaries
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching NIST controls for '{SearchTerm}'", searchTerm);
            return ToJson(new
            {
                success = false,
                error = $"Error searching controls: {ex.Message}"
            });
        }
    }

    private static string GetBriefDescription(Core.Models.Compliance.NistControl control)
    {
        var statement = control.Parts?
            .FirstOrDefault(p => p.Name?.Equals("statement", StringComparison.OrdinalIgnoreCase) == true);

        var prose = statement?.Prose ?? "";
        if (prose.Length > 200)
        {
            prose = prose.Substring(0, 197) + "...";
        }
        return prose;
    }
}
