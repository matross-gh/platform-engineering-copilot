# Platform Engineering Copilot - Visual Architecture

## System Overview

```mermaid
graph TB
    subgraph "Client Layer"
        WEB[Web Chat UI<br/>Port 5001]
        ADMIN[Admin Console<br/>Port 5003]
        COPILOT[GitHub Copilot<br/>stdio]
        CLAUDE[Claude Desktop<br/>stdio]
        M365[M365 Extension<br/>Teams]
    end

    subgraph "MCP Server Layer - Port 5100"
        MCP[MCP Server<br/>HTTP + stdio]
        
        subgraph "Agent Orchestration"
            GROUPCHAT[PlatformAgentGroupChat]
            STRATEGY[PlatformSelectionStrategy<br/>Fast-path routing]
            TERM[TerminationStrategy]
        end
    end

    subgraph "Specialized Agents"
        COMP[Compliance Agent<br/>12 tools<br/>NIST 800-53]
        INFRA[Infrastructure Agent<br/>8 tools<br/>Bicep/Terraform]
        COST[Cost Management<br/>6 tools<br/>Analysis/Optimization]
        DISC[Discovery Agent<br/>5 tools<br/>Resource Inventory]
        ENV[Environment Agent<br/>4 tools<br/>Lifecycle Management]
        SEC[Security Agent<br/>5 tools<br/>Vulnerability Scanning]
    end

    subgraph "Azure Services"
        OPENAI[Azure OpenAI<br/>GPT-4]
        ARM[Azure Resource Manager]
        GRAPH[Resource Graph]
        DEFENDER[Defender for Cloud]
        COSTMGMT[Cost Management API]
        POLICY[Azure Policy]
        KV[Key Vault]
    end

    subgraph "Data Layer"
        SQL[(SQL Database<br/>Assessments/History)]
        STORAGE[Blob Storage<br/>Evidence/Artifacts]
        REDIS[(Redis Cache<br/>Session/State)]
        LOGS[Log Analytics<br/>Monitoring]
    end

    WEB --> MCP
    ADMIN --> MCP
    COPILOT --> MCP
    CLAUDE --> MCP
    M365 --> MCP

    MCP --> GROUPCHAT
    GROUPCHAT --> STRATEGY
    STRATEGY --> COMP
    STRATEGY --> INFRA
    STRATEGY --> COST
    STRATEGY --> DISC
    STRATEGY --> ENV
    STRATEGY --> SEC

    COMP --> OPENAI
    INFRA --> OPENAI
    COST --> OPENAI
    DISC --> OPENAI
    ENV --> OPENAI
    SEC --> OPENAI

    COMP --> DEFENDER
    COMP --> POLICY
    COMP --> SQL
    COMP --> STORAGE
    
    INFRA --> ARM
    INFRA --> GRAPH
    INFRA --> SQL
    
    COST --> COSTMGMT
    COST --> SQL
    
    DISC --> GRAPH
    DISC --> ARM
    
    ENV --> ARM
    ENV --> SQL
    
    SEC --> DEFENDER
    SEC --> POLICY

    MCP --> REDIS
    MCP --> LOGS
    MCP --> KV

    style MCP fill:#0078d4,color:#fff
    style GROUPCHAT fill:#00bcf2,color:#000
    style COMP fill:#7fba00,color:#000
    style INFRA fill:#ffb900,color:#000
    style COST fill:#f25022,color:#fff
    style DISC fill:#00a4ef,color:#000
    style ENV fill:#737373,color:#fff
    style SEC fill:#e74856,color:#fff
```

## Agent Architecture (Microsoft Agent Framework)

```mermaid
graph LR
    subgraph "Microsoft Agent Framework Pattern"
        BASE[BaseAgent<br/>Abstract Class]
        TOOL[BaseTool<br/>Abstract Class]
    end

    subgraph "Agent Implementation"
        AGENT[Compliance Agent<br/>extends BaseAgent]
        AGENT_PROPS["Properties:<br/>• AgentId: 'compliance'<br/>• AgentName<br/>• Description<br/>• RegisteredTools[]"]
        AGENT_METHODS["Methods:<br/>• ProcessAsync()<br/>• GetSystemPrompt()"]
    end

    subgraph "Tool Implementation"
        TOOL1[run_compliance_assessment<br/>extends BaseTool]
        TOOL2[batch_remediation<br/>extends BaseTool]
        TOOL3[execute_remediation<br/>extends BaseTool]
        TOOL_PROPS["Properties:<br/>• Name<br/>• Description<br/>• Parameters[]"]
        TOOL_METHODS["Methods:<br/>• ExecuteAsync()<br/>• AsAITool()"]
    end

    BASE --> AGENT
    AGENT --> AGENT_PROPS
    AGENT --> AGENT_METHODS
    AGENT --> TOOL1
    AGENT --> TOOL2
    AGENT --> TOOL3
    TOOL --> TOOL1
    TOOL --> TOOL2
    TOOL --> TOOL3
    TOOL1 --> TOOL_PROPS
    TOOL1 --> TOOL_METHODS

    style BASE fill:#0078d4,color:#fff
    style TOOL fill:#0078d4,color:#fff
    style AGENT fill:#7fba00,color:#000
```

