# Configuration Plugin - Persistent Azure Subscription Management

## Overview

The **ConfigurationPlugin** provides persistent configuration management across all agents in the Platform Engineering Copilot. This solves the stateless nature of the MCP protocol used by GitHub Copilot, ensuring that subscription settings and other configurations persist across sessions and container restarts.

## Architecture

### Components

1. **ConfigService** (`Core/Services/ConfigService.cs`)
   - Manages persistent storage of configuration in JSON format
   - Location: `~/.platform-copilot/config.json`
   - Thread-safe singleton service
   - Survives Docker container restarts

2. **ConfigurationPlugin** (`Core/Plugins/ConfigurationPlugin.cs`)
   - Shared Semantic Kernel plugin available to all agents
   - Registered as transient in DI (fresh instance per agent)
   - Provides 4 KernelFunctions for configuration management

3. **Agent Integration**
   - All 10 agents inject and register ConfigurationPlugin
   - Automatic access to subscription management functions
   - Works seamlessly with Semantic Kernel's auto function calling

## Available Functions

### 1. `set_azure_subscription`
**Description:** Set the default Azure subscription for all operations

**Parameters:**
- `subscriptionId` (string, required): Azure subscription GUID

**Example:**
```
@platform set my subscription to 453c2549-4cc5-464f-ba66-acad920823e8
```

**Response:**
```
✅ Default Azure subscription set successfully!

📋 Subscription Details:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Subscription ID: 453c2549-4cc5-464f-ba66-acad920823e8
• Saved to: ~/.platform-copilot/config.json
• Last Updated: 2025-11-17 15:30:00Z

✨ This subscription will now be used automatically for all Azure operations
```

### 2. `get_azure_subscription`
**Description:** Show the current default Azure subscription

**Example:**
```
@platform what's my default subscription?
```

**Response:**
```
📋 Current Default Azure Subscription
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Subscription ID: 453c2549-4cc5-464f-ba66-acad920823e8
• Last Updated: 2025-11-17 15:30:00Z
```

### 3. `clear_azure_subscription`
**Description:** Remove the saved default subscription

**Example:**
```
@platform clear my subscription setting
```

**Response:**
```
✅ Default subscription cleared successfully!

The system will now require you to specify a subscription for each operation.
```

### 4. `set_azure_tenant`
**Description:** Configure which Azure tenant/directory to authenticate against

**Parameters:**
- `tenantId` (string, required): Azure tenant ID (GUID)

**Example:**
```
@platform use tenant 12345678-aaaa-bbbb-cccc-123456789012
```

**Response:**
```
✅ Azure Tenant Configured

Tenant ID set to: `12345678-aaaa-bbbb-cccc-123456789012`

All Azure authentication will use this tenant.

💡 What this means:
- Service Principal authentication will use this tenant
- Resource queries will be scoped to this tenant
- Multi-tenant scenarios are now properly configured

📁 Saved to: ~/.platform-copilot/config.json
```

### 5. `set_authentication_method`
**Description:** Set the authentication method for Azure operations

**Parameters:**
- `authenticationMethod` (string, required): credential, key, or connectionString

**Example:**
```
@platform use credential authentication
```

**Response:**
```
✅ Authentication Method Configured

Method set to: `credential`

Details: Azure Identity SDK - Will use Service Principal (AZURE_CLIENT_ID/SECRET), 
Managed Identity, or Azure CLI credentials

All Azure MCP operations will use this authentication method.

📁 Saved to: ~/.platform-copilot/config.json
```

**Valid Methods:**
- **credential** - Azure Identity SDK (Service Principal, Managed Identity, Azure CLI)
- **key** - Access key authentication (for Storage, Cosmos DB, etc.)
- **connectionString** - Connection string authentication

### 6. `show_config`
**Description:** Display all configuration settings including file location

**Example:**
```
@platform show my config
```

