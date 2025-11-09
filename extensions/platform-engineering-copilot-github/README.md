# Platform Engineering Copilot for GitHub Copilot

**AI-Powered Azure Infrastructure Management directly in VS Code**

This VS Code extension integrates Platform Engineering Copilot's **Multi-Agent MCP Server** with GitHub Copilot Chat, enabling natural language interactions for Azure infrastructure provisioning, compliance scanning, cost analysis, environment management, and more.

> **Latest Update (Oct 2025):** Fixed duplicate agent registration issue. Extension now fully supports all 7 specialized agents via the unified MCP server endpoint.

---

## 🌟 Features - 7 Specialized Agents

### 🏗️ **Infrastructure Agent**
- Provision Azure resources using natural language
- Deploy Bicep and Terraform templates with validation
- Network topology design and implementation
- Infrastructure-as-Code (IaC) template generation
- Azure Resource Manager (ARM) template deployment
- Real-time resource health monitoring and diagnostics

### 🔒 **Compliance Agent** 
- **NIST 800-53** compliance assessments (Rev 4 & 5)
- **NIST 800-171**, **ISO 27001**, **SOC 2**, **PCI-DSS** support
- Automated remediation plan generation
- **eMASS** package preparation and documentation
- **ATO (Authority to Operate)** documentation generation
- Security control mapping and gap analysis
- Code scanning for security vulnerabilities
- Policy-as-Code enforcement

### 💰 **Cost Management Agent**
- Real-time Azure cost estimation
- Resource-level cost breakdowns
- Cost optimization recommendations
- Budget forecasting and alerts
- Azure Advisor cost insights integration
- Reserved Instance and Savings Plan analysis
- Cost allocation by tags and resource groups

### 🌐 **Environment Agent**
- Multi-environment lifecycle management (Dev/Test/Prod)
- Environment cloning and templating
- Blue-green deployment support
- Environment scaling operations
- Configuration drift detection
- Environment health monitoring
- Resource tagging and organization

### 🔍 **Discovery Agent**
- Azure resource inventory across subscriptions
- Resource dependency mapping
- Orphaned resource detection
- Resource utilization analysis
- Tag compliance checking
- Resource group organization
- Cross-subscription resource discovery

### 🛠️ **Service Creation Agent**
- Mission-based service architecture design
- Microservice template generation
- API and service scaffolding
- Service mesh integration guidance
- Container orchestration setup (AKS, ACA)
- Service catalog management
- DevOps pipeline generation

### 📄 **Document Agent**
- Technical documentation generation
- Architecture diagram creation
- Compliance documentation (SSP, SAR, SAP)
- Runbook and playbook generation
- Security documentation (STIG, SCAP)
- Automated document analysis and extraction
- Policy and procedure documentation

---

## 📋 Prerequisites

1. **VS Code**: Version 1.90.0 or higher
2. **GitHub Copilot**: Active subscription with Chat enabled
3. **Platform MCP Server**: HTTP endpoint running (dual-mode: HTTP + stdio)
   - Default: `http://localhost:5100`
   - Configurable via extension settings
   - Supports all 7 specialized agents via unified orchestration
4. **Azure Credentials**: For actual Azure operations (optional for testing)
   - Azure CLI authenticated (`az login`)
   - Or Azure service principal with appropriate permissions

---

## 🚀 Installation

### Quick Install from VSIX (Recommended)

```bash
# Download the latest .vsix file from releases
# Install in VS Code:
code --install-extension platform-copilot-github-1.0.0.vsix

# Verify installation
code --list-extensions | grep platform
```

### Build from Source

```bash
# Clone the repository
git clone https://github.com/azurenoops/platform-engineering-copilot.git
cd platform-engineering-copilot/extensions/platform-engineering-copilot-github

# Install dependencies
npm install

# Compile TypeScript
npm run compile

# Package extension (creates .vsix file)
npm run package

# Install the generated VSIX
code --install-extension platform-copilot-github-1.0.0.vsix
```

### Start MCP Server

Before using the extension, ensure the MCP server is running:

```bash
# From the repository root
cd platform-engineering-copilot

# Using Docker (Recommended)
docker-compose -f docker-compose.essentials.yml up -d

# Or run directly (requires .NET 9)
dotnet run --project src/Platform.Engineering.Copilot.Mcp -- --http

# Verify server is running
curl http://localhost:5100/health
# Expected: {"status":"healthy","mode":"dual (http+stdio)","server":"Platform Engineering Copilot MCP","version":"1.0.0"}
```

---

## ⚙️ Configuration

### VS Code Settings

Press `Cmd+,` (Mac) or `Ctrl+,` (Windows/Linux) and search for "Platform Copilot":