## Data Flow - Compliance Assessment Example

```mermaid
sequenceDiagram
    participant User
    participant MCP as MCP Server
    participant Strategy as Selection Strategy
    participant Compliance as Compliance Agent
    participant Defender as Defender for Cloud
    participant OpenAI as Azure OpenAI
    participant DB as SQL Database
    participant Storage as Blob Storage

    User->>MCP: "Run NIST 800-53 compliance scan"
    MCP->>Strategy: Route request
    Strategy->>Compliance: Keyword match: "compliance", "NIST"
    Compliance->>Defender: Fetch security findings
    Defender-->>Compliance: Return findings + secure score
    Compliance->>DB: Query historical assessments
    DB-->>Compliance: Return previous results
    Compliance->>OpenAI: Analyze findings with context
    OpenAI-->>Compliance: Assessment + recommendations
    Compliance->>DB: Store assessment results
    Compliance->>Storage: Save evidence artifacts
    Storage-->>Compliance: Confirmation
    Compliance->>OpenAI: Generate summary
    OpenAI-->>Compliance: User-friendly report
    Compliance-->>MCP: Assessment complete
    MCP-->>User: Display results with remediation plan
```

## Deployment Architecture Options

```mermaid
graph TB
    subgraph "Deployment Options"
        
        subgraph "Option 1: Local Development"
            LOCAL[Developer Machine]
            DOTNET[.NET 9.0 Runtime]
            SQLITE[(SQLite DB)]
            LOCAL --> DOTNET
            DOTNET --> SQLITE
        end

        subgraph "Option 2: Docker Compose"
            DOCKER[Docker Engine]
            MCP_C[MCP Container]
            CHAT_C[Chat Container]
            ADMIN_C[Admin Container]
            REDIS_C[Redis Container]
            DOCKER --> MCP_C
            DOCKER --> CHAT_C
            DOCKER --> ADMIN_C
            DOCKER --> REDIS_C
        end

        subgraph "Option 3: Azure App Service"
            PLAN[App Service Plan<br/>P1v3]
            APP_MCP[MCP App Service]
            APP_CHAT[Chat App Service]
            APP_ADMIN[Admin App Service]
            PLAN --> APP_MCP
            PLAN --> APP_CHAT
            PLAN --> APP_ADMIN
        end

        subgraph "Option 4: Azure Container Instances"
            ACI_MCP[ACI: MCP Server<br/>2 cores, 4GB]
            ACI_CHAT[ACI: Chat UI]
            ACI_ADMIN[ACI: Admin UI]
        end

        subgraph "Option 5: Azure Kubernetes Service"
            AKS[AKS Cluster<br/>3 nodes]
            POD_MCP[MCP Pod]
            POD_CHAT[Chat Pod]
            POD_ADMIN[Admin Pod]
            INGRESS[Ingress Controller]
            AKS --> POD_MCP
            AKS --> POD_CHAT
            AKS --> POD_ADMIN
            AKS --> INGRESS
        end
    end

    subgraph "Shared Azure Resources"
        SQL2[(Azure SQL)]
        BLOB2[Blob Storage]
        KV2[Key Vault]
        MONITOR[App Insights]
        OPENAI2[Azure OpenAI]
    end

    APP_MCP --> SQL2
    APP_CHAT --> SQL2
    APP_ADMIN --> SQL2
    ACI_MCP --> SQL2
    POD_MCP --> SQL2
    
    APP_MCP --> BLOB2
    ACI_MCP --> BLOB2
    POD_MCP --> BLOB2

    APP_MCP --> KV2
    ACI_MCP --> KV2
    POD_MCP --> KV2

    APP_MCP --> MONITOR
    ACI_MCP --> MONITOR
    POD_MCP --> MONITOR

    APP_MCP --> OPENAI2
    ACI_MCP --> OPENAI2
    POD_MCP --> OPENAI2

    style LOCAL fill:#f0f0f0,color:#000
    style DOCKER fill:#2496ed,color:#fff
    style PLAN fill:#0078d4,color:#fff
    style ACI_MCP fill:#0078d4,color:#fff
    style AKS fill:#326ce5,color:#fff
```

