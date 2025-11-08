using System;
using System.Collections.Generic;

namespace Platform.Engineering.Copilot.Core.Models.Infrastructure;

/// <summary>
/// Options for configuring infrastructure remediation behavior
/// </summary>
public class InfrastructureRemediationOptions
{
    /// <summary>
    /// Maximum acceptable risk level for automated remediation
    /// </summary>
    public RemediationRiskLevel MaxRiskLevel { get; set; } = RemediationRiskLevel.Medium;

    /// <summary>
    /// Whether to require manual approval for high-risk changes
    /// </summary>
    public bool RequireApprovalForHighRisk { get; set; } = true;

    /// <summary>
    /// Timeout for individual remediation actions
    /// </summary>
    public TimeSpan ActionTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximum number of retry attempts for failed actions
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to create a rollback plan automatically
    /// </summary>
    public bool CreateRollbackPlan { get; set; } = true;

    /// <summary>
    /// Tags to add to resources during remediation for tracking
    /// </summary>
    public Dictionary<string, string> RemediationTags { get; set; } = new()
    {
        { "RemediationDate", DateTime.UtcNow.ToString("yyyy-MM-dd") },
        { "RemediationType", "AutoRemediation" },
        { "RemediationEngine", "PlatformEngineeringCopilot" }
    };

    /// <summary>
    /// Whether to send notifications during remediation
    /// </summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>
    /// Email addresses to notify during remediation
    /// </summary>
    public List<string> NotificationEmails { get; set; } = new();

    /// <summary>
    /// Whether to validate remediation immediately after execution
    /// </summary>
    public bool ValidateImmediately { get; set; } = true;

    /// <summary>
    /// Azure regions where remediation is allowed
    /// </summary>
    public List<string> AllowedRegions { get; set; } = new();

    /// <summary>
    /// Resource types that are excluded from auto-remediation
    /// </summary>
    public List<string> ExcludedResourceTypes { get; set; } = new();
}

/// <summary>
/// Result of executing an infrastructure remediation plan
/// </summary>
public class InfrastructureRemediationResult
{
    /// <summary>
    /// Reference to the original plan that was executed
    /// </summary>
    public InfrastructureRemediationPlan Plan { get; set; } = new();

    /// <summary>
    /// Unique identifier for this execution result
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Overall success status of the remediation
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Whether this was a dry run (simulation only)
    /// </summary>
    public bool WasDryRun { get; set; }

    /// <summary>
    /// When the remediation execution started
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the remediation execution completed
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total duration of the remediation execution
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;

    /// <summary>
    /// Results for each individual action that was executed
    /// </summary>
    public List<InfrastructureActionResult> ActionResults { get; set; } = new();

    /// <summary>
    /// Any errors that occurred during execution
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warnings generated during execution
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Information messages from execution
    /// </summary>
    public List<string> Information { get; set; } = new();

    /// <summary>
    /// Azure Resource Manager deployment ID if created
    /// </summary>
    public string? ArmDeploymentId { get; set; }

    /// <summary>
    /// Changes made to the resource during remediation
    /// </summary>
    public Dictionary<string, object> ChangesApplied { get; set; } = new();

    /// <summary>
    /// Rollback information if remediation needs to be undone
    /// </summary>
    public InfrastructureRollbackInfo? RollbackInfo { get; set; }
}

/// <summary>
/// Result of executing a single infrastructure remediation action
/// </summary>
public class InfrastructureActionResult
{
    /// <summary>
    /// Reference to the action that was executed
    /// </summary>
    public InfrastructureRemediationAction Action { get; set; } = new();

    /// <summary>
    /// Whether the action executed successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTTP status code returned from the API call
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Response body from the API call
    /// </summary>
    public string? ApiResponse { get; set; }

    /// <summary>
    /// When the action was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the action execution
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Result of validation if performed
    /// </summary>
    public InfrastructureValidationResult? ValidationResult { get; set; }
}

/// <summary>
/// Information needed to rollback an infrastructure remediation
/// </summary>
public class InfrastructureRollbackInfo
{
    /// <summary>
    /// Original values before remediation
    /// </summary>
    public Dictionary<string, object> OriginalValues { get; set; } = new();

    /// <summary>
    /// Actions needed to rollback the changes
    /// </summary>
    public List<InfrastructureRemediationAction> RollbackActions { get; set; } = new();

    /// <summary>
    /// When the rollback information was captured
    /// </summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}