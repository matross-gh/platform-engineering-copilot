using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Repository for NIST control remediation steps specific to Azure resources.
/// Provides remediation guidance, automation availability, and required actions per control.
/// </summary>
public interface INistRemediationStepsService
{
    /// <summary>
    /// Gets detailed remediation steps for a specific NIST control
    /// </summary>
    /// <param name="controlId">NIST control ID (e.g., "AC-2", "SC-7")</param>
    /// <returns>Remediation steps definition or null if not found</returns>
    Task<RemediationStepsDefinition?> GetRemediationStepsAsync(string controlId);

    /// <summary>
    /// Gets available remediation actions for a control and resource type
    /// </summary>
    /// <param name="controlId">NIST control ID</param>
    /// <param name="resourceType">Azure resource type (optional filter)</param>
    /// <returns>List of available remediation actions</returns>
    Task<IReadOnlyList<RemediationAction>> GetAvailableActionsAsync(string controlId, string resourceType = "");

    /// <summary>
    /// Checks if a control supports automation
    /// </summary>
    /// <param name="controlId">NIST control ID</param>
    /// <returns>True if automated remediation is available</returns>
    Task<bool> SupportsAutomationAsync(string controlId);

    /// <summary>
    /// Gets all controls that support automation
    /// </summary>
    /// <returns>List of control IDs with automation support</returns>
    Task<IReadOnlyList<string>> GetAutomatedControlsAsync();
}

/// <summary>
/// Detailed remediation steps for a NIST control
/// </summary>
public record RemediationStepsDefinition
{
    public required string ControlId { get; init; }
    public required string Priority { get; init; }
    public required TimeSpan EstimatedEffort { get; init; }
    public required bool IsAutomated { get; init; }
    public required IReadOnlyList<RemediationStep> Steps { get; init; }
    public required IReadOnlyList<string> ValidationSteps { get; init; }
    public IReadOnlyList<string> ApplicableResourceTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<RemediationAction> AvailableActions { get; init; } = Array.Empty<RemediationAction>();
}