## Solution Project Structure

```mermaid
graph TB
    subgraph "Platform Engineering Copilot Solution"
        
        subgraph "src/ - Application Code"
            CORE[Platform.Engineering<br/>.Copilot.Core<br/>Shared utilities, models]
            AGENTS[Platform.Engineering<br/>.Copilot.Agents<br/>6 specialized agents]
            MCP_PROJ[Platform.Engineering<br/>.Copilot.Mcp<br/>MCP Server + API]
            CHAT[Platform.Engineering<br/>.Copilot.Chat<br/>Web UI]
            STATE[Platform.Engineering<br/>.Copilot.State<br/>State management]
            CHANNELS[Platform.Engineering<br/>.Copilot.Channels<br/>Communication]
        end

        subgraph "tests/ - Test Suites"
            UNIT[Unit Tests<br/>xUnit]
            INTEGRATION[Integration Tests<br/>Azure integration]
            MANUAL[Manual Tests<br/>Scripts]
        end

        subgraph "infra/ - Infrastructure"
            BICEP[Bicep Templates<br/>Azure deployment]
            TERRAFORM[Terraform<br/>Alternative IaC]
            KUBERNETES[Kubernetes Manifests<br/>K8s deployment]
        end

        subgraph "docs/ - Documentation"
            ARCH_DOC[ARCHITECTURE.md]
            AGENTS_DOC[AGENTS.md]
            DEPLOY_DOC[DEPLOYMENT.md]
            AUTH_DOC[AUTHENTICATION.md]
        end

        subgraph "extensions/ - Extensions"
            M365_EXT[M365 Extension<br/>Teams integration]
        end
    end

    MCP_PROJ --> CORE
    AGENTS --> CORE
    CHAT --> CORE
    CHAT --> MCP_PROJ
    STATE --> CORE
    CHANNELS --> CORE
    MCP_PROJ --> AGENTS
    MCP_PROJ --> STATE
    MCP_PROJ --> CHANNELS

    UNIT --> CORE
    UNIT --> AGENTS
    INTEGRATION --> MCP_PROJ

    style CORE fill:#0078d4,color:#fff
    style AGENTS fill:#7fba00,color:#000
    style MCP_PROJ fill:#00bcf2,color:#000
```

## Infrastructure Components (Bicep Modules)

```mermaid
graph TB
    MAIN[main.bicep<br/>Orchestration]

    subgraph "Bicep Modules"
        NET[network.bicep<br/>VNet + Subnets]
        SQL_MOD[sql.bicep<br/>SQL Server + DB]
        KV_MOD[keyvault.bicep<br/>Key Vault]
        STORAGE_MOD[storage.bicep<br/>Storage Account]
        MONITOR_MOD[monitoring.bicep<br/>App Insights + Logs]
        APP_MOD[app-services.bicep<br/>App Service Plan]
        ACR_MOD[acr.bicep<br/>Container Registry]
        AKS_MOD[aks.bicep<br/>AKS Cluster]
        ACI_MOD[aci.bicep<br/>Container Instances]
    end

    MAIN --> NET
    MAIN --> SQL_MOD
    MAIN --> KV_MOD
    MAIN --> STORAGE_MOD
    MAIN --> MONITOR_MOD
    MAIN --> APP_MOD
    MAIN --> ACR_MOD
    MAIN --> AKS_MOD
    MAIN --> ACI_MOD

    style MAIN fill:#ff6c37,color:#fff
```

## Agent Interaction Flow

