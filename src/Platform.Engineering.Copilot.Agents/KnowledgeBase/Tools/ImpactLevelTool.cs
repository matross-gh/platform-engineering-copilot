using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for explaining impact levels (IL2-IL6) and FedRAMP baselines.
/// Provides guidance on selecting appropriate impact levels for systems.
/// </summary>
public class ImpactLevelTool : BaseTool
{
    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly IImpactLevelService _impactLevelService;

    public override string Name => "explain_impact_level";

    public override string Description =>
        "Explain DoD Impact Levels (IL2-IL6) and FedRAMP baselines. " +
        "Provides requirements, use cases, and guidance for selecting appropriate impact levels. " +
        "Use for questions like: 'What is IL5?', 'Compare FedRAMP High vs IL5', 'What IL level do I need?'.";

    public ImpactLevelTool(
        ILogger<ImpactLevelTool> logger,
        KnowledgeBaseStateAccessors stateAccessors,
        IImpactLevelService impactLevelService) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _impactLevelService = impactLevelService ?? throw new ArgumentNullException(nameof(impactLevelService));

        Parameters.Add(new ToolParameter(
            name: "level",
            description: "Impact level to explain: 'IL2', 'IL4', 'IL5', 'IL6', 'FedRAMP-Low', 'FedRAMP-Moderate', 'FedRAMP-High', or 'compare' for comparison",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var level = GetOptionalString(arguments, "level")?.ToUpperInvariant();

        Logger.LogInformation("Explaining impact level: {Level}", level ?? "overview");

        try
        {
            // Track the query
            await _stateAccessors.SetLastQueryAsync("system", level ?? "impact_levels", "impact_level", cancellationToken);

            if (string.IsNullOrEmpty(level) || level == "COMPARE" || level == "ALL")
            {
                return await _impactLevelService.GetImpactLevelComparisonAsync(cancellationToken);
            }

            // Normalize level format
            var normalizedLevel = level switch
            {
                "IL2" or "IL-2" or "2" => "IL2",
                "IL4" or "IL-4" or "4" => "IL4",
                "IL5" or "IL-5" or "5" => "IL5",
                "IL6" or "IL-6" or "6" => "IL6",
                "FEDRAMP-LOW" or "FEDRAMPLOW" or "LOW" => "FedRAMP-Low",
                "FEDRAMP-MODERATE" or "FEDRAMPMODERATE" or "MODERATE" => "FedRAMP-Moderate",
                "FEDRAMP-HIGH" or "FEDRAMPHIGH" or "HIGH" => "FedRAMP-High",
                _ => level
            };

            var impactLevel = await _impactLevelService.GetImpactLevelAsync(normalizedLevel, cancellationToken);

            if (impactLevel == null)
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Impact level '{level}' not found.",
                    availableLevels = new[] { "IL2", "IL4", "IL5", "IL6", "FedRAMP-Low", "FedRAMP-Moderate", "FedRAMP-High" }
                });
            }

            var requirementsText = impactLevel.Requirements.Any()
                ? string.Join("\n", impactLevel.Requirements.Select(r => $"- {r}"))
                : "See NIST baseline for detailed requirements.";

            var nistBaselineText = impactLevel.NistBaseline.Any()
                ? string.Join("\n", impactLevel.NistBaseline.Select(b => $"- {b}"))
                : "Refer to NIST 800-53 for control baseline.";

            var mandatoryControlsText = impactLevel.MandatoryControls.Any()
                ? string.Join(", ", impactLevel.MandatoryControls)
                : "See DoD Cloud Computing SRG for mandatory controls.";

            return $@"# {impactLevel.Name}

**Level:** {impactLevel.Level}

## Description

{impactLevel.Description}

## Key Requirements

{requirementsText}

## NIST Baseline

{nistBaselineText}

## Mandatory Controls

{mandatoryControlsText}

## Azure Configuration

{(impactLevel.AzureConfigurations.Any() 
    ? string.Join("\n", impactLevel.AzureConfigurations.Select(kv => $"- **{kv.Key}**: {kv.Value}"))
    : "Use Azure Government regions with appropriate compliance configurations.")}

---
*Use this guidance to select the appropriate impact level for your system based on data sensitivity and mission requirements.*";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error explaining impact level: {Level}", level);
            return ToJson(new
            {
                success = false,
                error = $"Error retrieving impact level information: {ex.Message}"
            });
        }
    }
}
