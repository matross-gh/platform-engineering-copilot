using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using System.Text.RegularExpressions;

namespace Platform.Engineering.Copilot.Core.Services.ServiceCreation;

/// <summary>
/// Validates DoD-specific metadata for Navy/DoD service creation
/// Ensures compliance with IL requirements, region restrictions, and organizational standards
/// </summary>
public class DoDMetadataValidator
{
    private static readonly Regex DoDaacPattern = new("^[A-Z0-9]{6}$", RegexOptions.Compiled);
    
    private static readonly HashSet<string> ValidMissionSponsors = new()
    {
        "PMW-120", "PMW-130", "PMW-150", "PMW-160", "PMW-170", "PMW-180", "PMW-190",
        "PMW-200", "PMW-205", "PMW-240", "PMW-260", "PMW-280",
        "SPAWAR", "NAVAIR", "NAVSEA", "NAVWAR", "NIWC", "NIWC Atlantic", "NIWC Pacific",
        "NCCIC", "DISA", "DCMA", "DLA"
    };
    
    private static readonly HashSet<string> AzureGovernmentRegions = new()
    {
        "usgovvirginia", "usgovtexas", "usgoviowa", "usgovarizona"
    };
    
    /// <summary>
    /// Validation result for DoD metadata
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        
        public static ValidationResult Success() => new();
        public static ValidationResult Fail(string error) => new() { Errors = new List<string> { error } };
        public static ValidationResult Warn(string warning) => new() { Warnings = new List<string> { warning } };
    }
    
    /// <summary>
    /// Validate DoDAAC format (6 alphanumeric characters)
    /// </summary>
    public ValidationResult ValidateDoDAAC(string? dodaac)
    {
        if (string.IsNullOrEmpty(dodaac))
            return ValidationResult.Warn("DoDAAC not provided - recommended for DoD environments");
        
        if (!DoDaacPattern.IsMatch(dodaac))
            return ValidationResult.Fail("DoDAAC must be exactly 6 alphanumeric characters (e.g., N12345, HQ0001)");
        
        return ValidationResult.Success();
    }
    
    /// <summary>
    /// Validate mission sponsor against known DoD/Navy programs
    /// </summary>
    public ValidationResult ValidateMissionSponsor(string? sponsor)
    {
        if (string.IsNullOrEmpty(sponsor))
            return ValidationResult.Warn("Mission Sponsor not provided - recommended for tracking and cost allocation");
        
        if (!ValidMissionSponsors.Contains(sponsor))
        {
            var result = ValidationResult.Warn($"Mission Sponsor '{sponsor}' not in known list. Valid sponsors include: {string.Join(", ", ValidMissionSponsors.Take(10))}...");
            result.Recommendations.Add("Verify sponsor with your program office");
            return result;
        }
        
        return ValidationResult.Success();
    }
    
    /// <summary>
    /// Validate Azure region is allowed for the specified Impact Level
    /// </summary>
    public ValidationResult ValidateRegionForImpactLevel(string region, ImpactLevel level)
    {
        var allowedRegions = GetAllowedRegionsForImpactLevel(level);
        
        if (!allowedRegions.Contains(region.ToLowerInvariant()))
        {
            return ValidationResult.Fail(
                $"Region '{region}' is not allowed for {level}. " +
                $"Allowed regions: {string.Join(", ", allowedRegions)}");
        }
        
        // Warn if using commercial Azure for IL4+
        if (level >= ImpactLevel.IL4 && !AzureGovernmentRegions.Contains(region.ToLowerInvariant()))
        {
            var result = ValidationResult.Warn(
                $"Using commercial Azure region '{region}' for {level}. " +
                $"Azure Government regions are strongly recommended: {string.Join(", ", AzureGovernmentRegions)}");
            result.Recommendations.Add($"Consider migrating to Azure Government for {level} compliance");
            return result;
        }
        
        // Error for IL5+ using commercial Azure
        if (level >= ImpactLevel.IL5 && !AzureGovernmentRegions.Contains(region.ToLowerInvariant()))
        {
            return ValidationResult.Fail(
                $"{level} REQUIRES Azure Government regions only. " +
                $"Commercial Azure region '{region}' is not authorized. " +
                $"Use: {string.Join(", ", AzureGovernmentRegions)}");
        }
        
        return ValidationResult.Success();
    }
    
    /// <summary>
    /// Validate data classification matches Impact Level requirements
    /// </summary>
    public ValidationResult ValidateDataClassification(string? classification, ImpactLevel level)
    {
        if (string.IsNullOrEmpty(classification))
            return ValidationResult.Fail("Data classification is required");
        
        var errors = new List<string>();
        
        // IL2: Public/Unclassified only
        if (level == ImpactLevel.IL2 && classification != "Unclassified" && classification != "Public")
        {
            errors.Add("IL2 is only for Unclassified or Public data");
        }
        
        // IL4: CUI
        if (level == ImpactLevel.IL4 && !classification.Contains("CUI") && classification != "Unclassified")
        {
            return ValidationResult.Warn("IL4 is typically for Controlled Unclassified Information (CUI)");
        }
        
        // IL5: Secret
        if (level == ImpactLevel.IL5 && !classification.Contains("Secret"))
        {
            errors.Add("IL5 requires 'Secret' data classification");
        }
        
        // IL6: Top Secret/SCI
        if (level == ImpactLevel.IL6 && !classification.Contains("Top Secret"))
        {
            errors.Add("IL6 requires 'Top Secret' or 'Top Secret/SCI' data classification");
        }
        
        if (errors.Any())
            return new ValidationResult { Errors = errors };
        
        return ValidationResult.Success();
    }
    
    /// <summary>
    /// Validate environment type is appropriate
    /// </summary>
    public ValidationResult ValidateEnvironment(string? environment)
    {
        if (string.IsNullOrEmpty(environment))
            return ValidationResult.Warn("Environment not specified - defaulting to 'Development'");
        
        var validEnvironments = new[] { "Development", "Dev", "Test", "Staging", "Production", "Prod" };
        if (!validEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationResult.Warn(
                $"Environment '{environment}' not standard. " +
                $"Recommended: {string.Join(", ", validEnvironments)}");
        }
        
        return ValidationResult.Success();
    }
    
    /// <summary>
    /// Validate service name follows DoD naming conventions
    /// </summary>
    public ValidationResult ValidateServiceName(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
            return ValidationResult.Fail("Service name is required");
        
        // Check length (3-63 characters for Azure resource naming)
        if (serviceName.Length < 3 || serviceName.Length > 63)
            return ValidationResult.Fail("Service name must be between 3 and 63 characters");
        
        // Check for valid characters (alphanumeric and hyphens only)
        if (!Regex.IsMatch(serviceName, "^[a-z0-9-]+$"))
            return ValidationResult.Fail("Service name must contain only lowercase letters, numbers, and hyphens");
        
        // Can't start or end with hyphen
        if (serviceName.StartsWith('-') || serviceName.EndsWith('-'))
            return ValidationResult.Fail("Service name cannot start or end with a hyphen");
        
        // Recommend including mission sponsor in name
        var result = ValidationResult.Success();
        if (!serviceName.Contains("pmw", StringComparison.OrdinalIgnoreCase))
        {
            result.Recommendations.Add("Consider including mission sponsor in service name (e.g., 'pmw120-logistics-api')");
        }
        
        return result;
    }
    
    /// <summary>
    /// Comprehensive validation of DoD compliance spec
    /// </summary>
    public ValidationResult ValidateDoDCompliance(DoDComplianceSpec? spec, string serviceName, string region, string? environment)
    {
        var result = new ValidationResult();
        
        if (spec == null)
        {
            result.Warnings.Add("No DoD compliance metadata provided - using defaults");
            return result;
        }
        
        // Validate service name
        var nameValidation = ValidateServiceName(serviceName);
        result.Errors.AddRange(nameValidation.Errors);
        result.Warnings.AddRange(nameValidation.Warnings);
        result.Recommendations.AddRange(nameValidation.Recommendations);
        
        // Validate DoDAAC
        var dodaacValidation = ValidateDoDAAC(spec.DoDAAC);
        result.Errors.AddRange(dodaacValidation.Errors);
        result.Warnings.AddRange(dodaacValidation.Warnings);
        result.Recommendations.AddRange(dodaacValidation.Recommendations);
        
        // Validate Mission Sponsor
        var sponsorValidation = ValidateMissionSponsor(spec.MissionSponsor);
        result.Errors.AddRange(sponsorValidation.Errors);
        result.Warnings.AddRange(sponsorValidation.Warnings);
        result.Recommendations.AddRange(sponsorValidation.Recommendations);
        
        // Validate Region for Impact Level
        var regionValidation = ValidateRegionForImpactLevel(region, spec.ImpactLevel);
        result.Errors.AddRange(regionValidation.Errors);
        result.Warnings.AddRange(regionValidation.Warnings);
        result.Recommendations.AddRange(regionValidation.Recommendations);
        
        // Validate Data Classification
        var classificationValidation = ValidateDataClassification(spec.DataClassification, spec.ImpactLevel);
        result.Errors.AddRange(classificationValidation.Errors);
        result.Warnings.AddRange(classificationValidation.Warnings);
        result.Recommendations.AddRange(classificationValidation.Recommendations);
        
        // Validate Environment
        var envValidation = ValidateEnvironment(environment);
        result.Errors.AddRange(envValidation.Errors);
        result.Warnings.AddRange(envValidation.Warnings);
        result.Recommendations.AddRange(envValidation.Recommendations);
        
        // IL4+ requires Mission Sponsor
        if (spec.ImpactLevel >= ImpactLevel.IL4 && string.IsNullOrEmpty(spec.MissionSponsor))
        {
            result.Errors.Add($"{spec.ImpactLevel} requires Mission Sponsor to be specified");
        }
        
        // IL5+ requires ATO package preparation
        if (spec.RequiresATO)
        {
            result.Recommendations.Add($"{spec.ImpactLevel} requires ATO package preparation - ensure you have ISSO/ISSM approval");
        }
        
        // IL5+ requires eMASS registration
        if (spec.RequireseMASS)
        {
            result.Recommendations.Add($"{spec.ImpactLevel} requires eMASS system registration before deployment");
        }
        
        return result;
    }
    
    /// <summary>
    /// Get allowed regions for Impact Level
    /// </summary>
    private List<string> GetAllowedRegionsForImpactLevel(ImpactLevel level)
    {
        return level switch
        {
            ImpactLevel.IL2 => new()
            {
                "eastus", "eastus2", "westus", "westus2", "centralus",
                "usgovvirginia", "usgovtexas", "usgoviowa", "usgovarizona"
            },
            ImpactLevel.IL4 => new()
            {
                "usgovvirginia", "usgovtexas", "usgoviowa", "usgovarizona",
                "eastus", "westus" // Allowed but not recommended
            },
            ImpactLevel.IL5 => new()
            {
                "usgovvirginia", "usgovtexas"
            },
            ImpactLevel.IL6 => new()
            {
                "usgovvirginia" // Restricted
            },
            _ => new() { "eastus" }
        };
    }
}
