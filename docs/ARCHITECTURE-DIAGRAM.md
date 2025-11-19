# Platform Engineering Copilot - Architecture Diagram

## Multi-Agent System Architecture

```mermaid
graph TB
    %% User Interface Layer
    User[👤 Chat Users]
    WebApp[🌐 Q&A Web App<br/>React/TypeScript]
    
    %% API Gateway Layer
    ChatAPI[💬 Chat API<br/>ASP.NET Core]
    AdminAPI[⚙️ Admin API<br/>Management]
    
    %% Orchestration Layer
    Orchestrator[🎯 Orchestrator Agent<br/>Intent Classification<br/>Execution Planning<br/>Agent Coordination]
    
    %% Specialized Agents
    InfraAgent[🏗️ Infrastructure Agent<br/>Bicep/Terraform Generation<br/>Network Design<br/>Predictive Scaling<br/>Compliance Templates]
    
    ComplianceAgent[🛡️ Compliance Agent<br/>NIST 800-53<br/>FedRAMP High<br/>DoD IL5<br/>RMF/ATO]
    
    CostAgent[💰 Cost Management Agent<br/>Cost Analysis<br/>Budget Tracking<br/>Optimization]
    
    DiscoveryAgent[🔍 Discovery Agent<br/>Resource Inventory<br/>Tagging Analysis<br/>Resource Search]
    
    EnvAgent[🌍 Environment Agent<br/>Deployment<br/>Validation<br/>Configuration]
    
    SecurityAgent[🔐 Security Agent<br/>Threat Detection<br/>Vulnerability Scan<br/>Policy Enforcement]
    
    DocAgent[📝 Document Agent<br/>RMF Documentation<br/>SSP Generation<br/>Technical Docs]
    
    %% Shared Services Layer
    SharedMemory[🧠 Shared Memory<br/>Conversation Context<br/>Agent Communication<br/>Template Storage]
    
    SemanticKernel[⚙️ Semantic Kernel<br/>LLM Orchestration<br/>Function Calling<br/>Plugin Management]
    
    %% Plugin System
    InfraPlugin[📦 Infrastructure Plugin<br/>Template Generation<br/>Network Design<br/>Scaling Analysis]
    
    DeployPlugin[📦 Deployment Plugin<br/>Bicep Deployment<br/>Azure Resource Manager]
    
    CompliancePlugin[📦 Compliance Plugin<br/>Policy Validation<br/>Control Assessment]
    
    %% Knowledge Base Layer
    VectorDB[🔮 Vector Database<br/>Azure AI Search<br/>Embeddings<br/>Semantic Search]
    
    CosmosDB[(🗄️ Cosmos DB<br/>Conversation History<br/>Source Documents<br/>Q&A Persistence)]
    
    BlobStorage[(💾 Blob Storage<br/>Generated Templates<br/>System Prompts<br/>Source Metadata)]
    
    %% External Services Layer
    AzureOpenAI[🤖 Azure OpenAI<br/>GPT-4o<br/>Embeddings API<br/>Function Calling]
    
    AzureMCP[🔧 Azure MCP Server<br/>Best Practices<br/>Schema Validation<br/>Resource Provider APIs]
    
    AzureRM[☁️ Azure Resources<br/>Subscriptions<br/>Resource Groups<br/>Resources]
    
    %% User Flow
    User -->|Ask Question| WebApp
    WebApp -->|Submit Query| ChatAPI
    
    %% API to Orchestrator
    ChatAPI -->|Route Request| Orchestrator
    AdminAPI -->|Manage Config| SharedMemory
    
    %% Orchestrator to Agents
    Orchestrator -->|Delegate Task| InfraAgent
    Orchestrator -->|Delegate Task| ComplianceAgent
    Orchestrator -->|Delegate Task| CostAgent
    Orchestrator -->|Delegate Task| DiscoveryAgent
    Orchestrator -->|Delegate Task| EnvAgent
    Orchestrator -->|Delegate Task| SecurityAgent
    Orchestrator -->|Delegate Task| DocAgent
    
    %% Agents to Shared Services
    InfraAgent -->|Use| SemanticKernel
    ComplianceAgent -->|Use| SemanticKernel
    CostAgent -->|Use| SemanticKernel
    DiscoveryAgent -->|Use| SemanticKernel
    EnvAgent -->|Use| SemanticKernel
    SecurityAgent -->|Use| SemanticKernel
    DocAgent -->|Use| SemanticKernel
    
    %% Agents to Shared Memory
    InfraAgent <-->|Share Context| SharedMemory
    ComplianceAgent <-->|Share Context| SharedMemory
    CostAgent <-->|Share Context| SharedMemory
    DiscoveryAgent <-->|Share Context| SharedMemory
    EnvAgent <-->|Share Context| SharedMemory
    SecurityAgent <-->|Share Context| SharedMemory
    DocAgent <-->|Share Context| SharedMemory
    Orchestrator <-->|Coordinate| SharedMemory
    
    %% Plugins
    InfraAgent -->|Use| InfraPlugin
    InfraAgent -->|Use| DeployPlugin
    ComplianceAgent -->|Use| CompliancePlugin
    
    %% Semantic Kernel to LLM
    SemanticKernel -->|Request Completion| AzureOpenAI
    SemanticKernel -->|Request Embeddings| AzureOpenAI
    
    %% Knowledge Base
    SemanticKernel -->|Search Context| VectorDB
    VectorDB <-->|Index/Retrieve| CosmosDB
    VectorDB <-->|Store Vectors| BlobStorage
    
    %% External Integrations
    InfraPlugin -->|Fetch Best Practices| AzureMCP
    DeployPlugin -->|Deploy Resources| AzureRM
    DiscoveryAgent -->|Query Resources| AzureRM
    CompliancePlugin -->|Scan Resources| AzureRM
    CostAgent -->|Get Cost Data| AzureRM
    
    %% Response Flow
    InfraAgent -->|Return Result| Orchestrator
    ComplianceAgent -->|Return Result| Orchestrator
    CostAgent -->|Return Result| Orchestrator
    DiscoveryAgent -->|Return Result| Orchestrator
    EnvAgent -->|Return Result| Orchestrator
    SecurityAgent -->|Return Result| Orchestrator
    DocAgent -->|Return Result| Orchestrator
    
    Orchestrator -->|Aggregate Response| ChatAPI
    ChatAPI -->|Return Answer| WebApp
    WebApp -->|Display| User
    
    %% Styling
    classDef userLayer fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef apiLayer fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef orchestratorLayer fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef agentLayer fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    classDef sharedLayer fill:#fff9c4,stroke:#f57f17,stroke-width:2px
    classDef pluginLayer fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    classDef dataLayer fill:#e0f2f1,stroke:#004d40,stroke-width:2px
    classDef externalLayer fill:#efebe9,stroke:#3e2723,stroke-width:2px
    
    class User,WebApp userLayer
    class ChatAPI,AdminAPI apiLayer
    class Orchestrator orchestratorLayer
    class InfraAgent,ComplianceAgent,CostAgent,DiscoveryAgent,EnvAgent,SecurityAgent,DocAgent agentLayer
    class SharedMemory,SemanticKernel sharedLayer
    class InfraPlugin,DeployPlugin,CompliancePlugin pluginLayer
    class VectorDB,CosmosDB,BlobStorage dataLayer
    class AzureOpenAI,AzureMCP,AzureRM externalLayer
```

