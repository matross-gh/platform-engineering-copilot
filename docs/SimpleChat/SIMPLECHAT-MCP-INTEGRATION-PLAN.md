# SimpleChat + Platform Engineering Copilot MCP Integration Plan

**Last Updated:** November 21, 2025  
**Status:** Planning Phase  
**Goal:** Integrate Platform Engineering Copilot's MCP server with Microsoft SimpleChat application

---

## 📋 Executive Summary

This document outlines the integration strategy for connecting your **Platform Engineering Copilot MCP Server** with Microsoft's **SimpleChat** application. This integration will enable SimpleChat users to interact with your 6 specialized AI agents (Infrastructure, Compliance, Cost Management, Environment, Discovery, Knowledge Base) through SimpleChat's existing UI.

### Key Benefits

✅ **Enhanced Capabilities**: SimpleChat gains Azure infrastructure provisioning, NIST 800-53 compliance, cost optimization  
✅ **Enterprise Features**: DoD/FedRAMP compliance, ATO documentation, security scanning  
✅ **Natural Language Infrastructure**: Users can deploy Azure resources through conversational AI  
✅ **Seamless Integration**: Your MCP server runs as a backend service alongside SimpleChat  
✅ **Minimal Code Changes**: Leverage your existing HTTP mode MCP server (port 5100)

---

## 🏗️ Architecture Overview

### Current State

```
┌─────────────────────────────────────────────────────────────┐
│                    SimpleChat                               │
├─────────────────────────────────────────────────────────────┤
│  Frontend (JavaScript/React)                                │
│       ↓                                                      │
│  Backend (Python/Flask)                                     │
│       ↓                                                      │
│  Azure OpenAI (GPT-4)                                       │
│  Azure AI Search (RAG)                                      │
│  Azure Cosmos DB                                            │
│  Azure Document Intelligence                                │
└─────────────────────────────────────────────────────────────┘
```

### Target State (Integrated)

