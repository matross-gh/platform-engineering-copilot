# Natural Language Test Cases for Multi-Agent System

This document contains natural language test queries to validate the multi-agent platform engineering copilot system. Use these queries to test agent routing, orchestration, and response quality.

## ⚠️ IMPORTANT: Template Generation vs Actual Provisioning vs Compliance Scanning

The system distinguishes between **FOUR types of requests**:

### 1️⃣ **COMPLIANCE SCANNING/ASSESSMENT** (Analyzing existing resources)
- **User says:** "check compliance", "run a compliance assessment", "scan my subscription", "compliance status", "security assessment"
- **User mentions:** Checking EXISTING resources ("my cluster", "my subscription", "current environment")
- **Keywords:** "assess", "scan", "check", "validate", "audit", "evaluate" + compliance/security
- **What happens:** ComplianceAgent ONLY - scans existing resources for compliance
- **Agents invoked:** Compliance agent only
- **Time estimate:** 30-60 seconds
- **No templates generated, no resources created**

### 2️⃣ **TEMPLATE GENERATION** (Infrastructure design/planning - SAFE DEFAULT)
- **User says:** "deploy", "create", "set up", "I need" infrastructure WITHOUT "actually"/"provision"/"make it live"
- **User confirms:** "yes", "proceed", "sounds good" after conversational requirements gathering
- **User asks for:** "template", "Bicep", "ARM", "IaC", "blueprint", "compliant infrastructure"
- **What happens:** InfrastructureAgent ONLY - generates templates/code
- **Agents invoked:** Infrastructure agent only
- **Time estimate:** 10-30 seconds
- **Safety:** Creates CODE only, NO actual Azure resources, NO costs
- **This is the DEFAULT for safety** - prevents accidental resource creation

### 3️⃣ **ACTUAL PROVISIONING** (Resource deployment - REQUIRES EXPLICIT INTENT)
- **User EXPLICITLY says:** "actually provision", "make it live", "deploy the template", "create the resources now", "execute deployment", "provision for real"
- **User requests:** "provision existing template", "I want to deploy this now"
- **What happens:** Full workflow - Infrastructure → Environment → Compliance → Discovery → Cost
- **Agents invoked:** All 5 agents in sequence
- **Time estimate:** 60-180 seconds
- **Warning:** Creates REAL Azure resources and incurs REAL costs
- **Requires explicit confirmation** to prevent accidental deployment

### 4️⃣ **INFORMATIONAL** (Questions/guidance)
- **User asks:** "What are...", "How do I...", "Best practices...", "Show me examples"
- **What happens:** Relevant agent provides guidance only
- **Agents invoked:** Single agent (usually Infrastructure or Compliance)
- **Time estimate:** 5-15 seconds
- **No templates generated, no resources created**

> **💡 Key Insight:** When you say "I need to deploy a new AKS cluster" and then answer questions and say "yes proceed", the system will **generate a Bicep template**, NOT actually provision resources. To actually provision, you must explicitly say "actually provision this" or "make it live".
> 
> **🔍 Compliance vs Infrastructure:** "Check compliance" scans EXISTING resources (ComplianceAgent). "Create compliant infrastructure" generates NEW templates (InfrastructureAgent).

## 🧪 How to Test

### Quick Test via curl
```bash
curl -X POST http://localhost:7001/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "YOUR_QUERY_HERE",
    "conversationId": "test-1"
  }' | jq .
```

### Using the Chat UI
1. Start the API: `dotnet run --project src/Platform.Engineering.Copilot.API`
2. Start the Admin Client: `dotnet run --project src/Platform.Engineering.Copilot.Admin.Client`
3. Navigate to the chat interface
4. Copy/paste queries from below

---

