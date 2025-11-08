# Platform Engineering Copilot - Agent Capabilities Matrix

## Extension → MCP Server → Agent Routing

The GitHub Copilot extension uses a **universal chat interface** that routes to all 7 specialized agents through natural language understanding.

---

## Architecture Flow

```
GitHub Copilot Chat (@platform user message)
    ↓
Extension: chatParticipant.ts
    ↓
McpClient.sendChatMessage(message, conversationId, context)
    ↓
HTTP POST → http://localhost:5100/mcp/chat
    ↓
MCP Server: OrchestratorAgent (Semantic Kernel + GPT-4)
    ├─ Intent Classification
    ├─ Confidence Scoring  
    └─ Agent Selection
        ↓
Specialized Agent Execution
    ↓
Formatted Response with Metadata
    ↓
Extension: Rich Markdown Rendering
```

---

## Agent Capabilities by Domain

### 🏗️ Infrastructure Agent

**Triggers:** Infrastructure, provisioning, deployment, network, Bicep, Terraform, ARM templates

**Extension Methods:**
- `sendChatMessage()` - Universal interface

**Example Prompts:**
```
@platform Create a storage account in East US
@platform Deploy Bicep template from ./infra/main.bicep
@platform Design a hub-spoke network topology
@platform Generate ARM template for AKS cluster
```

**Agent Capabilities:**
- Azure resource provisioning
- Bicep/Terraform/ARM template generation
- Network topology design
- Infrastructure validation
- Resource health monitoring

---

### 🔒 Compliance Agent

**Triggers:** Compliance, NIST, ATO, eMASS, security scan, vulnerabilities, controls

**Extension Methods:**
- `sendChatMessage()` - Universal interface
- `requestComplianceScan()` - Specialized wrapper
- `analyzeCodeForCompliance()` - Code analysis
- `analyzeRepositoryForCompliance()` - Repository scan

**Example Prompts:**
```
@platform Run NIST 800-53 compliance scan on subscription xyz
@platform Generate eMASS package for production
@platform Scan this code for security vulnerabilities
@platform Check ATO readiness for resource group rg-prod
```

**Agent Capabilities:**
- NIST 800-53, 800-171, ISO 27001, SOC 2, PCI-DSS assessments
- Code security scanning
- ATO documentation generation
- eMASS package preparation
- Security control mapping
- Remediation plan creation

---

### 💰 Cost Management Agent

**Triggers:** Cost, estimate, pricing, budget, optimization, savings

**Extension Methods:**
- `sendChatMessage()` - Universal interface

**Example Prompts:**
```
@platform Estimate cost for Standard_D4s_v3 VM running 24/7
@platform Show cost breakdown for resource group rg-dev
@platform Find cost optimization opportunities
@platform Compare pricing for AKS vs ACA
```

**Agent Capabilities:**
- Real-time cost estimation
- Resource-level cost breakdown
- Optimization recommendations
- Budget forecasting
- Reserved Instance analysis
- Cost allocation by tags

---

### 🌐 Environment Agent

**Triggers:** Environment, clone, scale, lifecycle, blue-green, deployment

**Extension Methods:**
- `sendChatMessage()` - Universal interface

**Example Prompts:**
```
@platform Clone production environment to staging
@platform Scale up environment to handle 2x traffic
@platform Create blue-green deployment for env-prod
@platform Check environment health for all dev environments
```

**Agent Capabilities:**
- Multi-environment lifecycle management
- Environment cloning and templating
- Blue-green deployments
- Scaling operations
- Configuration drift detection
- Health monitoring

---

### 🔍 Discovery Agent

**Triggers:** List, discover, find, inventory, resources, dependencies

**Extension Methods:**
- `sendChatMessage()` - Universal interface

**Example Prompts:**
```
@platform List all VMs in East US
@platform Find orphaned resources with no tags
@platform Show storage accounts in resource group rg-data
@platform Map dependencies for app-service-001
```

**Agent Capabilities:**
- Resource inventory across subscriptions
- Dependency mapping
- Orphaned resource detection
- Tag compliance checking
- Resource utilization analysis
- Cross-subscription discovery

---

### 🛠️ Service Creation Agent

**Triggers:** Service, microservice, architecture, template, scaffold, API

**Extension Methods:**
- `sendChatMessage()` - Universal interface

**Example Prompts:**
```
@platform Design microservice architecture for e-commerce
@platform Create REST API service template with auth
@platform Generate DevOps pipeline for .NET service
@platform Set up AKS-based container service
```

**Agent Capabilities:**
- Mission-based service design
- Microservice template generation
- API scaffolding
- Service mesh integration
- Container orchestration setup
- DevOps pipeline generation

---

### 📄 Document Agent

**Triggers:** Document, documentation, diagram, runbook, SSP, SAR, architecture

**Extension Methods:**
- `sendChatMessage()` - Universal interface
- `requestDocumentAnalysis()` - Document analysis

**Example Prompts:**
```
@platform Generate architecture documentation for rg-prod
@platform Create System Security Plan (SSP) for my environment
@platform Extract requirements from this compliance document
@platform Create deployment runbook for this template
```

