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

    /// <summary>
    /// Dictionary of agent names and their enabled status
    /// </summary>
    public Dictionary<string, bool> EnabledAgents { get; set; } = new()
    {
        { "Infrastructure", true },
        { "CostManagement", true },
        { "Environment", true },
        { "Discovery", true },
        { "ServiceCreation", true },
        { "Compliance", true },
        { "Security", true },
        { "Document", true }
    };

    /// <summary>
    /// Check if a specific agent is enabled
    /// </summary>
    public bool IsAgentEnabled(string agentName)
    {
        return EnabledAgents.TryGetValue(agentName, out var enabled) && enabled;
    }
}
