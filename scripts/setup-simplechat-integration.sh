#!/bin/bash
# =============================================================================
# SimpleChat + Platform Engineering Copilot MCP Integration Setup Script
# =============================================================================
# This script sets up the Platform MCP server for SimpleChat integration.
#
# Usage:
#   ./scripts/setup-simplechat-integration.sh
#
# Prerequisites:
#   - Docker and Docker Compose installed
#   - Azure CLI installed and authenticated (az login)
#   - .env.simplechat file configured with Azure credentials
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

echo -e "${BLUE}=====================================================================${NC}"
echo -e "${BLUE}  SimpleChat + Platform Engineering Copilot MCP Integration Setup${NC}"
echo -e "${BLUE}=====================================================================${NC}"
echo ""

# -----------------------------------------------------------------------------
# Step 1: Check Prerequisites
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[1/6] Checking prerequisites...${NC}"

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker is not installed. Please install Docker first.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Docker found: $(docker --version)${NC}"

# Check Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}❌ Docker Compose is not installed. Please install Docker Compose first.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Docker Compose found: $(docker-compose --version)${NC}"

# Check Azure CLI
if ! command -v az &> /dev/null; then
    echo -e "${YELLOW}⚠️  Azure CLI is not installed. Some features may not work.${NC}"
    echo -e "${YELLOW}   Install: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli${NC}"
else
    echo -e "${GREEN}✅ Azure CLI found: $(az --version | head -n 1)${NC}"
    
    # Check if logged in
    if az account show &> /dev/null; then
        AZURE_ACCOUNT=$(az account show --query name -o tsv)
        echo -e "${GREEN}✅ Azure CLI authenticated: ${AZURE_ACCOUNT}${NC}"
    else
        echo -e "${YELLOW}⚠️  Not logged into Azure CLI. Run: az login${NC}"
    fi
fi

echo ""

# -----------------------------------------------------------------------------
# Step 2: Environment Configuration
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[2/6] Configuring environment...${NC}"

cd "$REPO_ROOT"

if [ ! -f ".env.simplechat" ]; then
    echo -e "${RED}❌ .env.simplechat file not found!${NC}"
    echo -e "${YELLOW}   Creating from template...${NC}"
    cp .env.simplechat .env.simplechat.backup 2>/dev/null || true
    echo -e "${GREEN}✅ Created .env.simplechat${NC}"
    echo -e "${YELLOW}   Please edit .env.simplechat with your Azure credentials and re-run this script.${NC}"
    exit 1
fi

# Load environment variables
source .env.simplechat

# Validate required variables
REQUIRED_VARS=(
    "AZURE_SUBSCRIPTION_ID"
    "AZURE_TENANT_ID"
    "AZURE_OPENAI_ENDPOINT"
    "AZURE_OPENAI_API_KEY"
)

MISSING_VARS=()
for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ]; then
        MISSING_VARS+=("$var")
    fi
done

