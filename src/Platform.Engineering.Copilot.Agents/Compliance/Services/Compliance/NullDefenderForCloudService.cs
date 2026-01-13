using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Services.Compliance;

/// <summary>
/// Null implementation of DefenderForCloudService for when DFC is disabled
/// Provides graceful degradation without breaking existing functionality
/// </summary>
public class NullDefenderForCloudService : IDefenderForCloudService
{
    private readonly ILogger<NullDefenderForCloudService> _logger;

    public NullDefenderForCloudService(ILogger<NullDefenderForCloudService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<List<DefenderFinding>> GetSecurityAssessmentsAsync(
        string subscriptionId,
        string? resourceGroupName = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Defender for Cloud is disabled - returning empty findings");
        return Task.FromResult(new List<DefenderFinding>());
    }

    public List<AtoFinding> MapDefenderFindingsToNistControls(
        List<DefenderFinding> defenderFindings,
        string subscriptionId)
    {
        _logger.LogDebug("Defender for Cloud is disabled - returning empty NIST findings");
        return new List<AtoFinding>();
    }

    public Task<DefenderSecureScore> GetSecureScoreAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Defender for Cloud is disabled - returning zero secure score");
        return Task.FromResult(new DefenderSecureScore
        {
            CurrentScore = 0,
            MaxScore = 0,
            Percentage = 0,
            SubscriptionId = subscriptionId
        });
    }
}