```
┌─────────────────────────────────────────────────────────────┐
│              SimpleChat + MCP Integration                   │
├─────────────────────────────────────────────────────────────┤
│  Frontend (JavaScript/React)                                │
│       ↓                                                      │
│  Backend (Python/Flask)                                     │
│       ↓                                                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         Route Handler / Intent Detector             │   │
│  │  • Standard chat → Azure OpenAI                     │   │
│  │  • Infrastructure → MCP Server                      │   │
│  │  • Compliance → MCP Server                          │   │
│  │  • Cost → MCP Server                                │   │
│  └─────────────────┬───────────────────────────────────┘   │
│                    │                                        │
│         ┌──────────┴──────────┐                             │
│         ↓                     ↓                             │
│  Azure OpenAI           MCP Server (HTTP)                   │
│  (Existing)             (Port 5100)                         │
│                               ↓                             │
│                    Multi-Agent Orchestrator                 │
│                    • Infrastructure Agent                   │
│                    • Compliance Agent                       │
│                    • Cost Management Agent                  │
│                    • Environment Agent                      │
│                    • Discovery Agent                        │
│                    • Knowledge Base Agent                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Integration Strategy

### Option 1: **Plugin-Based Integration** (Recommended)

**Concept**: Create a SimpleChat "backend extension" that routes specific intents to your MCP server while keeping standard chat in Azure OpenAI.

**Pros:**
- ✅ Minimal changes to SimpleChat core
- ✅ Your MCP server remains independent
- ✅ Easy to enable/disable via admin settings
- ✅ Preserves SimpleChat's existing features

**Cons:**
- ⚠️ Requires intent detection logic
- ⚠️ Needs UI updates to show MCP-specific features

### Option 2: **Full Backend Replacement**

**Concept**: Replace SimpleChat's Azure OpenAI backend with your MCP server entirely.

**Pros:**
- ✅ Single unified backend
- ✅ All queries benefit from multi-agent orchestration

**Cons:**
- ❌ Loses SimpleChat's RAG/document features
- ❌ Major refactoring required
- ❌ Not recommended

### Option 3: **Dual-Mode Orchestrator**

**Concept**: Create a meta-orchestrator that decides between SimpleChat's RAG pipeline and your MCP agents.

**Pros:**
- ✅ Best of both worlds
- ✅ Intelligent routing

**Cons:**
- ⚠️ Complex orchestration logic
- ⚠️ Higher latency

---

## 📝 Recommended Implementation (Option 1)

### Phase 1: MCP Server Setup (1-2 days)

#### 1.1 Deploy MCP Server Alongside SimpleChat

**Goal**: Run your MCP server in HTTP mode on the same Docker network as SimpleChat.

**Steps:**

1. **Update Docker Compose Configuration**

   Create `docker-compose.simplechat-integration.yml`:

   ```yaml
   version: '3.8'
   
   services:
     # SimpleChat services (keep existing configuration)
     simplechat-frontend:
       # ... existing config
   
     simplechat-backend:
       # ... existing config
       environment:
         - MCP_SERVER_URL=http://platform-mcp:5100
         - MCP_ENABLED=true
   
     # Platform Engineering Copilot MCP Server
     platform-mcp:
       build:
         context: ../platform-engineering-copilot
         dockerfile: src/Platform.Engineering.Copilot.Mcp/Dockerfile
       ports:
         - "5100:5100"
       environment:
         - ASPNETCORE_ENVIRONMENT=Production
         - AZURE_TENANT_ID=${AZURE_TENANT_ID}
         - AZURE_SUBSCRIPTION_ID=${AZURE_SUBSCRIPTION_ID}
         - AZURE_OPENAI_ENDPOINT=${AZURE_OPENAI_ENDPOINT}
         - AZURE_OPENAI_API_KEY=${AZURE_OPENAI_API_KEY}
         - AZURE_OPENAI_DEPLOYMENT_NAME=${AZURE_OPENAI_DEPLOYMENT_NAME}
       command: ["--http", "--port", "5100"]
       networks:
         - simplechat-network
       depends_on:
         - platform-postgres
   
     # Optional: Dedicated database for MCP server
     platform-postgres:
       image: postgres:15
       environment:
         POSTGRES_DB: platform_engineering_copilot
         POSTGRES_USER: ${POSTGRES_USER}
         POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
       volumes:
         - platform-db-data:/var/lib/postgresql/data
       networks:
         - simplechat-network
   
   networks:
     simplechat-network:
       driver: bridge
   
   volumes:
     platform-db-data:
   ```

2. **Configure Environment Variables**

   Add to `.env`:

   ```bash
   # Platform Engineering Copilot MCP Settings
   MCP_SERVER_URL=http://platform-mcp:5100
   MCP_ENABLED=true
   MCP_TIMEOUT_SECONDS=120
   
   # Azure credentials (shared with SimpleChat)
   AZURE_TENANT_ID=your-tenant-id
   AZURE_SUBSCRIPTION_ID=your-subscription-id
   AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.us/
   AZURE_OPENAI_API_KEY=your-api-key
   AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
   
   # Database
   POSTGRES_USER=platform_user
   POSTGRES_PASSWORD=your-secure-password
   ```

3. **Test MCP Server**

   ```bash
   # Start services
   docker-compose -f docker-compose.simplechat-integration.yml up -d
   
   # Verify MCP server is running
   curl http://localhost:5100/health
   
   # Expected response:
   # {"status": "healthy", "version": "0.7.2", "mode": "http"}
   ```

---

### Phase 2: SimpleChat Backend Integration (3-5 days)

#### 2.1 Create MCP Client Service (Python)

**File**: `application/backend/services/mcp_client.py`

```python
import requests
import logging
from typing import Optional, Dict, Any
from dataclasses import dataclass

logger = logging.getLogger(__name__)


@dataclass
class McpResponse:
    """Response from MCP server"""
    success: bool
    response: str
    conversation_id: str
    intent_type: str
    confidence: float
    tool_executed: Optional[str] = None
    agents_invoked: Optional[list] = None
    processing_time_ms: Optional[int] = None
    suggestions: Optional[list] = None
    errors: Optional[list] = None


