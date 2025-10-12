using System.ComponentModel.DataAnnotations;

namespace Platform.Engineering.Copilot.Core.Configuration;

/// <summary>
/// Configuration options for governance policy enforcement and compliance
/// </summary>
public class GovernanceOptions
{
    public const string SectionName = "Governance";

    /// <summary>
    /// Whether to enforce governance policies
    /// </summary>
    public bool EnforcePolicies { get; set; } = true;

    /// <summary>
    /// Whether to require approval for high-risk operations
    /// </summary>
    public bool RequireApproval { get; set; } = true;

    /// <summary>
    /// Approval timeout in minutes
    /// </summary>
    public int ApprovalTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// List of approved Azure regions
    /// </summary>
    public List<string> ApprovedRegions { get; set; } = new();

    /// <summary>
    /// Whether to enforce naming conventions
    /// </summary>
    public bool EnforceNamingConventions { get; set; } = true;

    /// <summary>
    /// Whether to enforce tagging requirements
    /// </summary>
    public bool EnforceTagging { get; set; } = true;

    /// <summary>
    /// Required tags for all resources
    /// </summary>
    public List<string> RequiredTags { get; set; } = new() { "Environment", "Owner", "CostCenter" };

    /// <summary>
    /// Whether to log all governance decisions for audit
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Maximum resource cost threshold (USD) for auto-approval
    /// </summary>
    public decimal AutoApprovalCostThreshold { get; set; } = 1000m;
}