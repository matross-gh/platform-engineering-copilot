using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Configuration.Configuration;
using Platform.Engineering.Copilot.Agents.Configuration.Tools;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.Agents.Configuration.Agents;

/// <summary>
/// Configuration Agent for managing Platform Engineering Copilot settings.
/// Handles subscription configuration, environment settings, and user preferences.
/// This is a lightweight agent focused on configuration operations.
/// </summary>
public class ConfigurationAgent : BaseAgent
{
    public override string AgentId => "configuration";
    public override string AgentName => "Configuration Agent";
    public override string Description =>
        "Manages Platform Engineering Copilot configuration including Azure subscription settings, " +
        "default preferences, and environment configuration. Use this agent to set your default " +
        "subscription before running compliance scans or other Azure operations.";

    protected override float Temperature => (float)_options.Temperature;
    protected override int MaxTokens => _options.MaxTokens;

    private readonly ConfigurationAgentOptions _options;

    public ConfigurationAgent(
        IChatClient chatClient,
        ILogger<ConfigurationAgent> logger,
        IOptions<ConfigurationAgentOptions> options,
        ConfigurationTool configurationTool,
        IAgentStateManager? agentStateManager = null,
        ISharedMemory? sharedMemory = null)
        : base(chatClient, logger, agentStateManager, sharedMemory)
    {
        _options = options?.Value ?? new ConfigurationAgentOptions();

        // Register the configuration tool
        RegisterTool(configurationTool);

        Logger.LogInformation("✅ Configuration Agent initialized (Temperature: {Temperature}, MaxTokens: {MaxTokens})",
            _options.Temperature, _options.MaxTokens);
    }

    /// <summary>
    /// Get system prompt with configuration-specific guidance.
    /// </summary>
    protected override string GetSystemPrompt()
    {
        return @"You are the Configuration Agent for Platform Engineering Copilot.

## Your Purpose
You help users configure their Platform Engineering Copilot settings, especially Azure subscription defaults.

## Primary Capabilities
1. **Set Default Subscription** - Configure the Azure subscription for all operations
2. **Get Current Configuration** - Show current settings
3. **Clear Configuration** - Remove saved settings

## ⚠️ CRITICAL: When to Use the configure_subscription Tool

You MUST immediately call the `configure_subscription` tool when the user mentions ANY of these phrases:
- ""set my subscription""
- ""set subscription to""
- ""use subscription""
- ""configure subscription""
- ""my subscription is""
- ""switch to subscription""
- ""change subscription""
- ""default subscription""

### Examples of User Requests and Your Actions:

**User says:** ""Set my subscription to 453c2549-4cc5-464f-ba66-acad920823e8""
**You do:** Call `configure_subscription` with action='set' and subscriptionId='453c2549-4cc5-464f-ba66-acad920823e8'

**User says:** ""What's my current subscription?""
**You do:** Call `configure_subscription` with action='get'

**User says:** ""Clear my subscription settings""
**You do:** Call `configure_subscription` with action='clear'

## Response Format
After calling the tool, confirm the action taken and explain what the user can do next:
- If subscription set: Suggest they can now run compliance scans, discover resources, etc.
- If showing config: Explain what each setting means
- If cleared: Note they'll need to set a subscription before Azure operations

## Important Notes
- Always validate subscription IDs are in GUID format before setting
- Be friendly and concise in responses
- If user provides something that doesn't look like a subscription ID, ask for clarification";
    }
}
