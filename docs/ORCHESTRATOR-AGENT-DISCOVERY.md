# How the Orchestrator Discovers Enabled Agents

## Overview

The orchestrator automatically knows which agents are enabled through **dependency injection (DI)**. It doesn't need to read configuration directly - the enabled agents are injected into its constructor.

## The Complete Flow

### 1. Configuration (appsettings.json)

```json
{
  "AgentConfiguration": {
    "EnabledAgents": {
      "Infrastructure": true,
      "CostManagement": true,
      "Environment": false,
      "Discovery": true,
      "ServiceCreation": true,
      "Compliance": true,
      "Security": true,
      "Document": false
    }
  }
}
```

### 2. Startup Registration (Program.cs)

At startup, `Program.cs` reads the configuration and conditionally registers agents:

```csharp
// Load configuration
var agentConfig = builder.Configuration
    .GetSection(AgentConfiguration.SectionName)
    .Get<AgentConfiguration>() ?? new AgentConfiguration();

// Conditionally register ONLY enabled agents
if (agentConfig.IsAgentEnabled("Infrastructure"))
{
    builder.Services.AddInfrastructureCore();  // ← Registers InfrastructureAgent as ISpecializedAgent
    logger.LogInformation("✅ Infrastructure agent enabled");
}

if (agentConfig.IsAgentEnabled("Environment"))
{
    builder.Services.AddEnvironmentCore();  // ← NOT CALLED (disabled in config)
    logger.LogInformation("✅ Environment agent enabled");
}

if (agentConfig.IsAgentEnabled("Discovery"))
{
    builder.Services.AddDiscoveryCore();  // ← Registers DiscoveryAgent as ISpecializedAgent
    logger.LogInformation("✅ Discovery agent enabled");
}
```

### 3. Domain Extension Methods

Each domain's `ServiceCollectionExtensions.cs` registers its agent as **both** the concrete type and `ISpecializedAgent`:

```csharp
// Platform.Engineering.Copilot.Infrastructure.Core/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
{
    // Register concrete type (for direct injection if needed)
    services.AddScoped<InfrastructureAgent>();
    
    // Register as ISpecializedAgent (for orchestrator to discover)
    services.AddScoped<ISpecializedAgent, InfrastructureAgent>(
        sp => sp.GetRequiredService<InfrastructureAgent>()
    );
    
    services.AddScoped<InfrastructurePlugin>();
    // ... other services
    
    return services;
}
```

**Key Pattern**: The lambda `sp => sp.GetRequiredService<InfrastructureAgent>()` ensures both registrations return the **same instance** (singleton per scope).

### 4. Orchestrator Constructor Injection

The `OrchestratorAgent` receives all registered `ISpecializedAgent` instances:

```csharp
// Platform.Engineering.Copilot.Core/Services/Agents/OrchestratorAgent.cs
public class OrchestratorAgent
{
    private readonly Dictionary<AgentType, ISpecializedAgent> _agents;
    
    public OrchestratorAgent(
        ISemanticKernelService semanticKernelService,
        IEnumerable<ISpecializedAgent> agents,  // ← DI injects ALL registered ISpecializedAgent instances
        SharedMemory sharedMemory,
        ExecutionPlanValidator planValidator,
        ExecutionPlanCache planCache,
        ILogger<OrchestratorAgent> logger)
    {
        // Build agent registry from injected agents
        _agents = agents.ToDictionary(a => a.AgentType, a => a);
        
        _logger.LogInformation("🎼 OrchestratorAgent initialized with {AgentCount} specialized agents",
            _agents.Count);
    }
}
```

### 5. Result

Based on the example configuration above:

- **Orchestrator receives**: `InfrastructureAgent`, `CostManagementAgent`, `DiscoveryAgent`, `ServiceCreationAgent`, `ComplianceAgent`, `SecurityAgent` (6 agents)
- **Orchestrator does NOT receive**: `EnvironmentAgent`, `DocumentAgent` (disabled in config)

## Dependency Injection Magic

The .NET DI container automatically:

1. ✅ **Collects all registrations** of `ISpecializedAgent`
2. ✅ **Injects them as `IEnumerable<ISpecializedAgent>`** into the orchestrator
3. ✅ **Filters by configuration** (disabled agents are never registered)
4. ✅ **Resolves dependencies** for each agent

## Why This Works

- **No runtime configuration checks**: The orchestrator doesn't need to read `appsettings.json` or check `AgentConfiguration`
- **Compile-time safety**: If an agent isn't registered, it simply won't appear in the collection
- **Zero overhead**: Disabled agents are never instantiated or allocated memory
- **Testable**: In unit tests, you can inject specific agents without loading full configuration

## Adding a New Agent

To add a new agent to the system:

1. **Create the agent class** implementing `ISpecializedAgent`
2. **Create the extension method** that registers it:
   ```csharp
   public static IServiceCollection AddMyNewCore(this IServiceCollection services)
   {
       services.AddScoped<MyNewAgent>();
       services.AddScoped<ISpecializedAgent, MyNewAgent>(sp => sp.GetRequiredService<MyNewAgent>());
       return services;
   }
   ```
3. **Add to configuration** in `appsettings.json`:
   ```json
   "EnabledAgents": {
       "MyNew": true
   }
   ```
4. **Add conditional registration** in `Program.cs`:
   ```csharp
   if (agentConfig.IsAgentEnabled("MyNew"))
   {
       builder.Services.AddMyNewCore();
       logger.LogInformation("✅ MyNew agent enabled");
   }
   ```

The orchestrator will automatically discover it - **no changes needed to `OrchestratorAgent.cs`**!

## Summary

```
Configuration → Conditional Registration → DI Container → Orchestrator
============    =======================    ============    ============
appsettings     if (enabled)               Collection of   IEnumerable<ISpecializedAgent>
.json           AddInfrastructureCore()    ISpecializedAgent   ↓
                                          instances       Build _agents dictionary
```

The orchestrator is **configuration-agnostic** - it simply uses whatever agents the DI container provides. This is a clean separation of concerns:
- **Configuration layer**: Decides which agents to enable
- **DI layer**: Manages registration and lifecycle
- **Orchestrator**: Uses whatever agents are available

No coupling between orchestrator and configuration! 🎉
