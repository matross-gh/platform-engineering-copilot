using Microsoft.Extensions.Logging;

namespace Platform.Engineering.Copilot.Agents.Common;

/// <summary>
/// Registry of all available tools across agents
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, BaseTool> _tools = new();
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a tool
    /// </summary>
    public void Register(BaseTool tool)
    {
        if (_tools.ContainsKey(tool.Name))
        {
            _logger.LogWarning("Tool '{ToolName}' already registered, replacing", tool.Name);
        }

        _tools[tool.Name] = tool;
        _logger.LogInformation("Registered tool: {ToolName}", tool.Name);
    }

    /// <summary>
    /// Register multiple tools
    /// </summary>
    public void RegisterRange(IEnumerable<BaseTool> tools)
    {
        foreach (var tool in tools)
        {
            Register(tool);
        }
    }

    /// <summary>
    /// Get a tool by name
    /// </summary>
    public BaseTool? GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    /// <summary>
    /// Get all registered tools
    /// </summary>
    public IEnumerable<BaseTool> GetAllTools()
    {
        return _tools.Values;
    }

    /// <summary>
    /// Get tools for a specific agent (by prefix convention)
    /// </summary>
    public IEnumerable<BaseTool> GetToolsForAgent(string agentPrefix)
    {
        return _tools.Values.Where(t => t.Name.StartsWith(agentPrefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if a tool exists
    /// </summary>
    public bool HasTool(string name)
    {
        return _tools.ContainsKey(name);
    }

    /// <summary>
    /// Get tool count
    /// </summary>
    public int Count => _tools.Count;
}
