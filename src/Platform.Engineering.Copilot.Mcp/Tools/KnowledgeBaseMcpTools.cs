using System.ComponentModel;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

namespace Platform.Engineering.Copilot.Mcp.Tools;

/// <summary>
/// MCP tools for knowledge base operations. Wraps Agent Framework knowledge base tools
/// for exposure via the MCP protocol (GitHub Copilot, Claude Desktop, etc.)
/// </summary>
public class KnowledgeBaseMcpTools
{
    private readonly NistControlExplainerTool _nistControlExplainerTool;
    private readonly NistControlSearchTool _nistControlSearchTool;
    private readonly RmfExplainerTool _rmfExplainerTool;
    private readonly StigExplainerTool _stigExplainerTool;
    private readonly StigSearchTool _stigSearchTool;
    private readonly FedRampTemplateTool _fedRampTemplateTool;
    private readonly ImpactLevelTool _impactLevelTool;

    public KnowledgeBaseMcpTools(
        NistControlExplainerTool nistControlExplainerTool,
        NistControlSearchTool nistControlSearchTool,
        RmfExplainerTool rmfExplainerTool,
        StigExplainerTool stigExplainerTool,
        StigSearchTool stigSearchTool,
        FedRampTemplateTool fedRampTemplateTool,
        ImpactLevelTool impactLevelTool)
    {
        _nistControlExplainerTool = nistControlExplainerTool;
        _nistControlSearchTool = nistControlSearchTool;
        _rmfExplainerTool = rmfExplainerTool;
        _stigExplainerTool = stigExplainerTool;
        _stigSearchTool = stigSearchTool;
        _fedRampTemplateTool = fedRampTemplateTool;
        _impactLevelTool = impactLevelTool;
    }

    /// <summary>
    /// Explain a NIST 800-53 control
    /// </summary>
    [Description("Get detailed explanation of a NIST 800-53 control including purpose, requirements, implementation guidance, and related controls.")]
    public async Task<string> ExplainNistControlAsync(
        string controlId,
        string? impactLevel = null,
        bool includeEnhancements = false,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["control_id"] = controlId,
            ["impact_level"] = impactLevel,
            ["include_enhancements"] = includeEnhancements
        };
        return await _nistControlExplainerTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Search NIST 800-53 controls
    /// </summary>
    [Description("Search for NIST 800-53 controls by keyword, control family, or requirement. Returns matching controls with relevance scores.")]
    public async Task<string> SearchNistControlsAsync(
        string query,
        string? controlFamily = null,
        string? impactLevel = null,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = query,
            ["control_family"] = controlFamily,
            ["impact_level"] = impactLevel,
            ["max_results"] = maxResults
        };
        return await _nistControlSearchTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Explain the Risk Management Framework (RMF)
    /// </summary>
    [Description("Get detailed explanation of RMF steps, phases, and activities. Includes guidance for implementing RMF in your organization.")]
    public async Task<string> ExplainRmfAsync(
        string? rmfStep = null,
        string? topic = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["rmf_step"] = rmfStep,
            ["topic"] = topic
        };
        return await _rmfExplainerTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Explain STIG requirements
    /// </summary>
    [Description("Get detailed explanation of STIG (Security Technical Implementation Guide) requirements, including check procedures and fix guidance.")]
    public async Task<string> ExplainStigAsync(
        string stigId,
        string? ruleId = null,
        bool includeFixes = true,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["stig_id"] = stigId,
            ["rule_id"] = ruleId,
            ["include_fixes"] = includeFixes
        };
        return await _stigExplainerTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Search STIGs
    /// </summary>
    [Description("Search for STIGs by technology, severity, or keyword. Returns matching STIGs and rules.")]
    public async Task<string> SearchStigsAsync(
        string query,
        string? technology = null,
        string? severity = null,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = query,
            ["technology"] = technology,
            ["severity"] = severity,
            ["max_results"] = maxResults
        };
        return await _stigSearchTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Get FedRAMP templates and guidance
    /// </summary>
    [Description("Get FedRAMP templates, checklists, and guidance documents. Includes SSP templates, POA&M templates, and assessment procedures.")]
    public async Task<string> GetFedRampTemplateAsync(
        string templateType,
        string? impactLevel = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["template_type"] = templateType,
            ["impact_level"] = impactLevel
        };
        return await _fedRampTemplateTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Determine or compare impact levels
    /// </summary>
    [Description("Determine appropriate FIPS 199 impact level (Low, Moderate, High) based on system characteristics. Compare requirements across impact levels.")]
    public async Task<string> DetermineImpactLevelAsync(
        string operation,
        string? systemType = null,
        string? dataTypes = null,
        string? compareLevel1 = null,
        string? compareLevel2 = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["system_type"] = systemType,
            ["data_types"] = dataTypes,
            ["compare_level_1"] = compareLevel1,
            ["compare_level_2"] = compareLevel2
        };
        return await _impactLevelTool.ExecuteAsync(args, cancellationToken);
    }
}
