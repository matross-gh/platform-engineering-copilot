namespace Platform.Engineering.Copilot.Mcp.Prompts;

/// <summary>
/// MCP Prompts for discovery domain operations.
/// These prompts guide AI assistants in using the platform's resource discovery capabilities.
/// </summary>
public static class DiscoveryPrompts
{
    public static readonly McpPrompt DiscoverResources = new()
    {
        Name = "discover_resources",
        Description = "Discover and inventory Azure resources across subscriptions",
        Arguments = new[]
        {
            new PromptArgument { Name = "subscription_id", Description = "Azure subscription ID to scan", Required = false },
            new PromptArgument { Name = "resource_type", Description = "Filter by resource type (e.g., Microsoft.Compute/virtualMachines)", Required = false },
            new PromptArgument { Name = "tags", Description = "Filter by tags (JSON format)", Required = false }
        }
    };

    public static readonly McpPrompt GetResourceDetails = new()
    {
        Name = "get_resource_details",
        Description = "Get detailed information about a specific Azure resource",
        Arguments = new[]
        {
            new PromptArgument { Name = "resource_id", Description = "Full Azure resource ID", Required = true },
            new PromptArgument { Name = "include_metrics", Description = "Include performance metrics (true/false)", Required = false }
        }
    };

    public static readonly McpPrompt CheckResourceHealth = new()
    {
        Name = "check_resource_health",
        Description = "Check the health status of Azure resources",
        Arguments = new[]
        {
            new PromptArgument { Name = "resource_id", Description = "Azure resource ID to check", Required = true }
        }
    };

    public static readonly McpPrompt MapDependencies = new()
    {
        Name = "map_dependencies",
        Description = "Map dependencies between Azure resources",
        Arguments = new[]
        {
            new PromptArgument { Name = "resource_id", Description = "Starting resource ID", Required = true },
            new PromptArgument { Name = "depth", Description = "Dependency traversal depth (default: 2)", Required = false }
        }
    };

    public static readonly McpPrompt ListSubscriptions = new()
    {
        Name = "list_subscriptions",
        Description = "List available Azure subscriptions",
        Arguments = Array.Empty<PromptArgument>()
    };

    /// <summary>
    /// Get all discovery prompts
    /// </summary>
    public static IEnumerable<McpPrompt> GetAll() => new[]
    {
        DiscoverResources,
        GetResourceDetails,
        CheckResourceHealth,
        MapDependencies,
        ListSubscriptions
    };
}
