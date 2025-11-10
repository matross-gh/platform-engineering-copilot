using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using Platform.Engineering.Copilot.Core.Models.ServiceCreation;
using Platform.Engineering.Copilot.Core.Plugins;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.Core.Services.Generators.Documentation;
using Platform.Engineering.Copilot.Core.Services.ServiceCreation;
using System.ComponentModel;
using System.Text;

namespace Platform.Engineering.Copilot.Infrastructure.Agent.Plugins;

/// <summary>
/// Plugin for interactive DoD Service Creation Wizard
/// Provides 8-step guided workflow for IL2-IL6 compliant service generation
/// </summary>
public class ServiceWizardPlugin : BaseSupervisorPlugin
{
    private readonly ServiceWizardStateManager _stateManager;
    private readonly WizardPromptEngine _promptEngine;
    private readonly DoDMetadataValidator _validator;
    private readonly DynamicTemplateGeneratorService _templateGenerator;
    private readonly DoDDocumentationGenerator _docGenerator;
    
    public ServiceWizardPlugin(
        ILogger<ServiceWizardPlugin> logger,
        Kernel kernel,
        ServiceWizardStateManager stateManager,
        WizardPromptEngine promptEngine,
        DoDMetadataValidator validator,
        DynamicTemplateGeneratorService templateGenerator,
        DoDDocumentationGenerator docGenerator)
        : base(logger, kernel)
    {
        _stateManager = stateManager;
        _promptEngine = promptEngine;
        _validator = validator;
        _templateGenerator = templateGenerator;
        _docGenerator = docGenerator;
    }
    