```json
{
  "platform-copilot.apiUrl": "http://localhost:5100",
  "platform-copilot.apiKey": "",
  "platform-copilot.timeout": 60000,
  "platform-copilot.enableLogging": true
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| `apiUrl` | MCP server HTTP endpoint | `http://localhost:5100` |
| `apiKey` | Optional API key for authentication | `""` (empty) |
| `timeout` | Request timeout in milliseconds | `60000` (60 seconds) |
| `enableLogging` | Enable debug logging to Output panel | `true` |

### Quick Configuration

Run these commands from the Command Palette (`Cmd+Shift+P` / `Ctrl+Shift+P`):

- **Platform Copilot: Check Platform API Health** - Verify MCP server connectivity
- **Platform Copilot: Configure Platform API Connection** - Interactive setup wizard

---

## 💬 Usage - All 7 Agents

Open GitHub Copilot Chat and use the `@platform` participant:

### 🏗️ Infrastructure Agent Examples

```
@platform Create a storage account named mydata001 in resource group rg-dev with geo-redundant replication
```

```
@platform Provision an AKS cluster with 3 nodes in East US using Standard_D4s_v3 VMs
```

```
@platform Deploy a virtual network with 3 subnets for web, app, and database tiers
```

```
@platform Generate a Bicep template for a hub-spoke network topology
```

```
@platform Design a network topology for a 3-tier web application with DMZ
```

### 🔒 Compliance Agent Examples

```
@platform Run a NIST 800-53 compliance scan on subscription abc-123
```

```
@platform Check compliance for resource group rg-production on subscription abc-123 against NIST 800-53
```

```
@platform Generate an eMASS package for my production environment
```

```
@platform Create an ATO documentation package with SSP, SAR, and SAP
```

```
@platform Scan this code for security vulnerabilities and ATO compliance issues
```

```
@platform Generate a remediation plan for failed compliance controls
```

### 💰 Cost Management Agent Examples

```
@platform Estimate the cost of a Standard_D4s_v3 VM running 24/7 for a month
```

```
@platform Show me the cost breakdown for resource group rg-dev
```

```
@platform How much will it cost to run 5 AKS nodes for a month in East US?
```

```
@platform Analyze my subscription for cost optimization opportunities
```

```
@platform Compare costs between Standard_D4s_v3 and Standard_E4s_v3 for my workload
```

### 🌐 Environment Agent Examples

```
@platform Clone the development environment to create a new staging environment
```

```
@platform Scale up the production environment to handle 2x traffic
```

```
@platform Create a new environment template based on rg-prod configuration
```

```
@platform Check environment health for all dev environments
```

```
@platform Perform a blue-green deployment for environment env-prod-001
```

### 🔍 Discovery Agent Examples

```
@platform List all resources in East US region
```

```
@platform Show me storage accounts in resource group rg-data
```

```
@platform What VMs are running in my subscription?
```

```
@platform Find all orphaned resources with no tags
```

```
@platform Discover dependencies for resource rg-app/appservice-001
```

```
@platform Map all resources by cost center tag
```

### 🛠️ Service Creation Agent Examples

```
@platform Create a microservice architecture for an e-commerce platform
```

```
@platform Generate a service template for a REST API with authentication
```

```
@platform Design a mission-based service for real-time data processing
```

```
@platform Set up a container-based service with AKS and Azure Container Registry
```

```
@platform Create a DevOps pipeline for deploying a .NET microservice
```

### 📄 Document Agent Examples

```
@platform Generate architecture documentation for resource group rg-prod
```

```
@platform Create a System Security Plan (SSP) for my Azure environment
```

```
@platform Generate runbooks for incident response procedures
```

```
@platform Extract security requirements from this compliance document
```

```
@platform Create a deployment guide for this infrastructure template
```

---

## 🎯 Example Workflows

### Scenario 1: Complete Infrastructure Provisioning (Infrastructure Agent)

```
You: @platform I need to create a new development environment with AKS and monitoring

Platform: 🏗️ Infrastructure Agent

I'll help you set up a development environment. Let me create:

1. Resource Group: rg-dev-001
2. AKS Cluster: aks-dev-001
   - Node count: 3
   - VM size: Standard_D2s_v3
   - Kubernetes version: 1.28
3. Azure Monitor Workspace
4. Container Insights enabled

Would you like me to proceed with this configuration?

You: @platform Yes, proceed with the deployment in East US

Platform: ✅ Creating development environment...

**Created Resources:**
- Resource Group: rg-dev-001 (East US)
- AKS Cluster: aks-dev-001 (3 nodes, Standard_D2s_v3)
- Azure Monitor: monitor-dev-001
- Log Analytics Workspace: law-dev-001

**Estimated Monthly Cost:** $350.00

**Next Steps:**
1. Configure kubectl: `az aks get-credentials --resource-group rg-dev-001 --name aks-dev-001`
2. Deploy your applications
3. Set up CI/CD pipelines
```

### Scenario 2: Compliance Assessment and Remediation (Compliance Agent)

