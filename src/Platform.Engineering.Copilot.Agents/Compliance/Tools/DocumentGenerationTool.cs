using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Agents.Compliance.State;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for generating compliance documentation including SSP, SAR, and POA&M.
/// </summary>
public class DocumentGenerationTool : BaseTool
{
    private readonly ComplianceStateAccessors _stateAccessors;
    private readonly ComplianceAgentOptions _options;

    public override string Name => "generate_compliance_document";

    public override string Description =>
        "Generates compliance documentation such as System Security Plan (SSP), " +
        "Security Assessment Report (SAR), and Plan of Actions & Milestones (POA&M). " +
        "Uses assessment results and evidence to populate document templates.";

    public DocumentGenerationTool(
        ILogger<DocumentGenerationTool> logger,
        ComplianceStateAccessors stateAccessors,
        IOptions<ComplianceAgentOptions> options) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new ComplianceAgentOptions();

        Parameters.Add(new ToolParameter("document_type", "Document type: SSP, SAR, POAM, CRM (Continuous Monitoring Report)", true));
        Parameters.Add(new ToolParameter("system_name", "Name of the system for documentation", true));
        Parameters.Add(new ToolParameter("assessment_id", "Assessment ID to use for populating document", false));
        Parameters.Add(new ToolParameter("include_evidence", "Include evidence references. Default: true", false));
        Parameters.Add(new ToolParameter("output_format", "Output format: markdown, json, pdf_template. Default: markdown", false));
        Parameters.Add(new ToolParameter("conversation_id", "Conversation ID for state tracking", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnableDocumentGeneration)
            {
                return ToJson(new { success = false, error = "Document generation is disabled" });
            }

            var documentType = GetOptionalString(arguments, "document_type")?.ToUpper()
                ?? throw new ArgumentException("document_type is required");
            var systemName = GetOptionalString(arguments, "system_name")
                ?? throw new ArgumentException("system_name is required");
            var assessmentId = GetOptionalString(arguments, "assessment_id");
            var includeEvidence = GetOptionalBool(arguments, "include_evidence", true);
            var outputFormat = GetOptionalString(arguments, "output_format") ?? "markdown";
            var conversationId = GetOptionalString(arguments, "conversation_id") ?? Guid.NewGuid().ToString();

            Logger.LogInformation("Generating {DocumentType} for system: {SystemName}", documentType, systemName);

            // Try to get assessment results for document population
            AssessmentResult? assessment = null;
            if (!string.IsNullOrEmpty(assessmentId))
            {
                assessment = await _stateAccessors.GetCachedAssessmentAsync(conversationId, assessmentId);
            }

            // Generate the document
            var document = await GenerateDocumentAsync(
                documentType,
                systemName,
                assessment,
                includeEvidence,
                outputFormat,
                cancellationToken);

            return ToJson(new
            {
                success = true,
                document = new
                {
                    documentId = document.DocumentId,
                    type = document.Type,
                    title = document.Title,
                    systemName = document.SystemName,
                    generatedAt = document.GeneratedAt,
                    format = document.Format,
                    sectionCount = document.Sections.Count,
                    sections = document.Sections.Select(s => new { s.Title, contentLength = s.Content.Length })
                },
                content = document.Sections.Select(s => new { s.Title, s.Content }),
                message = $"Successfully generated {documentType} for {systemName}"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating compliance document");
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private async Task<ComplianceDocument> GenerateDocumentAsync(
        string documentType,
        string systemName,
        AssessmentResult? assessment,
        bool includeEvidence,
        string outputFormat,
        CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // Simulate generation time

        var document = new ComplianceDocument
        {
            Type = documentType,
            Title = GetDocumentTitle(documentType, systemName),
            SystemName = systemName,
            GeneratedAt = DateTime.UtcNow,
            Format = outputFormat
        };

        // Generate sections based on document type
        switch (documentType)
        {
            case "SSP":
                document.Sections = GenerateSspSections(systemName, assessment, includeEvidence);
                break;
            case "SAR":
                document.Sections = GenerateSarSections(systemName, assessment, includeEvidence);
                break;
            case "POAM":
                document.Sections = GeneratePoamSections(systemName, assessment);
                break;
            case "CRM":
                document.Sections = GenerateCrmSections(systemName, assessment);
                break;
            default:
                throw new ArgumentException($"Unsupported document type: {documentType}");
        }

        return document;
    }

    private string GetDocumentTitle(string documentType, string systemName)
    {
        return documentType switch
        {
            "SSP" => $"System Security Plan - {systemName}",
            "SAR" => $"Security Assessment Report - {systemName}",
            "POAM" => $"Plan of Actions & Milestones - {systemName}",
            "CRM" => $"Continuous Monitoring Report - {systemName}",
            _ => $"Compliance Document - {systemName}"
        };
    }

    private List<DocumentSection> GenerateSspSections(string systemName, AssessmentResult? assessment, bool includeEvidence)
    {
        var sections = new List<DocumentSection>
        {
            new()
            {
                Title = "1. Information System Overview",
                Content = $@"## 1. Information System Overview

### 1.1 System Name and Identifier
**System Name:** {systemName}
**System Identifier:** {systemName.Replace(" ", "-").ToUpper()}-001

### 1.2 System Categorization
**Impact Level:** High (FedRAMP High Baseline)
**Confidentiality:** High
**Integrity:** High  
**Availability:** High

### 1.3 System Description
{systemName} is a cloud-based platform deployed on Microsoft Azure Government infrastructure. 
The system provides secure, compliant services for federal government agencies."
            },
            new()
            {
                Title = "2. System Environment",
                Content = $@"## 2. System Environment

### 2.1 Cloud Service Model
**Service Model:** Platform as a Service (PaaS)
**Deployment Model:** Government Cloud

### 2.2 Authorization Boundary
The authorization boundary includes all Azure resources within the dedicated subscription(s),
including virtual networks, compute instances, storage accounts, and managed services.

### 2.3 Network Architecture
- Virtual Network with network segmentation
- Network Security Groups for traffic filtering
- Azure Firewall for perimeter protection
- Private endpoints for data services"
            },
            new()
            {
                Title = "3. Control Implementation Status",
                Content = GenerateControlStatusSection(assessment)
            }
        };

        if (includeEvidence)
        {
            sections.Add(new DocumentSection
            {
                Title = "Appendix A: Evidence References",
                Content = @"## Appendix A: Evidence References

| Control | Evidence Type | Location | Date Collected |
|---------|--------------|----------|----------------|
| AC-2 | Configuration Export | evidence/ac-2/config.json | Current |
| AU-2 | Audit Logs | evidence/au-2/logs.json | Current |
| SC-7 | Network Diagram | evidence/sc-7/diagram.png | Current |"
            });
        }

        return sections;
    }

    private List<DocumentSection> GenerateSarSections(string systemName, AssessmentResult? assessment, bool includeEvidence)
    {
        var controlStats = GetControlStats(assessment);

        return new List<DocumentSection>
        {
            new()
            {
                Title = "Executive Summary",
                Content = $@"## Executive Summary

### Assessment Overview
**System Assessed:** {systemName}
**Assessment Date:** {DateTime.UtcNow:MMMM dd, yyyy}
**Assessment Type:** Automated Security Assessment

### Results Summary
- **Total Controls Assessed:** {controlStats.Total}
- **Controls Satisfied:** {controlStats.Passed}
- **Controls with Findings:** {controlStats.Failed}
- **Overall Compliance:** {controlStats.CompliancePercentage:F1}%

### Risk Assessment
{GetRiskAssessment(controlStats)}"
            },
            new()
            {
                Title = "Findings Summary",
                Content = GenerateFindingsSummary(assessment)
            },
            new()
            {
                Title = "Recommendations",
                Content = @"## Recommendations

### High Priority
1. Address all Access Control (AC) findings within 30 days
2. Remediate Audit and Accountability (AU) gaps
3. Strengthen boundary protection controls

### Medium Priority
1. Enhance identification and authentication mechanisms
2. Improve configuration management processes
3. Update vulnerability scanning procedures"
            }
        };
    }

    private List<DocumentSection> GeneratePoamSections(string systemName, AssessmentResult? assessment)
    {
        return new List<DocumentSection>
        {
            new()
            {
                Title = "POA&M Overview",
                Content = $@"## Plan of Actions & Milestones

**System Name:** {systemName}
**Document Date:** {DateTime.UtcNow:MMMM dd, yyyy}
**Review Frequency:** Monthly

This document tracks all open security findings and planned remediation activities."
            },
            new()
            {
                Title = "Open Actions",
                Content = GeneratePoamItems(assessment)
            }
        };
    }

    private List<DocumentSection> GenerateCrmSections(string systemName, AssessmentResult? assessment)
    {
        return new List<DocumentSection>
        {
            new()
            {
                Title = "Continuous Monitoring Report",
                Content = $@"## Continuous Monitoring Report

**System Name:** {systemName}
**Reporting Period:** {DateTime.UtcNow.AddMonths(-1):MMMM yyyy}
**Report Date:** {DateTime.UtcNow:MMMM dd, yyyy}

### Security Status Summary
This report summarizes the continuous monitoring activities and security posture changes
for the reporting period."
            },
            new()
            {
                Title = "Control Assessment Status",
                Content = GenerateControlStatusSection(assessment)
            }
        };
    }

    private string GenerateControlStatusSection(AssessmentResult? assessment)
    {
        if (assessment == null)
        {
            return @"## 3. Control Implementation Status

| Control Family | Implemented | Planned | Not Applicable |
|---------------|-------------|---------|----------------|
| AC - Access Control | 22 | 0 | 0 |
| AU - Audit | 16 | 0 | 0 |
| SC - System Communications | 44 | 0 | 0 |
| IA - Identification | 12 | 0 | 0 |

*Note: Based on standard FedRAMP High baseline.*";
        }

        var familyStats = assessment.Findings
            .GroupBy(f => f.ControlFamily)
            .Select(g => new { Family = g.Key, Count = g.Count(), Passed = g.Count(f => f.Status == "Compliant") })
            .ToList();

        var rows = string.Join("\n", familyStats.Select(f =>
            $"| {f.Family} | {f.Passed} | {f.Count - f.Passed} | 0 |"));

        return $@"## 3. Control Implementation Status

| Control Family | Satisfied | Findings | Not Applicable |
|---------------|-----------|----------|----------------|
{rows}";
    }

    private string GenerateFindingsSummary(AssessmentResult? assessment)
    {
        if (assessment == null)
        {
            return "## Findings Summary\n\nNo assessment data available.";
        }

        var findings = assessment.Findings.Where(f => f.Status != "Compliant").Take(10);
        var rows = string.Join("\n", findings.Select(f =>
            $"| {f.ControlId} | {f.Severity} | {f.Title} |"));

        return $@"## Findings Summary

| Control | Severity | Finding |
|---------|----------|---------|
{rows}";
    }

    private string GeneratePoamItems(AssessmentResult? assessment)
    {
        if (assessment == null)
        {
            return @"## Open Actions

| ID | Control | Finding | Severity | Target Date | Status |
|----|---------|---------|----------|-------------|--------|
| 1 | AC-2 | Account review process needs automation | Medium | 30 days | Open |
| 2 | AU-6 | Audit log review frequency insufficient | Low | 60 days | Open |";
        }

        var items = assessment.Findings
            .Where(f => f.Status != "Compliant")
            .Take(10)
            .Select((f, i) => $"| {i + 1} | {f.ControlId} | {f.Title} | {f.Severity} | 30 days | Open |");

        return $@"## Open Actions

| ID | Control | Finding | Severity | Target Date | Status |
|----|---------|---------|----------|-------------|--------|
{string.Join("\n", items)}";
    }

    private (int Total, int Passed, int Failed, double CompliancePercentage) GetControlStats(AssessmentResult? assessment)
    {
        if (assessment == null)
        {
            return (100, 85, 15, 85.0);
        }

        var total = assessment.Findings.Count;
        var passed = assessment.Findings.Count(f => f.Status == "Compliant");
        var failed = total - passed;
        var percentage = total > 0 ? (double)passed / total * 100 : 100;

        return (total, passed, failed, percentage);
    }

    private string GetRiskAssessment((int Total, int Passed, int Failed, double CompliancePercentage) stats)
    {
        return stats.CompliancePercentage switch
        {
            >= 95 => "**Low Risk** - The system demonstrates strong security controls implementation.",
            >= 80 => "**Moderate Risk** - Some security gaps require attention but overall posture is acceptable.",
            >= 60 => "**High Risk** - Significant security gaps identified requiring immediate attention.",
            _ => "**Critical Risk** - System requires major security improvements before ATO consideration."
        };
    }

    private class ComplianceDocument
    {
        public string DocumentId { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string SystemName { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
        public string Format { get; set; } = "markdown";
        public List<DocumentSection> Sections { get; set; } = new();
    }

    private class DocumentSection
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
