# Infrastructure Agent Test Suite

**Last Updated:** November 13, 2025  
**Agent:** Infrastructure  
**Plugin Functions:** 21 total  
**Purpose:** Comprehensive testing of all Infrastructure Agent capabilities

## 📋 Overview

The Infrastructure Agent handles Azure infrastructure provisioning, template generation, network design, predictive scaling, and compliance-aware template enhancement.

**Key Capabilities:**
- **Template Generation**: Bicep/Terraform IaC code generation (NO provisioning)
- **Actual Provisioning**: Real Azure resource creation (⚠️ with explicit keywords)
- **Network Design**: Multi-tier topology design with CIDR calculation
- **Predictive Scaling**: AI-powered scaling forecasts and optimization
- **Compliance Integration**: FedRAMP, DoD IL5, NIST 800-53, SOC2, GDPR controls
- **Azure MCP Integration**: Best practices, schema validation, azd deployment

## 🎯 Quick Test Commands

```bash
# Test via MCP endpoint
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "YOUR_QUERY"}' | jq .

# Check orchestrator routing
docker logs plaform-engineering-copilot-mcp --since 2m | grep -E "(Plan created|Executing task with|agentType)"
```

## 🧪 Test Cases by Function Category

### 1️⃣ Template Generation Functions (7 functions)