## Infrastructure Agent - Conversational Requirements Gathering Flow

```mermaid
sequenceDiagram
    participant User
    participant Orchestrator
    participant InfraAgent
    participant SemanticKernel
    participant GPT4o as Azure OpenAI GPT-4o
    participant InfraPlugin
    participant SharedMemory
    participant AzureMCP
    
    %% Turn 1: Initial Request
    User->>Orchestrator: "I need an AKS cluster"
    Orchestrator->>InfraAgent: Route task to Infrastructure Agent
    InfraAgent->>SharedMemory: Check conversation context
    SharedMemory-->>InfraAgent: No prior context
    
    InfraAgent->>SemanticKernel: Process request with system prompt
    SemanticKernel->>GPT4o: Analyze request details
    Note over GPT4o: Detail Count: 1<br/>(only resource type)<br/>→ ASK QUESTIONS
    
    GPT4o-->>SemanticKernel: Generate clarifying questions
    SemanticKernel-->>InfraAgent: Return questions
    InfraAgent->>SharedMemory: Store conversation state
    InfraAgent-->>Orchestrator: Questions about environment, security, monitoring
    Orchestrator-->>User: "What environment? Zero Trust? Monitoring? ACR/KV?"
    
    %% Turn 2: User Answers
    User->>Orchestrator: "dev, zero trust, add monitoring, add ACR and KV"
    Orchestrator->>InfraAgent: Continue conversation
    InfraAgent->>SharedMemory: Retrieve conversation context
    SharedMemory-->>InfraAgent: Previous messages + answers
    
    InfraAgent->>SemanticKernel: Process with full context
    SemanticKernel->>GPT4o: User answered questions - generate template
    Note over GPT4o: Follow-up detected<br/>All info provided<br/>→ CALL FUNCTION
    
    GPT4o->>InfraPlugin: generate_infrastructure_template()
    Note over InfraPlugin: resourceType: "aks"<br/>location: "usgovvirginia"<br/>nodeCount: 3<br/>environment: "dev"<br/>zeroTrust: true<br/>monitoring: true<br/>includeACR: true<br/>includeKeyVault: true
    
    InfraPlugin->>AzureMCP: Fetch Azure best practices for AKS
    AzureMCP-->>InfraPlugin: Security hardening guidance
    
    InfraPlugin->>InfraPlugin: Generate Bicep modules
    Note over InfraPlugin: - main.bicep<br/>- modules/cluster.bicep<br/>- modules/network.bicep<br/>- modules/identity.bicep<br/>- modules/monitoring.bicep<br/>- modules/security.bicep<br/>- modules/acr.bicep<br/>- parameters.json
    
    InfraPlugin-->>GPT4o: Template files generated
    GPT4o-->>SemanticKernel: Return complete template code
    SemanticKernel-->>InfraAgent: Template response
    
    InfraAgent->>SharedMemory: Store generated templates
    InfraAgent-->>Orchestrator: Return template files
    Orchestrator-->>User: Display Bicep code + deployment instructions
    
    %% Optional Turn 3: Actual Provisioning
    Note over User: Optional: User can now<br/>request actual deployment
    User->>Orchestrator: "Actually provision this in subscription 453c..."
    Orchestrator->>EnvAgent: Execute deployment (not shown)
```

