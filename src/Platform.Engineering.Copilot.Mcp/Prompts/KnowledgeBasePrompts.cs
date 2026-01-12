namespace Platform.Engineering.Copilot.Mcp.Prompts;

/// <summary>
/// MCP Prompts for knowledge base domain operations.
/// These prompts guide AI assistants in using the platform's compliance knowledge base.
/// </summary>
public static class KnowledgeBasePrompts
{
    public static readonly McpPrompt ExplainNistControl = new()
    {
        Name = "explain_nist_control",
        Description = "Get detailed explanation of a NIST 800-53 control",
        Arguments = new[]
        {
            new PromptArgument { Name = "control_id", Description = "NIST control ID (e.g., AC-2, AU-3, IA-5)", Required = true },
            new PromptArgument { Name = "include_enhancements", Description = "Include control enhancements (true/false)", Required = false }
        }
    };

    public static readonly McpPrompt SearchNistControls = new()
    {
        Name = "search_nist_controls",
        Description = "Search for NIST 800-53 controls by keyword or requirement",
        Arguments = new[]
        {
            new PromptArgument { Name = "query", Description = "Search query", Required = true },
            new PromptArgument { Name = "control_family", Description = "Filter by control family (e.g., AC, AU, IA)", Required = false }
        }
    };

    public static readonly McpPrompt ExplainRmf = new()
    {
        Name = "explain_rmf",
        Description = "Explain the Risk Management Framework (RMF) steps and activities",
        Arguments = new[]
        {
            new PromptArgument { Name = "rmf_step", Description = "RMF step: prepare, categorize, select, implement, assess, authorize, or monitor", Required = false },
            new PromptArgument { Name = "topic", Description = "Specific topic within RMF", Required = false }
        }
    };

    public static readonly McpPrompt ExplainStig = new()
    {
        Name = "explain_stig",
        Description = "Get detailed explanation of STIG requirements",
        Arguments = new[]
        {
            new PromptArgument { Name = "stig_id", Description = "STIG identifier", Required = true },
            new PromptArgument { Name = "rule_id", Description = "Specific rule ID within the STIG", Required = false }
        }
    };

    public static readonly McpPrompt SearchStigs = new()
    {
        Name = "search_stigs",
        Description = "Search for STIGs by technology, severity, or keyword",
        Arguments = new[]
        {
            new PromptArgument { Name = "query", Description = "Search query", Required = true },
            new PromptArgument { Name = "technology", Description = "Filter by technology (e.g., Windows, Linux, Azure)", Required = false },
            new PromptArgument { Name = "severity", Description = "Filter by severity: CAT I, CAT II, or CAT III", Required = false }
        }
    };

    public static readonly McpPrompt GetFedRampTemplate = new()
    {
        Name = "get_fedramp_template",
        Description = "Get FedRAMP templates and guidance documents",
        Arguments = new[]
        {
            new PromptArgument { Name = "template_type", Description = "Template type: ssp, poam, sar, or checklist", Required = true },
            new PromptArgument { Name = "impact_level", Description = "Impact level: Low, Moderate, or High", Required = false }
        }
    };

    public static readonly McpPrompt DetermineImpactLevel = new()
    {
        Name = "determine_impact_level",
        Description = "Determine appropriate FIPS 199 impact level based on system characteristics",
        Arguments = new[]
        {
            new PromptArgument { Name = "system_type", Description = "Type of system", Required = false },
            new PromptArgument { Name = "data_types", Description = "Types of data processed", Required = false }
        }
    };

    /// <summary>
    /// Get all knowledge base prompts
    /// </summary>
    public static IEnumerable<McpPrompt> GetAll() => new[]
    {
        ExplainNistControl,
        SearchNistControls,
        ExplainRmf,
        ExplainStig,
        SearchStigs,
        GetFedRampTemplate,
        DetermineImpactLevel
    };
}