    /// <summary>
    /// Start a new service creation wizard session
    /// </summary>
    [KernelFunction("start_service_wizard")]
    [Description("Start interactive DoD Service Creation Wizard. Returns welcome prompt and first question.")]
    [return: Description("Welcome message and Step 1 prompt")]
    public async Task<string> StartWizardAsync()
    {
        // Create new session
        var state = await _stateManager.CreateSessionAsync();
        
        var sb = new StringBuilder();
        sb.AppendLine(_promptEngine.GetPromptForStep(state));
        sb.AppendLine();
        sb.AppendLine($"**Session ID:** `{state.SessionId}`");
        sb.AppendLine("(Save this ID to resume later)");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Provide answer to current wizard step and advance
    /// </summary>
    [KernelFunction("wizard_next_step")]
    [Description("Submit answer to current wizard question and advance to next step. Returns next prompt or completion message.")]
    [return: Description("Next wizard prompt or completion summary")]
    public async Task<string> NextStepAsync(
        [Description("Wizard session ID")] string sessionId,
        [Description("User's answer to current question")] string answer)
    {
        var state = await _stateManager.GetStateAsync(sessionId);
        
        if (state == null)
        {
            return "❌ Session not found. Use 'start_service_wizard' to begin.";
        }
        
        if (state.IsComplete)
        {
            return "✅ Wizard already complete. Use 'generate_service_repository' to create templates.";
        }
        
        // Special handling for welcome step
        if (state.CurrentStep == WizardStep.NotStarted)
        {
            if (answer.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                state.AdvanceToNextStep();
                await _stateManager.SaveStateAsync(state);
                return _promptEngine.GetPromptForStep(state);
            }
            else
            {
                return "Type 'yes' to start the wizard, or 'cancel' to exit.";
            }
        }
        
        // Validate response
        var validation = _promptEngine.ValidateResponse(state, answer);
        
        if (!validation.IsValid)
        {
            var sb = new StringBuilder();
            sb.AppendLine("❌ **Validation Failed:**");
            foreach (var error in validation.Errors)
            {
                sb.AppendLine($"  - {error}");
            }
            sb.AppendLine();
            sb.AppendLine("Please try again:");
            return sb.ToString();
        }
        
        // Store answer and advance
        await StoreAnswerAsync(state, answer);
        
        // Show warnings if any
        var warnings = new StringBuilder();
        if (validation.Warnings.Count > 0)
        {
            warnings.AppendLine("⚠️ **Warnings:**");
            foreach (var warning in validation.Warnings)
            {
                warnings.AppendLine($"  - {warning}");
            }
            warnings.AppendLine();
        }
        
        if (validation.Recommendations.Count > 0)
        {
            warnings.AppendLine("💡 **Recommendations:**");
            foreach (var rec in validation.Recommendations)
            {
                warnings.AppendLine($"  - {rec}");
            }
            warnings.AppendLine();
        }
        
        state.AdvanceToNextStep();
        await _stateManager.SaveStateAsync(state);
        
        return warnings + _promptEngine.GetPromptForStep(state);
    }
    
    /// <summary>
    /// Go back to previous wizard step
    /// </summary>
    [KernelFunction("wizard_go_back")]
    [Description("Go back to previous wizard step to change an answer")]
    [return: Description("Previous step prompt")]
    public async Task<string> GoBackAsync(
        [Description("Wizard session ID")] string sessionId)
    {
        var state = await _stateManager.GetStateAsync(sessionId);
        
        if (state == null)
        {
            return "❌ Session not found.";
        }
        
        if (state.GoBackToPreviousStep())
        {
            await _stateManager.SaveStateAsync(state);
            return $"↩️ **Went back to previous step**\n\n{_promptEngine.GetPromptForStep(state)}";
        }
        else
        {
            return "❌ Already at first step. Cannot go back further.";
        }
    }
    
    /// <summary>
    /// Start over with a new wizard session
    /// </summary>
    [KernelFunction("wizard_start_over")]
    [Description("Cancel current wizard session and start fresh")]
    [return: Description("New wizard welcome prompt")]
    public async Task<string> StartOverAsync(
        [Description("Current wizard session ID")] string sessionId)
    {
        await _stateManager.DeleteSessionAsync(sessionId);
        return await StartWizardAsync();
    }
    
    /// <summary>
    /// Get help for a specific term or concept
    /// </summary>
    [KernelFunction("wizard_help")]
    [Description("Get help about DoD terms like DoDAAC, Impact Level, Mission Sponsor, FIPS 140-2, CAC, ATO, eMASS")]
    [return: Description("Detailed explanation of the term")]
    public string GetHelp(
        [Description("Term to explain: DoDAAC, IL, Impact Level, Mission Sponsor, FIPS, CAC, ATO, eMASS, RMF, STIG, CUI, Secret, Top Secret")] string term)
    {
        var normalized = term.Trim().ToLowerInvariant();
        
        return normalized switch
        {
            "dodaac" => "**DoDAAC (DoD Activity Address Code)**: A 6-character alphanumeric code that uniquely identifies a DoD organization or unit. Format: 6 characters (e.g., N12345, HQ0001). Used for cost tracking, resource tagging, and organizational identification.",
            
            "il" or "impact level" => "**Impact Level (IL)**: DoD Cloud Computing Security Requirements Guide classification for data sensitivity:\n  - **IL2**: Public/Unclassified\n  - **IL4**: Controlled Unclassified Information (CUI)\n  - **IL5**: SECRET classified\n  - **IL6**: TOP SECRET/SCI\nHigher levels require stricter security controls.",
            
            "mission sponsor" => "**Mission Sponsor**: The DoD program office or organization sponsoring the system. Examples: PMW-120 (Above Water Sensors), PMW-150 (Cybersecurity), SPAWAR, NAVAIR, NAVSEA. Required for IL4+ systems for accountability and cost allocation.",
            
            "fips" or "fips 140-2" => "**FIPS 140-2**: Federal Information Processing Standard for cryptographic modules. Required for IL5+ systems. Ensures encryption uses government-validated cryptographic algorithms. Azure Key Vault Premium SKU provides FIPS 140-2 Level 2 validated HSMs.",
            
            "cac" => "**CAC (Common Access Card)**: DoD smart card for authentication using PIV (Personal Identity Verification) certificates. Required for IL6 systems. Provides multi-factor authentication via something you have (card) + something you know (PIN).",
            
            "ato" => "**ATO (Authority to Operate)**: Formal declaration by an Authorizing Official that a system is authorized to operate. Required for IL5+ systems. Part of RMF process. Typically granted for 3 years. Requires SSP, SAR, POA&M, and risk acceptance.",
            
            "emass" => "**eMASS (Enterprise Mission Assurance Support Service)**: DoD system for tracking security authorizations and continuous monitoring. Required for IL5+ systems. Used to manage ATO packages, POA&Ms, and compliance artifacts.",
            
            "rmf" => "**RMF (Risk Management Framework)**: NIST SP 800-37 process for managing cybersecurity risk:\n  1. Categorize (FIPS 199)\n  2. Select Controls (NIST 800-53)\n  3. Implement Controls\n  4. Assess Controls\n  5. Authorize System (ATO)\n  6. Monitor (Continuous)\nRequired for all DoD systems.",
            
            "stig" => "**STIG (Security Technical Implementation Guide)**: Configuration standards published by DISA for hardening IT systems. Provides specific security settings for OS, databases, applications. Automated scanning tools: SCAP, Nessus/ACAS.",
            
            "cui" => "**CUI (Controlled Unclassified Information)**: Unclassified information that requires safeguarding. Examples: FOUO, Law Enforcement Sensitive, Privacy Act data. Requires IL4 minimum. Governed by NIST 800-171.",
            
            "secret" => "**SECRET**: Classified information where unauthorized disclosure could cause serious damage to national security. Requires IL5. Must use Azure Government Cloud only. Requires FIPS 140-2, ATO, eMASS registration.",
            
            "top secret" or "ts" => "**TOP SECRET**: Highest classification level where unauthorized disclosure could cause exceptionally grave damage to national security. Requires IL6. SCI (Sensitive Compartmented Information) requires additional access controls. Azure Government usgovvirginia region only. Requires CAC authentication, isolated compute.",
            
            _ => $"No help available for '{term}'. Available terms: DoDAAC, Impact Level, Mission Sponsor, FIPS 140-2, CAC, ATO, eMASS, RMF, STIG, CUI, Secret, Top Secret"
        };
    }
    
    /// <summary>
    /// Get current wizard session status
    /// </summary>
    [KernelFunction("wizard_status")]
    [Description("Get current wizard session progress and collected information")]
    [return: Description("Session status summary")]
    public async Task<string> GetStatusAsync(
        [Description("Wizard session ID")] string sessionId)
    {
        var state = await _stateManager.GetStateAsync(sessionId);
        
        if (state == null)
        {
            return "❌ Session not found.";
        }
        
        var sb = new StringBuilder();
        sb.AppendLine($"📊 **Wizard Session Status**");
        sb.AppendLine($"Session ID: `{state.SessionId}`");
        sb.AppendLine($"Progress: {state.CompletionPercentage}% ({state.CurrentStep} of {WizardStep.Completed})");
        sb.AppendLine($"Started: {state.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Last Updated: {state.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        
        sb.AppendLine("**Collected Information:**");
        if (!string.IsNullOrEmpty(state.MissionSponsor))
            sb.AppendLine($"  ✅ Mission Sponsor: {state.MissionSponsor}");
        if (state.ImpactLevel.HasValue)
            sb.AppendLine($"  ✅ Impact Level: {state.ImpactLevel}");
        if (!string.IsNullOrEmpty(state.Region))
            sb.AppendLine($"  ✅ Region: {state.Region}");
        if (!string.IsNullOrEmpty(state.DataClassification))
            sb.AppendLine($"  ✅ Data Classification: {state.DataClassification}");
        if (!string.IsNullOrEmpty(state.Environment))
            sb.AppendLine($"  ✅ Environment: {state.Environment}");
        if (!string.IsNullOrEmpty(state.DoDAAC))
            sb.AppendLine($"  ✅ DoDAAC: {state.DoDAAC}");
        if (!string.IsNullOrEmpty(state.ServiceName))
            sb.AppendLine($"  ✅ Service: {state.ServiceType} - {state.ServiceName}");
        if (!string.IsNullOrEmpty(state.ProgrammingLanguage))
            sb.AppendLine($"  ✅ Language: {state.ProgrammingLanguage}");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generate service repository from completed wizard session
    /// </summary>
    [KernelFunction("generate_service_repository")]
    [Description("Generate IL-compliant service repository templates from completed wizard session. Templates stored in SharedMemory for workspace creation.")]
    [return: Description("Generation status and file count")]
    public async Task<string> GenerateServiceRepositoryAsync(
        [Description("Wizard session ID")] string sessionId)
    {
        var state = await _stateManager.GetStateAsync(sessionId);
        
        if (state == null)
        {
            return "❌ Session not found.";
        }
        
        if (!state.IsComplete)
        {
            return $"❌ Wizard not complete. Currently at step {state.CurrentStep}. Complete all 8 steps first.";
        }
        
        // Convert wizard state to TemplateGenerationRequest
        var request = ConvertWizardStateToRequest(state);
        
        // Validate DoD compliance
        var validation = _validator.ValidateDoDCompliance(
            request.DoDCompliance,
            state.ServiceName!,
            state.Region!,
            state.Environment);
        
        if (!validation.IsValid)
        {
            var sb = new StringBuilder();
            sb.AppendLine("❌ **DoD Compliance Validation Failed:**");
            foreach (var error in validation.Errors)
            {
                sb.AppendLine($"  - {error}");
            }
            return sb.ToString();
        }
        
        // Generate templates
        var result = await _templateGenerator.GenerateTemplateAsync(request);
        
        if (!result.Success)
        {
            return $"❌ Template generation failed: {result.ErrorMessage}";
        }
        
        // Generate DoD documentation
        var dodDocs = _docGenerator.GenerateDoDDocumentation(request);
        
        // Add DoD docs to result
        foreach (var doc in dodDocs)
        {
            result.Files[doc.Key] = doc.Value;
        }
        
        var summary = new StringBuilder();
        summary.AppendLine("✅ **Service Repository Generated Successfully!**");
        summary.AppendLine();
        summary.AppendLine($"**Service:** {state.ServiceName}");
        summary.AppendLine($"**Impact Level:** {state.ImpactLevel}");
        summary.AppendLine($"**Classification:** {state.DataClassification}");
        summary.AppendLine($"**Region:** {state.Region}");
        summary.AppendLine();
        summary.AppendLine($"**Files Generated:** {result.Files.Count}");
        summary.AppendLine();
        
        // Categorize files
        var categories = new Dictionary<string, List<string>>
        {
            ["Application Code"] = result.Files.Keys.Where(k => k.Contains("/src/") || k.EndsWith(".cs") || k.EndsWith(".py") || k.EndsWith(".js") || k.EndsWith(".java")).ToList(),
            ["Infrastructure"] = result.Files.Keys.Where(k => k.Contains("/terraform/") || k.Contains("/bicep/") || k.Contains("/k8s/")).ToList(),
            ["CI/CD Workflows"] = result.Files.Keys.Where(k => k.Contains(".github/workflows/")).ToList(),
            ["DoD Documentation"] = result.Files.Keys.Where(k => k.Contains("COMPLIANCE.md") || k.Contains("ATO-CHECKLIST.md") || k.Contains("SECURITY.md")).ToList(),
            ["Database"] = result.Files.Keys.Where(k => k.Contains("/database/") || k.Contains("migration")).ToList(),
            ["Docker"] = result.Files.Keys.Where(k => k.Contains("Dockerfile") || k.Contains("docker-compose")).ToList(),
            ["Documentation"] = result.Files.Keys.Where(k => k.Contains("README") || k.Contains("/docs/")).ToList()
        };
        
        foreach (var category in categories.Where(c => c.Value.Count > 0))
        {
            summary.AppendLine($"**{category.Key}:** {category.Value.Count} files");
            foreach (var file in category.Value.Take(3))
            {
                summary.AppendLine($"  - {file}");
            }
            if (category.Value.Count > 3)
            {
                summary.AppendLine($"  - ... and {category.Value.Count - 3} more");
            }
        }
        
        summary.AppendLine();
        summary.AppendLine("**IL Compliance Features:**");
        
        if (state.ImpactLevel >= ImpactLevel.IL4)
        {
            summary.AppendLine("  ✅ STIG security scanning workflow (TruffleHog, Checkov, tfsec, Trivy)");
        }
        
        if (state.ImpactLevel >= ImpactLevel.IL5)
        {
            summary.AppendLine("  ✅ DoD compliance validation workflow");
            summary.AppendLine("  ✅ ATO checklist documentation");
            summary.AppendLine("  ✅ eMASS registration guidance");
        }
        
        summary.AppendLine("  ✅ COMPLIANCE.md with NIST 800-53 controls");
        summary.AppendLine("  ✅ SECURITY.md with incident response procedures");
        summary.AppendLine("  ✅ Mandatory DoD resource tagging");
        summary.AppendLine();
        summary.AppendLine("**Next Steps:**");
        summary.AppendLine("  1. Templates are stored in SharedMemory");
        summary.AppendLine("  2. Use workspace creation feature to create local repository");
        summary.AppendLine("  3. Review COMPLIANCE.md for deployment requirements");
        
        if (validation.Warnings.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("⚠️ **Warnings:**");
            foreach (var warning in validation.Warnings)
            {
                summary.AppendLine($"  - {warning}");
            }
        }
        
        return summary.ToString();
    }
    
    /// <summary>
    /// List all wizard sessions
    /// </summary>
    [KernelFunction("list_wizard_sessions")]
    [Description("List all active and recent wizard sessions")]
    [return: Description("List of wizard sessions with status")]
    public async Task<string> ListSessionsAsync()
    {
        var sessions = await _stateManager.ListSessionsAsync(limit: 10);
        
        if (sessions.Count == 0)
        {
            return "No wizard sessions found. Use 'start_service_wizard' to begin.";
        }
        
        var sb = new StringBuilder();
        sb.AppendLine($"📋 **Wizard Sessions ({sessions.Count})**");
        sb.AppendLine();
        
        foreach (var session in sessions)
        {
            var status = session.IsComplete ? "✅ Complete" : $"🔄 In Progress ({session.CompletionPercentage}%)";
            sb.AppendLine($"**{session.SessionId.Substring(0, 8)}...** - {status}");
            sb.AppendLine($"  Service: {session.ServiceName ?? "Not set"}");
            sb.AppendLine($"  Step: {session.CurrentStep}");
            sb.AppendLine($"  Updated: {session.LastUpdated:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    // ========== HELPER METHODS ==========
    
    private async Task StoreAnswerAsync(ServiceWizardState state, string answer)
    {
        var trimmed = answer.Trim();
        
        switch (state.CurrentStep)
        {
            case WizardStep.Step1_MissionSponsor:
                state.MissionSponsor = trimmed.Equals("skip", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
                break;
                
            case WizardStep.Step2_ImpactLevel:
                state.ImpactLevel = Enum.Parse<ImpactLevel>(trimmed.ToUpperInvariant());
                break;
                
            case WizardStep.Step3_Region:
                state.Region = trimmed.ToLowerInvariant();
                break;
                
            case WizardStep.Step4_DataClassification:
                state.DataClassification = trimmed;
                break;
                
            case WizardStep.Step5_Environment:
                state.Environment = trimmed;
                break;
                
            case WizardStep.Step6_DoDAAC:
                state.DoDAAC = trimmed.Equals("skip", StringComparison.OrdinalIgnoreCase) ? null : trimmed.ToUpperInvariant();
                break;
                
            case WizardStep.Step7_ServiceType:
                ParseServiceTypeResponse(state, trimmed);
                break;
                
            case WizardStep.Step8_TechStack:
                ParseTechStackResponse(state, trimmed);
                break;
        }
        
        await _stateManager.SaveStateAsync(state);
    }
    
    private void ParseServiceTypeResponse(ServiceWizardState state, string response)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var key = parts[0].ToLowerInvariant();
                var value = parts[1].Trim();
                
                if (key.Contains("type"))
                {
                    state.ServiceType = value;
                }
                else if (key.Contains("name"))
                {
                    state.ServiceName = value;
                }
            }
        }
        
        // If format is just two lines without labels
        if (lines.Length == 2 && string.IsNullOrEmpty(state.ServiceType))
        {
            state.ServiceType = lines[0].Trim();
            state.ServiceName = lines[1].Trim();
        }
        
        // If single line, treat as service name
        if (lines.Length == 1 && string.IsNullOrEmpty(state.ServiceName))
        {
            state.ServiceName = lines[0].Trim();
            state.ServiceType = "API"; // Default
        }
    }
    
    private void ParseTechStackResponse(ServiceWizardState state, string response)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var key = parts[0].ToLowerInvariant();
                var value = parts[1].Trim();
                
                if (key.Contains("language"))
                {
                    state.ProgrammingLanguage = value;
                }
                else if (key.Contains("database") || key.Contains("db"))
                {
                    state.DatabaseType = value.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : value;
                }
                else if (key.Contains("infrastructure") || key.Contains("infra"))
                {
                    state.InfrastructureFormat = value;
                }
                else if (key.Contains("platform") || key.Contains("compute"))
                {
                    state.ComputePlatform = value;
                }
                else if (key.Contains("framework"))
                {
                    state.Framework = value;
                }
            }
        }
    }
    
    private TemplateGenerationRequest ConvertWizardStateToRequest(ServiceWizardState state)
    {
        var request = new TemplateGenerationRequest
        {
            ServiceName = state.ServiceName ?? "unnamed-service",
            Description = state.ServiceDescription ?? $"{state.ServiceType} service for {state.MissionSponsor}"
        };
        
        // DoD Compliance
        if (state.ImpactLevel.HasValue)
        {
            request.DoDCompliance = new DoDComplianceSpec
            {
                ImpactLevel = state.ImpactLevel.Value,
                MissionSponsor = state.MissionSponsor,
                DoDAAC = state.DoDAAC,
                OrganizationUnit = state.OrganizationUnit,
                DataClassification = state.DataClassification ?? "Unclassified"
            };
        }
        
        // Application
        if (!string.IsNullOrEmpty(state.ProgrammingLanguage))
        {
            request.Application = new ApplicationSpec
            {
                Language = ParseProgrammingLanguage(state.ProgrammingLanguage),
                Framework = state.Framework ?? "",
                Type = ParseApplicationType(state.ServiceType ?? "API")
            };
        }
        
        // Database
        if (!string.IsNullOrEmpty(state.DatabaseType))
        {
            request.Databases = new List<DatabaseSpec>
            {
                new DatabaseSpec
                {
                    Name = $"{state.ServiceName}-db",
                    Type = ParseDatabaseType(state.DatabaseType),
                    Location = state.ImpactLevel >= ImpactLevel.IL5 
                        ? DatabaseLocation.Cloud 
                        : DatabaseLocation.Cloud
                }
            };
        }
        
        // Infrastructure
        if (!string.IsNullOrEmpty(state.InfrastructureFormat) || !string.IsNullOrEmpty(state.ComputePlatform))
        {
            request.Infrastructure = new InfrastructureSpec
            {
                Format = ParseInfrastructureFormat(state.InfrastructureFormat ?? "Terraform"),
                Provider = state.Region?.StartsWith("usgov") == true ? CloudProvider.Azure : CloudProvider.Azure,
                Region = state.Region ?? "eastus",
                ComputePlatform = ParseComputePlatform(state.ComputePlatform ?? "AKS")
            };
        }
        
        return request;
    }
    
    private ProgrammingLanguage ParseProgrammingLanguage(string lang)
    {
        return lang.ToLowerInvariant() switch
        {
            ".net" or "c#" or "csharp" or "dotnet" => ProgrammingLanguage.DotNet,
            "java" => ProgrammingLanguage.Java,
            "python" or "py" => ProgrammingLanguage.Python,
            "node" or "nodejs" or "node.js" or "javascript" or "typescript" or "js" or "ts" => ProgrammingLanguage.NodeJS,
            "go" or "golang" => ProgrammingLanguage.Go,
            "rust" => ProgrammingLanguage.Rust,
            _ => ProgrammingLanguage.DotNet
        };
    }
    
    private ApplicationType ParseApplicationType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "api" or "rest api" or "graphql" => ApplicationType.WebAPI,
            "web" or "web app" or "webapp" => ApplicationType.WebApp,
            "worker" or "worker service" or "background" => ApplicationType.BackgroundWorker,
            "microservice" => ApplicationType.Microservice,
            _ => ApplicationType.WebAPI
        };
    }
    
    private DatabaseType ParseDatabaseType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "postgresql" or "postgres" => DatabaseType.PostgreSQL,
            "sql server" or "sqlserver" or "mssql" => DatabaseType.SQLServer,
            "mysql" => DatabaseType.MySQL,
            "cosmos" or "cosmosdb" or "cosmos db" => DatabaseType.CosmosDB,
            "mongodb" or "mongo" => DatabaseType.MongoDB,
            "redis" => DatabaseType.Redis,
            _ => DatabaseType.PostgreSQL
        };
    }
    
    private InfrastructureFormat ParseInfrastructureFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "terraform" or "tf" => InfrastructureFormat.Terraform,
            "bicep" => InfrastructureFormat.Bicep,
            "kubernetes" or "k8s" => InfrastructureFormat.Kubernetes,
            "arm" => InfrastructureFormat.ARM,
            _ => InfrastructureFormat.Terraform
        };
    }
    
    private ComputePlatform ParseComputePlatform(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "aks" or "azure kubernetes" => ComputePlatform.AKS,
            "app service" or "appservice" or "webapp" => ComputePlatform.AppService,
            "container apps" or "containerapps" => ComputePlatform.ContainerApps,
            "functions" or "azure functions" => ComputePlatform.Functions,
            _ => ComputePlatform.AKS
        };
    }
}
