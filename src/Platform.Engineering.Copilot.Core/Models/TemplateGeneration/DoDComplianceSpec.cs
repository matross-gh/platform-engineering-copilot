using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Models;

/// <summary>
/// DoD-specific compliance metadata for IL2/IL4/IL5/IL6 environments
/// Extends TemplateGenerationRequest with Navy/DoD organizational and security requirements
/// </summary>
public class DoDComplianceSpec
{
    // ========================================
    // MISSION & ORGANIZATION
    // ========================================
    
    /// <summary>
    /// Mission sponsor or program office (e.g., PMW-120, PMW-130, SPAWAR, NAVAIR, NAVSEA)
    /// </summary>
    public string? MissionSponsor { get; set; }
    
    /// <summary>
    /// DoD Activity Address Code - 6-character alphanumeric identifier
    /// </summary>
    public string? DoDAAC { get; set; }
    
    /// <summary>
    /// Organization unit, division, or department
    /// </summary>
    public string? OrganizationUnit { get; set; }
    
    // ========================================
    // SECURITY CLASSIFICATION
    // ========================================
    
    /// <summary>
    /// DoD Impact Level (IL2, IL4, IL5, IL6)
    /// Determines security controls, encryption requirements, and compliance frameworks
    /// </summary>
    public ImpactLevel ImpactLevel { get; set; } = ImpactLevel.IL2;
    
    /// <summary>
    /// Data classification level (Unclassified, CUI, Secret, Top Secret)
    /// </summary>
    public string DataClassification { get; set; } = "Unclassified";
    
    // ========================================
    // COMPLIANCE REQUIREMENTS (Auto-derived from Impact Level)
    // ========================================
    
    /// <summary>
    /// Requires FIPS 140-2 compliant cryptographic modules (IL5+)
    /// </summary>
    public bool RequiresFIPS140_2 => ImpactLevel >= ImpactLevel.IL5;
    
    /// <summary>
    /// Requires Common Access Card (CAC) authentication (IL6)
    /// </summary>
    public bool RequiresCAC => ImpactLevel >= ImpactLevel.IL6;
    
    /// <summary>
    /// Requires Authority to Operate (ATO) package (IL5+)
    /// </summary>
    public bool RequiresATO => ImpactLevel >= ImpactLevel.IL5;
    
    /// <summary>
    /// Requires Enterprise Mission Assurance Support Service (eMASS) registration (IL5+)
    /// </summary>
    public bool RequireseMASS => ImpactLevel >= ImpactLevel.IL5;
    
    /// <summary>
    /// Requires customer-managed encryption keys (IL4+)
    /// </summary>
    public bool RequiresCustomerManagedKeys => ImpactLevel >= ImpactLevel.IL4;
    
    /// <summary>
    /// Requires private endpoints for all PaaS resources (IL4+)
    /// </summary>
    public bool RequiresPrivateEndpoints => ImpactLevel >= ImpactLevel.IL4;
    
    /// <summary>
    /// Requires Azure Government regions only (IL5+)
    /// </summary>
    public bool RequiresAzureGovernment => ImpactLevel >= ImpactLevel.IL5;
    
    /// <summary>
    /// Required NIST 800-53 controls based on Impact Level
    /// Populated from knowledge base or hardcoded baseline
    /// </summary>
    public List<string> RequiredNistControls { get; set; } = new();
    
    /// <summary>
    /// Required STIG controls based on Impact Level
    /// Populated from knowledge base or hardcoded baseline
    /// </summary>
    public List<string> RequiredSTIGs { get; set; } = new();
    
    // ========================================
    // COMPLIANCE FRAMEWORKS
    // ========================================
    
    /// <summary>
    /// FedRAMP authorization level based on Impact Level
    /// IL2 = FedRAMP Low, IL4 = FedRAMP Moderate, IL5/IL6 = FedRAMP High
    /// </summary>
    public string FedRAMPLevel => ImpactLevel switch
    {
        ImpactLevel.IL2 => "FedRAMP Low",
        ImpactLevel.IL4 => "FedRAMP Moderate",
        ImpactLevel.IL5 => "FedRAMP High",
        ImpactLevel.IL6 => "FedRAMP High",
        _ => "Not Required"
    };
    
