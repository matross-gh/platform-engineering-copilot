# Platform Engineering Copilot - Architecture

**Version:** 3.0 (Microsoft Agent Framework Architecture)  
**Last Updated:** January 2026

---

## Overview

The Platform Engineering Copilot is an AI-powered infrastructure and compliance platform built on .NET 9.0. The system uses the **Microsoft Agent Framework** architecture pattern with specialized AI agents coordinated through a Model Context Protocol (MCP) server.

### Key Characteristics

- **Microsoft Agent Framework Pattern**: All agents extend `BaseAgent`, all tools extend `BaseTool`
- **MCP Server**: Dual-mode operation (HTTP:5100 + stdio for AI clients)
- **Multi-Agent Orchestration**: 6 specialized agents with shared context
- **Azure Government**: Primary target with NIST 800-53 compliance

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENTS                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────┐ │
│  │ Web Chat     │  │ Admin UI     │  │ AI Clients             │ │
│  │ (5001)       │  │ (5003)       │  │ • GitHub Copilot       │ │
│  │              │  │              │  │ • Claude Desktop       │ │
│  └──────┬───────┘  └──────┬───────┘  └────────┬───────────────┘ │
│         │ HTTP            │ HTTP              │ stdio            │
└─────────┴─────────────────┴───────────────────┴─────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    MCP SERVER (5100)                             │
│                Platform.Engineering.Copilot.Mcp                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              PlatformAgentGroupChat                         │ │
│  │  ├─ PlatformSelectionStrategy (fast-path routing)          │ │
│  │  ├─ PlatformTerminationStrategy                             │ │
│  │  └─ BaseAgent instances                                     │ │
│  └────────────────────────────────────────────────────────────┘ │
│                              ↓                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                 SPECIALIZED AGENTS                          │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │ │
│  │  │Infrastructure│ │ Compliance │ │    Cost     │           │ │
│  │  │   Agent     │ │   Agent    │ │   Agent     │           │ │
│  │  └─────────────┘ └─────────────┘ └─────────────┘           │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │ │
│  │  │  Discovery  │ │ Environment│ │  Security   │           │ │
│  │  │   Agent     │ │   Agent    │ │   Agent     │           │ │
│  │  └─────────────┘ └─────────────┘ └─────────────┘           │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    AZURE SERVICES                                │
│  • Azure Resource Manager  • Defender for Cloud                 │
│  • Cost Management API     • Azure Policy                       │
│  • Resource Graph          • Key Vault                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## Microsoft Agent Framework

### Core Abstractions

The Microsoft Agent Framework framework provides two base classes that all agents and tools must extend:

```
┌──────────────────────────────────────────────────────────────────┐
│                       Microsoft Agent Framework                  │
├──────────────────────────────────────────────────────────────────┤
│  BaseAgent (abstract)                                             │
│    ├─ AgentId: string                                             │
│    ├─ AgentName: string                                           │
│    ├─ Description: string                                         │
│    ├─ RegisteredTools: List<BaseTool>                             │
│    ├─ RegisterTool(tool) - adds tool to agent                     │
│    ├─ ProcessAsync(context) → AgentResponse                       │
│    └─ GetSystemPrompt() → string (tool selection guidance)        │
├──────────────────────────────────────────────────────────────────┤
│  BaseTool (abstract)                                              │
│    ├─ Name: string (e.g., "run_compliance_assessment")            │
│    ├─ Description: string (shown to LLM for selection)            │
│    ├─ Parameters: List<ToolParameter>                             │
│    ├─ ExecuteAsync(arguments) → string (JSON result)              │
│    └─ AsAITool() → AITool (for LLM function calling)              │
└──────────────────────────────────────────────────────────────────┘
```

### BaseAgent Pattern

