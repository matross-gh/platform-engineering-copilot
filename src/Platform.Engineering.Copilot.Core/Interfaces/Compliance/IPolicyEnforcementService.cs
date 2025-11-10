using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Service for enforcing IL5/IL6 compliance policies on infrastructure templates
/// Extends IAzurePolicyService with IL-specific validation and template generation
/// </summary>
public interface IPolicyEnforcementService
{
    /// <summary>
    /// Get policy configuration for a specific Impact Level
    /// </summary>
    Task<ImpactLevelPolicy> GetPolicyForImpactLevelAsync(
        ImpactLevel level, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a template against IL policies (Bicep, Terraform, ARM, Kubernetes)
    /// </summary>
    Task<PolicyValidationResult> ValidateTemplateAsync(
        TemplateValidationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate IL-compliant template with policies baked in
    /// Returns hardened template with encryption, networking, identity controls pre-configured
    /// </summary>
    Task<IlCompliantTemplate> GenerateCompliantTemplateAsync(
        IlTemplateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply IL policies to existing template (hardens existing IaC)
    /// </summary>
    Task<string> ApplyPoliciesToTemplateAsync(
        string templateContent,
        TemplateType type,
        ImpactLevel targetLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available policy rules for an Impact Level
    /// Filtered by category if specified (Encryption, Networking, Identity, etc.)
    /// </summary>
    Task<List<PolicyRule>> GetPolicyRulesAsync(
        ImpactLevel level,
        PolicyCategory? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate specific resource configuration against IL policies
    /// Used for pre-deployment validation
    /// </summary>
    Task<PolicyValidationResult> ValidateResourceConfigurationAsync(
        string resourceType,
        Dictionary<string, object> configuration,
        ImpactLevel targetLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get remediation guidance for policy violations
    /// Returns IaC-specific fix instructions (Bicep/Terraform)
    /// </summary>
    Task<string> GetRemediationGuidanceAsync(
        PolicyViolation violation,
        TemplateType templateType,
        CancellationToken cancellationToken = default);
}
