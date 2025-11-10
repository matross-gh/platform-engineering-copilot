using System;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Engineering.Copilot.Core.Models.Compliance;

/// <summary>
/// Impact Level classification for DoD environments
/// </summary>
public enum ImpactLevel
{
    IL2,  // Controlled Unclassified Information (CUI) Low
    IL4,  // CUI Moderate
    IL5,  // CUI High / Secret
    IL6   // Secret / Top Secret
}

/// <summary>
/// Policy enforcement category for IL-specific rules
/// </summary>
public enum PolicyCategory
{
    Encryption,
    Networking,
    Identity,
    Tagging,
    Region,
    Monitoring,
    Storage,
    Compute
}

// Note: PolicyViolation and PolicyViolationSeverity already exist in GovernanceModels.cs
// This file extends those models with IL-specific policy enforcement

/// <summary>
/// IL-specific policy configuration
/// </summary>
public class ImpactLevelPolicy
{
    public ImpactLevel Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PolicyRule> RequiredPolicies { get; set; } = new();
    public List<string> AllowedRegions { get; set; } = new();
    public Dictionary<string, string> RequiredTags { get; set; } = new();
    public EncryptionRequirements Encryption { get; set; } = new();
    public NetworkRequirements Networking { get; set; } = new();
    public IdentityRequirements Identity { get; set; } = new();
    
    // Convenience properties for backwards compatibility
    public ImpactLevel ImpactLevel => Level;  // Alias
    public bool RequiresPrivateEndpoints => Networking.RequirePrivateEndpoints;
    public bool RequiresCustomerManagedKeys => Encryption.RequireCustomerManagedKeys;
    public string MinimumTlsVersion => Encryption.MinimumTlsVersion;
    public bool AllowPublicNetworkAccess => !Networking.DisablePublicEndpoints;
    public Dictionary<string, string> MandatoryTags => RequiredTags;
    public EncryptionRequirements EncryptionRequirements => Encryption;
    public NetworkRequirements NetworkRequirements => Networking;
    public IdentityRequirements IdentityRequirements => Identity;
}

/// <summary>
/// Individual policy rule for IL enforcement
/// </summary>
public class PolicyRule
{
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicyCategory Category { get; set; }
    public PolicyViolationSeverity Severity { get; set; }
    public List<string> NistControls { get; set; } = new();
    public List<string> StigIds { get; set; } = new();
    public string ValidationLogic { get; set; } = string.Empty;
    public string RemediationGuidance { get; set; } = string.Empty;
    public ImpactLevel ImpactLevel { get; set; }  // Which IL this rule applies to
    public string Condition { get; set; } = string.Empty;  // Validation expression
}

/// <summary>
/// Encryption requirements for an Impact Level
/// </summary>
public class EncryptionRequirements
{
    public bool RequireCustomerManagedKeys { get; set; }
    public bool RequireEncryptionAtRest { get; set; }
    public bool RequireEncryptionInTransit { get; set; }
    public string MinimumTlsVersion { get; set; } = "1.2";
    public bool RequireFips140_2 { get; set; }
    public List<string> AllowedEncryptionAlgorithms { get; set; } = new();
    
    // Convenience properties
    public bool RequiresEncryptionAtRest => RequireEncryptionAtRest;
    public bool RequiresEncryptionInTransit => RequireEncryptionInTransit;
    public bool RequiresCustomerManagedKeys => RequireCustomerManagedKeys;
    public bool RequiresFipsCompliantEncryption => RequireFips140_2;
    public bool RequiresHsmBackedKeys { get; set; }
    public string AllowedKeyVaultSku { get; set; } = "Standard";
    public int MinimumKeySize { get; set; } = 2048;
}

/// <summary>
/// Network requirements for an Impact Level
/// </summary>
public class NetworkRequirements
{
    public bool DisablePublicEndpoints { get; set; }
    public bool RequirePrivateEndpoints { get; set; }
    public bool RequireNsgDefaultDeny { get; set; }
    public bool DisablePublicIpAddresses { get; set; }
    public bool RequireAzureFirewall { get; set; }
    public bool RequireNetworkIsolation { get; set; }
    public List<string> AllowedIngressPorts { get; set; } = new();
    
