using System.ComponentModel;
using Platform.Engineering.Copilot.Agents.Compliance.Tools;

namespace Platform.Engineering.Copilot.Mcp.Tools;

/// <summary>
/// MCP tools for compliance operations. Wraps Agent Framework compliance tools
/// for exposure via the MCP protocol (GitHub Copilot, Claude Desktop, etc.)
/// </summary>
public class ComplianceMcpTools
{
    private readonly ComplianceAssessmentTool _assessmentTool;
    private readonly ControlFamilyTool _controlFamilyTool;
    private readonly DocumentGenerationTool _documentGenerationTool;
    private readonly EvidenceCollectionTool _evidenceCollectionTool;
    private readonly RemediationExecuteTool _remediationTool;
    private readonly ValidateRemediationTool _validateRemediationTool;
    private readonly RemediationPlanTool _remediationPlanTool;
    private readonly AssessmentAuditLogTool _auditLogTool;
    private readonly ComplianceHistoryTool _historyTool;
    private readonly ComplianceStatusTool _statusTool;

    public ComplianceMcpTools(
        ComplianceAssessmentTool assessmentTool,
        ControlFamilyTool controlFamilyTool,
        DocumentGenerationTool documentGenerationTool,
        EvidenceCollectionTool evidenceCollectionTool,
        RemediationExecuteTool remediationTool,
        ValidateRemediationTool validateRemediationTool,
        RemediationPlanTool remediationPlanTool,
        AssessmentAuditLogTool auditLogTool,
        ComplianceHistoryTool historyTool,
        ComplianceStatusTool statusTool)
    {
        _assessmentTool = assessmentTool;
        _controlFamilyTool = controlFamilyTool;
        _documentGenerationTool = documentGenerationTool;
        _evidenceCollectionTool = evidenceCollectionTool;
        _remediationTool = remediationTool;
        _validateRemediationTool = validateRemediationTool;
        _remediationPlanTool = remediationPlanTool;
        _auditLogTool = auditLogTool;
        _historyTool = historyTool;
        _statusTool = statusTool;
    }

    /// <summary>
    /// Run a compliance assessment against an Azure subscription
    /// </summary>
    [Description("Run a compliance assessment against an Azure subscription using NIST 800-53, FedRAMP, or DoD IL frameworks. Returns findings organized by control family and severity.")]
    public async Task<string> RunComplianceAssessmentAsync(
        string? subscriptionId = null,
        string? framework = null,
        string? controlFamilies = null,
        string? resourceTypes = null,
        bool includePassed = false,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["framework"] = framework,
            ["control_families"] = controlFamilies,
            ["resource_types"] = resourceTypes,
            ["include_passed"] = includePassed
        };
        return await _assessmentTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Get information about NIST 800-53 control families
    /// </summary>
    [Description("Get detailed information about NIST 800-53 control families including controls, descriptions, and Azure implementation guidance.")]
    public async Task<string> GetControlFamilyInfoAsync(
        string familyId,
        bool includeControls = true,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["family_id"] = familyId,
            ["include_controls"] = includeControls
        };
        return await _controlFamilyTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Generate compliance documentation (SSP, SAR, POA&M)
    /// </summary>
    [Description("Generate compliance documentation such as System Security Plans (SSP), Security Assessment Reports (SAR), and Plan of Action & Milestones (POA&M).")]
    public async Task<string> GenerateComplianceDocumentAsync(
        string documentType,
        string? subscriptionId = null,
        string? framework = null,
        string? systemName = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["document_type"] = documentType,
            ["subscription_id"] = subscriptionId,
            ["framework"] = framework,
            ["system_name"] = systemName
        };
        return await _documentGenerationTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Collect evidence for compliance controls
    /// </summary>
    [Description("Collect and organize evidence artifacts for compliance controls from Azure resources. Supports automated evidence gathering for audits.")]
    public async Task<string> CollectComplianceEvidenceAsync(
        string controlId,
        string? subscriptionId = null,
        string? resourceGroup = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["control_id"] = controlId,
            ["subscription_id"] = subscriptionId,
            ["resource_group"] = resourceGroup
        };
        return await _evidenceCollectionTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Remediate compliance findings
    /// </summary>
    [Description("Generate and optionally apply remediation scripts for compliance findings. Supports configuration-level fixes for Azure resources.")]
    public async Task<string> RemediateComplianceFindingAsync(
        string findingId,
        bool applyRemediation = false,
        bool dryRun = true,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["finding_id"] = findingId,
            ["apply_remediation"] = applyRemediation,
            ["dry_run"] = dryRun
        };
        return await _remediationTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Validate that a remediation was successfully applied
    /// </summary>
    [Description("Validate that a remediation was successfully applied by re-checking the finding status. Use after executing remediation to confirm the fix.")]
    public async Task<string> ValidateRemediationAsync(
        string findingId,
        string? executionId = null,
        string? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["finding_id"] = findingId,
            ["execution_id"] = executionId,
            ["subscription_id"] = subscriptionId
        };
        return await _validateRemediationTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Generate a prioritized remediation plan for findings
    /// </summary>
    [Description("Generate a prioritized remediation plan for compliance findings. Organizes remediations by priority, effort, and impact for efficient remediation workflows.")]
    public async Task<string> GenerateRemediationPlanAsync(
        string? subscriptionId = null,
        string? resourceGroupName = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["resource_group_name"] = resourceGroupName
        };
        return await _remediationPlanTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Get the audit log of compliance assessments
    /// </summary>
    [Description("Get the audit trail of compliance assessments including who ran them, when, and what findings were discovered. Essential for compliance reporting.")]
    public async Task<string> GetAssessmentAuditLogAsync(
        string? subscriptionId = null,
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["days"] = days
        };
        return await _auditLogTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Get compliance history and trends
    /// </summary>
    [Description("Get compliance history and trends over time. Shows how compliance posture has changed and tracks remediation progress.")]
    public async Task<string> GetComplianceHistoryAsync(
        string? subscriptionId = null,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["days"] = days
        };
        return await _historyTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Get current compliance status summary
    /// </summary>
    [Description("Get a summary of current compliance status including scores by control family, critical findings count, and overall compliance percentage.")]
    public async Task<string> GetComplianceStatusAsync(
        string? subscriptionId = null,
        string? framework = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["framework"] = framework
        };
        return await _statusTool.ExecuteAsync(args, cancellationToken);
    }
}
