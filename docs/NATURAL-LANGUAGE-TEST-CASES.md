# Natural Language Test Cases for Multi-Agent System

**Last Updated:** November 11, 2025  
**Version:** 0.6.35

Quick reference guide for testing the platform engineering copilot multi-agent system.

## 🎯 Quick Start

```bash
# Test via API
curl -X POST http://localhost:5100/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{"message": "YOUR_QUERY", "conversationId": "test-1"}' | jq .

# Test via MCP endpoint (new)
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "YOUR_QUERY"}' | jq .
```

## ✨ What's New in v0.6.35

### Azure MCP Best Practices Integration
- Template generation now fetches real-time Azure best practices
- Bicep templates include security hardening automatically
- Terraform templates leverage Azure-specific guidance
- Graceful fallback if MCP server unavailable

### LLM Configuration & Optimization
- Documented model requirements for all 7 agents
- Token cost estimates for different operations
- Temperature recommendations per agent (planned feature)
- Troubleshooting guide for common LLM issues

### Bug Fixes
- Fixed dependency injection issues (ISemanticTextMemory, ICodeScanningEngine)
- Service Creation Agent now works correctly
- PR review integration functional

## 📖 Understanding Request Types

| Type | Keywords | Agents | Time | Output |
|------|----------|--------|------|--------|
| **Compliance Scan** | "check", "scan", "assess" + existing resources | ComplianceAgent | 30-60s | Compliance report |
| **Template Gen** | "create", "deploy" (no "actually") | InfrastructureAgent | 10-30s | Bicep code |
| **Actual Provision** | "actually provision", "make it live" | Infrastructure + Environment agents | 60-180s | Real Azure resources ⚠️ |
| **Info/Guidance** | "what are", "how do I", "best practices" | 1 agent | 5-15s | Documentation |

> **🔑 Key Difference:** "Deploy AKS" → generates template. "Actually provision this AKS" → creates real resources.

---

## 📋 Core Test Cases by Agent

### 🏗️ Infrastructure Agent

#### Azure MCP Best Practices Tests (NEW in v0.6.35)

**Test 1.31: Template with Best Practices Integration**
```
Generate a production AKS cluster with Azure best practices built in
```
**Expected Output:**
- ✅ Bicep template with security hardening from Azure MCP
- ✅ Best practices automatically incorporated (no manual specification)
- ✅ Logs show: "📚 Fetching Azure best practices for aks via Azure MCP"
- ✅ Logs show: "✅ Retrieved Azure best practices guidance (XXX chars)"
- ✅ Template includes: TLS 1.2+, RBAC enabled, private cluster, managed identity
- ⏱️ Time: 20-35 seconds (includes MCP call)

**Validation:**
- ✅ Check logs for MCP integration messages
- ✅ Template has enhanced security configurations
- ✅ Response mentions best practices source
- ✅ Graceful handling if MCP server unavailable

**Test 1.32: Terraform with Azure Best Practices**
```
Create Terraform for AKS with Azure best practices for usgovvirginia
```
**Expected Output:**
- ✅ Terraform files (main.tf, variables.tf, outputs.tf)
- ✅ Calls `azureterraformbestpractices` MCP tool
- ✅ Includes Azure Gov specific configurations
- ✅ Best practices in comments and README
- ⏱️ Time: 20-35 seconds

**Test 1.33: FedRAMP Template with MCP Enhancement**
```
Generate FedRAMP High compliant infrastructure with Azure best practices
```
**Expected Output:**
- ✅ Template combines NIST controls + Azure MCP best practices
- ✅ Logs show both compliance controls and MCP guidance
- ✅ Enhanced security beyond basic FedRAMP requirements
- ⏱️ Time: 25-40 seconds

**Test 1.34: MCP Server Unavailable - Graceful Fallback**
```
Generate AKS template (with MCP server stopped)
```
**Expected Output:**
- ✅ Template still generated successfully
- ✅ Warning logged: "⚠️ Could not retrieve best practices from Azure MCP - continuing without them"
- ✅ No errors, just missing enhanced guidance
- ⏱️ Time: 15-25 seconds (faster without MCP call)

#### Template Generation (Safe - No Resources Created)