**Response:**
```
⚙️ Platform Engineering Copilot Configuration

📁 Config file: ~/.platform-copilot/config.json
✅ Status: Configuration file exists

Settings:

🔹 Default Subscription
   `453c2549-4cc5-464f-ba66-acad920823e8`

🔹 Tenant ID
   `12345678-aaaa-bbbb-cccc-123456789012`

🔹 Authentication Method
   credential

🔹 Azure Environment
   Default (AzureCloud)

🔹 Last Updated
   2025-11-17 15:30:00 UTC
```

## Configuration File Format

**Location:** `~/.platform-copilot/config.json`

**Structure:**
```json
{
  "defaultSubscription": "453c2549-4cc5-464f-ba66-acad920823e8",
  "lastUpdated": "2025-11-17T15:30:00Z",
  "subscriptionName": "Production",
  "azureEnvironment": "AzureUSGovernment",
  "tenantId": "12345678-aaaa-bbbb-cccc-123456789012",
  "authenticationMethod": "credential"
}
```

**Manual Editing:**
You can edit this file directly if needed. The service will read the latest values on each request.

## Implementation Details

### Service Registration

In `Core/ServiceCollectionExtensions.cs`:
```csharp
// Singleton service for persistent config storage
services.AddSingleton<ConfigService>();

// Transient plugin injected into each agent
services.AddTransient<Plugins.ConfigurationPlugin>();
```

### Agent Integration Pattern

All 10 agents follow this pattern:

```csharp
public SomeAgent(
    ISemanticKernelService semanticKernelService,
    ILogger<SomeAgent> logger,
    SomePlugin somePlugin,
    Platform.Engineering.Copilot.Core.Plugins.ConfigurationPlugin configurationPlugin)
{
    _kernel = semanticKernelService.CreateSpecializedKernel(AgentType.SomeType);
    
    // Register shared configuration plugin FIRST
    _kernel.Plugins.Add(
        KernelPluginFactory.CreateFromObject(configurationPlugin, "ConfigurationPlugin"));
    
    // Then register agent-specific plugins
    _kernel.Plugins.Add(
        KernelPluginFactory.CreateFromObject(somePlugin, "SomePlugin"));
}
```

### Agents with ConfigurationPlugin

✅ **All 10 agents registered:**
1. ComplianceAgent
2. InfrastructureAgent
3. CostManagementAgent
4. EnvironmentAgent
5. DiscoveryAgent
6. KnowledgeBaseAgent
7. CodeScanningAgent
8. DocumentAgent
9. AtoPreparationAgent
10. OrchestratorAgent

## Benefits

### 1. **Stateless Protocol Support**
- GitHub Copilot MCP uses JSONRPC 2.0 over stdio (stateless)
- IMemoryCache doesn't persist between requests
- File-based storage solves this limitation

### 2. **Container Restart Persistence**
- Config file stored in `~/.platform-copilot/` (user home directory)
- Survives Docker container rebuilds and restarts
- No need to reconfigure after deployment

### 3. **Consistent User Experience**
- Works like `az account set` - one command, permanent until changed
- No need to specify subscription in every command
- Reduces cognitive load and command verbosity

### 4. **Cross-Agent Consistency**
- All agents share the same configuration
- Ensures consistent subscription usage across operations
- Prevents confusion from different defaults per agent

## Usage Patterns

### First-Time Setup
```bash
# Set your default subscription
@platform set my subscription to 453c2549-4cc5-464f-ba66-acad920823e8

# Set your tenant (if using Service Principal or multi-tenant)
@platform use tenant 12345678-aaaa-bbbb-cccc-123456789012

# Set authentication method (optional, defaults to 'credential')
@platform use credential authentication

# Verify it's saved
@platform show config

# Now use any command without specifying subscription
@platform check NIST 800-53 compliance
@platform list all VMs
@platform analyze costs for this month
```

### Changing Subscriptions
```bash
# Switch to a different subscription
@platform set my subscription to f8b41c88-2b3e-4d7a-9c5f-1a8e3d6b4f9e

# All subsequent commands use the new subscription
@platform check security posture
```

