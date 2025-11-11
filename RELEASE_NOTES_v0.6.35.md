# Platform Engineering Copilot v0.6.35 Release Notes

**Release Date:** November 11, 2025  
**GitHub Copilot Extension Version:** 0.6.35

---

## 🎉 What's New

### Azure MCP Best Practices Integration
Integrated Azure MCP Server to fetch real-time best practices during infrastructure template generation:
- ✅ **Bicep Templates**: Automatically incorporates Azure best practices via `get_bestpractices` tool
- ✅ **Terraform Templates**: Leverages `azureterraformbestpractices` for Terraform-specific guidance
- ✅ **3 Template Functions Enhanced**:
  - `generate_infrastructure_template` - Standard Bicep/Terraform generation
  - `generate_compliant_infrastructure_template` - FedRAMP/NIST compliant templates
  - `generate_il_compliant_template` - DoD Impact Level (IL4/IL5) templates
- ✅ **Graceful Fallback**: Continues template generation if MCP server unavailable
- 📚 **Live Azure Documentation**: Fetches up-to-date Azure service best practices during generation

### Comprehensive LLM Configuration Guide
New documentation to help users optimize their LLM deployments:
- 📖 **Supported Providers**: Azure OpenAI, OpenAI, Ollama comparison matrix
- 🎯 **Model Requirements by Agent**: Detailed requirements for all 7 specialized agents
  - Context window recommendations (32K - 128K tokens)
  - Recommended temperature settings (0.0 - 0.3)
  - Function calling requirements (8+ to 15+ functions per agent)
- 💰 **Token Budget Planning**: Cost estimates for different operation types
  - Simple queries: ~$0.002
  - Template generation: ~$0.04-$0.08
  - Compliance scans: ~$0.05-$0.10
  - Multi-agent workflows: ~$0.15-$0.30
- 🔧 **Troubleshooting Guide**: 4 common LLM integration issues with solutions
- 🏛️ **Azure Government**: Deployment considerations and endpoint configuration
- 📊 **Performance Comparison**: GPT-4o vs GPT-4 Turbo analysis

**Documentation Added:**
- `docs/GETTING-STARTED.md` - Complete LLM configuration section (270+ lines)
- `README.md` - Quick reference table for model requirements

---

## 🐛 Bug Fixes

### Dependency Injection Issues Resolved
Fixed critical dependency injection failures that prevented server startup:

**Issue 1: Missing ISemanticTextMemory Registration**
- **Problem**: `ServiceWizardStateManager` required `ISemanticTextMemory` but service wasn't registered
- **Solution**: Added singleton registration using `MemoryBuilder` in `ServiceCollectionExtensions.cs`
- **Impact**: Service Creation Agent now works correctly

**Issue 2: Concrete Class Injection**
- **Problem**: `PullRequestReviewService` was injecting concrete `CodeScanningEngine` instead of interface
- **Solution**: Changed to `ICodeScanningEngine` interface injection
- **Impact**: PR review integration now works with proper dependency injection

**Suppressed Warnings:**
- Added `#pragma warning disable SKEXP0001` for experimental Semantic Kernel Memory APIs
- Ensures clean builds while using preview features

---

## 🏗️ Infrastructure Improvements

### Natural Language Azure Context Management
Enhanced Azure context configuration with fast-path routing:
- ✅ **set_azure_subscription**: Natural language subscription switching ("Use subscription X")
- ✅ **set_azure_tenant**: Tenant context management
- ✅ **set_authentication_method**: Switch between managed identity and service principal
- ✅ **get_azure_context**: Query current Azure configuration
- 🚀 **Fast-Path Detection**: Simple commands bypass LLM for instant execution
- 📋 **Formatted Results**: Returns exact function results without LLM paraphrasing

### Service Creation Wizard
New Service Creation Agent with interactive wizard:
- 🎨 **Microservice Templates**: Pre-configured templates for common patterns
- 🔧 **Customization Options**: Interactive configuration of service parameters
- 📦 **Complete Workspace Generation**: Generates code, IaC, CI/CD, and documentation
- 🏛️ **DoD Compliance**: IL4/IL5 compliant templates available

---

## 📚 Documentation Enhancements

### Getting Started Guide Updates
- ✅ **LLM Configuration Section**: Complete guide for choosing and configuring LLM models
- ✅ **Agent Requirements Table**: Quick reference for all 7 specialized agents
- ✅ **Cost Optimization Tips**: Reduce token usage and Azure OpenAI costs
- ✅ **Environment Variables Reference**: Complete list of LLM configuration options
- ✅ **Testing Guide**: How to test with different models (GPT-4o, GPT-4 Turbo)

### README.md Improvements
- ✅ **LLM Requirements Table**: Model, context, temperature, and notes for each agent
- ✅ **Token Usage Estimates**: Cost breakdown per operation type
- ✅ **Supported Providers**: Quick comparison of Azure OpenAI, OpenAI, Ollama

---

## 🔧 Technical Details

### Agent Enhancements

**Infrastructure Agent:**
- Integrated Azure MCP best practices fetching
- Supports both Bicep and Terraform best practices
- Enhanced template generation with real-time Azure guidance
- 15+ kernel functions including Azure MCP integration

**Compliance Agent:**
- NIST 800-53 control mappings (1000+ controls)
- STIG compliance scanning (500+ rules)
- FedRAMP High/Moderate template generation
- DoD Impact Level (IL4/IL5) assessments
- 10+ kernel functions for compliance workflows