```
You: @platform Run a comprehensive NIST 800-53 compliance scan on my production subscription

Platform: 🔒 Compliance Agent

Running NIST 800-53 Rev 5 compliance assessment...

**Compliance Score: 78%**

✅ **Passed Controls:** 156 (High: 89, Moderate: 45, Low: 22)
❌ **Failed Controls:** 34 (High: 8, Moderate: 18, Low: 8)
⚠️  **Warnings:** 12 controls need review

**Top Critical Issues:**
1. **AC-2 (Account Management)** - Missing MFA on 12 accounts
2. **SC-7 (Boundary Protection)** - No NSG on 5 subnets
3. **AU-2 (Audit Events)** - Diagnostic logs not enabled on 8 resources
4. **CM-7 (Least Functionality)** - Unused ports open on 3 VMs
5. **IA-5 (Authenticator Management)** - Weak password policies detected

Would you like me to generate an automated remediation plan?

You: @platform Yes, create a remediation plan and show me what it will fix

Platform: ✅ **Automated Remediation Plan Created**

**Plan ID:** rem-plan-20241107-001
**Estimated Time:** 15-20 minutes
**Impact:** Low (non-breaking changes)

**Actions:**
1. Enable MFA for 12 user accounts (AC-2)
2. Create and attach NSGs to 5 subnets (SC-7)
3. Enable diagnostic settings on 8 resources (AU-2)
4. Close unused ports on 3 VMs (CM-7)
5. Update password policy to meet NIST requirements (IA-5)

**Estimated Cost Impact:** +$25/month (Log Analytics storage)

Would you like to:
- Execute plan in **DRY RUN** mode (preview only)
- Execute plan with **AUTO-APPROVAL**
- Review detailed changes before execution
```

### Scenario 3: Cost Optimization Analysis (Cost Management Agent)

```
You: @platform Analyze my Azure costs and find optimization opportunities

Platform: 💰 Cost Management Agent

**Cost Analysis Summary (Last 30 Days)**

**Total Spend:** $5,234.00
**Trend:** ↑ 12% vs previous month
**Forecast (Next 30 Days):** $5,860.00

**Top 5 Expensive Resources:**
1. AKS Cluster (aks-prod-001): $1,800/month
   - 10 nodes × Standard_D4s_v3
   - 98% average utilization
2. SQL Database (sql-prod-db): $950/month
   - Business Critical tier
   - 45% average DTU usage
3. Storage Account (stprod001): $430/month
   - Premium SSD, Hot tier
   - 2.5 TB used
4. Virtual Machines (3x Standard_D4s_v3): $1,200/month
   - Running 24/7
   - Average CPU: 35%
5. Azure Firewall: $854/month
   - Standard tier

💡 **Optimization Opportunities:**

**High Impact (Quick Wins):**
1. **Reserved Instances for VMs** → Save $450/month (38% savings)
   - 3-year commitment recommended
2. **Right-size SQL Database** → Save $320/month
   - Downgrade to General Purpose tier
   - Current usage supports smaller tier
3. **Enable Auto-Shutdown for Dev VMs** → Save $200/month
   - VMs idle after 6 PM and weekends

**Medium Impact:**
4. **Lifecycle Management for Storage** → Save $120/month
   - Move to Cool tier after 30 days
   - Archive after 90 days
5. **AKS Node Pool Optimization** → Save $180/month
   - Enable cluster autoscaler
   - Use spot instances for non-critical workloads

**Total Potential Savings:** $1,270/month (24% reduction)
**Annual Savings:** $15,240

Would you like me to:
- Generate implementation plan for top 3 optimizations?
- Create budget alerts at $5,000/month threshold?
- Set up cost anomaly detection?
```

### Scenario 4: Environment Cloning (Environment Agent)

```
You: @platform Clone production environment to create a new staging environment

Platform: 🌐 Environment Agent

**Environment Cloning Operation**

**Source Environment:** rg-prod
- Location: East US
- Resources: 47 resources
- Tags: env=production, criticality=high

**Target Environment:** rg-staging (to be created)

**Resources to Clone:**
✅ AKS Cluster (scaled down: 3 → 2 nodes)
✅ SQL Database (tier: Business Critical → General Purpose)
✅ Storage Accounts (3)
✅ Key Vault (new instance, keys rotated)
✅ Virtual Network + Subnets
✅ Application Gateway
✅ Azure Monitor + Log Analytics

**Configuration Changes:**
- VM sizes: Downscale to Standard_D2s_v3
- Backup policies: Weekly → None
- Monitoring: Production → Development tier
- High availability: Disabled for cost savings

**Estimated Cost:**
- Production: $5,234/month
- Staging (cloned): $1,450/month (72% reduction)

**Time to Complete:** ~25 minutes

Proceed with cloning operation?

You: @platform Yes, proceed and notify me when complete

Platform: ✅ **Environment Cloning in Progress...**

[Progress bar: ████████░░ 80%]

**Completed Steps:**
✓ Resource group created
✓ Virtual network deployed
✓ Storage accounts provisioned
✓ AKS cluster deployment in progress...
⏳ Configuring monitoring...

**Status:** All resources deployed successfully!
**Time Taken:** 23 minutes
**Next Steps:** Update application configs to point to staging endpoints
```

