using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;
using Platform.Engineering.Copilot.Core.Plugins;
using Platform.Engineering.Copilot.Core.Services.Agents;

namespace Platform.Engineering.Copilot.Compliance.Agent.Plugins;

/// <summary>
/// Kernel plugin for RMF/STIG/DoD knowledge base queries and explanations.
/// Provides compliant advisory-only information about compliance frameworks.
/// </summary>
public class KnowledgeBasePlugin : BaseSupervisorPlugin
{
    private readonly IRmfKnowledgeService _rmfService;
    private readonly IStigKnowledgeService _stigService;
    private readonly IDoDInstructionService _dodInstructionService;
    private readonly IDoDWorkflowService _workflowService;
    

    public KnowledgeBasePlugin(
        IRmfKnowledgeService rmfService,
        IStigKnowledgeService stigService,
        IDoDInstructionService dodInstructionService,
        IDoDWorkflowService workflowService,
        ILogger<KnowledgeBasePlugin> logger,
        Kernel kernel) : base(logger, kernel)
    {
        _rmfService = rmfService ?? throw new ArgumentNullException(nameof(rmfService));
        _stigService = stigService ?? throw new ArgumentNullException(nameof(stigService));
        _dodInstructionService = dodInstructionService ?? throw new ArgumentNullException(nameof(dodInstructionService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
    }

    #region RMF Functions

    [KernelFunction("explain_rmf_process")]
    [Description("Explain the Risk Management Framework (RMF) process for DoD systems. " +
                 "Provides overview of all 6 RMF steps or detailed explanation of a specific step. " +
                 "Use this when users ask about RMF, ATO process, or authorization workflows. " +
                 "Examples: 'Explain RMF', 'What is RMF Step 4?', 'How do I get an ATO?'")]
    public async Task<string> ExplainRmfProcessAsync(
        [Description("Optional: Specific RMF step number (1-6). Leave empty for complete overview.")] string? step = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Explaining RMF process{Step}", step != null ? $" step {step}" : "");
        
        try
        {
            return await _rmfService.ExplainRmfProcessAsync(step, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining RMF process");
            return "Error retrieving RMF process information. Please check logs for details.";
        }
    }

    [KernelFunction("get_rmf_deliverables")]
    [Description("Get required deliverables for a specific RMF step. " +
                 "Returns list of documents and artifacts needed for each RMF phase. " +
                 "Use when users ask 'What documents do I need for RMF Step X?'")]
    public async Task<string> GetRmfDeliverablesAsync(
        [Description("RMF step number (1-6)")] string step,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting RMF deliverables for step {Step}", step);
        
        try
        {
            var deliverables = await _rmfService.GetRmfOutputsForStepAsync(step, cancellationToken);
            
            if (!deliverables.Any())
                return $"No deliverables found for RMF Step {step}. Valid steps are 1-6.";

            return $@"**RMF Step {step} Required Deliverables:**

{string.Join("\n", deliverables.Select((d, i) => $"{i + 1}. {d}"))}

Use the 'explain_rmf_process' function with step={step} for complete details about this phase.
";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting RMF deliverables");
            return "Error retrieving RMF deliverables. Please check logs for details.";
        }
    }

    #endregion

    #region STIG Functions

    [KernelFunction("explain_stig")]
    [Description("Explain a specific STIG (Security Technical Implementation Guide) control. " +
                 "Provides detailed information including severity, check procedures, remediation steps, and Azure implementation. " +
                 "Use when users ask about specific STIG IDs like 'Explain STIG V-219153' or 'How do I fix V-219187?'")]
    public async Task<string> ExplainStigAsync(
        [Description("STIG ID (e.g., 'V-219153') or Vulnerability ID")] string stigId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Explaining STIG {StigId}", stigId);
        
        try
        {
            return await _stigService.ExplainStigAsync(stigId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining STIG");
            return $"Error retrieving STIG {stigId} information. Please check logs for details.";
        }
    }

    [KernelFunction("search_stigs")]
    [Description("Search for STIG controls by keyword or topic. " +
                 "Use when users ask general questions like 'What STIGs apply to encryption?' or 'STIGs for MFA'")]
    public async Task<string> SearchStigsAsync(
        [Description("Search term or keyword (e.g., 'encryption', 'MFA', 'public IP')")] string searchTerm,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching STIGs for: {SearchTerm}", searchTerm);
        
        try
        {
            var results = await _stigService.SearchStigsAsync(searchTerm, cancellationToken);
            
            if (!results.Any())
                return $"No STIG controls found matching '{searchTerm}'. Try broader search terms.";

            return $@"**STIG Search Results for ""{searchTerm}""**

Found {results.Count} matching controls:

{string.Join("\n\n", results.Take(5).Select(s => $@"**{s.StigId}**: {s.Title}
- Severity: {s.Severity}
- Category: {s.Category}
- NIST Controls: {string.Join(", ", s.NistControls)}
- Description: {(s.Description.Length > 150 ? s.Description.Substring(0, 150) + "..." : s.Description)}"))}

{(results.Count > 5 ? $"\n...and {results.Count - 5} more results.\n" : "")}

Use 'explain_stig' function with a specific STIG ID for complete details.
";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching STIGs");
            return "Error searching STIG controls. Please check logs for details.";
        }
    }

    [KernelFunction("get_stigs_for_nist_control")]
    [Description("Get all STIG controls that map to a specific NIST 800-53 control. " +
                 "Use when users ask 'What STIGs implement AC-2?' or 'Show STIGs for control IA-2(1)'")]
    public async Task<string> GetStigsForNistControlAsync(
        [Description("NIST 800-53 control ID (e.g., 'AC-2', 'IA-2(1)', 'SC-28')")] string nistControlId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting STIGs for NIST control {ControlId}", nistControlId);
        
        try
        {
            var stigs = await _stigService.GetStigsByNistControlAsync(nistControlId, cancellationToken);
            
            if (!stigs.Any())
                return $"No STIG controls found for NIST control {nistControlId}.";

            return $@"**STIG Controls for NIST {nistControlId}**

Found {stigs.Count} STIG control{(stigs.Count > 1 ? "s" : "")}:

{string.Join("\n", stigs.Select(s => $"- **{s.StigId}**: {s.Title} (Severity: {s.Severity})"))}

Use 'explain_stig' for detailed information on any of these controls.
";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting STIGs for NIST control");
            return "Error retrieving STIG mappings. Please check logs for details.";
        }
    }

    [KernelFunction("get_control_mapping")]
    [Description("Get complete mapping between NIST 800-53 controls, STIGs, CCIs, and DoD instructions. " +
                 "Use when users ask about compliance mappings or relationships between frameworks.")]
    public async Task<string> GetControlMappingAsync(
        [Description("NIST 800-53 control ID (e.g., 'AC-2', 'IA-2(1)')")] string nistControlId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting control mapping for {ControlId}", nistControlId);
        
        try
        {
            var mapping = await _stigService.GetControlMappingAsync(nistControlId, cancellationToken);
            
            if (mapping == null)
                return $"No control mapping found for NIST control {nistControlId}.";

            return $@"**Control Mapping for NIST {nistControlId}**

{mapping.Description}

**STIG IDs:** {string.Join(", ", mapping.StigIds)}
**CCI References:** {string.Join(", ", mapping.CciIds)}
**DoD Instructions:** {string.Join(", ", mapping.DoDInstructions)}

**Implementation Guidance:**
{string.Join("\n", mapping.ImplementationGuidance.Select(kvp => $"- **{kvp.Key}**: {kvp.Value}"))}
";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting control mapping");
            return "Error retrieving control mapping. Please check logs for details.";
        }
    }

    #endregion

    #region DoD Instruction Functions

    [KernelFunction("explain_dod_instruction")]
    [Description("Explain a DoD Instruction and its requirements. " +
                 "Use when users ask about specific DoD policies like 'Explain DoDI 8500.01' or 'What is DoDI 8510.01?'")]
    public async Task<string> ExplainDoDInstructionAsync(
        [Description("DoD Instruction ID (e.g., 'DoDI 8500.01', 'CNSSI 1253')")] string instructionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Explaining DoD Instruction {InstructionId}", instructionId);
        
        try
        {
            return await _dodInstructionService.ExplainInstructionAsync(instructionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining DoD instruction");
            return $"Error retrieving DoD Instruction {instructionId}. Please check logs for details.";
        }
    }

    [KernelFunction("search_dod_instructions")]
    [Description("Search for DoD Instructions by keyword or topic. " +
                 "Use when users ask about DoD policies on specific topics like 'DoD cybersecurity policy' or 'PKI instructions'")]
    public async Task<string> SearchDoDInstructionsAsync(
        [Description("Search term or keyword")] string searchTerm,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching DoD instructions for: {SearchTerm}", searchTerm);
        
        try
        {
            var results = await _dodInstructionService.SearchInstructionsAsync(searchTerm, cancellationToken);
            
            if (!results.Any())
                return $"No DoD Instructions found matching '{searchTerm}'.";

            return $@"**DoD Instruction Search Results**

Found {results.Count} instruction{(results.Count > 1 ? "s" : "")}:

{string.Join("\n\n", results.Select(i => $@"**{i.InstructionId}**: {i.Title}
{i.Description}
Applicability: {i.Applicability}"))}

Use 'explain_dod_instruction' for complete details.
";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching DoD instructions");
            return "Error searching DoD instructions. Please check logs for details.";
        }
    }

    #endregion

    #region Workflow Functions

    [KernelFunction("explain_navy_workflow")]
    [Description("Explain a Navy/DoD workflow process (e.g., ATO process, eMASS registration, PMW deployment). " +
                 "Use when users ask 'How do I get an ATO in Navy?' or 'What is the PMW cloud deployment process?'")]
    public async Task<string> ExplainNavyWorkflowAsync(
        [Description("Workflow ID (e.g., 'WF-NAV-ATO-001', 'WF-PMW-CLOUD-001', 'WF-NAV-EMASS-001')")] string workflowId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Explaining Navy workflow {WorkflowId}", workflowId);
        
        try
        {
            return await _workflowService.ExplainWorkflowAsync(workflowId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining Navy workflow");
            return $"Error retrieving workflow {workflowId}. Please check logs for details.";
        }
    }

    [KernelFunction("get_navy_ato_process")]
    [Description("Get the complete Navy RMF/ATO authorization process with all steps and timelines. " +
                 "Use when users ask 'How do I get an ATO?' or 'What is the Navy authorization process?'")]
    public async Task<string> GetNavyAtoProcessAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting Navy ATO process");
        return await ExplainNavyWorkflowAsync("WF-NAV-ATO-001", cancellationToken);
    }

    [KernelFunction("get_pmw_deployment_process")]
    [Description("Get the PMW (Program Manager Warfare Systems) cloud deployment workflow. " +
                 "Use for PMW-specific cloud system deployment guidance.")]
    public async Task<string> GetPmwDeploymentProcessAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting PMW deployment process");
        return await ExplainNavyWorkflowAsync("WF-PMW-CLOUD-001", cancellationToken);
    }

    [KernelFunction("get_emass_registration_process")]
    [Description("Get the eMASS (Enterprise Mission Assurance Support Service) system registration process. " +
                 "Use when users ask about registering systems in eMASS or maintaining eMASS records.")]
    public async Task<string> GetEmassRegistrationProcessAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting eMASS registration process");
        return await ExplainNavyWorkflowAsync("WF-NAV-EMASS-001", cancellationToken);
    }

    #endregion

    #region IL-Specific Guidance

    [KernelFunction("explain_impact_level")]
    [Description("Explain Impact Level (IL) requirements for DoD cloud systems. " +
                 "Covers IL2, IL4, IL5, and IL6 with specific Azure configurations, controls, and restrictions. " +
                 "Use when users ask 'What is IL5?' or 'IL6 boundary protection requirements?'")]
    public async Task<string> ExplainImpactLevelAsync(
        [Description("Impact Level (IL2, IL4, IL5, or IL6)")] string impactLevel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Explaining Impact Level {Level}", impactLevel);
        
        // This will be expanded with actual IL data from dod-instructions.json
        return impactLevel.ToUpper() switch
        {
            "IL5" => @"# Impact Level 5 (IL5)

**Classification:** DoD CUI High / Secret

## Description
Systems processing high-sensitivity CUI or classified information up to Secret.

## Requirements
- NIST 800-53 High baseline
- FedRAMP High compliance
- Complete network isolation
- Dedicated infrastructure
- FIPS 140-2 cryptography

## Azure Configurations

**Encryption:** Customer-managed keys REQUIRED (FIPS 140-2)
**Networking:** No public endpoints, dedicated ExpressRoute
**Identity:** CAC/PIV authentication mandatory
**Logging:** 365 days minimum retention, immutable logs
**Region:** USGov Virginia, USGov Arizona only
**Isolation:** Dedicated tenants, no multi-tenancy

## Mandatory NIST Controls
All IL4 controls PLUS:
- SC-7(3): Access Points
- SC-7(5): Deny by Default / Allow by Exception
- SC-28(1): Cryptographic Protection
- AU-9: Protection of Audit Information
- IA-2(1): Network Access to Privileged Accounts - MFA
- IA-2(12): PIV Credential

## Boundary Protection
- No public IP addresses on VMs
- Private endpoints for ALL PaaS services
- Dedicated ExpressRoute with private peering
- Network Security Groups with default deny
- Azure Firewall for egress filtering
- No direct internet connectivity

For complete IL5 guidance, consult CNSSI 1253 and your organization's security team.",

            "IL6" => @"# Impact Level 6 (IL6)

**Classification:** Classified Secret / Top Secret

## Description
Systems processing classified information at Secret or Top Secret levels.

## Requirements
- NIST 800-53 High + classified overlays
- CNSSI 1253 controls
- Complete air-gapped infrastructure
- NSA-approved cryptography (Suite B)
- Continuous monitoring and logging

## Azure Configurations

**Encryption:** NSA Suite B cryptography REQUIRED
**Networking:** Dedicated isolated network, no internet connectivity
**Identity:** CAC/PIV with hardware token mandatory
**Logging:** Indefinite retention, air-gapped SIEM
**Region:** Secret/Top Secret regions only (Azure Government Secret)
**Isolation:** Physically separated infrastructure
**Personnel:** Cleared personnel only (Secret/TS/SCI)

## Mandatory Controls
All IL5 controls PLUS:
- SC-8(1): Cryptographic Protection for transmission
- MA-4(6): Remote Maintenance - Cryptographic Protection
- PE-3: Physical Access Control
- PE-6: Monitoring Physical Access
- PS-3: Personnel Screening

## Network Isolation
- Physically isolated infrastructure
- No connection to internet or commercial networks
- Dedicated cross-domain solutions for controlled transfers
- All communications through approved gateways
- Continuous monitoring of all network flows

For IL6 systems, engagement with NSA and Defense Security Service is required.",

            _ => $"Impact Level {impactLevel} not found. Valid levels are IL2, IL4, IL5, and IL6."
        };
    }

    #endregion
}
