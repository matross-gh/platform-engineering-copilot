using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using Microsoft.Extensions.Logging;

namespace Platform.Engineering.Copilot.Core.Services.Compliance;

/// <summary>
/// Service for enforcing IL2/IL4/IL5/IL6 compliance policies on infrastructure templates
/// Advisory-only mode: validates templates and provides guidance without making Azure changes
/// Leverages NIST controls, STIG requirements, and DoD Impact Level knowledge base
/// </summary>
public class PolicyEnforcementService : IPolicyEnforcementService
{
    private readonly ILogger<PolicyEnforcementService> _logger;
    private readonly IGovernanceEngine _governanceEngine;
    private readonly IAzurePolicyService _azurePolicyService;
    private readonly INistControlsService? _nistControlsService;
    private readonly IStigKnowledgeService? _stigKnowledgeService;
    private readonly IImpactLevelService? _impactLevelService;
    private readonly Dictionary<ImpactLevel, ImpactLevelPolicy> _policyCache;

    public PolicyEnforcementService(
        ILogger<PolicyEnforcementService> logger,
        IGovernanceEngine governanceEngine,
        IAzurePolicyService azurePolicyService,
        INistControlsService? nistControlsService = null,
        IStigKnowledgeService? stigKnowledgeService = null,
        IImpactLevelService? impactLevelService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _governanceEngine = governanceEngine ?? throw new ArgumentNullException(nameof(governanceEngine));
        _azurePolicyService = azurePolicyService ?? throw new ArgumentNullException(nameof(azurePolicyService));
        _nistControlsService = nistControlsService;
        _stigKnowledgeService = stigKnowledgeService;
        _impactLevelService = impactLevelService;
        _policyCache = new Dictionary<ImpactLevel, ImpactLevelPolicy>();
        
        InitializePolicies();
    }