class PlatformMcpClient:
    """Client for Platform Engineering Copilot MCP Server"""
    
    def __init__(self, base_url: str, timeout: int = 120):
        self.base_url = base_url.rstrip('/')
        self.timeout = timeout
        self.session = requests.Session()
    
    def process_message(
        self,
        message: str,
        conversation_id: str,
        user_id: str,
        context: Optional[Dict[str, Any]] = None
    ) -> McpResponse:
        """
        Send a message to the MCP server for processing.
        
        Args:
            message: User's query/request
            conversation_id: Unique conversation identifier
            user_id: User identifier
            context: Optional additional context
        
        Returns:
            McpResponse with agent results
        """
        try:
            payload = {
                "message": message,
                "conversationId": conversation_id,
                "context": context or {}
            }
            
            response = self.session.post(
                f"{self.base_url}/api/chat/intelligent-query",
                json=payload,
                timeout=self.timeout
            )
            
            response.raise_for_status()
            data = response.json()
            
            return McpResponse(
                success=data.get("success", True),
                response=data.get("response", ""),
                conversation_id=data.get("conversationId", conversation_id),
                intent_type=data.get("intentType", "unknown"),
                confidence=data.get("confidence", 0.0),
                tool_executed=data.get("toolExecuted"),
                agents_invoked=data.get("agentsInvoked", []),
                processing_time_ms=data.get("processingTimeMs"),
                suggestions=data.get("suggestions", []),
                errors=data.get("errors", [])
            )
            
        except requests.exceptions.RequestException as e:
            logger.error(f"MCP server request failed: {e}")
            return McpResponse(
                success=False,
                response=f"Failed to connect to infrastructure services: {str(e)}",
                conversation_id=conversation_id,
                intent_type="error",
                confidence=0.0,
                errors=[str(e)]
            )
    
    def get_conversation_history(self, conversation_id: str) -> Dict[str, Any]:
        """Retrieve conversation history from MCP server"""
        try:
            response = self.session.get(
                f"{self.base_url}/api/chat/history/{conversation_id}",
                timeout=30
            )
            response.raise_for_status()
            return response.json()
        except Exception as e:
            logger.error(f"Failed to retrieve conversation history: {e}")
            return {"success": False, "error": str(e)}
    
    def health_check(self) -> bool:
        """Check if MCP server is healthy"""
        try:
            response = self.session.get(
                f"{self.base_url}/health",
                timeout=5
            )
            return response.status_code == 200
        except:
            return False
```

#### 2.2 Create Intent Detector

**File**: `application/backend/services/intent_detector.py`

```python
import re
from typing import Tuple, Optional
from enum import Enum


class IntentType(Enum):
    """Types of user intents"""
    STANDARD_CHAT = "standard_chat"  # Regular chat → Azure OpenAI
    INFRASTRUCTURE = "infrastructure"  # Azure resources → MCP
    COMPLIANCE = "compliance"  # NIST/FedRAMP → MCP
    COST_MANAGEMENT = "cost_management"  # Cost analysis → MCP
    ENVIRONMENT = "environment"  # Environment lifecycle → MCP
    DISCOVERY = "discovery"  # Resource discovery → MCP
    SECURITY = "security"  # Security scanning → MCP
    ATO_DOCUMENTATION = "ato_documentation"  # ATO docs → MCP


class IntentDetector:
    """Detect user intent to route to appropriate backend"""
    
    # Keywords that indicate MCP routing
    MCP_KEYWORDS = {
        IntentType.INFRASTRUCTURE: [
            'create', 'deploy', 'provision', 'bicep', 'terraform',
            'storage account', 'virtual machine', 'aks', 'kubernetes',
            'resource group', 'vnet', 'subnet', 'nsg', 'infrastructure',
            'iac', 'template', 'arm template'
        ],
        IntentType.COMPLIANCE: [
            'nist', '800-53', 'fedramp', 'compliance', 'control',
            'gap analysis', 'assessment', 'dod', 'il5', 'il4',
            'security control', 'rmf', 'ato'
        ],
        IntentType.COST_MANAGEMENT: [
            'cost', 'budget', 'spending', 'savings', 'optimization',
            'cost analysis', 'cost dashboard', 'forecast', 'billing'
        ],
        IntentType.ENVIRONMENT: [
            'environment', 'clone environment', 'scale environment',
            'dev environment', 'prod environment', 'staging'
        ],
        IntentType.DISCOVERY: [
            'discover', 'list resources', 'inventory', 'health check',
            'resource count', 'what resources', 'show resources'
        ],
        IntentType.SECURITY: [
            'vulnerability', 'security scan', 'cve', 'security policy',
            'defender', 'security assessment'
        ],
        IntentType.ATO_DOCUMENTATION: [
            'ssp', 'sar', 'poam', 'ato package', 'system security plan',
            'security assessment report', 'plan of action'
        ]
    }
    
    @staticmethod
    def detect_intent(message: str) -> Tuple[IntentType, float]:
        """
        Detect user intent from message.
        
        Returns:
            (IntentType, confidence_score)
        """
        message_lower = message.lower()
        
        # Check for MCP-specific intents
        for intent_type, keywords in IntentDetector.MCP_KEYWORDS.items():
            for keyword in keywords:
                if keyword in message_lower:
                    # Calculate confidence based on keyword specificity
                    confidence = 0.9 if len(keyword.split()) > 1 else 0.7
                    return intent_type, confidence
        
        # Default to standard chat
        return IntentType.STANDARD_CHAT, 1.0
    
    @staticmethod
    def should_use_mcp(message: str, threshold: float = 0.6) -> bool:
        """
        Determine if message should be routed to MCP server.
        
        Args:
            message: User's message
            threshold: Confidence threshold for MCP routing
        
        Returns:
            True if should route to MCP, False for standard chat
        """
        intent, confidence = IntentDetector.detect_intent(message)
        return intent != IntentType.STANDARD_CHAT and confidence >= threshold
