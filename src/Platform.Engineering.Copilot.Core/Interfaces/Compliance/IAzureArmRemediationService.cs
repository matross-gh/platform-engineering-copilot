using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Service for executing Azure ARM-based remediation operations.
/// Provides a generic pattern for updating Azure resource properties via ARM API.
/// </summary>
public interface IAzureArmRemediationService
{
    /// <summary>
    /// Updates Azure resource properties using a generic update pattern
    /// </summary>
    /// <param name="resourceId">Full Azure resource ID</param>
    /// <param name="propertyUpdater">Function that modifies resource properties</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Description of changes applied</returns>
    Task<string> UpdateResourcePropertiesAsync(
        string resourceId, 
        Func<Dictionary<string, object>, Dictionary<string, object>> propertyUpdater, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates diagnostic settings for a resource
    /// </summary>
    Task<string> CreateDiagnosticSettingsAsync(
        string resourceId, 
        DiagnosticSettingsConfig config, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates alert rules for a resource
    /// </summary>
    Task<string> CreateAlertRuleAsync(
        string resourceId, 
        AlertRuleConfig config, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an Azure Policy assignment to a resource or scope
    /// </summary>
    Task<string> ApplyPolicyAssignmentAsync(
        string subscriptionId,
        string resourceId, 
        string? policyDefinitionId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for diagnostic settings
/// </summary>
public record DiagnosticSettingsConfig
{
    public string? WorkspaceId { get; init; }
    public List<LogCategory> Logs { get; init; } = new();
    public List<MetricCategory> Metrics { get; init; } = new();
}

public record LogCategory
{
    public required string Category { get; init; }
    public bool Enabled { get; init; } = true;
    public int RetentionDays { get; init; } = 90;
}

public record MetricCategory
{
    public required string Category { get; init; }
    public bool Enabled { get; init; } = true;
    public int RetentionDays { get; init; } = 90;
}

/// <summary>
/// Configuration for alert rules
/// </summary>
public record AlertRuleConfig
{
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required int Severity { get; init; }
    public required string Query { get; init; }
    public required string EvaluationFrequency { get; init; }
    public required string WindowSize { get; init; }
    public string? WorkspaceId { get; init; }
}