```csharp
public class ComplianceAgent : BaseAgent
{
    public override string AgentId => "compliance";
    public override string AgentName => "Compliance Agent";
    public override string Description => "NIST 800-53 compliance scanning and remediation";

    public ComplianceAgent(
        IChatClient chatClient,
        ILogger<ComplianceAgent> logger,
        ComplianceAssessmentTool assessmentTool,
        BatchRemediationTool remediationTool,
        DefenderForCloudTool dfcTool,
        // ... other tools
    ) : base(chatClient, logger)
    {
        RegisterTool(assessmentTool);
        RegisterTool(remediationTool);
        RegisterTool(dfcTool);
    }

    protected override string GetSystemPrompt() => @"
        You are the Compliance Agent for Azure Government...
        
        ## Tool Selection
        - run_compliance_assessment: NIST 800-53 scanning
        - batch_remediation: Fix multiple findings
        - get_defender_findings: DFC secure score and findings
    ";
}
```

### BaseTool Pattern

```csharp
public class ComplianceAssessmentTool : BaseTool
{
    public override string Name => "run_compliance_assessment";
    
    public override string Description =>
        "Run NIST 800-53 compliance assessment against Azure subscription. " +
        "Use when user asks: 'check compliance', 'run assessment', 'NIST scan'";

    public ComplianceAssessmentTool(
        ILogger<ComplianceAssessmentTool> logger,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _complianceEngine = complianceEngine;
        
        Parameters.Add(new ToolParameter("subscription_id", "Azure subscription ID", false));
        Parameters.Add(new ToolParameter("resource_group", "Scope to resource group", false));
        Parameters.Add(new ToolParameter("control_families", "Filter: AC,AU,SC", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var subscriptionId = GetRequiredString(arguments, "subscription_id");
        var result = await _complianceEngine.RunAssessmentAsync(subscriptionId);
        return ToJson(new { success = true, assessment = result });
    }
}
```

---

## Project Structure

```
src/
├── Platform.Engineering.Copilot.Mcp/           # MCP Server (HTTP:5100 + stdio)
│   ├── Program.cs                              # Dual-mode startup
│   ├── Services/
│   │   └── McpHttpBridge.cs                    # HTTP endpoint bridge
│   └── Dockerfile
│
├── Platform.Engineering.Copilot.Agents/        # All agents and tools
│   ├── Common/
│   │   ├── BaseAgent.cs                        # Agent base class
│   │   └── BaseTool.cs                         # Tool base class
│   │
│   ├── Compliance/                             # Compliance Agent
│   │   ├── Agents/ComplianceAgent.cs
│   │   ├── Tools/
│   │   │   ├── ComplianceAssessmentTool.cs
│   │   │   ├── BatchRemediationTool.cs
│   │   │   ├── DefenderForCloudTool.cs
│   │   │   └── ... (12 tools)
│   │   ├── Services/
│   │   │   └── Engines/AtoComplianceEngine.cs
│   │   └── Extensions/ServiceCollectionExtensions.cs
│   │
│   ├── Infrastructure/                         # Infrastructure Agent
│   │   ├── Agents/InfrastructureAgent.cs
│   │   └── Tools/
│   │
│   ├── CostManagement/                         # Cost Agent
│   ├── Discovery/                              # Discovery Agent
│   ├── Environment/                            # Environment Agent
│   └── Security/                               # Security Agent
│
├── Platform.Engineering.Copilot.Core/          # Shared models, interfaces
│   ├── Interfaces/
│   ├── Models/
│   └── Data/
│
├── Platform.Engineering.Copilot.Chat/          # Web Chat UI (5001)
└── Platform.Engineering.Copilot.Admin.API/     # Admin API (5003)
```

---

## Agent Catalog

| Agent | Tools | Primary Capability |
|-------|-------|-------------------|
| **Compliance** | 12 | NIST 800-53 scanning, remediation, DFC integration |
| **Infrastructure** | 8 | Azure provisioning, IaC generation (Bicep/Terraform) |
| **Cost** | 6 | Cost analysis, optimization, budget tracking |
| **Discovery** | 5 | Resource inventory, health monitoring |
| **Environment** | 4 | Environment lifecycle, cloning |
| **Security** | 5 | Vulnerability scanning, policy enforcement |

### Compliance Agent Tools

