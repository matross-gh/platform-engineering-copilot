# Agent Selection Configuration

## Overview

The Platform Engineering Copilot now supports selective agent loading through configuration. This allows you to enable or disable specific agents based on your needs, reducing resource usage and tailoring the platform to your requirements.

## How It Works

The agent selection system uses dependency injection to control which agents are available:

1. **Configuration Loading**: At startup, `Program.cs` reads the `AgentConfiguration` section from `appsettings.json` (or environment variables)
2. **Conditional Registration**: Only enabled agents have their domain services registered in the DI container via `Add*Core()` extension methods
3. **Orchestrator Discovery**: The `OrchestratorAgent` receives `IEnumerable<ISpecializedAgent>` through its constructor
4. **Automatic Filtering**: The orchestrator only sees agents that were registered (enabled in configuration)

**Key Point**: The orchestrator doesn't need to check configuration - it automatically receives only the enabled agents through DI. When an agent is disabled, its `Add*Core()` method is never called, so it's not registered in the container.

### Flow Diagram

```
appsettings.json              Program.cs                    OrchestratorAgent
===============              ==========                    =================
{                            
  "EnabledAgents": {         if (IsEnabled("Infrastructure"))
    "Infrastructure": true     ├─> AddInfrastructureCore()  
    "CostManagement": true     │   ├─> Register InfrastructureAgent
    "Environment": false   ────┼───│   └─> as ISpecializedAgent    ──┐
    "Discovery": true          │   │                                 │
  }                            │   └─> Register services             │
}                              │                                     │
                               if (IsEnabled("CostManagement"))     │
                                 ├─> AddCostManagementCore()        │
                                 │   └─> Register CostManagementAgent  ──> IEnumerable<ISpecializedAgent>
                                 │       as ISpecializedAgent        │    (Auto-injected into constructor)
                               if (IsEnabled("Environment"))        │    
                                 └─> [SKIPPED - disabled]           │    Only contains:
                                                                     │    • InfrastructureAgent
                               if (IsEnabled("Discovery"))          │    • CostManagementAgent  
                                 └─> AddDiscoveryCore()             │    • DiscoveryAgent
                                     └─> Register DiscoveryAgent ───┘
                                         as ISpecializedAgent
```

The orchestrator never sees `EnvironmentAgent` because it was never registered in the DI container.

## Configuration

### Location
Agent configuration is stored in `appsettings.json` under the `AgentConfiguration` section.

### Format

```json
{
  "AgentConfiguration": {
    "EnabledAgents": {
      "Infrastructure": true,
      "CostManagement": true,
      "Environment": true,
      "Discovery": true,
      "ServiceCreation": true,
      "Compliance": true,
      "Security": true,
      "Document": false
    }
  }
}
```

### Available Agents

| Agent Name | Description | Default |
|------------|-------------|---------|
| **Infrastructure** | Azure infrastructure provisioning and management | ✅ Enabled |
| **CostManagement** | Cost analysis, optimization, and budget tracking | ✅ Enabled |
| **Environment** | Environment lifecycle management and cloning | ✅ Enabled |
| **Discovery** | Resource discovery, inventory, and health monitoring | ✅ Enabled |
| **ServiceCreation** | New service/mission ServiceCreation workflows | ✅ Enabled |
| **Compliance** | ATO compliance scanning and NIST controls | ✅ Enabled |
| **Security** | Security operations, SIEM setup, incident response | ✅ Enabled |
| **Document** | Document processing and architecture analysis | ❌ Disabled |

## Usage Examples

### Minimal Configuration (Core Infrastructure Only)

For basic infrastructure operations:

```json
{
  "AgentConfiguration": {
    "EnabledAgents": {
      "Infrastructure": true,
      "CostManagement": false,
      "Environment": false,
      "Discovery": true,
      "ServiceCreation": false,
      "Compliance": false,
      "Security": false,
      "Document": false
    }
  }
}
```

### Compliance-Focused Configuration

For ATO and compliance workflows:

```json
{
  "AgentConfiguration": {
    "EnabledAgents": {
      "Infrastructure": true,
      "CostManagement": false,
      "Environment": false,
      "Discovery": true,
      "ServiceCreation": false,
      "Compliance": true,
      "Security": true,
      "Document": true
    }
  }
}
```

