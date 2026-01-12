# Platform Engineering Copilot - Documentation

**Version:** 3.0 (Agent FX Architecture)  
**Last Updated:** January 2026

---

## 📚 Documentation Index

### Getting Started

| Document | Description |
|----------|-------------|
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Quick start guide (15 min setup) |
| [AUTHENTICATION.md](./AUTHENTICATION.md) | Azure authentication setup |
| [CAC-AUTHENTICATION-QUICKSTART.md](./CAC-AUTHENTICATION-QUICKSTART.md) | CAC/PIV for Azure Government |

### Architecture

| Document | Description |
|----------|-------------|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | System architecture, Agent FX framework |
| [AGENTS.md](./AGENTS.md) | All 6 agents with tool catalogs |

### Deployment

| Document | Description |
|----------|-------------|
| [DEPLOYMENT.md](./DEPLOYMENT.md) | Docker, ACI, AKS deployment |
| [DEVELOPMENT.md](./DEVELOPMENT.md) | Local development setup |

### Azure Integration

| Document | Description |
|----------|-------------|
| [AZURE-ARC.md](./AZURE-ARC.md) | Hybrid infrastructure with Arc |

### Agent Prompts

Agent prompt files are in [.github/prompts/](../.github/prompts/):

| Prompt | Description |
|--------|-------------|
| [compliance-agent.prompt.md](../.github/prompts/compliance-agent.prompt.md) | Compliance Agent patterns |

---

## Quick Links

**Run the platform:**
```bash
docker-compose up -d
curl http://localhost:5100/health
```

**Key ports:**
- `5100` - MCP Server
- `5001` - Web Chat
- `5003` - Admin UI

**Configuration:** `appsettings.json` at repository root

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    MCP SERVER (5100)                             │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              PlatformAgentGroupChat                         ││
│  │  ├─ PlatformSelectionStrategy                               ││
│  │  └─ 6 Specialized Agents (BaseAgent)                        ││
│  │       ├─ Compliance (12 tools)                              ││
│  │       ├─ Infrastructure (8 tools)                           ││
│  │       ├─ Cost (6 tools)                                     ││
│  │       ├─ Discovery (5 tools)                                ││
│  │       ├─ Environment (4 tools)                              ││
│  │       └─ Security (5 tools)                                 ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## Adding Documentation

1. Create `.md` file in appropriate location
2. Update this index
3. Follow existing formatting patterns
4. Keep docs focused and concise
