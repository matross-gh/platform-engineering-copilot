# Infrastructure Agent Configuration Implementation

## Overview

The Infrastructure Agent now fully implements the configuration settings defined in `appsettings.json`. This was previously a planned feature (as noted in `RELEASE_NOTES_v0.6.35.md`) and is now complete.

## What Was Implemented

### 1. Configuration Class (`InfrastructureAgentOptions.cs`)

Created a new configuration class that binds to the `InfrastructureAgent` section in `appsettings.json`:

```csharp
public class InfrastructureAgentOptions
{
    public const string SectionName = "InfrastructureAgent";
    
    public double Temperature { get; set; } = 0.4;
    public int MaxTokens { get; set; } = 8000;
    public string DefaultRegion { get; set; } = "eastus";
    public bool EnableComplianceEnhancement { get; set; } = true;
    public string DefaultComplianceFramework { get; set; } = "FedRAMPHigh";
    public bool EnablePredictiveScaling { get; set; } = true;
    public bool EnableNetworkDesign { get; set; } = true;
}
```

### 2. Dependency Injection Registration

Updated `ServiceCollectionExtensions.cs` to:
- Register `InfrastructureAgentOptions` from configuration
- Accept `IConfiguration` parameter in `AddInfrastructureAgent()`

Updated all `Program.cs` files (Chat, Admin API, MCP) to pass `builder.Configuration`:

```csharp
builder.Services.AddInfrastructureAgent(builder.Configuration);
```

### 3. Agent Implementation

Modified `InfrastructureAgent.cs` to:
- Inject `IOptions<InfrastructureAgentOptions>`
- Use `_options.Temperature` and `_options.MaxTokens` instead of hardcoded values
- Conditionally inject optional services based on `Enable*` flags

**Before:**
```csharp
Temperature = 0.1,  // Hardcoded
MaxTokens = 4096    // Hardcoded
```

**After:**
```csharp
Temperature = _options.Temperature,  // From config (default: 0.4)
MaxTokens = _options.MaxTokens       // From config (default: 8000)
```

### 4. Conditional Service Enablement

Services are now conditionally enabled based on configuration flags:

| Configuration Flag | Service | Behavior When Disabled |
|-------------------|---------|------------------------|
| `EnableNetworkDesign` | `INetworkTopologyDesignService` | Returns error: "Network topology design is disabled" |
| `EnablePredictiveScaling` | `IPredictiveScalingEngine` | Returns error: "Predictive scaling is disabled" |
| `EnableComplianceEnhancement` | `IComplianceAwareTemplateEnhancer` | Falls back to basic template generation without compliance controls |

**Implementation:**
```csharp
// In constructor
_networkDesignService = _options.EnableNetworkDesign ? networkDesignService : null;
_scalingEngine = _options.EnablePredictiveScaling ? scalingEngine : null;
_complianceEnhancer = _options.EnableComplianceEnhancement ? complianceEnhancer : null;

// In plugin methods
if (_networkDesignService == null)
{
    return "❌ Network topology design is disabled. Enable 'EnableNetworkDesign' in configuration.";
}
```

### 5. Plugin Updates

Updated `InfrastructurePlugin.cs` to:
- Accept nullable service parameters
- Check for null before calling service methods
- Return user-friendly error messages when features are disabled

### 6. Documentation Updates

Updated `README.md` to:
- Document all configuration options
- Explain what each setting controls
- Provide default values
- Note behavior when features are disabled

## Configuration Options

### Complete Configuration Example

```json
{
  "InfrastructureAgent": {
    "Temperature": 0.4,                          // AI creativity (0.0-2.0)
    "MaxTokens": 8000,                           // Max response tokens
    "DefaultRegion": "eastus",                   // Default Azure region
    "EnableComplianceEnhancement": true,         // Auto-inject compliance controls
    "DefaultComplianceFramework": "FedRAMPHigh", // Default framework
    "EnablePredictiveScaling": true,             // AI scaling predictions
    "EnableNetworkDesign": true                  // Network topology design
  }
}
```

### Feature Toggles

To disable specific features, set the corresponding flag to `false`:

```json
{
  "InfrastructureAgent": {
    "EnablePredictiveScaling": false,  // Disables all scaling prediction functions
    "EnableNetworkDesign": false,      // Disables network topology and subnet calculation
    "EnableComplianceEnhancement": false  // Generates basic templates without compliance
  }
}
```

## Benefits

1. **Flexibility**: Operators can tune AI behavior without code changes
2. **Performance**: Disable unused features to reduce overhead
3. **Compliance**: Set default framework organization-wide
4. **Cost Control**: Adjust token limits based on budget
5. **Regional Optimization**: Set default region for faster provisioning

## Migration Notes

### Breaking Changes

The `AddInfrastructureAgent()` extension method now requires an `IConfiguration` parameter:

**Before:**
```csharp
builder.Services.AddInfrastructureAgent();
```

**After:**
```csharp
builder.Services.AddInfrastructureAgent(builder.Configuration);
```

### Backward Compatibility

All configuration options have sensible defaults, so existing deployments without the `InfrastructureAgent` section in `appsettings.json` will continue to work with default values.

## Testing

To verify the configuration is working:

1. **Check Temperature**: Monitor logs for the temperature value being used
2. **Disable Features**: Set `EnableNetworkDesign: false` and verify network functions return error messages
3. **Change MaxTokens**: Set to a low value (e.g., 100) and verify responses are truncated
4. **DefaultRegion**: Request resource creation without specifying region, verify default is used

## Future Enhancements

Potential future improvements:

1. **Per-Resource Defaults**: Different defaults for AKS vs App Service
2. **Environment-Specific Settings**: Different configs for dev/staging/prod
3. **Dynamic Reconfiguration**: Hot-reload config changes without restart
4. **Metrics**: Track usage of each feature for optimization
5. **Compliance Profiles**: Predefined sets of controls for common frameworks

## References

- Configuration class: `src/Platform.Engineering.Copilot.Infrastructure.Agent/Configuration/InfrastructureAgentOptions.cs`
- Agent implementation: `src/Platform.Engineering.Copilot.Infrastructure.Agent/Services/Agents/InfrastructureAgent.cs`
- Plugin updates: `src/Platform.Engineering.Copilot.Infrastructure.Agent/Plugins/InfrastructurePlugin.cs`
- DI registration: `src/Platform.Engineering.Copilot.Infrastructure.Agent/Extensions/ServiceCollectionExtensions.cs`
- Documentation: `src/Platform.Engineering.Copilot.Infrastructure.Agent/README.md`