### Development/Testing Configuration

Enable all agents for full functionality testing:

```json
{
  "AgentConfiguration": {
    "EnabledAgents": {
      "Infrastructure": true,
      "CostManagement": true,
      "Environment": true,
      "Discovery": true,
      "ServiceCreation": true,
      "Compliance": true,
      "Security": true,
      "Document": true
    }
  }
}
```

## Environment-Specific Configuration

You can override agent settings per environment:

### Production (`appsettings.Production.json`)
```json
{
  "AgentConfiguration": {
    "EnabledAgents": {
      "Infrastructure": true,
      "CostManagement": true,
      "Environment": true,
      "Discovery": true,
      "ServiceCreation": true,
      "Compliance": true,
      "Security": true,
      "Document": false
    }
  }
}
```

### Development (`appsettings.Development.json`)
```json
{
  "AgentConfiguration": {
    "EnabledAgents": {
      "Infrastructure": true,
      "CostManagement": false,
      "Environment": false,
      "Discovery": true,
      "ServiceCreation": false,
      "Compliance": false,
      "Security": false,
      "Document": false
    }
  }
}
```

## Startup Logs

When the application starts, you'll see which agents are loaded:

```
[INFO] 🔧 Loading agents based on configuration...
[INFO] ✅ Compliance agent enabled
[INFO] ✅ Infrastructure agent enabled
[INFO] ✅ CostManagement agent enabled
[INFO] ✅ Environment agent enabled
[INFO] ✅ Discovery agent enabled
[INFO] ✅ ServiceCreation agent enabled
[INFO] ✅ Security agent enabled
[INFO] 🚀 Loaded 7 of 8 available agents
```

## Environment Variables Override

You can also override agent configuration using environment variables:

```bash
export AgentConfiguration__EnabledAgents__Infrastructure=false
export AgentConfiguration__EnabledAgents__CostManagement=true
```

## Docker Configuration

When running in Docker, you can pass configuration through environment variables:

```bash
docker run -e "AgentConfiguration__EnabledAgents__Infrastructure=true" \
           -e "AgentConfiguration__EnabledAgents__Compliance=true" \
           platform-engineering-copilot-mcp
```

Or through docker-compose.yml:

```yaml
services:
  mcp-server:
    environment:
      - AgentConfiguration__EnabledAgents__Infrastructure=true
      - AgentConfiguration__EnabledAgents__Compliance=true
      - AgentConfiguration__EnabledAgents__Security=true
```

## Kubernetes Configuration

In Kubernetes, use ConfigMaps or environment variables:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: agent-config
data:
  appsettings.json: |
    {
      "AgentConfiguration": {
        "EnabledAgents": {
          "Infrastructure": true,
          "Compliance": true,
          "Security": true
        }
      }
    }
```

## Benefits

1. **Resource Optimization**: Only load agents you need, reducing memory and CPU usage
2. **Security**: Disable agents that handle sensitive operations in certain environments
3. **Customization**: Tailor the platform to specific use cases (dev, compliance, ops, etc.)
4. **Testing**: Easily enable/disable agents during development and testing
5. **Multi-Tenancy**: Different configurations for different teams or environments

## Best Practices

1. **Start Minimal**: Enable only the agents you need
2. **Review Regularly**: Audit which agents are enabled in each environment
3. **Document Dependencies**: Some agents may depend on others (e.g., Environment depends on Infrastructure)
4. **Use Environment Files**: Maintain separate configurations for dev, staging, production
5. **Monitor Impact**: Check logs to verify expected agents are loaded

## Troubleshooting

### Agent Not Responding

If an agent doesn't respond to queries:
1. Check the startup logs to verify the agent was loaded
2. Verify the agent is enabled in `appsettings.json`
3. Check for any dependency errors in the logs

### Missing Functionality

If certain features are unavailable:
1. Verify the responsible agent is enabled
2. Check if the agent has dependencies on other agents
3. Review agent-specific configuration settings

## Future Enhancements

Planned features for agent selection:
- ✅ Configuration-based selection (implemented)
- 🔄 Runtime agent loading/unloading
- 🔄 Admin UI for agent management
- 🔄 Agent dependency validation
- 🔄 Per-user agent permissions
- 🔄 Agent usage analytics and metrics
