using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

// Alias the Azure namespace to avoid conflict with Platform.Engineering.Copilot.Agents.Compliance.Services.Azure
using AzureCore = global::Azure.Core;
using AzureRM = global::Azure.ResourceManager;
using AzureResources = global::Azure.ResourceManager.Resources;
using AzureSecurityCenter = global::Azure.ResourceManager.SecurityCenter;

namespace Platform.Engineering.Copilot.Agents.Compliance.Services.Compliance;

/// <summary>
/// Service for integrating Microsoft Defender for Cloud findings into compliance assessments
/// Maps DFC security recommendations to NIST 800-53 controls
/// </summary>

public class DefenderForCloudService : IDefenderForCloudService
{
    private readonly ILogger<DefenderForCloudService> _logger;
    private readonly AzureCore.TokenCredential _credential;
    private readonly DefenderForCloudOptions _options;
    private readonly IMemoryCache _cache;
    private readonly AzureRM.ArmClient _armClient;
    
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

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);
    private const string CacheKeyPrefix = "DFC_";

    public DefenderForCloudService(
        ILogger<DefenderForCloudService> logger,
        AzureCore.TokenCredential credential,
        IOptions<ComplianceAgentOptions> options,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _options = options?.Value?.DefenderForCloud ?? new DefenderForCloudOptions();
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _armClient = new AzureRM.ArmClient(credential);
    }

    public async Task<List<DefenderFinding>> GetSecurityAssessmentsAsync(
        string subscriptionId,
        string? resourceGroupName = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Defender for Cloud assessments for subscription {SubscriptionId}", 
            subscriptionId);

        // Check cache first
        var cacheKey = $"{CacheKeyPrefix}Assessments_{subscriptionId}_{resourceGroupName ?? "all"}";
        if (_cache.TryGetValue(cacheKey, out List<DefenderFinding>? cachedFindings) && cachedFindings != null)
        {
            _logger.LogDebug("Returning {Count} cached DFC findings for subscription {SubscriptionId}", 
                cachedFindings.Count, subscriptionId);
            return cachedFindings;
        }

        var findings = new List<DefenderFinding>();

        try
        {
            var subscriptionResource = _armClient.GetSubscriptionResource(
                AzureResources.SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            // Use the correct beta.6 API to get security assessments
            // The API now requires a scope parameter
            var scope = new AzureCore.ResourceIdentifier($"/subscriptions/{subscriptionId}");
            
            await foreach (var assessment in AzureSecurityCenter.SecurityCenterExtensions.GetSecurityAssessmentsAsync(_armClient, scope, cancellationToken: cancellationToken))
            {
                // Filter by resource group if specified
                if (!string.IsNullOrEmpty(resourceGroupName))
                {
                    var resourceId = assessment.Data.Id?.ToString() ?? "";
                    if (!resourceId.Contains($"/resourceGroups/{resourceGroupName}/", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                var finding = new DefenderFinding
                {
                    Id = assessment.Data.Id?.ToString() ?? Guid.NewGuid().ToString(),
                    DisplayName = assessment.Data.DisplayName ?? "Unknown Assessment",
                    Severity = assessment.Data.Metadata?.Severity.ToString() ?? "Low",
                    Status = assessment.Data.Status?.Code.ToString() ?? "Unknown",
                    Description = assessment.Data.Metadata?.Description,
                    RemediationSteps = assessment.Data.Metadata?.RemediationDescription,
                    AffectedResource = assessment.Data.Id?.ToString(),
                    AssessmentType = assessment.Data.Metadata?.AssessmentType.ToString() ?? "BuiltIn"
                };

                findings.Add(finding);
            }

            _logger.LogInformation("Retrieved {Count} Defender for Cloud assessments for subscription {SubscriptionId}", 
                findings.Count, subscriptionId);

            // Cache results
            var cacheDuration = TimeSpan.FromMinutes(_options.CacheDurationMinutes > 0 
                ? _options.CacheDurationMinutes 
                : 60);
            _cache.Set(cacheKey, findings, cacheDuration);
        }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogWarning("Access denied to Defender for Cloud for subscription {SubscriptionId}. " +
                "Ensure the identity has 'Security Reader' role.", subscriptionId);
        }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Defender for Cloud not found or not enabled for subscription {SubscriptionId}", 
                subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Defender for Cloud assessments for subscription {SubscriptionId}", 
                subscriptionId);
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

        // Check cache first
        var cacheKey = $"{CacheKeyPrefix}SecureScore_{subscriptionId}";
        if (_cache.TryGetValue(cacheKey, out DefenderSecureScore? cachedScore) && cachedScore != null)
        {
            _logger.LogDebug("Returning cached secure score for subscription {SubscriptionId}", subscriptionId);
            return cachedScore;
        }

        try
        {
            var subscriptionResource = _armClient.GetSubscriptionResource(
                AzureResources.SubscriptionResource.CreateResourceIdentifier(subscriptionId));

            // Try to get secure scores using the Security Center extension
            var secureScoreCollection = AzureSecurityCenter.SecurityCenterExtensions.GetSecureScores(subscriptionResource);
            
            await foreach (var score in secureScoreCollection.GetAllAsync(cancellationToken: cancellationToken))
            {
                // The "ascScore" is the default overall secure score
                if (score.Data.Name == "ascScore")
                {
                    // In beta.6, properties may have changed - use direct properties
                    var currentScore = score.Data.Current ?? 0;
                    var maxScore = score.Data.Max ?? 0;
                    var percentage = maxScore > 0 ? (currentScore / maxScore) * 100 : 0;
                    
                    var result = new DefenderSecureScore
                    {
                        CurrentScore = currentScore,
                        MaxScore = maxScore,
                        Percentage = percentage,
                        SubscriptionId = subscriptionId
                    };

                    // Cache the result
                    var cacheDuration = TimeSpan.FromMinutes(_options.CacheDurationMinutes > 0 
                        ? _options.CacheDurationMinutes 
                        : 60);
                    _cache.Set(cacheKey, result, cacheDuration);

                    _logger.LogInformation("Secure score for subscription {SubscriptionId}: {Current}/{Max} ({Percentage}%)",
                        subscriptionId, result.CurrentScore, result.MaxScore, result.Percentage);

                    return result;
                }
            }

            // Fallback: Calculate from assessments if secure score not available
            _logger.LogDebug("Secure score not directly available, calculating from assessments");
            var findings = await GetSecurityAssessmentsAsync(subscriptionId, null, cancellationToken);
            
            var totalFindings = findings.Count;
            var healthyFindings = findings.Count(f => f.Status == "Healthy");
            var percentageCalc = totalFindings > 0 ? ((double)healthyFindings / totalFindings) * 100 : 100;
            
            var fallbackScore = new DefenderSecureScore
            {
                CurrentScore = healthyFindings,
                MaxScore = totalFindings,
                Percentage = Math.Round(percentageCalc, 1),
                SubscriptionId = subscriptionId
            };

            _cache.Set(cacheKey, fallbackScore, TimeSpan.FromMinutes(30));
            return fallbackScore;
        }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogWarning("Access denied to secure scores for subscription {SubscriptionId}. " +
                "Ensure the identity has 'Security Reader' role.", subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Defender secure score for subscription {SubscriptionId}", 
                subscriptionId);
        }
        
        // Return zero score to allow assessment to continue
        return new DefenderSecureScore
        {
            CurrentScore = 0,
            MaxScore = 0,
            Percentage = 0,
            SubscriptionId = subscriptionId
        };
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