**Test 1: Basic AKS Template**
```
Deploy an AKS cluster with 3 nodes in usgovvirginia
```
**Expected Output:**
- ✅ Bicep template files generated (main.bicep, modules/*)
- ✅ Shows file paths and configuration summary
- ✅ **NO** "resources created" message until phase 2 complete
- ⏱️ Time: 15-30 seconds

**Test 2: Multi-Resource Template**
```
Create infrastructure for web app with SQL database, storage, and VNet in usgovarizona
```
**Expected Output:**
- ✅ Multiple Bicep modules (app service, database, storage, network)
- ✅ Network topology diagram
- ✅ Deployment commands provided
- ⏱️ Time: 20-35 seconds

**Test 3: Compliance-Enhanced Template**
```
Generate FedRAMP High compliant AKS template with all NIST controls
```
**Expected Output:**
- ✅ Template with compliance controls (AC, AU, SC, IA families)
- ✅ Compliance validation report
- ✅ Control implementation checklist
- ⏱️ Time: 25-40 seconds

#### Actual Provisioning (⚠️ Creates Real Resources)

**Test 4: Explicit Provisioning**
```
Actually provision a production AKS cluster with 5 nodes in usgovvirginia. Make it live now in subscription 00000000-0000-0000-0000-000000000000
```
**Expected Output:**
- ✅ All 5 agents invoked (Infrastructure → Environment → Discovery → Compliance → Cost)
- ✅ "Deployment started" or "Resources created successfully"
- ✅ Resource IDs and connection info
- ✅ Compliance score for new resources
- ✅ Cost estimate
- ⏱️ Time: 60-180 seconds

#### Network Design

**Test 5: 3-Tier Network**
```
Design a 3-tier network with 10.0.0.0/16, include Bastion and Firewall
```
**Expected Output:**
- ✅ Tier layout: Web (10.0.1.0/24), App (10.0.2.0/24), Data (10.0.3.0/24)
- ✅ Special subnets: Bastion, AzureFirewall, Gateway
- ✅ CIDR calculations with IP counts
- ✅ Service endpoints configuration
- ⏱️ Time: 10-20 seconds

---

### 🛡️ Compliance Agent

**Test 6: Subscription Compliance Scan**
```
Check NIST 800-53 compliance for subscription 00000000-0000-0000-0000-000000000000
```
**Expected Output:**
- ✅ Compliance score (e.g., "78% compliant")
- ✅ Failing controls list (AC-2, AU-3, SC-7, etc.)
- ✅ Findings with severity (Critical/High/Medium/Low)
- ✅ Remediation recommendations
- ⏱️ Time: 30-60 seconds

**Test 7: FedRAMP Compliance Gap Analysis**
```
What do I need for FedRAMP High ATO? Analyze my AKS cluster compliance gaps
```
**Expected Output:**
- ✅ FedRAMP High control list (AC, AU, SC, IA, PE families)
- ✅ Current vs required controls comparison
- ✅ Implementation guidance for missing controls
- ✅ RMF documentation requirements
- ⏱️ Time: 15-30 seconds

---

### 💰 Cost Management Agent

**Test 8: Cost Analysis**
```
Show me Azure spending last month broken down by service
```
**Expected Output:**
- ✅ Total spend amount
- ✅ Cost breakdown by service (AKS: $X, SQL: $Y, Storage: $Z)
- ✅ Month-over-month comparison
- ✅ Top 5 most expensive resources
- ⏱️ Time: 10-20 seconds

**Test 9: Cost Optimization**
```
Find cost savings opportunities in subscription 00000000-0000-0000-0000-000000000000
```
**Expected Output:**
- ✅ Top 10 recommendations with estimated savings
- ✅ Quick wins (e.g., "Stop unused VM: Save $450/month")
- ✅ Medium-term optimizations (e.g., "Reserved instances: Save $2,100/year")
- ✅ Implementation guide
- ⏱️ Time: 20-40 seconds

---

### 🔍 Discovery Agent

**Test 10: Resource Inventory**
```
What resources are running in my subscription? Give me complete inventory
```
**Expected Output:**
- ✅ Resource count by type
- ✅ Resource list with names, types, locations
- ✅ Resource groups summary
- ✅ Untagged resources flagged
- ⏱️ Time: 15-30 seconds

**Test 11: Find Specific Resources**
```
Find all AKS clusters across all subscriptions
```
**Expected Output:**
- ✅ List of AKS clusters with names
- ✅ Node count, Kubernetes version per cluster
- ✅ Location and subscription ID
- ✅ Resource group associations
- ⏱️ Time: 10-25 seconds

---

### 🌍 Environment Agent

**Test 12: Environment Overview**
```
Show all environments - dev, test, prod configurations
```
**Expected Output:**
- ✅ Environment list with status
- ✅ Configuration differences highlighted
- ✅ Resource counts per environment
- ✅ Deployment history summary
- ⏱️ Time: 10-20 seconds

---

## 🆕 MCP-Enhanced Functions (19 New Test Cases)

### Resource Discovery + MCP

**Test M1: Schema-Validated Discovery**
```
Discover resources in subscription 00000000-0000-0000-0000-000000000000 with Bicep schema validation
```
**Expected Output:**
- ✅ Resource list with schema validation status
- ✅ API versions shown
- ✅ Schema compliance: Valid ✓ / Invalid ✗
- 🔧 MCP Tool: `bicepschema`

**Test M2: Best Practices for Resource**
```
Get best practices for AKS cluster "aks-prod-eastus-001"
```
**Expected Output:**
- ✅ Resource details
- ✅ Azure best practices specific to AKS
- ✅ Security hardening recommendations
- ✅ Cost optimization tips
- 🔧 MCP Tool: `get_bestpractices`

**Test M3: Documentation Search**
```
Search Azure docs for AKS private cluster networking
```
**Expected Output:**
- ✅ Relevant documentation excerpts
- ✅ Configuration examples
- ✅ Links to official docs
- 🔧 MCP Tool: `get_bestpractices`

---

### Infrastructure + MCP

**Test M4: Template with Best Practices**
```
Generate production AKS template with Azure best practices built in
```
**Expected Output:**
- ✅ Bicep template with security hardening
- ✅ Best practices checklist applied
- ✅ Monitoring/logging configured
- ✅ Deployment guide
- 🔧 MCP Tools: `bicepschema`, `get_bestpractices`

**Test M5: Azure Developer CLI Deployment**
```
Deploy template using azd with environment setup
```
**Expected Output:**
- ✅ azd init/up commands
- ✅ Environment configuration steps
- ✅ Service connections setup
- ✅ Deployment status
- 🔧 MCP Tool: `azd`

---

### Deployment + MCP

**Test M6: Bicep Validation**
```
Validate my AKS Bicep template against official schema
```
**Expected Output:**
- ✅ Validation result: PASS/FAIL
- ✅ Schema violations list (if any)
- ✅ Fix recommendations
- ✅ API version compatibility check
- 🔧 MCP Tool: `bicepschema`

**Test M7: Pre-Deployment Checks**
```
Run pre-deployment validation for my infrastructure template
```
**Expected Output:**
- ✅ Schema validation ✓
- ✅ Best practices check ✓
- ✅ Security validation ✓
- ✅ Cost estimate: $X/month
- ✅ Overall: PASS/FAIL with recommendations
- 🔧 MCP Tools: `bicepschema`, `get_bestpractices`

**Test M8: Deployment Troubleshooting**
```
My AKS deployment failed with "QuotaExceeded" error. Help troubleshoot
```
**Expected Output:**
- ✅ Root cause analysis
- ✅ Activity log details
- ✅ Remediation steps
- ✅ Quota increase guidance
- 🔧 MCP Tools: `applens`, `activitylogs`

---

### Compliance + MCP

**Test M9: Azure Policy Validation**
```
Check Azure Policy compliance for resource group "rg-prod-eastus"
```
**Expected Output:**
- ✅ Policy compliance status
- ✅ Violations list with details
- ✅ Remediation recommendations
- ✅ Compliance percentage
- 🔧 MCP Tool: `azurepolicy`

**Test M10: NIST Compliance Guidance**
```
How do I make my AKS cluster NIST 800-53 compliant?
```
**Expected Output:**
- ✅ Applicable NIST controls list
- ✅ Current compliance gaps
- ✅ Implementation guidance per control
- ✅ Configuration examples
- 🔧 MCP Tool: `get_bestpractices`

---

### Security + MCP

**Test M11: Vulnerability Scan**
```
Scan resource group "rg-prod-eastus" for vulnerabilities
```
**Expected Output:**
- ✅ Security findings list
- ✅ Severity: Critical (X) | High (Y) | Medium (Z) | Low (W)
- ✅ CVE details where applicable
- ✅ Remediation guidance
- 🔧 MCP Tool: `securitycenter`

**Test M12: AKS Security Hardening**
```
Security best practices for hardening my AKS cluster
```
**Expected Output:**
- ✅ Network security (private cluster, NSGs)
- ✅ Identity (managed identity, RBAC)
- ✅ Encryption requirements
- ✅ Threat protection setup
- 🔧 MCP Tools: `get_bestpractices`, `defender`

---

### Cost Management + MCP

**Test M13: Cost Optimization with Advisor**
```
Analyze subscription 00000000-0000-0000-0000-000000000000 for cost savings
```
**Expected Output:**
- ✅ Top 10 opportunities ranked by savings
- ✅ Estimated monthly savings: $X | Annual: $Y
- ✅ Azure Advisor recommendations
- ✅ FinOps best practices
- ✅ Implementation guide (quick/medium/long-term)
- 🔧 MCP Tools: `advisor`, `get_bestpractices`

**Test M14: Budget Recommendations**
```
What budget should I set? Recommend alerts based on current spending
```
**Expected Output:**
- ✅ Suggested monthly budget: $X (current avg + 10% buffer)
- ✅ Alert thresholds:
  - 50%: Informational
  - 75%: Warning
  - 90%: Critical
  - 100%: Budget exceeded
- ✅ Automation scripts (Azure CLI)
- 🔧 MCP Tool: `get_bestpractices`

---

### Environment Management + MCP

**Test M15: Environment Best Practices**
```
Best practices for production environment with tagging and naming conventions
```
**Expected Output:**
- ✅ Mandatory tags: Environment, Owner, CostCenter, Application
- ✅ Recommended tags: ManagedBy, Criticality, DataClassification
- ✅ Tag value examples
- ✅ Naming patterns: `{type}-{app}-{env}-{region}-{seq}`
- ✅ Examples: `aks-platform-prod-eastus-001`
- ✅ Well-Architected Framework guidance
- 🔧 MCP Tool: `get_bestpractices`

**Test M16: Basic Environment Validation**
```
Validate environment "dev-aks-eastus" basic configuration
```
**Expected Output:**
- ✅ Checks: Naming ✓ | Tagging ⚠ | RBAC ✓ | Networking ✓
- ✅ Overall: Passed with warnings
- ✅ Critical issues: 0 | Warnings: 2
- ✅ Next steps provided
- 🔧 MCP Tools: `azurepolicy`, `securitycenter`

**Test M17: Comprehensive Environment Validation**
```
Run comprehensive validation of "prod-aks-eastus" in subscription 00000000-0000-0000-0000-000000000000
```
**Expected Output:**
- ✅ 16 total checks (Basic + Standard + Comprehensive)
- ✅ Categories: Naming, Tagging, RBAC, Network, Security, Monitoring, Backup, Cost, Compliance, DR, Scaling, Docs
- ✅ Azure Policy compliance
- ✅ Defender for Cloud findings
- ✅ Azure Advisor recommendations
- ✅ Remediation scripts (az tag create, az network nsg create, etc.)
- ✅ Prioritized next steps
- 🔧 MCP Tools: `azurepolicy`, `securitycenter`, `advisor`, `get_bestpractices`

---

## 🔄 Multi-Agent Workflows

**Test W1: Sequential - Deploy and Assess**
```
Deploy AKS with 3 nodes in usgovvirginia, then run compliance check
```
**Expected Output:**
- ✅ Agent sequence: Infrastructure → Compliance
- ✅ Template generated
- ✅ Compliance report on template
- ⏱️ Time: 25-50 seconds

**Test W2: Parallel - Health Report**
```
Complete health report: show resources, costs, and compliance status
```
**Expected Output:**
- ✅ Agents parallel: Discovery + Cost + Compliance
- ✅ Resource inventory
- ✅ Cost breakdown
- ✅ Compliance score
- ⏱️ Time: 30-60 seconds

**Test W3: Complex - Full Deployment**
```
Turn 1: "Deploy production AKS for new mission"
Turn 2-4: (AI asks questions, user answers)
Turn 5: "yes proceed"
Turn 6: (Template shown)
Turn 7: "Actually provision this in subscription 00000000-0000-0000-0000-000000000000"
```
**Expected Output:**
- ✅ Turns 1-6: Infrastructure agent only (template generation)
- ✅ Turn 7: All 5 agents (Infrastructure → Environment → Discovery → Compliance → Cost)
- ✅ Real resources created
- ✅ Compliance scan on new resources
- ✅ Cost estimate
- ⏱️ Total time: ~3-5 minutes

---

## 🧪 Validation Checklist

Use this to verify correct behavior:

### Template Generation (Should NOT Create Resources)
- [ ] Only InfrastructureAgent invoked
- [ ] Response contains Bicep code or file paths
- [ ] Time: 10-30 seconds (15-35 with MCP)
- [ ] NO "resources created" message
- [ ] NO real Azure resources exist after
- [ ] **NEW:** Check logs for MCP best practices integration
- [ ] **NEW:** Template includes security hardening from Azure MCP

### Actual Provisioning (Creates Real Resources ⚠️)
- [ ] All 5 agents invoked in sequence
- [ ] Time: 60-180 seconds
- [ ] Response says "deployment started" or "resources created"
- [ ] Real Azure resources exist after
- [ ] Compliance scan on NEW resources only (not entire subscription)

### Compliance Scan (Existing Resources)
- [ ] Only ComplianceAgent invoked
- [ ] Time: 30-60 seconds
- [ ] Scans EXISTING resources
- [ ] NO template generated
- [ ] NO resources created

### MCP-Enhanced Functions (NEW in v0.6.35)
- [ ] Response mentions MCP tool used
- [ ] Includes best practices or external guidance
- [ ] Combines platform data + MCP insights
- [ ] Provides actionable recommendations
- [ ] **Infrastructure templates:** Logs show "📚 Fetching Azure best practices"
- [ ] **Infrastructure templates:** Enhanced security configurations present
- [ ] **Graceful degradation:** Works even if MCP server unavailable

### LLM Configuration Validation (NEW in v0.6.35)
- [ ] Server starts successfully with all agents
- [ ] GPT-4o or GPT-4 Turbo configured
- [ ] Context window: 32K-128K tokens depending on agent
- [ ] Token usage logged for cost tracking
- [ ] Function calling works correctly (99%+ accuracy with GPT-4o)

---

## 🐛 Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Template generation invokes all 5 agents | Orchestrator misrouted | Check for "actually provision" keywords |
| "Actually provision" only generates template | ExecutionPlanValidator bug | ✅ **FIXED** in v0.6.35 |
| Compliance scans entire subscription | Scope too broad | Specify resource group or resource ID |
| MCP functions return generic responses | MCP server not connected | Check Azure MCP server status at port 5100 |
| Show me AKS module returns wrong code | Module filter bug | Use specific module name ("AKS cluster", "database", etc.) |
| **Server won't start - DI errors** | **Missing service registration** | ✅ **FIXED** in v0.6.35 - ISemanticTextMemory registered |
| **PR review not working** | **Concrete class injection** | ✅ **FIXED** in v0.6.35 - Uses ICodeScanningEngine interface |
| **Azure best practices not included** | **MCP server not running** | Start MCP server or template will use defaults |
| **High LLM costs** | **Wrong model/no caching** | Use GPT-4o, enable caching (see GETTING-STARTED.md) |
| **Function calling failures** | **Using GPT-3.5 or older** | Upgrade to GPT-4o or GPT-4 Turbo |
| **Slow responses** | **Using GPT-4 Turbo** | Switch to GPT-4o (2-3x faster) |

---

## � Quick Reference

### Test Subscription
**⚠️ Use Your Own Subscription:** Replace with your Azure subscription ID in all tests.

To set your subscription for testing:
```bash
# Azure CLI
az account set --subscription "YOUR-SUBSCRIPTION-ID"

# Or use in environment variables
export AZURE_SUBSCRIPTION_ID="YOUR-SUBSCRIPTION-ID"

# Or use the chat plugin function
"Set my Azure subscription to YOUR-SUBSCRIPTION-ID"
```

### Regions
- `usgovvirginia` - US Gov Virginia
- `usgovarizona` - US Gov Arizona

### Common Resource Names
- AKS: `aks-{env}-{region}-{seq}`
- RG: `rg-{app}-{env}-{region}`
- Storage: `st{app}{env}{region}{seq}`

### LLM Configuration (NEW in v0.6.35)

**Recommended Models:**
- **Production:** GPT-4o (best function calling, 2-3x faster)
- **Fallback:** GPT-4 Turbo (slower but reliable)
- **Not Recommended:** GPT-3.5, Ollama (limited function calling)

**Model Requirements by Agent:**
| Agent | Model | Context | Temp | Notes |
|-------|-------|---------|------|-------|
| Orchestrator | GPT-4o/GPT-4T | 128K | 0.0 | Deterministic routing |
| Infrastructure | **GPT-4o** | 128K | 0.2 | Code gen + **Azure MCP** |
| Compliance | GPT-4o/GPT-4T | 128K | 0.0 | Strict compliance |
| Cost | GPT-4o/GPT-4T | 32K | 0.1 | Precise analysis |
| Discovery | GPT-4o/GPT-4T | 32K | 0.0 | Accurate queries |
| Security | **GPT-4o** | 64K | 0.0 | Threat detection |
| Document | GPT-4o/GPT-4T | 128K | 0.3 | Creative writing |

**Token Cost Estimates (GPT-4o):**
- Simple query: ~$0.002 (100-500 input + 50-200 output)
- Template generation: ~$0.04-$0.08 (2000-5000 input + 2000-6000 output)
- Compliance scan: ~$0.05-$0.10 (3000-8000 input + 1000-4000 output)
- Multi-agent workflow: ~$0.15-$0.30 (8000-20000 input + 5000-15000 output)

**Environment Variables:**
```bash
# Required
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_SUBSCRIPTION_ID=00000000-0000-0000-0000-000000000000
AZURE_TENANT_ID=your-tenant-id
```

**📖 For detailed LLM configuration, see:** [GETTING-STARTED.md](./GETTING-STARTED.md#llm-configuration--model-requirements)

### MCP Tools Available
- `bicepschema` - Schema validation
- `azd` - Azure Developer CLI
- `aks` - AKS operations
- `deploy` - Deployment orchestration
- `azurepolicy` - Policy compliance
- `securitycenter` - Security scanning
- `threatintelligence` - Threat detection
- `defender` - Defender for Cloud
- `advisor` - Azure Advisor
- `applens` - Application diagnostics
- `activitylogs` - Activity logs
- `get_bestpractices` - **Azure best practices (most used, auto-integrated in templates)**
- `azureterraformbestpractices` - **Terraform best practices (NEW in v0.6.35)**

---

## 🤖 Conversational Requirements Gathering

> **✨ NEW: All Agents Support Conversational Requirements Gathering**  
> All 5 primary agents (Infrastructure, CostManagement, Compliance, Environment, Discovery) use a conversational approach to gather requirements BEFORE taking action:
> - **InfrastructureAgent**: Asks about environment, security, monitoring, scaling, integrations
> - **ComplianceAgent**: Asks about subscription, scope, framework, control families
> - **CostManagementAgent**: Asks about analysis scope, breakdown preferences, optimization focus
> - **DiscoveryAgent**: Asks about resource types, search criteria, output format
> - **EnvironmentAgent**: Asks about environment type, location, configuration level
>
> **Note:** ServiceCreation functionality is planned but not yet implemented. Service Wizard functionality is available via ServiceWizardPlugin.
>
> **Expected Conversation Flow:**
> 1. User makes initial request (may be vague or detailed)
> 2. Agent asks 2-5 targeted questions to fill gaps
> 3. User provides answers
> 4. Agent IMMEDIATELY calls the appropriate function (no confirmation needed)
>
> **Key Principles:**
> - ONE question cycle only (don't repeat questions)
> - Ask ONLY for missing critical information
> - Use smart defaults for non-critical details
> - Check SharedMemory for context from previous agents

---

### 🏗️ InfrastructureAgent (Bicep/Terraform/IaC)

> **🤖 NEW: Conversational Requirements Gathering**  
> The InfrastructureAgent now uses a conversational approach to gather requirements BEFORE generating templates:
> - **No immediate template generation** - AI asks 3-5 targeted questions first
> - **Resource-specific questions** - Tailored for AKS, App Service, Container Apps, Storage, etc.
> - **Production-ready defaults** - Based on environment type and compliance needs
> - **Complete configuration** - Uses 50+ configuration points from user responses
> - **Supports**: AKS (Zero Trust), App Service (security + scaling), Container Apps (DAPR + microservices)
>
> **Expected Conversation Flow:**
> 1. User makes initial request
> 2. AI asks 3-5 probing questions about environment, security, monitoring, scaling, integrations
> 3. User provides answers
> 4. AI summarizes proposed configuration with all features explained
> 5. User confirms
> 6. AI generates modular dynamic template with all configurations applied

> **✨ Network Topology Design Feature**  
> The InfrastructureAgent now includes intelligent network topology design with dynamic tier naming:
> - **1 tier**: Application
> - **2 tiers**: Web → Data
> - **3 tiers**: Web → Application → Data
> - **4 tiers**: DMZ → Web → Application → Data  
> - **5 tiers**: DMZ → Web → Application → Business → Data
> - **6+ tiers**: DMZ → AppTier1...N → Data
>
> Automatically calculates subnet CIDRs, includes special subnets (Gateway, Bastion, Firewall), and configures service endpoints.

> **🔮 Predictive Scaling Feature**  
> The InfrastructureAgent includes AI-powered predictive scaling capabilities:
> - **Forecast scaling needs**: Predict resource requirements hours, days, or weeks ahead
> - **Optimize auto-scaling**: Analyze and tune scaling configurations for efficiency
> - **Performance analysis**: Review historical scaling events and cost impact
> - **Supports**: VM Scale Sets (VMSS), App Service Plans, AKS clusters
> - **Metrics tracked**: CPU, memory, network, custom metrics
> - **Cost optimization**: Identify over/under-provisioning patterns

> **🛡️ Compliance-Aware Template Generation**  
> The InfrastructureAgent automatically enhances templates with compliance controls:
> - **FedRAMP High**: 10 NIST controls (AC, AU, SC, IA families) - Federal Risk and Authorization Management Program
> - **DoD IL5**: 12 NIST controls (adds PE-3, PE-6) - Department of Defense Impact Level 5
> - **NIST 800-53**: 10 baseline controls - National Institute of Standards and Technology
> - **SOC2**: 4 Trust Services Criteria (CC6.1, CC6.6, CC6.7, CC7.2) - System and Organization Controls
> - **GDPR**: 3 Articles (Art-32, Art-33, Art-34) - General Data Protection Regulation
> - **Auto-injection**: TLS, RBAC, encryption at rest/transit, audit logging, network isolation
> - **Validation**: Scans generated templates for compliance violations and provides findings
> - **Metadata**: Includes required controls, validation status, and remediation recommendations

---

#### 🆕 Conversational Requirements Gathering Tests

**Test 1.1: AKS with Requirements Gathering (Template Generation)**
```
I need to deploy a new Kubernetes cluster in Azure. Can you help me set up an AKS cluster in the US GOV Virginia region with 3 nodes in subscription 00000000-0000-0000-0000-000000000000?
```
**Expected Behavior:**
1. AI acknowledges request enthusiastically
2. AI asks 5-7 questions:
   - Environment type (dev/staging/production)?
   - Security requirements (Zero Trust, private endpoints)?
   - Compliance needs (FedRAMP High, DoD IL5)?
   - Monitoring preferences (Container Insights, Prometheus)?
   - Auto-scaling needed (min/max nodes)?
   - ACR integration required?
   - Key Vault for secrets?
3. User answers: "dev, zero trust, yes, add monitoring, add ACR and KV"
4. AI summarizes full configuration with Zero Trust features
5. User says: "yes proceed"
6. **AI generates Bicep template ONLY** - modular with 12 files (main, cluster, network, identity, monitoring, security, acr, etc.)
7. **NO actual resources are created** (this is template generation, not provisioning)

**Validation:**
- ✅ AI does NOT immediately generate template
- ✅ AI asks questions before calling template generation function
- ✅ Questions cover environment, security, monitoring, scaling, integrations
- ✅ **CRITICAL:** Only InfrastructureAgent is invoked (no Environment, Compliance, Discovery, or Cost agents)
- ✅ **CRITICAL:** Response includes Bicep template code, NOT "resources created successfully"
- ✅ Final template uses BicepAKSModuleGenerator (refactored dynamic code)
- ✅ Template includes modern patterns (Managed Identity, API 2023-10-01, K8s 1.30, Zero Trust)
- ✅ All 50+ configuration points properly set from conversation
- ✅ Execution time: 15-30 seconds (not 60-180 seconds for provisioning)

**To Actually Provision:** After receiving the template, user must say:
```
Actually provision this template and create the resources
```
Then the full workflow executes: Infrastructure → Environment → Compliance → Discovery → Cost

**Test 1.2: App Service with Requirements Gathering (Template Generation)**
```
Deploy a .NET 8 web API to App Service for production with SQL database integration
```
**Expected Behavior:**
1. AI asks about:
   - .NET version confirmation?
   - App Service Plan SKU (Premium for production)?
   - Auto-scaling needs?
   - Private endpoints required?
   - VNet integration for SQL access?
   - Key Vault for connection strings?
   - Application Insights monitoring?
2. User provides answers
3. AI summarizes Premium v3 configuration with managed identity, private endpoints, VNet integration
4. **AI generates Bicep template ONLY** for App Service
5. **NO actual resources are created**

**Validation:**
- ✅ Questions specific to App Service (SKU, Always On, VNet integration, HTTPS)
- ✅ **CRITICAL:** Only InfrastructureAgent invoked (template generation only)
- ✅ Template uses BicepAppServiceModuleGenerator
- ✅ Includes managed identity, private endpoints, Application Insights
- ✅ Proper SKU selection based on environment
- ✅ Response is Bicep code, not "provisioning started"

**Test 1.3: Container Apps with DAPR (Template Generation)**
```
I need to deploy a microservice with DAPR to Container Apps for production
```
**Expected Behavior:**
1. AI asks about:
   - Container image location (ACR recommended)?
   - Resource limits (CPU cores, memory)?
   - Scaling strategy (min/max replicas, zero-scale)?
   - DAPR components needed (state store, pub/sub)?
   - Internal or external ingress?
   - Managed identity for Azure services?
2. User: "ACR image, need DAPR with Service Bus pub/sub and Cosmos state store, internal only"
3. AI summarizes Container Apps Environment with DAPR components, managed identity, internal ingress
4. **AI generates Bicep template ONLY** for Container Apps
5. **NO actual resources are created**

**Validation:**
- ✅ Questions specific to Container Apps (DAPR, scaling, ingress, containers)
- ✅ **CRITICAL:** Only InfrastructureAgent invoked (template generation only)
- ✅ Template uses BicepContainerAppsModuleGenerator
- ✅ Includes DAPR configuration, managed environment, proper scaling
- ✅ Internal ingress for production
- ✅ Response is Bicep code, not "resources provisioned"

**Test 1.4: Multi-Turn Conversation - Refinement (Template Generation)**
```
Turn 1: "I need an AKS cluster"
Turn 2: (AI asks questions)
Turn 3: "dev environment"
Turn 4: (AI asks more questions)
Turn 5: "Actually, also needs to be FedRAMP High compliant"
Turn 6: (AI adjusts recommendations)
Turn 7: "Yes, proceed with that configuration"
```
**Expected Behavior:**
- AI maintains conversation context across all turns
- Requirements are refined through natural conversation
- Final template incorporates all gathered requirements
- **No template generated until user confirms**
- **Only InfrastructureAgent invoked** (template generation, not provisioning)

**Validation:**
- ✅ Context maintained across multiple turns
- ✅ AI doesn't forget earlier answers
- ✅ Template reflects ALL requirements from conversation
- ✅ **CRITICAL:** Only 1 agent invoked (Infrastructure), not 5 agents

**Test 1.5: Minimal Information - Maximum Questions (Template Generation)**
```
Deploy something to Azure
```
**Expected Behavior:**
1. AI asks clarifying questions:
   - What type of resource? (AKS, App Service, Container Apps, Storage, etc.)
   - What's the purpose/workload?
   - Which region?
   - What environment (dev/staging/prod)?
2. User provides info through conversation
3. AI continues gathering requirements specific to resource type
4. Only generates after complete picture

**Validation:**
- ✅ AI handles vague requests gracefully
- ✅ Asks clarifying questions first
- ✅ Doesn't make assumptions without confirmation

---

#### � Actual Provisioning Tests (Real Resource Deployment)

> **⚠️ WARNING:** These tests create REAL Azure resources and incur REAL costs. Only use in test subscriptions.

**Test 1.6: Actual AKS Provisioning (Full Workflow)**
```
Turn 1: "I need to deploy a new Kubernetes cluster in Azure for development"
Turn 2: (AI asks questions about environment, security, etc.)
Turn 3: "dev environment, basic security is fine, 3 nodes, Standard_D2s_v3"
Turn 4: (AI summarizes configuration)
Turn 5: "yes proceed"
Turn 6: (AI shows Bicep template)
Turn 7: "Actually provision this template and create the resources now in subscription 00000000-0000-0000-0000-000000000000"
```
**Expected Behavior:**
1. **Infrastructure Agent**: Generates Bicep template based on requirements
2. **Environment Agent**: Executes deployment, creates AKS cluster
3. **Discovery Agent**: Scans new resources, verifies creation **in the new resource group only**
4. **Compliance Agent**: Performs security scan **on the new resource group only** (not entire subscription)
5. **Cost Management Agent**: Estimates monthly costs **for the newly created resources**

**Validation:**
- ✅ **CRITICAL:** All 5 agents invoked **in correct order** (Infrastructure → Environment → Discovery → Compliance → Cost)
- ✅ **CRITICAL:** Execution time 60-180 seconds (not 15-30 seconds)
- ✅ **CRITICAL:** Response includes "resources created successfully" or "provisioning started"
- ✅ **CRITICAL:** Actual Azure resources exist in subscription after completion
- ✅ **CRITICAL:** Compliance scan targets ONLY the new resource group, not entire subscription
- ✅ Environment Agent shows cluster creation status
- ✅ Discovery Agent lists newly created resources **in the deployed resource group**
- ✅ Compliance Agent shows compliance score **for the new resource group only**
- ✅ Cost Agent shows estimated monthly cost **for the new resources**

**✅ BUG FIXED (2025-10-22):**
This workflow was previously **NOT WORKING** due to `ExecutionPlanValidator.IsTemplateGenerationRequest()` incorrectly treating "Actually provision" as template generation.

**Fix Applied:**
1. Added `IsActualProvisioningRequest()` method to detect explicit provisioning keywords
2. Updated `IsTemplateGenerationRequest()` to check for provisioning intent FIRST
3. Added `HasAllProvisioningAgents()` to validate 5-agent plans
4. Created `CreateActualProvisioningPlan()` to generate correct 5-agent sequential workflow
5. Modified `ValidateAndCorrect()` to prioritize: Compliance Scanning → Actual Provisioning → Template Generation

**Files Modified:**
- `src/Platform.Engineering.Copilot.Core/Services/Agents/ExecutionPlanValidator.cs`

**Testing:** Restart the API and retry the workflow to verify the fix.

**Test 1.7: Explicit Provisioning Request**
```
Actually provision a production AKS cluster in usgovvirginia with 5 nodes in subscription 00000000-0000-0000-0000-000000000000. Make it live right now.
```
**Expected Behavior:**
- User explicitly requests provisioning with "actually provision" and "make it live"
- Full workflow executes: Infrastructure → Environment → Compliance → Discovery → Cost
- Real resources created in Azure

**Validation:**
- ✅ System recognizes explicit provisioning intent
- ✅ All 5 agents invoked
- ✅ Real deployment occurs

---

#### 📝 Standard Infrastructure Tests (Template Generation Only)

**Test 1.8: Simple AKS Deployment (Conversational Template Generation)**
```
I need to deploy a new Kubernetes cluster in Azure. Can you help me set up an AKS cluster in the US GOV Virginia region with 3 nodes in subscription 00000000-0000-0000-0000-000000000000?
```
**Expected:** InfrastructureAgent asks questions, then generates Bicep template after gathering requirements (NO provisioning)

**Test 1.9: Multi-Resource Infrastructure (Conversational Template Generation)**
```
I'm setting up a new application that needs an Azure SQL database, a storage account, and a virtual network. Can you create the infrastructure code for this in the US GOV Arizona region?
```
**Expected:** InfrastructureAgent asks about environment, security, backup needs, then generates comprehensive Bicep template (NO provisioning)

**Test 1.10: Infrastructure with Specific Requirements (Template Generation)**
```
We need to provision a production-grade environment with high availability. I need an Application Gateway, AKS cluster with 5 nodes across 3 availability zones, Azure Database for PostgreSQL with geo-replication, and all networking configured with private endpoints.
```
**Expected:** InfrastructureAgent confirms requirements, asks clarifying questions, then generates enterprise-grade Bicep (NO provisioning)

**🔍 Validation Checklist (When Code is NOT Displayed in Response):**
- ✅ **Agent Invocation**: Check `metadata.agentsInvoked` contains ONLY `InfrastructureAgent`
- ✅ **File Generation**: Response should indicate "Generated files" or "Created X Bicep templates"
- ✅ **File Count**: Should list specific file paths (e.g., "modules/aks/main.bicep", "modules/network/main.bicep")
- ✅ **Resource Coverage**: Confirms ALL requested resources (App Gateway, AKS, PostgreSQL, VNet, Private Endpoints)
- ✅ **Network Topology**: Shows subnet layout with CIDRs and service endpoints
- ✅ **Configuration Summary**: Lists key settings (node count, availability zones, geo-replication)
- ✅ **Next Steps**: Provides deployment commands (e.g., `az deployment group create`)
- ✅ **Time Check**: Processing time should be 15-30 seconds (template generation, NOT 60-180s provisioning)
- ❌ **NO Actual Deployment**: Response should NOT say "resources created" or "provisioning started"
- ❌ **NO Multi-Agent**: Should NOT invoke Environment, Compliance, Discovery, or Cost agents

**� WHERE IS THE GENERATED CODE?**

When the chat response says "Generated files" but doesn't show the code, the Bicep templates are stored in the system's job artifacts. Here's how to access them:

**Option 1: Request Code Display in Chat**
```
Can you show me the generated Bicep code for the AKS module?
```
or
```
Display the main.bicep file content
```
or
```
Show me all the generated files
```

**💡 NEW: Module-Specific Code Retrieval**
The system now supports filtering by module type! When you request "Show me the AKS module", the AI uses the new `get_module_files` function to return only AKS-related files (not all files or wrong module files).

**Supported Module Types:**
- `aks` - Kubernetes cluster files
- `database` - SQL/PostgreSQL database files
- `network` - VNet, subnet, NSG files
- `storage` - Storage account files
- `appservice` - App Service Plan files
- `containerapps` - Container Apps files

**Examples:**
```
Show me the database module code
Display the network infrastructure files
Can I see the AKS cluster Bicep templates?
```

**🐛 Bug Fixed (2025-10-21):** Previously, requesting "Show me the AKS module" would return SQL database code instead. This has been fixed with a new kernel function that properly filters files by module type.

**Option 2: Download via API (for programmatic access)**
```bash
# Get the job ID from the response metadata
curl -X GET http://localhost:5100/api/jobs/{jobId}/artifacts
```

**Option 3: Check Job Output in Admin UI**
1. Navigate to the Admin Client (usually http://localhost:5000)
2. Go to Jobs → Recent Jobs
3. Find the job by timestamp or conversation ID
4. View/download the generated artifacts

**Option 4: Direct File System (if running locally)**
- Templates are stored in: `{workspace}/generated-templates/{job-id}/`
- Look for directories matching the resource types (aks, network, database)

**� Pro Tip:** The response lists file paths like "modules/aks/main.bicep". These are relative paths within the generated job artifacts, NOT your workspace. To see the actual code, you must request it in chat or download the artifacts.

**�🚨 Common Confusion:** 
- ❌ "Generated files" does NOT mean files are in your VS Code workspace
- ❌ The file paths shown are NOT clickable links to local files
- ✅ Files are stored in job artifacts and must be explicitly requested or downloaded
- ✅ Response showing "Generated files" means templates were created in the system, NOT that Azure resources were provisioned
- ✅ To actually deploy, you must explicitly say "actually provision this" or "deploy the template now"

**Test 1.11: Modify Existing Infrastructure**
```
I have an existing AKS cluster that needs to be scaled up from 3 nodes to 10 nodes. Can you show me how to modify the infrastructure code?
```
**Expected:** InfrastructureAgent provides scaling modifications

**Test 1.12: Infrastructure Best Practices (Informational)**
```
What are the best practices for deploying a secure AKS cluster in Azure Government cloud?
```
**Expected:** InfrastructureAgent provides guidance and may offer to generate template with best practices (informational query, not provisioning)

---

#### 🌐 Network Topology Design Tests

**Test 1.13: Network Topology Design - Simple 2-Tier**
```
I need to design a network topology for a simple web application with a database. Can you help me design a VNet with the right subnets?
```
**Expected:** InfrastructureAgent designs 2-tier topology (Web + Data) with automatic subnet calculations

**Test 1.12: Network Topology Design - Classic 3-Tier**
```
Design a 3-tier network architecture with 10.0.0.0/16 address space. Include Azure Bastion for secure access.
```
**Expected:** InfrastructureAgent designs 3-tier topology (Web + Application + Data) with Bastion subnet

**Test 1.13: Network Topology Design - Complex Multi-Tier**
```
I'm building a complex enterprise application that needs 5 separate network tiers. Can you design a network topology with address space 172.16.0.0/12?
```
**Expected:** InfrastructureAgent designs 5-tier topology (DMZ + Web + Application + Business + Data)

**Test 1.14: Network Topology Design - With Security**
```
Create a network design with 10.0.0.0/16 that includes Azure Firewall, VPN Gateway, and Bastion. I need 4 application tiers.
```
**Expected:** InfrastructureAgent designs 4-tier topology with all special subnets (Gateway, Bastion, Firewall)

**Test 1.15: Network Topology Design - Minimal Setup**
```
I just need a simple single-tier network for testing. Can you design a VNet with 192.168.0.0/24?
```
**Expected:** InfrastructureAgent designs 1-tier topology (Application only) with minimal configuration

**Test 1.16: Subnet Calculator**
```
How many /27 subnets can I fit in 10.0.0.0/20? Show me the CIDR ranges.
```
**Expected:** InfrastructureAgent calculates subnet CIDRs with IP counts

**Test 1.17: Network Topology with Template Generation**
```
Design a 3-tier network topology with 10.0.0.0/16, then generate the Bicep code for it.
```
**Expected:** InfrastructureAgent designs topology and generates Bicep template

---

#### 🔮 Predictive Scaling Tests

**Test 1.18: Predictive Scaling - Near Future**
```
I need to design a network topology for a simple web application with a database. Can you help me design a VNet with the right subnets?
```
**Expected:** InfrastructureAgent designs 2-tier topology (Web + Data) with automatic subnet calculations

**Test 1.7: Network Topology Design - Classic 3-Tier**
```
Design a 3-tier network architecture with 10.0.0.0/16 address space. Include Azure Bastion for secure access.
```
**Expected:** InfrastructureAgent designs 3-tier topology (Web + Application + Data) with Bastion subnet

**Test 1.8: Network Topology Design - Complex Multi-Tier**
```
I'm building a complex enterprise application that needs 5 separate network tiers. Can you design a network topology with address space 172.16.0.0/12?
```
**Expected:** InfrastructureAgent designs 5-tier topology (DMZ + Web + Application + Business + Data)

**Test 1.9: Network Topology Design - With Security**
```
Create a network design with 10.0.0.0/16 that includes Azure Firewall, VPN Gateway, and Bastion. I need 4 application tiers.
```
**Expected:** InfrastructureAgent designs 4-tier topology with all special subnets (Gateway, Bastion, Firewall)

**Test 1.10: Network Topology Design - Minimal Setup**
```
I just need a simple single-tier network for testing. Can you design a VNet with 192.168.0.0/24?
```
**Expected:** InfrastructureAgent designs 1-tier topology (Application only) with minimal configuration

**Test 1.11: Subnet Calculator**
```
How many /27 subnets can I fit in 10.0.0.0/20? Show me the CIDR ranges.
```
**Expected:** InfrastructureAgent calculates subnet CIDRs with IP counts

**Test 1.12: Network Topology with Template Generation**
```
Design a 3-tier network topology with 10.0.0.0/16, then generate the Bicep code for it.
```
**Expected:** InfrastructureAgent designs topology and generates Bicep template

#### 🔮 Predictive Scaling Tests

**Test 1.18: Predictive Scaling - Near Future**
```
Will my AKS cluster need to scale up in the next 24 hours? Can you predict the load?
```
**Expected:** InfrastructureAgent predicts scaling needs with confidence score and recommendations

**Test 1.19: Predictive Scaling - Weekly Forecast**
```
I need to forecast my VMSS scaling requirements for next week. What should I expect?
```
**Expected:** InfrastructureAgent predicts weekly scaling trends with metric predictions

**Test 1.20: Optimize Auto-Scaling Configuration**
```
My App Service Plan in subscription 00000000-0000-0000-0000-000000000000 keeps scaling up and down too frequently. Can you optimize the auto-scaling configuration?
```
**Expected:** InfrastructureAgent analyzes and provides optimized scaling configuration

**⚠️ Note:** For actual optimization, you need to provide the full App Service Plan resource ID or name:
```
My App Service Plan "prod-app-service-plan" in resource group "prod-rg" and subscription 00000000-0000-0000-0000-000000000000 keeps scaling up too frequently. Can you optimize it?
```
If you don't provide specific resource details, the AI will provide **general scaling optimization guidance** rather than analyzing a specific resource.

**Test 1.21: Analyze Scaling Performance**
```
How has my AKS cluster's auto-scaling been performing over the last 7 days? Is it efficient?
```
**Expected:** InfrastructureAgent analyzes historical scaling events and provides efficiency metrics

**Test 1.22: Scaling Cost Analysis**
```
Review my VMSS scaling performance for the last 30 days and tell me if I'm over-provisioning or under-provisioning.
```
**Expected:** InfrastructureAgent analyzes scaling with cost impact and provisioning balance

**Test 1.23: Predictive Scaling with Optimization**
```
Predict my scaling needs for tomorrow, then optimize my auto-scaling rules to handle that load efficiently.
```
**Expected:** InfrastructureAgent predicts needs and provides optimized configuration

---

#### 🛡️ Compliance-Aware Template Generation Tests

**Test 1.24: Compliance - FedRAMP High AKS**
```
I need to deploy an AKS cluster that meets FedRAMP High compliance requirements. Can you generate the Bicep template with all necessary controls?
```
**Expected:** InfrastructureAgent asks about environment/requirements, then generates compliance-enhanced template with FedRAMP High controls (AC, AU, SC, IA families)

**Test 1.25: Compliance - DoD IL5 Infrastructure**
```
Generate a DoD IL5 compliant infrastructure template for a web application with PostgreSQL database in Azure Government.
```
**Expected:** InfrastructureAgent asks clarifying questions, then generates DoD IL5 template with enhanced security controls including physical security (PE) controls

**Test 1.26: Compliance - NIST 800-53 Storage**
```
Create a NIST 800-53 compliant Azure Storage account with all required encryption and audit controls.
```
**Expected:** InfrastructureAgent asks about usage/access patterns, then generates **Storage Account** template (NOT AKS) with encryption at rest (SC-28), audit logging (AU-2/AU-3), and access controls (AC-3/AC-6)

**🐛 KNOWN ISSUE (FIXED 2025-10-21):** 
Previously, this prompt incorrectly generated AKS templates instead of Storage Account templates. This was caused by the `generate_compliant_infrastructure_template` function not properly setting the `ComputePlatform` based on the resource type. The fix:
- Now infers resource type from the description (e.g., "storage account" → Storage platform)
- Maps resource type to correct `ComputePlatform` enum value
- Uses `BuildTemplateGenerationRequest()` to create properly configured request

**Files Generated (after fix):**
- `infra/modules/storage/main.bicep` - Main orchestration file
- `infra/modules/storage/storage-account.bicep` - Storage account resource with NIST controls
- `infra/modules/storage/private-endpoint.bicep` - Private endpoint for secure access (SC-7, AC-3)
- `infra/modules/storage/diagnostics.bicep` - Diagnostic logging (AU-2, AU-3)
- `infra/modules/storage/README.md` - Documentation

**Test 1.27: Compliance - SOC2 App Service**
```
I need a SOC2 compliant App Service Plan with all Trust Services Criteria implemented.
```
**Expected:** InfrastructureAgent asks about app details, then generates template with SOC2 controls (CC6.1, CC6.6, CC6.7, CC7.2)

**Test 1.28: Compliance - GDPR Data Store**
```
Deploy a GDPR-compliant Cosmos DB with proper data protection controls for EU data residency.
```
**Expected:** InfrastructureAgent asks about data types/regions, then generates template with GDPR Articles (Art-32, Art-33, Art-34) and EU region

**Test 1.29: Compliance Validation**
```
Generate a production-ready AKS cluster for government use with full compliance validation.
```
**Expected:** InfrastructureAgent asks which compliance framework, then generates compliant template, validates controls, and shows compliance summary with findings

**Test 1.30: Multi-Framework Compliance**
```
I need to deploy an AKS cluster that meets FedRAMP High compliance requirements. Can you generate the Bicep template with all necessary controls?
```
**Expected:** InfrastructureAgent generates compliance-enhanced template with FedRAMP High controls (AC, AU, SC, IA families)

**Test 1.20: Compliance - DoD IL5 Infrastructure**
```
Generate a DoD IL5 compliant infrastructure template for a web application with PostgreSQL database in Azure Government.
```
**Expected:** InfrastructureAgent generates DoD IL5 template with enhanced security controls including physical security (PE) controls

**Test 1.21: Compliance - NIST 800-53 Storage**
```
Create a NIST 800-53 compliant Azure Storage account with all required encryption and audit controls.
```
**Expected:** InfrastructureAgent generates template with encryption at rest (SC-28), audit logging (AU-2/AU-3), and access controls (AC-3/AC-6)

**Test 1.22: Compliance - SOC2 App Service**
```
I need a SOC2 compliant App Service Plan with all Trust Services Criteria implemented.
```
**Expected:** InfrastructureAgent generates template with SOC2 controls (CC6.1, CC6.6, CC6.7, CC7.2)

**Test 1.23: Compliance - GDPR Data Store**
```
Deploy a GDPR-compliant Cosmos DB with proper data protection controls for EU data residency.
```
**Expected:** InfrastructureAgent generates template with GDPR Articles (Art-32, Art-33, Art-34) and EU region

**Test 1.24: Compliance Validation**
```
Generate a production-ready AKS cluster for government use with full compliance validation.
```
**Expected:** InfrastructureAgent generates compliant template, validates controls, and shows compliance summary with findings

**Test 1.25: Multi-Framework Compliance**
```
I need infrastructure that meets both FedRAMP High and SOC2 requirements. Can you help?
```
**Expected:** InfrastructureAgent generates template with FedRAMP High controls (superset of SOC2) and explains coverage

---

### 🛡️ ComplianceAgent (RMF/NIST/Security)

> **🤖 Conversational Requirements Gathering**  
> ComplianceAgent asks about subscription, scope, framework, and control families before running assessments.

**Test 2.1: Basic Compliance Check (Conversational)**
```
I need to check if my Azure subscription is compliant with NIST 800-53 controls. Can you run a compliance assessment?
```
**Expected Behavior:**
1. Agent asks: ""Which subscription? What scope (entire subscription or specific RG)? Any specific control families?""
2. User: ""subscription 453c..., entire subscription, focus on AC and AU families""
3. **Agent IMMEDIATELY calls run_compliance_assessment** (no confirmation)

**Validation:**
- ✅ Agent asks for missing info (subscription, scope)
- ✅ Only ONE question cycle
- ✅ After user answers, function is called immediately
- ✅ Response shows compliance scan results

**Test 2.2: Gap Analysis (Conversational)**
```
We're trying to achieve FedRAMP High compliance. Can you analyze our gaps?
```
**Expected Behavior:**
1. Agent asks: ""Which subscription? Do you have existing controls? What's your priority (critical gaps, quick wins, all)?""
2. User: ""subscription 453c..., some controls in place, focus on critical gaps""
3. **Agent calls gap analysis function immediately**

**Validation:**
- ✅ Agent asks targeted questions about target compliance and current state
- ✅ Only one question cycle
- ✅ Response includes gap analysis with critical/high severity items prioritized

**Test 2.3: ATO Package Generation (Conversational)**
```
I need to create a complete RMF package for my application
```
**Expected Behavior:**
1. Agent asks: ""What type of ATO? Who is the issuing authority? Tell me about your system (name, type, impact level, boundary)""
2. User provides system details
3. **Agent generates RMF documentation immediately**

**Validation:**
- ✅ Agent collects all required ATO metadata before proceeding
- ✅ Progressive questioning (doesn't overwhelm user with all questions at once)
- ✅ Response includes SSP and other required documents

---

### 💰 CostManagementAgent (Costs/Budgets/Optimization)

> **🤖 Conversational Requirements Gathering**  
> CostManagementAgent asks about scope, time period, breakdown preferences, and optimization focus.

**Test 3.1: Cost Overview (Conversational)**
```
What did I spend on Azure?
```
**Expected Behavior:**
1. Agent asks: ""Which subscription? What time period (last month, last 90 days, year-to-date)? How should I break down costs (by service, resource group, or location)?""
2. User: ""subscription 453c..., last month, by service""
3. **Agent calls cost analysis function immediately**

**Validation:**
- ✅ Agent asks for all missing critical details in ONE message
- ✅ After user answers, NO confirmation request - just executes
- ✅ Response shows cost breakdown as requested

**Test 3.2: Cost Optimization (Conversational)**
```
My Azure bill seems high. Can you find ways to reduce costs?
```
**Expected Behavior:**
1. Agent asks: ""Which subscription? What focus area (compute, storage, networking, databases, all)? Any constraints (must maintain performance, can tolerate downtime)? Savings target?""
2. User: ""subscription 453c..., focus on compute, maintain performance, target 20% reduction""
3. **Agent calls optimization function immediately**

**Validation:**
- ✅ Agent asks about optimization scope and constraints
- ✅ Smart defaults used (if user doesn't specify target, agent finds all opportunities)
- ✅ Response includes actionable recommendations with estimated savings

**Test 3.3: Budget Setup (Conversational)**
```
I need to set up a budget for my development environment
```
**Expected Behavior:**
1. Agent asks: ""What's your monthly budget amount? When should I alert you (50%, 75%, 90%, 100%)? What should this budget cover (entire subscription, specific RGs, tagged resources)? What actions when exceeded?""
2. User: ""$10,000/month, alert at 75% and 100%, cover dev RG only, email notifications""
3. **Agent configures budget immediately**

**Validation:**
- ✅ Agent collects budget parameters systematically
- ✅ Provides sensible defaults for alert thresholds if not specified
- ✅ Response confirms budget setup with alert configuration

---

### 🔍 DiscoveryAgent (Inventory/Resources)

> **🤖 Conversational Requirements Gathering**  
> DiscoveryAgent asks about resource types, search criteria, and output format preferences.

**Test 5.1: Complete Resource Discovery (Conversational)**
```
What resources do I have running?
```
**Expected Behavior:**
1. Agent asks: ""Which subscription? What should I discover (all resources, specific types, specific RG, tagged resources)? How would you like results (summary, detailed list, inventory report)?""
2. User: ""subscription 453c..., all resources, summary with count by type""
3. **Agent calls discovery function immediately**

**Validation:**
- ✅ Agent asks about scope and output format
- ✅ After user answers, executes immediately
- ✅ Response includes resource inventory as requested

**Test 5.2: Resource Search (Conversational)**
```
I'm looking for a storage account
```
**Expected Behavior:**
1. Agent asks: ""What search criteria (name pattern, tags, location)? Where should I search (specific subscription, all subscriptions, specific RGs)?""
2. User: ""name contains 'production', search in subscription 453c...""
3. **Agent searches immediately**

**Validation:**
- ✅ Agent asks for search criteria and scope
- ✅ Uses pattern matching when user provides partial info
- ✅ Response includes matching resources

**Test 5.3: Tagging Analysis (Conversational)**
```
Show me resources that don't have proper tags
```
**Expected Behavior:**
1. Agent asks: ""Which tags are required in your organization (Environment, Owner, CostCenter, Application, or custom)? What should I analyze (all resources, specific types, specific RGs)?""
2. User: ""Environment and CostCenter required, check all resources in subscription 453c...""
3. **Agent analyzes tagging compliance immediately**

**Validation:**
- ✅ Agent asks which tags are mandatory
- ✅ Scans specified scope for missing tags
- ✅ Response lists non-compliant resources

---

### 🌍 EnvironmentAgent (Environments/Configurations)

> **🤖 Conversational Requirements Gathering**  
> EnvironmentAgent asks about environment type, location, configuration level, and validation depth.

**Test 4.1: Environment Creation (Conversational)**
```
Set up a production environment for my new web app
```
**Expected Behavior:**
1. Agent asks: ""What's the application name? Which Azure region (usgovvirginia, usgovarizona)? Which subscription? What configuration level (basic, standard, enterprise)?""
2. User: ""webapp-api, usgovvirginia, subscription 453c..., enterprise""
3. **Agent calls create_environment immediately**

**Validation:**
- ✅ Agent asks for all environment creation parameters
- ✅ After user answers, NO confirmation - just creates
- ✅ Response shows environment creation status

**Test 4.2: Environment Validation (Conversational)**
```
Can you check if my production environment is configured correctly?
```
**Expected Behavior:**
1. Agent asks: ""Which environment? Validation level (basic, standard, comprehensive)?""
2. User: ""prod-webapp environment, comprehensive validation""
3. **Agent validates immediately**

**Validation:**
- ✅ Agent asks about validation depth
- ✅ Checks SharedMemory for recent deployments first
- ✅ Response includes detailed validation results with pass/fail for each check

**Test 4.3: Environment Comparison (Conversational)**
```
Is my staging environment configured the same as production?
```
**Expected Behavior:**
1. Agent asks: ""Which two environments should I compare? What aspects (resource config, network, security, scaling, all)?""
2. User: ""staging vs production, compare all aspects""
3. **Agent compares immediately**

**Validation:**
- ✅ Agent clarifies which environments to compare
- ✅ Response highlights configuration differences
- ✅ Provides recommendations for alignment

---

### 🚀 Service Wizard (Mission/Team Onboarding)

> **⚠️ Note:** ServiceCreation agent is defined in AgentType enum but not yet implemented. Service onboarding functionality is currently provided via **ServiceWizardPlugin** (see PHASE1.md for implementation details).
>
> **Current Implementation:** ServiceWizardPlugin provides an 8-step interactive wizard for mission/team onboarding with progressive questioning.

**Test 6.1: Service Wizard Availability (Plugin-Based)**
```
How do I onboard a new mission?
```
**Expected Behavior:**
- System should recognize service onboarding query
- May route to ServiceWizardPlugin or provide guidance
- Should explain available service creation workflows

**Validation:**
- ✅ Query recognized and handled appropriately
- ✅ User receives guidance on service onboarding process
- ✅ No errors about missing ServiceCreationAgent

**Test 6.2: Future ServiceCreation Agent (Planned)**
```
Note: Full ServiceCreationAgent implementation is planned for future release.
Current functionality via ServiceWizardPlugin includes:
- 8-step interactive wizard
- DoD compliance validation
- Mission metadata collection
- Automated approval workflows
```

**Related Documentation:**
- See [PHASE1.md](./PHASE1.md) for ServiceWizardPlugin implementation details
- See [AGENT-ORCHESTRATION.md](./AGENT-ORCHESTRATION.md) for agent architecture
1. Agent asks: ""What's the mission name? Who is the mission owner (name, email, org)? What's the data classification? What's the timeline?""
2. User provides basic info
3. Agent continues with technical questions
4. Progressive flow through all 4 phases

**Validation:**
- ✅ Agent handles minimal initial information gracefully
- ✅ Asks clarifying questions systematically
- ✅ Doesn't make assumptions without confirmation

---

## 🔄 Multi-Agent Workflows with Conversational Gathering

## 🔄 Multi-Agent Workflows with Conversational Gathering

> **🆕 Conversational Flows in Multi-Agent Scenarios**  
> When multiple agents are involved, each agent uses conversational gathering for its specific domain:
> - First agent gathers its requirements → executes → stores results in SharedMemory
> - Second agent checks SharedMemory for context → asks ONLY for additional info it needs
> - This avoids redundant questions (e.g., asking for subscription ID twice)

### Sequential Execution (Task Dependencies)

**Test 7.1: Deploy and Assess (Conversational)**
```
Turn 1: "I need to deploy a new AKS cluster and check if it's compliant"
Turn 2: (InfrastructureAgent asks about environment, security, monitoring)
Turn 3: "dev, zero trust, monitoring, usgovvirginia, subscription 453c..."
Turn 4: (InfrastructureAgent generates template)
Turn 5: (ComplianceAgent asks: "Should I scan the newly created resource group?")
Turn 6: "yes"
```
**Expected:** 
- InfrastructureAgent asks questions → generates template
- ComplianceAgent checks SharedMemory for resource group → confirms with user → scans
- NO redundant subscription ID questions

**Validation:**
- ✅ InfrastructureAgent → ComplianceAgent (sequential)
- ✅ ComplianceAgent uses resource group from SharedMemory
- ✅ No duplicate questions

**Test 7.2: Discover and Optimize (Conversational)**
```
Turn 1: "Find all my VMs and tell me how to reduce their costs"
Turn 2: (DiscoveryAgent asks: "Which subscription? What output format?")
Turn 3: "subscription 453c..., detailed list"
Turn 4: (DiscoveryAgent lists VMs)
Turn 5: (CostManagementAgent asks: "What focus area? Any constraints?")
Turn 6: "focus on compute, maintain performance"
```
**Expected:**
- DiscoveryAgent gathers scope → discovers VMs → stores in SharedMemory
- CostManagementAgent uses VM list from SharedMemory → asks ONLY about optimization preferences
- Each agent asks domain-specific questions

**Validation:**
- ✅ DiscoveryAgent → CostManagementAgent (sequential)
- ✅ CostManagementAgent doesn't re-ask for subscription (gets from SharedMemory)
- ✅ Optimization recommendations based on discovered VMs

---

### Parallel Execution (Independent Tasks with Conversational Gathering)

**Test 8.1: Comprehensive Health Report (Conversational)**
```
Turn 1: "I need a complete health report for my subscription"
Turn 2: (All 3 agents ask simultaneously)
  - DiscoveryAgent: "Which subscription? Summary or detailed?"
  - CostManagementAgent: "Time period? Breakdown preference?"
  - ComplianceAgent: "Compliance framework? Control families?"
Turn 3: User answers ALL agents: "subscription 453c..., summary, last month by service, NIST 800-53 all families"
```
**Expected:**
- All 3 agents ask their questions in parallel
- User provides consolidated answer
- All agents execute simultaneously

**Validation:**
- ✅ DiscoveryAgent + CostManagementAgent + ComplianceAgent (parallel)
- ✅ Each agent asks domain-specific questions
- ✅ Combined results in single response

---

### Collaborative Execution (Complex Workflows with Conversational Gathering)

**Test 9.1: Complete Infrastructure Deployment (Conversational - Multi-Turn)**
```
Turn 1: "I need to deploy a new AKS cluster for a production workload"
Turn 2: (InfrastructureAgent asks: environment type, location, security requirements)
Turn 3: "Production, usgovvirginia, FedRAMP High, Zero Trust, subscription 453c..."
Turn 4: (InfrastructureAgent asks: monitoring, scaling, integrations)
Turn 5: "Yes monitoring, autoscaling 3-10 nodes, integrate with Key Vault"
Turn 6: (InfrastructureAgent generates FedRAMP-compliant template)
Turn 7: (EnvironmentAgent asks: "Should I deploy this to usgovvirginia?")
Turn 8: "yes, proceed"
Turn 9: (EnvironmentAgent deploys → ComplianceAgent confirms: "Scan new resources?")
Turn 10: "yes"
Turn 11: (ComplianceAgent scans → CostManagementAgent confirms: "Estimate costs?")
Turn 12: "yes"
```
**Expected:**
- InfrastructureAgent: Asks environment/security questions → generates template
- EnvironmentAgent: Uses template from SharedMemory, confirms deploy → deploys
- ComplianceAgent: Uses resource group from SharedMemory, confirms scan → scans
- CostManagementAgent: Uses resources from SharedMemory, confirms estimate → estimates

**Validation:**
- ✅ All 4 agents invoked in sequence (Infrastructure → Environment → Compliance → Cost)
- ✅ Each agent checks SharedMemory before asking questions
- ✅ No redundant questions (subscription, location asked once)
- ✅ User only confirms intent, not re-provides data
- ✅ Full deployment with compliance and cost tracking

---

## 🎯 Conversational Edge Cases

### Handling Incomplete Information

**Test 10.1: Very Vague Request**
```
"Deploy something to Azure"
```
**Expected:**
- InfrastructureAgent asks: "What type of resource? (AKS, App Service, Storage, VMs, etc.)"
- User: "Kubernetes"
- Agent asks: "Which region? Environment type? Node count?"
- Progressive questioning until enough info collected

**Validation:**
- ✅ Agent handles minimal info gracefully
- ✅ Asks clarifying questions systematically
- ✅ Doesn't proceed without critical details

**Test 10.2: Partial Information**
```
"I need an AKS cluster"
```
**Expected:**
- Agent says: "Great! I need a few more details: region, environment (dev/staging/prod), node count?"
- Uses defaults for everything else

**Validation:**
- ✅ Agent recognizes resource type (AKS)
- ✅ Asks ONLY for essential missing info
- ✅ Uses smart defaults for security, monitoring, etc.

---

### Context Carryover (Multi-Turn Conversations with Conversational Gathering)

**Test 12.1: Reference Previous Context (Conversational)**
```
Turn 1: "Show me all my AKS clusters"
Turn 2: (DiscoveryAgent asks: "Which subscription?")
Turn 3: "subscription 453c..."
Turn 4: (DiscoveryAgent lists clusters)
Turn 5: "What are they costing me?"
Turn 6: (CostManagementAgent doesn't re-ask for subscription - uses from Turn 3)
Turn 7: "Check if they're compliant"
Turn 8: (ComplianceAgent doesn't re-ask for subscription - uses from Turn 3)
```
**Expected:**
- DiscoveryAgent asks for subscription once
- CostManagementAgent uses subscription from conversation history
- ComplianceAgent uses subscription from conversation history
- NO redundant questions

**Validation:**
- ✅ Context maintained across all turns
- ✅ Subscription asked ONCE, used by all agents
- ✅ Each agent focuses on its domain-specific questions only

---

## 🧪 Validation Checklist for Conversational Flows

**Test 7.2: Discover and Optimize**
```
First, find all the virtual machines I have running, then analyze which ones are costing the most and recommend optimizations.
```
**Expected:** DiscoveryAgent → CostManagementAgent (sequential)

**Test 7.3: Provision and Configure**
```
Create a new production environment with an AKS cluster, then configure it with the proper networking, monitoring, and security settings.
```
**Expected:** InfrastructureAgent → EnvironmentAgent (sequential)

**Test 7.4: Network Design and Deployment**
```
Design a 3-tier network topology with 10.0.0.0/16 address space, include Azure Firewall and Bastion, then generate the Bicep template and show me the deployment cost estimate.
```
**Expected:** InfrastructureAgent (design topology) → InfrastructureAgent (generate template) → CostManagementAgent (cost estimate)

**Test 7.5: Predictive Scaling and Cost Impact**
```
Predict my AKS cluster scaling needs for the next 48 hours, then estimate the cost impact if I follow those recommendations.
```
**Expected:** InfrastructureAgent (predict scaling) → CostManagementAgent (cost estimate)

**Test 7.6: Compliance-Enhanced Deployment with Validation**
```
Generate a FedRAMP High compliant AKS infrastructure template, then validate it against NIST 800-53 controls and show me the compliance gaps.
```
**Expected:** InfrastructureAgent (generate compliant template with validation) → ComplianceAgent (detailed control assessment)

---

### Parallel Execution (Independent Tasks)

**Test 8.1: Comprehensive Health Report**
```
I need a complete health report for my subscription. Show me all my resources, what I'm spending, and my compliance status.
```
**Expected:** DiscoveryAgent + CostManagementAgent + ComplianceAgent (parallel)

**Test 8.2: Multi-Aspect Analysis**
```
Give me an overview of my production environment including what's deployed, how much it costs, and whether it's compliant with our security policies.
```
**Expected:** DiscoveryAgent + CostManagementAgent + ComplianceAgent (parallel)

**Test 8.3: Resource and Cost Review**
```
I need to know what resources I have and how much they're costing me. Break down the costs by resource type.
```
**Expected:** DiscoveryAgent + CostManagementAgent (parallel)

---

### Collaborative Execution (Complex Workflows)

**Test 9.1: Complete Infrastructure Deployment with Compliance**
```
I need a production AKS cluster in USGov Virginia with SQL database and Key Vault, configure for FedRAMP High, check compliance, and set up cost tracking with a $50k/month budget.
```
**Expected:** InfrastructureAgent → EnvironmentAgent → ComplianceAgent → CostManagementAgent

**Validation:**
- ✅ Infrastructure generates FedRAMP-compliant templates
- ✅ Environment deploys to usgovvirginia
- ✅ Compliance scans for NIST 800-53 controls
- ✅ Cost tracking configured with budget alert

**Test 9.2: Environment Migration**
```
I need to migrate my application from our dev environment to production. First discover what's in dev, create matching production infrastructure, configure it, check compliance, and monitor the costs.
```
**Expected:** DiscoveryAgent → InfrastructureAgent → EnvironmentAgent → ComplianceAgent → CostManagementAgent

**Test 9.3: Audit and Remediation**
```
We're preparing for an audit. Please inventory all our resources, check them for compliance issues, identify cost optimization opportunities, and provide recommendations for remediating any problems found.
```
**Expected:** DiscoveryAgent → ComplianceAgent → CostManagementAgent (collaborative analysis)

**Test 9.4: Enterprise Network Design and Deployment**
```
I'm building a new secure application platform. First, design a 5-tier network architecture with 10.0.0.0/16 including all security features (Firewall, Bastion, Gateway). Then generate the Bicep infrastructure code, estimate the monthly costs, and check if the design meets NIST 800-53 networking controls.
```
**Expected:** InfrastructureAgent (network design) → InfrastructureAgent (template generation) → CostManagementAgent (cost estimate) → ComplianceAgent (security validation)

**Test 9.5: Intelligent Scaling Optimization Workflow**
```
I have an AKS cluster that's been running for a month. Analyze its scaling performance, predict what it needs for the next week, optimize the auto-scaling configuration based on those predictions, and estimate the cost savings if I apply the optimized settings.
```
**Expected:** InfrastructureAgent (analyze performance) → InfrastructureAgent (predict needs) → InfrastructureAgent (optimize config) → CostManagementAgent (cost savings estimate)

**Test 9.6: Full Compliance Deployment Workflow**
```
I'm deploying a new mission-critical application for a federal agency. Generate FedRAMP High compliant infrastructure (AKS, SQL, Storage), validate all NIST 800-53 controls are implemented, estimate monthly costs, and create the compliance documentation package for ATO submission.
```
**Expected:** InfrastructureAgent (generate compliant templates) → ComplianceAgent (validate controls & generate docs) → CostManagementAgent (cost estimate) → ComplianceAgent (ATO package generation)

---

## 🎯 Edge Cases and Special Scenarios

### Conversational (No Agent Required)

**Test 10.1: General Help**
```
What can you help me with? What are your capabilities?
```
**Expected:** Conversational response, no agent invocation

**Test 10.2: Clarification Request**
```
I'm not sure what I need. Can you explain what services are available on this platform?
```
**Expected:** Conversational response with guidance

**Test 10.3: Greeting**
```
Hello! I'm new here. How does this system work?
```
**Expected:** Conversational welcome response

---

### Ambiguous Intent (Agent Clarification)

**Test 11.1: Unclear Scope**
```
I need to set something up in Azure.
```
**Expected:** OrchestratorAgent asks for clarification

**Test 11.2: Multiple Possible Agents**
```
Check my subscription.
```
**Expected:** OrchestratorAgent clarifies what to check (costs, compliance, resources?)

**Test 11.3: Vague Request**
```
Can you help me with compliance?
```
**Expected:** OrchestratorAgent asks what type of compliance help needed

---

### Context Carryover (Multi-Turn Conversations)

**Test 12.1: Reference Previous Context**
```
Turn 1: "Show me all my AKS clusters"
Turn 2: "What are they costing me?"
Turn 3: "Check if they're compliant"
```
**Expected:** Context maintained across all turns

**Test 12.2: Follow-up Questions**
```
Turn 1: "Deploy an AKS cluster in US GOV Virginia"
Turn 2: "How much will it cost?"
Turn 3: "Is it compliant with NIST 800-53?"
Turn 4: "Go ahead and deploy it"
```
**Expected:** Context about "it" maintained

**Test 12.3: Refinement**
```
Turn 1: "I need infrastructure for a web application"
Turn 2: "Actually, make it highly available across multiple regions"
Turn 3: "And it needs to be FedRAMP compliant"
```
**Expected:** Requirements refined across turns

---

### Error Handling and Recovery

**Test 13.1: Invalid Subscription**
```
Check compliance for subscription 00000000-0000-0000-0000-000000000000
```
**Expected:** Graceful error message

**Test 13.2: Unsupported Request**
```
Can you delete my production database right now?
```
**Expected:** Explanation that destructive operations require confirmation

**Test 13.3: Missing Information**
```
Deploy infrastructure.
```
**Expected:** Agent asks for required details (what type, where, etc.)

---

## 🆕 MCP-Enhanced Functions Test Cases

> **✨ NEW: Azure MCP Integration**  
> The platform has been enhanced with 19 MCP-powered functions across 7 plugins that combine existing platform services with Azure MCP tools for comprehensive guidance. These functions provide best practices, schemas, compliance validation, security scanning, cost optimization, and environment governance.

### � Resource Discovery with MCP (AzureResourceDiscoveryPlugin)

**Test MCP-1.1: Discover Resources with Schema Validation**
```
Discover all Azure resources in subscription 00000000-0000-0000-0000-000000000000 and validate them against Bicep schemas
```
**Expected:** AzureResourceDiscoveryPlugin uses MCP `bicepschema` tool to discover resources with schema validation
**Validation:**
- ✅ Lists resources with schema validation status
- ✅ Shows Bicep resource type for each resource
- ✅ Indicates schema validation results (valid/invalid/missing)
- ✅ Provides API version information

**Test MCP-1.2: Get Resource Details with Best Practices**
```
Show me details for AKS cluster "aks-prod-eastus-001" and include Azure best practices recommendations
```
**Expected:** AzureResourceDiscoveryPlugin combines resource details with MCP best practices guidance
**Validation:**
- ✅ Shows resource configuration details
- ✅ Includes Azure best practices for AKS
- ✅ Provides optimization recommendations
- ✅ Shows compliance considerations

**Test MCP-1.3: Search Azure Resource Documentation**
```
Search Azure documentation for AKS private cluster configuration and networking best practices
```
**Expected:** AzureResourceDiscoveryPlugin uses MCP to search Azure docs
**Validation:**
- ✅ Returns relevant documentation content
- ✅ Includes configuration examples
- ✅ Provides links to official docs
- ✅ Covers networking and security guidance

**Test MCP-1.4: Get Infrastructure Best Practices**
```
What are the Azure best practices for deploying AKS clusters with high availability?
```
**Expected:** AzureResourceDiscoveryPlugin provides comprehensive IaC best practices via MCP
**Validation:**
- ✅ Covers Bicep/Terraform/ARM templates
- ✅ Includes HA configuration patterns
- ✅ Shows zone redundancy recommendations
- ✅ Provides example code snippets

**Test MCP-1.5: Generate Bicep Template from Schema**
```
Generate a Bicep template for an Azure Storage Account based on the official schema with all required properties
```
**Expected:** AzureResourceDiscoveryPlugin uses MCP `bicepschema` to generate schema-compliant template
**Validation:**
- ✅ Template includes all required properties
- ✅ Uses latest API version
- ✅ Includes optional recommended properties
- ✅ Shows property descriptions and constraints

---

### 🏗️ Infrastructure with MCP (InfrastructurePlugin)

**Test MCP-2.1: Generate Infrastructure Template with Best Practices**
```
Generate an AKS cluster template for production with Azure best practices and security hardening built in
```
**Expected:** InfrastructurePlugin combines template generation with MCP best practices
**Validation:**
- ✅ Template includes security best practices
- ✅ Shows which best practices were applied
- ✅ Includes monitoring and logging
- ✅ Provides deployment guidance

**Test MCP-2.2: Deploy Infrastructure with Azure Developer CLI**
```
Deploy the generated template using Azure Developer CLI (azd) with environment setup
```
**Expected:** InfrastructurePlugin uses MCP `azd` tool for deployment orchestration
**Validation:**
- ✅ Uses azd commands for deployment
- ✅ Shows environment configuration steps
- ✅ Includes service connections
- ✅ Provides deployment status

**Test MCP-2.3: Provision AKS Cluster with MCP**
```
Provision a new AKS cluster in usgovvirginia with Azure best practices and monitoring enabled
```
**Expected:** InfrastructurePlugin uses MCP `aks` tool for cluster provisioning
**Validation:**
- ✅ Cluster provisioned with best practices
- ✅ Monitoring automatically configured
- ✅ Network policies enabled
- ✅ Shows cluster status and connection info

---

### 🚀 Deployment with MCP (DeploymentPlugin)

**Test MCP-3.1: Validate Bicep Template with Schema**
```
Validate my AKS deployment Bicep template against the official Azure schema before deploying
```
**Expected:** DeploymentPlugin uses MCP `bicepschema` for validation
**Validation:**
- ✅ Shows validation results (pass/fail)
- ✅ Lists any schema violations
- ✅ Provides fix recommendations
- ✅ Checks API version compatibility

**Test MCP-3.2: Run Pre-Deployment Checks**
```
Run comprehensive pre-deployment validation checks for my infrastructure template before I deploy to production
```
**Expected:** DeploymentPlugin performs multi-level validation via MCP
**Validation:**
- ✅ Schema validation (Bicep)
- ✅ Best practices check
- ✅ Security validation
- ✅ Cost estimation
- ✅ Pass/fail status with recommendations

**Test MCP-3.3: Troubleshoot Deployment with MCP**
```
My AKS deployment failed with error "QuotaExceeded". Can you help me troubleshoot this?
```
**Expected:** DeploymentPlugin uses MCP tools (applens, activitylogs) for troubleshooting
**Validation:**
- ✅ Analyzes deployment error
- ✅ Provides root cause analysis
- ✅ Shows relevant activity logs
- ✅ Suggests remediation steps
- ✅ Includes quota increase guidance

---

### 🛡️ Compliance with MCP (CompliancePlugin)

**Test MCP-4.1: Validate Azure Policy Compliance**
```
Check Azure Policy compliance for resource group "rg-prod-eastus" in subscription 00000000-0000-0000-0000-000000000000
```
**Expected:** CompliancePlugin uses MCP `azurepolicy` tool for validation
**Validation:**
- ✅ Shows policy compliance status
- ✅ Lists policy violations
- ✅ Provides violation details
- ✅ Includes remediation recommendations
- ✅ Shows compliance percentage

**Test MCP-4.2: Get Compliance Recommendations (NIST 800-53)**
```
What do I need to do to make my AKS cluster compliant with NIST 800-53 controls?
```
**Expected:** CompliancePlugin provides NIST compliance guidance via MCP
**Validation:**
- ✅ Lists applicable NIST controls
- ✅ Shows current compliance gaps
- ✅ Provides implementation guidance
- ✅ Includes control descriptions
- ✅ Offers remediation steps

**Test MCP-4.3: Get Compliance Recommendations (FedRAMP High)**
```
I need to make my infrastructure FedRAMP High compliant. What controls do I need to implement?
```
**Expected:** CompliancePlugin provides FedRAMP compliance guidance via MCP
**Validation:**
- ✅ Lists FedRAMP High controls
- ✅ Shows implementation requirements
- ✅ Includes technical controls (AC, AU, SC, IA)
- ✅ Provides configuration examples
- ✅ Links to FedRAMP documentation

---

### 🔒 Security with MCP (SecurityPlugin)

**Test MCP-5.1: Scan for Vulnerabilities with MCP**
```
Scan resource group "rg-prod-eastus" for security vulnerabilities and misconfigurations
```
**Expected:** SecurityPlugin uses MCP `securitycenter` tool for vulnerability scanning
**Validation:**
- ✅ Lists security findings
- ✅ Shows severity levels (Critical/High/Medium/Low)
- ✅ Provides vulnerability descriptions
- ✅ Includes CVE information where applicable
- ✅ Offers remediation guidance

**Test MCP-5.2: Get Security Best Practices for AKS**
```
What are the security best practices I should implement for my AKS cluster to harden it?
```
**Expected:** SecurityPlugin provides comprehensive security guidance via MCP
**Validation:**
- ✅ Lists security best practices
- ✅ Covers network security (private cluster, NSGs)
- ✅ Includes identity and access (managed identity, RBAC)
- ✅ Shows encryption requirements
- ✅ Provides threat protection recommendations

**Test MCP-5.3: Security Scanning with Threat Intelligence**
```
Run a comprehensive security scan including threat intelligence analysis for subscription 00000000-0000-0000-0000-000000000000
```
**Expected:** SecurityPlugin uses MCP `threatintelligence` and `defender` tools
**Validation:**
- ✅ Shows threat detection findings
- ✅ Lists active threats or suspicious activity
- ✅ Provides threat context and severity
- ✅ Includes Microsoft Defender recommendations
- ✅ Offers mitigation strategies

---

### 💰 Cost Management with MCP (CostManagementPlugin)

**Test MCP-6.1: Get Cost Optimization Recommendations**
```
Analyze my Azure subscription 00000000-0000-0000-0000-000000000000 and give me cost optimization recommendations with estimated savings
```
**Expected:** CostManagementPlugin combines platform cost engine with Azure Advisor and FinOps best practices
**Validation:**
- ✅ Shows top 10 cost-saving opportunities
- ✅ Includes estimated monthly/annual savings
- ✅ Provides Azure Advisor recommendations
- ✅ Includes FinOps best practices
- ✅ Offers implementation guide (quick wins, medium-term, long-term)
- ✅ Shows savings by resource type

**Test MCP-6.2: Get Budget Recommendations**
```
I need to set up budget alerts for my subscription. What budget should I set based on my current spending?
```
**Expected:** CostManagementPlugin provides smart budget recommendations with MCP best practices
**Validation:**
- ✅ Suggests monthly budget based on current spending
- ✅ Provides alert thresholds (50%, 75%, 90%, 100%)
- ✅ Includes severity levels for each threshold
- ✅ Shows budget best practices
- ✅ Includes financial governance guidance
- ✅ Provides automation scripts (Azure CLI)
- ✅ Offers anomaly detection strategies

**Test MCP-6.3: Cost Optimization with Resource Group Filter**
```
Show me cost optimization recommendations for resource group "rg-prod-eastus" only
```
**Expected:** CostManagementPlugin filters recommendations to specific resource group
**Validation:**
- ✅ Recommendations scoped to resource group
- ✅ Shows resource-specific savings
- ✅ Includes targeted optimization actions
- ✅ Provides resource group summary

---

### 🌍 Environment Management with MCP (EnvironmentManagementPlugin)

**Test MCP-7.1: Get Environment Best Practices with Tagging**
```
What are the Azure best practices for setting up a production environment with proper resource tagging and naming conventions?
```
**Expected:** EnvironmentManagementPlugin provides comprehensive environment guidance via MCP
**Validation:**
- ✅ Shows environment setup best practices
- ✅ Includes mandatory tags (Environment, Owner, CostCenter, Application)
- ✅ Includes recommended tags (ManagedBy, Criticality, DataClassification)
- ✅ Provides tag value examples
- ✅ Shows Azure naming convention patterns
- ✅ Includes resource type naming examples (AKS, App Service, Storage)
- ✅ Provides Well-Architected Framework guidance
- ✅ Includes 10 actionable recommendations

**Test MCP-7.2: Get Environment Best Practices (Development)**
```
I'm setting up a development environment. What are the best practices I should follow for dev environments versus production?
```
**Expected:** EnvironmentManagementPlugin provides dev-specific guidance
**Validation:**
- ✅ Environment type set to "development"
- ✅ Development-specific recommendations
- ✅ Cost optimization for dev (auto-shutdown, smaller SKUs)
- ✅ Security appropriate for dev (less restrictive than prod)
- ✅ Tagging for environment isolation

**Test MCP-7.3: Validate Environment Configuration (Basic)**
```
Validate the configuration of environment "dev-aks-eastus" at a basic level
```
**Expected:** EnvironmentManagementPlugin performs basic validation checks
**Validation:**
- ✅ Runs basic checks: Naming, Tagging, RBAC, Networking
- ✅ Shows validation results (✓ passed, ⚠ warnings)
- ✅ Provides overall status (Passed/Passed with warnings/Failed)
- ✅ Lists critical issues and warnings
- ✅ Includes Azure Policy compliance status
- ✅ Shows Security Center recommendations
- ✅ Provides next steps

**Test MCP-7.4: Validate Environment Configuration (Comprehensive)**
```
Run a comprehensive validation of environment "prod-aks-eastus" in subscription 00000000-0000-0000-0000-000000000000 including security, compliance, cost, and DR checks
```
**Expected:** EnvironmentManagementPlugin performs comprehensive validation
**Validation:**
- ✅ Runs all checks: Basic + Standard + Comprehensive (16 total checks)
- ✅ Covers: Naming, Tagging, RBAC, Networking, Security, Monitoring, Backup, Cost, Compliance, DR, Scaling, Documentation
- ✅ Shows Azure Policy compliance status
- ✅ Includes Microsoft Defender for Cloud findings
- ✅ Shows Azure Advisor recommendations
- ✅ Provides validation best practices
- ✅ Includes remediation scripts (Azure CLI) for common issues
- ✅ Lists prioritized next steps

**Test MCP-7.5: Validate with Remediation Scripts**
```
Validate environment "prod-app-service" and include remediation scripts for any issues found
```
**Expected:** EnvironmentManagementPlugin provides validation with remediation automation
**Validation:**
- ✅ Shows validation results
- ✅ Includes remediation scripts for:
  - Tagging (az tag create)
  - NSG creation (az network nsg create)
  - Diagnostic settings (az monitor diagnostic-settings create)
  - Backup configuration (az backup protection enable)
- ✅ Scripts are ready to execute
- ✅ Includes script descriptions

---

## 🧪 MCP Integration Verification Tests

### Verify MCP Tool Availability

**Test MCP-V1: Check MCP Connection**
```
Can you verify that Azure MCP tools are available and working?
```
**Expected:** System confirms MCP connectivity and lists available tools
**Validation:**
- ✅ Shows MCP connection status
- ✅ Lists available tools (15+ tools)
- ✅ Includes: bicepschema, azd, aks, deploy, azurepolicy, securitycenter, advisor, etc.

### Test MCP Tool Integration

**Test MCP-V2: Bicep Schema Tool**
```
Use the Bicep schema tool to show me the latest API version for Microsoft.Storage/storageAccounts
```
**Expected:** Uses MCP `bicepschema` tool
**Validation:**
- ✅ Returns latest API version
- ✅ Shows available properties
- ✅ Includes required vs optional fields

**Test MCP-V3: Azure Advisor Tool**
```
Get Azure Advisor recommendations for cost optimization in subscription 00000000-0000-0000-0000-000000000000
```
**Expected:** Uses MCP `advisor` tool
**Validation:**
- ✅ Returns Advisor recommendations
- ✅ Categorizes by type (Cost, Security, Performance, etc.)
- ✅ Includes impact and estimated savings

**Test MCP-V4: Best Practices Tool**
```
Get Azure best practices for AKS cluster security hardening
```
**Expected:** Uses MCP `get_bestpractices` tool
**Validation:**
- ✅ Returns comprehensive best practices
- ✅ Covers security, networking, identity
- ✅ Includes implementation guidance

---

## 🔥 Complex Real-World Scenarios
```
We're deploying a new mission-critical application for SPAWAR. The application is called "Maritime Ops Dashboard" and needs to be FedRAMP High compliant. We need:
- AKS cluster with 5 nodes in USGov Virginia
- Azure SQL Database with geo-replication
- Azure Key Vault for secrets management
- Application Gateway for load balancing
- All resources must be in private VNets with no public IPs
- We have a budget of $25,000 per month
- Need complete RMF documentation package for ATO

Please help me get this set up and ensure everything is compliant.
```
**Expected:** Multi-agent orchestration (Infrastructure → Environment → Compliance → Cost)

**Validation:**
- ✅ InfrastructureAgent generates FedRAMP-compliant templates
- ✅ EnvironmentAgent configures private VNets and security
- ✅ ComplianceAgent validates RMF requirements
- ✅ CostManagementAgent sets up budget alerts at $25k threshold

---

### Scenario 2: Cost Crisis Response
```
Our Azure bill for last month was $150,000 but we only budgeted $50,000. I need to figure out what happened and get costs under control immediately. Can you:
1. Show me what's costing the most
2. Identify any resources that shouldn't be running
3. Find optimization opportunities
4. Help me set up proper cost controls
5. Make sure any changes won't break compliance
```
**Expected:** Discovery → Cost → Compliance analysis

---

### Scenario 3: Audit Preparation
```
We have a security audit coming up in 2 weeks. I need to prepare a complete audit package including:
- Inventory of all resources across all environments
- Compliance status for NIST 800-53 controls
- Evidence of security controls implementation
- Cost reports showing we're within budget
- Documentation of all infrastructure configurations

Can you help me gather all of this?
```
**Expected:** Discovery → Compliance → Cost → Infrastructure (comprehensive report)

---

### Scenario 4: Environment Troubleshooting
```
Our production environment is having issues. Users are reporting slow performance and we're seeing errors. Can you help me:
1. Check what's currently deployed in production
2. Compare it to our staging environment to see if there are differences
3. Review the configuration for any misconfigurations
4. Check if we're hitting any resource limits
5. Verify everything is still compliant while we troubleshoot
```
**Expected:** Discovery → Environment → Compliance analysis

---

### Scenario 5: Mission Expansion
```
We have an existing mission "Project Alpha" that's been running successfully. Now we need to expand it to support 10x more users. We need to:
1. Assess current infrastructure and costs
2. Design scaled infrastructure to handle the increased load
3. Estimate new costs and ensure we stay within budget
4. Ensure all new infrastructure maintains compliance
5. Plan the migration with minimal downtime

What's the best approach?
```
**Expected:** Discovery → Infrastructure → Cost → Compliance (expansion planning)

---

## ✅ What to Validate

For each test query, check:

1. **✅ Correct Agent Selection**
   - Did the OrchestratorAgent select the right agent(s)?
   - Check `metadata.agentsInvoked`

2. **✅ Appropriate Execution Pattern**
   - Sequential for dependent tasks
   - Parallel for independent tasks
   - Collaborative for complex workflows
   - Check `metadata.executionPattern`

3. **✅ Response Quality**
   - Does the response address the query?
   - Is the information accurate and helpful?
   - Are suggestions actionable?

4. **✅ Context Awareness**
   - Multi-turn conversations maintain context
   - Follow-up questions understood correctly
   - References to "it", "them", etc. resolved

5. **✅ Performance**
   - Response time reasonable (< 10 seconds for complex queries)
   - Check `metadata.processingTimeMs`

6. **✅ Error Handling**
   - Graceful degradation on errors
   - Clear error messages
   - Helpful suggestions for resolution

---

## 📊 Testing Tips

1. **Start Simple**: Test single-agent scenarios first before complex multi-agent orchestration
2. **Use Real Data**: Replace placeholder subscription IDs with real ones for accurate testing
3. **Check Logs**: Monitor agent logs to see planning and execution details
4. **Test Edge Cases**: Try ambiguous queries, missing information, and invalid inputs
5. **Validate Metadata**: Always check the metadata to confirm agent selection and execution pattern
6. **Test Conversations**: Multi-turn conversations are key to validating context carryover
7. **Performance Baseline**: Establish baseline response times for different query types

---

## 🚀 Quick Start Commands

```bash
# Test a single query
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Show me all my AKS clusters and check if they are compliant",
    "conversationId": "test-1"
  }' | jq .

# Test multi-turn conversation
# Turn 1
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Discover all AKS clusters in my subscription",
    "conversationId": "conversation-1"
  }' | jq .

# Turn 2 (references "them" from Turn 1)
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "How much are they costing me per month?",
    "conversationId": "conversation-1"
  }' | jq .
```

---

## 🐛 Known Issues and Troubleshooting

### ✅ Issue #1: "Actually Provision" Workflow - **FIXED** (2025-10-22)

**Status:** ✅ **RESOLVED**

**Original Symptom:**
- User says "Actually provision this template and create the resources now"
- Only InfrastructureAgent was invoked (generated another template)
- NO actual Azure resources were created
- NO Environment, Compliance, Discovery, or Cost agents invoked

**Root Cause:**
The `ExecutionPlanValidator.IsTemplateGenerationRequest()` method was incorrectly classifying "Actually provision" messages as template generation requests because it checked for keywords like "provision" without first checking for explicit provisioning intent.

**Fix Applied:**
Updated `ExecutionPlanValidator.cs` with the following changes:

1. **Added `IsActualProvisioningRequest()` method** - Detects explicit provisioning keywords:
   - "actually provision", "make it live", "create the resources now"
   - "execute deployment", "deploy the template", "provision now"
   - Combination of urgency + provisioning keywords

2. **Updated `IsTemplateGenerationRequest()` method** - Now checks for provisioning intent FIRST:
   ```csharp
   // CRITICAL: Check if this is actually a provisioning request FIRST
   if (IsActualProvisioningRequest(message))
   {
       return false; // NOT template generation - it's provisioning!
   }
   ```

3. **Added `HasAllProvisioningAgents()` method** - Validates that plan has all 5 agents

4. **Created `CreateActualProvisioningPlan()` method** - Generates correct 5-agent workflow:
   - Infrastructure (priority 1) - Generate/validate template **FIRST**
   - Environment (priority 2) - Execute deployment **SECOND**
   - Discovery (priority 3) - Verify resources created in new RG **THIRD**
   - Compliance (priority 4) - Scan **only the new resource group** **FOURTH**
   - CostManagement (priority 5) - Estimate costs for new resources **LAST**
   - Sequential pattern, 120-second estimate

5. **Updated `ValidateAndCorrect()` logic** - Proper priority order:
   - First: Check compliance scanning (existing resources)
   - Second: Check actual provisioning (explicit deploy intent)
   - Third: Check template generation (default/safe mode)

**Testing:**
1. Restart the API server
2. Run Test Case 1.6 workflow
3. Verify all 5 agents are invoked when user says "Actually provision"
4. Verify execution time is 60-180 seconds (not 15-30 seconds)
5. Check logs for: `🚀 DETECTED ACTUAL PROVISIONING REQUEST`

**Verification in Logs:**
When fix is working, you'll see:
```
info: ExecutionPlanValidator[0]
      🚀 DETECTED ACTUAL PROVISIONING REQUEST: 'Actually provision this template...'
info: ExecutionPlanValidator[0]
      ✅ Plan has all 5 provisioning agents: Infrastructure, Environment, Discovery, Compliance, CostManagement
info: OrchestratorAgent[0]
      📋 Execution plan created: Sequential with 5 tasks
```

Instead of the old incorrect behavior:
```
warn: ExecutionPlanValidator[0]
      ⚠️  Plan validation: Template generation request incorrectly includes provisioning agents
info: ExecutionPlanValidator[0]
      ✅ Correcting plan to infrastructure-only template generation
info: OrchestratorAgent[0]
      📋 Execution plan created: Sequential with 1 tasks
```

---
  }' | jq .

# Turn 2 (references "them" from Turn 1)
curl -X POST http://localhost:5100/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "How much are they costing me per month?",
    "conversationId": "conversation-1"
  }' | jq .
```

---

## 🧪 Validation Checklist for Conversational Flows

Use this checklist to verify conversational requirements gathering is working correctly:

### Single-Agent Conversational Flow
- [ ] Agent asks questions in ONE message (not multiple back-and-forth)
- [ ] Questions are targeted to missing critical information only
- [ ] Agent uses smart defaults for non-critical details
- [ ] After user answers, agent IMMEDIATELY calls function (no "Should I proceed?")
- [ ] Agent does NOT repeat the same questions
- [ ] Agent does NOT ask for information user already provided

### Multi-Agent Conversational Flow
- [ ] First agent asks for shared info (subscription, location)
- [ ] Subsequent agents check SharedMemory BEFORE asking
- [ ] Subsequent agents ask ONLY domain-specific questions
- [ ] NO redundant questions across agents
- [ ] Context is maintained throughout conversation
- [ ] Each agent confirms intent but doesn't re-ask for data

### Service Wizard (Plugin-Based, Not Agent)
**Note:** ServiceCreation functionality currently provided via ServiceWizardPlugin
- [ ] Wizard provides 8-step interactive flow
- [ ] Includes DoD compliance validation
- [ ] Mission metadata collection
- [ ] See PHASE1.md for implementation details

### Error Handling
- [ ] Agent handles very vague requests gracefully (asks clarifying questions)
- [ ] Agent provides examples when user is confused
- [ ] Agent validates critical inputs before proceeding
- [ ] Agent confirms destructive operations
- [ ] Agent explains smart defaults being used

---

## 🐛 Common Issues with Conversational Flows

| Issue | Cause | Solution |
|-------|-------|----------|
| Agent asks same question twice | Not checking conversation history | Agent must review previous messages before asking |
| Agent asks for confirmation after user answers | Over-cautious prompting | After user provides answers, call function immediately |
| Multi-agent workflow asks for subscription ID 3 times | Not checking SharedMemory | Each agent must check SharedMemory for context first |
| Agent overwhelms with too many questions at once | No progressive disclosure | Ask 2-5 targeted questions per turn |
| Agent generates template without asking questions | Missing conversational trigger | Agent must ask questions BEFORE calling functions |
| Agent provides guidance but doesn't call function | Response-only mode | Agent must CALL FUNCTIONS, not just explain what it would do |

---

## 📊 Quick Reference - Conversational Patterns

### Expected Question Counts by Agent
- **InfrastructureAgent**: 3-5 questions (environment, security, monitoring, scaling, integrations)
- **ComplianceAgent**: 2-4 questions (subscription, scope, framework, control families)
- **CostManagementAgent**: 2-4 questions (scope, time period, breakdown, optimization focus)
- **DiscoveryAgent**: 2-3 questions (resource types, scope, output format)
- **EnvironmentAgent**: 3-4 questions (environment type, location, configuration level)

**Note:** ServiceWizardPlugin (8-step wizard) is available for mission/team onboarding workflows

### Conversational Flow Patterns

**Single-Turn (User provides all details):**
```
User: "Deploy AKS with 3 nodes in usgovvirginia, dev environment, with monitoring"
Agent: [IMMEDIATELY generates template - no questions needed]
```

**Two-Turn (User provides partial details):**
```
Turn 1: User: "Deploy an AKS cluster"
Turn 2: Agent: "I need a few details: region? environment? node count?"
Turn 3: User: "usgovvirginia, dev, 3 nodes"
Turn 4: Agent: [IMMEDIATELY generates template - no confirmation]
```

**Multi-Turn Progressive (Complex ServiceCreation):**
```
Turn 1-2: Mission basics (4 questions)
Turn 3-4: Technical requirements (4 questions)
Turn 5-6: Compliance needs (4 questions)
Turn 7-8: Budget and constraints (2 questions)
Turn 9: Agent creates ServiceCreation request
```

---

**Last Updated:** October 2025  
**Total Test Cases:** 17 core + 17 MCP + 15 conversational = 49 tests  
**Document Version:** 3.0 (Conversational Requirements Gathering)

---

**Happy Testing!** 🎉

Use these natural language queries to thoroughly test the multi-agent system and validate that the orchestration is working as expected.