```

#### 2.3 Modify SimpleChat Backend Main Handler

**File**: `application/backend/app.py` (or equivalent route handler)

Add the MCP integration to the chat endpoint:

```python
import os
from flask import Flask, request, jsonify
from services.mcp_client import PlatformMcpClient, McpResponse
from services.intent_detector import IntentDetector

app = Flask(__name__)

# Initialize MCP client
MCP_SERVER_URL = os.getenv('MCP_SERVER_URL', 'http://localhost:5100')
MCP_ENABLED = os.getenv('MCP_ENABLED', 'false').lower() == 'true'

mcp_client = PlatformMcpClient(MCP_SERVER_URL) if MCP_ENABLED else None


@app.route('/api/chat', methods=['POST'])
def chat():
    """
    Handle chat messages - route to MCP or Azure OpenAI based on intent
    """
    data = request.get_json()
    message = data.get('message', '')
    conversation_id = data.get('conversation_id', '')
    user_id = data.get('user_id', '')
    
    # Check if MCP is enabled and should handle this request
    if MCP_ENABLED and mcp_client and IntentDetector.should_use_mcp(message):
        return handle_mcp_request(message, conversation_id, user_id, data)
    else:
        return handle_standard_chat(message, conversation_id, user_id, data)


def handle_mcp_request(message: str, conversation_id: str, user_id: str, data: dict):
    """Route request to Platform Engineering Copilot MCP server"""
    try:
        # Get intent for logging/tracking
        intent, confidence = IntentDetector.detect_intent(message)
        
        # Prepare context from SimpleChat
        context = {
            "user_id": user_id,
            "session_id": data.get('session_id'),
            "workspace_type": data.get('workspace_type'),  # personal/group
            "intent_detected": intent.value,
            "confidence": confidence
        }
        
        # Call MCP server
        mcp_response = mcp_client.process_message(
            message=message,
            conversation_id=conversation_id,
            user_id=user_id,
            context=context
        )
        
        # Format response for SimpleChat UI
        response = {
            "success": mcp_response.success,
            "response": mcp_response.response,
            "conversation_id": mcp_response.conversation_id,
            "source": "platform_mcp",
            "metadata": {
                "intent": mcp_response.intent_type,
                "confidence": mcp_response.confidence,
                "agents_invoked": mcp_response.agents_invoked,
                "processing_time_ms": mcp_response.processing_time_ms,
                "tool_executed": mcp_response.tool_executed
            },
            "suggestions": mcp_response.suggestions or [],
            "citations": []  # MCP doesn't use SimpleChat's citation format
        }
        
        return jsonify(response)
        
    except Exception as e:
        app.logger.error(f"MCP request failed: {e}")
        return jsonify({
            "success": False,
            "response": f"Infrastructure service error: {str(e)}",
            "source": "error"
        }), 500


def handle_standard_chat(message: str, conversation_id: str, user_id: str, data: dict):
    """Route to existing SimpleChat logic (Azure OpenAI + RAG)"""
    # ... existing SimpleChat implementation
    pass


@app.route('/api/mcp/health', methods=['GET'])
def mcp_health():
    """Check MCP server health"""
    if not MCP_ENABLED:
        return jsonify({"enabled": False})
    
    healthy = mcp_client.health_check() if mcp_client else False
    return jsonify({
        "enabled": True,
        "healthy": healthy,
        "url": MCP_SERVER_URL
    })
```

---

### Phase 3: Frontend Updates (2-3 days)

#### 3.1 Add MCP Status Indicator

**File**: `application/frontend/src/components/McpStatusBadge.jsx`

```jsx
import React, { useState, useEffect } from 'react';
import { Badge, Tooltip } from '@fluentui/react-components';

const McpStatusBadge = () => {
  const [mcpStatus, setMcpStatus] = useState({ enabled: false, healthy: false });
  
  useEffect(() => {
    // Check MCP status on component mount
    fetch('/api/mcp/health')
      .then(res => res.json())
      .then(data => setMcpStatus(data))
      .catch(() => setMcpStatus({ enabled: false, healthy: false }));
  }, []);
  
  if (!mcpStatus.enabled) return null;
  
  return (
    <Tooltip content="Platform Engineering Copilot MCP Server" relationship="label">
      <Badge 
        appearance={mcpStatus.healthy ? 'filled' : 'outline'}
        color={mcpStatus.healthy ? 'success' : 'danger'}
      >
        {mcpStatus.healthy ? '🏗️ Infrastructure AI' : '⚠️ Offline'}
      </Badge>
    </Tooltip>
  );
};

