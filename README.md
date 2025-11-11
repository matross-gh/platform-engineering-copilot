# Platform Engineering Copilot

> **AI-Powered Infrastructure Provisioning & Governance Platform for Azure Government & Multi-Cloud Environments**

An enterprise-grade platform engineering solution that combines AI-powered natural language interfaces with real-time Azure resource management, compliance automation, and multi-cloud infrastructure orchestration. Built on .NET 9.0 and Microsoft Semantic Kernel, it provides intelligent infrastructure provisioning, ATO compliance scanning, cost optimization, and policy-aware deployment workflows.

---

## 🌟 Overview

The Platform Engineering Copilot transforms cloud infrastructure management through its MCP (Model Context Protocol) Server architecture:

- **🤖 Multi-Agent Orchestration**: 6 specialized AI agents working together for comprehensive infrastructure management
- **🛡️ ATO Compliance Automation**: NIST 800-53 Rev 5 compliance scanning with automated gap analysis and remediation
- **💰 Cost Intelligence**: Real-time cost analysis, budget tracking, and optimization recommendations
- **🔐 Security & Policy Enforcement**: Azure Policy integration with security scanning and vulnerability assessment
- **📊 Real-Time Chat Interface**: Web-based conversational AI for infrastructure operations
- **🎯 Infrastructure as Code**: Generate production-ready Terraform, Bicep, and Kubernetes manifests
- **🔧 MCP Server Integration**: Dual-mode operation (HTTP + stdio) for AI clients and web applications

### MCP Server Architecture

The platform is built on a **Model Context Protocol (MCP) Server** that orchestrates 6 specialized agents:

1. **Infrastructure Agent**: Azure resource provisioning, management, and infrastructure as code generation
2. **Cost Optimization Agent**: Cost analysis, budget management, and savings recommendations
3. **Compliance Agent**: RMF/NIST 800-53 compliance assessment and gap analysis
4. **Security Agent**: Security scanning, vulnerability assessment, and threat detection
5. **Document Agent**: ATO documentation generation and compliance artifact management
6. **ATO Preparation Agent**: End-to-end ATO package orchestration and submission

**Dual-Mode Operation**:
- **HTTP Mode**: REST API for web applications (Chat interface on port 5100)
- **stdio Mode**: Direct integration with AI tools (GitHub Copilot, Claude Desktop)

---

## 🚀 Quick Start

### Prerequisites

- **.NET 9.0 SDK** or later
- **Docker & Docker Compose** (recommended for deployment)
- **Azure Subscription** (Azure Government or Commercial)
- **Azure CLI** (for authentication) - **Required**
- **Azure OpenAI** (gpt-4o deployment recommended)
- **SQL Server** (or Docker container)

### LLM Requirements

The platform uses **Microsoft Semantic Kernel** with the following LLM requirements:

| Component | Required Model | Context Window | Temperature | Notes |
|-----------|---------------|----------------|-------------|-------|
| **Orchestrator** | GPT-4o or GPT-4 Turbo | 128K | 0.0 | Routes requests to specialized agents |
| **Infrastructure Agent** | GPT-4o (recommended) | 128K | 0.2 | Code generation, Azure MCP integration |
| **Compliance Agent** | GPT-4o or GPT-4 Turbo | 128K | 0.0 | NIST 800-53 compliance assessment |
| **Cost Management** | GPT-4o or GPT-4 Turbo | 32K | 0.1 | Cost analysis and forecasting |
| **Discovery Agent** | GPT-4o or GPT-4 Turbo | 32K | 0.0 | Resource inventory and queries |
| **Security Agent** | GPT-4o | 64K | 0.0 | Vulnerability scanning |
| **Document Agent** | GPT-4o or GPT-4 Turbo | 128K | 0.3 | Technical documentation |

**Recommended**: **Azure OpenAI GPT-4o** for best performance and function calling accuracy

**Supported Providers**:
- ✅ **Azure OpenAI** (recommended) - Azure Gov and Commercial
- ⚠️ **OpenAI** - Commercial only, no FedRAMP compliance
- ❌ **Ollama** - Local testing only, limited function calling

**Token Usage**:
- Simple queries: ~150-500 tokens (~$0.002)
- Template generation: ~4000-8000 tokens (~$0.04-$0.08)
- Compliance scans: ~5000-12000 tokens (~$0.06-$0.12)
- Multi-agent workflows: ~10000-30000 tokens (~$0.12-$0.35)