## Data Flow - Template Generation vs Actual Provisioning

```mermaid
graph LR
    %% Template Generation Flow (Safe)
    subgraph TemplateGen["📝 Template Generation (Safe - No Azure Resources Created)"]
        direction TB
        TG1[User Request:<br/>'I need AKS cluster']
        TG2[Infrastructure Agent<br/>Asks Questions]
        TG3[User Provides Answers]
        TG4[Generate Bicep Template]
        TG5[Store in SharedMemory]
        TG6[Return Code to User]
        
        TG1 --> TG2 --> TG3 --> TG4 --> TG5 --> TG6
    end
    
    %% Actual Provisioning Flow (Creates Resources)
    subgraph ActualProv["⚠️ Actual Provisioning (Creates Real Azure Resources)"]
        direction TB
        AP1[User Request:<br/>'Actually provision this']
        AP2[Infrastructure Agent<br/>Retrieves Template]
        AP3[Environment Agent<br/>Deploys to Azure]
        AP4[Discovery Agent<br/>Verifies Resources]
        AP5[Compliance Agent<br/>Scans New Resources]
        AP6[Cost Agent<br/>Estimates Costs]
        
        AP1 --> AP2 --> AP3 --> AP4 --> AP5 --> AP6
    end
    
    TemplateGen -.->|"User decides<br/>to provision"| ActualProv
    
    style TemplateGen fill:#e8f5e9,stroke:#1b5e20,stroke-width:3px
    style ActualProv fill:#ffebee,stroke:#c62828,stroke-width:3px
```

## Agent Execution Patterns