    /// <summary>
    /// Initialize IL2/IL4/IL5/IL6 policy configurations
    /// Based on DoD Cloud Computing SRG, NIST 800-53, and FedRAMP requirements
    /// Optionally enriched with data from knowledge base services
    /// </summary>
    private void InitializePolicies()
    {
        if (_impactLevelService != null || _stigKnowledgeService != null)
        {
            _logger.LogInformation("Initializing IL policies with knowledge base enrichment");
            // In future: could async load from knowledge base during first use
            // For now, use hardcoded baseline with option to enrich later
        }
        
        // IL2: Public data, basic security controls (FedRAMP Low equivalent)
        _policyCache[ImpactLevel.IL2] = new ImpactLevelPolicy
        {
            Level = ImpactLevel.IL2,
            Name = "Impact Level 2 (Public Data)",
            Description = "Basic security controls for public data. FedRAMP Low equivalent.",
            AllowedRegions = new List<string> { "usgovvirginia", "usgovtexas", "usgoviowa", "usgovarizona", "eastus", "westus" },
            RequiredTags = new Dictionary<string, string>
            {
                { "ImpactLevel", "IL2" },
                { "DataClassification", "Public" },
                { "Environment", "Production" }
            },
            Encryption = new EncryptionRequirements
            {
                RequireEncryptionAtRest = true,
                RequireEncryptionInTransit = true,
                RequireCustomerManagedKeys = false,
                MinimumTlsVersion = "1.2",
                AllowedKeyVaultSku = "Standard",
                MinimumKeySize = 2048
            },
            Networking = new NetworkRequirements
            {
                DisablePublicEndpoints = false,
                RequirePrivateEndpoints = false,
                RequireNetworkIsolation = false,
                AllowedServiceEndpoints = new List<string>(),
                RequireNsgDefaultDeny = true,
                AllowInternetEgress = true
            },
            Identity = new IdentityRequirements
            {
                RequireManagedIdentity = true,
                AllowsServicePrincipals = true,
                RequireMfa = false,
                RequiresConditionalAccess = false
            }
        };

        // IL4: Controlled Unclassified Information (CUI), moderate security controls (FedRAMP Moderate)
        _policyCache[ImpactLevel.IL4] = new ImpactLevelPolicy
        {
            Level = ImpactLevel.IL4,
            Name = "Impact Level 4 (CUI)",
            Description = "Enhanced security controls for Controlled Unclassified Information. FedRAMP Moderate equivalent.",
            AllowedRegions = new List<string> { "usgovvirginia", "usgovtexas", "usgoviowa", "usgovarizona" },
            RequiredTags = new Dictionary<string, string>
            {
                { "ImpactLevel", "IL4" },
                { "DataClassification", "CUI" },
                { "Environment", "Production" },
                { "ComplianceFramework", "FedRAMP-Moderate" }
            },
            Encryption = new EncryptionRequirements
            {
                RequireEncryptionAtRest = true,
                RequireEncryptionInTransit = true,
                RequireCustomerManagedKeys = true,
                MinimumTlsVersion = "1.2",
                AllowedKeyVaultSku = "Premium",
                MinimumKeySize = 4096
            },
            Networking = new NetworkRequirements
            {
                DisablePublicEndpoints = true,
                RequirePrivateEndpoints = true,
                RequireNetworkIsolation = true,
                AllowedServiceEndpoints = new List<string> { "Microsoft.Storage", "Microsoft.KeyVault", "Microsoft.Sql" },
                RequireNsgDefaultDeny = true,
                AllowInternetEgress = false
            },
            Identity = new IdentityRequirements
            {
                RequireManagedIdentity = true,
                AllowsServicePrincipals = false,
                RequireMfa = true,
                RequiresConditionalAccess = true
            }
        };

        // IL5: Classified data up to SECRET, strict security controls (FedRAMP High)
        _policyCache[ImpactLevel.IL5] = new ImpactLevelPolicy
        {
            Level = ImpactLevel.IL5,
            Name = "Impact Level 5 (SECRET)",
            Description = "Strict security controls for classified data up to SECRET. FedRAMP High equivalent.",
            AllowedRegions = new List<string> { "usgovvirginia", "usgovtexas" }, // Azure Government only
            RequiredTags = new Dictionary<string, string>
            {
                { "ImpactLevel", "IL5" },
                { "DataClassification", "SECRET" },
                { "Environment", "Production" },
                { "ComplianceFramework", "FedRAMP-High" },
                { "STIGCompliance", "Required" }
            },
            Encryption = new EncryptionRequirements
            {
                RequireEncryptionAtRest = true,
                RequireEncryptionInTransit = true,
                RequireCustomerManagedKeys = true,
                MinimumTlsVersion = "1.3",
                RequireFips140_2 = true,
                AllowedKeyVaultSku = "Premium",
                MinimumKeySize = 4096
            },
            Networking = new NetworkRequirements
            {
                DisablePublicEndpoints = true,
                RequirePrivateEndpoints = true,
                RequireNetworkIsolation = true,
                AllowedServiceEndpoints = new List<string> { "Microsoft.Storage", "Microsoft.KeyVault" },
                RequireNsgDefaultDeny = true,
                AllowInternetEgress = false,
                RequiresDDoSProtection = true
            },
            Identity = new IdentityRequirements
            {
                RequireManagedIdentity = true,
                AllowsServicePrincipals = false,
                RequireMfa = true,
                RequiresConditionalAccess = true,
                RequirePim = true
            }
        };

        // IL6: Classified data up to TOP SECRET/SCI, maximum security controls
        _policyCache[ImpactLevel.IL6] = new ImpactLevelPolicy
        {
            Level = ImpactLevel.IL6,
            Name = "Impact Level 6 (TOP SECRET/SCI)",
            Description = "Maximum security controls for TOP SECRET/SCI classified data. Requires dedicated hardware.",
            AllowedRegions = new List<string> { "usgovvirginia" }, // Restricted to dedicated Azure Government Secret regions
            RequiredTags = new Dictionary<string, string>
            {
                { "ImpactLevel", "IL6" },
                { "DataClassification", "TOP-SECRET-SCI" },
                { "Environment", "Production" },
                { "ComplianceFramework", "FedRAMP-High" },
                { "STIGCompliance", "Required" },
                { "DedicatedHardware", "Required" }
            },
            Encryption = new EncryptionRequirements
            {
                RequireEncryptionAtRest = true,
                RequireEncryptionInTransit = true,
                RequireCustomerManagedKeys = true,
                MinimumTlsVersion = "1.3",
                RequireFips140_2 = true,
                RequiresHsmBackedKeys = true,
                AllowedKeyVaultSku = "Premium",
                MinimumKeySize = 4096
            },
            Networking = new NetworkRequirements
            {
                DisablePublicEndpoints = true,
                RequirePrivateEndpoints = true,
                RequireNetworkIsolation = true,
                AllowedServiceEndpoints = new List<string>(), // No service endpoints, private endpoints only
                RequireNsgDefaultDeny = true,
                AllowInternetEgress = false,
                RequiresDDoSProtection = true,
                RequiresDedicatedSubnet = true
            },
            Identity = new IdentityRequirements
            {
                RequireManagedIdentity = true,
                AllowsServicePrincipals = false,
                RequireMfa = true,
                RequiresConditionalAccess = true,
                RequirePim = true,
                RequireCacPiv = true
            }
        };

        _logger.LogInformation("Initialized {Count} Impact Level policy configurations", _policyCache.Count);
    }

