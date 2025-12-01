# SimpleChat MCP Integration - Phase 1 Quick Start

This guide helps you set up the Platform Engineering Copilot MCP server for SimpleChat integration.

## Overview

Phase 1 deploys the MCP server in HTTP mode, making it ready to receive requests from SimpleChat's backend.

**What you'll deploy:**
- Platform MCP Server (HTTP mode, port 5100)
- SQL Server Database (for MCP data)
- Health monitoring and logging

## Prerequisites

- ✅ Docker & Docker Compose installed
- ✅ Azure CLI installed and authenticated (`az login`)
- ✅ Azure OpenAI service deployed
- ✅ Azure subscription with appropriate permissions

## Quick Setup (Automated)

### Option 1: Using the Setup Script (Recommended)

```bash
# 1. Navigate to repository root
cd /path/to/platform-engineering-copilot

# 2. Copy and configure environment file
cp .env.simplechat .env.simplechat.local
nano .env.simplechat.local  # Edit with your Azure credentials

# 3. Run setup script
./scripts/setup-simplechat-integration.sh
```

The script will:
- ✅ Validate prerequisites
- ✅ Check environment configuration
- ✅ Build Docker images
- ✅ Start SQL Server database
- ✅ Start MCP server in HTTP mode
- ✅ Verify health endpoints

### Option 2: Manual Setup

```bash
# 1. Configure environment
cp .env.simplechat .env

# Edit .env with your values:
# - AZURE_SUBSCRIPTION_ID
# - AZURE_TENANT_ID
# - AZURE_CLIENT_ID
# - AZURE_CLIENT_SECRET
# - AZURE_OPENAI_ENDPOINT
# - AZURE_OPENAI_API_KEY

# 2. Load environment variables
source .env

# 3. Build MCP server
docker-compose -f docker-compose.simplechat-integration.yml build platform-mcp

# 4. Start SQL Server
docker-compose -f docker-compose.simplechat-integration.yml up -d platform-mcp-sqlserver

# Wait 30 seconds for SQL Server to initialize
sleep 30

# 5. Start MCP server
docker-compose -f docker-compose.simplechat-integration.yml up -d platform-mcp

# Wait 30 seconds for MCP server to initialize
sleep 30

# 6. Verify health
curl http://localhost:5100/health
```

## Verification

### Check Running Services

```bash
docker-compose -f docker-compose.simplechat-integration.yml ps
```

Expected output:
```
NAME                      STATUS         PORTS
platform-mcp              Up (healthy)   0.0.0.0:5100->5100/tcp
platform-mcp-sqlserver    Up (healthy)   0.0.0.0:1433->1433/tcp
```

### Test Health Endpoint

```bash
curl http://localhost:5100/health
```

Expected response:
```json
{"status": "healthy", "version": "0.7.2", "mode": "http"}
```

### Run Integration Tests

```bash
./scripts/test-mcp-integration.sh
```

This will test:
- ✅ Health check
- ✅ Infrastructure queries
- ✅ Compliance queries
- ✅ Cost management queries
- ✅ IaC template generation
- ✅ Conversation history

## Example Queries

### Test Infrastructure Agent

```bash
curl -X POST http://localhost:5100/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "List all resource groups in my subscription",
    "conversationId": "test-1"
  }'
```

### Test Compliance Agent

```bash
curl -X POST http://localhost:5100/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What are the NIST 800-53 control families?",
    "conversationId": "test-2"
  }'
```

### Test IaC Generation

```bash
curl -X POST http://localhost:5100/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Generate a Bicep template for a storage account",
    "conversationId": "test-3"
  }'
```

## Logs & Debugging

### View MCP Server Logs

```bash
# Real-time logs
docker-compose -f docker-compose.simplechat-integration.yml logs -f platform-mcp

# Last 100 lines
docker-compose -f docker-compose.simplechat-integration.yml logs --tail 100 platform-mcp

# All logs
docker logs platform-mcp
```

### View SQL Server Logs

```bash
docker-compose -f docker-compose.simplechat-integration.yml logs platform-mcp-sqlserver
```

### Check Container Status

```bash
# Detailed status
docker-compose -f docker-compose.simplechat-integration.yml ps

# Resource usage
docker stats platform-mcp platform-mcp-sqlserver
```

## Common Issues

### Issue 1: MCP Server Health Check Failing

**Symptoms:**
- `curl http://localhost:5100/health` returns connection refused
- Container status shows "unhealthy"