## 📋 Single-Agent Test Cases

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
I need to deploy a new Kubernetes cluster in Azure. Can you help me set up an AKS cluster in the US GOV Virginia region with 3 nodes in subscription 453c2549-4cc5-464f-ba66-acad920823e8?
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
Turn 7: "Actually provision this template and create the resources now in subscription 453c2549-4cc5-464f-ba66-acad920823e8"
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
Actually provision a production AKS cluster in usgovvirginia with 5 nodes in subscription 453c2549-4cc5-464f-ba66-acad920823e8. Make it live right now.
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
I need to deploy a new Kubernetes cluster in Azure. Can you help me set up an AKS cluster in the US GOV Virginia region with 3 nodes in subscription 453c2549-4cc5-464f-ba66-acad920823e8?
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
curl -X GET http://localhost:7001/api/jobs/{jobId}/artifacts
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
My App Service Plan in subscription 453c2549-4cc5-464f-ba66-acad920823e8 keeps scaling up and down too frequently. Can you optimize the auto-scaling configuration?
```
**Expected:** InfrastructureAgent analyzes and provides optimized scaling configuration

**⚠️ Note:** For actual optimization, you need to provide the full App Service Plan resource ID or name:
```
My App Service Plan "prod-app-service-plan" in resource group "prod-rg" and subscription 453c2549-4cc5-464f-ba66-acad920823e8 keeps scaling up too frequently. Can you optimize it?
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

**Test 2.1: Basic Compliance Check**
```
I need to check if my Azure subscription is compliant with NIST 800-53 controls. Can you run a compliance assessment?
```
**Expected:** ComplianceAgent performs compliance scan on EXISTING subscription resources

**Validation:**
- ✅ **CRITICAL:** Only ComplianceAgent is invoked (NOT InfrastructureAgent)
- ✅ Response shows compliance scan results (compliance score, failing controls, findings)
- ✅ Response includes resource-level compliance details
- ✅ Response does NOT include Bicep templates or infrastructure code
- ✅ **If you see template generation** → Bug! Orchestrator routed incorrectly

**Test 2.2: Specific Control Assessment**
```
Can you check the AC (Access Control) family controls from NIST 800-53 for my production environment and tell me what's failing?
```
**Expected:** ComplianceAgent focuses on Access Control family

**Test 2.3: RMF Package Generation**
```
I need to create a complete RMF package for my application. The system includes AKS, Azure SQL, and Storage Accounts. Can you help me generate the System Security Plan and other required documents?
```
**Expected:** ComplianceAgent generates RMF documentation

**Test 2.4: eMASS Integration**
```
How do I upload my compliance evidence to eMASS? What format does it need to be in?
```
**Expected:** ComplianceAgent provides eMASS guidance

**Test 2.5: Compliance Gap Analysis**
```
We're trying to achieve an Authority to Operate (ATO). Can you analyze our current environment and tell me what compliance gaps we need to address?
```
**Expected:** ComplianceAgent performs gap analysis

---

### 💰 CostManagementAgent (Costs/Budgets/Optimization)

**Test 3.1: Cost Overview**
```
What did I spend on Azure last month? Can you break it down by service?
```
**Expected:** CostManagementAgent provides cost analysis

**Test 3.2: Cost Trends**
```
Show me the cost trends for my subscription over the last 6 months. Are costs going up or down?
```
**Expected:** CostManagementAgent analyzes trends

**Test 3.3: Cost Optimization**
```
My Azure bill seems high. Can you identify resources that are costing a lot and suggest ways to reduce costs?
```
**Expected:** CostManagementAgent provides optimization recommendations

**Test 3.4: Budget Management**
```
I need to set up a budget of $10,000 per month for my development environment. Can you help me configure budget alerts?
```
**Expected:** CostManagementAgent helps with budget setup

**Test 3.5: Cost Forecast**
```
Based on my current usage patterns, what will my Azure costs be next month?
```
**Expected:** CostManagementAgent provides forecast

**Test 3.6: Resource-Specific Costs**
```
How much am I spending on AKS clusters specifically? Which cluster is the most expensive?
```
**Expected:** CostManagementAgent analyzes AKS costs

---

### 🌍 EnvironmentAgent (Environments/Configurations)

**Test 4.1: Environment Overview**
```
Can you show me all the environments I have set up? I need to know what's running in dev, test, and production.
```
**Expected:** EnvironmentAgent lists environments

**Test 4.2: Environment Configuration Review**
```
I want to make sure my production environment is configured correctly. Can you review the settings and check for any issues?
```
**Expected:** EnvironmentAgent validates configuration

**Test 4.3: Environment Comparison**
```
Is my staging environment configured the same way as production? I need to ensure consistency.
```
**Expected:** EnvironmentAgent compares configurations

**Test 4.4: Environment Promotion**
```
I need to promote my application from dev to staging. What's the process and what configuration changes do I need to make?
```
**Expected:** EnvironmentAgent provides promotion guidance

**Test 4.5: Environment Best Practices**
```
What are the recommended settings for a development environment versus a production environment?
```
**Expected:** EnvironmentAgent provides best practices