export default McpStatusBadge;
```

#### 3.2 Update Chat Message Component

**File**: `application/frontend/src/components/ChatMessage.jsx`

Add visual distinction for MCP-generated responses:

```jsx
const ChatMessage = ({ message }) => {
  const isMcpResponse = message.source === 'platform_mcp';
  
  return (
    <div className={`chat-message ${isMcpResponse ? 'mcp-response' : ''}`}>
      {isMcpResponse && (
        <div className="mcp-header">
          <span className="mcp-badge">🏗️ Platform Engineering Copilot</span>
          {message.metadata?.agents_invoked && (
            <span className="agents-badge">
              Agents: {message.metadata.agents_invoked.join(', ')}
            </span>
          )}
        </div>
      )}
      
      <div className="message-content">
        {message.response}
      </div>
      
      {/* Show suggestions from MCP */}
      {message.suggestions && message.suggestions.length > 0 && (
        <div className="suggestions">
          <h4>💡 Suggestions:</h4>
          {message.suggestions.map((sug, idx) => (
            <div key={idx} className="suggestion-card">
              <strong>{sug.title}</strong>
              <p>{sug.description}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
```

#### 3.3 Add CSS Styling

**File**: `application/frontend/src/styles/mcp.css`

```css
/* MCP-specific response styling */
.chat-message.mcp-response {
  border-left: 4px solid #0078d4;
  background-color: #f3f9ff;
}

.mcp-header {
  display: flex;
  gap: 10px;
  margin-bottom: 10px;
  padding: 8px;
  background-color: #e6f2ff;
  border-radius: 4px;
}

.mcp-badge {
  background-color: #0078d4;
  color: white;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.agents-badge {
  background-color: #f0f0f0;
  padding: 4px 8px;
  border-radius: 8px;
  font-size: 11px;
  color: #555;
}

.suggestions {
  margin-top: 16px;
  padding: 12px;
  background-color: #fffbf0;
  border: 1px solid #ffd700;
  border-radius: 8px;
}

.suggestion-card {
  padding: 8px;
  margin: 8px 0;
  background-color: white;
  border-radius: 4px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}

.suggestion-card strong {
  color: #0078d4;
}
```

---

### Phase 4: Admin Configuration (1 day)

#### 4.1 Add MCP Settings to Admin Panel

**File**: `application/backend/admin/settings.py`

```python
# Add MCP configuration to admin settings
MCP_SETTINGS = {
    'enabled': {
        'type': 'boolean',
        'default': False,
        'description': 'Enable Platform Engineering Copilot MCP integration',
        'admin_only': True
    },
    'server_url': {
        'type': 'string',
        'default': 'http://platform-mcp:5100',
        'description': 'MCP server URL',
        'admin_only': True
    },
    'intent_threshold': {
        'type': 'number',
        'default': 0.6,
        'min': 0.0,
        'max': 1.0,
        'description': 'Confidence threshold for routing to MCP (0.0-1.0)',
        'admin_only': True
    },
    'timeout_seconds': {
        'type': 'number',
        'default': 120,
        'description': 'MCP request timeout in seconds',
        'admin_only': True
    }
}
```

---

### Phase 5: Testing & Validation (2-3 days)

#### 5.1 Test Scenarios

Create comprehensive test suite:

**File**: `tests/test_mcp_integration.py`

```python
import pytest
from services.mcp_client import PlatformMcpClient
from services.intent_detector import IntentDetector, IntentType


class TestIntentDetection:
    """Test intent detection logic"""
    
    def test_infrastructure_intent(self):
        messages = [
            "Create a storage account named mydata in eastus",
            "Deploy an AKS cluster with 3 nodes",
            "Generate a Bicep template for a web app"
        ]
        for msg in messages:
            intent, confidence = IntentDetector.detect_intent(msg)
            assert intent == IntentType.INFRASTRUCTURE
            assert confidence > 0.6
    
    def test_compliance_intent(self):
        messages = [
            "Run a NIST 800-53 compliance scan",
            "Show FedRAMP High gaps",
            "Generate ATO documentation"
        ]
        for msg in messages:
            intent, _ = IntentDetector.detect_intent(msg)
            assert intent in [IntentType.COMPLIANCE, IntentType.ATO_DOCUMENTATION]
    
    def test_standard_chat(self):
        messages = [
            "What is the weather today?",
            "Tell me about machine learning",
            "Help me understand quantum computing"
        ]
        for msg in messages:
            intent, _ = IntentDetector.detect_intent(msg)
            assert intent == IntentType.STANDARD_CHAT


class TestMcpClient:
    """Test MCP client integration"""
    
    @pytest.fixture
    def mcp_client(self):
        return PlatformMcpClient("http://localhost:5100")
    
    def test_health_check(self, mcp_client):
        """Test MCP server health check"""
        assert mcp_client.health_check() == True
    
    def test_infrastructure_request(self, mcp_client):
        """Test infrastructure provisioning request"""
        response = mcp_client.process_message(
            message="List all resource groups in subscription",
            conversation_id="test-123",
            user_id="test-user"
        )
        assert response.success == True
        assert response.intent_type in ["infrastructure", "discovery"]
        assert len(response.response) > 0
    
    def test_compliance_request(self, mcp_client):
        """Test compliance assessment request"""
        response = mcp_client.process_message(
            message="Run a NIST 800-53 compliance scan",
            conversation_id="test-456",
            user_id="test-user"
        )
        assert response.success == True
        assert "compliance" in response.intent_type.lower()
```

#### 5.2 Integration Test Commands

```bash
# 1. Start all services
docker-compose -f docker-compose.simplechat-integration.yml up -d

# 2. Wait for services to be healthy
sleep 30

# 3. Test MCP health
curl http://localhost:5100/health

# 4. Test SimpleChat MCP status endpoint
curl http://localhost:8080/api/mcp/health

# 5. Test infrastructure query through SimpleChat
curl -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "List all resource groups in my subscription",
    "conversation_id": "test-integration-1",
    "user_id": "test-user"
  }'

# 6. Test compliance query
curl -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Run a NIST 800-53 compliance scan",
    "conversation_id": "test-integration-2",
    "user_id": "test-user"
  }'

# 7. Test standard chat (should NOT go to MCP)
curl -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is the capital of France?",
    "conversation_id": "test-integration-3",
    "user_id": "test-user"
  }'
```

---

## 🔧 Configuration Reference

### Environment Variables

```bash
# MCP Server Configuration
MCP_SERVER_URL=http://platform-mcp:5100
MCP_ENABLED=true
MCP_TIMEOUT_SECONDS=120
MCP_INTENT_THRESHOLD=0.6

# Azure Configuration (shared)
AZURE_TENANT_ID=your-tenant-id
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.us/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o

# Database Configuration
POSTGRES_USER=platform_user
POSTGRES_PASSWORD=your-secure-password
CONNECTION_STRING=Server=platform-postgres;Database=platform_engineering_copilot;User Id=platform_user;Password=your-secure-password;
```

---

## 📊 Example User Flows

### Flow 1: Infrastructure Provisioning

```
User: "Create a storage account named platformdata001 in eastus with hot tier"
  ↓
Intent Detector: INFRASTRUCTURE (confidence: 0.9)
  ↓
Route to: MCP Server
  ↓
MCP Orchestrator: 
  - Detected intent: Infrastructure provisioning
  - Selected agent: Infrastructure Agent
  - Plugin: InfrastructurePlugin.CreateStorageAccount
  ↓
Azure ARM API: Create storage account
  ↓
Response: "✅ Storage account 'platformdata001' created successfully in East US
           - SKU: Standard_LRS (Hot tier)
           - Resource Group: Auto-created 'rg-platformdata001'
           - Endpoint: https://platformdata001.blob.core.windows.net/"
```

### Flow 2: Compliance Assessment

```
User: "Run a NIST 800-53 compliance scan on my subscription"
  ↓
Intent Detector: COMPLIANCE (confidence: 0.95)
  ↓
Route to: MCP Server
  ↓
MCP Orchestrator:
  - Detected intent: Compliance assessment
  - Selected agent: Compliance Agent
  - Plugin: CompliancePlugin.RunNistScan
  ↓
Azure Policy API: Fetch compliance data
NIST Service: Load control framework
  ↓
Response: "📊 NIST 800-53 Compliance Scan Results:
           - Total Controls: 1,015
           - Compliant: 847 (83%)
           - Non-Compliant: 168 (17%)
           - Critical Gaps: 12 (AC, IA, SC families)
           
           💡 Suggestions:
           1. Review Access Control (AC) family - 8 gaps
           2. Enable Azure Security Center recommendations
           3. Configure network encryption (SC-8)"
```

### Flow 3: Cost Analysis

```
User: "Show me cost breakdown for the last 30 days"
  ↓
Intent Detector: COST_MANAGEMENT (confidence: 0.85)
  ↓
Route to: MCP Server
  ↓
MCP Orchestrator:
  - Detected intent: Cost analysis
  - Selected agent: Cost Management Agent
  - Plugin: CostManagementPlugin.GetCostAnalysis
  ↓
Azure Cost Management API: Fetch cost data
  ↓
Response: "💰 Cost Analysis (Last 30 Days):
           - Total Spend: $12,450.23
           - Top Resources:
             1. AKS Cluster 'prod-cluster': $4,200 (34%)
             2. Azure SQL Database: $2,800 (22%)
             3. Storage Accounts: $1,650 (13%)
           
           📈 Trends:
           - 15% increase from previous period
           - Compute costs up 23%
           
           💡 Savings Opportunities:
           1. Reserved instances for AKS: Save $840/month
           2. Unused storage accounts: 3 detected, $200/month"
```

### Flow 4: Standard Chat (NOT routed to MCP)

```
User: "What is machine learning?"
  ↓
Intent Detector: STANDARD_CHAT (confidence: 1.0)
  ↓
Route to: Azure OpenAI (SimpleChat default)
  ↓
Response: [Standard GPT-4 response about ML]
```

---

## 🚀 Deployment Checklist

### Pre-Deployment

- [ ] Clone SimpleChat repository
- [ ] Configure Azure resources (OpenAI, Cosmos DB, AI Search)
- [ ] Set up environment variables
- [ ] Build MCP server Docker image
- [ ] Configure Docker Compose

### Deployment Steps

- [ ] Start MCP server: `docker-compose up -d platform-mcp`
- [ ] Verify MCP health: `curl http://localhost:5100/health`
- [ ] Deploy modified SimpleChat backend
- [ ] Deploy updated SimpleChat frontend
- [ ] Run integration tests
- [ ] Verify intent routing (MCP vs. standard chat)
- [ ] Test all 6 agent types

### Post-Deployment

- [ ] Monitor MCP server logs
- [ ] Set up alerting for MCP failures
- [ ] Configure admin settings
- [ ] Train users on new capabilities
- [ ] Document example queries

---

## 🔐 Security Considerations

### Authentication

**SimpleChat** uses Azure AD (Entra ID) for authentication. Your MCP server should:

1. **Accept Azure AD tokens** from SimpleChat backend
2. **Validate user permissions** before executing infrastructure operations
3. **Implement RBAC** (same roles as your Admin API)

**Recommended Implementation:**

```python
# In MCP client
def process_message(self, message, conversation_id, user_id, context=None, auth_token=None):
    headers = {
        "Content-Type": "application/json"
    }
    
    if auth_token:
        headers["Authorization"] = f"Bearer {auth_token}"
    
    response = self.session.post(
        f"{self.base_url}/api/chat/intelligent-query",
        json=payload,
        headers=headers,
        timeout=self.timeout
    )
```

### Network Security

- [ ] Use **private Docker network** for MCP ↔ SimpleChat communication
- [ ] **Do NOT expose** MCP server (port 5100) to public internet
- [ ] Use **Azure Private Endpoints** for production
- [ ] Enable **TLS/SSL** for MCP server

### Data Privacy

- [ ] MCP server does **NOT store** SimpleChat user data
- [ ] Conversation history stored in SimpleChat's Cosmos DB
- [ ] MCP maintains separate session context (temporary)
- [ ] Ensure **no PII leakage** in MCP logs

---

## 📈 Monitoring & Observability

### Key Metrics to Track

1. **MCP Routing Rate**: % of queries routed to MCP vs. standard chat
2. **Agent Invocation Distribution**: Which agents are used most
3. **Response Times**: MCP vs. Azure OpenAI latency
4. **Error Rates**: MCP failures vs. total requests
5. **Intent Detection Accuracy**: False positives/negatives

### Logging

**MCP Server Logs:**
```bash
docker logs -f platform-mcp
```

**SimpleChat Backend Logs:**
```bash
# Add logging in app.py
app.logger.info(f"Routing to MCP: intent={intent.value}, confidence={confidence}")
```

### Alerts

- [ ] Alert if MCP health check fails (> 3 consecutive failures)
- [ ] Alert if MCP response time > 60 seconds
- [ ] Alert if intent detection confidence drops below 0.5

---

## 🎓 User Training Materials

### Example Queries for Users

**Infrastructure Management:**
- "Create a storage account named mydata in eastus"
- "Deploy an AKS cluster with 3 nodes in westus2"
- "Generate a Bicep template for a 3-tier web app"
- "List all resource groups in my subscription"

**Compliance & Security:**
- "Run a NIST 800-53 compliance scan"
- "Show FedRAMP High gaps in my environment"
- "Generate an ATO package for my application"
- "Run a security vulnerability scan"

**Cost Management:**
- "Show cost breakdown for last 30 days"
- "What are my top 5 most expensive resources?"
- "Recommend cost optimizations"
- "Set a budget of $10,000 for resource group rg-prod"

**Environment Lifecycle:**
- "Clone production environment to staging"
- "List all environments"
- "Scale dev environment to match prod"

**Discovery & Monitoring:**
- "Show health status of all resources"
- "What resources are in rg-production?"
- "Discover orphaned resources"

---

## 🛠️ Troubleshooting

### Issue 1: MCP Server Not Responding

**Symptoms:**
- Timeout errors in SimpleChat
- 502 Bad Gateway

**Solutions:**
```bash
# Check MCP server status
docker ps | grep platform-mcp

# View logs
docker logs platform-mcp --tail 100

# Restart MCP server
docker-compose restart platform-mcp

# Verify network connectivity
docker exec simplechat-backend ping platform-mcp
```

### Issue 2: Incorrect Intent Detection

**Symptoms:**
- Infrastructure queries going to standard chat
- General questions routed to MCP

**Solutions:**
1. Adjust `MCP_INTENT_THRESHOLD` in environment variables
2. Add more keywords to `IntentDetector.MCP_KEYWORDS`
3. Review logs for misclassified queries
4. Consider implementing ML-based intent detection (future enhancement)

### Issue 3: Slow MCP Responses

**Symptoms:**
- MCP requests timing out
- Response times > 60 seconds

**Solutions:**
1. Increase `MCP_TIMEOUT_SECONDS` (default: 120)
2. Check Azure API rate limits
3. Scale MCP server (increase Docker resources)
4. Enable parallel agent execution in orchestrator

---

## 🔮 Future Enhancements

### Phase 2 Features (Post-MVP)

1. **ML-Based Intent Detection**
   - Train a classifier on historical queries
   - Improve routing accuracy
   - Reduce false positives

2. **Hybrid RAG + MCP**
   - Combine SimpleChat's document RAG with MCP infrastructure knowledge
   - "Based on this architecture diagram (uploaded PDF), deploy the infrastructure"

3. **Visual Dashboards**
   - Embed MCP cost dashboards in SimpleChat UI
   - Real-time compliance status widgets
   - Infrastructure topology diagrams

4. **Workflow Automation**
   - Multi-step workflows (provision → configure → scan → document)
   - Approval gates for infrastructure changes
   - Scheduled compliance scans

5. **Advanced Analytics**
   - Track most common infrastructure requests
   - Identify cost optimization opportunities
   - Predict future infrastructure needs

---

## 📚 Additional Resources

### Documentation

- [Platform Engineering Copilot README](../README.md)
- [MCP Server Architecture](./ARCHITECTURE.md)
- [Agent Guide](./AGENTS.md)
- [SimpleChat Documentation](https://github.com/microsoft/simplechat/blob/main/README.md)

### Related Projects

- [Microsoft SimpleChat](https://github.com/microsoft/simplechat)
- [Model Context Protocol](https://github.com/modelcontextprotocol)
- [Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/)

### Support

For issues or questions:
1. Check [Troubleshooting](#-troubleshooting) section
2. Review MCP server logs
3. Open an issue in the repository

---

## ✅ Summary

This integration plan provides a **comprehensive roadmap** for connecting your Platform Engineering Copilot MCP server with Microsoft SimpleChat. The key advantages:

1. **Minimal Changes**: SimpleChat backend modified with just 3 files (mcp_client.py, intent_detector.py, app.py updates)
2. **Smart Routing**: Intent detection ensures infrastructure queries go to MCP, general chat stays in Azure OpenAI
3. **Enhanced Capabilities**: SimpleChat users gain Azure provisioning, NIST compliance, cost optimization
4. **Independent Operation**: MCP server and SimpleChat can be developed/deployed separately
5. **Easy Rollback**: MCP integration can be disabled via environment variable

**Estimated Implementation Time:** 8-12 days (1.5-2 weeks)

**Next Steps:**
1. Review this plan with your team
2. Set up development environment
3. Implement Phase 1 (MCP server deployment)
4. Proceed with Phases 2-5
5. Deploy to production

---

**Document Version:** 1.0  
**Last Updated:** November 21, 2025  
**Author:** Platform Engineering Copilot Team