**Solutions:**

```bash
# Check logs
docker logs platform-mcp --tail 50

# Restart MCP server
docker-compose -f docker-compose.simplechat-integration.yml restart platform-mcp

# Check environment variables
docker exec platform-mcp env | grep AZURE
```

### Issue 2: SQL Server Not Starting

**Symptoms:**
- SQL Server container shows "unhealthy"
- MCP server fails to connect to database

**Solutions:**

```bash
# Check SQL Server logs
docker logs platform-mcp-sqlserver

# Verify password in .env
grep PLATFORM_MCP_SA_PASSWORD .env

# Restart SQL Server
docker-compose -f docker-compose.simplechat-integration.yml restart platform-mcp-sqlserver
```

### Issue 3: Azure Authentication Errors

**Symptoms:**
- MCP returns "Unauthorized" errors
- Azure API calls fail

**Solutions:**

```bash
# Verify Azure credentials
az login
az account show

# Check environment variables
echo $AZURE_SUBSCRIPTION_ID
echo $AZURE_TENANT_ID

# Update .env with correct values
nano .env

# Restart MCP server
docker-compose -f docker-compose.simplechat-integration.yml restart platform-mcp
```

## Managing Services

### Start Services

```bash
# Start all services
docker-compose -f docker-compose.simplechat-integration.yml up -d

# Start only MCP server
docker-compose -f docker-compose.simplechat-integration.yml up -d platform-mcp
```

### Stop Services

```bash
# Stop all services
docker-compose -f docker-compose.simplechat-integration.yml down

# Keep data volumes (recommended)
docker-compose -f docker-compose.simplechat-integration.yml down

# Remove everything including data (⚠️ data loss)
docker-compose -f docker-compose.simplechat-integration.yml down -v
```

### Restart Services

```bash
# Restart all
docker-compose -f docker-compose.simplechat-integration.yml restart

# Restart only MCP
docker-compose -f docker-compose.simplechat-integration.yml restart platform-mcp
```

### View Status

```bash
docker-compose -f docker-compose.simplechat-integration.yml ps
```

## Configuration

### Environment Variables

Key variables in `.env`:

```bash
# Azure Credentials
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-service-principal-client-id
AZURE_CLIENT_SECRET=your-service-principal-secret

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.us/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=gpt-4o

# MCP Settings
MCP_ENABLED=true
MCP_TIMEOUT_SECONDS=120
MCP_INTENT_THRESHOLD=0.6

# Database
PLATFORM_MCP_SA_PASSWORD=PlatformMcpDB123!
```

### Ports

| Service | Port | Description |
|---------|------|-------------|
| MCP Server | 5100 | HTTP API for SimpleChat integration |
| SQL Server | 1433 | Database for MCP data |

## Next Steps

✅ **Phase 1 Complete!** Your MCP server is running and ready.

**Next:** Proceed to Phase 2 - SimpleChat Backend Integration

1. Clone SimpleChat repository:
   ```bash
   git clone https://github.com/microsoft/simplechat.git
   cd simplechat
   ```

2. Follow Phase 2 instructions in:
   ```
   docs/SIMPLECHAT-MCP-INTEGRATION-PLAN.md
   ```

3. Implement backend integration:
   - Create `mcp_client.py`
   - Create `intent_detector.py`
   - Modify chat handler

## Support

- **Documentation:** `docs/SIMPLECHAT-MCP-INTEGRATION-PLAN.md`
- **Logs:** `docker-compose -f docker-compose.simplechat-integration.yml logs -f`
- **Issues:** Check troubleshooting section above

## Architecture

```
┌─────────────────────────────────────┐
│     Platform MCP Server             │
│     (HTTP Mode - Port 5100)         │
│                                     │
│  ┌──────────────────────────────┐  │
│  │   Multi-Agent Orchestrator   │  │
│  │                              │  │
│  │  • Infrastructure Agent      │  │
│  │  • Compliance Agent          │  │
│  │  • Cost Management Agent     │  │
│  │  • Environment Agent         │  │
│  │  • Discovery Agent           │  │
│  │  • Knowledge Base Agent      │  │
│  └──────────────────────────────┘  │
└─────────────────┬───────────────────┘
                  │
                  ↓
         ┌────────────────┐
         │  SQL Server    │
         │  (Port 1433)   │
         └────────────────┘
```

Ready for SimpleChat integration in Phase 2! 🚀
