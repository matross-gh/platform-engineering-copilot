# Platform Engineering Copilot - Agents

**Version:** 3.0 (Microsoft Agent Framework Architecture)  
**Last Updated:** January 2026

---

## Overview

The Platform Engineering Copilot uses **6 specialized AI agents** built on the Microsoft Agent Framework. Each agent extends `BaseAgent` and registers domain-specific tools extending `BaseTool`.

### Microsoft Agent Framework Pattern

```
┌─────────────────────────────────────────────────────────────────┐
│                    PlatformAgentGroupChat                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │           PlatformSelectionStrategy                        │  │
│  │  (Fast-path keyword matching for agent routing)            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              ↓                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              {Agent} : BaseAgent                           │  │
│  │  ├─ RegisteredTools: List<BaseTool>                        │  │
│  │  ├─ ProcessAsync(context) → AgentResponse                  │  │
│  │  └─ GetSystemPrompt() → tool selection guidance            │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Agent Catalog

| Agent | Tools | Location | Description |
|-------|-------|----------|-------------|
| [Compliance](#compliance-agent) | 12 | `Agents/Compliance/` | NIST 800-53, DFC, remediation |
| [Infrastructure](#infrastructure-agent) | 8 | `Agents/Infrastructure/` | Azure provisioning, IaC |
| [Cost](#cost-management-agent) | 6 | `Agents/CostManagement/` | Cost analysis, optimization |
| [Discovery](#discovery-agent) | 5 | `Agents/Discovery/` | Resource inventory, health |
| [Environment](#environment-agent) | 4 | `Agents/Environment/` | Lifecycle, cloning |
| [Security](#security-agent) | 5 | `Agents/Security/` | Vulnerability, policy |

---

## Compliance Agent

**Purpose**: NIST 800-53 compliance assessment, remediation, and ATO documentation.

**Prompt File**: [.github/prompts/compliance-agent.prompt.md](../.github/prompts/compliance-agent.prompt.md)

### Tools (12)

| Tool | Description |
|------|-------------|
| `run_compliance_assessment` | Run NIST 800-53 assessment against subscription |
| `batch_remediation` | Fix multiple findings by severity filter |
| `execute_remediation` | Fix single finding (requires finding_id) |
| `generate_remediation_plan` | Create prioritized remediation plan |
| `validate_remediation` | Verify remediation was successful |
| `get_defender_findings` | Fetch DFC findings + secure score |
| `get_control_family_details` | Get NIST control family details |
| `collect_evidence` | Gather compliance evidence artifacts |
| `generate_compliance_document` | Generate SSP, SAR, or POA&M |
| `get_compliance_status` | Current compliance status summary |
| `get_compliance_history` | Compliance trends over time |
| `get_assessment_audit_log` | Assessment audit trail |

### Example Queries

```
"Run NIST 800-53 compliance scan"
"Check compliance for subscription xyz"
"Start remediation for high-priority issues"
"Generate SSP document"
"What's my secure score?"
"Show defender findings"
```

### Configuration

```json
{
  "ComplianceAgent": {
    "Enabled": true,
    "Temperature": 0.2,
    "MaxTokens": 6000,
    "EnableAutomatedRemediation": true,
    "DefaultFramework": "NIST80053",
    "DefenderForCloud": {
      "Enabled": true,
      "IncludeSecureScore": true,
      "MapToNistControls": true
    }
  }
}
```

---

## Infrastructure Agent

**Purpose**: Azure resource provisioning and Infrastructure-as-Code generation.

### Tools (8)

| Tool | Description |
|------|-------------|
| `create_resource` | Create Azure resource via ARM API |
| `generate_bicep` | Generate Bicep template |
| `generate_terraform` | Generate Terraform configuration |
| `generate_kubernetes` | Generate K8s manifests |
| `validate_template` | Validate IaC template |
| `deploy_template` | Deploy template to Azure |
| `get_resource_details` | Get resource configuration |
| `delete_resource` | Delete Azure resource |

### Example Queries

```
"Create storage account data001 in rg-dr"
"Generate Bicep for AKS cluster"
"Create Terraform for 3-tier app"
"Deploy the template"
```

### Configuration

```json
{
  "InfrastructureAgent": {
    "Enabled": true,
    "Temperature": 0.4,
    "MaxTokens": 8000,
    "DefaultRegion": "usgovvirginia",
    "EnableComplianceEnhancement": true
  }
}
```

---

## Cost Management Agent

**Purpose**: Cost analysis, optimization recommendations, and budget tracking.

### Tools (6)

| Tool | Description |
|------|-------------|
| `get_cost_analysis` | Analyze costs by service/RG/tag |
| `get_cost_forecast` | Forecast future spending |
| `get_budget_status` | Check budget utilization |
| `get_savings_recommendations` | Right-sizing, reserved instances |
| `identify_idle_resources` | Find unused resources |
| `create_cost_report` | Generate cost report |

### Example Queries

```
"Show cost analysis for last 30 days"
"What are my top spending services?"
"Find idle resources"
"Create savings recommendations"
```

### Configuration

```json
{
  "CostManagementAgent": {
    "Enabled": true,
    "Temperature": 0.3,
    "DefaultCurrency": "USD"
  }
}
```

---

## Discovery Agent

**Purpose**: Resource discovery, inventory management, and health monitoring.

### Tools (5)

| Tool | Description |
|------|-------------|
| `list_resources` | List Azure resources with filters |
| `get_resource_health` | Get resource health status |
| `get_resource_inventory` | Full inventory report |
| `search_resources` | Search by name/tag/type |
| `get_resource_topology` | Resource dependency graph |

### Example Queries

```
"List all VMs in my subscription"
"What resources are in rg-production?"
"Which resources are unhealthy?"
"Search for resources tagged owner:john"
```

### Configuration

```json
{
  "DiscoveryAgent": {
    "Enabled": true,
    "Temperature": 0.3,
    "EnableHealthMonitoring": true
  }
}
```

---

## Environment Agent

**Purpose**: Environment lifecycle management, cloning, and scaling.

### Tools (4)

| Tool | Description |
|------|-------------|
| `clone_environment` | Clone environment to new RG |
| `scale_environment` | Scale environment resources |
| `get_environment_status` | Environment health summary |
| `destroy_environment` | Delete environment resources |

### Example Queries

```
"Clone dev environment to staging"
"Scale production to high availability"
"What's the status of dev environment?"
```

### Configuration

```json
{
  "EnvironmentAgent": {
    "Enabled": true
  }
}
```

---

## Security Agent

**Purpose**: Security posture assessment, vulnerability scanning, and policy enforcement.

### Tools (5)

| Tool | Description |
|------|-------------|
| `get_security_posture` | Overall security score |
| `run_vulnerability_scan` | Scan for vulnerabilities |
| `get_policy_compliance` | Azure Policy compliance |
| `get_security_recommendations` | Security improvement suggestions |
| `get_threat_alerts` | Active threat alerts |

### Example Queries

```
"What's my security posture?"
"Run vulnerability scan"
"Show policy compliance status"
"Are there any active threats?"
```

### Configuration

```json
{
  "SecurityAgent": {
    "Enabled": true
  }
}
```

---

## Fast-Path Selection

The `PlatformSelectionStrategy` routes requests to agents based on keywords:

| Keywords | Agent |
|----------|-------|
| compliance, nist, fedramp, stig, assessment, remediation | Compliance |
| create, deploy, provision, terraform, bicep, kubernetes | Infrastructure |
| cost, spending, budget, savings, optimization | Cost |
| list, resources, inventory, health, discover | Discovery |
| environment, clone, scale, lifecycle | Environment |
| security, vulnerability, threat, policy | Security |

---

## Agent Coordination

Agents coordinate through `PlatformAgentGroupChat` with shared context:

1. **No Direct Agent Calls**: Agents never call each other directly
2. **Shared Memory**: Assessment results cached for multi-turn workflows
3. **Context Passing**: Subscription ID, findings shared across turns
4. **Tool Chaining**: One agent's output can inform another's action

### Example Multi-Turn Workflow

```
Turn 1: "Check compliance" → Compliance Agent runs assessment
Turn 2: "Start remediation" → Uses cached findings (no re-scan)
Turn 3: "Show cost impact" → Cost Agent analyzes affected resources
```

---

## Adding a New Tool

1. **Create tool class** in `Agents/{Agent}/Tools/`:

```csharp
public class MyNewTool : BaseTool
{
    public override string Name => "my_new_tool";
    
    public override string Description =>
        "Description shown to LLM for selection. " +
        "Use when user says: 'do X', 'perform Y'";

    public MyNewTool(ILogger<MyNewTool> logger, IMyService service) 
        : base(logger)
    {
        _service = service;
        Parameters.Add(new ToolParameter("param1", "Description", true));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var param1 = GetRequiredString(arguments, "param1");
        var result = await _service.DoSomethingAsync(param1);
        return ToJson(new { success = true, result });
    }
}
```

2. **Register in DI** (`ServiceCollectionExtensions.cs`):
```csharp
services.AddScoped<MyNewTool>();
```

3. **Inject into agent** constructor and call `RegisterTool(myNewTool)`

4. **Add to MCP tool list** in `McpHttpBridge.cs`

---

## Related Documentation

- [ARCHITECTURE.md](./ARCHITECTURE.md) - System architecture
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Deployment guide
- [.github/prompts/](../.github/prompts/) - Agent prompt files