### Scenario 5: Service Architecture Design (Service Creation Agent)

```
You: @platform Design a microservice architecture for a real-time order processing system

Platform: 🛠️ Service Creation Agent

**Mission-Based Service Architecture Design**

**Mission:** Real-Time Order Processing Platform

**Recommended Architecture:**

**1. Core Microservices (AKS)**
   - Order API Service (Node.js/Express)
   - Payment Processing Service (C#/.NET)
   - Inventory Service (Python/FastAPI)
   - Notification Service (Node.js)
   - Shipping Service (C#/.NET)

**2. Data Layer**
   - Cosmos DB (Order data, globally distributed)
   - Azure SQL (Inventory management)
   - Redis Cache (Session state, rate limiting)
   - Azure Storage (Order documents, receipts)

**3. Messaging & Events**
   - Azure Service Bus (order workflow)
   - Event Grid (event-driven notifications)
   - Application Insights (distributed tracing)

**4. API Management**
   - Azure API Management (gateway)
   - Rate limiting: 1000 req/min per client
   - OAuth 2.0 + Azure AD B2C

**5. DevOps Pipeline**
   - GitHub Actions (CI/CD)
   - Container Registry (ACR)
   - Helm charts for deployments
   - Blue-green deployment strategy

**Estimated Monthly Cost:** $2,850
**Expected Throughput:** 10,000 orders/hour
**SLA Target:** 99.95% availability

Would you like me to:
- Generate complete Bicep/Terraform templates?
- Create service scaffolding code?
- Set up DevOps pipelines?
```

---

## 🏗️ Architecture

The extension integrates with the **Platform Engineering Copilot Multi-Agent MCP Server**, which orchestrates 7 specialized agents using Semantic Kernel and GPT-4.

```
GitHub Copilot Chat (@platform)
    ↓
VS Code Extension (TypeScript)
    ↓
HTTP Client (Axios)
    ↓
┌─────────────────────────────────────────────────────────┐
│  Platform MCP Server (Port 5100)                        │
│  Dual Mode: HTTP + stdio                                │
│                                                          │
│  ┌─────────────────────────────────────────────┐       │
│  │   OrchestratorAgent (Semantic Kernel)       │       │
│  │   - Intent Classification                   │       │
│  │   - Execution Planning                      │       │
│  │   - Multi-Agent Coordination                │       │
│  └─────────────────────────────────────────────┘       │
│                       ↓                                  │
│  ┌────────────────────────────────────────────────────┐│
│  │        7 Specialized Agents (ISpecializedAgent)    ││
│  ├────────────────────────────────────────────────────┤│
│  │ 🏗️  Infrastructure Agent                           ││
│  │ 🔒 Compliance Agent (NIST, ATO, eMASS)            ││
│  │ 💰 Cost Management Agent                          ││
│  │ 🌐 Environment Agent                              ││
│  │ 🔍 Discovery Agent                                ││
│  │ 🛠️  Service Creation Agent                        ││
│  │ 📄 Document Agent                                 ││
│  └────────────────────────────────────────────────────┘│
│                       ↓                                  │
│  ┌─────────────────────────────────────────────┐       │
│  │   Azure Resource Manager                    │       │
│  │   - Bicep/ARM Templates                     │       │
│  │   - Terraform Providers                     │       │
│  │   - Azure SDK                               │       │
│  └─────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────┘
                       ↓
        ┌──────────────────────────────┐
        │     Azure Resources          │
        │  (Compute, Network, Storage, │
        │   Security, Cost Management) │
        └──────────────────────────────┘
```

---

## 💾 Workspace Creation - Save Templates Directly

**New Feature:** Automatically save infrastructure templates to your VS Code workspace with one click!

When you generate infrastructure templates (Bicep, Terraform, Kubernetes), the extension detects them and offers buttons to save them directly to your workspace with proper folder structure.

### Supported Template Types

| Template Type | Auto-Organization | Generated Files |
|--------------|-------------------|----------------|
| **Bicep** | ✅ Modules in `modules/` subfolder | `main.bicep`, `main.parameters.json`, `modules/`, `README.md` |
| **Terraform** | ✅ Modules in `modules/` subfolder | `main.tf`, `variables.tf`, `outputs.tf`, `providers.tf`, `.gitignore`, `README.md` |
| **Kubernetes** | ✅ Manifests in `manifests/` subfolder | `deployment.yaml`, `service.yaml`, etc. |
| **ARM Templates** | ✅ Parameters separate | `azuredeploy.json`, `parameters.json`, `README.md` |

### How It Works

1. **Ask for a template:**
   ```
   @platform Create a Bicep template for an Azure Storage Account with private endpoint
   ```