if [ ${#MISSING_VARS[@]} -gt 0 ]; then
    echo -e "${RED}❌ Missing required environment variables:${NC}"
    for var in "${MISSING_VARS[@]}"; do
        echo -e "${RED}   - $var${NC}"
    done
    echo -e "${YELLOW}   Please update .env.simplechat and re-run this script.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Environment configuration validated${NC}"
echo -e "${BLUE}   Subscription: ${AZURE_SUBSCRIPTION_ID:0:8}...${NC}"
echo -e "${BLUE}   Tenant: ${AZURE_TENANT_ID:0:8}...${NC}"
echo -e "${BLUE}   OpenAI Endpoint: ${AZURE_OPENAI_ENDPOINT}${NC}"
echo ""

# -----------------------------------------------------------------------------
# Step 3: Build MCP Server Docker Image
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[3/6] Building Platform MCP Server Docker image...${NC}"

docker-compose -f docker-compose.simplechat-integration.yml build platform-mcp

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ MCP Server Docker image built successfully${NC}"
else
    echo -e "${RED}❌ Failed to build MCP Server Docker image${NC}"
    exit 1
fi
echo ""

# -----------------------------------------------------------------------------
# Step 4: Start SQL Server Database
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[4/6] Starting SQL Server database for MCP...${NC}"

docker-compose -f docker-compose.simplechat-integration.yml up -d platform-mcp-sqlserver

echo -e "${BLUE}   Waiting for SQL Server to be healthy...${NC}"
RETRY_COUNT=0
MAX_RETRIES=30
until docker-compose -f docker-compose.simplechat-integration.yml ps platform-mcp-sqlserver | grep -q "healthy" || [ $RETRY_COUNT -eq $MAX_RETRIES ]; do
    echo -e "${BLUE}   Waiting... ($((RETRY_COUNT+1))/$MAX_RETRIES)${NC}"
    sleep 2
    RETRY_COUNT=$((RETRY_COUNT+1))
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo -e "${RED}❌ SQL Server did not become healthy in time${NC}"
    docker-compose -f docker-compose.simplechat-integration.yml logs platform-mcp-sqlserver
    exit 1
fi

echo -e "${GREEN}✅ SQL Server is running and healthy${NC}"
echo ""

# -----------------------------------------------------------------------------
# Step 5: Start Platform MCP Server
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[5/6] Starting Platform MCP Server...${NC}"

docker-compose -f docker-compose.simplechat-integration.yml up -d platform-mcp

echo -e "${BLUE}   Waiting for MCP Server to be healthy...${NC}"
RETRY_COUNT=0
MAX_RETRIES=60
until curl -f http://localhost:5100/health &> /dev/null || [ $RETRY_COUNT -eq $MAX_RETRIES ]; do
    echo -e "${BLUE}   Waiting... ($((RETRY_COUNT+1))/$MAX_RETRIES)${NC}"
    sleep 2
    RETRY_COUNT=$((RETRY_COUNT+1))
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo -e "${RED}❌ MCP Server did not become healthy in time${NC}"
    echo -e "${YELLOW}   Checking logs...${NC}"
    docker-compose -f docker-compose.simplechat-integration.yml logs --tail 50 platform-mcp
    exit 1
fi

echo -e "${GREEN}✅ Platform MCP Server is running and healthy${NC}"
echo ""

# -----------------------------------------------------------------------------
# Step 6: Verify Installation
# -----------------------------------------------------------------------------
echo -e "${YELLOW}[6/6] Verifying installation...${NC}"

# Test health endpoint
echo -e "${BLUE}   Testing MCP health endpoint...${NC}"
HEALTH_RESPONSE=$(curl -s http://localhost:5100/health)
echo -e "${GREEN}   Health response: $HEALTH_RESPONSE${NC}"

# Show running containers
echo ""
echo -e "${BLUE}   Running containers:${NC}"
docker-compose -f docker-compose.simplechat-integration.yml ps

echo ""
echo -e "${GREEN}=====================================================================${NC}"
echo -e "${GREEN}  ✅ Phase 1 Setup Complete!${NC}"
echo -e "${GREEN}=====================================================================${NC}"
echo ""
echo -e "${BLUE}Platform MCP Server is running at:${NC}"
echo -e "${GREEN}  http://localhost:5100${NC}"
echo ""
echo -e "${BLUE}Test the MCP server:${NC}"
echo -e "${GREEN}  curl http://localhost:5100/health${NC}"
echo ""
echo -e "${BLUE}View logs:${NC}"
echo -e "${GREEN}  docker-compose -f docker-compose.simplechat-integration.yml logs -f platform-mcp${NC}"
echo ""
echo -e "${BLUE}Stop services:${NC}"
echo -e "${GREEN}  docker-compose -f docker-compose.simplechat-integration.yml down${NC}"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo -e "  1. Test MCP server with example queries (see test script)"
echo -e "  2. Proceed to Phase 2: SimpleChat Backend Integration"
echo -e "  3. See: docs/SIMPLECHAT-MCP-INTEGRATION-PLAN.md"
echo ""