    public Task<ImpactLevelPolicy> GetPolicyForImpactLevelAsync(
        ImpactLevel level,
        CancellationToken cancellationToken = default)
    {
        if (_policyCache.TryGetValue(level, out var policy))
        {
            _logger.LogDebug("Retrieved policy configuration for {ImpactLevel}", level);
            return Task.FromResult(policy);
        }

        throw new ArgumentException($"No policy configuration found for Impact Level: {level}");
    }

    public async Task<PolicyValidationResult> ValidateTemplateAsync(
        TemplateValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating {TemplateType} template for {ImpactLevel}", 
            request.TemplateType, request.TargetLevel);

        var result = new PolicyValidationResult
        {
            IsCompliant = true,
            Violations = new List<PolicyViolation>(),
            Warnings = new List<string>(),
            TargetLevel = request.TargetLevel,
            ValidatedAt = DateTime.UtcNow
        };

        var policy = await GetPolicyForImpactLevelAsync(request.TargetLevel, cancellationToken);

        // Validate encryption requirements
        await ValidateEncryptionAsync(request, policy, result, cancellationToken);

        // Validate network requirements
        await ValidateNetworkingAsync(request, policy, result, cancellationToken);

        // Validate identity requirements
        await ValidateIdentityAsync(request, policy, result, cancellationToken);

        // Validate tagging requirements
        await ValidateTaggingAsync(request, policy, result, cancellationToken);

        // Validate region restrictions
        await ValidateRegionAsync(request, policy, result, cancellationToken);

        // Leverage existing governance engine for additional validation
        if (request.RequiresApproval)
        {
            _logger.LogDebug("Template requires approval workflow for {ImpactLevel}", request.TargetLevel);
            result.Warnings.Add($"Templates for {request.TargetLevel} require approval workflow before deployment");
        }

        // Calculate compliance summary
        result.CriticalViolations = result.Violations.Count(v => v.Severity == PolicyViolationSeverity.Critical);
        result.HighViolations = result.Violations.Count(v => v.Severity == PolicyViolationSeverity.High);
        result.MediumViolations = result.Violations.Count(v => v.Severity == PolicyViolationSeverity.Medium);
        result.LowViolations = result.Violations.Count(v => v.Severity == PolicyViolationSeverity.Low);
        result.IsCompliant = result.CriticalViolations == 0 && result.HighViolations == 0;

