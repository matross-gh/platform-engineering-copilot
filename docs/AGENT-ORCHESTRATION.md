# Agent Orchestration & Configuration

> **Quick Reference:** [Agent Configuration](#agent-configuration) | [How Discovery Works](#how-agent-discovery-works) | [Available Agents](#available-agents)

---

## Table of Contents

1. [Overview](#overview)
2. [Agent Configuration](#agent-configuration)
3. [How Agent Discovery Works](#how-agent-discovery-works)
4. [Available Agents](#available-agents)
5. [Usage Examples](#usage-examples)
6. [Troubleshooting](#troubleshooting)

---

## Overview

The Platform Engineering Copilot supports selective agent loading through configuration. This allows you to enable or disable specific agents based on your needs, reducing resource usage and tailoring the platform to your requirements.

### Key Concepts

**Automatic Discovery:**
- The orchestrator automatically knows which agents are enabled through **dependency injection (DI)**
- No configuration checking needed in agent code
- Enabled agents are injected into the orchestrator's constructor

**Configuration-Driven:**
- Agents are enabled/disabled in `appsettings.json`
- Only enabled agents are registered in the DI container
- Disabled agents consume zero resources

---

## Agent Configuration

### Location

Agent configuration is stored in `appsettings.json` under the `AgentConfiguration` section, with each agent having its own `Enabled` property.

### Format

```json
{
  "AgentConfiguration": {
    "InfrastructureAgent": {
      "Enabled": true,
      "Temperature": 0.4,
      "MaxTokens": 8000,
      "DefaultRegion": "usgovvirginia",
      "EnableComplianceEnhancement": true
    },
    "ComplianceAgent": {
      "Enabled": true,
      "Temperature": 0.2,
      "MaxTokens": 6000,
      "EnableAutomatedRemediation": true,
      "DefaultFramework": "NIST80053"
    },
    "CostManagementAgent": {
      "Enabled": true,
      "Temperature": 0.3,
      "MaxTokens": 4000,
      "DefaultCurrency": "USD"
    },
    "DiscoveryAgent": {
      "Enabled": true,
      "Temperature": 0.3,
      "MaxTokens": 4000,
      "EnableHealthMonitoring": true
    },
    "EnvironmentAgent": {
      "Enabled": true
    },
    "SecurityAgent": {
      "Enabled": true
    },
    "KnowledgeBaseAgent": {
      "Enabled": true
    }
  }
}
```

### Available Agents

| Agent Name | Description | Default | Use Case |
|------------|-------------|---------|----------|
| **Infrastructure** | Azure infrastructure provisioning and management | ✅ Enabled | Template generation, resource provisioning |
| **CostManagement** | Cost analysis, optimization, and budget tracking | ✅ Enabled | Cost analysis, budget tracking |
| **Environment** | Environment lifecycle management and cloning | ✅ Enabled | Dev/test environment management |
| **Discovery** | Resource discovery, inventory, and health monitoring | ✅ Enabled | Resource queries, inventory |
| **Compliance** | ATO compliance scanning, NIST controls, and documentation | ✅ Enabled | Compliance checks, RMF/STIG, ATO docs |

**Note:** Security and Document capabilities are provided via plugins, not standalone agents. ServiceCreation is defined in AgentType enum but not yet implemented.

### Environment Variables

Override configuration via environment variables (use double underscores for nesting):

```bash
export AgentConfiguration__InfrastructureAgent__Enabled=true
export AgentConfiguration__CostManagementAgent__Enabled=false
export AgentConfiguration__InfrastructureAgent__Temperature=0.5
export AgentConfiguration__ComplianceAgent__DefaultFramework=FedRAMPHigh
export AgentConfiguration__DiscoveryAgent__EnableHealthMonitoring=false
```

---

## How Agent Discovery Works

### The Complete Flow

#### 1. Configuration (appsettings.json)

```json
{
  "AgentConfiguration": {
    "InfrastructureAgent": {
      "Enabled": true,
      "Temperature": 0.4,
      "MaxTokens": 8000
    },
    "CostManagementAgent": {
      "Enabled": true
    },
    "EnvironmentAgent": {
      "Enabled": false
    },
    "DiscoveryAgent": {
      "Enabled": true,
      "Temperature": 0.3,
      "EnableHealthMonitoring": true
    }
  }
}
```

#### 2. Startup Registration (Program.cs)

At startup, `Program.cs` reads the configuration and conditionally registers agents:

```csharp
// Configure agent options from nested sections
builder.Services.Configure<InfrastructureAgentOptions>(
    builder.Configuration.GetSection("AgentConfiguration:InfrastructureAgent"));
builder.Services.Configure<DiscoveryAgentOptions>(
    builder.Configuration.GetSection("AgentConfiguration:DiscoveryAgent"));

// Load configuration
var agentConfig = builder.Configuration
    .GetSection(AgentConfiguration.SectionName)
    .Get<AgentConfiguration>() ?? new AgentConfiguration();

// Conditionally register ONLY enabled agents
if (agentConfig.IsAgentEnabled("Infrastructure"))
{
    builder.Services.AddInfrastructureAgent();  
    logger.LogInformation("✅ Infrastructure agent enabled");
}

if (agentConfig.IsAgentEnabled("Environment"))
{
    builder.Services.AddEnvironmentAgent();  // ← NOT CALLED (disabled)
    logger.LogInformation("✅ Environment agent enabled");
}

if (agentConfig.IsAgentEnabled("Discovery"))
{
    builder.Services.AddDiscoveryAgent();  
    logger.LogInformation("✅ Discovery agent enabled");
}
```

#### 3. Domain Extension Methods

Each domain's `ServiceCollectionExtensions.cs` registers its agent as **both** the concrete type and `ISpecializedAgent`:

```csharp
// Platform.Engineering.Copilot.Infrastructure.Core/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructureAgent(this IServiceCollection services)
{
    // Register concrete type
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

**Key Pattern:** The lambda `sp => sp.GetRequiredService<InfrastructureAgent>()` ensures both registrations return the **same instance** (singleton per scope).

#### 4. Orchestrator Constructor Injection

The `OrchestratorAgent` receives all registered `ISpecializedAgent` instances:

```csharp
public class OrchestratorAgent
{
    private readonly Dictionary<AgentType, ISpecializedAgent> _agents;
    
    public OrchestratorAgent(
        ISemanticKernelService semanticKernelService,
        IEnumerable<ISpecializedAgent> agents,  // ← DI injects ALL registered agents
        SharedMemory sharedMemory,
        ExecutionPlanValidator planValidator,
        ExecutionPlanCache planCache,
        ILogger<OrchestratorAgent> logger)
    {
        // Build dictionary from injected agents
        _agents = agents.ToDictionary(a => a.Type);
        
        _logger.LogInformation(
            "Orchestrator initialized with {Count} agents: {AgentTypes}",
            _agents.Count,
            string.Join(", ", _agents.Keys)
        );
    }
}
```

**Result:** The orchestrator automatically has access to only the enabled agents. No configuration checking needed!

### Flow Diagram

```
appsettings.json              Program.cs                    OrchestratorAgent
===============              ==========                    =================
{                            
  "AgentConfiguration": {    if (IsEnabled("Infrastructure"))
    "InfrastructureAgent": {   ├─> AddInfrastructureAgent()  
      "Enabled": true          │   ├─> Register InfrastructureAgent
    },                         │   └─> as ISpecializedAgent    ──┐
    "CostManagementAgent": {   │                                 │
      "Enabled": true  ────┼───└─> Register services             │
    },                         │                                     │
    "EnvironmentAgent": {      if (IsEnabled("CostManagement"))     │
      "Enabled": false         │ ├─> AddCostManagementAgent()        │
    }                          │ └─> Register as ISpecializedAgent ──> IEnumerable<ISpecializedAgent>
  }                            │                                  │    (Auto-injected into constructor)
}                              if (IsEnabled("Environment"))        │    
                                 └─> [SKIPPED - disabled]           │    Only contains:
                                                                     │    • InfrastructureAgent
                               if (IsEnabled("Discovery"))          │    • CostManagementAgent  
                                 └─> AddDiscoveryAgent()             │    • DiscoveryAgent
                                     └─> Register as ISpecializedAgent ──┘
```

**Key Point:** The orchestrator never sees `EnvironmentAgent` because it was never registered in the DI container.

---

## Usage Examples

### Minimal Configuration (Core Infrastructure Only)

For basic infrastructure operations:

```json
{
  "AgentConfiguration": {
    "InfrastructureAgent": {
      "Enabled": true
    },
    "CostManagementAgent": {
      "Enabled": false
    },
    "EnvironmentAgent": {
      "Enabled": false
    },
    "DiscoveryAgent": {
      "Enabled": true
    },
    "ServiceCreationAgent": {
      "Enabled": false
    },
    "ComplianceAgent": {
      "Enabled": false
    },
    "SecurityAgent": {
      "Enabled": false
    },
    "DocumentAgent": {
      "Enabled": false
    }
  }
}
```

**Result:** Only Infrastructure and Discovery agents are available. Memory usage reduced by ~60%.

### Development Configuration

For local development with all features:

```json
{
  "AgentConfiguration": {
    "InfrastructureAgent": {
      "Enabled": true
    },
    "CostManagementAgent": {
      "Enabled": true
    },
    "EnvironmentAgent": {
      "Enabled": true
    },
    "DiscoveryAgent": {
      "Enabled": true
    },
    "ServiceCreationAgent": {
      "Enabled": true
    },
    "ComplianceAgent": {
      "Enabled": true
    },
    "SecurityAgent": {
      "Enabled": true
    },
    "DocumentAgent": {
      "Enabled": true
    }
  }
}
```

**Result:** All agents enabled for full feature testing.

### Production Configuration (IL5/IL6)

For production DoD environments with compliance requirements:

```json
{
  "AgentConfiguration": {
    "InfrastructureAgent": {
      "Enabled": true
    },
    "CostManagementAgent": {
      "Enabled": true
    },
    "EnvironmentAgent": {
      "Enabled": true
    },
    "DiscoveryAgent": {
      "Enabled": true
    },
    "ServiceCreationAgent": {
      "Enabled": true
    },
    "ComplianceAgent": {
      "Enabled": true  // ← Required for IL5/IL6
    },
    "SecurityAgent": {
      "Enabled": true   // ← Required for IL5/IL6
    },
    "DocumentAgent": {
      "Enabled": false
    }
  }
}
```

**Result:** All agents except Document enabled. Compliance and Security agents provide RMF/STIG support.

### Cost-Focused Configuration

For cost management and analysis only:

```json
{
  "AgentConfiguration": {
    "InfrastructureAgent": {
      "Enabled": false
    },
    "CostManagementAgent": {
      "Enabled": true
    },
    "EnvironmentAgent": {
      "Enabled": false
    },
    "DiscoveryAgent": {
      "Enabled": true
    },
    "ServiceCreationAgent": {
      "Enabled": false
    },
    "ComplianceAgent": {
      "Enabled": false
    },
    "SecurityAgent": {
      "Enabled": false
    },
    "DocumentAgent": {
      "Enabled": false
    }
  }
}
```

**Result:** Only Cost and Discovery agents. Ideal for cost analysis workloads.

### Compliance-Only Configuration

For compliance scanning and reporting:

```json
{
  "AgentConfiguration": {
    "InfrastructureAgent": {
      "Enabled": false
    },
    "CostManagementAgent": {
      "Enabled": false
    },
    "EnvironmentAgent": {
      "Enabled": false
    },
    "DiscoveryAgent": {
      "Enabled": true
    },
    "ServiceCreationAgent": {
      "Enabled": false
    },
    "ComplianceAgent": {
      "Enabled": true
    },
    "SecurityAgent": {
      "Enabled": true
    },
    "DocumentAgent": {
      "Enabled": false
    }
  }
}
```

**Result:** Discovery, Compliance, and Security agents. Ideal for compliance audits.

---

## Verification

### Check Enabled Agents at Startup

Look for log messages:

```
[Info] ✅ Infrastructure agent enabled
[Info] ✅ CostManagement agent enabled
[Info] ✅ Discovery agent enabled
[Info] Orchestrator initialized with 3 agents: Infrastructure, CostManagement, Discovery
```

### Query Available Agents

```csharp
// In OrchestratorAgent
public List<AgentType> GetAvailableAgents()
{
    return _agents.Keys.ToList();
}
```

### Runtime Agent Check

```csharp
public bool IsAgentAvailable(AgentType type)
{
    return _agents.ContainsKey(type);
}
```

---

## Troubleshooting

### Issue: Agent not appearing despite being enabled

**Cause:** Extension method not called in `Program.cs`

**Solution:** Verify the `if (agentConfig.IsAgentEnabled(...))` block calls the correct extension method:
```csharp
if (agentConfig.IsAgentEnabled("Infrastructure"))
{
    builder.Services.AddInfrastructureAgent();  // ← This must be present
}
```

### Issue: Orchestrator has zero agents

**Cause:** All agents disabled or configuration not loading

**Solution:**
1. Check `appsettings.json` exists and is valid JSON
2. Verify at least one agent is enabled
3. Check environment variables aren't overriding config
4. Review startup logs for configuration errors

### Issue: Agent appears enabled but throws errors

**Cause:** Agent's domain services not registered

**Solution:** Verify the `Add*Agent()` extension method registers all required services:
```csharp
public static IServiceCollection AddInfrastructureAgent(this IServiceCollection services)
{
    services.AddScoped<InfrastructureAgent>();
    services.AddScoped<ISpecializedAgent, InfrastructureAgent>(...);
    services.AddScoped<InfrastructurePlugin>();
    
    // Add all required services
    services.AddScoped<NetworkTopologyDesignService>();
    services.AddScoped<PredictiveScalingEngine>();
    // ... etc
    
    return services;
}
```

### Issue: Wrong agent responds to query

**Cause:** Agent selection logic routing to incorrect agent

**Solution:** Review orchestrator's agent selection logic:
```csharp
// OrchestratorAgent.cs
private AgentType SelectAgent(string userQuery)
{
    // Check available agents
    if (userQuery.Contains("cost") && _agents.ContainsKey(AgentType.CostManagement))
        return AgentType.CostManagement;
    
    // Fallback logic
    return AgentType.Infrastructure;
}
```

### Issue: Memory usage still high with disabled agents

**Cause:** Agent's domain services still registered

**Solution:** Ensure the entire `if` block is skipped when disabled:
```csharp
// CORRECT:
if (agentConfig.IsAgentEnabled("Infrastructure"))
{
    builder.Services.AddInfrastructureAgent();  // Adds agent AND services
}

// INCORRECT:
builder.Services.AddInfrastructureCore();  // Always adds services
if (agentConfig.IsAgentEnabled("Infrastructure"))
{
    builder.Services.AddScoped<InfrastructureAgent>();  // Only agent conditional
}
```

---

## Best Practices

### Development
✅ Enable all agents for full feature testing  
✅ Use `appsettings.Development.json` for dev-specific configuration  
✅ Test with minimal agent sets to verify degradation gracefully

### Production
✅ Only enable agents required for your use case  
✅ Document why each agent is enabled/disabled  
✅ Use environment variables for environment-specific overrides  
✅ Monitor agent usage and disable unused agents

### Compliance (IL5/IL6)
✅ Always enable Compliance and Security agents  
✅ Document agent configuration in SSP  
✅ Audit log which agents are enabled  
✅ Restrict agent enabling to authorized personnel

---

## Architecture Benefits

### Resource Efficiency
- **Memory:** Only enabled agents consume memory
- **Startup:** Faster startup with fewer agents
- **CPU:** Less overhead from unused agents

### Flexibility
- **Per-Environment:** Different agents per environment
- **Per-Customer:** Tailor to customer needs
- **Feature Flags:** Easy feature toggling

### Security
- **Attack Surface:** Disabled agents can't be exploited
- **Compliance:** Only load IL-approved agents
- **Audit:** Clear record of enabled capabilities

---

**Document Version:** 2.0  
**Last Updated:** November 2025  
**Status:** ✅ Consolidated and Production Ready

**Supersedes:** AGENT-SELECTION.md, ORCHESTRATOR-AGENT-DISCOVERY.md
