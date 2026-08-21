# Platform Engineering Copilot - Deployment Guide

**Version:** 3.0  
**Last Updated:** January 2026

---

## Overview

The Platform Engineering Copilot can be deployed in three modes:

| Mode | Use Case | Command |
|------|----------|---------|
| **Local** | Development | `dotnet run` |
| **Docker** | Quick start, testing | `docker-compose up` |
| **ACI** | Azure container deployment | `deployment/` Bicep (registry modules) |
| **AKS** | Production Kubernetes | `deployment/` Bicep (registry modules) |

---

## Quick Start (Docker)

### Prerequisites

- Docker & Docker Compose
- Azure CLI (authenticated)
- Azure OpenAI endpoint

### MCP Server Only (Recommended for AI Clients)

```bash
# Start MCP server
docker-compose -f docker-compose.essentials.yml up -d

# Verify
curl http://localhost:5100/health
```

### Full Platform (Web UI + MCP)

```bash
# Start all services
docker-compose up -d

# Access
open http://localhost:5001   # Chat UI
open http://localhost:5003   # Admin UI
curl http://localhost:5100/health  # MCP Server
```

### Configuration

Copy and configure environment:

```bash
cp .env.example .env
# Edit .env with:
# - AZURE_OPENAI_ENDPOINT
# - AZURE_OPENAI_API_KEY
# - AZURE_TENANT_ID
# - AZURE_SUBSCRIPTION_ID
```

---

## Local Development

```bash
# Build
dotnet build

# Azure authentication
az cloud set --name AzureUSGovernment  # or AzureCloud
az login
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)

# Run MCP server (stdio mode for AI clients)
dotnet run --project src/Platform.Engineering.Copilot.Mcp

# Run MCP server (HTTP mode for web apps)
dotnet run --project src/Platform.Engineering.Copilot.Mcp -- --http
```

---

## Docker Compose Files

| File | Services | Use Case |
|------|----------|----------|
| `docker-compose.yml` | MCP, Chat, Admin | Full platform |
| `docker-compose.essentials.yml` | MCP only | AI client development |
| `docker-compose.dev.yml` | Dev overrides | Local development |
| `docker-compose.prod.yml` | Production settings | Production |

Redis is no longer run as a local Docker container. All environments, including
local dev, connect to the Azure Cache for Redis instance provisioned by
`deployment/infrastructure/cicd.deploy.pe.infrastructure.bicep` (set the
connection string via `StateManagement__RedisConnectionString`, e.g. from Key Vault).

### Service Ports

| Service | Port | Description |
|---------|------|-------------|
| MCP Server | 5100 | Model Context Protocol server |
| Chat UI | 5001 | Web chat interface |
| Admin API | 5003 | Admin console API |

---

## Build Container Images

```bash
# Create buildx builder
docker buildx create --name platform-builder --use --bootstrap

# Build images
docker buildx build --load -t platform-engineering-copilot-mcp:latest \
  -f src/Platform.Engineering.Copilot.Mcp/Dockerfile .

docker buildx build --load -t platform-engineering-copilot-chat:latest \
  -f src/Platform.Engineering.Copilot.Chat/Dockerfile .
```

---

## Azure Container Instances (ACI)

### Push to ACR

```bash
ACR_NAME="your-acr-name"

# Login and push
az acr login --name $ACR_NAME
docker tag platform-engineering-copilot-mcp:latest \
  ${ACR_NAME}.azurecr.io/platform-engineering-copilot-mcp:latest
docker push ${ACR_NAME}.azurecr.io/platform-engineering-copilot-mcp:latest
```

### Deploy with Bicep

Infrastructure and workload are deployed from the `deployment/` folder using modules
from the enterprise Bicep registry (`br/enterprisebicepregistry`, see
`deployment/bicepconfig.json`).

