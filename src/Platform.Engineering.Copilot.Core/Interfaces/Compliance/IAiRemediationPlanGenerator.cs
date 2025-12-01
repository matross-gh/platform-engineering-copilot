using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Service for AI-enhanced remediation plan generation using Azure OpenAI.
/// Optional service - works gracefully when Azure OpenAI is not configured.
/// </summary>
public interface IAiRemediationPlanGenerator
{
    /// <summary>
    /// Generates an AI-enhanced remediation plan for a finding
    /// </summary>
    Task<RemediationPlan?> GenerateAiEnhancedPlanAsync(
        AtoFinding finding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates AI-powered remediation script (Azure CLI, PowerShell, Terraform)
    /// </summary>
    Task<RemediationScript> GenerateRemediationScriptAsync(
        AtoFinding finding,
        string scriptType = "AzureCLI",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets natural language remediation guidance for engineers
    /// </summary>
    Task<RemediationGuidance> GetRemediationGuidanceAsync(
        AtoFinding finding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prioritizes findings using AI with business context
    /// </summary>
    Task<List<PrioritizedFinding>> PrioritizeFindingsWithAiAsync(
        List<AtoFinding> findings,
        string businessContext = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if AI features are available (Azure OpenAI configured)
    /// </summary>
    bool IsAvailable { get; }
}

/// <summary>
/// AI-generated remediation plan response
/// </summary>
public record AiRemediationPlanResponse
{
    public List<string>? Commands { get; init; }
    public string? Impact { get; init; }
    public List<string>? Risks { get; init; }
    public int EstimatedMinutes { get; init; }
}
