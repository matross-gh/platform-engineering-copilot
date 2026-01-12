using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Core.Services;

namespace Platform.Engineering.Copilot.Agents.Configuration.Tools;

/// <summary>
/// Agent FX tool for managing Platform Engineering Copilot configuration.
/// Handles persistent settings like default Azure subscription across all agents.
/// Replaces the legacy Semantic Kernel ConfigurationPlugin.
/// </summary>
public class ConfigurationTool : BaseTool
{
    private readonly ConfigService _configService;

    public override string Name => "configure_subscription";

    public override string Description =>
        "⚠️ **PRIORITY ACTION** ⚠️ Configure the default Azure subscription for all Platform Engineering Copilot operations. " +
        "This setting persists across sessions and container restarts via ~/.platform-copilot/config.json. " +
        "🔴 **CRITICAL - CALL THIS FIRST**: When user says ANY of these phrases, call THIS function IMMEDIATELY: " +
        "'set my subscription', 'use subscription', 'set subscription to', 'my subscription is', 'configure subscription', " +
        "'switch to subscription', 'change subscription', 'default subscription is'. " +
        "Actions: 'set' to configure, 'get' to show current, 'clear' to remove default. " +
        "Example: 'Set my subscription to 453c2549-4cc5-464f-ba66-acad920823e8'";

    public ConfigurationTool(
        ILogger<ConfigurationTool> logger,
        ConfigService configService) : base(logger)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        Parameters.Add(new ToolParameter(
            name: "action",
            description: "Action to perform: 'set' (configure subscription), 'get' (show current), 'clear' (remove default), 'show' (display all config)",
            required: true));

        Parameters.Add(new ToolParameter(
            name: "subscriptionId",
            description: "Azure subscription ID (GUID) when action is 'set'. Example: '453c2549-4cc5-464f-ba66-acad920823e8'",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var action = GetRequiredString(arguments, "action").ToLowerInvariant();
        var subscriptionId = GetOptionalString(arguments, "subscriptionId");

        await Task.CompletedTask; // Satisfy async requirement

        return action switch
        {
            "set" => SetSubscription(subscriptionId),
            "get" => GetSubscription(),
            "clear" => ClearSubscription(),
            "show" => ShowConfig(),
            _ => ToJson(new { success = false, error = $"Unknown action: '{action}'. Valid actions: set, get, clear, show" })
        };
    }

    private string SetSubscription(string? subscriptionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Subscription ID is required for 'set' action"
                });
            }

            // Validate it's a GUID
            if (!Guid.TryParse(subscriptionId, out _))
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Invalid subscription ID format: '{subscriptionId}'. Must be a valid GUID.",
                    example = "453c2549-4cc5-464f-ba66-acad920823e8"
                });
            }

            Logger.LogInformation("📋 Setting default Azure subscription: {SubscriptionId}", subscriptionId);

            // Persist to config file
            _configService.SetDefaultSubscription(subscriptionId);
            var configPath = _configService.GetConfigFilePath();

            return $@"✅ **Azure Subscription Configured**

Default subscription set to: `{subscriptionId}`

All subsequent Platform Engineering Copilot operations will use this subscription unless explicitly overridden.

💡 **What this means:**
- Compliance scans will target this subscription
- Infrastructure deployments will go to this subscription  
- Cost analysis will use this subscription's data
- Environment queries will scope to this subscription

📁 **Configuration saved to:** `{configPath}`

This setting persists across:
- GitHub Copilot chat sessions
- Docker container restarts
- Terminal sessions

🔄 **To change:** Use `set subscription to <new-id>` anytime
🗑️ **To clear:** Use `clear my subscription setting`";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set default Azure subscription");
            return ToJson(new
            {
                success = false,
                error = $"Failed to save subscription: {ex.Message}"
            });
        }
    }

    private string GetSubscription()
    {
        try
        {
            var subscriptionId = _configService.GetDefaultSubscription();
            var configPath = _configService.GetConfigFilePath();

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return $@"ℹ️ **No Default Subscription Configured**

No default Azure subscription is currently set.

**To configure:**
Use `set my subscription to <subscription-id>`

**Example:**
`set my subscription to 453c2549-4cc5-464f-ba66-acad920823e8`

📁 **Config file location:** `{configPath}`
(File will be created when you set a subscription)";
            }

            return $@"✅ **Current Default Subscription**

Subscription ID: `{subscriptionId}`

All operations will use this subscription unless you explicitly specify a different one.

📁 **Configuration file:** `{configPath}`

🔄 **To change:** Use `set subscription to <new-id>`
🗑️ **To clear:** Use `clear my subscription setting`";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get default Azure subscription");
            return ToJson(new
            {
                success = false,
                error = $"Failed to read subscription: {ex.Message}"
            });
        }
    }

    private string ClearSubscription()
    {
        try
        {
            var currentSub = _configService.GetDefaultSubscription();
            _configService.ClearDefaultSubscription();
            var configPath = _configService.GetConfigFilePath();

            if (string.IsNullOrWhiteSpace(currentSub))
            {
                return $@"ℹ️ **No Subscription to Clear**

No default subscription was configured.

📁 **Config file:** `{configPath}`";
            }

            return $@"✅ **Default Subscription Cleared**

Removed default subscription: `{currentSub}`

**Impact:**
All operations will now require you to specify a subscription explicitly.

**To set a new default:**
Use `set my subscription to <subscription-id>`

📁 **Config file:** `{configPath}`";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to clear default Azure subscription");
            return ToJson(new
            {
                success = false,
                error = $"Failed to clear subscription: {ex.Message}"
            });
        }
    }

    private string ShowConfig()
    {
        try
        {
            var config = _configService.GetConfig();
            var configPath = _configService.GetConfigFilePath();
            var fileExists = File.Exists(configPath);

            if (config == null || !fileExists)
            {
                return $@"ℹ️ **Platform Engineering Copilot Configuration**

**Status:** No configuration file found

📁 **Config file location:** `{configPath}`

**To create:**
Set a default subscription: `set my subscription to <subscription-id>`

This will create the config file automatically.";
            }

            return $@"⚙️ **Platform Engineering Copilot Configuration**

📁 **Config file:** `{configPath}`
✅ **Status:** Configuration file exists

**Settings:**

🔹 **Default Subscription**
   {(string.IsNullOrWhiteSpace(config.DefaultSubscription) ? "Not set" : $"`{config.DefaultSubscription}`")}
   {(!string.IsNullOrWhiteSpace(config.SubscriptionName) ? $"Name: {config.SubscriptionName}" : "")}

🔹 **Azure Environment**
   {config.AzureEnvironment ?? "Default (AzureCloud)"}

🔹 **Last Updated**
   {(config.LastUpdated.HasValue ? config.LastUpdated.Value.ToString("yyyy-MM-dd HH:mm:ss UTC") : "Never")}

**Available Commands:**
- `set my subscription to <id>` - Configure default subscription
- `get my subscription` - Show current default
- `clear my subscription setting` - Remove default
- `show config` - Display this information";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to show configuration");
            return ToJson(new
            {
                success = false,
                error = $"Failed to read configuration: {ex.Message}"
            });
        }
    }
}