| Tool | Description |
|------|-------------|
| `run_compliance_assessment` | NIST 800-53 assessment |
| `batch_remediation` | Fix multiple findings by severity |
| `execute_remediation` | Fix single finding |
| `generate_remediation_plan` | Create prioritized plan |
| `get_defender_findings` | DFC findings + secure score |
| `get_control_family_details` | Control family info |
| `collect_evidence` | Gather compliance evidence |
| `generate_compliance_document` | SSP/SAR/POA&M generation |
| `get_compliance_status` | Current status summary |
| `get_compliance_history` | Trends over time |
| `validate_remediation` | Verify fixes applied |
| `get_assessment_audit_log` | Audit trail |

---

## Request Flow

```
User: "Check NIST compliance for my subscription"
                    ↓
┌─────────────────────────────────────────────────────────────────┐
│ PlatformAgentGroupChat.InvokeAsync()                            │
│   └─ PlatformSelectionStrategy.SelectAgentAsync()               │
│       └─ Fast-path: "compliance" + "NIST" → Compliance Agent    │
└─────────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────────┐
│ ComplianceAgent.ProcessAsync(context)                           │
│   ├─ Build messages with GetSystemPrompt()                      │
│   ├─ Include RegisteredTools as AITools                         │
│   └─ ChatClient.GetResponseAsync(messages, tools)               │
└─────────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────────┐
│ LLM selects: run_compliance_assessment                          │
│   Arguments: { subscription_id: "..." }                         │
└─────────────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────────────┐
│ ComplianceAssessmentTool.ExecuteAsync()                         │
│   ├─ Call AtoComplianceEngine.RunAssessmentAsync()              │
│   ├─ Scan Azure resources via Resource Graph                    │
│   ├─ Integrate DFC findings for RA/CA families                  │
│   └─ Return JSON with findings, scores, recommendations         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Fast-Path Agent Selection

The `PlatformSelectionStrategy` uses keyword matching for instant routing:

```csharp
// Compliance patterns
if (message.ContainsAny("compliance", "nist", "fedramp", "stig", "assessment"))
    return ComplianceAgent;

// Infrastructure patterns
if (message.ContainsAny("create", "deploy", "provision", "terraform", "bicep"))
    return InfrastructureAgent;

// Cost patterns
if (message.ContainsAny("cost", "spending", "budget", "savings"))
    return CostAgent;
```

---

## Configuration

All configuration in `appsettings.json`:

```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "Enabled": true,
      "Temperature": 0.2,
      "MaxTokens": 6000,
      "DefenderForCloud": {
        "Enabled": true,
        "IncludeSecureScore": true,
        "MapToNistControls": true
      }
    },
    "InfrastructureAgent": {
      "Enabled": true,
      "Temperature": 0.4,
      "DefaultRegion": "usgovvirginia"
    }
  }
}
```

---

## Adding a New Agent

1. **Create agent folder**: `src/Platform.Engineering.Copilot.Agents/{Name}/`

2. **Create agent class** extending `BaseAgent`:
   ```csharp
   public class MyAgent : BaseAgent
   {
       public override string AgentId => "my-agent";
       public override string AgentName => "My Agent";
   }
   ```

3. **Create tools** extending `BaseTool`:
   ```csharp
   public class MyTool : BaseTool
   {
       public override string Name => "my_tool";
   }
   ```

4. **Register in DI** (`ServiceCollectionExtensions.cs`):
   ```csharp
   services.AddScoped<MyTool>();
   services.AddScoped<MyAgent>();
   ```

5. **Add to MCP tool list** (`McpHttpBridge.cs`)

6. **Create agent prompt** (`.github/prompts/{name}-agent.prompt.md`)

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 9.0 |
| AI Framework | Microsoft Semantic Kernel 1.26+ |
| MCP | ModelContextProtocol 0.4.0-preview |
| Azure SDK | Azure.ResourceManager.* |
| Database | SQLite (default) / SQL Server |
| Cache | IMemoryCache |
| Container | Docker, ACI, AKS |

---

## Related Documentation

- [AGENTS.md](./AGENTS.md) - Detailed agent capabilities
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Docker, ACI, AKS deployment
- [GETTING-STARTED.md](./GETTING-STARTED.md) - Quick start guide
- [.github/prompts/](../.github/prompts/) - Agent prompt files