    // Convenience properties
    public bool RequiresPrivateEndpoints => RequirePrivateEndpoints;
    public bool RequiresNetworkIsolation => RequireNetworkIsolation;
    public List<string> AllowedServiceEndpoints { get; set; } = new();
    public bool RequiresNsgRules => RequireNsgDefaultDeny;
    public bool AllowInternetEgress { get; set; } = true;
    public bool RequiresDDoSProtection { get; set; }
    public bool RequiresDedicatedSubnet { get; set; }
}

/// <summary>
/// Identity and access requirements for an Impact Level
/// </summary>
public class IdentityRequirements
{
    public bool RequireManagedIdentity { get; set; }
    public bool RequireMfa { get; set; }
    public bool RequirePim { get; set; }
    public bool RequireCacPiv { get; set; }
    public bool DisablePasswordAuth { get; set; }
    public List<string> RequiredRbacRoles { get; set; } = new();
    
    // Convenience properties
    public bool RequiresManagedIdentity => RequireManagedIdentity;
    public bool RequiresMfa => RequireMfa;
    public bool RequiresPim => RequirePim;
    public bool RequiresCac => RequireCacPiv;
    public bool AllowsServicePrincipals { get; set; } = true;
    public bool RequiresConditionalAccess { get; set; }
}

/// <summary>
/// Policy validation result for IL compliance
/// </summary>
public class PolicyValidationResult
{
    public bool IsCompliant { get; set; }
    public ImpactLevel TargetLevel { get; set; }
    public List<PolicyViolation> Violations { get; set; } = new();
    public List<PolicyCompliance> PassedChecks { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    
    // Summary counts
    public int CriticalViolations { get; set; }
    public int HighViolations { get; set; }
    public int MediumViolations { get; set; }
    public int LowViolations { get; set; }
    
    // Computed properties for backwards compatibility
    public int CriticalCount => Violations.Count(v => v.Severity == PolicyViolationSeverity.Critical);
    public int HighCount => Violations.Count(v => v.Severity == PolicyViolationSeverity.High);
    public int MediumCount => Violations.Count(v => v.Severity == PolicyViolationSeverity.Medium);
    public int LowCount => Violations.Count(v => v.Severity == PolicyViolationSeverity.Low);
}

/// <summary>
/// Passed compliance check
/// </summary>
public class PolicyCompliance
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public PolicyCategory Category { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Template compliance validation request
/// </summary>
public class TemplateValidationRequest
{
    public string TemplatePath { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public TemplateType TemplateType { get; set; }  // Alias for Type
    public ImpactLevel TargetImpactLevel { get; set; }
    public ImpactLevel TargetLevel => TargetImpactLevel;  // Alias
    public Dictionary<string, string>? CustomTags { get; set; }
    public bool RequiresApproval { get; set; }
}

/// <summary>
/// Template type for IaC
/// </summary>
public enum TemplateType
{
    Bicep,
    Terraform,
    ARM,
    Kubernetes,
    Helm
}

/// <summary>
/// IL-compliant template generation request
/// </summary>
public class IlTemplateRequest
{
    public ImpactLevel ImpactLevel { get; set; }
    public TemplateType TemplateType { get; set; }
    public AzureResourceType ResourceType { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Azure resource types for template generation
/// </summary>
public enum AzureResourceType
{
    StorageAccount,
    VirtualMachine,
    AksCluster,
    SqlDatabase,
    KeyVault,
    AppService,
    ContainerRegistry,
    CosmosDb,
    FunctionApp,
    ApiManagement,
    ServiceBus,
    VirtualNetwork,
    NetworkSecurityGroup
}

/// <summary>
/// Generated IL-compliant template
/// </summary>
public class IlCompliantTemplate
{
    public string TemplateName { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public TemplateType TemplateType { get; set; }  // Alias
    public ImpactLevel ImpactLevel { get; set; }
    public AzureResourceType ResourceType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;  // Alias for Content
    public string ParametersContent { get; set; } = string.Empty;
    public List<string> AppliedPolicies { get; set; } = new();
    public List<string> NistControls { get; set; } = new();
    public List<string> StigIds { get; set; } = new();
    public string DeploymentInstructions { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