**Environment Agent:**
- Temperature set to 0.3 for balanced environment operations
- SharedMemory integration for Bicep template retrieval
- Conversation ID tracking for multi-agent workflows

### Configuration Schema Updates

**Temperature Settings (Planned Feature):**
```json
{
  "AgentConfiguration": {
    "Orchestrator": { "Temperature": 0.0 },      // Deterministic routing
    "Infrastructure": { "Temperature": 0.2 },    // Balanced code gen
    "Compliance": { "Temperature": 0.0 },        // Strict compliance
    "CostManagement": { "Temperature": 0.1 },    // Precise analysis
    "Discovery": { "Temperature": 0.0 },         // Accurate queries
    "Security": { "Temperature": 0.0 },          // Security scanning
    "Document": { "Temperature": 0.3 }           // Creative writing
  }
}
```

> **Note**: Per-agent temperature configuration is currently a planned feature. See `docs/GETTING-STARTED.md` for current behavior and future roadmap.

---

## 📦 Dependencies

### Updated Packages
- Microsoft.SemanticKernel - Memory builder integration
- Azure.Identity - Managed identity support
- System.Text.Json - Enhanced serialization

### Development Dependencies
- .NET 9.0 SDK
- Node.js 18+ (for React frontends)
- Docker Desktop (optional)

---

## 🚀 Deployment Notes

### Docker
```bash
# Pull latest image
docker pull ghcr.io/azurenoops/platform-engineering-copilot:latest

# Or build locally
docker compose -f docker-compose.essentials.yml build platform-mcp
docker compose -f docker-compose.essentials.yml up -d
```

### Environment Variables
```bash
# Required
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_SUBSCRIPTION_ID=your-subscription-id
AZURE_TENANT_ID=your-tenant-id

# Optional - Model overrides per agent
INFRASTRUCTURE_AGENT_MODEL=gpt-4o
COMPLIANCE_AGENT_MODEL=gpt-4o
COST_AGENT_MODEL=gpt-4-turbo
```

### Health Check
```bash
curl http://localhost:5100/health
# Expected: {"status":"healthy","mode":"dual (http+stdio)"}
```

---

## 🔍 Known Issues

### Per-Agent Temperature Configuration
- **Issue**: Temperature settings cannot be configured per agent via `appsettings.json`
- **Current Behavior**: Each agent has hardcoded temperature in code
- **Workaround**: Modify agent source code if custom temperature needed
- **Planned Fix**: Configuration-based temperature override in future release

### Azure MCP Dependency
- **Issue**: Template generation requires Azure MCP Server running
- **Current Behavior**: Gracefully falls back if MCP unavailable (logs warning)
- **Impact**: Best practices guidance not included in templates if MCP down
- **Recommendation**: Deploy Azure MCP Server for production use

---

## 📖 Documentation

### Updated Guides
- [GETTING-STARTED.md](docs/GETTING-STARTED.md) - Complete setup with LLM configuration
- [README.md](README.md) - Quick reference and LLM requirements table
- [ARCHITECTURE.md](docs/ARCHITECTURE.md) - System design and agent details
- [DEPLOYMENT.md](docs/DEPLOYMENT.md) - Production deployment guide

### New Sections
- **LLM Configuration**: Complete guide to choosing and configuring models
- **Token Budget Planning**: Cost estimation and optimization strategies
- **Troubleshooting LLM Issues**: Common problems and solutions

---

## 🙏 Contributors

Thank you to everyone who contributed to this release!

### Code Contributors
- Infrastructure Agent enhancements
- Compliance Agent NIST/STIG integration
- Dependency injection fixes
- Azure MCP best practices integration

### Documentation Contributors
- LLM configuration guide
- Model requirements documentation
- Cost optimization strategies
- Troubleshooting guides

---

## 📝 Breaking Changes

None in this release. All changes are backward compatible.

---

## 🔜 What's Next (v0.7.0 Roadmap)

### Planned Features
- ✨ **Per-Agent Temperature Configuration**: Configurable via `appsettings.json`
- ✨ **Model Override Per Agent**: Deploy different models for different agents
- ✨ **Enhanced Cost Tracking**: Real-time token usage monitoring
- ✨ **Azure MCP Fallback Caching**: Cache best practices for offline scenarios
- ✨ **Multi-Region Support**: Deploy across multiple Azure regions
- ✨ **Advanced Compliance**: CIS Benchmark integration

### Performance Improvements
- 🚀 **Parallel Agent Execution**: Run multiple agents concurrently
- 🚀 **Response Streaming**: Stream LLM responses for better UX
- 🚀 **Intelligent Caching**: Cache frequently used templates and queries

---

## 📬 Feedback & Support

### Report Issues
- **GitHub Issues**: [Create an issue](https://github.com/azurenoops/platform-engineering-copilot/issues)
- **Discussions**: [GitHub Discussions](https://github.com/azurenoops/platform-engineering-copilot/discussions)

### Documentation
- **Complete Docs**: [docs/README.md](docs/README.md)
- **Quick Start**: [docs/GETTING-STARTED.md](docs/GETTING-STARTED.md)
- **Architecture**: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

### Community
- **Questions**: Open a GitHub Discussion
- **Feature Requests**: Open a GitHub Issue with `enhancement` label

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Full Changelog**: [v0.6.34...v0.6.35](https://github.com/azurenoops/platform-engineering-copilot/compare/v0.6.34...v0.6.35)