```mermaid
graph TD
    subgraph Sequential["Sequential Execution"]
        S1[Infrastructure Agent<br/>Generates Template] --> S2[Compliance Agent<br/>Validates Template]
        S2 --> S3[Cost Agent<br/>Estimates Cost]
    end
    
    subgraph Parallel["Parallel Execution"]
        P1[Orchestrator<br/>Broadcasts Task]
        P1 --> P2[Discovery Agent<br/>Resource Inventory]
        P1 --> P3[Cost Agent<br/>Cost Analysis]
        P1 --> P4[Compliance Agent<br/>Compliance Scan]
        P2 --> P5[Aggregated<br/>Health Report]
        P3 --> P5
        P4 --> P5
    end
    
    subgraph Collaborative["Collaborative Workflow"]
        C1[Infrastructure Agent<br/>Design + Generate]
        C2[Environment Agent<br/>Deploy]
        C3[Discovery Agent<br/>Verify]
        C4[Compliance Agent<br/>Scan]
        C5[Cost Agent<br/>Estimate]
        
        C1 -->|Template| C2
        C2 -->|Resource IDs| C3
        C3 -->|Resource List| C4
        C4 -->|Compliance Data| C5
    end
    
    style Sequential fill:#fff3e0,stroke:#e65100
    style Parallel fill:#e1f5ff,stroke:#01579b
    style Collaborative fill:#f3e5f5,stroke:#4a148c
```

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | React, TypeScript | Q&A Web Interface |
| **API** | ASP.NET Core 9.0 | REST API, WebSockets |
| **Orchestration** | Semantic Kernel | LLM coordination, plugin management |
| **LLM** | Azure OpenAI GPT-4o | Function calling, conversation |
| **Embeddings** | Azure OpenAI text-embedding-3-large | Semantic search |
| **Vector DB** | Azure AI Search | Hybrid search (vector + keyword) |
| **Document Store** | Cosmos DB | Conversation history, Q&A pairs |
| **Blob Storage** | Azure Blob Storage | Templates, source documents |
| **MCP Server** | Azure MCP | Best practices, schema validation |
| **IaC** | Bicep, Terraform | Infrastructure as Code |
| **Deployment** | Azure Resource Manager | Resource provisioning |

## Key Features

### 🤖 Conversational Requirements Gathering
- Agents ask targeted questions before taking action
- Smart defaults based on environment type
- One question cycle maximum
- Context maintained across conversation turns

### 🔮 Intelligent Agent Routing
- Intent classification by Orchestrator
- Multi-agent workflows (sequential, parallel, collaborative)
- Execution plan validation
- Agent-to-agent communication via SharedMemory

### 🛡️ Compliance-Aware Templates
- Automatic injection of security controls
- FedRAMP High, DoD IL5, NIST 800-53, SOC2, GDPR
- Template validation against compliance frameworks
- RMF/ATO documentation generation

### 📊 Predictive Scaling
- Forecast resource needs (hours, days, weeks ahead)
- Auto-scaling optimization
- Historical performance analysis
- Cost impact assessment

### 🔧 Azure MCP Integration
- Real-time Azure best practices
- Schema validation against Azure Resource Provider APIs
- Enhanced security hardening
- Graceful fallback if MCP unavailable

## Deployment Architecture

```mermaid
graph TB
    subgraph AzureGov["Azure Government Cloud"]
        subgraph AppServices["App Services (Premium v3)"]
            ChatApp[Chat API<br/>ASP.NET Core]
            AdminApp[Admin API<br/>Management]
        end
        
        subgraph AI["Azure OpenAI"]
            GPT4o[GPT-4o<br/>Deployment]
            Embeddings[text-embedding-3-large]
        end
        
        subgraph Data["Data Services"]
            AISearch[Azure AI Search<br/>Vector + Keyword]
            Cosmos[Cosmos DB<br/>SQL API]
            Blob[Blob Storage<br/>Templates]
        end
        
        subgraph Network["Networking"]
            VNet[Virtual Network<br/>10.0.0.0/16]
            PrivateEndpoints[Private Endpoints<br/>Secure Access]
        end
        
        subgraph Security["Security"]
            KeyVault[Key Vault<br/>Secrets, Keys]
            ManagedID[Managed Identity<br/>RBAC]
            Defender[Defender for Cloud]
        end
    end
    
    ChatApp --> GPT4o
    ChatApp --> Embeddings
    ChatApp --> AISearch
    ChatApp --> Cosmos
    ChatApp --> Blob
    ChatApp --> KeyVault
    AdminApp --> Cosmos
    
    AISearch -.->|Private Endpoint| PrivateEndpoints
    Cosmos -.->|Private Endpoint| PrivateEndpoints
    Blob -.->|Private Endpoint| PrivateEndpoints
    KeyVault -.->|Private Endpoint| PrivateEndpoints
    
    PrivateEndpoints -.-> VNet
    
    ChatApp -->|Uses| ManagedID
    AdminApp -->|Uses| ManagedID
    Defender -.->|Monitors| ChatApp
    Defender -.->|Monitors| AdminApp
    
    style AzureGov fill:#f0f4c3,stroke:#827717,stroke-width:3px
    style AppServices fill:#e1f5ff,stroke:#01579b
    style AI fill:#f3e5f5,stroke:#4a148c
    style Data fill:#e0f2f1,stroke:#004d40
    style Network fill:#fff3e0,stroke:#e65100
    style Security fill:#ffebee,stroke:#c62828
```

---

**Last Updated:** November 12, 2025  
**Version:** 0.6.35
