using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Service for integrating Microsoft Defender for Cloud findings into compliance assessments
/// Maps DFC security recommendations to NIST 800-53 controls
/// </summary>
public interface IDefenderForCloudService
{
    /// <summary>
    /// Get all Defender for Cloud security assessments for a subscription
    /// </summary>
    Task<List<DefenderFinding>> GetSecurityAssessmentsAsync(
        string subscriptionId,
        string? resourceGroupName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Map Defender for Cloud findings to NIST 800-53 control violations
    /// </summary>
    List<AtoFinding> MapDefenderFindingsToNistControls(
        List<DefenderFinding> defenderFindings,
        string subscriptionId);

    /// <summary>
    /// Get Defender for Cloud secure score
    /// </summary>
    Task<DefenderSecureScore> GetSecureScoreAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);
}