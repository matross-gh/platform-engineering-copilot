using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using Platform.Engineering.Copilot.Core.Services.Azure;

namespace Platform.Engineering.Copilot.Core.Plugins;

/// <summary>
/// Plugin for managing Platform Engineering Copilot configuration.
/// Handles persistent settings like default Azure subscription, tenant, and authentication method across all agents.
/// </summary>
public class ConfigurationPlugin
{
    private readonly ILogger<ConfigurationPlugin> _logger;
    private readonly Services.ConfigService _configService;
    private readonly AzureMcpClient _azureMcpClient;

    public ConfigurationPlugin(
        ILogger<ConfigurationPlugin> logger,
        Services.ConfigService configService,
        AzureMcpClient azureMcpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _azureMcpClient = azureMcpClient ?? throw new ArgumentNullException(nameof(azureMcpClient));
    }

    [KernelFunction("set_azure_subscription")]
    [Description("⚠️ **PRIORITY ACTION** ⚠️ Configure the default Azure subscription for all Platform Engineering Copilot operations. " +
                 "This setting persists across sessions and container restarts via ~/.platform-copilot/config.json. " +
                 "🔴 **CRITICAL - CALL THIS FIRST**: When user says ANY of these phrases, call THIS function IMMEDIATELY (do NOT run assessments or other operations): " +
                 "'set my subscription', 'use subscription', 'set subscription to', 'my subscription is', 'configure subscription', " +
                 "'switch to subscription', 'change subscription', 'default subscription is'. " +
                 "This is a CONFIGURATION-ONLY function - it ONLY saves settings, does NOT scan/assess/deploy anything. " +
                 "Example user phrases: 'Set my subscription to 453c2549-4cc5-464f-ba66-acad920823e8', 'Use subscription abc-123', " +
                 "'My subscription is xyz-789', 'Configure subscription for all operations'. " +
                 "⚠️ IMPORTANT: After calling this function, return ONLY the success message - do NOT run assessments or ask follow-up questions.")]
    public string SetAzureSubscription(
        [Description("Azure subscription ID (GUID) to use as the default for all operations. Example: '453c2549-4cc5-464f-ba66-acad920823e8'")]
        string subscriptionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID cannot be empty"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Validate it's a GUID
            if (!Guid.TryParse(subscriptionId, out _))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Invalid subscription ID format: '{subscriptionId}'. Must be a valid GUID.",
                    example = "453c2549-4cc5-464f-ba66-acad920823e8"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            _logger.LogInformation("📋 Setting default Azure subscription: {SubscriptionId}", subscriptionId);

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
            _logger.LogError(ex, "Failed to set default Azure subscription");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Failed to save subscription: {ex.Message}"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [KernelFunction("get_azure_subscription")]
    [Description("Get the currently configured default Azure subscription. " +
                 "Returns the subscription ID that will be used for all operations if no subscription is explicitly specified. " +
                 "Use this to verify what subscription is configured or troubleshoot subscription context issues.")]
    public string GetAzureSubscription()
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
            _logger.LogError(ex, "Failed to get default Azure subscription");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Failed to read subscription: {ex.Message}"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [KernelFunction("clear_azure_subscription")]
    [Description("Clear the default Azure subscription configuration. " +
                 "After calling this, all operations will require an explicit subscription parameter. " +
                 "Use this when switching contexts or when you no longer want a default subscription.")]
    public string ClearAzureSubscription()
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
            _logger.LogError(ex, "Failed to clear default Azure subscription");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Failed to clear subscription: {ex.Message}"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [KernelFunction("show_config")]
    [Description("Show all Platform Engineering Copilot configuration settings. " +
                 "Displays the config file location, default subscription, and other settings. " +
                 "Use this to troubleshoot configuration issues or verify your setup.")]
    public string ShowConfig()
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
            _logger.LogError(ex, "Failed to show configuration");
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Failed to read configuration: {ex.Message}"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [KernelFunction("set_azure_tenant")]
    [Description("Configure which Azure tenant/directory to authenticate against for Azure operations. " +
                 "ONLY use this function when users explicitly want to change the tenant context. " +
                 "Keywords: 'use tenant', 'set tenant', 'authenticate to tenant', 'switch tenant'. " +
                 "Examples: 'Use tenant 12345678-aaaa-bbbb-cccc-123456789012', 'Set tenant to abc-123'. " +
                 "This is a CONFIGURATION function, NOT for creating resources.")]
    public async Task<string> SetAzureTenantAsync(
        [Description("Azure tenant ID (GUID) to use for authentication")]
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔐 Setting Azure tenant ID: {TenantId}", tenantId);

            // Save to persistent config
            _configService.SetTenantId(tenantId);

            // Update the MCP configuration
            var config = _azureMcpClient.GetType()
                .GetField("_configuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_azureMcpClient) as AzureMcpConfiguration;

            if (config != null)
            {
                config.TenantId = tenantId;
            }

            // Reinitialize MCP with new context
            await _azureMcpClient.InitializeAsync(cancellationToken);

            return $@"✅ **Azure Tenant Configured**

Tenant ID set to: `{tenantId}`

All Azure authentication will use this tenant.

💡 **What this means:**
- Service Principal authentication will use this tenant
- Resource queries will be scoped to this tenant
- Multi-tenant scenarios are now properly configured

📁 **Saved to:** ~/.platform-copilot/config.json";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Azure tenant");
            return $"❌ Failed to set tenant: {ex.Message}";
        }
    }

