using System.Text;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using Platform.Engineering.Copilot.Core.Models.ServiceCreation;

namespace Platform.Engineering.Copilot.Core.Services.ServiceCreation;

/// <summary>
/// Generates context-aware prompts for the Service Creation Wizard
/// Provides intelligent suggestions based on previous answers
/// </summary>
public class WizardPromptEngine
{
    private readonly DoDMetadataValidator _validator;
    
    public WizardPromptEngine(DoDMetadataValidator validator)
    {
        _validator = validator;
    }
    
    /// <summary>
    /// Get the prompt for the current wizard step
    /// </summary>
    public string GetPromptForStep(ServiceWizardState state)
    {
        return state.CurrentStep switch
        {
            WizardStep.NotStarted => GetWelcomePrompt(),
            WizardStep.Step1_MissionSponsor => GetMissionSponsorPrompt(),
            WizardStep.Step2_ImpactLevel => GetImpactLevelPrompt(state),
            WizardStep.Step3_Region => GetRegionPrompt(state),
            WizardStep.Step4_DataClassification => GetDataClassificationPrompt(state),
            WizardStep.Step5_Environment => GetEnvironmentPrompt(state),
            WizardStep.Step6_DoDAAC => GetDoDaacPrompt(state),
            WizardStep.Step7_ServiceType => GetServiceTypePrompt(state),
            WizardStep.Step8_TechStack => GetTechStackPrompt(state),
            WizardStep.Completed => GetCompletionSummary(state),
            _ => "Unknown step"
        };
    }
    
    /// <summary>
    /// Validate user response for current step
    /// </summary>
    public WizardStepValidationResult ValidateResponse(ServiceWizardState state, string response)
    {
        return state.CurrentStep switch
        {
            WizardStep.Step1_MissionSponsor => ValidateMissionSponsorResponse(response),
            WizardStep.Step2_ImpactLevel => ValidateImpactLevelResponse(response),
            WizardStep.Step3_Region => ValidateRegionResponse(state, response),
            WizardStep.Step4_DataClassification => ValidateDataClassificationResponse(state, response),
            WizardStep.Step5_Environment => ValidateEnvironmentResponse(response),
            WizardStep.Step6_DoDAAC => ValidateDoDaacResponse(response),
            WizardStep.Step7_ServiceType => ValidateServiceTypeResponse(response),
            WizardStep.Step8_TechStack => ValidateTechStackResponse(response),
            _ => WizardStepValidationResult.Failure("Invalid wizard step")
        };
    }
    
    // ========== PROMPT GENERATORS ==========
    
    private string GetWelcomePrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("🎯 **DoD Service Creation Wizard**");
        sb.AppendLine();
        sb.AppendLine("This wizard will guide you through creating a DoD-compliant service repository.");
        sb.AppendLine("We'll collect information about:");
        sb.AppendLine("  1. Mission Sponsor (PMW, SPAWAR, NAVAIR, etc.)");
        sb.AppendLine("  2. Impact Level (IL2, IL4, IL5, IL6)");
        sb.AppendLine("  3. Azure Region");
        sb.AppendLine("  4. Data Classification");
        sb.AppendLine("  5. Environment (Dev, Staging, Production)");
        sb.AppendLine("  6. DoDAAC (Optional)");
        sb.AppendLine("  7. Service Type & Name");
        sb.AppendLine("  8. Technology Stack");
        sb.AppendLine();
        sb.AppendLine("The wizard will generate:");
        sb.AppendLine("  ✅ IL-compliant infrastructure templates");
        sb.AppendLine("  ✅ STIG security scanning workflows (IL4+)");
        sb.AppendLine("  ✅ Compliance documentation (COMPLIANCE.md, ATO-CHECKLIST.md)");
        sb.AppendLine("  ✅ Application code scaffolding");
        sb.AppendLine("  ✅ CI/CD pipelines");
        sb.AppendLine();
        sb.AppendLine("**Note:** This wizard creates templates only (Phase 1 mode). No resources will be deployed.");
        sb.AppendLine();
        sb.AppendLine("Ready to begin? Type 'yes' to start.");
        