    /// <summary>
    /// Applicable compliance frameworks
    /// </summary>
    public List<string> ComplianceFrameworks => ImpactLevel switch
    {
        ImpactLevel.IL2 => new() { "NIST 800-53 Low Baseline", "FedRAMP Low" },
        ImpactLevel.IL4 => new() { "NIST 800-53 Moderate Baseline", "FedRAMP Moderate", "DoD Cloud SRG" },
        ImpactLevel.IL5 => new() { "NIST 800-53 High Baseline", "FedRAMP High", "DoD Cloud SRG", "STIG Compliance", "RMF" },
        ImpactLevel.IL6 => new() { "NIST 800-53 High Baseline", "FedRAMP High", "DoD Cloud SRG", "STIG Compliance", "RMF", "ICD 503" },
        _ => new()
    };
    
    // ========================================
    // AZURE RESOURCE TAGGING
    // ========================================
    
    /// <summary>
    /// Generate mandatory Azure resource tags for DoD compliance
    /// These tags are required for all resources in IL environments
    /// </summary>
    public Dictionary<string, string> GenerateMandatoryTags(string environment = "Production")
    {
        var tags = new Dictionary<string, string>
        {
            { "ImpactLevel", ImpactLevel.ToString() },
            { "DataClassification", DataClassification },
            { "Environment", environment },
            { "ComplianceFramework", FedRAMPLevel }
        };
        
        if (!string.IsNullOrEmpty(MissionSponsor))
            tags["MissionSponsor"] = MissionSponsor;
        
        if (!string.IsNullOrEmpty(DoDAAC))
            tags["DoDAAC"] = DoDAAC;
        
        if (!string.IsNullOrEmpty(OrganizationUnit))
            tags["OrganizationUnit"] = OrganizationUnit;
        
        if (RequiresATO)
            tags["ATORequired"] = "true";
        
        if (RequiredSTIGs.Any())
            tags["STIGCompliance"] = "Required";
        
        return tags;
    }
    
    // ========================================
    // ALLOWED AZURE REGIONS
    // ========================================
    
    /// <summary>
    /// Get allowed Azure regions based on Impact Level
    /// IL2: Commercial + Government
    /// IL4: Government preferred
    /// IL5: Government only
    /// IL6: Restricted Government regions only
    /// </summary>
    public List<string> GetAllowedRegions()
    {
        return ImpactLevel switch
        {
            ImpactLevel.IL2 => new()
            {
                // Commercial Azure regions
                "eastus", "eastus2", "westus", "westus2", "centralus",
                // Azure Government regions
                "usgovvirginia", "usgovtexas", "usgoviowa", "usgovarizona"
            },
            ImpactLevel.IL4 => new()
            {
                // Azure Government regions (preferred for IL4)
                "usgovvirginia", "usgovtexas", "usgoviowa", "usgovarizona",
                // Commercial allowed but not recommended
                "eastus", "westus"
            },
            ImpactLevel.IL5 => new()
            {
                // Azure Government ONLY
                "usgovvirginia", "usgovtexas"
            },
            ImpactLevel.IL6 => new()
            {
                // Restricted to specific Government regions
                "usgovvirginia" // Top Secret/SCI requires dedicated hardware
            },
            _ => new() { "eastus" }
        };
    }
    
    // ========================================
    // SECURITY CONFIGURATION
    // ========================================
    
    /// <summary>
    /// Get minimum TLS version required for Impact Level
    /// </summary>
    public string GetMinimumTlsVersion()
    {
        return ImpactLevel >= ImpactLevel.IL5 ? "1.3" : "1.2";
    }
    
    /// <summary>
    /// Get required Key Vault SKU for Impact Level
    /// </summary>
    public string GetKeyVaultSku()
    {
        return ImpactLevel >= ImpactLevel.IL4 ? "Premium" : "Standard";
    }
    