**📖 Complete LLM guide:** [GETTING-STARTED.md - LLM Configuration](./docs/GETTING-STARTED.md#-llm-configuration--model-requirements)

### 1. Clone and Build

```bash
git clone https://github.com/azurenoops/platform-engineering-copilot.git
cd platform-engineering-copilot
dotnet build
```

### 2. Configure Azure Authentication

The application uses **DefaultAzureCredential** - no hardcoded credentials needed!

```bash
# For Azure Government
az cloud set --name AzureUSGovernment
az login
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)

# For Azure Commercial
az cloud set --name AzureCloud
az login

# Set your default subscription (optional)
az account set --subscription "YOUR-SUBSCRIPTION-ID"
```

**📖 See [QUICKSTART-AUTHENTICATION.md](./QUICKSTART-AUTHENTICATION.md) for detailed setup**  
**📖 See [AZURE-AUTHENTICATION.md](./AZURE-AUTHENTICATION.md) for comprehensive authentication guide**

### 3. Configure Environment Variables

Create a `.env` file in the project root:

```bash
cp .env.example .env
```

Edit `.env` with your Azure credentials:

```env
# Azure Configuration
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CLOUD_ENVIRONMENT=AzureGovernment  # or AzureCloud
AZURE_USE_MANAGED_IDENTITY=false
AZURE_ENABLED=true

# Azure OpenAI Configuration
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_ENDPOINT=https://your-endpoint.openai.azure.us/
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_OPENAI_CHAT_DEPLOYMENT=gpt-4o
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-ada-002

# Database
SA_PASSWORD=YourStrongPassword123!
```

**📖 See [DOCKER.md](./DOCKER.md) for complete environment variable documentation**

### 4. Start with Docker Compose

**Option 1: MCP Server Only (Recommended for AI Client Development)**
```bash
docker-compose -f docker-compose.essentials.yml up -d
```

This starts:
- MCP Server (port 5100)
- SQL Server (port 1433)

**Option 2: All Services (Complete Platform)**
```bash
docker-compose up -d
```

This starts:
- MCP Server (port 5100)
- Platform Chat (port 5001) - Web chat interface
- Admin API (port 5002) - Admin backend
- Admin Client (port 5003) - Admin web console
- SQL Server (port 1433)

**📖 See [DOCKER-COMPOSE-GUIDE.md](./DOCKER-COMPOSE-GUIDE.md) for all deployment options**

### 5. Verify Services

```bash
# Check MCP Server health
curl http://localhost:5100/health

# Check all services (if using full deployment)
docker-compose ps
```

### 6. Try Natural Language Queries

**Using the Web Chat Interface** (if running all services):
```bash
open http://localhost:5001
```

**Using REST API**:
```bash
curl -X POST http://localhost:5100/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "test-session",
    "message": "Create storage account data001 in rg-dr in usgovvirginia"
  }'
```

**Using AI Clients** (GitHub Copilot, Claude Desktop):
See [GITHUB-COPILOT-INTEGRATION.md](./docs/GITHUB-COPILOT-INTEGRATION.md) and [M365-COPILOT-INTEGRATION.md](./docs/M365-COPILOT-INTEGRATION.md)

**Example Queries**:
```
"List all resource groups in my subscription"
"Create storage account data001 in resource group rg-dr"
"Run ATO compliance assessment for my subscription"
"Show me cost analysis for the last 30 days"
"Check Azure policies for storage account in usgovvirginia"
```

---

## 🎯 Key Features

### 🤖 Multi-Agent Orchestration (MCP Server)

**6 Specialized AI Agents**:

1. **Infrastructure Agent**
   - Azure resource provisioning and management
   - Infrastructure as Code generation (Terraform, Bicep, Kubernetes)
   - Resource discovery and inventory management
   - Template validation and deployment

2. **Cost Optimization Agent**
   - Real-time cost analysis and breakdowns
   - Budget management and forecasting
   - Savings recommendations (right-sizing, reserved instances)
   - Cost anomaly detection

3. **Compliance Agent**
   - NIST 800-53 Rev 5 compliance assessment
   - FedRAMP High/Moderate/Low baseline validation
   - Gap analysis and remediation plans
   - Control family coverage (18 families, 1000+ controls)

4. **Security Agent**
   - Security scanning and vulnerability assessment
   - Azure Policy integration and enforcement
   - Threat detection and mitigation
   - Security posture monitoring

5. **Document Agent**
   - ATO documentation generation
   - Compliance artifact management
   - Evidence collection and organization
   - Document versioning and templates

6. **ATO Preparation Agent**
   - End-to-end ATO package orchestration
   - Submission workflow management
   - Stakeholder coordination
   - Timeline and milestone tracking

**Agent Coordination**:
- Seamless handoffs between agents based on query intent
- Shared context and session memory
- Parallel execution for complex multi-step workflows
- Unified natural language interface

### 🛡️ ATO Compliance & Governance

**NIST 800-53 Rev 5 Compliance Engine**:
- Automated scanning of 18 control families (1000+ controls)
- FedRAMP High, Moderate, and Low baseline assessments
- Real-time policy evaluation via Azure Policy Insights API
- Risk assessment with POAM generation

**Compliance Features**:
```
✅ AC (Access Control) - 25 controls
✅ AT (Awareness and Training) - 18 controls  
✅ AU (Audit and Accountability) - 16 controls
✅ CA (Assessment, Authorization, and Monitoring) - 9 controls
✅ CM (Configuration Management) - 14 controls
✅ CP (Contingency Planning) - 13 controls
✅ IA (Identification and Authentication) - 12 controls
✅ IR (Incident Response) - 10 controls
✅ MA (Maintenance) - 6 controls
✅ MP (Media Protection) - 8 controls
✅ PE (Physical and Environmental Protection) - 23 controls
✅ PL (Planning) - 11 controls
✅ PM (Program Management) - 16 controls
✅ PS (Personnel Security) - 9 controls
✅ RA (Risk Assessment) - 10 controls
✅ SA (System and Services Acquisition) - 23 controls
✅ SC (System and Communications Protection) - 51 controls
✅ SI (System and Information Integrity) - 23 controls
```

### 💰 Cost Management & Optimization

**Azure Cost Management Integration**:
- Real-time cost analysis with breakdowns by resource group, type, location, and tags
- Budget monitoring with alerts
- Cost forecasting and trend analysis
- Optimization recommendations for right-sizing and reserved instances

**Cost Query Examples**:
```
"Show cost analysis for subscription [id] for last 3 months"
"Show budget status for subscription [id]"
"Provide optimization recommendations for production resources"
```

### 🔐 Azure Policy Integration

**Policy-Aware Infrastructure**:
- Real-time policy evaluation before deployment
- Database-backed approval workflows for policy exceptions
- Severity-based decisions (Critical, High, Medium, Low)
- 5-minute intelligent caching for performance optimization

**Policy Workflow**:
1. **Pre-Deployment Validation**: Evaluate proposed resources against active policies
2. **Approval Workflows**: Policy violations trigger approval requests (stored in database)
3. **Exception Management**: Time-bounded exceptions with justifications and mitigations
4. **Audit Trail**: Complete history of approvals, rejections, and changes

---

## 🏗️ Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────┐
│                    CLIENT LAYER                         │
├─────────────────────────────────────────────────────────┤
│  • Platform Chat (Web UI) - Port 5001                   │
│  • Admin Console (Web UI) - Port 5003                   │
│  • GitHub Copilot (stdio)                               │
│  • Claude Desktop (stdio)                               │
│  • Custom AI Clients (stdio/HTTP)                       │
└─────────────────────────────────────────────────────────┘
                       ↓ HTTP / stdio
┌─────────────────────────────────────────────────────────┐
│              MCP SERVER (Port 5100)                     │
├─────────────────────────────────────────────────────────┤
│  Platform.Engineering.Copilot.Mcp                       │
│  ├── Dual-Mode Operation                                │
│  │   ├── HTTP Mode (Web Apps)                           │
│  │   └── stdio Mode (AI Clients)                        │
│  └── Multi-Agent Orchestrator                           │
│      ├── Infrastructure Agent                           │
│      ├── Cost Optimization Agent                        │
│      ├── Compliance Agent                               │
│      ├── Security Agent                                 │
│      ├── Document Agent                                 │
│      └── ATO Preparation Agent                          │
└─────────────────────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────┐
│                 BUSINESS LOGIC LAYER                    │
├─────────────────────────────────────────────────────────┤
│  Platform.Engineering.Copilot.Core                      │
│  ├── Semantic Kernel Integration                        │
│  ├── Agent Plugins & Tools                              │
│  │   ├── InfrastructurePlugin (Azure ARM)               │
│  │   ├── CompliancePlugin (NIST 800-53)                 │
│  │   ├── CostManagementPlugin (Cost Analysis)           │
│  │   ├── SecurityPlugin (Policy & Scanning)             │
│  │   └── DocumentPlugin (ATO Docs)                      │
│  └── Domain Services                                    │
│      ├── InfrastructureProvisioningService              │
│      ├── RmfComplianceEngine (NIST 800-53)              │
│      ├── CostOptimizationEngine                         │
│      ├── SecurityScanningService                        │
│      └── AtoDocumentationService                        │
└─────────────────────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────┐
│                 DATA LAYER                              │
├─────────────────────────────────────────────────────────┤
│  Platform.Engineering.Copilot.Data                      │
│  • Entity Framework Core 9.0                            │
│  • McpContext, ChatContext, AdminContext                │
│  • SQL Server 2022                                      │
│  • Databases: McpDb, ChatDb, AdminDb                    │
└─────────────────────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────┐
│              EXTERNAL SERVICES                          │
├─────────────────────────────────────────────────────────┤
│  • Azure Resource Manager (Azure Government/Commercial) │
│  • Azure Policy Insights API                            │
│  • Azure Cost Management API                            │
│  • Azure OpenAI (gpt-4o deployment)                     │
│  • Azure Storage, Key Vault, SQL Database               │
└─────────────────────────────────────────────────────────┘
```

### Dependency Hierarchy

```
Level 0 (Foundation):
└── Data (NO DEPENDENCIES - Isolated)

Level 1 (Core Business Logic):
└── Core ──→ Data

Level 2 (MCP Server):
└── Mcp ──→ Core ──→ Data

Level 3 (Web Applications):
├── Chat ──→ Core, Data (connects to MCP via HTTP)
├── Admin.API ──→ Core, Data
└── Admin.Client ──→ Admin.API (Web UI)
```

### MCP Server Dual-Mode Architecture

```
┌─────────────────────────────────────────┐
│         MCP Server Process              │
├─────────────────────────────────────────┤
│                                         │
│  ┌───────────────────────────────────┐ │
│  │   HTTP Mode (Port 5100)           │ │
│  │   - Web Chat Interface            │ │
│  │   - REST API Endpoints            │ │
│  │   - Health Checks                 │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │   stdio Mode                      │ │
│  │   - GitHub Copilot Integration    │ │
│  │   - Claude Desktop Integration    │ │
│  │   - Custom AI Clients             │ │
│  └───────────────────────────────────┘ │
│                                         │
│         Multi-Agent Core                │
│  ┌─────────────────────────────────┐   │
│  │ • Infrastructure Agent          │   │
│  │ • Cost Optimization Agent       │   │
│  │ • Compliance Agent              │   │
│  │ • Security Agent                │   │
│  │ • Document Agent                │   │
│  │ • ATO Preparation Agent         │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

---

## 📚 Documentation

### Getting Started
- **[QUICKSTART-AUTHENTICATION.md](./docs/QUICKSTART-AUTHENTICATION.md)** - Azure authentication quick start
- **[AZURE-AUTHENTICATION.md](./docs/AZURE-AUTHENTICATION.md)** - Comprehensive authentication guide
- **[DOCKER.md](./DOCKER.md)** - Docker configuration and deployment guide
- **[DOCKER-COMPOSE-GUIDE.md](./DOCKER-COMPOSE-GUIDE.md)** - Docker Compose configuration reference
- **[DEVELOPMENT.md](./docs/DEVELOPMENT.md)** - Development setup and workflows

### Integration Guides
- **[GITHUB-COPILOT-INTEGRATION.md](./docs/GITHUB-COPILOT-INTEGRATION.md)** - GitHub Copilot MCP server integration
- **[M365-COPILOT-INTEGRATION.md](./docs/M365-COPILOT-INTEGRATION.md)** - Microsoft 365 Copilot declarative agent

### Architecture & Design
- **[ARCHITECTURE.md](./docs/ARCHITECTURE.md)** - Detailed system architecture and data flows
- **[DEPLOYMENT.md](./docs/DEPLOYMENT.md)** - Cloud deployment instructions (Azure, Kubernetes)

### Testing & Quality
- **[NATURAL-LANGUAGE-TEST-CASES.md](./docs/NATURAL-LANGUAGE-TEST-CASES.md)** - Comprehensive test scenarios

---

## 🛠️ Technology Stack

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 9.0 | Primary framework |
| **Microsoft Semantic Kernel** | 1.26.0 | AI orchestration |
| **Entity Framework Core** | 9.0.0 | Data access |
| **Azure SDK** | 1.48.0+ | Azure resource management |
| **SignalR** | 9.0.0 | Real-time communication |

### AI & ML
| Technology | Purpose |
|------------|---------|
| **Azure OpenAI (GPT-4o)** | Natural language understanding |
| **Semantic Kernel** | AI plugin orchestration |
| **Intent Classification** | Query routing to specialized plugins |
| **Context Management** | Conversation memory across sessions |

### Azure Integration
| Service | Purpose |
|---------|---------|
| **Azure Resource Manager** | Infrastructure provisioning |
| **Azure Policy Insights API** | Policy evaluation |
| **Azure Cost Management API** | Cost analysis |
| **Azure Storage** | Blob storage, file storage |
| **Azure Key Vault** | Secrets management |
| **Azure SQL Database** | Production data storage |

### Infrastructure
| Technology | Purpose |
|------------|---------|
| **Terraform** | Multi-cloud IaC |
| **Bicep** | Azure-native IaC |
| **Kubernetes** | Container orchestration |
| **Docker** | Containerization |

---

## 🎯 Use Cases

### 1. Infrastructure Provisioning via Natural Language
```
User: "Create storage account data001 in resource group rg-dr in usgovvirginia"

Infrastructure Agent:
✅ Validates Azure subscription and authentication
✅ Checks if resource group exists (creates if needed)
✅ Creates storage account with security defaults:
   - HTTPS-only traffic
   - TLS 1.2 minimum
   - Encryption at rest enabled
   - Standard_LRS SKU
✅ Returns resource ID and deployment status
```

### 2. Compliance Assessment & Gap Analysis
```
User: "Run NIST 800-53 compliance assessment for my subscription"

Compliance Agent:
✅ Scans all 18 NIST 800-53 Rev 5 control families
✅ Evaluates 1000+ controls against Azure resources
✅ Performs gap analysis identifying missing controls
✅ Generates compliance report with:
   - Overall compliance score
   - Critical/High/Medium/Low findings
   - Gap analysis with remediation roadmap
   - Risk assessment and POAM generation
✅ Stores assessment for audit trail
```

### 3. Cost Analysis & Optimization
```
User: "Show cost overview for last 30 days with optimization recommendations"

Cost Optimization Agent:
✅ Retrieves cost data from Azure Cost Management API
✅ Provides cost dashboard with:
   - Total monthly spend + month-over-month comparison
   - Top 5 services by cost with percentage breakdown
   - Daily cost trends
   - Breakdown by service, resource group, location, tags
✅ Analyzes usage patterns
✅ Recommends cost optimizations:
   - Right-sizing opportunities
   - Reserved instance recommendations
   - Idle resource identification
```

### 4. Security Scanning & Policy Validation
```
User: "Scan my infrastructure for security vulnerabilities and policy violations"

Security Agent:
✅ Calls Azure Policy Insights API
✅ Evaluates resources against active policies
✅ Performs security scanning
✅ Returns findings with:
   - Severity levels (Critical/High/Medium/Low)
   - Policy violations and definitions
   - Security vulnerabilities
   - Remediation guidance
✅ Triggers approval workflows for exceptions
```

### 5. ATO Documentation Generation
```
User: "Generate System Security Plan for my production environment"

Document Agent + ATO Preparation Agent:
✅ Collects environment configuration and evidence
✅ Generates ATO documentation artifacts:
   - System Security Plan (SSP)
   - Security Assessment Report (SAR)
   - Plan of Action & Milestones (POAM)
   - Control Implementation Summary
✅ Organizes evidence collection
✅ Tracks ATO submission progress
```

---

## 🔐 Security Features

### Authentication & Authorization
- **Azure DefaultAzureCredential**: Managed Identity, Service Principal, Azure CLI, and interactive auth
- **Azure Government Cloud Support**: Native support for Azure US Government
- **RBAC**: Role-based access control for all operations
- **Azure Key Vault Integration**: Secure secrets management

### Compliance & Governance
- **NIST 800-53 Rev 5**: Complete control family implementation (18 families, 1000+ controls)
- **FedRAMP Baselines**: High, Moderate, and Low compliance validation
- **RMF Framework**: Full Risk Management Framework support
- **Gap Analysis**: Automated compliance gap identification and remediation planning
- **Azure Policy**: Real-time policy evaluation and enforcement
- **Audit Logging**: Complete audit trail with change tracking

### Data Security
- **Encryption at Rest**: All database data encrypted
- **Encryption in Transit**: TLS 1.2+ for all communications
- **Secure Configuration**: Security defaults for all provisioned resources
- **Secrets Management**: Azure Key Vault integration for all credentials

---

## 📊 Key Capabilities

### Infrastructure Management (Infrastructure Agent)
✅ Create and manage Azure resources via natural language
✅ List resource groups across subscriptions  
✅ Create storage accounts with security defaults  
✅ Provision virtual networks with subnets  
✅ Deploy Key Vaults with RBAC and policies
✅ Generate Infrastructure as Code (Terraform, Bicep, Kubernetes)
✅ Template validation and deployment  
✅ Auto-create resource groups with managed tags  

### Compliance & Governance (Compliance Agent)
✅ NIST 800-53 Rev 5 scanning (18 control families, 1000+ controls)  
✅ FedRAMP High/Moderate/Low baseline assessments  
✅ **Gap Analysis**: Identify missing controls and compliance gaps
✅ Remediation plan generation with prioritized actions
✅ POAM (Plan of Action & Milestones) creation  
✅ eMASS package generation  
✅ Risk assessment and scoring  
✅ Control mapping and evidence collection

### Cost Optimization (Cost Optimization Agent)
✅ **Cost Overview Dashboard**: Total spend, top services, daily trends
✅ Real-time cost analysis with multi-dimensional breakdowns  
✅ Budget tracking and forecasting  
✅ Cost anomaly detection  
✅ Right-sizing recommendations  
✅ Reserved instance analysis
✅ Idle resource identification
✅ Cost allocation by tags, resource groups, and locations

### Security & Policy (Security Agent)
✅ Security vulnerability scanning
✅ Azure Policy evaluation and enforcement
✅ Threat detection and mitigation
✅ Security posture assessment
✅ Compliance violation identification
✅ Policy exception workflows
✅ Security best practices validation

### Documentation & ATO (Document Agent + ATO Preparation Agent) - Still in Development
✅ System Security Plan (SSP) generation
✅ Security Assessment Report (SAR) creation
✅ POAM documentation
✅ Control Implementation Summary
✅ Evidence collection and organization
✅ ATO package orchestration
✅ Submission workflow management
✅ Stakeholder coordination
✅ Reserved instance analysis  

---

## 🚀 Deployment Options

### Quick Start with Docker Compose

**Option 1: MCP Server Only (Essentials)**
```bash
# Start MCP Server + SQL Server
docker-compose -f docker-compose.essentials.yml up -d

# Verify health
curl http://localhost:5100/health

# Connect with AI clients (GitHub Copilot, Claude Desktop)
# See GITHUB-COPILOT-INTEGRATION.md for configuration
```

**Option 2: Full Platform (All Services)**
```bash
# Start all services
docker-compose up -d

# Access web interfaces
open http://localhost:5001  # Chat Interface
open http://localhost:5003  # Admin Console

# Verify all services
docker-compose ps
```

**Development Mode (Hot Reload)**
```bash
# MCP Server only with hot reload
docker-compose -f docker-compose.essentials.yml -f docker-compose.dev.yml up -d

# All services with hot reload
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

**Production Mode (Scaling + Resource Limits)**
```bash
# Production deployment with 2 replicas
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# With reverse proxy
docker-compose -f docker-compose.yml -f docker-compose.prod.yml --profile proxy up -d
```

**📖 See [DOCKER-COMPOSE-GUIDE.md](./DOCKER-COMPOSE-GUIDE.md) for all configuration options**  
**📖 See [DOCKER.md](./DOCKER.md) for comprehensive Docker documentation**

### Cloud Deployment

**Azure Container Instances**
```bash
# See DEPLOYMENT.md for complete instructions
az container create \
  --resource-group rg-platform-copilot \
  --name platform-mcp \
  --image platform-engineering-copilot-mcp:latest \
  --cpu 2 --memory 4 \
  --ports 5100
```

**Azure Kubernetes Service (AKS)**
```bash
kubectl apply -f infra/k8s/
```

**Azure Container Apps**
```bash
# Recommended for production
# See DEPLOYMENT.md for configuration
```

**📖 See [DEPLOYMENT.md](./docs/DEPLOYMENT.md) for complete Azure deployment guide**

---

## 🔌 Integration Options

### GitHub Copilot Integration
Connect the MCP Server to GitHub Copilot for AI-powered infrastructure operations directly in VS Code.

**Configuration**: Add to `.github/copilot/config.json`
```json
{
  "mcp": {
    "servers": {
      "platform-engineering-copilot": {
        "command": "docker",
        "args": ["exec", "-i", "plaform-engineering-copilot-mcp", 
                 "dotnet", "run", "--project", "/app/Platform.Engineering.Copilot.Mcp.csproj"]
      }
    }
  }
}
```

**📖 See [GITHUB-COPILOT-INTEGRATION.md](./docs/GITHUB-COPILOT-INTEGRATION.md) for complete setup**

### Claude Desktop Integration
```json
{
  "mcpServers": {
    "platform-engineering-copilot": {
      "command": "docker",
      "args": ["exec", "-i", "plaform-engineering-copilot-mcp", 
               "dotnet", "run", "--project", "/app/Platform.Engineering.Copilot.Mcp.csproj"]
    }
  }
}
```

### Web Chat Interface
Built-in web chat interface connects to MCP Server via HTTP on port 5100.
```bash
# Start full platform
docker-compose up -d

# Access chat
open http://localhost:5001
```

---

## 🤝 Contributing

Contributions welcome! Please:
1. Read the [ARCHITECTURE.md](./docs/ARCHITECTURE.md) guide
2. Check existing [documentation](./docs/DEVELOPMENT.md)
3. Submit pull requests with tests
4. Follow existing code patterns

---

## 📄 License

Copyright © 2025 Microsoft Federal

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🔗 Resources

- **[Azure Government Documentation](https://docs.microsoft.com/en-us/azure/azure-government/)**
- **[NIST 800-53 Rev 5](https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final)**
- **[RMF Framework](https://csrc.nist.gov/projects/risk-management/about-rmf)**
- **[FedRAMP Compliance](https://www.fedramp.gov/)**
- **[Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)**
- **[Model Context Protocol](https://modelcontextprotocol.io/)**
- **[Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/)**

---

**Built with ❤️ for platform engineers working in secure government cloud environments**

**Version**: 0.1.23  
**Last Updated**: October 29, 2025  
**Maintained by**: Micrsoft Federal CSU Platform Engineering Team

---

## 📞 Support

For issues, questions, or contributions:
- **GitHub Issues**: [Report bugs or request features](https://github.com/azurenoops/platform-engineering-copilot/issues)
- **Documentation**: [Complete documentation index](./docs/DEVELOPMENT.md)
- **Architecture**: [System design guide](./docs/ARCHITECTURE.md)
- **Docker Guide**: [Docker deployment options](./DOCKER-COMPOSE-GUIDE.md)

---

## ✨ What's New in 2.1

### MCP Server Architecture
- 🔧 **Model Context Protocol Server**: Dual-mode operation (HTTP + stdio)
- 🤖 **Multi-Agent Orchestration**: 6 specialized AI agents
- 🔌 **AI Client Integration**: GitHub Copilot and Claude Desktop support
- 📦 **Flexible Deployment**: Essentials (MCP only) and Full platform options

### Enhanced Features
- 📊 **Gap Analysis**: Automated compliance gap identification in Compliance Agent
- 💰 **Cost Overview Dashboard**: Comprehensive cost visibility in Cost Optimization Agent
- 🛡️ **NIST 800-53 Rev 5**: Complete implementation with 18 control families
- 🔐 **RMF Framework**: Full Risk Management Framework support
- 📝 **ATO Documentation**: Automated SSP, SAR, and POAM generation

### Deployment Improvements
- 🐳 **Docker Compose Configurations**: Essentials vs Full platform options
- 📖 **Enhanced Documentation**: New guides for Docker, deployment, and integration
- 🚀 **Production Ready**: Scaling, resource limits, and health checks
- **Entity Framework Core** - ORM
- **SQLite** - Development database
- **SignalR** - Real-time chat

### Frontend
- **React 18** - UI framework
- **Tailwind CSS** - Styling
- **Monaco Editor** - Code editor
- **Axios** - HTTP client

### Infrastructure
- **Terraform** - AWS/GCP provisioning
- **Bicep** - Azure provisioning
- **Kubernetes** - Container orchestration
- **Docker** - Containerization

### AI/ML
- **Model Context Protocol (MCP)** - AI tool integration
- **Natural Language Processing** - Parameter extraction
- **Intent Classification** - Workflow routing

## 📚 Advanced Topics

### Authentication & Security
- **[Quick Start Authentication](./QUICKSTART-AUTHENTICATION.md)** - Get started with Azure authentication in 30 seconds
- **[Azure Authentication Guide](./AZURE-AUTHENTICATION.md)** - Comprehensive authentication architecture and setup
- **[Authentication Cheat Sheet](./CHEATSHEET-AUTHENTICATION.md)** - Quick reference for developers
- **[Authentication Docs Summary](./AUTHENTICATION-DOCS.md)** - Navigation hub for all auth documentation

### Long-Running Operations
- **[Long-Running Tasks Architecture](./LONG-RUNNING-TASKS.md)** - Complete async job pattern design
- **[Long-Running Tasks Summary](./LONG-RUNNING-TASKS-SUMMARY.md)** - Implementation guide and API usage
- **[Long-Running Tasks Cheat Sheet](./CHEATSHEET-LONG-RUNNING-TASKS.md)** - Quick reference for async operations

### Testing & Development
- **[Natural Language Test Cases](./NATURAL-LANGUAGE-TEST-CASES.md)** - 50+ test scenarios for multi-agent system

### For Developers
- **[Generic ServiceCreation Framework](./docs/GENERIC-ServiceCreation-FRAMEWORK.md)** - Build custom ServiceCreation workflows
- **[Generator Architecture](./docs/ARCHITECTURE.md#generator-architecture)** - Create new generators
- **[Implementation Roadmap](./docs/IMPLEMENTATION-ROADMAP.md)** - Development roadmap

### For DevOps
- **[Docker Deployment](./DEPLOYMENT.md)** - Container deployment guide
- **[Networking Configuration](./docs/NETWORKING-CONFIGURATION-GUIDE.md)** - VNet/VPC setup
- **[Monitoring Setup](./docs/MONITORING-ENABLEMENT-GUIDE.md)** - Observability configuration

### For Security Engineers
- **[Zero Trust Guide](./docs/ZERO-TRUST-SECURITY-GUIDE.md)** - Zero Trust implementation
- **[Security Testing](./docs/ZERO-TRUST-TESTING-RESULTS.md)** - Security validation results

## 🤝 Contributing

Contributions welcome! Please:
1. Read the [Architecture Guide](./docs/ARCHITECTURE.md)
2. Check existing [documentation](./docs/INDEX.md)
3. Submit pull requests with tests
4. Follow existing code patterns

## � License

Copyright © 2025 Microsoft Federal

---

**Maintained by**: Micrsoft Federal CSU Platform Engineering Team
**Last Updated**: October 29, 2025  
**Documentation**: [Complete Index](./docs/INDEX.md)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Links

- **[Azure Government Documentation](https://docs.microsoft.com/en-us/azure/azure-government/)**
- **[Model Context Protocol Specification](https://modelcontextprotocol.io/)**
- **[FedRAMP Compliance Guidelines](https://www.fedramp.gov/)**
- **[Platform Engineering Best Practices](https://platformengineering.org/)**

---

*Built with ❤️ for platform engineers working in secure government cloud environments*

## 🎯 What This Does

- **🏗️ Infrastructure**: Create Azure resources, deploy with Terraform/Bicep
- **🐳 Containers**: Deploy to Kubernetes, build Docker images  
- **🛡️ Security**: Vulnerability scanning, ATO compliance checks
- **📊 Monitoring**: Create dashboards, setup alerts
- **🚀 Applications**: Deploy apps with approval workflows

## 🗣️ Chat with AI

Use natural language in VS Code Copilot Chat:

```
@platform provision infrastructure for a web app with database
@mission-owner deploy my application to staging environment  
@platform run security scan on container "myapp:latest"
@mission-owner check if my resources are ATO compliant
```

## 🏗️ Architecture

**Dual MCP Servers:**
- **Platform Server (8080)**: Infrastructure, containers, monitoring, security
- **Mission Owner Server (8081)**: Application deployment, ATO compliance, governance

**VS Code Extension:**  
- Chat participants: `@platform`
- 20+ commands for platform engineering operations
- Multi-cloud Azure authentication + GitHub integration

---

**🎉 Ready to get started? [Read the complete documentation](DOCUMENTATION.md) for everything you need!**