2. **Template appears in chat response** with proper syntax highlighting

3. **Click "📁 Create Project in Workspace"** button below the response
   - Prompts for project name
   - Automatically organizes files based on template type
   - Creates README with deployment instructions
   - Opens main template file in editor

4. **Or click "💾 Save Single File"** to save individual files

### Example Workflow: Bicep Project Creation

**Chat Request:**
```
@platform Generate a Bicep template for an AKS cluster with Azure Monitor
```

**Response includes:**
````bicep
// main.bicep
param location string = 'eastus'
param clusterName string = 'aks-dev-001'
...

// modules/monitoring.bicep
resource monitor 'Microsoft.Monitor/accounts@2023-04-03' = {
  name: monitorName
  ...
}
````

**Click "Create Project in Workspace"** → Enter project name: `aks-infrastructure`

**Created structure:**
```
aks-infrastructure/
├── main.bicep
├── main.parameters.json
├── modules/
│   └── monitoring.bicep
└── README.md
```

**README.md includes:**
- Deployment instructions with `az deployment` commands
- Parameter descriptions
- Prerequisites
- Resource naming conventions

### Example: Terraform Project Creation

**Chat Request:**
```
@platform Create Terraform for VNet with 3 subnets and NSGs
```

**Created structure:**
```
vnet-infrastructure/
├── main.tf
├── variables.tf
├── outputs.tf
├── providers.tf
├── .gitignore
├── modules/
│   └── network.tf
└── README.md
```

**README.md includes:**
- `terraform init`, `plan`, `apply` instructions
- Variable descriptions
- Output values
- State management notes

### Template Detection

The extension automatically detects templates by analyzing code blocks:
- **Bicep**: ` ```bicep `
- **Terraform**: ` ```terraform ` or ` ```hcl `
- **Kubernetes**: ` ```yaml ` with `apiVersion:` and `kind:`
- **ARM**: ` ```json ` with `"type": "Microsoft.*"`

### Commands

| Command | Description |
|---------|-------------|
| `Platform Copilot: Create Workspace` | Create multi-file project from templates |
| `Platform Copilot: Save Template` | Save single template file |

### Button Actions

| Button | Action |
|--------|--------|
| 📁 **Create Project in Workspace** | Creates complete project structure |
| 💾 **Save Single File** | Saves individual template file |

---

### Key Components

**1. GitHub Copilot Chat Participant** (`chatParticipant.ts`)
- Handles `@platform` chat requests
- Routes prompts to MCP server
- Formats responses with rich markdown
- Manages conversation context
- Provides action buttons (export, share, Azure Portal links)

**2. MCP HTTP Client** (`services/mcpClient.ts`)
- Axios-based HTTP client
- Request/response interceptors
- Comprehensive error handling
- Typed TypeScript interfaces
- Support for all agent operations:
  - Chat messaging (`/mcp/chat`)
  - Code analysis (`/mcp/analyze-code`)
  - Repository scanning (`/mcp/analyze-repository`)
  - Health checks (`/health`)

**3. MCP Server (Backend - .NET 9)**
- **OrchestratorAgent**: AI-powered intent classification and task routing
- **7 Specialized Agents**: Each with domain expertise and plugins
- **Semantic Kernel Integration**: GPT-4 for natural language understanding
- **Database**: Entity Framework Core with SQL Server/SQLite
- **Dual Protocol Support**: HTTP (port 5100) + stdio (for AI tools)

**4. Configuration Manager** (`config.ts`)
- Centralized VS Code settings management
- Dynamic configuration updates
- Debug logging to Output panel
- API key management (optional)

### Agent Orchestration Flow

1. **User Input**: Natural language request via `@platform` in GitHub Copilot Chat
2. **Extension Processing**: ChatParticipant validates and forwards to MCP server
3. **Intent Classification**: OrchestratorAgent analyzes request using GPT-4
4. **Agent Selection**: Routes to appropriate specialized agent(s)
5. **Execution**: Agent executes using Azure SDK, Semantic Kernel plugins
6. **Response**: Formatted markdown with metadata, action buttons
7. **Follow-up**: Optional follow-up prompts for multi-step workflows

### Recent Fixes (November 2024)

**Fixed:** Duplicate agent registration issue
- **Problem**: Multiple sub-agents (CodeScanningAgent, AtoPreparationAgent, DocumentAgent) were registered as `ISpecializedAgent` with the same `AgentType.Compliance`
- **Impact**: OrchestratorAgent constructor failed with "duplicate key" error
- **Solution**: Only `ComplianceAgent` is registered as `ISpecializedAgent`; sub-agents registered by concrete type
- **Result**: ✅ All 7 agents now load successfully without conflicts

---

## 🔧 Development

### Project Structure

```
platform-engineering-copilot-github/
├── src/
│   ├── extension.ts              # Extension entry point
│   ├── chatParticipant.ts        # GitHub Copilot Chat handler
│   ├── config.ts                 # Configuration management
│   └── services/
│       └── platformApiClient.ts  # MCP HTTP client (legacy name)
├── package.json                  # Extension manifest
├── tsconfig.json                 # TypeScript configuration
├── .env.example                  # Environment template
└── README.md                     # This file
```

### Build Commands

```bash
# Install dependencies
npm install