---

### 🔍 DiscoveryAgent (Inventory/Resources)

**Test 5.1: Complete Resource Discovery**
```
What resources do I have running in my Azure subscription right now? Give me a complete inventory.
```
**Expected:** DiscoveryAgent scans and lists all resources

**Test 5.2: Specific Resource Type**
```
Show me all the Kubernetes clusters I have across all my subscriptions. How many are there and where are they located?
```
**Expected:** DiscoveryAgent finds all AKS clusters

**Test 5.3: Resource Search**
```
I'm looking for a storage account that has "production" in the name. Can you help me find it?
```
**Expected:** DiscoveryAgent searches for specific resource

**Test 5.4: Resource Dependencies**
```
I need to delete a virtual network, but I'm not sure what else depends on it. Can you show me all the resources connected to it?
```
**Expected:** DiscoveryAgent maps dependencies

**Test 5.5: Orphaned Resources**
```
Are there any resources in my subscription that aren't being used? I want to clean up and save money.
```
**Expected:** DiscoveryAgent identifies unused resources

**Test 5.6: Resource Tags**
```
Show me all resources that don't have proper tags. We require CostCenter and Environment tags on everything.
```
**Expected:** DiscoveryAgent finds untagged resources

---

### 🚀 OnboardingAgent (Mission/Team Onboarding)

**Test 6.1: New Mission Onboarding**
```
We have a new mission coming online called "Project Lighthouse" for NAVWAR. The mission owner is Commander Sarah Johnson (sarah.johnson@navy.mil). We need to onboard this mission to the platform.
```
**Expected:** OnboardingAgent initiates onboarding workflow

**Test 6.2: Team Setup**
```
I need to set up a new team for the development of a classified application. The team has 5 developers, 2 DevOps engineers, and 1 security officer. What's the process?
```
**Expected:** OnboardingAgent provides team setup guidance

**Test 6.3: Resource Requirements**
```
For our new mission "Operation Atlas", we need AKS, Azure SQL Database, Azure Key Vault, and Azure Monitor in the USGov Virginia region. Can you help me get started?
```
**Expected:** OnboardingAgent collects requirements

**Test 6.4: Onboarding Status**
```
What's the status of the "Project Lighthouse" onboarding? Are we ready to start deploying resources?
```
**Expected:** OnboardingAgent checks onboarding progress

**Test 6.5: Onboarding Checklist**
```
What are all the steps required to onboard a new mission to the platform? I want to make sure we don't miss anything.
```
**Expected:** OnboardingAgent provides complete checklist

---

## 🔄 Multi-Agent Orchestration Test Cases

### Sequential Execution (Task Dependencies)

**Test 7.1: Deploy and Assess**
```
I need to deploy a new AKS cluster in US GOV Virginia with 3 nodes, and then run a compliance check on it to make sure it meets our security requirements.
```
**Expected:** InfrastructureAgent → ComplianceAgent (sequential)

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

**Test 9.1: Complete Mission Deployment**
```
I'm starting a new mission called "Secure Operations Platform" for NSWC. We need a complete setup: onboard the mission, provision infrastructure (AKS, SQL, Key Vault) in USGov Virginia, configure it properly, check compliance, and set up cost tracking with a $50k/month budget.
```
**Expected:** OnboardingAgent → InfrastructureAgent → EnvironmentAgent → ComplianceAgent → CostManagementAgent

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

## 🔥 Complex Real-World Scenarios

### Scenario 1: New Application Deployment
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
**Expected:** Full multi-agent orchestration (Onboarding → Infrastructure → Environment → Compliance → Cost)

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
curl -X POST http://localhost:7001/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Show me all my AKS clusters and check if they are compliant",
    "conversationId": "test-1"
  }' | jq .

# Test multi-turn conversation
# Turn 1
curl -X POST http://localhost:7001/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Discover all AKS clusters in my subscription",
    "conversationId": "conversation-1"
  }' | jq .

# Turn 2 (references "them" from Turn 1)
curl -X POST http://localhost:7001/api/chat/intelligent-query \
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
curl -X POST http://localhost:7001/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "How much are they costing me per month?",
    "conversationId": "conversation-1"
  }' | jq .
```

---

**Happy Testing!** 🎉

Use these natural language queries to thoroughly test the multi-agent system and validate that the orchestration is working as expected.
