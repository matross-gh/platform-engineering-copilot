using Microsoft.Extensions.Configuration;

namespace Platform.Engineering.Copilot.Core.Configuration;

/// <summary>
/// Configuration for enabling/disabling specific agents
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "AgentConfiguration";

    private IConfiguration? _configuration;

    /// <summary>
    /// Sets the configuration to read agent enabled status from nested sections
    /// </summary>
    public void SetConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Check if a specific agent is enabled by reading from nested agent configuration
    /// </summary>
    public bool IsAgentEnabled(string agentName)
    {
        if (_configuration == null)
        {
            return false;
        }

        var agentSection = _configuration.GetSection($"{agentName}Agent");
        var enabled = agentSection.GetValue<bool?>("Enabled");
        return enabled ?? false;
    }
}