# Compile TypeScript
npm run compile

# Watch mode (auto-compile on changes)
npm run watch

# Lint code
npm run lint

# Format code
npm run format

# Package extension
npm run package
```

### Testing

```bash
# Press F5 in VS Code to launch Extension Development Host
# Or use the command:
npm test
```

### Debugging

1. Open project in VS Code
2. Press `F5` to launch Extension Development Host
3. Set breakpoints in source code
4. Use GitHub Copilot Chat with `@platform` in the dev window

---

## 📝 Configuration Examples

### Enterprise Setup (Azure Government)

```json
{
  "platform-copilot.apiUrl": "https://mcp.yourdomain.com",
  "platform-copilot.apiKey": "your-production-api-key",
  "platform-copilot.timeout": 120000,
  "platform-copilot.enableLogging": false
}
```

### Local Development

```json
{
  "platform-copilot.apiUrl": "http://localhost:5100",
  "platform-copilot.apiKey": "",
  "platform-copilot.timeout": 60000,
  "platform-copilot.enableLogging": true
}
```

### Testing Environment

```json
{
  "platform-copilot.apiUrl": "https://mcp-dev.yourdomain.com",
  "platform-copilot.apiKey": "dev-api-key",
  "platform-copilot.timeout": 90000,
  "platform-copilot.enableLogging": true
}
```

---

## 🆘 Troubleshooting

### Extension Not Activating

**Problem**: `@platform` participant doesn't appear in GitHub Copilot Chat

**Solutions**:
1. Verify GitHub Copilot Chat extension is installed and enabled
2. Check VS Code version: `code --version` (must be 1.90.0+)
3. Reload VS Code: `Developer: Reload Window` (Cmd+Shift+P)
4. Check extension is enabled: Extensions panel → Platform Engineering Copilot
5. Review Output panel: `View → Output → Platform Engineering Copilot`

### Cannot Reach MCP Server

**Problem**: Extension reports connection errors to MCP server

**Solutions**:
1. **Verify MCP server is running:**
   ```bash
   curl http://localhost:5100/health
   # Expected: {"status":"healthy","mode":"dual (http+stdio)","server":"Platform Engineering Copilot MCP","version":"1.0.0"}
   ```

2. **Start MCP server if not running:**
   ```bash
   # Using Docker (recommended)
   docker-compose -f docker-compose.essentials.yml up -d
   
   # Check container status
   docker ps | grep mcp
   
   # View logs
   docker logs plaform-engineering-copilot-mcp --tail 50
   ```

3. **Check VS Code settings:**
   - Open Settings: `Cmd+,` (Mac) or `Ctrl+,` (Windows/Linux)
   - Search for: `platform-copilot.apiUrl`
   - Verify URL matches MCP server: `http://localhost:5100`

4. **Run health check command:**
   - Open Command Palette: `Cmd+Shift+P`
   - Run: `Platform Copilot: Check Platform API Health`

5. **Check firewall/network:**
   - Ensure port 5100 is not blocked
   - If using remote MCP server, verify network connectivity
   - Check firewall rules on both client and server

### Timeout Errors

**Problem**: Requests timing out, especially for complex operations

**Solutions**:
1. **Increase timeout in settings:**
   ```json
   {
     "platform-copilot.timeout": 120000  // 2 minutes (default: 60s)
   }
   ```

2. **Check MCP server performance:**
   ```bash
   # View CPU/memory usage
   docker stats plaform-engineering-copilot-mcp
   ```

3. **Review server logs for slow operations:**
   ```bash
   docker logs plaform-engineering-copilot-mcp | grep -i "error\|timeout\|slow"
   ```

4. **Verify Azure OpenAI service availability:**
   - Check Azure OpenAI endpoint is responding
   - Verify API key/credentials are valid
   - Check rate limiting and quotas

### Chat Participant Not Responding

**Problem**: `@platform` doesn't respond to prompts

**Solutions**:
1. **Check extension activation:**
   - Extensions panel → Platform Engineering Copilot (should have green checkmark)
   - If not activated, reload: `Developer: Reload Window`

2. **Check Output panel for errors:**
   - `View → Output`
   - Select: `Platform Engineering Copilot` from dropdown
   - Look for error messages or stack traces

3. **Verify GitHub Copilot subscription:**
   - Ensure active GitHub Copilot subscription
   - Check Copilot status: `GitHub Copilot: Sign In/Out`

4. **Test MCP server directly:**
   ```bash
   curl -X POST http://localhost:5100/mcp/chat \
     -H "Content-Type: application/json" \
     -d '{"message":"test","conversationId":"test-123"}'
   ```