### Multi-Subscription Workflow
```bash
# Set default to Production
@platform set my subscription to 453c2549-4cc5-464f-ba66-acad920823e8

# Most commands use Production automatically
@platform check compliance

# Override for a specific command if needed
@platform check compliance in subscription f8b41c88-2b3e-4d7a-9c5f-1a8e3d6b4f9e
```

## Troubleshooting

### Config file not found
**Issue:** `~/.platform-copilot/config.json` doesn't exist

**Solution:** Run `set_azure_subscription` to create it automatically:
```
@platform set my subscription to YOUR_SUBSCRIPTION_ID
```

### Permission errors
**Issue:** Cannot write to `~/.platform-copilot/config.json`

**Solution:** Check directory permissions:
```bash
mkdir -p ~/.platform-copilot
chmod 755 ~/.platform-copilot
```

### Subscription not persisting
**Issue:** Subscription resets after container restart

**Solution:** Ensure config file is in the correct location (user home directory, not container-specific path)

### Invalid subscription ID
**Issue:** Set operation fails with validation error

**Solution:** Ensure subscription ID is a valid GUID format:
```
Valid:   453c2549-4cc5-464f-ba66-acad920823e8
Invalid: my-subscription
Invalid: 12345
```

## Design Rationale

### Why Shared Plugin vs. Agent-Specific?

**User Insight:** "Shouldn't set_azure_subscription be in core since all plugins use it? Also move set_azure_tenant and set_authentication_method."

**Decision:** Create shared ConfigurationPlugin in Core with all Azure configuration functions

**Benefits:**
- ✅ DRY principle - single source of truth
- ✅ Consistent behavior across all agents
- ✅ Easier maintenance and updates
- ✅ Reduced code duplication (removed 120+ lines from InfrastructurePlugin)
- ✅ Logical grouping of all configuration-related functions

### Why File-Based Storage?

**Alternatives Considered:**
1. **IMemoryCache** - Doesn't persist across requests in GitHub Copilot MCP
2. **Database** - Overkill for simple configuration, adds dependency
3. **Environment Variables** - Not user-configurable at runtime
4. **Azure Key Vault** - Too complex, requires Azure credentials

**File-Based Chosen Because:**
- ✅ Simple, no dependencies
- ✅ Cross-platform (user home directory)
- ✅ Survives container restarts
- ✅ Easy to backup and manually edit
- ✅ Works like standard CLI tools (az, kubectl, etc.)

## Future Enhancements

### Potential Features
1. **Multiple Profiles**
   ```json
   {
     "profiles": {
       "production": { "subscription": "...", "tenant": "..." },
       "development": { "subscription": "...", "tenant": "..." }
     },
     "activeProfile": "production"
   }
   ```

2. **Tenant Configuration**
   - Set default Azure AD tenant
   - Set authentication method preference
   - Set Azure environment (Commercial, Government, China)

3. **Resource Group Defaults**
   ```json
   {
     "defaultSubscription": "...",
     "defaultResourceGroup": "rg-platform-prod",
     "defaultLocation": "eastus2"
   }
   ```

4. **Config Validation**
   - Verify subscription exists and is accessible
   - Validate tenant ID against Azure AD
   - Check authentication status

5. **Config Sync**
   - Sync settings across multiple machines
   - Import/export configuration profiles
   - Integration with Azure CLI config

## Related Documentation

- [Authentication Guide](./AUTHENTICATION.md) - Azure authentication methods
- [Getting Started](./GETTING-STARTED.md) - Initial setup and configuration
- [Agent Orchestration](./AGENT-ORCHESTRATION.md) - Multi-agent architecture
- [Development Guide](./DEVELOPMENT.md) - Contributing to the project

## Support

For issues or questions about configuration management:
1. Check [GitHub Issues](https://github.com/your-org/platform-engineering-copilot/issues)
2. Review troubleshooting section above
3. Check Docker logs: `docker-compose logs platform-mcp`
4. Verify config file exists: `cat ~/.platform-copilot/config.json`
