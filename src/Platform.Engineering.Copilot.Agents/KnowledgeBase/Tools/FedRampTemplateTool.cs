using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for providing FedRAMP template guidance and requirements.
/// Helps users understand and prepare FedRAMP authorization documentation.
/// </summary>
public class FedRampTemplateTool : BaseTool
{
    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly IFedRampTemplateService _templateService;

    public override string Name => "get_fedramp_template_guidance";

    public override string Description =>
        "Get guidance on FedRAMP authorization package templates and requirements. " +
        "Provides information about SSP, SAR, POA&M, and other FedRAMP documentation. " +
        "Use for questions like: 'What's in a FedRAMP SSP?', 'FedRAMP High template requirements'.";

    public FedRampTemplateTool(
        ILogger<FedRampTemplateTool> logger,
        KnowledgeBaseStateAccessors stateAccessors,
        IFedRampTemplateService templateService) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));

        Parameters.Add(new ToolParameter(
            name: "template_type",
            description: "Template type: 'SSP', 'SAR', 'POA&M', 'CRM', 'CIS', or 'overview' for all templates",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "baseline",
            description: "FedRAMP baseline: 'Low', 'Moderate', 'High' (default: High)",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var templateType = GetOptionalString(arguments, "template_type")?.ToUpperInvariant();
        var baseline = GetOptionalString(arguments, "baseline") ?? "High";

        Logger.LogInformation("Getting FedRAMP template guidance: {Type} at {Baseline} baseline", 
            templateType ?? "overview", baseline);

        try
        {
            // Track the query
            await _stateAccessors.SetLastQueryAsync("system", templateType ?? "fedramp_templates", "fedramp", cancellationToken);

            // Handle SSP section requests
            if (templateType == "SSP")
            {
                var sspSection = await _templateService.GetSspSectionTemplateAsync("1", cancellationToken);
                var checklist = await _templateService.GetAuthorizationPackageChecklistAsync(cancellationToken);
                
                return $@"# FedRAMP System Security Plan (SSP) Template Guidance

## Overview
The SSP is the core document of the FedRAMP authorization package. It describes the system's security controls, implementation, and authorization boundary.

## Key Sections
{sspSection}

## Authorization Package Checklist
{string.Join("\n", checklist.Select(c => $"- {c}"))}

## Tips
- Use FedRAMP SSP templates as starting point
- Be specific about control implementations
- Include diagrams for system boundaries
- Document all interconnections

---
*Reference: FedRAMP {baseline} Baseline Authorization Package*";
            }

            // Handle POA&M requests
            if (templateType == "POA&M" || templateType == "POAM")
            {
                var poamTemplate = await _templateService.GetPoamTemplateAsync(cancellationToken);
                return $@"# FedRAMP Plan of Action and Milestones (POA&M) Guidance

{poamTemplate}

---
*Reference: FedRAMP {baseline} Baseline*";
            }

            // Handle continuous monitoring requests
            if (templateType == "CRM" || templateType == "CONMON")
            {
                var conmon = await _templateService.GetContinuousMonitoringRequirementsAsync(cancellationToken);
                return $@"# FedRAMP Continuous Monitoring Requirements

{conmon}

---
*Reference: FedRAMP {baseline} Baseline*";
            }

            // Default overview
            var checklistItems = await _templateService.GetAuthorizationPackageChecklistAsync(cancellationToken);
            return $@"# FedRAMP Authorization Package Templates ({baseline} Baseline)

## Required Documents

### System Security Plan (SSP)
Core document describing security controls and implementation.

### Security Assessment Report (SAR)
Documents the results of security testing.

### Plan of Action and Milestones (POA&M)
Tracks remediation of identified vulnerabilities.

### Continuous Monitoring (ConMon)
Ongoing security monitoring requirements.

## Authorization Package Checklist
{string.Join("\n", checklistItems.Select(c => $"- {c}"))}

## Next Steps
Ask about specific templates:
- 'Tell me about SSP requirements'
- 'Show me POA&M template guidance'
- 'Explain continuous monitoring requirements'

---
*Reference: FedRAMP.gov Authorization Templates*";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting FedRAMP template guidance: {Type}", templateType);
            return ToJson(new
            {
                success = false,
                error = $"Error retrieving template guidance: {ex.Message}"
            });
        }
    }
}