5. **Reinstall extension:**
   ```bash
   # Uninstall
   code --uninstall-extension platform-copilot-github
   
   # Reinstall
   code --install-extension platform-copilot-github-1.0.0.vsix
   ```

### Agent-Specific Issues

**Problem**: Specific agent functionality not working (e.g., Compliance, Cost Management)

**Solutions**:
1. **Check agent configuration in MCP server:**
   ```bash
   # View appsettings.json to verify enabled agents
   docker exec plaform-engineering-copilot-mcp cat /app/appsettings.json | jq '.AgentConfiguration'
   ```

2. **Verify all 7 agents loaded:**
   ```bash
   docker logs plaform-engineering-copilot-mcp | grep -i "agent.*enabled\|agent.*initialized"
   # Expected: 7 agents (Infrastructure, Compliance, CostManagement, Environment, Discovery, ServiceCreation, Document)
   ```

3. **Check for dependency injection errors:**
   ```bash
   docker logs plaform-engineering-copilot-mcp | grep -i "error\|exception"
   ```

4. **Restart MCP container:**
   ```bash
   docker-compose -f docker-compose.essentials.yml restart platform-mcp
   ```

### Database Connection Issues

**Problem**: MCP server errors related to database connectivity

**Solutions**:
1. **Check SQL Server container:**
   ```bash
   docker ps | grep sqlserver
   docker logs plaform-engineering-copilot-sqlserver
   ```

2. **Verify connection string:**
   ```bash
   docker exec plaform-engineering-copilot-mcp printenv | grep ConnectionStrings
   ```

3. **Test database connectivity:**
   ```bash
   docker exec plaform-engineering-copilot-sqlserver /opt/mssql-tools/bin/sqlcmd \
     -S localhost -U sa -P 'SupervisorDB123!' -Q "SELECT @@VERSION"
   ```

### Performance Issues

**Problem**: Extension or MCP server responding slowly

**Solutions**:
1. **Check resource usage:**
   ```bash
   docker stats
   ```

2. **Increase Docker resources:**
   - Docker Desktop → Settings → Resources
   - Increase CPUs: 4+ cores recommended
   - Increase Memory: 8GB+ recommended

3. **Enable caching in MCP server:**
   - Execution plan caching is enabled by default
   - Check cache hit rate in logs

4. **Review OpenAI API latency:**
   ```bash
   docker logs plaform-engineering-copilot-mcp | grep "OpenAI\|GPT-4"
   ```

### Debug Mode

**Enable verbose logging for troubleshooting:**

```json
{
  "platform-copilot.enableLogging": true
}
```

Then check Output panel: `View → Output → Platform Engineering Copilot`

**Enable MCP server debug logging:**
```bash
# Edit docker-compose.essentials.yml
environment:
  - Logging__LogLevel__Default=Debug
  
# Restart
docker-compose -f docker-compose.essentials.yml restart platform-mcp
```

---

## 📚 Additional Resources

- [Platform Engineering Copilot Documentation](../../docs/)
- [GitHub Copilot Integration Guide](../../docs/GITHUB-COPILOT-INTEGRATION.md)
- [M365 Copilot Integration Guide](../../docs/M365-COPILOT-INTEGRATION.md)
- [Chat Application Integration](../../docs/CHAT-APPLICATION-INTEGRATION.md)
- [MCP Server Documentation](../../docs/DEVELOPMENT.md)

---

## 🤝 Contributing

Contributions are welcome! Please see the main repository for contribution guidelines.

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details

---

## 🎉 What's New

### Version 1.0.0 (November 2024)

**✅ Major Updates:**
- **Fixed duplicate agent registration bug** - All 7 specialized agents now load correctly
- **Full multi-agent support** - Infrastructure, Compliance, Cost, Environment, Discovery, Service Creation, Document agents
- **Enhanced MCP integration** - Stable HTTP + stdio dual-mode server
- **Complete refactor** - Modern TypeScript architecture aligned with VS Code best practices
- **Improved error handling** - Better error messages and troubleshooting guidance
- **Centralized configuration** - Simplified settings management
- **Rich chat responses** - Formatted markdown with metadata and action buttons
- **Azure Portal integration** - Quick links to resources in Azure Portal

**🏗️ Architecture Improvements:**
- OrchestratorAgent with Semantic Kernel for intelligent task routing
- Entity Framework Core 9.0 with dual database support (SQLite dev, SQL Server prod)
- Comprehensive dependency injection with all services registered
- Docker containerization with docker-compose support
- Database migrations and seed data

**📚 Documentation:**
- Complete README with all 7 agent examples
- Detailed troubleshooting guide
- Architecture diagrams
- Example workflows for each agent

**🔧 Technical Enhancements:**
- TypeScript strict mode enabled
- Better type safety with comprehensive interfaces
- Request/response interceptors for debugging
- Health check commands
- Conversation context management
- Timeout configuration

