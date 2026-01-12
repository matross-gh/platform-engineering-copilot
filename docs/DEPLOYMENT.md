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
| **ACI** | Azure container deployment | Bicep templates |
| **AKS** | Production Kubernetes | Helm charts |

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
| `docker-compose.yml` | MCP, Chat, Admin, Redis | Full platform |
| `docker-compose.essentials.yml` | MCP only | AI client development |
| `docker-compose.dev.yml` | Dev overrides | Local development |
| `docker-compose.prod.yml` | Production settings | Production |

### Service Ports

| Service | Port | Description |
|---------|------|-------------|
| MCP Server | 5100 | Model Context Protocol server |
| Chat UI | 5001 | Web chat interface |
| Admin API | 5003 | Admin console API |
| Redis | 6379 | Session/cache (internal) |

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

```bash
# Deploy ACI infrastructure
az deployment sub create \
  --name "platform-engineering-$(date +%Y%m%d)" \
  --location eastus \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/main.parameters.aci.json \
  --parameters environment=dev containerDeploymentTarget=aci
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

```bash
# Deploy AKS cluster
az deployment sub create \
  --template-file infra/bicep/main.bicep \
  --parameters infra/bicep/main.parameters.aks.json \
  --parameters environment=prod containerDeploymentTarget=aks

# Get credentials
az aks get-credentials \
  --resource-group rg-platform-engineering-prod \
  --name aks-platform-engineering-prod
```

### Deploy Application

```bash
# Apply Kubernetes manifests
kubectl apply -f infra/kubernetes/

# Or use Helm
helm install platform-engineering ./infra/helm/platform-engineering
```

### Kubernetes Resources

```
infra/kubernetes/
├── namespace.yaml
├── configmap.yaml
├── secrets.yaml
├── mcp-deployment.yaml
├── mcp-service.yaml
├── chat-deployment.yaml
├── chat-service.yaml
├── ingress.yaml
└── hpa.yaml
```

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
