using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Services.Engines.Policy;

/// <summary>
/// Implementation of IAzurePolicyService that evaluates Azure policies for infrastructure provisioning.
/// Provides pre-flight policy checks and compliance validation without MCP-specific dependencies.
/// </summary>
public class AzurePolicyEngine : IAzurePolicyService
{
    private readonly ILogger<AzurePolicyEngine> _logger;

    // Default required policies for common scenarios
    private static readonly Dictionary<string, List<string>> RequiredPoliciesByResourceType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft.Storage/storageAccounts"] = new() { "storage-https-only", "storage-encryption-required", "storage-public-access-disabled" },
        ["Microsoft.KeyVault/vaults"] = new() { "keyvault-purge-protection", "keyvault-soft-delete", "keyvault-private-endpoint" },
        ["Microsoft.Compute/virtualMachines"] = new() { "vm-managed-disks", "vm-encryption-at-host", "vm-approved-extensions" },
        ["Microsoft.Sql/servers"] = new() { "sql-tde-encryption", "sql-auditing-enabled", "sql-private-endpoint" },
        ["Microsoft.Web/sites"] = new() { "webapp-https-only", "webapp-minimum-tls", "webapp-managed-identity" },
        ["Microsoft.ContainerRegistry/registries"] = new() { "acr-admin-disabled", "acr-private-endpoint", "acr-content-trust" },
        ["Microsoft.ContainerService/managedClusters"] = new() { "aks-private-cluster", "aks-azure-policy", "aks-network-policy" },
    };

    // Severity mapping for policy violations
    private static readonly Dictionary<string, PolicyViolationSeverity> PolicySeverityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["storage-https-only"] = PolicyViolationSeverity.High,
        ["storage-encryption-required"] = PolicyViolationSeverity.Critical,
        ["storage-public-access-disabled"] = PolicyViolationSeverity.High,
        ["keyvault-purge-protection"] = PolicyViolationSeverity.Medium,
        ["keyvault-soft-delete"] = PolicyViolationSeverity.Medium,
        ["keyvault-private-endpoint"] = PolicyViolationSeverity.High,
        ["sql-tde-encryption"] = PolicyViolationSeverity.Critical,
        ["sql-auditing-enabled"] = PolicyViolationSeverity.Medium,
        ["sql-private-endpoint"] = PolicyViolationSeverity.High,
    };

    public AzurePolicyEngine(ILogger<AzurePolicyEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PolicyEvaluationResult> EvaluatePreFlightPoliciesAsync(
        InfrastructureProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Evaluating pre-flight policies for {ResourceType} '{ResourceName}' in {Location}",
            request.ResourceType, request.ResourceName, request.Location);

        var result = new PolicyEvaluationResult
        {
            IsCompliant = true,
            PolicyDecision = GovernancePolicyDecision.Allow,
            EvaluatedBy = "AzurePolicyEngine"
        };

        try
        {
            // Get applicable policies for this resource type
            var applicablePolicies = GetApplicablePoliciesForResourceType(request.ResourceType);

            foreach (var policyId in applicablePolicies)
            {
                var violation = await EvaluatePolicyAsync(policyId, request, cancellationToken);
                if (violation != null)
                {
                    result.PolicyViolations.Add(violation);
                    result.IsCompliant = false;
                }
            }

            // Determine policy decision based on violations
            if (result.PolicyViolations.Any(v => v.Severity == PolicyViolationSeverity.Critical))
            {
                result.PolicyDecision = GovernancePolicyDecision.Deny;
                result.Messages.Add("Critical policy violations detected - deployment blocked");
            }
            else if (result.PolicyViolations.Any(v => v.Severity == PolicyViolationSeverity.High))
            {
                result.PolicyDecision = GovernancePolicyDecision.RequiresApproval;
                result.Messages.Add("High-severity policy violations require manual approval");
            }
            else if (result.PolicyViolations.Any())
            {
                result.PolicyDecision = GovernancePolicyDecision.AuditOnly;
                result.Messages.Add("Minor policy violations detected - audit logged");
            }
            else
            {
                result.Messages.Add("All policy checks passed");
            }

            _logger.LogInformation("Policy evaluation completed: {Decision} with {ViolationCount} violations",
                result.PolicyDecision, result.PolicyViolations.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating pre-flight policies for {ResourceType}", request.ResourceType);
            return new PolicyEvaluationResult
            {
                IsCompliant = false,
                PolicyDecision = GovernancePolicyDecision.Deny,
                Messages = new List<string> { $"Policy evaluation failed: {ex.Message}" }
            };
        }
    }

    /// <inheritdoc />
    public async Task<PolicyEvaluationResult> EvaluateResourcePoliciesAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Evaluating policies for existing resource: {ResourceId}", resourceId);

        // Parse resource type from resource ID
        var resourceType = ParseResourceTypeFromId(resourceId);

        // Create a minimal request for evaluation
        var request = new InfrastructureProvisioningRequest
        {
            ResourceType = resourceType,
            ResourceName = ParseResourceNameFromId(resourceId),
            SubscriptionId = ParseSubscriptionFromId(resourceId),
            ResourceGroupName = ParseResourceGroupFromId(resourceId)
        };

        return await EvaluatePreFlightPoliciesAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<AzurePolicyEvaluation>> GetApplicablePoliciesAsync(
        string subscriptionId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting applicable policies for {ResourceType} in subscription {SubscriptionId}",
            resourceType, subscriptionId);

        var policies = new List<AzurePolicyEvaluation>();
        var applicablePolicyIds = GetApplicablePoliciesForResourceType(resourceType);

        foreach (var policyId in applicablePolicyIds)
        {
            policies.Add(new AzurePolicyEvaluation
            {
                PolicyDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/policyDefinitions/{policyId}",
                PolicyAssignmentId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/policyAssignments/{policyId}-assignment",
                ComplianceState = PolicyComplianceState.Unknown,
                PolicyEffect = "deny",
                EvaluatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult(policies);
    }

    #region Private Helper Methods

    private List<string> GetApplicablePoliciesForResourceType(string resourceType)
    {
        // Normalize resource type
        var normalizedType = resourceType.Trim();
        
        if (RequiredPoliciesByResourceType.TryGetValue(normalizedType, out var policies))
        {
            return policies;
        }

        // Return generic policies if specific type not found
        return new List<string> { "resource-tagging-required", "resource-location-allowed" };
    }

    private async Task<PolicyViolation?> EvaluatePolicyAsync(
        string policyId,
        InfrastructureProvisioningRequest request,
        CancellationToken cancellationToken)
    {
        // Evaluate specific policies based on request properties
        return policyId switch
        {
            "storage-https-only" => EvaluateStorageHttpsPolicy(request),
            "storage-encryption-required" => EvaluateStorageEncryptionPolicy(request),
            "storage-public-access-disabled" => EvaluateStoragePublicAccessPolicy(request),
            "keyvault-soft-delete" => EvaluateKeyVaultSoftDeletePolicy(request),
            "keyvault-purge-protection" => EvaluateKeyVaultPurgeProtectionPolicy(request),
            "resource-tagging-required" => EvaluateTaggingPolicy(request),
            "resource-location-allowed" => EvaluateLocationPolicy(request),
            _ => null // Unknown policy - skip
        };
    }

    private PolicyViolation? EvaluateStorageHttpsPolicy(InfrastructureProvisioningRequest request)
    {
        // Check if HTTPS-only is configured
        if (request.Parameters?.TryGetValue("supportsHttpsTrafficOnly", out var httpsOnly) == true)
        {
            if (httpsOnly?.ToString()?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateViolation("storage-https-only", 
                    "Storage account must enforce HTTPS traffic only",
                    "Set 'supportsHttpsTrafficOnly' to true");
            }
        }
        return null;
    }

    private PolicyViolation? EvaluateStorageEncryptionPolicy(InfrastructureProvisioningRequest request)
    {
        // Azure enables encryption by default, but check if explicitly disabled
        if (request.Parameters?.TryGetValue("encryption", out var encryption) == true)
        {
            if (encryption?.ToString()?.Contains("disabled", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateViolation("storage-encryption-required",
                    "Storage account encryption is required",
                    "Enable storage service encryption (SSE)");
            }
        }
        return null;
    }

    private PolicyViolation? EvaluateStoragePublicAccessPolicy(InfrastructureProvisioningRequest request)
    {
        if (request.Parameters?.TryGetValue("allowBlobPublicAccess", out var publicAccess) == true)
        {
            if (publicAccess?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateViolation("storage-public-access-disabled",
                    "Storage account public blob access must be disabled",
                    "Set 'allowBlobPublicAccess' to false");
            }
        }
        return null;
    }

    private PolicyViolation? EvaluateKeyVaultSoftDeletePolicy(InfrastructureProvisioningRequest request)
    {
        if (request.Parameters?.TryGetValue("enableSoftDelete", out var softDelete) == true)
        {
            if (softDelete?.ToString()?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateViolation("keyvault-soft-delete",
                    "Key Vault soft delete must be enabled",
                    "Set 'enableSoftDelete' to true");
            }
        }
        return null;
    }

    private PolicyViolation? EvaluateKeyVaultPurgeProtectionPolicy(InfrastructureProvisioningRequest request)
    {
        if (request.Parameters?.TryGetValue("enablePurgeProtection", out var purgeProtection) == true)
        {
            if (purgeProtection?.ToString()?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
            {
                return CreateViolation("keyvault-purge-protection",
                    "Key Vault purge protection should be enabled for production",
                    "Set 'enablePurgeProtection' to true");
            }
        }
        return null;
    }

    private PolicyViolation? EvaluateTaggingPolicy(InfrastructureProvisioningRequest request)
    {
        var requiredTags = new[] { "environment", "project", "owner" };
        var missingTags = requiredTags.Where(t => 
            request.Tags == null || 
            !request.Tags.ContainsKey(t) || 
            string.IsNullOrWhiteSpace(request.Tags[t])).ToList();

        if (missingTags.Any())
        {
            return CreateViolation("resource-tagging-required",
                $"Required tags missing: {string.Join(", ", missingTags)}",
                $"Add the following tags: {string.Join(", ", missingTags)}",
                PolicyViolationSeverity.Medium);
        }
        return null;
    }

    private PolicyViolation? EvaluateLocationPolicy(InfrastructureProvisioningRequest request)
    {
        var approvedLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eastus", "eastus2", "westus", "westus2", "centralus",
            "usgovvirginia", "usgovarizona", "usdodeast", "usdodcentral"
        };

        if (!string.IsNullOrEmpty(request.Location) && !approvedLocations.Contains(request.Location))
        {
            return CreateViolation("resource-location-allowed",
                $"Location '{request.Location}' is not in the approved regions list",
                $"Use one of the approved locations: {string.Join(", ", approvedLocations.Take(5))}",
                PolicyViolationSeverity.High);
        }
        return null;
    }

    private PolicyViolation CreateViolation(string policyId, string description, string remediation, 
        PolicyViolationSeverity? overrideSeverity = null)
    {
        var severity = overrideSeverity ?? 
            (PolicySeverityMap.TryGetValue(policyId, out var mappedSeverity) ? mappedSeverity : PolicyViolationSeverity.Medium);

        return new PolicyViolation
        {
            PolicyId = policyId,
            PolicyName = policyId.Replace("-", " ").ToUpperInvariant(),
            Severity = severity,
            Description = description,
            RecommendedAction = remediation
        };
    }

    private string ParseResourceTypeFromId(string resourceId)
    {
        // Format: /subscriptions/{sub}/resourceGroups/{rg}/providers/{namespace}/{type}/{name}
        var parts = resourceId.Split('/');
        var providerIndex = Array.IndexOf(parts, "providers");
        if (providerIndex >= 0 && providerIndex + 2 < parts.Length)
        {
            return $"{parts[providerIndex + 1]}/{parts[providerIndex + 2]}";
        }
        return "Unknown";
    }

    private string ParseResourceNameFromId(string resourceId)
    {
        var parts = resourceId.Split('/');
        return parts.LastOrDefault() ?? "Unknown";
    }

    private string ParseSubscriptionFromId(string resourceId)
    {
        var parts = resourceId.Split('/');
        var subIndex = Array.IndexOf(parts, "subscriptions");
        if (subIndex >= 0 && subIndex + 1 < parts.Length)
        {
            return parts[subIndex + 1];
        }
        return string.Empty;
    }

    private string ParseResourceGroupFromId(string resourceId)
    {
        var parts = resourceId.Split('/');
        var rgIndex = Array.IndexOf(parts, "resourceGroups");
        if (rgIndex >= 0 && rgIndex + 1 < parts.Length)
        {
            return parts[rgIndex + 1];
        }
        return string.Empty;
    }

    #endregion
}
