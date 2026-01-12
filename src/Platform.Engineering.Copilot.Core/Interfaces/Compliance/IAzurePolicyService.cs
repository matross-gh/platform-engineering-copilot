using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Service for evaluating Azure policies and compliance requirements for infrastructure provisioning.
/// Provides pre-flight policy checks without requiring MCP-specific types.
/// </summary>
public interface IAzurePolicyService
{
    /// <summary>
    /// Evaluates pre-flight policies for an infrastructure provisioning request.
    /// Returns policy violations and compliance decision before deployment.
    /// </summary>
    /// <param name="request">The infrastructure provisioning request to evaluate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy evaluation result with violations and decision</returns>
    Task<PolicyEvaluationResult> EvaluatePreFlightPoliciesAsync(
        InfrastructureProvisioningRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates policies for a specific Azure resource by ID.
    /// </summary>
    /// <param name="resourceId">The Azure resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy evaluation result for the resource</returns>
    Task<PolicyEvaluationResult> EvaluateResourcePoliciesAsync(
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of applicable policies for a resource type in a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID</param>
    /// <param name="resourceType">The Azure resource type (e.g., Microsoft.Storage/storageAccounts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applicable Azure policy evaluations</returns>
    Task<List<AzurePolicyEvaluation>> GetApplicablePoliciesAsync(
        string subscriptionId,
        string resourceType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of policy evaluation containing violations and governance decision
/// </summary>
public class PolicyEvaluationResult
{
    /// <summary>
    /// Whether the evaluation passed all policy checks
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// The governance policy decision based on evaluation
    /// </summary>
    public GovernancePolicyDecision PolicyDecision { get; set; } = GovernancePolicyDecision.Unknown;

    /// <summary>
    /// List of policy violations found during evaluation
    /// </summary>
    public List<PolicyViolation> PolicyViolations { get; set; } = new();

    /// <summary>
    /// Human-readable messages about the evaluation
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// When the evaluation was performed
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identifier of the evaluating service/engine
    /// </summary>
    public string EvaluatedBy { get; set; } = "AzurePolicyService";
}
