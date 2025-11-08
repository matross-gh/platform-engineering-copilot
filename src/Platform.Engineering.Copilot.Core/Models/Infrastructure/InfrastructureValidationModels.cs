using System;
using System.Collections.Generic;

namespace Platform.Engineering.Copilot.Core.Models.Infrastructure;

/// <summary>
/// Result of validating an infrastructure remediation
/// </summary>
public class InfrastructureRemediationValidation
{
    /// <summary>
    /// Reference to the remediation result being validated
    /// </summary>
    public InfrastructureRemediationResult RemediationResult { get; set; } = new();

    /// <summary>
    /// Whether the remediation validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether the original compliance finding has been resolved
    /// </summary>
    public bool IsComplianceResolved { get; set; }

    /// <summary>
    /// Updated compliance status after remediation
    /// </summary>
    public string ComplianceStatus { get; set; } = string.Empty;

    /// <summary>
    /// When the validation was performed
    /// </summary>
    public DateTime ValidationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Details of individual validation checks
    /// </summary>
    public List<InfrastructureValidationResult> ValidationResults { get; set; } = new();

    /// <summary>
    /// Any issues found during validation
    /// </summary>
    public List<string> ValidationIssues { get; set; } = new();

    /// <summary>
    /// Recommendations based on validation results
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Next scan date to verify compliance is maintained
    /// </summary>
    public DateTime? NextScanDate { get; set; }
}

/// <summary>
/// Result of a single validation check
/// </summary>
public class InfrastructureValidationResult
{
    /// <summary>
    /// Name of the validation check performed
    /// </summary>
    public string ValidationName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this validation check passed
    /// </summary>
    public bool IsPassed { get; set; }

    /// <summary>
    /// Expected value for the validation
    /// </summary>
    public object? ExpectedValue { get; set; }

    /// <summary>
    /// Actual value found during validation
    /// </summary>
    public object? ActualValue { get; set; }

    /// <summary>
    /// Property path that was validated
    /// </summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Rollback plan for infrastructure remediation
/// </summary>
public class InfrastructureRollbackPlan
{
    /// <summary>
    /// Unique identifier for this rollback plan
    /// </summary>
    public string RollbackPlanId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Actions needed to rollback the remediation
    /// </summary>
    public List<InfrastructureRemediationAction> RollbackActions { get; set; } = new();

    /// <summary>
    /// Original resource configuration before remediation
    /// </summary>
    public Dictionary<string, object> OriginalConfiguration { get; set; } = new();

    /// <summary>
    /// Estimated time to complete rollback
    /// </summary>
    public TimeSpan EstimatedRollbackDuration { get; set; }

    /// <summary>
    /// Whether rollback is possible for this remediation
    /// </summary>
    public bool IsRollbackPossible { get; set; } = true;

    /// <summary>
    /// Reasons why rollback might not be possible
    /// </summary>
    public List<string> RollbackLimitations { get; set; } = new();

    /// <summary>
    /// When this rollback plan was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of executing a rollback operation
/// </summary>
public class InfrastructureRollbackResult
{
    /// <summary>
    /// Reference to the rollback plan that was executed
    /// </summary>
    public InfrastructureRollbackPlan RollbackPlan { get; set; } = new();

    /// <summary>
    /// Whether the rollback was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// When the rollback was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the rollback execution
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Results of individual rollback actions
    /// </summary>
    public List<InfrastructureActionResult> RollbackActionResults { get; set; } = new();

    /// <summary>
    /// Any errors that occurred during rollback
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Final resource state after rollback
    /// </summary>
    public Dictionary<string, object> FinalState { get; set; } = new();
}

/// <summary>
/// Impact assessment for infrastructure remediation
/// </summary>
public class InfrastructureRemediationImpactAssessment
{
    /// <summary>
    /// Overall risk level of the remediation
    /// </summary>
    public RemediationRiskLevel OverallRisk { get; set; }

    /// <summary>
    /// Estimated downtime during remediation
    /// </summary>
    public TimeSpan EstimatedDowntime { get; set; }

    /// <summary>
    /// Services that may be affected during remediation
    /// </summary>
    public List<string> AffectedServices { get; set; } = new();

    /// <summary>
    /// Dependencies that may be impacted
    /// </summary>
    public List<string> ImpactedDependencies { get; set; } = new();

    /// <summary>
    /// Potential risks and their mitigation strategies
    /// </summary>
    public List<RemediationRisk> Risks { get; set; } = new();

    /// <summary>
    /// Recommendations for safe execution
    /// </summary>
    public List<string> SafetyRecommendations { get; set; } = new();

    /// <summary>
    /// Best time windows for executing the remediation
    /// </summary>
    public List<MaintenanceWindow> RecommendedMaintenanceWindows { get; set; } = new();

    /// <summary>
    /// Whether manual approval is recommended
    /// </summary>
    public bool RecommendManualApproval { get; set; }

    /// <summary>
    /// When this assessment was performed
    /// </summary>
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a risk associated with infrastructure remediation
/// </summary>
public class RemediationRisk
{
    /// <summary>
    /// Name/type of the risk
    /// </summary>
    public string RiskType { get; set; } = string.Empty;

    /// <summary>
    /// Description of the risk
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the risk
    /// </summary>
    public RemediationRiskLevel Severity { get; set; }

    /// <summary>
    /// Probability of the risk occurring (0.0 to 1.0)
    /// </summary>
    public double Probability { get; set; }

    /// <summary>
    /// Mitigation strategy for this risk
    /// </summary>
    public string MitigationStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Represents a maintenance window for safe remediation execution
/// </summary>
public class MaintenanceWindow
{
    /// <summary>
    /// Start time of the maintenance window
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the maintenance window
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Time zone of the maintenance window
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Reason why this window is recommended
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Priority of this maintenance window (1 = highest)
    /// </summary>
    public int Priority { get; set; } = 1;
}