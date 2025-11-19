using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Compliance.Core.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services.Compliance;

/// <summary>
/// Service for integrating Microsoft Defender for Cloud findings into compliance assessments
/// Maps DFC security recommendations to NIST 800-53 controls
/// </summary>

public class DefenderForCloudService : IDefenderForCloudService
{
    private readonly ILogger<DefenderForCloudService> _logger;
    private readonly TokenCredential _credential;
    private readonly DefenderForCloudOptions _options;
    
    // NIST 800-53 control mapping for common Defender findings
    private static readonly Dictionary<string, string[]> DefenderToNistMapping = new()
    {
        // Identity & Access
        ["MFARequired"] = new[] { "AC-2", "IA-2", "IA-5" },
        ["AdminAccountsRestricted"] = new[] { "AC-2", "AC-6" },
        ["ServicePrincipalsProtected"] = new[] { "AC-2", "IA-2" },
        ["PasswordPolicyEnforced"] = new[] { "IA-5" },
        
        // Network Security
        ["NetworkSecurityGroupsMissing"] = new[] { "SC-7", "AC-4" },
        ["PublicIPRestricted"] = new[] { "SC-7", "AC-17" },
        ["PrivateEndpointEnabled"] = new[] { "SC-7" },
        ["JITAccessEnabled"] = new[] { "AC-17", "SC-7" },
        
        // Data Protection
        ["EncryptionAtRest"] = new[] { "SC-28", "SC-13" },
        ["EncryptionInTransit"] = new[] { "SC-8", "SC-13" },
        ["BackupsEnabled"] = new[] { "CP-9" },
        ["KeyVaultUsed"] = new[] { "SC-12", "SC-13" },
        
        // Monitoring & Logging
        ["DiagnosticLogsEnabled"] = new[] { "AU-2", "AU-3", "AU-12" },
        ["LogAnalyticsConfigured"] = new[] { "AU-6", "SI-4" },
        ["SecurityAlertsEnabled"] = new[] { "SI-4", "IR-4" },
        
        // Vulnerability Management
        ["VulnerabilityAssessmentEnabled"] = new[] { "RA-5", "SI-2" },
        ["SecurityUpdatesApplied"] = new[] { "SI-2" },
        ["EndpointProtectionInstalled"] = new[] { "SI-3" },
        
        // Configuration Management
        ["SecureConfigurationApplied"] = new[] { "CM-6", "CM-7" },
        ["AdaptiveApplicationControlsEnabled"] = new[] { "CM-7", "SC-18" },
        ["DiskEncryptionEnabled"] = new[] { "SC-28" }
    };

    public DefenderForCloudService(
        ILogger<DefenderForCloudService> logger,
        TokenCredential credential,
        IOptions<ComplianceAgentOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _options = options?.Value?.DefenderForCloud ?? new DefenderForCloudOptions();
    }

    public async Task<List<DefenderFinding>> GetSecurityAssessmentsAsync(
        string subscriptionId,
        string? resourceGroupName = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Defender for Cloud assessments for subscription {SubscriptionId}", 
            subscriptionId);

        var findings = new List<DefenderFinding>();

        try
        {
            // NOTE: Azure.ResourceManager.SecurityCenter 1.2.0-beta.6 API has changed
            // The GetSecurityAssessments() extension method is not available in this version
            // For now, we fall back to manual scanning - future implementation will use
            // the correct beta.6 API once documentation is available
            
            _logger.LogWarning(
                "Defender for Cloud integration temporarily unavailable in beta.6 API. " +
                "Falling back to manual compliance scanning for subscription {SubscriptionId}. " +
                "This will be updated once the stable API is released.", 
                subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Defender for Cloud assessments");
            // Don't throw - allow fallback to manual scanning
        }

        return findings;
    }

    public List<AtoFinding> MapDefenderFindingsToNistControls(
        List<DefenderFinding> defenderFindings,
        string subscriptionId)
    {
        _logger.LogInformation("Mapping {Count} Defender findings to NIST controls", defenderFindings.Count);

        var nistFindings = new List<AtoFinding>();

        foreach (var defenderFinding in defenderFindings)
        {
            // Determine which NIST controls this finding maps to
            var nistControls = DetermineNistControls(defenderFinding);

            foreach (var controlId in nistControls)
            {
                var finding = new AtoFinding
                {
                    Id = $"DFC-{Guid.NewGuid():N}",
                    Title = $"{controlId} - {defenderFinding.DisplayName}",
                    Description = defenderFinding.Description ?? defenderFinding.DisplayName,
                    Severity = MapDefenderSeverityToAto(defenderFinding.Severity),
                    ComplianceStatus = defenderFinding.Status == "Healthy" 
                        ? AtoComplianceStatus.Compliant 
                        : AtoComplianceStatus.NonCompliant,
                    ResourceId = defenderFinding.AffectedResource ?? string.Empty,
                    ResourceType = ExtractResourceType(defenderFinding.AffectedResource),
                    RemediationGuidance = defenderFinding.RemediationSteps ?? "See Defender for Cloud for detailed remediation steps",
                    IsAutoRemediable = DetermineIfAutoRemediable(defenderFinding),
                    AffectedNistControls = nistControls.ToList(),
                    DetectedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Source"] = "Defender for Cloud",
                        ["DefenderFindingId"] = defenderFinding.Id,
                        ["DefenderSeverity"] = defenderFinding.Severity,
                        ["AssessmentType"] = defenderFinding.AssessmentType ?? "Unknown"
                    }
                };

                nistFindings.Add(finding);
            }
        }

