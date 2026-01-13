using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Platform.Engineering.Copilot.Agents.Common;

/// <summary>
/// Base class for all agent tools.
/// Provides common functionality for defining and executing tools.
/// </summary>
public abstract class BaseTool
{
    protected readonly ILogger Logger;

    /// <summary>
    /// Unique name of the tool (used by AI for function calling)
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Description of what the tool does (shown to AI)
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Tool parameter definitions
    /// </summary>
    public List<ToolParameter> Parameters { get; } = new();

    protected BaseTool(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute the tool with provided arguments
    /// </summary>
    public abstract Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert to AI tool for chat completion - returns a custom wrapper
    /// </summary>
    public AITool AsAITool()
    {
        // Return a simple function wrapper that can be used with chat completion
        return new ToolWrapper(this);
    }
    
    /// <summary>
    /// Internal wrapper that exposes BaseTool as an AIFunction
    /// </summary>
    private sealed class ToolWrapper : AIFunction
    {
        private readonly BaseTool _tool;
        
        public ToolWrapper(BaseTool tool) => _tool = tool;
        
        public override string Name => _tool.Name;
        public override string Description => _tool.Description;
        
        public override JsonElement JsonSchema => BuildParameterSchema();
        
        private JsonElement BuildParameterSchema()
        {
            var properties = new Dictionary<string, object>();
            var required = new List<string>();
            
            foreach (var param in _tool.Parameters)
            {
                properties[param.Name] = new Dictionary<string, string>
                {
                    ["type"] = param.Type,
                    ["description"] = param.Description
                };
                if (param.Required)
                    required.Add(param.Name);
            }
            
            var schema = new
            {
                type = "object",
                properties,
                required
            };
            
            return JsonSerializer.SerializeToElement(schema);
        }

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var argDict = arguments?.ToDictionary(kv => kv.Key, kv => kv.Value)
                          ?? new Dictionary<string, object?>();
            return await _tool.ExecuteAsync(argDict, cancellationToken);
        }
    }

    /// <summary>
    /// Get a required string argument
    /// </summary>
    protected string GetRequiredString(IDictionary<string, object?> arguments, string parameterName)
    {
        if (!arguments.TryGetValue(parameterName, out var value) || value == null)
        {
            throw new ArgumentException($"Required parameter '{parameterName}' is missing");
        }

        return value.ToString() ?? throw new ArgumentException($"Parameter '{parameterName}' cannot be null");
    }

    /// <summary>
    /// Get an optional string argument
    /// </summary>
    protected string? GetOptionalString(IDictionary<string, object?> arguments, string parameterName)
    {
        if (arguments.TryGetValue(parameterName, out var value) && value != null)
        {
            return value.ToString();
        }
        return null;
    }

    /// <summary>
    /// Get an optional boolean argument
    /// </summary>
    protected bool GetOptionalBool(IDictionary<string, object?> arguments, string parameterName, bool defaultValue = false)
    {
        if (arguments.TryGetValue(parameterName, out var value) && value != null)
        {
            if (value is bool boolValue) return boolValue;
            if (bool.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    /// <summary>
    /// Get an optional integer argument
    /// </summary>
    protected int? GetOptionalInt(IDictionary<string, object?> arguments, string parameterName)
    {
        if (arguments.TryGetValue(parameterName, out var value) && value != null)
        {
            if (value is int intValue) return intValue;
            if (value is long longValue) return (int)longValue;
            if (int.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return null;
    }

    /// <summary>
    /// Get an optional decimal argument
    /// </summary>
    protected decimal? GetOptionalDecimal(IDictionary<string, object?> arguments, string parameterName)
    {
        if (arguments.TryGetValue(parameterName, out var value) && value != null)
        {
            if (value is decimal decValue) return decValue;
            if (value is double doubleValue) return (decimal)doubleValue;
            if (decimal.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return null;
    }

    /// <summary>
    /// Serialize result to JSON
    /// </summary>
    protected string ToJson<T>(T result)
    {
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}

/// <summary>
/// Defines a tool parameter
/// </summary>
public class ToolParameter
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public string Type { get; set; }
    public object? DefaultValue { get; set; }

    public ToolParameter(string name, string description, bool required = false, string type = "string", object? defaultValue = null)
    {
        Name = name;
        Description = description;
        Required = required;
        Type = type;
        DefaultValue = defaultValue;
    }
}