**Agent Capabilities:**
- Technical documentation generation
- Architecture diagram creation
- Compliance documentation (SSP, SAR, SAP)
- Runbook generation
- Security documentation (STIG, SCAP)
- Document analysis and extraction

---

## Remediation Operations (Compliance Agent)

**Extension Methods:**
- `requestRemediationPlan()` - Generate plan
- `executeRemediation()` - Execute fixes
- `getRemediationStatus()` - Check progress

**Example Prompts:**
```
@platform Generate remediation plan for failed controls
@platform Execute remediation in dry-run mode
@platform Show remediation progress for execution exe-123
```

---

## Code & Repository Analysis (Compliance Agent)

**Extension Methods:**
- `analyzeCodeForCompliance()` - Analyze current file
- `analyzeRepositoryForCompliance()` - Scan entire repo

**Triggers:**
- "analyze code"
- "scan code"  
- "code compliance"
- "security scan"
- "analyze this file"

**Auto-Detection:**
The extension automatically detects code analysis requests and routes to the appropriate method with the active editor's content.

---

## How It Works: Natural Language → Agent Routing

1. **User sends message** via `@platform` in GitHub Copilot Chat
2. **Extension receives** message in `chatParticipant.ts`
3. **Check for code analysis** keywords → route to `analyzeCodeForCompliance()`
4. **Otherwise, send to universal endpoint** → `sendChatMessage()` 
5. **MCP Server OrchestratorAgent** analyzes intent using GPT-4
6. **Agent selection** based on confidence scores:
   - Infrastructure keywords → InfrastructureAgent
   - Compliance keywords → ComplianceAgent
   - Cost keywords → CostManagementAgent
   - Environment keywords → EnvironmentAgent
   - Discovery keywords → DiscoveryAgent
   - Service keywords → ServiceCreationAgent
   - Document keywords → DocumentAgent
7. **Specialized agent executes** using Azure SDK + Semantic Kernel plugins
8. **Response formatted** with metadata, suggestions, action buttons
9. **Extension renders** rich markdown in GitHub Copilot Chat

---

## Agent Intent Keywords

### Infrastructure
`provision`, `deploy`, `create resource`, `bicep`, `terraform`, `arm template`, `network`, `topology`, `infrastructure`, `aks`, `vm`, `storage`, `vnet`, `subnet`

### Compliance  
`compliance`, `nist`, `800-53`, `800-171`, `iso`, `soc 2`, `pci-dss`, `ato`, `emass`, `security`, `vulnerability`, `scan`, `control`, `assessment`, `audit`

### Cost
`cost`, `estimate`, `price`, `pricing`, `budget`, `spend`, `optimize`, `savings`, `reserved instance`, `forecast`

### Environment
`environment`, `clone`, `scale`, `lifecycle`, `blue-green`, `canary`, `staging`, `production`, `dev`, `test`, `drift`

### Discovery
`list`, `find`, `discover`, `inventory`, `show`, `resources`, `dependency`, `orphaned`, `tags`, `utilization`

### Service Creation
`service`, `microservice`, `architecture`, `design`, `template`, `scaffold`, `api`, `devops`, `pipeline`, `container`, `mesh`

### Document
`document`, `documentation`, `diagram`, `architecture`, `runbook`, `ssp`, `sar`, `sap`, `procedure`, `policy`, `extract`, `generate docs`

---

## Multi-Agent Orchestration

The OrchestratorAgent can invoke **multiple agents sequentially or in parallel** for complex requests:

**Example:**
```
@platform Create a production environment with compliance scanning and cost estimate
```

**Execution Plan:**
1. Infrastructure Agent → Provision resources
2. Compliance Agent → Run NIST 800-53 scan  
3. Cost Management Agent → Calculate monthly costs
4. Document Agent → Generate architecture documentation

**Response includes:**
- All agent results aggregated
- Execution pattern (Sequential/Parallel)
- Processing time per agent
- Suggestions for next steps

---

## Extension Configuration

All agents accessible through single configuration:

```json
{
  "platform-copilot.apiUrl": "http://localhost:5100",
  "platform-copilot.timeout": 60000,
  "platform-copilot.enableLogging": true
}
```

No per-agent configuration needed - the MCP server handles agent orchestration internally.

---

## Summary: Complete Agent Coverage ✅

| Agent | Extension Support | Method | Status |
|-------|------------------|--------|--------|
| Infrastructure | ✅ Full | `sendChatMessage()` | ✅ Working |
| Compliance | ✅ Full + Specialized | `sendChatMessage()`, `requestComplianceScan()`, `analyzeCodeForCompliance()` | ✅ Working |
| Cost Management | ✅ Full | `sendChatMessage()` | ✅ Working |
| Environment | ✅ Full | `sendChatMessage()` | ✅ Working |
| Discovery | ✅ Full | `sendChatMessage()` | ✅ Working |
| Service Creation | ✅ Full | `sendChatMessage()` | ✅ Working |
| Document | ✅ Full + Specialized | `sendChatMessage()`, `requestDocumentAnalysis()` | ✅ Working |

**All 7 agents fully supported through the GitHub Copilot extension!**