#### Test INFRA-1.1: Generate Basic Infrastructure Template
```
Generate a Bicep template for an AKS cluster with 3 nodes in usgovvirginia
```
**Expected Function:** `generate_infrastructure_template`  
**Expected Output:**
- ✅ Bicep files generated (main.bicep, modules/*)
- ✅ AKS configuration with 3 nodes
- ✅ Node pool configuration
- ✅ Networking setup
- ✅ Managed identity
- ✅ **NO** actual resources created

**Validation:**
- ✅ Agent: Infrastructure ONLY
- ✅ Intent: "infrastructure"
- ✅ Response includes file paths
- ⏱️ Time: 15-30 seconds
- ❌ **NO** "resources created" message

---

#### Test INFRA-1.2: Generate Compliant Infrastructure Template
```
Generate a FedRAMP High compliant AKS template with all NIST controls
```
**Expected Function:** `generate_compliant_infrastructure_template`  
**Expected Output:**
- ✅ Bicep files with compliance controls
- ✅ FedRAMP High controls (AC, AU, SC, IA families)
- ✅ Security hardening (TLS, RBAC, encryption)
- ✅ Compliance validation report
- ✅ Control implementation checklist

**Validation:**
- ✅ Template includes compliance controls
- ✅ Validation findings shown
- ✅ Remediation recommendations provided

---

#### Test INFRA-1.3: Generate Template with Best Practices
```
Generate a production AKS template with Azure best practices built in
```
**Expected Function:** `generate_template_with_best_practices`  
**Expected Output:**
- ✅ Bicep template with security hardening
- ✅ Best practices from Azure MCP
- ✅ Logs show: "📚 Fetching Azure best practices via Azure MCP"
- ✅ Enhanced security configurations
- ✅ Monitoring/logging configured

**Validation:**
- ✅ Check logs for MCP integration
- ✅ Template has enhanced security
- ✅ Response mentions best practices source
- ⏱️ Time: 20-35 seconds (includes MCP call)

---

#### Test INFRA-1.4: Get Generated File Content
```
Show me the main.bicep file from the last generation
```
**Expected Function:** `get_generated_file`  
**Expected Output:**
- ✅ File content displayed
- ✅ Bicep code shown
- ✅ Syntax highlighted (if supported)

**Validation:**
- ✅ Correct file content returned
- ✅ No errors if file exists

---

#### Test INFRA-1.5: Get All Generated Files
```
Show me all the files that were generated
```
**Expected Function:** `get_all_generated_files`  
**Expected Output:**
- ✅ List of all files with paths
- ✅ File sizes shown
- ✅ Content of each file (or summary)

**Validation:**
- ✅ Complete file list
- ✅ Accurate content

---

#### Test INFRA-1.6: Get Module-Specific Files
```
Show me the AKS module code
```
**Expected Function:** `get_module_files`  
**Expected Output:**
- ✅ Only AKS-related files returned
- ✅ Filtered by module type
- ✅ No SQL/Storage/other module files

**Validation:**
- ✅ Correct module filtering
- ✅ Bug fix verified (was returning wrong modules)

---

#### Test INFRA-1.7: Generate IL-Compliant Template
```
Generate a DoD IL5 compliant infrastructure template
```
**Expected Function:** `generate_il_compliant_template`  
**Expected Output:**
- ✅ Template with DoD IL5 controls
- ✅ Enhanced security (PE controls)
- ✅ Azure Government configurations
- ✅ Impact Level policy compliance

**Validation:**
- ✅ IL5-specific controls included
- ✅ Physical security considerations

---

### 2️⃣ Actual Provisioning Functions (2 functions)

⚠️ **WARNING:** These create REAL Azure resources

#### Test INFRA-2.1: Provision Infrastructure Immediately
```
Actually provision an AKS cluster NOW in subscription 00000000-0000-0000-0000-000000000000
```
**Expected Function:** `provision_infrastructure`  
**Expected Output:**
- ✅ Real Azure resources created
- ✅ Resource IDs returned
- ✅ Deployment status shown
- ✅ Connection information provided

**Validation:**
- ✅ **CRITICAL:** Only use with explicit "NOW", "IMMEDIATELY" keywords
- ✅ Real resources exist in Azure
- ⏱️ Time: 60-180 seconds

---

#### Test INFRA-2.2: Provision AKS with Best Practices
```
Actually provision an AKS cluster with best practices NOW in subscription 00000000-0000-0000-0000-000000000000
```
**Expected Function:** `provision_aks_with_best_practices`  
**Expected Output:**
- ✅ AKS cluster created
- ✅ Best practices applied
- ✅ Security hardening enabled
- ✅ Monitoring configured

**Validation:**
- ✅ Real AKS cluster exists
- ✅ Best practices verified in configuration

---

### 3️⃣ Network Design Functions (2 functions)

#### Test INFRA-3.1: Design Network Topology
```
Design a 3-tier network with 10.0.0.0/16, include Bastion and Firewall
```
**Expected Function:** `design_network_topology`  
**Expected Output:**
- ✅ 3-tier layout (Web, Application, Data)
- ✅ Subnet CIDRs calculated
- ✅ Special subnets (Bastion, Firewall, Gateway)
- ✅ Service endpoints configured
- ✅ Network diagram (text or visual)

**Validation:**
- ✅ Correct tier naming (Web → Application → Data)
- ✅ CIDR calculations accurate
- ✅ All special subnets included

---

#### Test INFRA-3.2: Calculate Subnet CIDRs
```
How many /27 subnets can I fit in 10.0.0.0/20? Show me the CIDR ranges
```
**Expected Function:** `calculate_subnet_cidrs`  
**Expected Output:**
- ✅ Number of /27 subnets (128)
- ✅ CIDR range list
- ✅ IP count per subnet (32 IPs)
- ✅ Usable IPs (29, accounting for Azure reserved)

**Validation:**
- ✅ Math is correct
- ✅ Azure-specific IP reservations noted

---

### 4️⃣ Predictive Scaling Functions (3 functions)

#### Test INFRA-4.1: Predict Scaling Needs
```
Will my AKS cluster need to scale up in the next 24 hours?
```
**Expected Function:** `predict_scaling_needs`  
**Expected Output:**
- ✅ Scaling forecast with confidence score
- ✅ Predicted resource requirements
- ✅ Recommendations for capacity
- ✅ Metric predictions (CPU, memory)

**Validation:**
- ✅ Forecast time range matches query
- ✅ Confidence level provided
- ✅ Actionable recommendations

---

#### Test INFRA-4.2: Optimize Scaling Configuration
```
My App Service Plan keeps scaling up too frequently. Can you optimize it?
```
**Expected Function:** `optimize_scaling_configuration`  
**Expected Output:**
- ✅ Current scaling analysis
- ✅ Identified inefficiencies
- ✅ Optimized scaling rules
- ✅ Expected cost impact

**Validation:**
- ✅ Specific resource analyzed
- ✅ Optimization recommendations provided
- ✅ Before/after comparison

---

#### Test INFRA-4.3: Analyze Scaling Performance
```
How has my AKS cluster's auto-scaling been performing over the last 7 days?
```
**Expected Function:** `analyze_scaling_performance`  
**Expected Output:**
- ✅ Historical scaling events
- ✅ Efficiency metrics
- ✅ Over/under-provisioning analysis
- ✅ Cost analysis of scaling

**Validation:**
- ✅ 7-day time range analyzed
- ✅ Performance metrics shown
- ✅ Cost impact calculated

---

### 5️⃣ Compliance & Policy Functions (5 functions)

#### Test INFRA-5.1: Validate Template IL Compliance
```
Validate my AKS template against DoD IL5 requirements
```
**Expected Function:** `validate_template_il_compliance`  
**Expected Output:**
- ✅ Compliance validation results
- ✅ Pass/fail for each control
- ✅ Missing controls identified
- ✅ Remediation guidance

**Validation:**
- ✅ IL5-specific validation
- ✅ Detailed findings provided

---

#### Test INFRA-5.2: Get IL Policy Requirements
```
What are the requirements for DoD Impact Level 5?
```
**Expected Function:** `get_il_policy_requirements`  
**Expected Output:**
- ✅ IL5 control list
- ✅ Required NIST controls
- ✅ Physical security requirements
- ✅ Comparison with IL4

**Validation:**
- ✅ Comprehensive IL5 requirements
- ✅ DoD Cloud Computing SRG referenced

---

#### Test INFRA-5.3: Apply IL Policies to Template
```
Apply DoD IL5 policies to my existing template
```
**Expected Function:** `apply_il_policies_to_template`  
**Expected Output:**
- ✅ Enhanced template with IL5 controls
- ✅ Policy application summary
- ✅ Changes made to template
- ✅ Validation report

**Validation:**
- ✅ Template modified correctly
- ✅ All IL5 policies applied

---

#### Test INFRA-5.4: Get Remediation Guidance
```
How do I fix the compliance violations in my template?
```
**Expected Function:** `get_remediation_guidance`  
**Expected Output:**
- ✅ List of violations
- ✅ Step-by-step remediation
- ✅ Code examples
- ✅ Priority order

**Validation:**
- ✅ Actionable guidance provided
- ✅ Code snippets included

---

### 6️⃣ Azure Integration Functions (3 functions)

#### Test INFRA-6.1: Deploy with Azure Developer CLI
```
Deploy this template using azd
```
**Expected Function:** `deploy_infrastructure_with_azd`  
**Expected Output:**
- ✅ azd init/up commands
- ✅ Environment setup steps
- ✅ Service connections
- ✅ Deployment status

**Validation:**
- ✅ azd commands shown
- ✅ Deployment workflow provided

---

#### Test INFRA-6.2: Set Azure Subscription
```
Set my Azure subscription to 00000000-0000-0000-0000-000000000000
```
**Expected Function:** `set_azure_subscription`  
**Expected Output:**
- ✅ Subscription ID saved
- ✅ Confirmation message
- ✅ Context updated

**Validation:**
- ✅ Subscription set correctly
- ✅ Future operations use this subscription

---

#### Test INFRA-6.3: Delete Resource Group
```
Delete the resource group rg-test-eastus
```
**Expected Function:** `delete_resource_group`  
**Expected Output:**
- ✅ Confirmation prompt (if enabled)
- ✅ Deletion status
- ✅ Cleanup summary

**Validation:**
- ✅ Resource group deleted in Azure
- ⚠️ **DESTRUCTIVE** operation

---

## 🔄 Multi-Turn Conversation Tests

### Test INFRA-7.1: Progressive Template Refinement
```
Turn 1: "I need an AKS cluster"
Turn 2: (Agent asks about environment, security, etc.)
Turn 3: "Production, zero trust security, monitoring"
Turn 4: (Agent confirms configuration)
Turn 5: "Yes, generate the template"
```
**Expected Behavior:**
- Agent gathers requirements conversationally
- Template reflects ALL conversation inputs
- No template generated until confirmation

**Validation:**
- ✅ Context maintained across turns
- ✅ All requirements in final template
- ✅ Only Infrastructure agent invoked (template gen)

---

### Test INFRA-7.2: Template Generation → Provisioning
```
Turn 1: "Generate an AKS template"
Turn 2: (Agent asks questions, generates template)
Turn 3: "Actually provision this template NOW"
```
**Expected Behavior:**
- Turn 1-2: Template generation only
- Turn 3: Full workflow (Infrastructure → Environment → Discovery → Compliance → Cost)

**Validation:**
- ✅ Template generated first
- ✅ Provisioning only on explicit request
- ✅ All 5 agents invoked for provisioning

---

## 🎯 Edge Cases & Error Handling

### Test INFRA-8.1: Invalid Resource Type
```
Generate a template for a FooBar service
```
**Expected:** Graceful error or clarification request

---

### Test INFRA-8.2: Conflicting Requirements
```
Generate a free-tier AKS cluster for production with 100 nodes
```
**Expected:** Agent identifies conflict, asks for clarification

---

### Test INFRA-8.3: Missing Critical Info
```
Deploy infrastructure
```
**Expected:** Agent asks clarifying questions (what resource? where? which subscription?)

---

## 🚨 Anti-Patterns (Should NOT Work This Way)

### ❌ Test INFRA-9.1: Compliance Scan Routed to Infrastructure
```
Check my subscription for NIST compliance
```
**Expected:** Routes to **Compliance Agent**, NOT Infrastructure  
**Reason:** This is assessment, not template generation

---

### ❌ Test INFRA-9.2: Cost Analysis Routed to Infrastructure
```
Show me cost savings opportunities
```
**Expected:** Routes to **Cost Management Agent**, NOT Infrastructure  
**Reason:** This is cost analysis, not infrastructure

---

## 📊 Validation Checklist

For each Infrastructure Agent test, verify:

### Agent Routing
- [ ] `agentType: "Infrastructure"` in plan
- [ ] Only Infrastructure agent for template generation
- [ ] All 5 agents for actual provisioning

### Response Quality
- [ ] Template generation returns code/files
- [ ] Provisioning returns resource IDs
- [ ] Compliance controls applied when requested
- [ ] Best practices integrated from Azure MCP

### Performance
- [ ] Template generation: 15-30 seconds (20-35 with MCP)
- [ ] Actual provisioning: 60-180 seconds
- [ ] Network design: 10-20 seconds

### Context Handling
- [ ] Conversation context maintained
- [ ] Requirements gathered systematically
- [ ] No redundant questions

---

## 🐛 Known Issues & Limitations

### Issue 1: MCP Best Practices Not Included
**Status:** ✅ WORKING (v0.6.35+)  
**Feature:** Azure MCP integration for best practices

### Issue 2: Storage Template Generated Instead of Requested Resource
**Status:** ✅ FIXED (2025-10-21)  
**Fix:** Infer resource type from description

### Issue 3: "Actually Provision" Generates Template Only
**Status:** ✅ FIXED (2025-10-22)  
**Fix:** ExecutionPlanValidator detects provisioning intent

---

## 📖 Related Documentation

- **Agent Architecture:** [AGENT-ORCHESTRATION.md](./AGENT-ORCHESTRATION.md)
- **General Test Cases:** [NATURAL-LANGUAGE-TEST-CASES.md](./NATURAL-LANGUAGE-TEST-CASES.md)
- **Deployment Guide:** [DEPLOYMENT.md](./DEPLOYMENT.md)

---

**Last Updated:** November 13, 2025  
**Test Coverage:** 21 functions, 35+ test cases  
**Status:** Ready for comprehensive testing
