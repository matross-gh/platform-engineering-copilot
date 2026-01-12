namespace Platform.Engineering.Copilot.Mcp.Prompts;

/// <summary>
/// Represents an MCP prompt definition.
/// Prompts are pre-defined templates that guide AI assistants in using the platform.
/// </summary>
public class McpPrompt
{
    /// <summary>
    /// Unique identifier for the prompt
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the prompt does
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Arguments that can be passed to the prompt
    /// </summary>
    public PromptArgument[] Arguments { get; init; } = Array.Empty<PromptArgument>();
}

/// <summary>
/// Represents an argument for an MCP prompt
/// </summary>
public class PromptArgument
{
    /// <summary>
    /// Argument name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the argument
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether the argument is required
    /// </summary>
    public bool Required { get; init; }
}

/// <summary>
/// Registry of all MCP prompts available in the platform.
/// Used by McpServer to respond to prompts/list requests.
/// </summary>
public static class PromptRegistry
{
    /// <summary>
    /// Get all registered MCP prompts
    /// </summary>
    public static IEnumerable<McpPrompt> GetAllPrompts()
    {
        return CompliancePrompts.GetAll()
            .Concat(InfrastructurePrompts.GetAll())
            .Concat(DiscoveryPrompts.GetAll())
            .Concat(CostManagementPrompts.GetAll())
            .Concat(KnowledgeBasePrompts.GetAll());
    }

    /// <summary>
    /// Get prompts by domain
    /// </summary>
    public static IEnumerable<McpPrompt> GetPromptsByDomain(string domain)
    {
        return domain.ToLowerInvariant() switch
        {
            "compliance" => CompliancePrompts.GetAll(),
            "infrastructure" => InfrastructurePrompts.GetAll(),
            "discovery" => DiscoveryPrompts.GetAll(),
            "cost" or "costmanagement" => CostManagementPrompts.GetAll(),
            "knowledge" or "knowledgebase" => KnowledgeBasePrompts.GetAll(),
            _ => Enumerable.Empty<McpPrompt>()
        };
    }

    /// <summary>
    /// Find a prompt by name
    /// </summary>
    public static McpPrompt? FindPrompt(string name)
    {
        return GetAllPrompts().FirstOrDefault(p => 
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