        _logger.LogInformation("Mapped to {Count} NIST control findings", nistFindings.Count);
        return nistFindings;
    }

    public async Task<DefenderSecureScore> GetSecureScoreAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Defender secure score for subscription {SubscriptionId}", 
            subscriptionId);

        try
        {
            // Note: Secure Score API requires specific permissions and may not be available
            // in all environments. For now, we calculate an approximation from assessments.
            var findings = await GetSecurityAssessmentsAsync(subscriptionId, null, cancellationToken);
            
            // Calculate score based on compliant vs non-compliant assessments
            var totalFindings = findings.Count;
            var healthyFindings = findings.Count(f => f.Status == "Healthy");
            var percentage = totalFindings > 0 ? ((double)healthyFindings / totalFindings) * 100 : 0;
            
            return new DefenderSecureScore
            {
                CurrentScore = healthyFindings,
                MaxScore = totalFindings,
                Percentage = percentage,
                SubscriptionId = subscriptionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Defender secure score, returning zero score");
            
            // Return zero score to allow assessment to continue
            return new DefenderSecureScore
            {
                CurrentScore = 0,
                MaxScore = 0,
                Percentage = 0,
                SubscriptionId = subscriptionId
            };
        }
    }

    private string[] DetermineNistControls(DefenderFinding finding)
    {
        // Try to match finding to known patterns
        foreach (var mapping in DefenderToNistMapping)
        {
            if (finding.DisplayName?.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase) == true ||
                finding.Description?.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase) == true)
            {
                return mapping.Value;
            }
        }

        // Default fallback based on finding characteristics
        if (finding.DisplayName?.Contains("MFA", StringComparison.OrdinalIgnoreCase) == true)
            return new[] { "AC-2", "IA-2" };
        
        if (finding.DisplayName?.Contains("Encryption", StringComparison.OrdinalIgnoreCase) == true)
            return new[] { "SC-28", "SC-13" };
        
        if (finding.DisplayName?.Contains("Network", StringComparison.OrdinalIgnoreCase) == true)
            return new[] { "SC-7" };
        
        if (finding.DisplayName?.Contains("Logging", StringComparison.OrdinalIgnoreCase) == true ||
            finding.DisplayName?.Contains("Audit", StringComparison.OrdinalIgnoreCase) == true)
            return new[] { "AU-2", "AU-3" };

        // Generic fallback
        return new[] { "CM-6" }; // Configuration Settings
    }

    private AtoFindingSeverity MapDefenderSeverityToAto(string defenderSeverity)
    {
        return defenderSeverity?.ToLowerInvariant() switch
        {
            "critical" => AtoFindingSeverity.Critical,
            "high" => AtoFindingSeverity.High,
            "medium" => AtoFindingSeverity.Medium,
            "low" => AtoFindingSeverity.Low,
            _ => AtoFindingSeverity.Low
        };
    }

    private string ExtractResourceType(string? resourceId)
    {
        if (string.IsNullOrEmpty(resourceId))
            return "Unknown";

        // Extract from resource ID: /subscriptions/.../providers/Microsoft.Compute/virtualMachines/...
        var parts = resourceId.Split('/');
        var providerIndex = Array.IndexOf(parts, "providers");
        
        if (providerIndex >= 0 && providerIndex + 2 < parts.Length)
        {
            return $"{parts[providerIndex + 1]}/{parts[providerIndex + 2]}";
        }

        return "Unknown";
    }

    private bool DetermineIfAutoRemediable(DefenderFinding finding)
    {
        // Some Defender findings can be auto-remediated via Azure Policy
        var autoRemediablePatterns = new[]
        {
            "diagnostic",
            "encryption",
            "backup",
            "update",
            "security agent"
        };

        return autoRemediablePatterns.Any(pattern => 
            finding.DisplayName?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true);
    }
}
