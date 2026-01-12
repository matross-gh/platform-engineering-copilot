namespace Platform.Engineering.Copilot.Mcp.Prompts;

/// <summary>
/// MCP Prompts for infrastructure domain operations.
/// These prompts guide AI assistants in using the platform's infrastructure capabilities.
/// </summary>
public static class InfrastructurePrompts
{
    public static readonly McpPrompt GenerateBicep = new()
    {
        Name = "generate_bicep",
        Description = "Generate a Bicep template for Azure infrastructure",
        Arguments = new[]
        {
            new PromptArgument { Name = "resource_type", Description = "Type of Azure resource (e.g., aks, vm, storage, network)", Required = true },
            new PromptArgument { Name = "environment", Description = "Target environment: dev, staging, or prod", Required = false },
            new PromptArgument { Name = "compliance_level", Description = "Compliance requirements: commercial, govcloud, or il5", Required = false }
        }
    };

    public static readonly McpPrompt GenerateTerraform = new()
    {
        Name = "generate_terraform",
        Description = "Generate a Terraform template for Azure infrastructure",
        Arguments = new[]
        {
            new PromptArgument { Name = "resource_type", Description = "Type of Azure resource (e.g., aks, vm, storage, network)", Required = true },
            new PromptArgument { Name = "environment", Description = "Target environment: dev, staging, or prod", Required = false },
            new PromptArgument { Name = "compliance_level", Description = "Compliance requirements: commercial, govcloud, or il5", Required = false }
        }
    };

    public static readonly McpPrompt ProvisionResources = new()
    {
        Name = "provision_resources",
        Description = "Provision Azure resources using generated templates",
        Arguments = new[]
        {
            new PromptArgument { Name = "template", Description = "The IaC template to deploy", Required = true },
            new PromptArgument { Name = "subscription_id", Description = "Target Azure subscription", Required = true },
            new PromptArgument { Name = "resource_group", Description = "Target resource group", Required = true },
            new PromptArgument { Name = "dry_run", Description = "Preview changes without deploying (true/false)", Required = false }
        }
    };

    public static readonly McpPrompt AnalyzeScaling = new()
    {
        Name = "analyze_scaling",
        Description = "Analyze resource utilization and provide scaling recommendations",
        Arguments = new[]
        {
            new PromptArgument { Name = "resource_id", Description = "Azure resource ID to analyze", Required = true },
            new PromptArgument { Name = "time_range", Description = "Analysis time range (e.g., 7d, 30d)", Required = false }
        }
    };

    public static readonly McpPrompt ManageAzureArc = new()
    {
        Name = "manage_azure_arc",
        Description = "Onboard or manage hybrid resources with Azure Arc",
        Arguments = new[]
        {
            new PromptArgument { Name = "operation", Description = "Operation: onboard, status, or configure", Required = true },
            new PromptArgument { Name = "resource_type", Description = "Resource type: server, kubernetes, or data-services", Required = true }
        }
    };

    /// <summary>
    /// Get all infrastructure prompts
    /// </summary>
    public static IEnumerable<McpPrompt> GetAll() => new[]
    {
        GenerateBicep,
        GenerateTerraform,
        ProvisionResources,
        AnalyzeScaling,
        ManageAzureArc
    };
}