    [KernelFunction("set_authentication_method")]
    [Description("Set the authentication method for Azure operations. " +
                 "Use this when users specify how to authenticate. " +
                 "Methods: 'credential' (Service Principal/Managed Identity/Azure CLI), 'key', 'connectionString'. " +
                 "Examples: 'Use credential authentication', 'Set authentication to key-based'")]
    public async Task<string> SetAuthenticationMethodAsync(
        [Description("Authentication method: credential, key, or connectionString")]
        string authenticationMethod,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔑 Setting authentication method: {Method}", authenticationMethod);

            // Validate authentication method
            var validMethods = new[] { "credential", "key", "connectionString" };
            if (!validMethods.Contains(authenticationMethod.ToLower()))
            {
                return $@"❌ **Invalid Authentication Method**

Provided: `{authenticationMethod}`

Valid methods:
- **credential** - Azure Identity SDK (Service Principal, Managed Identity, Azure CLI)
- **key** - Access key authentication (for Storage, Cosmos DB, etc.)
- **connectionString** - Connection string authentication

Please specify one of the valid methods.";
            }

            // Save to persistent config
            _configService.SetAuthenticationMethod(authenticationMethod);

            // Update the MCP configuration
            var config = _azureMcpClient.GetType()
                .GetField("_configuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_azureMcpClient) as AzureMcpConfiguration;

            if (config != null)
            {
                config.AuthenticationMethod = authenticationMethod.ToLower();
            }

            // Reinitialize MCP with new context
            await _azureMcpClient.InitializeAsync(cancellationToken);

            var methodDescription = authenticationMethod.ToLower() switch
            {
                "credential" => "Azure Identity SDK - Will use Service Principal (AZURE_CLIENT_ID/SECRET), Managed Identity, or Azure CLI credentials",
                "key" => "Access Key - Uses storage account keys or database keys for authentication",
                "connectionString" => "Connection String - Uses full connection strings including credentials",
                _ => "Unknown method"
            };

            return $@"✅ **Authentication Method Configured**

Method set to: `{authenticationMethod}`

**Details:** {methodDescription}

All Azure MCP operations will use this authentication method.

📁 **Saved to:** ~/.platform-copilot/config.json";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting authentication method");
            return $"❌ Failed to set authentication method: {ex.Message}";
        }
    }
}