```mermaid
graph LR
    subgraph "User Query Processing"
        INPUT[User Input:<br/>'Create storage account<br/>and scan compliance']
    end

    subgraph "Agent Orchestration"
        ROUTER[Selection Strategy<br/>Keyword Analysis]
        INFRA_A[Infrastructure Agent<br/>Creates storage]
        COMP_A[Compliance Agent<br/>Scans compliance]
    end

    subgraph "Tool Execution"
        CREATE_TOOL[create_storage_account<br/>Tool]
        SCAN_TOOL[run_compliance_assessment<br/>Tool]
    end

    subgraph "Azure Integration"
        ARM_API[Azure RM API<br/>Provision storage]
        DEFENDER_API[Defender API<br/>Compliance scan]
    end

    INPUT --> ROUTER
    ROUTER -->|Keywords: create, storage| INFRA_A
    ROUTER -->|Keywords: compliance, scan| COMP_A
    INFRA_A --> CREATE_TOOL
    CREATE_TOOL --> ARM_API
    ARM_API -->|Success| COMP_A
    COMP_A --> SCAN_TOOL
    SCAN_TOOL --> DEFENDER_API
    DEFENDER_API -->|Results| COMP_A
    COMP_A -->|Final Report| INPUT

    style INPUT fill:#f0f0f0,color:#000
    style ROUTER fill:#00bcf2,color:#000
    style INFRA_A fill:#ffb900,color:#000
    style COMP_A fill:#7fba00,color:#000
```

## Key Features by Agent

```mermaid
mindmap
  root((Platform<br/>Engineering<br/>Copilot))
    Compliance Agent
      NIST 800-53 Assessment
      Automated Remediation
      Defender Integration
      Evidence Collection
      SSP/SAR/POA&M Generation
      Audit Trail
    Infrastructure Agent
      Resource Provisioning
      Bicep Generation
      Terraform Generation
      Network Design
      Predictive Scaling
      Compliance Enhancement
    Cost Management
      Cost Analysis
      Budget Monitoring
      Optimization Recommendations
      Anomaly Detection
      Forecasting
      Multi-subscription Support
    Discovery Agent
      Resource Inventory
      Health Monitoring
      Performance Metrics
      Dependency Mapping
      Tag Compliance
    Environment Agent
      Environment Lifecycle
      Environment Cloning
      Configuration Management
      Deployment Tracking
    Security Agent
      Vulnerability Scanning
      Policy Enforcement
      Secure Score
      Secret Detection
      STIG Compliance
```

## Technology Stack

```mermaid
graph TB
    subgraph "Frontend"
        BLAZOR[Blazor WebAssembly]
        HTML[HTML/CSS/JS]
    end

    subgraph "Backend"
        DOTNET[.NET 9.0]
        SK[Microsoft Semantic Kernel]
        MCP_SDK[Model Context Protocol]
        FRAMEWORK[Microsoft Agent Framework]
    end

    subgraph "AI/ML"
        GPT[Azure OpenAI GPT-4]
        EMBEDDING[Text Embeddings]
        VECTOR[Vector Search]
    end

    subgraph "Data Storage"
        SQL_DB[SQL Server / SQLite]
        BLOB[Azure Blob Storage]
        REDIS_CACHE[Redis Cache]
    end

    subgraph "Azure Services"
        ARM_SVC[Azure Resource Manager]
        GRAPH_SVC[Resource Graph]
        DEFENDER_SVC[Defender for Cloud]
        COST_SVC[Cost Management]
        POLICY_SVC[Azure Policy]
        KV_SVC[Key Vault]
        MONITOR_SVC[Azure Monitor]
    end

    subgraph "Infrastructure"
        DOCKER_INFRA[Docker]
        K8S[Kubernetes]
        BICEP_INFRA[Bicep IaC]
        TERRAFORM_INFRA[Terraform IaC]
    end

    BLAZOR --> DOTNET
    HTML --> DOTNET
    DOTNET --> SK
    DOTNET --> MCP_SDK
    DOTNET --> FRAMEWORK
    SK --> GPT
    SK --> EMBEDDING
    FRAMEWORK --> GPT
    
    DOTNET --> SQL_DB
    DOTNET --> BLOB
    DOTNET --> REDIS_CACHE
    
    DOTNET --> ARM_SVC
    DOTNET --> GRAPH_SVC
    DOTNET --> DEFENDER_SVC
    DOTNET --> COST_SVC
    DOTNET --> POLICY_SVC
    DOTNET --> KV_SVC
    DOTNET --> MONITOR_SVC

    style DOTNET fill:#512bd4,color:#fff
    style SK fill:#0078d4,color:#fff
    style GPT fill:#10a37f,color:#fff
    style DOCKER_INFRA fill:#2496ed,color:#fff
    style K8S fill:#326ce5,color:#fff
```

---

## Legend

- **Blue Components**: Core platform services
- **Green Components**: AI agents
- **Orange Components**: Infrastructure/deployment
- **Red Components**: Security-related
- **Gray Components**: Supporting services

