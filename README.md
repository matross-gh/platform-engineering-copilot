# Platform Engineering Copilot

> **AI-Powered Infrastructure & Compliance Platform for Azure Government**

Built on .NET 9.0, Microsoft Semantic Kernel, and Model Context Protocol (MCP). Uses the **Microsoft Agent Framework** architecture pattern with specialized AI agents for infrastructure, compliance, and cost management.

---

## Quick Start

```bash
# Clone and build
git clone https://github.com/azurenoops/platform-engineering-copilot.git
cd platform-engineering-copilot
dotnet build

# Azure authentication
az cloud set --name AzureUSGovernment  # or AzureCloud
az login
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)

# Configure
cp .env.example .env
# Edit .env with Azure OpenAI and subscription details

# Run MCP server (Docker)
docker-compose -f docker-compose.essentials.yml up -d
curl http://localhost:5100/health

# Run full platform with web UI
docker-compose up -d
open http://localhost:5001
```

---

## Architecture

The platform uses **Microsoft Agent Framework** - all agents extend `BaseAgent`, all tools extend `BaseTool`.

```
┌─────────────────────────────────────────────────────────────────┐
│                    MCP SERVER (5100)                             │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              PlatformAgentGroupChat                         ││
│  │  ├─ PlatformSelectionStrategy (fast-path routing)          ││
│  │  └─ 6 Specialized Agents                                    ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ Compliance  │ │Infrastructure│ │    Cost     │               │
│  │ (12 tools)  │ │  (8 tools)  │ │  (6 tools)  │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │  Discovery  │ │ Environment │ │  Security   │               │
│  │  (5 tools)  │ │  (4 tools)  │ │  (5 tools)  │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
└─────────────────────────────────────────────────────────────────┘
```

### Agents

| Agent | Tools | Capability |
|-------|-------|------------|
| **Compliance** | 12 | NIST 800-53, DFC integration, remediation |
| **Infrastructure** | 8 | Azure provisioning, Bicep/Terraform |
| **Cost** | 6 | Cost analysis, optimization |
| **Discovery** | 5 | Resource inventory, health |
| **Environment** | 4 | Lifecycle, cloning |
| **Security** | 5 | Vulnerability, policy |

---

## Example Queries

```
"Run NIST 800-53 compliance scan"
"Create storage account data001 in rg-dr"
"Show cost analysis for last 30 days"
"What's my secure score?"
"List all VMs in my subscription"
"Generate Bicep for AKS cluster"
```

---

## MCP Client Configuration

### GitHub Copilot

`~/.copilot/config.json`:
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

`~/Library/Application Support/Claude/claude_desktop_config.json`:
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

---

## Project Structure

```
src/
├── Platform.Engineering.Copilot.Mcp/      # MCP Server (HTTP:5100 + stdio)
├── Platform.Engineering.Copilot.Agents/   # All agents and tools
│   ├── Common/BaseAgent.cs, BaseTool.cs   # Microsoft Agent Framework base classes
│   ├── Compliance/                        # Compliance Agent (12 tools)
│   ├── Infrastructure/                    # Infrastructure Agent
│   ├── CostManagement/                    # Cost Agent
│   ├── Discovery/                         # Discovery Agent
│   ├── Environment/                       # Environment Agent
│   └── Security/                          # Security Agent
├── Platform.Engineering.Copilot.Core/     # Shared models, interfaces
├── Platform.Engineering.Copilot.Chat/     # Web Chat UI (5001)
└── Platform.Engineering.Copilot.Admin.API/ # Admin API (5003)
```

---

## Configuration

All configuration in `appsettings.json`:

```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "Enabled": true,
      "Temperature": 0.2,
      "DefenderForCloud": {
        "Enabled": true,
        "IncludeSecureScore": true
      }
    },
    "InfrastructureAgent": {
      "Enabled": true,
      "DefaultRegion": "usgovvirginia"
    }
  }
}
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture, Microsoft Agent Framework framework |
| [docs/AGENTS.md](docs/AGENTS.md) | All agents with tool catalogs |
| [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) | Docker, ACI, AKS deployment |
| [docs/GETTING-STARTED.md](docs/GETTING-STARTED.md) | Complete setup guide |
| [docs/AUTHENTICATION.md](docs/AUTHENTICATION.md) | Azure authentication |

### Agent Prompts

Detailed agent patterns in `.github/prompts/`:
- [compliance-agent.prompt.md](.github/prompts/compliance-agent.prompt.md)

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 9.0 |
| AI Framework | Microsoft Semantic Kernel 1.26+ |
| MCP | ModelContextProtocol 0.4.0-preview |
| Azure SDK | Azure.ResourceManager.* |
| Database | SQLite (default) |

---

## Development

```bash
# Build
dotnet build Platform.Engineering.Copilot.sln

# Test
dotnet test Platform.Engineering.Copilot.sln

# Run MCP server (stdio mode)
dotnet run --project src/Platform.Engineering.Copilot.Mcp

# Run MCP server (HTTP mode)
dotnet run --project src/Platform.Engineering.Copilot.Mcp -- --http
```

---

## License

MIT License - see [LICENSE](LICENSE)
