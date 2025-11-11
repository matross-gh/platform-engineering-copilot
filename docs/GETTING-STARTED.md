# Getting Started with Platform Engineering Copilot

> **Complete setup guide from zero to running system in 15 minutes**  
> **Last Updated:** November 2025

---

## 📋 Table of Contents

1. [Quick Start (5 Minutes)](#quick-start-5-minutes)
2. [Development Setup](#development-setup)
3. [Docker Deployment](#docker-deployment)
4. [Production Deployment](#production-deployment)
5. [AI Client Integration](#ai-client-integration)
6. [Verification & Testing](#verification--testing)
7. [Next Steps](#next-steps)
8. [Troubleshooting](#troubleshooting)

---

## 🚀 Quick Start (5 Minutes)

Choose your path:

### Option A: Docker (Recommended for First-Time Users)

```bash
# 1. Clone repository
git clone https://github.com/azurenoops/platform-engineering-copilot.git
cd platform-engineering-copilot

# 2. Copy environment template
cp .env.example .env

# 3. Edit .env with your Azure credentials (see AUTHENTICATION.md)
# Required: AZURE_SUBSCRIPTION_ID, AZURE_TENANT_ID, AZURE_OPENAI_API_KEY

# 4. Start services
docker compose -f docker-compose.essentials.yml up -d

# 5. Verify
curl http://localhost:5100/health
```

**You're running!** MCP Server is at `http://localhost:5100`

### Option B: Local Development (.NET)

```bash
# 1. Prerequisites: .NET 9.0 SDK, Node.js 18+
dotnet --version  # Should show 9.0.x
node --version    # Should show 18.x or higher

# 2. Clone and restore
git clone https://github.com/azurenoops/platform-engineering-copilot.git
cd platform-engineering-copilot
dotnet restore

# 3. Setup database (SQLite - no Docker required)
dotnet tool update --global dotnet-ef
cd src/Platform.Engineering.Copilot.Data
dotnet ef database update
cd ../..

# 4. Configure Azure authentication
az login  # See AUTHENTICATION.md for details

# 5. Run MCP Server
cd src/Platform.Engineering.Copilot.Mcp
dotnet run
```

**You're running!** MCP Server is at `http://localhost:5100`

---

## 🛠️ Development Setup

### Prerequisites

| Component | Version | Required | Notes |
|-----------|---------|----------|-------|
| **.NET SDK** | 9.0+ | ✅ Yes | [Download](https://dotnet.microsoft.com/download) |
| **Node.js** | 18 LTS+ | ✅ Yes | For React frontends |
| **Docker Desktop** | Latest | ⚠️ Recommended | For full stack development |
| **VS Code** | Latest | ⚠️ Recommended | With C# Dev Kit extension |
| **Azure CLI** | 2.50+ | ⚠️ Recommended | For Azure authentication |
| **Git** | 2.0+ | ✅ Yes | Version control |

### Step 1: Clone and Install

```bash
# Clone repository
git clone https://github.com/azurenoops/platform-engineering-copilot.git
cd platform-engineering-copilot

# Restore .NET dependencies
dotnet restore Platform.Engineering.Copilot.sln

# Build solution
dotnet build Platform.Engineering.Copilot.sln

# Run tests
dotnet test Platform.Engineering.Copilot.sln

# Install frontend dependencies
npm install --prefix src/Platform.Engineering.Copilot.Chat/ClientApp
npm install --prefix src/Platform.Engineering.Copilot.Admin.Client/ClientApp
```

### Step 2: Database Setup

**SQLite (Default - Zero Configuration)**

```bash
# Install EF Core tools
dotnet tool update --global dotnet-ef

# Run migrations
cd src/Platform.Engineering.Copilot.Data
dotnet ef database update
cd ../..
```

This creates `platform_engineering_copilot_management.db` in the repository root.

**SQL Server (Optional - Team Environments)**

```bash
# Start SQL Server via Docker
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name platform-sql -d mcr.microsoft.com/mssql/server:2022-latest

# Update connection string in appsettings.Development.json
# Then run migrations
cd src/Platform.Engineering.Copilot.Data
dotnet ef database update \
  --connection "Server=localhost,1433;Database=PlatformCopilot;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
cd ../..
```

### Step 3: Azure Authentication

**Development (User Identity)**

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "your-subscription-id"

# Verify
az account show
```

**Production (Managed Identity)**

See **[AUTHENTICATION.md](./AUTHENTICATION.md)** for production setup with managed identity.

### Step 4: Configuration

Create `src/Platform.Engineering.Copilot.Mcp/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Azure": {
    "SubscriptionId": "your-subscription-id",
    "TenantId": "your-tenant-id",
    "CloudEnvironment": "AzureCloud"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=../../../platform_engineering_copilot_management.db"
  },
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

### Step 5: Run Services

**MCP Server Only**

```bash
cd src/Platform.Engineering.Copilot.Mcp
dotnet run
# Runs on http://localhost:5100
```

**Full Stack (All Services)**

Terminal 1 - MCP Server:
```bash
cd src/Platform.Engineering.Copilot.Mcp
dotnet run  # Port 5100
```

Terminal 2 - Chat Service:
```bash
cd src/Platform.Engineering.Copilot.Chat
dotnet run  # Port 5001
```

Terminal 3 - Admin API:
```bash
cd src/Platform.Engineering.Copilot.Admin.API
dotnet run  # Port 5002
```

Terminal 4 - Admin Client:
```bash
cd src/Platform.Engineering.Copilot.Admin.Client
dotnet run  # Port 5003
```

**Or use the convenience scripts:**

```bash
# Start MCP only
./scripts/start-mcp.sh

# Start Chat + MCP
./scripts/start-chat.sh

# Start everything
./scripts/start-all.sh
```

---

## 🐳 Docker Deployment

### Quick Docker Start

```bash
# 1. Copy environment template
cp .env.example .env

# 2. Edit .env with your credentials
# Required variables:
#   - AZURE_SUBSCRIPTION_ID
#   - AZURE_TENANT_ID
#   - AZURE_OPENAI_API_KEY
#   - AZURE_OPENAI_ENDPOINT

# 3. Start essentials (MCP + Database)
docker compose -f docker-compose.essentials.yml up -d

# 4. View logs
docker compose logs -f platform-mcp

# 5. Verify
curl http://localhost:5100/health
```

### Full Stack Docker

```bash
# Start all services (MCP, Chat, Admin, Database)
docker compose up -d

# View all logs
docker compose logs -f

# Check status
docker compose ps
```

### Docker Compose Options

**Essentials (MCP + SQL Server)**
- `docker-compose.essentials.yml` - MCP Server and database only
- Best for: AI client development, MCP protocol testing

**Development (Hot Reload)**
- `docker-compose.dev.yml` - Hot reload enabled
- Combine with: `-f docker-compose.yml -f docker-compose.dev.yml`
- Best for: Active development, rapid iteration

**Production (Optimized)**
- `docker-compose.prod.yml` - Production optimizations
- Combine with: `-f docker-compose.yml -f docker-compose.prod.yml`
- Best for: Production deployments, performance testing

**All Services**
- `docker-compose.yml` - Complete platform
- Best for: Full feature testing, demo environments

### Docker Management

```bash
# View logs
docker compose logs -f platform-mcp

# Restart service
docker compose restart platform-mcp

# Stop all
docker compose down

# Stop and remove volumes
docker compose down -v

# Rebuild specific service
docker compose build platform-mcp
docker compose up -d platform-mcp

# Scale services
docker compose up -d --scale platform-mcp=3
```

**📖 Complete Docker guide:** [DEPLOYMENT.md](./DEPLOYMENT.md#docker-deployment)

---

## ☁️ Production Deployment

### Azure Container Apps (Recommended)

```bash
# 1. Create Container Apps environment
az containerapp env create \
  --name platform-copilot-env \
  --resource-group platform-copilot-rg \
  --location eastus

# 2. Deploy MCP Server
az containerapp create \
  --name platform-mcp \
  --resource-group platform-copilot-rg \
  --environment platform-copilot-env \
  --image your-registry/platform-mcp:latest \
  --target-port 5100 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 10

# 3. Deploy Chat Service (optional)
az containerapp create \
  --name platform-chat \
  --resource-group platform-copilot-rg \
  --environment platform-copilot-env \
  --image your-registry/platform-chat:latest \
  --target-port 5001 \
  --ingress external \
  --env-vars McpServer__BaseUrl=https://platform-mcp.yourdomain.com
```

### Kubernetes (AKS)

```bash
# 1. Create AKS cluster
az aks create \
  --resource-group platform-copilot-rg \
  --name platform-copilot-aks \
  --node-count 3 \
  --enable-addons monitoring

# 2. Get credentials
az aks get-credentials \
  --resource-group platform-copilot-rg \
  --name platform-copilot-aks

# 3. Deploy
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/deployments.yaml
kubectl apply -f k8s/services.yaml
kubectl apply -f k8s/ingress.yaml
```

### Azure Container Instances (Simple)

```bash
# Deploy MCP Server
az container create \
  --resource-group platform-copilot-rg \
  --name platform-mcp \
  --image your-registry/platform-mcp:latest \
  --ports 5100 \
  --environment-variables ASPNETCORE_ENVIRONMENT=Production \
  --cpu 2 \
  --memory 4
```

**📖 Complete deployment guide:** [DEPLOYMENT.md](./DEPLOYMENT.md)

---

## 🤖 AI Client Integration

### GitHub Copilot (VS Code Extension)

```bash
# 1. Install extension
# Navigate to extensions/ platform-engineering-copilot-github
cd extensions/platform-engineering-copilot-github

# 2. Build and install
npm install
npm run compile
code --install-extension platform-engineering-copilot-github-0.1.0.vsix

# 3. Configure (VS Code Settings)
{
  "platformCopilot.mcpServer.url": "http://localhost:5100",
  "platformCopilot.azure.subscriptionId": "your-subscription-id"
}

# 4. Use in VS Code
@platform Show me all VMs in resource group rg-prod
```

**📖 Complete integration guide:** [INTEGRATIONS.md](./INTEGRATIONS.md#github-copilot-integration)

### Claude Desktop (MCP stdio)

```json
// ~/Library/Application Support/Claude/claude_desktop_config.json (macOS)
{
  "mcpServers": {
    "platform-copilot": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/platform-engineering-copilot/src/Platform.Engineering.Copilot.Mcp"
      ],
      "env": {
        "AZURE_SUBSCRIPTION_ID": "your-subscription-id",
        "AZURE_TENANT_ID": "your-tenant-id"
      }
    }
  }
}
```

**📖 Complete MCP integration:** [INTEGRATIONS.md](./INTEGRATIONS.md#mcp-protocol-integration)

### M365 Copilot

```bash
# 1. Install extension
cd extensions/platform-engineering-copilot-m365
npm install
npm run build

# 2. Deploy to Teams
# Upload to Teams App Studio
# Configure permissions

# 3. Use in Teams/M365
@PlatformCopilot analyze costs for subscription xyz
```

**📖 Complete M365 guide:** [INTEGRATIONS.md](./INTEGRATIONS.md#m365-copilot-integration)

---

## ✅ Verification & Testing

### Health Checks

```bash
# MCP Server
curl http://localhost:5100/health
# Expected: {"status":"Healthy","results":{...}}

# Chat Service (if running)
curl http://localhost:5001/health

# Admin API (if running)
curl http://localhost:5002/health

# Admin Client (if running)
curl http://localhost:5003/health
```

### MCP Tools Test

```bash
# List available MCP tools
curl http://localhost:5100/mcp/tools

# Test infrastructure query
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"List all VMs in my subscription"}'
```

### Natural Language Queries

```bash
# Test via Chat UI (if running)
# Navigate to http://localhost:5001

# Try these queries:
# - "Show me all VMs in my subscription"
# - "Analyze costs for resource group rg-prod"
# - "Check compliance status for AKS clusters"
# - "Generate a Bicep template for a web app"
```

**📖 Complete test cases:** [NATURAL-LANGUAGE-TEST-CASES.md](./NATURAL-LANGUAGE-TEST-CASES.md)

### Run Automated Tests

```bash
# Unit tests
dotnet test tests/Platform.Engineering.Copilot.Tests.Unit

# Integration tests (requires Azure credentials)
dotnet test tests/Platform.Engineering.Copilot.Tests.Integration

# All tests
dotnet test Platform.Engineering.Copilot.sln
```

---

## 🎯 Feature Examples

### Infrastructure Provisioning

**Generate Bicep Template**
```bash
# Via Chat UI or MCP
"Generate a Bicep template for a web app with:
- App Service Plan (B1 tier)
- Web App with .NET 8 runtime
- Azure SQL Database (Basic tier)
- Application Insights
- All resources in resource group rg-prod"

# Returns complete Bicep template ready to deploy
```

**Generate Terraform**
```bash
"Create Terraform configuration for:
- AKS cluster with 3 nodes
- Azure Container Registry
- Log Analytics workspace
- Network security groups
- All in East US region"

# Returns modular Terraform files (main.tf, variables.tf, outputs.tf)
```

**Kubernetes Manifests**
```bash
"Generate Kubernetes deployment for Node.js application:
- Deployment with 3 replicas
- Service (LoadBalancer)
- ConfigMap for environment variables
- HorizontalPodAutoscaler (2-10 pods)"

# Returns complete K8s manifests
```

**📖 Learn more:** [ARCHITECTURE.md](./ARCHITECTURE.md) - Infrastructure agent capabilities and template generation

---

### Cost Analysis & Optimization

**Analyze Resource Group Costs**
```bash
"Analyze costs for resource group rg-prod over the last 30 days"

# Returns:
# - Total costs by service
# - Breakdown by resource
# - Cost trends
# - Optimization recommendations
```

**Cost Optimization Recommendations**
```bash
"What can I do to reduce costs in my subscription?"

# Returns:
# - Underutilized VMs (recommend downsizing)
# - Unattached disks (recommend deletion)
# - Reserved instance opportunities
# - Shutdown schedules for dev/test environments
# - Estimated savings per recommendation
```

**Budget & Alerts**
```bash
"Create a budget of $5000/month for resource group rg-prod with:
- Alert at 80% threshold
- Alert at 100% threshold
- Email notifications to admin@company.com"

# Creates Azure budget with alerts
```

**Cost Forecasting**
```bash
"Predict my Azure costs for next quarter based on current usage"

# Returns:
# - Projected costs by month
# - Growth trends
# - Anomaly predictions
# - Confidence intervals
```

**📖 Learn more:** [ARCHITECTURE.md](./ARCHITECTURE.md) - Cost Management agent features and predictive analytics

---

### Compliance & Security

**RMF/STIG Compliance Check**
```bash
"Check NIST 800-53 compliance for AKS cluster aks-prod"

# Returns:
# - Control family assessment (AC, AU, CM, IA, etc.)
# - Gap analysis
# - Non-compliant configurations
# - Remediation steps
# - STIG checklist status
```

**NIST Control Lookup**
```bash
"Show me all NIST 800-53 controls for Azure SQL Database"

# Returns:
# - Applicable controls (AC-2, AC-3, AU-2, etc.)
# - Implementation guidance
# - Azure Policy mappings
# - DoD STIG mappings
```

**Vulnerability Scan**
```bash
"Scan resource group rg-prod for security vulnerabilities"

# Returns:
# - Open ports and exposed services
# - Unencrypted storage accounts
# - Missing network security groups
# - Outdated TLS versions
# - Missing Azure Policy assignments
# - Severity ratings (Critical, High, Medium, Low)
```

**ATO Documentation Generation**
```bash
"Generate ATO documentation package for subscription xyz including:
- System Security Plan (SSP)
- Security Assessment Report (SAR)
- Plan of Action & Milestones (POA&M)
- Control Implementation Summary"

# Generates complete ATO package with NIST/STIG mappings
```

**📖 Learn more:** 
- [KNOWLEDGE-BASE.md](./KNOWLEDGE-BASE.md) - Complete RMF/STIG compliance guide
- [PHASE1.md](./PHASE1.md) - Current compliance status (98%)
- [KNOWLEDGE-BASE-INTEGRATION-GUIDE.md](./KNOWLEDGE-BASE-INTEGRATION-GUIDE.md) - Integration patterns

---

### Resource Discovery & Monitoring

**Inventory Query**
```bash
"Show me all virtual machines in my subscription with their:
- Size
- Status (running/stopped)
- OS type
- Location
- Cost per month"

# Returns tabular inventory with details
```

**Health Status**
```bash
"Check health status of all resources in resource group rg-prod"

# Returns:
# - Resource health state
# - Active alerts
# - Recent failures
# - Performance metrics
# - Recommended actions
```

**Network Topology**
```bash
"Analyze network topology for resource group rg-prod"

# Returns:
# - Virtual networks and subnets
# - Network security groups and rules
# - Connected resources
# - Public IP addresses
# - Network flow diagram
```

**Dependency Mapping**
```bash
"Show me all dependencies for web app myapp-prod"

# Returns:
# - Database connections
# - Storage account dependencies
# - Key Vault references
# - Managed identity assignments
# - Dependency graph visualization
```

**📖 Learn more:** [ARCHITECTURE.md](./ARCHITECTURE.md) - Discovery agent capabilities and monitoring features

---

### Architecture Diagrams

**C4 Container Diagram**
```bash
"Generate C4 container diagram for resource group rg-microservices"

# Returns Mermaid diagram showing:
# - System boundaries
# - Containers (services)
# - Databases
# - Message queues
# - Relationships
```

**Sequence Diagram**
```bash
"Create sequence diagram for our PR review workflow:
1. Developer creates PR
2. GitHub webhook triggers review
3. Copilot scans IaC files
4. Posts inline comments
5. Approves or requests changes"

# Returns Mermaid sequence diagram
```

**Database ERD**
```bash
"Generate entity-relationship diagram for our user management database"

# Returns Mermaid ERD with:
# - Tables (Users, Roles, Permissions, Sessions)
# - Columns and data types
# - Primary/foreign key relationships
# - Cardinality indicators
```

**Deployment Flowchart**
```bash
"Create flowchart for our environment provisioning process"

# Returns Mermaid flowchart with:
# - Process steps
# - Decision points
# - Manual approval gates
# - Success/failure paths
```

**📖 Learn more:** [ARCHITECTURE.md](./ARCHITECTURE.md#architecture-diagram-generation) - Complete diagram generation guide (9 diagram types)

---

### Workspace & Template Creation

**Create Microservice Workspace**
```bash
"Create a workspace template for microservice with:
- .NET 8 Web API project structure
- Dockerfile for containerization
- Bicep templates for Azure resources
- GitHub Actions CI/CD pipeline
- README with setup instructions"

# Generates complete workspace ready to clone
```

**DoD Compliant Template**
```bash
"Generate IL5-compliant workspace template including:
- RMF/STIG baseline configuration
- Azure Policy assignments
- NIST 800-53 control mappings
- Security documentation templates
- Compliance checklist"

# Creates DoD-ready workspace with compliance baked in
```

**Custom Template**
```bash
"Create workspace template for Python data science project with:
- Jupyter notebooks setup
- requirements.txt
- Docker configuration
- Azure ML integration
- Data pipeline templates"

# Generates specialized workspace for data science
```

**📖 Learn more:** [WORKSPACE-CREATION.md](./WORKSPACE-CREATION.md) - Complete workspace creation guide and template system

---

### Environment Management

**Clone Environment**
```bash
"Clone production environment to staging with:
- Same infrastructure configuration
- Scaled-down SKUs (save costs)
- Separate database
- Isolated network"

# Creates staging environment mirroring production
```

**Environment Comparison**
```bash
"Compare dev and prod environments and show differences in:
- Resource configurations
- Network topology
- Security settings
- Costs"

# Returns detailed comparison report
```

**Auto-Scaling Configuration**
```bash
"Set up auto-scaling for AKS cluster aks-prod:
- Min 3 nodes, max 10 nodes
- Scale up at 70% CPU
- Scale down at 30% CPU
- Cool down period: 5 minutes"

# Configures cluster autoscaler
```

**📖 Learn more:** [ARCHITECTURE.md](./ARCHITECTURE.md) - Environment agent capabilities and lifecycle management

---

## 📚 Next Steps

### For Developers

1. **Understand the Architecture**
   - Read [ARCHITECTURE.md](./ARCHITECTURE.md) - System design and components
   - Review [AGENT-ORCHESTRATION.md](./AGENT-ORCHESTRATION.md) - Agent configuration

2. **Configure Agents**
   - Edit `appsettings.json` → `AgentConfiguration.EnabledAgents`
   - See [AGENT-ORCHESTRATION.md](./AGENT-ORCHESTRATION.md#agent-configuration)

3. **Explore Features**

   **Infrastructure as Code Generation**
   - Generate Bicep templates: "Create a Bicep template for a web app with SQL database"
   - Generate Terraform: "Generate Terraform for AKS cluster with monitoring"
   - Kubernetes manifests: "Create K8s deployment for Node.js app"
   - See all templates: [ARCHITECTURE.md](./ARCHITECTURE.md)

   **Cost Management**
   - Analyze costs: "Show me costs for resource group rg-prod"
   - Cost optimization: "What resources can I optimize to save money?"
   - Budget tracking: "Create budget alerts for subscription xyz"
   - Forecasting: "Predict costs for next quarter"

   **Compliance & Security**
   - RMF/STIG checks: "Check compliance for AKS cluster"
   - NIST controls: "Show me NIST 800-53 controls for Azure SQL"
   - Vulnerability scanning: "Scan resource group for security issues"
   - ATO preparation: "Generate ATO documentation for my environment"
   - [KNOWLEDGE-BASE.md](./KNOWLEDGE-BASE.md) - Complete compliance guide

   **Resource Discovery**
   - Inventory: "List all VMs in my subscription"
   - Health monitoring: "Show health status of all resources"
   - Configuration analysis: "Analyze network topology for rg-prod"
   - Dependency mapping: "Show dependencies for web app xyz"

   **Architecture Diagrams**
   - C4 diagrams: "Generate C4 container diagram for rg-prod"
   - Sequence diagrams: "Create sequence diagram for PR review workflow"
   - ERD diagrams: "Diagram database schema for user management"
   - Flowcharts: "Generate flowchart for deployment process"

   **Workspace Creation**
   - One-click templates: "Create workspace for microservice with CI/CD"
   - Custom templates: "Generate workspace with Terraform and GitHub Actions"
   - DoD compliance: "Create IL5-compliant workspace template"
   - [WORKSPACE-CREATION.md](./WORKSPACE-CREATION.md)

   **Environment Management**
   - Clone environments: "Clone production to staging"
   - Environment comparison: "Compare dev and prod configurations"
   - Automated scaling: "Set up auto-scaling for AKS cluster"

4. **Build Integrations**
   - [INTEGRATIONS.md](./INTEGRATIONS.md) - GitHub Copilot, M365, MCP API
   - [PR-REVIEW-INTEGRATION.md](./PR-REVIEW-INTEGRATION.md) - Automated PR reviews

### For DevOps Engineers

1. **Production Deployment**
   - [DEPLOYMENT.md](./DEPLOYMENT.md) - Complete deployment guide
   - [AUTHENTICATION.md](./AUTHENTICATION.md) - Managed identity setup

2. **Monitoring**
   - [DEPLOYMENT.md](./DEPLOYMENT.md#monitoring--observability) - Application Insights, Prometheus

3. **Security**
   - [DEPLOYMENT.md](./DEPLOYMENT.md#security-considerations) - HTTPS, secrets management

### For Compliance Teams

1. **Compliance Status**
   - [PHASE1.md](./PHASE1.md) - Phase 1 status (98% complete)
   - [PHASE2.md](./PHASE2.md) - Phase 2 roadmap

2. **RMF/STIG Knowledge Base**
   - [KNOWLEDGE-BASE.md](./KNOWLEDGE-BASE.md) - Complete compliance guide
   - [KNOWLEDGE-BASE-INTEGRATION-GUIDE.md](./KNOWLEDGE-BASE-INTEGRATION-GUIDE.md) - Integration patterns

### For Architects

1. **System Design**
   - [ARCHITECTURE.md](./ARCHITECTURE.md) - Complete architecture
   - [AGENT-ORCHESTRATION.md](./AGENT-ORCHESTRATION.md) - Agent patterns

2. **Advanced Topics**
   - [KNOWLEDGE-BASE-ARCHITECTURE-DIAGRAM.md](./KNOWLEDGE-BASE-ARCHITECTURE-DIAGRAM.md) - KB architecture
   - [DEVELOPMENT.md](./DEVELOPMENT.md) - Deep dive into codebase

---

## 🆘 Troubleshooting

### Common Issues

**Issue: "dotnet: command not found"**
```bash
# Install .NET 9.0 SDK
# macOS: brew install dotnet
# Windows: Download from https://dotnet.microsoft.com/download
```

**Issue: "Azure authentication failed"**
```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "your-subscription-id"

# Verify
az account show
```

**Issue: "Database migration failed"**
```bash
# Ensure EF tools installed
dotnet tool update --global dotnet-ef

# Delete database and recreate
rm platform_engineering_copilot_management.db
cd src/Platform.Engineering.Copilot.Data
dotnet ef database update
```

**Issue: "Docker container won't start"**
```bash
# Check logs
docker compose logs -f platform-mcp

# Verify environment variables
docker compose config

# Restart services
docker compose down
docker compose up -d
```

**Issue: "MCP Server returns 500 error"**
```bash
# Check logs
docker compose logs platform-mcp

# Verify Azure credentials in .env
cat .env | grep AZURE

# Test Azure connection
az account show
```

**Issue: "Agent not responding"**
```bash
# Check agent configuration
cat src/Platform.Engineering.Copilot.Mcp/appsettings.json | grep AgentConfiguration

# Verify agent is enabled
# See AGENT-ORCHESTRATION.md for configuration
```

### Getting Help

- **Documentation**: See [docs/README.md](./README.md) for complete index
- **Issues**: Create issue at [GitHub Issues](https://github.com/azurenoops/platform-engineering-copilot/issues)
- **Authentication**: See [AUTHENTICATION.md](./AUTHENTICATION.md)
- **Deployment**: See [DEPLOYMENT.md](./DEPLOYMENT.md)
- **Development**: See [DEVELOPMENT.md](./DEVELOPMENT.md)

---

## 📖 Additional Documentation

| Document | Purpose |
|----------|---------|
| **[AUTHENTICATION.md](./AUTHENTICATION.md)** | Azure authentication setup |
| **[DEPLOYMENT.md](./DEPLOYMENT.md)** | Production deployment guide |
| **[DEVELOPMENT.md](./DEVELOPMENT.md)** | Development deep dive |
| **[ARCHITECTURE.md](./ARCHITECTURE.md)** | System architecture |
| **[INTEGRATIONS.md](./INTEGRATIONS.md)** | GitHub Copilot, M365, MCP API |
| **[AGENT-ORCHESTRATION.md](./AGENT-ORCHESTRATION.md)** | Agent configuration |
| **[KNOWLEDGE-BASE.md](./KNOWLEDGE-BASE.md)** | RMF/STIG compliance |
| **[WORKSPACE-CREATION.md](./WORKSPACE-CREATION.md)** | Workspace templates |
| **[PHASE1.md](./PHASE1.md)** | Compliance status (98%) |

---

**Document Version:** 1.0  
**Last Updated:** November 2025  
**Status:** ✅ Production Ready

**Supersedes:** N/A (New consolidated quick-start guide)
