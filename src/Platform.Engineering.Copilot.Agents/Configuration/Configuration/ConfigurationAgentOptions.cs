namespace Platform.Engineering.Copilot.Agents.Configuration.Configuration;

/// <summary>
/// Configuration options for the Configuration Agent.
/// </summary>
public class ConfigurationAgentOptions
{
    /// <summary>
    /// Temperature for LLM responses (0.0 = deterministic, 1.0 = creative).
    /// Configuration agent uses low temperature for consistent responses.
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Maximum tokens for LLM responses.
    /// Configuration responses are typically short.
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Whether the agent is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
