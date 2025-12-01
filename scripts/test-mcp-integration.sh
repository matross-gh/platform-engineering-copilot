#!/bin/bash
# =============================================================================
# Platform MCP Server Integration Tests
# =============================================================================
# This script tests the Platform MCP server integration endpoints.
#
# Usage:
#   ./scripts/test-mcp-integration.sh
#
# Prerequisites:
#   - Platform MCP server running (setup-simplechat-integration.sh)
#   - curl installed
# =============================================================================

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

MCP_URL="http://localhost:5100"

echo -e "${BLUE}=====================================================================${NC}"
echo -e "${BLUE}  Platform MCP Server Integration Tests${NC}"
echo -e "${BLUE}=====================================================================${NC}"
echo ""

# Test 1: Health Check
echo -e "${YELLOW}[Test 1] Health Check${NC}"
RESPONSE=$(curl -s -w "\n%{http_code}" "$MCP_URL/health")
HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ PASSED${NC}"
    echo -e "${BLUE}   Response: $BODY${NC}"
else
    echo -e "${RED}❌ FAILED (HTTP $HTTP_CODE)${NC}"
    echo -e "${RED}   Response: $BODY${NC}"
fi
echo ""

# Test 2: Infrastructure Query - List Resource Groups
echo -e "${YELLOW}[Test 2] Infrastructure Query - List Resource Groups${NC}"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$MCP_URL/api/chat/intelligent-query" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "List all resource groups in my subscription",
    "conversationId": "test-integration-1"
  }')

HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ PASSED${NC}"
    echo -e "${BLUE}   Response preview:${NC}"
    echo "$BODY" | jq -r '.response' 2>/dev/null | head -n 10 || echo "$BODY" | head -c 500
else
    echo -e "${RED}❌ FAILED (HTTP $HTTP_CODE)${NC}"
    echo -e "${RED}   Response: $BODY${NC}"
fi
echo ""

# Test 3: Compliance Query - NIST Scan
echo -e "${YELLOW}[Test 3] Compliance Query - NIST 800-53 Information${NC}"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$MCP_URL/api/chat/intelligent-query" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What are the NIST 800-53 control families?",
    "conversationId": "test-integration-2"
  }')

HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ PASSED${NC}"
    echo -e "${BLUE}   Response preview:${NC}"
    echo "$BODY" | jq -r '.response' 2>/dev/null | head -n 10 || echo "$BODY" | head -c 500
else
    echo -e "${RED}❌ FAILED (HTTP $HTTP_CODE)${NC}"
    echo -e "${RED}   Response: $BODY${NC}"
fi
echo ""

# Test 4: Cost Management Query
echo -e "${YELLOW}[Test 4] Cost Management Query${NC}"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$MCP_URL/api/chat/intelligent-query" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "How can I optimize my Azure costs?",
    "conversationId": "test-integration-3"
  }')

HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ PASSED${NC}"
    echo -e "${BLUE}   Response preview:${NC}"
    echo "$BODY" | jq -r '.response' 2>/dev/null | head -n 10 || echo "$BODY" | head -c 500
else
    echo -e "${RED}❌ FAILED (HTTP $HTTP_CODE)${NC}"
    echo -e "${RED}   Response: $BODY${NC}"
fi
echo ""

# Test 5: IaC Template Generation
echo -e "${YELLOW}[Test 5] IaC Template Generation - Bicep${NC}"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$MCP_URL/api/chat/intelligent-query" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Generate a Bicep template for a storage account",
    "conversationId": "test-integration-4"
  }')

HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ PASSED${NC}"
    echo -e "${BLUE}   Response preview:${NC}"
    echo "$BODY" | jq -r '.response' 2>/dev/null | head -n 10 || echo "$BODY" | head -c 500
else
    echo -e "${RED}❌ FAILED (HTTP $HTTP_CODE)${NC}"
    echo -e "${RED}   Response: $BODY${NC}"
fi
echo ""

# Test 6: Conversation History
echo -e "${YELLOW}[Test 6] Conversation History Retrieval${NC}"
RESPONSE=$(curl -s -w "\n%{http_code}" "$MCP_URL/api/chat/history/test-integration-1")

HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ PASSED${NC}"
    MESSAGE_COUNT=$(echo "$BODY" | jq -r '.messageCount' 2>/dev/null || echo "N/A")
    echo -e "${BLUE}   Message count: $MESSAGE_COUNT${NC}"
else
    echo -e "${RED}❌ FAILED (HTTP $HTTP_CODE)${NC}"
    echo -e "${RED}   Response: $BODY${NC}"
fi
echo ""

# Summary
echo -e "${GREEN}=====================================================================${NC}"
echo -e "${GREEN}  Integration Tests Complete!${NC}"
echo -e "${GREEN}=====================================================================${NC}"
echo ""
echo -e "${BLUE}MCP Server is ready for SimpleChat integration.${NC}"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo -e "  1. Proceed to Phase 2: SimpleChat Backend Integration"
echo -e "  2. Clone SimpleChat: git clone https://github.com/microsoft/simplechat"
echo -e "  3. Implement backend integration (see integration plan)"
echo ""