        return sb.ToString();
    }
    
    private string GetMissionSponsorPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 1 of 8: Mission Sponsor");
        sb.AppendLine();
        sb.AppendLine("**What is your Mission Sponsor?**");
        sb.AppendLine();
        sb.AppendLine("Common Navy/DoD sponsors:");
        sb.AppendLine("  - **PMW-120** - Above Water Sensors");
        sb.AppendLine("  - **PMW-130** - Mine Warfare");
        sb.AppendLine("  - **PMW-150** - Cybersecurity");
        sb.AppendLine("  - **PMW-160** - Tactical Networks");
        sb.AppendLine("  - **PMW-170** - Command and Control");
        sb.AppendLine("  - **PMW-180** - Undersea Warfare");
        sb.AppendLine("  - **PMW-200** - Littoral Combat Ships");
        sb.AppendLine("  - **SPAWAR** - Space and Naval Warfare Systems Command");
        sb.AppendLine("  - **NAVAIR** - Naval Air Systems Command");
        sb.AppendLine("  - **NAVSEA** - Naval Sea Systems Command");
        sb.AppendLine("  - **NAVWAR** - Naval Information Warfare Systems Command");
        sb.AppendLine();
        sb.AppendLine("Enter your mission sponsor (or type 'skip' if not applicable):");
        
        return sb.ToString();
    }
    
    private string GetImpactLevelPrompt(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 2 of 8: Impact Level (IL)");
        sb.AppendLine();
        sb.AppendLine("**What is the Impact Level for this system?**");
        sb.AppendLine();
        sb.AppendLine("Impact Levels determine security controls and compliance requirements:");
        sb.AppendLine();
        sb.AppendLine("**IL2** - Public / Unclassified");
        sb.AppendLine("  - Data: Public information only");
        sb.AppendLine("  - Cloud: Commercial Azure or Azure Government");
        sb.AppendLine("  - Compliance: Basic FedRAMP Low");
        sb.AppendLine("  - Example: Public-facing websites");
        sb.AppendLine();
        sb.AppendLine("**IL4** - Controlled Unclassified Information (CUI)");
        sb.AppendLine("  - Data: CUI, FOUO, Law Enforcement Sensitive");
        sb.AppendLine("  - Cloud: Azure Government preferred");
        sb.AppendLine("  - Compliance: FedRAMP Moderate, NIST 800-171");
        sb.AppendLine("  - Requirements: Private endpoints, customer-managed keys");
        sb.AppendLine("  - Example: Personnel records, logistics data");
        sb.AppendLine();
        sb.AppendLine("**IL5** - SECRET");
        sb.AppendLine("  - Data: SECRET classified information");
        sb.AppendLine("  - Cloud: Azure Government ONLY (usgovvirginia, usgovtexas)");
        sb.AppendLine("  - Compliance: FedRAMP High, ATO required, eMASS registration");
        sb.AppendLine("  - Requirements: FIPS 140-2, private endpoints, TLS 1.3");
        sb.AppendLine("  - Example: Operational plans, intelligence data");
        sb.AppendLine();
        sb.AppendLine("**IL6** - TOP SECRET / SCI");
        sb.AppendLine("  - Data: TOP SECRET, Sensitive Compartmented Information");
        sb.AppendLine("  - Cloud: Azure Government ONLY (usgovvirginia)");
        sb.AppendLine("  - Compliance: FedRAMP High, ATO, eMASS, CAC authentication");
        sb.AppendLine("  - Requirements: All IL5 + PIV/CAC, isolated compute");
        sb.AppendLine("  - Example: SIGINT, HUMINT, compartmented programs");
        sb.AppendLine();
        sb.AppendLine("Enter Impact Level (IL2, IL4, IL5, or IL6):");
        
        return sb.ToString();
    }
    
    private string GetRegionPrompt(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 3 of 8: Azure Region");
        sb.AppendLine();
        
        if (state.ImpactLevel.HasValue)
        {
            var allowedRegions = new DoDComplianceSpec { ImpactLevel = state.ImpactLevel.Value }.GetAllowedRegions();
            
            sb.AppendLine($"**Select Azure region for {state.ImpactLevel} deployment:**");
            sb.AppendLine();
            
            var govRegions = allowedRegions.Where(r => r.StartsWith("usgov")).ToList();
            var commercialRegions = allowedRegions.Where(r => !r.StartsWith("usgov")).ToList();
            
            if (govRegions.Any())
            {
                sb.AppendLine("**Azure Government (RECOMMENDED for " + state.ImpactLevel + "):**");
                foreach (var region in govRegions)
                {
                    sb.AppendLine($"  - {region} ✅");
                }
                sb.AppendLine();
            }
            
            if (commercialRegions.Any())
            {
                var suffix = state.ImpactLevel >= ImpactLevel.IL5 ? " ❌ NOT ALLOWED" : " ⚠️ Not recommended";
                sb.AppendLine("**Commercial Azure" + suffix + ":**");
                foreach (var region in commercialRegions)
                {
                    sb.AppendLine($"  - {region}");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("**Select Azure region:**");
            sb.AppendLine("  - usgovvirginia (Azure Government - Virginia)");
            sb.AppendLine("  - usgovtexas (Azure Government - Texas)");
            sb.AppendLine("  - eastus (Commercial Azure - East US)");
            sb.AppendLine("  - westus (Commercial Azure - West US)");
            sb.AppendLine();
        }
        
        sb.AppendLine("Enter region name:");
        
        return sb.ToString();
    }
    
    private string GetDataClassificationPrompt(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 4 of 8: Data Classification");
        sb.AppendLine();
        sb.AppendLine("**What type of data will this system process?**");
        sb.AppendLine();
        
        if (state.ImpactLevel == ImpactLevel.IL2)
        {
            sb.AppendLine("For IL2, typical classifications:");
            sb.AppendLine("  - **Public** - Information intended for public release");
            sb.AppendLine("  - **Unclassified** - Not sensitive but not for public release");
        }
        else if (state.ImpactLevel == ImpactLevel.IL4)
        {
            sb.AppendLine("For IL4, typical classifications:");
            sb.AppendLine("  - **CUI** - Controlled Unclassified Information");
            sb.AppendLine("  - **FOUO** - For Official Use Only");
            sb.AppendLine("  - **LES** - Law Enforcement Sensitive");
        }
        else if (state.ImpactLevel == ImpactLevel.IL5)
        {
            sb.AppendLine("For IL5, classification:");
            sb.AppendLine("  - **Secret** - Classified information at SECRET level");
        }
        else if (state.ImpactLevel == ImpactLevel.IL6)
        {
            sb.AppendLine("For IL6, classifications:");
            sb.AppendLine("  - **Top Secret** - Classified information at TOP SECRET level");
            sb.AppendLine("  - **Top Secret/SCI** - Sensitive Compartmented Information");
        }
        
        sb.AppendLine();
        sb.AppendLine("Enter data classification:");
        
        return sb.ToString();
    }
    
    private string GetEnvironmentPrompt(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 5 of 8: Environment");
        sb.AppendLine();
        sb.AppendLine("**What environment is this for?**");
        sb.AppendLine();
        sb.AppendLine("  - **Development** / **Dev** - Development environment");
        sb.AppendLine("  - **Test** - Testing environment");
        sb.AppendLine("  - **Staging** - Pre-production staging");
        sb.AppendLine("  - **Production** / **Prod** - Production environment");
        sb.AppendLine();
        sb.AppendLine("Enter environment name:");
        
        return sb.ToString();
    }
    
    private string GetDoDaacPrompt(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 6 of 8: DoDAAC (Optional)");
        sb.AppendLine();
        sb.AppendLine("**What is your DoDAAC (DoD Activity Address Code)?**");
        sb.AppendLine();
        sb.AppendLine("DoDAAC is a 6-character code identifying your DoD organization.");
        sb.AppendLine("Format: 6 alphanumeric characters (e.g., N12345, HQ0001)");
        sb.AppendLine();
        sb.AppendLine("This is optional but recommended for cost tracking and resource tagging.");
        sb.AppendLine();
        sb.AppendLine("Enter DoDAAC (or type 'skip'):");
        
        return sb.ToString();
    }
    
    private string GetServiceTypePrompt(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 7 of 8: Service Type & Name");
        sb.AppendLine();
        sb.AppendLine("**What type of service are you creating?**");
        sb.AppendLine();
        sb.AppendLine("  - **API** - REST API / GraphQL API");
        sb.AppendLine("  - **Web App** - Web application with UI");
        sb.AppendLine("  - **Worker Service** - Background processing / message queue worker");
        sb.AppendLine("  - **Database** - Managed database service");
        sb.AppendLine("  - **Microservice** - Microservice architecture component");
        sb.AppendLine("  - **Function** - Serverless function (Azure Functions / Lambda)");
        sb.AppendLine();
        sb.AppendLine("Enter service type, then service name on the next line.");
        sb.AppendLine("Example:");
        sb.AppendLine("  Type: API");
        sb.AppendLine("  Name: pmw150-logistics-api");
        
        return sb.ToString();
    }
    
    private string GetTechStackPrompt(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Step 8 of 8: Technology Stack");
        sb.AppendLine();
        sb.AppendLine("**Select your technology stack:**");
        sb.AppendLine();
        sb.AppendLine("**Programming Language:**");
        sb.AppendLine("  - .NET (C#)");
        sb.AppendLine("  - Java");
        sb.AppendLine("  - Python");
        sb.AppendLine("  - Node.js (JavaScript/TypeScript)");
        sb.AppendLine("  - Go");
        sb.AppendLine("  - Rust");
        sb.AppendLine();
        sb.AppendLine("**Database (optional):**");
        sb.AppendLine("  - PostgreSQL");
        sb.AppendLine("  - SQL Server");
        sb.AppendLine("  - MySQL");
        sb.AppendLine("  - Cosmos DB");
        sb.AppendLine("  - MongoDB");
        sb.AppendLine("  - Redis");
        sb.AppendLine("  - None");
        sb.AppendLine();
        sb.AppendLine("**Infrastructure Format:**");
        sb.AppendLine("  - Terraform");
        sb.AppendLine("  - Bicep");
        sb.AppendLine("  - Kubernetes");
        sb.AppendLine();
        sb.AppendLine("**Compute Platform:**");
        sb.AppendLine("  - AKS (Azure Kubernetes Service)");
        sb.AppendLine("  - App Service (Azure Web Apps)");
        sb.AppendLine("  - Container Apps (Azure Container Apps)");
        sb.AppendLine("  - Functions (Serverless)");
        sb.AppendLine();
        sb.AppendLine("Enter your selections in format:");
        sb.AppendLine("  Language: [language]");
        sb.AppendLine("  Database: [database or 'none']");
        sb.AppendLine("  Infrastructure: [format]");
        sb.AppendLine("  Platform: [platform]");
        
        return sb.ToString();
    }
    
    private string GetCompletionSummary(ServiceWizardState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine("✅ **Service Creation Wizard Complete!**");
        sb.AppendLine();
        sb.AppendLine("**Configuration Summary:**");
        sb.AppendLine($"  - Mission Sponsor: {state.MissionSponsor ?? "Not specified"}");
        sb.AppendLine($"  - Impact Level: {state.ImpactLevel}");
        sb.AppendLine($"  - Region: {state.Region}");
        sb.AppendLine($"  - Classification: {state.DataClassification}");
        sb.AppendLine($"  - Environment: {state.Environment}");
        sb.AppendLine($"  - DoDAAC: {state.DoDAAC ?? "Not specified"}");
        sb.AppendLine($"  - Service: {state.ServiceType} - {state.ServiceName}");
        sb.AppendLine($"  - Language: {state.ProgrammingLanguage}");
        sb.AppendLine($"  - Database: {state.DatabaseType ?? "None"}");
        sb.AppendLine($"  - Infrastructure: {state.InfrastructureFormat}");
        sb.AppendLine($"  - Platform: {state.ComputePlatform}");
        sb.AppendLine();
        sb.AppendLine("Ready to generate repository. Use 'generate_service_repository' to create templates.");
        
        return sb.ToString();
    }
    
    // ========== VALIDATORS ==========
    
    private WizardStepValidationResult ValidateMissionSponsorResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response) || response.Trim().Equals("skip", StringComparison.OrdinalIgnoreCase))
        {
            return WizardStepValidationResult.Success();
        }
        
        var validation = _validator.ValidateMissionSponsor(response.Trim());
        
        var result = new WizardStepValidationResult { IsValid = validation.IsValid };
        result.Errors.AddRange(validation.Errors);
        result.Warnings.AddRange(validation.Warnings);
        result.Recommendations.AddRange(validation.Recommendations);
        
        return result;
    }
    
    private WizardStepValidationResult ValidateImpactLevelResponse(string response)
    {
        var normalized = response.Trim().ToUpperInvariant();
        
        if (Enum.TryParse<ImpactLevel>(normalized, out _))
        {
            return WizardStepValidationResult.Success();
        }
        
        return WizardStepValidationResult.Failure(
            "Invalid Impact Level. Please enter: IL2, IL4, IL5, or IL6");
    }
    
    private WizardStepValidationResult ValidateRegionResponse(ServiceWizardState state, string response)
    {
        if (!state.ImpactLevel.HasValue)
        {
            return WizardStepValidationResult.Failure("Impact Level not set");
        }
        
        var validation = _validator.ValidateRegionForImpactLevel(response.Trim(), state.ImpactLevel.Value);
        
        var result = new WizardStepValidationResult { IsValid = validation.IsValid };
        result.Errors.AddRange(validation.Errors);
        result.Warnings.AddRange(validation.Warnings);
        result.Recommendations.AddRange(validation.Recommendations);
        
        return result;
    }
    
    private WizardStepValidationResult ValidateDataClassificationResponse(ServiceWizardState state, string response)
    {
        if (!state.ImpactLevel.HasValue)
        {
            return WizardStepValidationResult.Failure("Impact Level not set");
        }
        
        var validation = _validator.ValidateDataClassification(response.Trim(), state.ImpactLevel.Value);
        
        var result = new WizardStepValidationResult { IsValid = validation.IsValid };
        result.Errors.AddRange(validation.Errors);
        result.Warnings.AddRange(validation.Warnings);
        result.Recommendations.AddRange(validation.Recommendations);
        
        return result;
    }
    
    private WizardStepValidationResult ValidateEnvironmentResponse(string response)
    {
        var validation = _validator.ValidateEnvironment(response.Trim());
        
        var result = new WizardStepValidationResult { IsValid = validation.IsValid };
        result.Errors.AddRange(validation.Errors);
        result.Warnings.AddRange(validation.Warnings);
        result.Recommendations.AddRange(validation.Recommendations);
        
        return result;
    }
    
    private WizardStepValidationResult ValidateDoDaacResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response) || response.Trim().Equals("skip", StringComparison.OrdinalIgnoreCase))
        {
            return WizardStepValidationResult.Success();
        }
        
        var validation = _validator.ValidateDoDAAC(response.Trim());
        
        var result = new WizardStepValidationResult { IsValid = validation.IsValid };
        result.Errors.AddRange(validation.Errors);
        result.Warnings.AddRange(validation.Warnings);
        result.Recommendations.AddRange(validation.Recommendations);
        
        return result;
    }
    
    private WizardStepValidationResult ValidateServiceTypeResponse(string response)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 1)
        {
            return WizardStepValidationResult.Failure("Please provide service type and name");
        }
        
        return WizardStepValidationResult.Success();
    }
    
    private WizardStepValidationResult ValidateTechStackResponse(string response)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 3)
        {
            return WizardStepValidationResult.Failure(
                "Please provide: Language, Database, Infrastructure, and Platform");
        }
        
        return WizardStepValidationResult.Success();
    }
}