**✨ Agent Capabilities:**
1. **Infrastructure Agent**: Bicep/Terraform deployment, network topology design, IaC generation
2. **Compliance Agent**: NIST 800-53/171, ISO 27001, SOC 2, ATO documentation, eMASS packages
3. **Cost Management Agent**: Real-time estimation, optimization recommendations, budget forecasting
4. **Environment Agent**: Multi-environment lifecycle, cloning, blue-green deployments
5. **Discovery Agent**: Resource inventory, dependency mapping, orphan detection
6. **Service Creation Agent**: Microservice architecture, DevOps pipelines, service templates
7. **Document Agent**: Technical docs, architecture diagrams, compliance documentation

### Removed (Obsolete)

- ❌ Universal MCP Parser (no longer needed with direct HTTP integration)
- ❌ Legacy MCP Client (replaced with modern Axios-based client)
- ❌ Test commands and debug utilities (replaced with proper health checks)
- ❌ Duplicate agent registrations (fixed registration pattern)

### Known Issues

- **Azure OpenAI function name length**: Some plugin function names exceed 64-character limit
  - Impact: Certain complex operations may fail
  - Workaround: Function names being shortened in next release
  - Tracking: Issue #42

### Upcoming Features (Roadmap)

**Q1 2025:**
- [ ] Security Agent integration (dedicated security operations)
- [ ] Monitoring Agent (Azure Monitor, Application Insights integration)
- [ ] Multi-subscription support
- [ ] Template library browser
- [ ] Cost prediction ML models

**Q2 2025:**
- [ ] CI/CD pipeline generation
- [ ] Infrastructure drift detection
- [ ] Automated compliance remediation execution
- [ ] Policy-as-Code enforcement engine
- [ ] GitOps integration (ArgoCD, Flux)

**Future:**
- [ ] M365 Copilot integration (Teams, Outlook)
- [ ] Slack/Discord bot support
- [ ] Mobile companion app
- [ ] AI-powered cost forecasting
- [ ] Automated architecture review

---

## 📧 Support

For issues, feature requests, and contributions:

- **GitHub Issues**: [platform-engineering-copilot/issues](https://github.com/azurenoops/platform-engineering-copilot/issues)
- **Documentation**: [docs/](../../docs/)
- **Discord Community**: [Join us](https://discord.gg/azurenoops) _(coming soon)_

### Reporting Issues

When reporting issues, please include:
1. VS Code version (`code --version`)
2. Extension version (check Extensions panel)
3. MCP server health check output (`curl http://localhost:5100/health`)
4. Relevant logs from Output panel
5. Steps to reproduce

### Contributing

We welcome contributions! See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

**Development Setup:**
```bash
# Fork and clone the repository
git clone https://github.com/YOUR-USERNAME/platform-engineering-copilot.git
cd platform-engineering-copilot/extensions/platform-engineering-copilot-github

# Install dependencies
npm install

# Make changes and test
npm run compile
code --extensionDevelopmentPath=.

# Submit pull request
```

---

## 🔗 Related Resources

- **Main Documentation**: [Platform Engineering Copilot Docs](../../docs/)
- **GitHub Copilot Integration**: [GITHUB-COPILOT-INTEGRATION.md](../../docs/GITHUB-COPILOT-INTEGRATION.md)
- **M365 Copilot Extension**: [../platform-engineering-copilot-m365/](../platform-engineering-copilot-m365/)
- **MCP Server**: [src/Platform.Engineering.Copilot.Mcp/](../../src/Platform.Engineering.Copilot.Mcp/)
- **Architecture**: [ARCHITECTURE.md](../../docs/ARCHITECTURE.md)
- **Deployment Guide**: [DEPLOYMENT.md](../../docs/DEPLOYMENT.md)
- **Development Guide**: [DEVELOPMENT.md](../../docs/DEVELOPMENT.md)

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

Copyright (c) 2024 Azure NoOps Team

---

## 🙏 Acknowledgments

Built with:
- **Semantic Kernel** - AI orchestration framework
- **Azure OpenAI** - GPT-4 language models
- **GitHub Copilot** - AI pair programmer
- **Model Context Protocol (MCP)** - AI agent communication standard
- **.NET 9** - Modern application platform
- **TypeScript** - Type-safe JavaScript
- **VS Code Extension API** - Rich editor integration

Special thanks to:
- Microsoft Semantic Kernel team
- GitHub Copilot team  
- Azure NoOps community
- All contributors and users

---

## 🌟 Star History

If you find this extension useful, please consider starring the repository!

[![Star History Chart](https://api.star-history.com/svg?repos=azurenoops/platform-engineering-copilot&type=Date)](https://star-history.com/#azurenoops/platform-engineering-copilot&Date)

---

**Built with ❤️ by the Azure NoOps Team**

*Empowering platform engineers with AI-powered Azure infrastructure management*