    /// <summary>
    /// Whether HSM-backed keys are required (IL6 only)
    /// </summary>
    public bool RequiresHsmBackedKeys => ImpactLevel == ImpactLevel.IL6;
    
    /// <summary>
    /// Get recommended VM SKUs for Impact Level
    /// IL6 requires isolated/dedicated compute
    /// </summary>
    public List<string> GetRecommendedVmSkus()
    {
        return ImpactLevel switch
        {
            ImpactLevel.IL6 => new() { "Standard_E64is_v3", "Standard_M128ms" }, // Isolated instances
            ImpactLevel.IL5 => new() { "Standard_D4s_v3", "Standard_E4s_v3" },
            _ => new() { "Standard_B2s", "Standard_D2s_v3" }
        };
    }
    
    // ========================================
    // VALIDATION
    // ========================================
    
    /// <summary>
    /// Validate DoD compliance configuration
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        // Validate DoDAAC format (6 alphanumeric characters)
        if (!string.IsNullOrEmpty(DoDAAC) && !System.Text.RegularExpressions.Regex.IsMatch(DoDAAC, "^[A-Z0-9]{6}$"))
        {
            errors.Add("DoDAAC must be 6 alphanumeric characters (e.g., N12345)");
        }
        
        // Validate mission sponsor is provided for IL4+
        if (ImpactLevel >= ImpactLevel.IL4 && string.IsNullOrEmpty(MissionSponsor))
        {
            errors.Add("Mission Sponsor is required for IL4 and higher environments");
        }
        
        // Validate data classification matches impact level
        if (ImpactLevel == ImpactLevel.IL5 && DataClassification != "Secret")
        {
            errors.Add("IL5 requires 'Secret' data classification");
        }
        
        if (ImpactLevel == ImpactLevel.IL6 && !DataClassification.Contains("Top Secret"))
        {
            errors.Add("IL6 requires 'Top Secret' or 'Top Secret/SCI' data classification");
        }
        
        return errors;
    }
    
    /// <summary>
    /// Get human-readable summary of DoD compliance requirements
    /// </summary>
    public string GetComplianceSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"Impact Level: {ImpactLevel} ({FedRAMPLevel})");
        summary.AppendLine($"Data Classification: {DataClassification}");
        
        if (!string.IsNullOrEmpty(MissionSponsor))
            summary.AppendLine($"Mission Sponsor: {MissionSponsor}");
        
        if (!string.IsNullOrEmpty(DoDAAC))
            summary.AppendLine($"DoDAAC: {DoDAAC}");
        
        summary.AppendLine("\nCompliance Requirements:");
        summary.AppendLine($"- Customer-Managed Keys: {(RequiresCustomerManagedKeys ? "Required" : "Optional")}");
        summary.AppendLine($"- Private Endpoints: {(RequiresPrivateEndpoints ? "Required" : "Optional")}");
        summary.AppendLine($"- FIPS 140-2: {(RequiresFIPS140_2 ? "Required" : "Not Required")}");
        summary.AppendLine($"- CAC Authentication: {(RequiresCAC ? "Required" : "Not Required")}");
        summary.AppendLine($"- ATO Package: {(RequiresATO ? "Required" : "Not Required")}");
        summary.AppendLine($"- eMASS Registration: {(RequireseMASS ? "Required" : "Not Required")}");
        summary.AppendLine($"- Minimum TLS Version: {GetMinimumTlsVersion()}");
        summary.AppendLine($"- Key Vault SKU: {GetKeyVaultSku()}");
        
        summary.AppendLine("\nAllowed Azure Regions:");
        foreach (var region in GetAllowedRegions())
        {
            summary.AppendLine($"- {region}");
        }
        
        summary.AppendLine("\nApplicable Frameworks:");
        foreach (var framework in ComplianceFrameworks)
        {
            summary.AppendLine($"- {framework}");
        }
        
        return summary.ToString();
    }
}