```bash
cd deployment

# Deploy shared infrastructure (LAW, VNet, Key Vault, Storage, SQL, ACR, AKS, Redis, Foundry)
az deployment group create \
  --name "pe-infrastructure-$(date +%Y%m%d)" \
  --resource-group rg-pec-infrastructure-dev \
  --template-file infrastructure/cicd.deploy.pe.infrastructure.bicep \
  --parameters infrastructure/cicd.deploy.pe.infrastructure.bicepparam

# Deploy the ACI workload (mcp, chat, admin-api, admin-client)
az deployment group create \
  --name "pe-workload-$(date +%Y%m%d)" \
  --resource-group rg-pec-infrastructure-dev \
  --template-file workload/cicd.deploy.pe.workload.bicep \
  --parameters workload/cicd.deploy.pe.workload.bicepparam
```

### ACI Environment Variables

Set in Azure Portal or Bicep:

```
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.us
AZURE_OPENAI_API_KEY=your-key
AZURE_TENANT_ID=your-tenant-id
AZURE_SUBSCRIPTION_ID=your-subscription-id
```

---

## Azure Kubernetes Service (AKS)

### Deploy AKS Infrastructure

AKS is one of the modules deployed by `deployment/infrastructure/cicd.deploy.pe.infrastructure.bicep`
(see the Bicep section above); adjust `pAksName` / `pAksNodeResourceGroupName` etc. in
`cicd.deploy.pe.infrastructure.bicepparam` for the target environment, then:

```bash
cd deployment
az deployment group create \
  --name "pe-infrastructure-$(date +%Y%m%d)" \
  --resource-group rg-pec-infrastructure-prod \
  --template-file infrastructure/cicd.deploy.pe.infrastructure.bicep \
  --parameters infrastructure/cicd.deploy.pe.infrastructure.bicepparam

# Get credentials
az aks get-credentials \
  --resource-group rg-pec-infrastructure-prod \
  --name aks-pec-prod
```

### Deploy Application

AKS clusters for this project are created exclusively through the enterprise Bicep
registry's `microsoft.containerservice/managedclusters` module (wired in
`deployment/infrastructure/cicd.deploy.pe.infrastructure.bicep`), since IL5 requires
the cluster to run in an Azure Dedicated Host / Host Group. Raw Terraform and
hand-written Kubernetes manifests are no longer used to provision or size the
cluster. Today the workload itself is deployed as ACI container groups via
`deployment/workload/cicd.deploy.pe.workload.bicep` (see the ACI section above);
Kubernetes application manifests for running the workload on this AKS cluster do
not exist yet and are pending a future update.

---

## MCP Client Configuration

### GitHub Copilot

Create `~/.copilot/config.json`:

```json
{
  "mcpServers": {
    "platform-engineering-copilot": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/Platform.Engineering.Copilot.Mcp"]
    }
  }
}
```

### Claude Desktop

Create `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "platform-engineering-copilot": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/Platform.Engineering.Copilot.Mcp"]
    }
  }
}
```

### Docker Mode (HTTP)

For containerized deployments:

```json
{
  "mcpServers": {
    "platform-engineering-copilot": {
      "url": "http://localhost:5100"
    }
  }
}
```

---

## Health Checks

```bash
# MCP Server
curl http://localhost:5100/health

# Chat UI
curl http://localhost:5001/health

# Admin API
curl http://localhost:5003/health
```

---

## Troubleshooting

### Container Logs

```bash
# Docker Compose
docker-compose logs -f mcp

# ACI
az container logs --resource-group rg-platform-engineering --name aci-mcp

# AKS
kubectl logs -f deployment/mcp-deployment
```

### Common Issues

| Issue | Solution |
|-------|----------|
| Auth failure | Run `az login` and set `AZURE_TENANT_ID` |
| OpenAI timeout | Verify `AZURE_OPENAI_ENDPOINT` is correct |
| Port conflict | Change port mapping in docker-compose.yml |
| Build cache errors | Add `--no-cache` to docker build |

---

## Related Documentation

- [ARCHITECTURE.md](./ARCHITECTURE.md) - System architecture
- [GETTING-STARTED.md](./GETTING-STARTED.md) - Quick start
- [DOCKER-COMPOSE-GUIDE.md](../DOCKER-COMPOSE-GUIDE.md) - Docker details