        _logger.LogInformation("Validation complete. Compliant: {IsCompliant}, Violations: {ViolationCount}", 
            result.IsCompliant, result.Violations.Count);

        return result;
    }

    private Task ValidateEncryptionAsync(
        TemplateValidationRequest request,
        ImpactLevelPolicy policy,
        PolicyValidationResult result,
        CancellationToken cancellationToken)
    {
        var encryption = policy.EncryptionRequirements;

        // This is a simplified validation - in production, parse template and check actual resource configurations
        if (encryption.RequiresCustomerManagedKeys && 
            !request.TemplateContent.Contains("customerManagedKey", StringComparison.OrdinalIgnoreCase))
        {
            result.Violations.Add(new PolicyViolation
            {
                PolicyId = "ENC-001",
                PolicyName = $"{policy.ImpactLevel} - Customer Managed Keys Required",
                Severity = PolicyViolationSeverity.Critical,
                Description = $"Impact Level {policy.ImpactLevel} requires customer-managed keys for encryption",
                RecommendedAction = "Configure customer-managed keys (CMK) using Azure Key Vault"
            });
            result.IsCompliant = false;
        }

        if (encryption.RequiresFipsCompliantEncryption && 
            !request.TemplateContent.Contains("FIPS", StringComparison.OrdinalIgnoreCase))
        {
            result.Violations.Add(new PolicyViolation
            {
                PolicyId = "ENC-002",
                PolicyName = $"{policy.ImpactLevel} - FIPS Compliance Required",
                Severity = PolicyViolationSeverity.High,
                Description = "FIPS 140-2 compliant encryption required for classified data",
                RecommendedAction = "Enable FIPS-compliant cryptographic modules"
            });
        }

        return Task.CompletedTask;
    }

    private Task ValidateNetworkingAsync(
        TemplateValidationRequest request,
        ImpactLevelPolicy policy,
        PolicyValidationResult result,
        CancellationToken cancellationToken)
    {
        var network = policy.NetworkRequirements;

        if (network.RequiresPrivateEndpoints && 
            !request.TemplateContent.Contains("privateEndpoint", StringComparison.OrdinalIgnoreCase))
        {
            result.Violations.Add(new PolicyViolation
            {
                PolicyId = "NET-001",
                PolicyName = $"{policy.ImpactLevel} - Private Endpoints Required",
                Severity = PolicyViolationSeverity.Critical,
                Description = $"Impact Level {policy.ImpactLevel} requires private endpoints for all PaaS resources",
                RecommendedAction = "Configure Azure Private Endpoints for Storage, SQL, Key Vault, etc."
            });
            result.IsCompliant = false;
        }

        if (policy.AllowPublicNetworkAccess == false && 
            request.TemplateContent.Contains("publicNetworkAccess: 'Enabled'", StringComparison.OrdinalIgnoreCase))
        {
            result.Violations.Add(new PolicyViolation
            {
                PolicyId = "NET-002",
                PolicyName = $"{policy.ImpactLevel} - Public Network Access Denied",
                Severity = PolicyViolationSeverity.Critical,
                Description = "Public network access is not allowed for this Impact Level",
                RecommendedAction = "Set publicNetworkAccess to 'Disabled' and use private endpoints"
            });
            result.IsCompliant = false;
        }

        if (network.RequiresDDoSProtection && 
            !request.TemplateContent.Contains("DDoS", StringComparison.OrdinalIgnoreCase))
        {
            result.Violations.Add(new PolicyViolation
            {
                PolicyId = "NET-003",
                PolicyName = $"{policy.ImpactLevel} - DDoS Protection Required",
                Severity = PolicyViolationSeverity.High,
                Description = "Azure DDoS Protection Standard is required",
                RecommendedAction = "Enable DDoS Protection Standard on virtual networks"
            });
        }

        return Task.CompletedTask;
    }

    private Task ValidateIdentityAsync(
        TemplateValidationRequest request,
        ImpactLevelPolicy policy,
        PolicyValidationResult result,
        CancellationToken cancellationToken)
    {
        var identity = policy.IdentityRequirements;

        if (identity.RequiresManagedIdentity && 
            !request.TemplateContent.Contains("managedIdentity", StringComparison.OrdinalIgnoreCase))
        {
            result.Violations.Add(new PolicyViolation
            {
                PolicyId = "ID-001",
                PolicyName = $"{policy.ImpactLevel} - Managed Identity Required",
                Severity = PolicyViolationSeverity.High,
                Description = "Managed identities must be used instead of service principals",
                RecommendedAction = "Configure system-assigned or user-assigned managed identities"
            });
        }

        if (identity.RequiresPim)
        {
            result.Warnings.Add("Privileged Identity Management (PIM) required for administrative access");
        }

        if (identity.RequiresCac)
        {
            result.Warnings.Add("Common Access Card (CAC) authentication required for IL6 access");
        }

        return Task.CompletedTask;
    }

    private Task ValidateTaggingAsync(
        TemplateValidationRequest request,
        ImpactLevelPolicy policy,
        PolicyValidationResult result,
        CancellationToken cancellationToken)
    {
        foreach (var requiredTag in policy.MandatoryTags)
        {
            if (!request.TemplateContent.Contains($"{requiredTag.Key}", StringComparison.OrdinalIgnoreCase))
            {
                result.Violations.Add(new PolicyViolation
                {
                    PolicyId = "TAG-001",
                    PolicyName = $"{policy.ImpactLevel} - Mandatory Tag Missing",
                    Severity = PolicyViolationSeverity.Medium,
                    Description = $"Required tag '{requiredTag.Key}' with value '{requiredTag.Value}' is missing",
                    RecommendedAction = $"Add tag: {requiredTag.Key} = {requiredTag.Value}"
                });
            }
        }

        return Task.CompletedTask;
    }

    private Task ValidateRegionAsync(
        TemplateValidationRequest request,
        ImpactLevelPolicy policy,
        PolicyValidationResult result,
        CancellationToken cancellationToken)
    {
        // Simplified region validation - in production, parse location from template
        var foundAllowedRegion = policy.AllowedRegions.Any(region => 
            request.TemplateContent.Contains(region, StringComparison.OrdinalIgnoreCase));

        if (!foundAllowedRegion && policy.ImpactLevel >= ImpactLevel.IL5)
        {
            result.Violations.Add(new PolicyViolation
            {
                PolicyId = "REG-001",
                PolicyName = $"{policy.ImpactLevel} - Region Restriction",
                Severity = PolicyViolationSeverity.Critical,
                Description = $"Resources must be deployed to allowed regions: {string.Join(", ", policy.AllowedRegions)}",
                RecommendedAction = $"Deploy to one of: {string.Join(", ", policy.AllowedRegions)}"
            });
            result.IsCompliant = false;
        }

        return Task.CompletedTask;
    }

    public async Task<IlCompliantTemplate> GenerateCompliantTemplateAsync(
        IlTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {ImpactLevel}-compliant {TemplateType} template for {ResourceType}",
            request.ImpactLevel, request.TemplateType, request.ResourceType);

        var policy = await GetPolicyForImpactLevelAsync(request.ImpactLevel, cancellationToken);
        
        var template = new IlCompliantTemplate
        {
            TemplateType = request.TemplateType,
            ResourceType = request.ResourceType,
            ImpactLevel = request.ImpactLevel,
            GeneratedAt = DateTime.UtcNow,
            TemplateContent = GenerateTemplateContent(request, policy),
            AppliedPolicies = (await GetPolicyRulesAsync(request.ImpactLevel, cancellationToken: cancellationToken))
                .Select(r => r.RuleId).ToList()  // Convert PolicyRule list to string list
        };

        _logger.LogInformation("Generated compliant template with {PolicyCount} policies applied", 
            template.AppliedPolicies.Count);

        return template;
    }

    private string GenerateTemplateContent(IlTemplateRequest request, ImpactLevelPolicy policy)
    {
        // Placeholder for template generation - in production, use template engines
        // This would generate Bicep/Terraform with hardened configurations
        return request.TemplateType switch
        {
            TemplateType.Bicep => GenerateBicepTemplate(request, policy),
            TemplateType.Terraform => GenerateTerraformTemplate(request, policy),
            TemplateType.ARM => GenerateArmTemplate(request, policy),
            _ => throw new ArgumentException($"Unsupported template type: {request.TemplateType}")
        };
    }

    private string GenerateBicepTemplate(IlTemplateRequest request, ImpactLevelPolicy policy)
    {
        // Simplified Bicep generation - production would use proper template engine
        return $@"// {policy.Name} Compliant {request.ResourceType}
// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
// Advisory: Review and customize before deployment

param location string = '{policy.AllowedRegions.First()}'
param tags object = {string.Join("\n  ", policy.MandatoryTags.Select(t => $"  {t.Key}: '{t.Value}'"))}

// TODO: Add resource-specific {request.ResourceType} configuration with IL{(int)policy.ImpactLevel} controls
// - Encryption: {(policy.EncryptionRequirements.RequiresCustomerManagedKeys ? "Customer-Managed Keys" : "Platform-Managed Keys")}
// - Networking: {(policy.NetworkRequirements.RequiresPrivateEndpoints ? "Private Endpoints Required" : "Public Access Allowed")}
// - TLS Version: {policy.MinimumTlsVersion}
";
    }

    private string GenerateTerraformTemplate(IlTemplateRequest request, ImpactLevelPolicy policy)
    {
        return $@"# {policy.Name} Compliant {request.ResourceType}
# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
# Advisory: Review and customize before deployment

terraform {{
  required_version = "">= 1.0""
}}

# TODO: Add resource-specific {request.ResourceType} configuration with IL{(int)policy.ImpactLevel} controls
";
    }

    private string GenerateArmTemplate(IlTemplateRequest request, ImpactLevelPolicy policy)
    {
        return $@"{{
  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""metadata"": {{
    ""description"": ""{policy.Name} Compliant {request.ResourceType}"",
    ""generatedAt"": ""{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC""
  }}
}}";
    }

    public async Task<string> ApplyPoliciesToTemplateAsync(
        string templateContent,
        TemplateType type,
        ImpactLevel targetLevel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying {ImpactLevel} policies to existing {TemplateType} template", 
            targetLevel, type);

        var policy = await GetPolicyForImpactLevelAsync(targetLevel, cancellationToken);

        // In production, parse template and inject hardened configurations
        // For now, return advisory comment
        var hardenedTemplate = $@"// ADVISORY: {policy.Name} Policies Applied
// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
// Review the following hardening recommendations:
// - Encryption: {(policy.EncryptionRequirements.RequiresCustomerManagedKeys ? "Enable Customer-Managed Keys" : "Use Platform-Managed Keys")}
// - Networking: {(policy.NetworkRequirements.RequiresPrivateEndpoints ? "Configure Private Endpoints" : "Secure public endpoints")}
// - Region: Deploy to {string.Join(" or ", policy.AllowedRegions)}

{templateContent}";

        return hardenedTemplate;
    }

    public async Task<List<PolicyRule>> GetPolicyRulesAsync(
        ImpactLevel level,
        PolicyCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyForImpactLevelAsync(level, cancellationToken);
        var rules = new List<PolicyRule>();

        // Generate policy rules based on Impact Level configuration
        if (!category.HasValue || category == PolicyCategory.Encryption)
        {
            rules.AddRange(GenerateEncryptionRules(policy));
        }

        if (!category.HasValue || category == PolicyCategory.Networking)
        {
            rules.AddRange(GenerateNetworkingRules(policy));
        }

        if (!category.HasValue || category == PolicyCategory.Identity)
        {
            rules.AddRange(GenerateIdentityRules(policy));
        }

        if (!category.HasValue || category == PolicyCategory.Tagging)
        {
            rules.AddRange(GenerateTaggingRules(policy));
        }

        _logger.LogDebug("Generated {Count} policy rules for {ImpactLevel}", rules.Count, level);
        return rules;
    }

    private List<PolicyRule> GenerateEncryptionRules(ImpactLevelPolicy policy)
    {
        var rules = new List<PolicyRule>
        {
            new PolicyRule
            {
                RuleId = "ENC-001",
                Name = "Encryption at Rest Required",
                Category = PolicyCategory.Encryption,
                Description = "All data must be encrypted at rest",
                Severity = PolicyViolationSeverity.Critical,
                ImpactLevel = policy.ImpactLevel,
                Condition = "encryption.atRest == true"
            }
        };

        if (policy.EncryptionRequirements.RequiresCustomerManagedKeys)
        {
            rules.Add(new PolicyRule
            {
                RuleId = "ENC-002",
                Name = "Customer-Managed Keys Required",
                Category = PolicyCategory.Encryption,
                Description = "Customer-managed keys (CMK) must be used for encryption",
                Severity = PolicyViolationSeverity.Critical,
                ImpactLevel = policy.ImpactLevel,
                Condition = "encryption.customerManagedKeys == true"
            });
        }

        return rules;
    }

    private List<PolicyRule> GenerateNetworkingRules(ImpactLevelPolicy policy)
    {
        var rules = new List<PolicyRule>();

        if (policy.NetworkRequirements.RequiresPrivateEndpoints)
        {
            rules.Add(new PolicyRule
            {
                RuleId = "NET-001",
                Name = "Private Endpoints Required",
                Category = PolicyCategory.Networking,
                Description = "All PaaS resources must use private endpoints",
                Severity = PolicyViolationSeverity.Critical,
                ImpactLevel = policy.ImpactLevel,
                Condition = "network.privateEndpoints == true"
            });
        }

        if (!policy.AllowPublicNetworkAccess)
        {
            rules.Add(new PolicyRule
            {
                RuleId = "NET-002",
                Name = "Public Network Access Denied",
                Category = PolicyCategory.Networking,
                Description = "Public network access is not permitted",
                Severity = PolicyViolationSeverity.Critical,
                ImpactLevel = policy.ImpactLevel,
                Condition = "network.publicAccess == false"
            });
        }

        return rules;
    }

    private List<PolicyRule> GenerateIdentityRules(ImpactLevelPolicy policy)
    {
        var rules = new List<PolicyRule>();

        if (policy.IdentityRequirements.RequiresManagedIdentity)
        {
            rules.Add(new PolicyRule
            {
                RuleId = "ID-001",
                Name = "Managed Identity Required",
                Category = PolicyCategory.Identity,
                Description = "Resources must use managed identities for authentication",
                Severity = PolicyViolationSeverity.High,
                ImpactLevel = policy.ImpactLevel,
                Condition = "identity.type == 'SystemAssigned' || identity.type == 'UserAssigned'"
            });
        }

        return rules;
    }

    private List<PolicyRule> GenerateTaggingRules(ImpactLevelPolicy policy)
    {
        return policy.MandatoryTags.Select(tag => new PolicyRule
        {
            RuleId = $"TAG-{tag.Key.ToUpper()}",
            Name = $"Tag {tag.Key} Required",
            Category = PolicyCategory.Tagging,
            Description = $"Tag '{tag.Key}' with value '{tag.Value}' is mandatory",
            Severity = PolicyViolationSeverity.Medium,
            ImpactLevel = policy.ImpactLevel,
            Condition = $"tags.{tag.Key} == '{tag.Value}'"
        }).ToList();
    }

    public async Task<PolicyValidationResult> ValidateResourceConfigurationAsync(
        string resourceType,
        Dictionary<string, object> configuration,
        ImpactLevel targetLevel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating {ResourceType} configuration against {ImpactLevel} policies",
            resourceType, targetLevel);

        var result = new PolicyValidationResult
        {
            IsCompliant = true,
            Violations = new List<PolicyViolation>(),
            TargetLevel = targetLevel,
            ValidatedAt = DateTime.UtcNow
        };

        var policy = await GetPolicyForImpactLevelAsync(targetLevel, cancellationToken);

        // Validate resource-specific configuration
        // In production, implement detailed resource type validation (Storage, SQL, AKS, etc.)
        
        result.CriticalViolations = result.Violations.Count(v => v.Severity == PolicyViolationSeverity.Critical);
        result.HighViolations = result.Violations.Count(v => v.Severity == PolicyViolationSeverity.High);
        result.IsCompliant = result.CriticalViolations == 0 && result.HighViolations == 0;

        return result;
    }

    public async Task<string> GetRemediationGuidanceAsync(
        PolicyViolation violation,
        TemplateType templateType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating remediation guidance for {PolicyId} in {TemplateType}",
            violation.PolicyId, templateType);

        // Generate IaC-specific remediation guidance
        return templateType switch
        {
            TemplateType.Bicep => GenerateBicepRemediation(violation),
            TemplateType.Terraform => GenerateTerraformRemediation(violation),
            TemplateType.ARM => GenerateArmRemediation(violation),
            _ => violation.RecommendedAction
        };
    }

    private string GenerateBicepRemediation(PolicyViolation violation)
    {
        return violation.PolicyId switch
        {
            "ENC-001" => @"// Enable customer-managed keys in Bicep:
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource key 'Microsoft.KeyVault/vaults/keys@2023-07-01' existing = {
  parent: keyVault
  name: keyName
}

// Add to storage account:
encryption: {
  services: {
    blob: { enabled: true }
    file: { enabled: true }
  }
  keySource: 'Microsoft.Keyvault'
  keyvaultproperties: {
    keyname: key.name
    keyvaulturi: keyVault.properties.vaultUri
  }
}",
            "NET-001" => @"// Add private endpoint in Bicep:
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${resourceName}-pe'
  location: location
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [{
      name: '${resourceName}-connection'
      properties: {
        privateLinkServiceId: resource.id
        groupIds: ['blob'] // or 'file', 'table', etc.
      }
    }]
  }
}",
            _ => violation.RecommendedAction
        };
    }

    private string GenerateTerraformRemediation(PolicyViolation violation)
    {
        return violation.PolicyId switch
        {
            "ENC-001" => @"# Enable customer-managed keys in Terraform:
resource ""azurerm_storage_account"" ""example"" {
  # ... other configuration ...
  
  customer_managed_key {
    key_vault_key_id = azurerm_key_vault_key.example.id
  }
}",
            "NET-001" => @"# Add private endpoint in Terraform:
resource ""azurerm_private_endpoint"" ""example"" {
  name                = ""${var.resource_name}-pe""
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  subnet_id           = azurerm_subnet.example.id

  private_service_connection {
    name                           = ""${var.resource_name}-connection""
    private_connection_resource_id = azurerm_storage_account.example.id
    subresource_names              = [""blob""]
    is_manual_connection           = false
  }
}",
            _ => violation.RecommendedAction
        };
    }

    private string GenerateArmRemediation(PolicyViolation violation)
    {
        return violation.PolicyId switch
        {
            "ENC-001" => @"{
  ""type"": ""Microsoft.Storage/storageAccounts"",
  ""encryption"": {
    ""services"": {
      ""blob"": { ""enabled"": true }
    },
    ""keySource"": ""Microsoft.Keyvault"",
    ""keyvaultproperties"": {
      ""keyname"": ""[parameters('keyName')]"",
      ""keyvaulturi"": ""[reference(parameters('keyVaultId')).vaultUri]""
    }
  }
}",
            _ => violation.RecommendedAction
        };
    }
}
