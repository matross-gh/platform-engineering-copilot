using Platform.Engineering.Copilot.Core.Models.Compliance;
using Platform.Engineering.Copilot.Core.Models.KnowledgeBase;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Service for validating STIG (Security Technical Implementation Guide) compliance
/// across Azure resources. Provides STIG-specific validation logic for different
/// service types (Network, Storage, Compute, Database, etc.)
/// </summary>
public interface IStigValidationService
{
    /// <summary>
    /// Validates all STIGs for a specific NIST control family
    /// </summary>
    /// <param name="subscriptionId">Azure subscription ID</param>
    /// <param name="resourceGroupName">Optional resource group filter</param>
    /// <param name="family">NIST control family (e.g., "AC", "SC", "AU")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of compliance findings for STIGs in the control family</returns>
    Task<List<AtoFinding>> ValidateFamilyStigsAsync(
        string subscriptionId,
        string? resourceGroupName,
        string family,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a specific STIG control
    /// </summary>
    /// <param name="subscriptionId">Azure subscription ID</param>
    /// <param name="resourceGroupName">Optional resource group filter</param>
    /// <param name="stig">STIG control to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of compliance findings for the STIG</returns>
    Task<List<AtoFinding>> ValidateStigComplianceAsync(
        string subscriptionId,
        string? resourceGroupName,
        StigControl stig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of supported STIG service types
    /// </summary>
    /// <returns>List of supported STIG service types</returns>
    Task<IReadOnlyList<StigServiceType>> GetSupportedServiceTypesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific STIG is supported for automated validation
    /// </summary>
    /// <param name="stigId">STIG ID (e.g., "V-219187")</param>
    /// <returns>True if the STIG has automated validation support</returns>
    Task<bool> IsStigSupportedAsync(
        string stigId,
        CancellationToken cancellationToken = default);
}
