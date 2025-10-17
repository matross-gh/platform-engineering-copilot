# Platform Engineering Copilot - Comprehensive Prompt Guide

**Last Updated:** January 17, 2025

> **AI-Powered Infrastructure Provisioning with Natural Language**

---

## 📋 Table of Contents

1. [Introduction](#introduction)
2. [Prompt Fundamentals](#prompt-fundamentals)
3. [Chat Interface Prompts](#chat-interface-prompts)
4. [REST API Prompts](#rest-api-prompts)
5. [Admin Console Operations](#admin-console-operations)
6. [Advanced Techniques](#advanced-techniques)
7. [Best Practices](#best-practices)
8. [Troubleshooting Prompts](#troubleshooting-prompts)

---

## 🎯 Introduction

The Platform Engineering Copilot uses natural language processing powered by Azure OpenAI (GPT-4o) and Microsoft Semantic Kernel to understand your infrastructure needs and execute real-time Azure operations. This guide teaches you how to write effective prompts to get the best results.

### Understanding the AI System

The system uses four levels of intelligence:

1. **Intent Classification**: Determines what you want to do (provision, compliance scan, cost analysis, etc.)
2. **Parameter Extraction**: Extracts structured data from your conversational input
3. **Context Awareness**: Remembers previous messages and maintains conversation state across sessions
4. **Policy Evaluation**: Real-time Azure Policy validation before deployment
5. **Action Execution**: Direct integration with Azure Resource Manager APIs for real infrastructure operations

### Key Features

- **Real Azure Resource Management**: Direct Azure ARM API integration for infrastructure provisioning
- **ATO Compliance Automation**: NIST 800-53 Rev 5 scanning with automated remediation recommendations
- **Azure Policy Integration**: Real-time policy evaluation via Azure Policy Insights API
- **Cost Intelligence**: Live cost analysis and optimization recommendations via Azure Cost Management API
- **Approval Workflows**: Database-backed approval system for policy exceptions and infrastructure changes
- **Persistent Storage**: All workflows and assessments stored in SQLite database for audit trails
- **Intelligent Caching**: 5-minute cache for policy evaluations to optimize performance

### Conversation Context & Memory

**What's Persisted**:
- ✅ **Message History**: All chat messages stored in SQLite database (Chat.App)
- ✅ **Approval Workflows**: All policy exception requests and approvals persisted
- ✅ **Onboarding Requests**: All service onboarding data stored for audit trail

**What's Ephemeral** (In-Memory Only):
- ⚠️ **AI Context Understanding**: The AI's semantic understanding of your conversation (extracted parameters, workflow state, inferred intent) is stored in-memory only
- ⚠️ **Session State**: Lost on application restart or browser refresh
- ⚠️ **Multi-Turn Dialogue Context**: Works within a session but not across sessions

**Best Practice**: Complete related tasks in a single conversation session. If you need to pause and resume, provide full context in your next message rather than relying on the system to remember from a previous session.

### Primary Interfaces

1. **REST API** (`http://localhost:7001`): Direct HTTP API for programmatic access
2. **Chat App** (`http://localhost:3000`): React-based conversational interface for onboarding
3. **Admin Console** (`http://localhost:3001`): Template management and approval workflows

---

## 🎓 Prompt Fundamentals

### The Anatomy of a Good Prompt

**Structure**:
```
[CONTEXT] + [ACTION] + [REQUIREMENTS] + [CONSTRAINTS]
```

**Example**:
```
Context: "I'm a Navy Commander working on a classified mission"
Action: "I need to deploy a web application"
Requirements: "to Azure Government with SQL database and blob storage"
Constraints: "must meet FedRAMP High compliance"

Complete Prompt:
"I'm a Navy Commander working on a classified mission. I need to deploy 
a web application to Azure Government with SQL database and blob storage. 
It must meet FedRAMP High compliance requirements."
```

### Progressive Disclosure

Start simple, add details as needed:

**Level 1 - Basic Request**:
```
"I need to deploy a web application"
```

**Level 2 - Add Cloud Context**:
```
"I need to deploy a web application to Azure"
```

**Level 3 - Add Security Requirements**:
```
"I need to deploy a web application to Azure Government with FedRAMP compliance"
```

**Level 4 - Complete Specification**:
```
"I need to deploy a .NET web application to Azure Government using AKS 
with Azure SQL database, Redis cache, and blob storage. Must meet 
FedRAMP High compliance with zero-trust networking."
```

### Key Information to Include

| Category | What to Include | Example |
|----------|----------------|---------|
| **Identity** | Rank, service branch, organization | "CDR Johnson, Navy, NAVWAR" |
| **Mission** | Mission name, purpose, classification | "Secure Comms Platform, CUI" |
| **Cloud** | Provider, environment, region | "Azure Government, US Gov Virginia" |
| **Services** | Compute, database, storage, networking | "AKS, SQL Server, Blob Storage, VNet" |
| **Compliance** | Framework, level | "FedRAMP High (NIST 800-53)" |
| **Scale** | Size, traffic, users | "10 nodes, 50K users, HA required" |

### Understanding Compliance Frameworks

The system uses **NIST 800-53 Rev 5** as the foundation for all compliance assessments:

- **NIST 800-53 Rev 5**: Comprehensive security control catalog (1000+ controls and enhancements)
- **FedRAMP** (Federal Risk and Authorization Management Program):
  - **High**: ~421 NIST controls (high-impact systems, classified data)
  - **Moderate**: ~325 NIST controls (moderate-impact systems)
  - **Low**: ~125 NIST controls (low-impact systems, public data)
- **DoD Impact Levels**: NIST 800-53 controls + DoD-specific requirements
  - IL2-IL6 for different classification levels
- **CMMC**: Derived from NIST 800-171 (which is subset of NIST 800-53)

**Key Point**: When you specify "FedRAMP High," the system assesses your resources against 
the NIST 800-53 controls required by that baseline. All federal compliance frameworks use 
NIST 800-53 as the control catalog.

---

## 💬 Chat Interface Prompts

### 1. Service Onboarding Workflows

#### Basic Onboarding

**Simple Onboarding Request**:
```
"I need to onboard a new mission for NAVWAR"
```

**System Response** (Acknowledges but doesn't ask questions):
```
I'll help you onboard a new mission for NAVWAR. To create a complete onboarding request, I'll need:

- Mission name
- Your rank and service branch
- Contact email
- Type of application/services needed
- Cloud environment (Commercial or Government)
- Classification level
- Expected user count
- Compliance requirements

You can provide these details now, or I can create a draft and you can fill in details later.
```

**Reality**: The system does **NOT** ask follow-up questions interactively. It will tell you what's needed, but you must provide the information in your next message.

**Better Approach - Complete Onboarding in One Prompt**:
```
"I need to onboard a new mission called 'Tactical Edge Platform' for NAVWAR. 
I'm Commander Sarah Johnson from the Navy. We need to deploy a microservices 
architecture with AKS cluster, Azure SQL Database, Redis cache, and blob storage.
My email is sarah.johnson@navy.mil. Classification is CUI. We expect about 
5000 concurrent users and need FedRAMP High compliance. Default VNet is fine."
```

**System Response** (Interactive Multi-turn):
```
System: "I've started your Flankspeed onboarding for 'Tactical Edge Platform'.

📝 Information captured:
✅ Mission: Tactical Edge Platform
✅ Organization: NAVWAR  
✅ Rank: Commander (O-5), Navy
✅ Email: sarah.johnson@navy.mil
✅ Classification: CUI
✅ Services: AKS, Azure SQL, Redis, Blob Storage
✅ Expected users: 5000
✅ VNet: 10.100.0.0/16 (default)

I still need:
- Which Azure region? (Commercial or Government?)
- What's your command/unit?
- Any specific compliance frameworks beyond FedRAMP High?

Please provide these details to complete your onboarding request."
```

**Note**: The system uses **interactive conversation** to gather all required fields. 
It doesn't automatically infer cost estimates or generate infrastructure until all 
information is collected and the request is approved.

#### Government Cloud Onboarding

**Classified Mission**:
```
"I'm deploying a classified intelligence application to Azure Government. 
Mission name is 'Operation Oversight'. I'm a Lieutenant Commander in the 
Navy, assigned to NSA. Requirements:
- Classification: SECRET
- Compliance: FedRAMP High + DoD IL4
- Services: Kubernetes cluster, SQL Server with Always Encrypted, 
  Key Vault with HSM, isolated VNet with no internet access
- Data sovereignty: All resources must stay in US Gov regions
- Multi-region: Primary in Gov Virginia, DR in Gov Texas
- Estimated monthly cost: <$10,000"
```

**Multi-Cloud Deployment**:
```
"I need to deploy the same microservice to both Azure Commercial (dev/staging) 
and Azure Government (production). Service name is 'payment-processor'. 
Requirements:
- Dev: Azure AKS in East US 2, standard tier
- Staging: Azure AKS in West US 2, standard tier
- Production: Azure Government AKS in Gov Virginia, FedRAMP High
- All environments need: PostgreSQL, Redis, Service Bus
- CI/CD: GitHub Actions with approval gates for production
- Cost constraint: Total <$5000/month across all environments"
```

### 2. Military Rank & Identity Prompts

The system understands 80+ military rank variations. You can use any format:

**Rank Variations** (all understood):
```
✅ "I'm CDR Johnson"
✅ "I'm Commander Johnson"
✅ "I'm a Commander (O-5)"
✅ "I'm LCDR Smith"
✅ "I'm Lieutenant Commander"
✅ "I'm Maj Williams" (Air Force)
✅ "I'm MAJ Williams" (Army)
✅ "I'm Captain Johnson" (when service branch specified, resolves ambiguity)
✅ "I'm a Navy O-5"
✅ "I'm an E-7 in the Air Force"
```

**Service Branch Specifications**:
```
Navy: "CDR Johnson, Navy" or "I'm with NAVWAR"
Army: "MAJ Smith, USA" or "I'm from Army Cyber Command"
Air Force: "Maj Williams, USAF" or "I'm stationed at Vandenberg SFB"
Marines: "Capt Brown, USMC" or "I'm with Marine Corps Forces Cyberspace"
Space Force: "Capt Davis, USSF" or "I'm with Space Systems Command"
Coast Guard: "LCDR Taylor, USCG" or "I'm with Coast Guard Cyber Command"
```

### 3. Onboarding Request Specifications

The chat interface is designed for **onboarding workflows**, not direct infrastructure deployment. When you describe infrastructure requirements, the system captures them as part of an onboarding request that goes through approval before template generation.

**What Works - Describing Infrastructure Needs During Onboarding**:
```
"For my mission, we need:
- Kubernetes cluster (AKS) with about 5 nodes
- Azure SQL Database for persistence
- Redis cache for sessions
- Blob storage for documents
- We expect 5000 users
- Classification is CUI
- Need FedRAMP High compliance"
```

**System Response**:
```
"I've captured your infrastructure requirements:
✅ Compute: AKS cluster
✅ Database: Azure SQL
✅ Cache: Redis
✅ Storage: Blob Storage
✅ Scale: 5000 users
✅ Compliance: FedRAMP High

After your request is approved, we'll generate infrastructure templates 
with these services configured for FedRAMP compliance."
```

**What Doesn't Work - Direct Deployment Commands**:
```
❌ "Deploy a Kubernetes cluster with 5 nodes"
❌ "Create an AKS cluster named mission-app-cluster"
❌ "Provision Azure SQL Database with 8 vCores"
```

**Why**: The chat interface is for **onboarding conversations**, not direct Azure API calls. Infrastructure provisioning happens **after** approval through generated templates.

**For Direct Infrastructure Operations**: Use the **Admin Console** or **deployment orchestration** after templates are generated and approved.

### 4. Infrastructure Requirements (For Onboarding)

When onboarding a mission, you can describe your infrastructure needs. The system captures these as **requirements**, not immediate deployments.

#### Compute Requirements

**Describing Container Platform Needs**:
```
"We need a container platform for our microservices:
- Kubernetes (AKS preferred)
- Start with 5 nodes, able to scale up to 10
- Need network policies for security
- Must integrate with Azure AD for authentication"
```

**Describing Web App Needs**:
```
"We're deploying a .NET web application:
- Need App Service with auto-scaling
- 2-5 instances based on load
- Must support deployment slots (dev, staging, prod)
- Need Application Insights for monitoring"
```

#### Database Requirements

**Describing Database Needs**:
```
"For data storage, we need:
- SQL Server with high availability
- Geo-replication for disaster recovery
- Encrypted at rest and in transit
- Must support Always Encrypted for PII data"
```

**Describing NoSQL Needs**:
```
"We need a NoSQL database:
- Cosmos DB for global distribution
- Multi-region replication
- Auto-scaling throughput
- Session consistency is acceptable"
```

#### Storage Requirements

**Describing Storage Needs**:
```
"For file storage:
- Blob storage for documents and media
- Hot tier for active data, cool tier for archives
- Geo-redundant replication
- Private endpoints only, no public access
- Lifecycle policies to move old data to archive"
```

**Note**: These descriptions are captured during onboarding. After approval, the platform generates appropriate infrastructure templates based on your requirements.

#### Networking

**Virtual Network Design**:
```
"Design VNet architecture for 3-tier application:
- VNet CIDR: 10.100.0.0/16 (65,536 IPs)
- Subnets:
  * Frontend subnet: 10.100.1.0/24 (Application Gateway)
  * Application subnet: 10.100.10.0/23 (AKS nodes, 512 IPs)
  * Data subnet: 10.100.20.0/24 (SQL, Redis, private endpoints)
  * Management subnet: 10.100.100.0/24 (Bastion, VPN Gateway)
- NSG rules:
  * Frontend: Allow 443 from internet, deny all inbound
  * Application: Allow 443 from frontend, allow 1433 to data subnet
  * Data: Allow from application subnet only, deny all internet
- Route table: Force tunnel all traffic through Azure Firewall
- DNS: Azure Private DNS zones for privatelink resources
- Peering: Hub VNet for shared services (firewall, DNS, monitoring)"
```

**Zero-Trust Networking**:
```
"Implement zero-trust network for classified application:
- No public IP addresses on any resource
- All ingress through Azure Application Gateway with WAF
- All egress through Azure Firewall with FQDN filtering
- Network segmentation: Micro-segmentation with NSGs per service
- Service-to-service: mTLS with service mesh (Istio)
- Identity-based access: Azure AD workload identity for pods
- Private endpoints: For all Azure PaaS services (SQL, Storage, Key Vault)
- Monitoring: NSG flow logs, firewall logs to Log Analytics
- Compliance: NIST 800-207 Zero Trust Architecture"
```

### 4. Compliance & Security Prompts

#### Compliance Scanning

**Basic Compliance Check - Subscription Scope**:
```
"Check FedRAMP compliance for my production subscription"
```

**Basic Compliance Check - Resource Group Scope**:
```
"Check FedRAMP compliance for resource group 'mission-prod-rg'"
```

**Detailed Compliance Scan - Subscription**:
```
"Run comprehensive compliance scan for subscription '453c2549-4cc5-464f-ba66-acad920823e8'"

OR with more details:

"Check compliance for subscription '453c2549-4cc5-464f-ba66-acad920823e8' against FedRAMP High 
(NIST 800-53 Rev 5 baseline). Show me all findings with severity levels and remediation 
recommendations."
```

**Note**: FedRAMP High is a baseline that uses a specific subset of **NIST 800-53 Rev 5** controls. 
The system assesses your resources against NIST 800-53 controls and determines FedRAMP compliance 
by checking if the required controls are satisfied. The scan produces a detailed assessment report. 
To actually execute remediation, use a separate command like "generate remediation plan" or 
"execute remediation for [finding-id]" after reviewing the assessment.

**Detailed Compliance Scan - Resource Group**:
```
"Check compliance for resource group 'mission-prod-rg' in subscription '453c2549-4cc5-464f-ba66-acad920823e8' 
against FedRAMP High (NIST 800-53 baseline). Show all findings with remediation guidance."
```

**Understanding Compliance Frameworks**:
- **NIST 800-53 Rev 5**: Comprehensive security control catalog (1000+ controls)
- **FedRAMP High**: Requires ~421 specific NIST 800-53 controls + enhancements
- **FedRAMP Moderate**: Requires ~325 NIST 800-53 controls
- **FedRAMP Low**: Requires ~125 NIST 800-53 controls

The system assesses against NIST 800-53 and maps results to FedRAMP baselines.

**Remediation Workflow**:
```
Step 1: "Run compliance assessment for subscription [id]"
        → Review the assessment report

Step 2: "Generate remediation plan for the assessment"
        → Get prioritized remediation steps

Step 3: "Execute remediation for finding [finding-id]" (optional: add "dry-run mode")
        → Apply automated fixes for specific findings
```

**Multi-Framework Compliance**:
```
"Validate compliance against multiple frameworks:
- FedRAMP High (NIST 800-53 Rev 5 baseline - ~421 controls)
- NIST 800-53 Rev 5 (complete catalog assessment)
- ISO 27001 (information security management)
- SOC 2 Type II (trust service criteria)
- Scope: Entire Azure Government subscription
- Priority: Identify gaps blocking ATO approval
- Timeline: Need ATO within 60 days, prioritize critical gaps"
```

**Note**: All federal compliance frameworks (FedRAMP, DoD IL levels, CMMC) are based on 
NIST 800-53 controls. The system uses NIST 800-53 as the foundation and can report 
compliance for any framework that uses these controls.

#### Security Hardening

**Security Baseline**:
```
"Apply security hardening to all resources in subscription:
- Encryption: Encrypt all data at rest with customer-managed keys
- Networking: Disable public access, enable private endpoints
- Authentication: Enforce Azure AD, disable local auth
- MFA: Require MFA for all admin access
- RBAC: Least privilege, no Owner role assignments to users
- Logging: Enable diagnostic settings on all resources
- Monitoring: Azure Defender for Cloud (all plans)
- Secrets: No connection strings in app config, use Key Vault references
- Certificates: Managed certificates with auto-renewal
- Vulnerabilities: Enable Defender for containers, SQL, storage"
```

**Incident Response**:
```
"Set up security incident response:
- SIEM: Microsoft Sentinel workspace
- Data sources: Azure AD logs, activity logs, NSG flow logs, WAF logs
- Analytics rules: Detect suspicious logins, privilege escalation, 
  data exfiltration, crypto-mining, lateral movement
- Automation: Auto-block suspicious IPs, disable compromised accounts, 
  isolate infected VMs
- Notifications: Email security team, create PagerDuty incident
- Playbooks: Auto-response for common incident types
- Retention: 2 years (compliance requirement)"
```

### 5. Cost Optimization Prompts

**Cost Analysis**:
```
Show comprehensive cost analysis for subscription 453c2549-4cc5-464f-ba66-acad920823e8 for the last 3 months with breakdowns by resource group, type, location, and tags
```

> **💡 Tip**: The cost analysis uses keyword detection. Avoid words like "budget" or "alert" in your main query if you want comprehensive analysis. Use those keywords only when specifically requesting budget monitoring.

**Budget Monitoring** (use these keywords to get budget-specific view):
```
Show budget status and alerts for subscription 453c2549-4cc5-464f-ba66-acad920823e8
```


**Optimization Recommendations**:
```
"Provide cost optimization recommendations for production environment:
- subscription 453c2549-4cc5-464f-ba66-acad920823e8
- Right-sizing: Identify oversized VMs and databases
- Reserved instances: Analyze usage for RI/savings plan opportunities
- Unused resources: Find idle resources (stopped VMs, unattached disks, 
  orphaned NICs, old snapshots)
- Storage tiering: Move infrequently accessed data to cool/archive
- Auto-shutdown: Identify non-production resources for scheduled shutdown
- Licensing: Optimize SQL licensing with Azure Hybrid Benefit
- Expected savings: Target 30-40% cost reduction
- Implementation: Prioritize quick wins, then long-term optimizations"
```

---

## 🔧 VS Code Extension Prompts

### Using the @platform Chat Participant

The VS Code extension provides the `@platform` chat participant for infrastructure operations directly in your IDE.

### 1. Infrastructure Provisioning

**Quick Resource Creation**:
```
@platform create a storage account named "myappstorage" in resource group "test-rg"
```

**Complex Multi-Resource Deployment**:
```
provision complete infrastructure for microservice "order-processor":
- Resource group: orders-prod-rg in East US 2
- AKS cluster: 5 nodes, Standard_D4s_v3
- Azure SQL: Business Critical, 4 vCores, geo-replication
- Redis Cache: Premium P1, 6GB
- Storage: Premium blob storage with lifecycle policies
- Service Bus: Premium tier with topics for event-driven architecture
- Application Gateway: WAF v2 with custom domain
- Key Vault: Premium with HSM for sensitive keys
- Generate: Terraform templates, Kubernetes manifests, CI/CD pipelines
```

### 2. Template Generation

**Generate Bicep Template**:
```
generate Bicep template for serverless architecture:
- Container Apps environment with 3 microservices
- Cosmos DB with SQL API
- Service Bus for async messaging
- Application Insights for monitoring
- Managed Identity for all services
- Include: Variables file, parameters, outputs
```

**Generate Terraform Template**:
```
create Terraform module for AWS EKS:
- Cluster version: 1.27
- Node groups: 2 (system nodes and application nodes)
- Networking: VPC with public and private subnets
- Add-ons: AWS Load Balancer Controller, EBS CSI driver, Cluster Autoscaler
- Security: IRSA (IAM Roles for Service Accounts)
- Include: Backend config for S3, variables.tf, outputs.tf
```

### 3. Security Scanning

**Container Security Scan**:
```
scan container image "myregistry.azurecr.io/webapp:v2.1.0" for:
- Vulnerabilities: CVEs in base image and dependencies
- Secrets: Hardcoded passwords, API keys, tokens
- Best practices: Dockerfile optimization, non-root user, minimal layers
- Compliance: CIS Docker Benchmark
- Tools: Trivy, Grype, Dockle
- Output: Detailed report with severity levels and remediation steps
```

**Code Security Scan**:
```
run security scan on current workspace:
- SAST: Static analysis for code vulnerabilities (SQL injection, XSS, etc.)
- Dependency scan: Known vulnerabilities in NuGet/npm packages
- Secret detection: Scan for accidentally committed secrets
- License compliance: Check for GPL or other restrictive licenses
- Output: Create GitHub issue for each critical/high vulnerability
```

### 4. Deployment Operations

**Deploy to Kubernetes**:
```
deploy application "payment-api" version v2.1.0 to production:
- Cluster: production-aks-eastus2
- Namespace: payments
- Strategy: Rolling update with 25% max surge, 0% max unavailable
- Health checks: /health (liveness), /ready (readiness)
- Resources: 500m CPU, 1Gi memory (requests), 1000m CPU, 2Gi memory (limits)
- Replicas: 5 (HPA: min 5, max 20, target 70% CPU)
- ConfigMap: payment-config from Key Vault
- Secrets: Database connection string, Stripe API key
- Monitoring: Send deployment event to Application Insights
```

**Blue-Green Deployment**:
```
execute blue-green deployment for "customer-portal":
- Current version (green): v1.5.2 (receiving 100% traffic)
- New version (blue): v1.6.0 (deploy but no traffic)
- Validation: Run smoke tests, check error rate <1%
- Traffic shift: If validation passes, shift 10% → 50% → 100% over 30 minutes
- Rollback triggers: Error rate >2%, response time >500ms p95, or manual command
- Keep green: Maintain for 1 hour after 100% cutover, then tear down
```

### 5. Monitoring & Observability

**Create Dashboard**:
```
create monitoring dashboard for "e-commerce-platform":
- Metrics:
  * Application: Request rate, error rate, response time (p50, p95, p99)
  * Infrastructure: CPU, memory, disk, network per service
  * Database: DTU utilization, query performance, deadlocks
  * Business: Orders/minute, revenue/hour, cart abandonment rate
- Time range: Last 24 hours with auto-refresh every 30 seconds
- Alerts: Visual indicators when metrics exceed thresholds
- Drill-down: Click to view logs and traces for specific time range
- Share: Generate public link for stakeholders
```

**Set Up Alerting**:
```
configure alerts for production environment:
- Critical (PagerDuty):
  * Application error rate >5% for 5 minutes
  * Database DTU >90% for 10 minutes
  * Any pod in CrashLoopBackOff state
  * SSL certificate expires in <7 days
- Warning (Email):
  * Response time p95 >1000ms for 15 minutes
  * CPU >80% for 20 minutes
  * Disk usage >85%
- Informational (Slack):
  * Deployment started/completed
  * Auto-scaling events
  * Certificate rotated
```

### 6. Documentation Generation

**Generate README**:
```
@platform create comprehensive README for current repository:
- Project overview: Parse csproj/package.json for description
- Architecture: Detect services and generate architecture diagram
- Prerequisites: .NET version, Node.js, Docker, Azure resources
- Local development: Step-by-step setup instructions
- Environment variables: List all required env vars with examples
- Deployment: Azure deployment guide with CLI commands
- Testing: How to run unit tests, integration tests
- Troubleshooting: Common issues and solutions
- Contributing: How to contribute (if CONTRIBUTING.md exists)
```

---

## 📊 Admin Console Operations

### Template Management

**Browse Templates**:
```
Filter: Cloud=Azure, Service=Kubernetes, Compliance=FedRAMP
Sort: Most recent
```

**Create Custom Template**:
```
Template Name: "Secure Microservice Baseline"
Description: "FedRAMP High compliant microservice template with zero-trust networking"
Cloud: Azure Government
Services: AKS, SQL Database, Key Vault, Application Gateway
Compliance Level: FedRAMP High
Tags: microservices, zero-trust, production-ready
Files: [Upload or generate 35 files]
```

**Template Search Examples**:
```
"kubernetes azure sql fedramp"
→ Finds: Templates with AKS + SQL + FedRAMP compliance

"serverless aws lambda python"
→ Finds: AWS Lambda templates with Python runtime

"machine learning gpu training"
→ Finds: Templates with GPU-enabled compute for ML training
```

### Approval Workflows

**Approve Onboarding Request**:
```
Review Request: "Mission App Deployment"
Submitted by: CDR Johnson (Navy)
Classification: CUI
Resources: AKS (10 nodes), SQL (8 vCores), Storage (1TB)
Estimated cost: $4,200/month
Compliance: FedRAMP High (98% score)

Action: ✅ Approve with conditions:
- Limit AKS nodes to 8 (cost savings)
- Require quarterly compliance scans
- Set budget alert at $4,000/month

Comments: "Approved for production deployment. Ensure MFA is enforced 
for all admin access. Schedule security review in 90 days."
```

**Reject with Feedback**:
```
Action: ❌ Reject
Reason: Compliance gaps
Details:
- 5 critical FedRAMP controls not implemented (AC-2, IA-5, SC-7)
- Public blob access enabled (security risk)
- No encryption for data at rest configured

Required actions before resubmission:
1. Implement all critical controls
2. Disable public access on all storage accounts
3. Enable customer-managed encryption keys
4. Schedule security review with InfoSec team

Resubmit after: Remediation complete and validated
```

### Azure Policy Evaluation & Governance

**Understanding Policy-Aware Infrastructure**:

The Platform Engineering Copilot now integrates with Azure Policy Insights API to provide real-time policy evaluation and approval workflows for infrastructure changes. This ensures compliance before deployment.

**How It Works**:
1. **Pre-Deployment Validation**: Before deploying infrastructure, the system evaluates proposed resources against active Azure Policies
2. **Approval Workflows**: Policy violations trigger approval workflows that persist to database
3. **Real-time Compliance**: Direct integration with Azure Policy Insights API (no mock data)
4. **Severity-Based Decisions**: Critical violations block deployment; warnings allow conditional approval

**Check Policy Compliance Before Deployment**:
```
check Azure policies for my proposed deployment:
- Resource Group: "rg-ml-sbx-jrs"
- Subscription: "453c2549-4cc5-464f-ba66-acad920823e8"
- Location: "usgovvirginia"
- Resources: App Service, Web Site
- Show: All policy violations with severity and recommendations
```

**Important: Subscription Identification**
- **Use Subscription ID (preferred)**: `"453c2549-4cc5-464f-ba66-acad920823e8"`
- **Or Subscription Name**: `"production-sub"` (if it exists in your Azure environment)
- **Error Handling**: If subscription is not found, you'll receive:
  ```
  ❌ Subscription with name 'production-sub' not found
  
  Please use one of these methods:
  1. Find your subscription ID in Azure Portal → Subscriptions
  2. Use Azure CLI: az account list --query "[].{name:name, id:id}" -o table
  3. Check with admin if you don't have access to the subscription
  ```

**Evaluate Specific Resource Type**:
```
evaluate policies for Azure Kubernetes Service:
- Location: "usgovvirginia"
- Configuration:
  * Node count: 5
  * VM size: Standard_D4s_v3
  * Network plugin: Azure CNI
  * Network policy: Calico
- Check against: All production policies
- Report: Compliant/Non-compliant with reasons
```

**Request Approval for Policy Violations**:
```
I need approval to deploy storage account with these policy exceptions:
- Violation: "Public network access disabled" policy
- Justification: "Temporary public access needed for data migration from on-prem"
- Duration: 7 days (March 1-7, 2025)
- Mitigations:
  * IP whitelist restricted to corporate VPN only
  * All access logged to Log Analytics
  * MFA required for all admin access
- Rollback plan: Disable public access after migration complete
```

**Batch Policy Evaluation**:
```
@platform scan all resources in subscription "prod-sub" for policy compliance:
- Framework: FedRAMP High baseline
- Severity filter: Critical and High only
- Output format: Grouped by policy definition
- Show remediation commands for each violation
```

**Policy Workflow Status**:
```
@platform show status of my approval workflows:
- Filter: Pending approvals
- Submitted by: me
- Last 30 days
- Include: Policy violations, justifications, approver comments
```

**Auto-Remediate Policy Violations** (with caution):
```
@platform remediate non-compliant resources in "dev-rg":
- Policy: "Require encryption at rest"
- Resources: Storage accounts without encryption
- Action: Enable default encryption with platform-managed keys
- Dry run: Yes (show changes before applying)
- Approval required: Yes
- Notification: Email me when complete
```

**Policy Violation Severities**:
- **Critical** (Deny effect): Blocks deployment immediately - requires exception approval
- **High** (Audit with enforcement): Deployment allowed but requires justification
- **Medium** (Audit): Logged for review, no blocking
- **Low** (Informational): Best practice recommendations

**Database-Backed Approval Workflows**:

All approval workflows now persist to database (`ApprovalWorkflows` table) with:
- **Persistent Storage**: Workflows survive application restarts
- **Audit Trail**: Complete history of approvals, rejections, and justifications
- **Query Performance**: 8 indexes for fast filtering by status, environment, resource type
- **Concurrent Support**: Multiple users can request/approve workflows simultaneously

**Example Approval Workflow Data**:
```
Workflow ID: "wf-2025-10-17-abc123"
Status: Pending Approval
Requested by: CDR Johnson
Resource: Storage Account "datamigration-store"
Policy Violation: "Require private endpoints only"
Justification: "Temporary public access for 7-day migration window"
Required Approvers: ["security-team@navy.mil", "compliance-lead@navy.mil"]
Expires: 2025-10-24 (7 days)
Priority: High
Created: 2025-10-17 10:30 AM
```

**Query Your Workflows**:
```
@platform show my workflows:
- Status: All (pending, approved, rejected)
- Date range: Last 90 days
- Sort by: Created date descending
- Include: Policy violations, approver comments, current status
```

**Technical Integration Details**:

The Azure Policy Engine now provides:
- ✅ **Real Azure Policy API**: Direct calls to `management.azure.com/policyStates` 
- ✅ **Bearer Token Auth**: Uses `DefaultAzureCredential` for secure authentication
- ✅ **5-Minute Cache**: Performance optimization with in-memory caching
- ✅ **Severity Mapping**: Automatic severity assignment based on policy effect (Deny→Critical, Audit→Low)
- ✅ **Database Persistence**: All workflows stored in SQLite with EF Core
- ✅ **Scoped Service Lifetime**: Proper DI registration for DbContext compatibility

**Best Practices for Policy Approvals**:

1. **Be Specific**: Clearly state the resource, policy, and business justification
2. **Time-Bounded**: Always specify duration for exceptions (temporary access)
3. **Mitigation Plan**: Explain what controls are in place during the exception
4. **Rollback Ready**: Describe how to revert changes after the exception expires
5. **Document Everything**: All workflows persist to database for compliance audits

**Policy Evaluation Prompts**:
```
# Check single resource
@platform check policies for VM "web-server-01" in "prod-rg"

# Check resource group
@platform evaluate all policies for resource group "staging-rg"

# Check subscription
@platform scan subscription "dev-sub" for policy violations

# Check before deployment
@platform will this deployment violate any policies?
- Resources: [list resources]
- Location: [region]
- Configuration: [key settings]
```

---

## 🎯 Advanced Techniques

### 1. Context Chaining

Build complex requirements through conversation **within a single session**:

> **⚠️ Current Limitations**: 
> - Conversation context is maintained **in-memory only** during an active session
> - The system does **NOT** proactively ask follow-up questions - you must provide all information
> - The AI will work with whatever information you provide, inferring what it can
> - For best results, provide complete details in your initial prompt or follow-up messages

**How It Actually Works**:

**Turn 1** (You provide partial info):
```
"I need to deploy a microservices application"
```

**System Response** (NO follow-up questions):
```
"I can help you deploy a microservices application. I'll need more details to provide specific guidance.
Please tell me:
- Cloud provider (Azure, AWS, GCP)
- Number of services
- Infrastructure requirements

Or I can provide general guidance based on best practices."
```

**Turn 2** (You provide more details):
```
"Azure Government for production, 5 services: API gateway, auth service, order service, payment service, notification service"
```

**System Response**:
```
"I'll help you design a 5-service microservices architecture on Azure Government.
[Generates architecture recommendations]"
```

**Reality Check**:
- ❌ **System does NOT ask**: "What cloud provider would you like to use?"
- ❌ **System does NOT ask**: "How many microservices will you deploy?"
- ✅ **System DOES**: Acknowledge what's missing and work with what you provided
- ✅ **System DOES**: Remember previous messages in the conversation (last 10 messages)
- ✅ **Context is maintained** for the duration of your session (stored in memory)
- ❌ **Context is NOT persisted** to database (lost on app restart)

**Best Practice**: Provide complete information upfront rather than expecting the system to guide you through a questionnaire.

### 2. Referencing Previous Context

The system remembers conversation history **within the same session**:

> **⚠️ Session-Based Memory**: Context is maintained in-memory during an active conversation. If you close your browser or the application restarts, context is lost. For best results, complete related tasks in a single conversation session.

```
User: "Deploy web app to Azure"
System: [Generates templates]

User: "Now add a database to that deployment"
System: [Updates templates to include Azure SQL]

User: "Make the database geo-replicated"
System: [Updates database config with geo-replication]

User: "What's the monthly cost for everything we've configured?"
System: "Estimated monthly cost: $1,850 (App Service $150, SQL Database $1,400, 
Storage $100, Networking $200)"
```

**Context Window**: The system uses the **last 10 messages** to build context for each response. For complex multi-step workflows, consider consolidating related requests into fewer, more detailed prompts rather than spreading them across many small messages.

### 3. Template Inheritance

Build on existing templates:

```
"Use the 'Secure Microservice Baseline' template but:
- Change from Azure to AWS
- Replace AKS with EKS
- Use RDS PostgreSQL instead of Azure SQL
- Add CloudFront CDN
- Keep all FedRAMP compliance controls"
```

### 4. Batch Operations

Execute multiple operations in one prompt:

```
"Perform the following operations:
1. Create storage account 'data002' in rg-dr in subscription 453c2549-4cc5-464f-ba66-acad920823e8
3. Set up geo-replication between data001 and data002
4. Apply lifecycle policy: hot→cool after 30 days, cool→archive after 90 days
5. Enable soft delete with 30-day retention
6. Create managed identity 'app-identity' with read access to both storage accounts
7. Generate Bicep template for all resources
8. Create GitHub Action workflow to deploy the template"
```

### 5. Conditional Logic

Express complex requirements with conditions:

```
"Deploy application infrastructure with these rules:
- IF environment=production THEN:
  * Use Premium tier for all services
  * Enable geo-replication
  * Require manual approval for deployments
  * Set budget alert at $5000/month
- ELSE IF environment=staging THEN:
  * Use Standard tier
  * No geo-replication
  * Auto-deploy on merge to main
  * Set budget alert at $1000/month
- ELSE (development):
  * Use Basic tier where available
  * Single instance, no HA
  * Auto-deploy on any commit
  * Auto-shutdown at 8 PM weekdays, all day weekends
  * Set budget alert at $500/month"
```

---

## ✅ Best Practices

### 1. Provide Complete Information Upfront

❌ **Bad** (Too vague, expecting system to ask questions):
```
"Deploy my app"
```

**Reality**: The system will NOT ask clarifying questions. It will work with what you give it or provide generic guidance.

✅ **Good** (Complete information in initial prompt):
```
"I need to deploy a .NET 8 web API to Azure Government with SQL Server database 
using geo-replication. This is for a classified mission with FedRAMP High compliance 
requirements. Expected load: 10,000 concurrent users, 99.9% uptime SLA."
```

✅ **Also Acceptable** (Progressive detail in follow-up messages):
```
Turn 1: "I need to deploy a web application"
Turn 2: "It's a .NET 8 API for Azure Government with SQL Server and geo-replication"
Turn 3: "Classification is SECRET, need FedRAMP High, 10K concurrent users"
```

**Key Point**: Don't expect the system to guide you through questions. Provide details proactively.

### 2. Include Classification Early

❌ **Bad** (Security as afterthought):
```
"Deploy app, oh and it's classified SECRET"
```

✅ **Good** (Security first):
```
"I need to deploy a SECRET classified application to Azure Government..."
```

### 3. Specify Compliance Requirements

❌ **Bad**:
```
"Make it secure"
```

✅ **Good**:
```
"Must meet FedRAMP High compliance (NIST 800-53 Rev 5 baseline)"
```

**Compliance Framework Options**:
- FedRAMP High: ~421 NIST 800-53 controls (high-impact systems)
- FedRAMP Moderate: ~325 NIST 800-53 controls (moderate-impact systems)  
- DoD IL4/IL5: NIST 800-53 + DoD-specific requirements
- CMMC Level 3: NIST 800-171 derived from NIST 800-53

### 4. Provide Cost Constraints

❌ **Bad**:
```
"Deploy the biggest cluster possible"
```

✅ **Good**:
```
"Deploy AKS cluster with monthly cost <$3000, optimizing for cost over performance"
```

### 5. Be Specific About Scale

❌ **Bad**:
```
"It needs to handle lots of users"
```

✅ **Good**:
```
"Expected: 50,000 concurrent users, 100M requests/day, 99.9% uptime SLA"
```

### 6. Mention Time Constraints

❌ **Bad**:
```
"Deploy when you can"
```

✅ **Good**:
```
"Need production deployment by end of quarter (60 days) for ATO deadline"
```

---

## 🐛 Troubleshooting Prompts

### Azure Subscription & Authentication Errors

**Subscription Not Found**:
```
❌ Error: "Subscription with name 'production-sub' not found"

✅ Solutions:
1. Use subscription ID instead of name:
   @platform check policies for subscription "453c2549-4cc5-464f-ba66-acad920823e8"

2. List your subscriptions:
   az account list --query "[].{name:name, id:id}" -o table

3. Verify you have access:
   az account show

4. Check if logged in:
   az login --use-device-code  # For Azure Government
   az login  # For Azure Commercial
```

**Authentication Failures**:
```
❌ Error: "DefaultAzureCredential failed to retrieve token"

✅ Solutions:
1. Check Azure CLI login:
   az account show
   az login --use-device-code

2. Verify correct cloud:
   az cloud set --name AzureUSGovernment  # For Gov
   az cloud set --name AzureCloud  # For Commercial

3. Check permissions:
   - Reader role required at minimum
   - Policy Reader role for policy operations
   - Contact Azure admin if access denied

4. Verify service principal (if used):
   - AZURE_TENANT_ID set correctly
   - AZURE_CLIENT_ID set correctly
   - AZURE_CLIENT_SECRET not expired
```

**Resource Group Not Found**:
```
❌ Error: "Resource group 'prod-rg' not found in subscription"

✅ Solutions:
1. List resource groups:
   az group list --subscription "YOUR-SUB-ID" -o table

2. Verify subscription context:
   az account show
   
3. Use correct subscription:
   @platform check policies for resource group "prod-rg" 
   in subscription "453c2549-4cc5-464f-ba66-acad920823e8"

4. Check permissions:
   az role assignment list --scope "/subscriptions/YOUR-SUB-ID"
```

**Insufficient Permissions**:
```
❌ Error: "The client does not have authorization to perform action"

✅ Required Azure Roles:
- Policy Operations: "Resource Policy Contributor" or "Policy Insights Data Writer"
- Read Operations: "Reader" role at subscription/resource group level
- Compliance Scanning: "Reader" + "Security Reader"
- Cost Analysis: "Cost Management Reader"

Contact your Azure administrator to request appropriate roles.
```

### Debugging Failed Deployments

**Get Deployment Logs**:
```
@platform show deployment logs for "payment-api-v2" that failed 15 minutes ago
```

**Analyze Failure**:
```
@platform diagnose why deployment "customer-portal-prod-20250109" failed:
- Show error messages
- Check resource quota limits
- Verify Azure permissions
- Review template validation errors
- Suggest remediation steps
```

### Performance Issues

**Diagnose Slow Performance**:
```
@platform investigate performance issues in production:
- Application: customer-portal
- Symptom: Response times >3s (normally <200ms)
- Started: 2 hours ago
- Check: Database queries, API dependencies, cache hit rates, 
  resource utilization, network latency
- Provide: Root cause analysis and recommended fixes
```

### Cost Surprises

**Unexpected Costs**:
```
@platform analyze why Azure costs increased 150% this month:
- Previous month: $5,000
- Current month: $12,500
- Show: Top 10 cost increases by resource
- Identify: New resources, size changes, increased usage
- Recommend: How to reduce back to $5,000-$6,000
```

### Compliance Failures

**Fix Compliance Issues**:
```
@platform auto-remediate compliance failures in resource group "prod-rg":
- Framework: FedRAMP High (NIST 800-53 baseline)
- Severity: Critical and High only (don't touch Low/Medium)
- Dry run first: Show what will be changed
- After approval: Execute remediation
- Report: Before/after compliance scores
```

**Available Compliance Frameworks**:
```
- NIST 800-53 Rev 5 (foundation - 1000+ controls)
- FedRAMP High (~421 controls)
- FedRAMP Moderate (~325 controls)
- FedRAMP Low (~125 controls)
- DoD IL2-IL6 (NIST 800-53 + DoD requirements)
- CMMC (derived from NIST 800-171/800-53)
```

---

## 📝 Prompt Templates Library

### Copy-Paste Templates

#### New Service Deployment
```
I need to deploy [SERVICE_TYPE] named "[SERVICE_NAME]" for [ORGANIZATION].
I'm [RANK] [NAME] from [BRANCH].

Requirements:
- Cloud: [Azure Government | Azure Commercial | AWS | GCP]
- Region: [REGION]
- Classification: [UNCLASS | CUI | SECRET]
- Compliance: [FedRAMP High | FedRAMP Moderate | NIST 800-53]
- Services needed:
  * Compute: [AKS | EKS | App Service | Lambda | etc.]
  * Database: [SQL | PostgreSQL | Cosmos DB | DynamoDB]
  * Storage: [Blob | S3 | Cloud Storage]
  * Other: [List any additional services]
- Scale:
  * Users: [NUMBER] concurrent
  * Requests: [NUMBER] per day
  * Data: [SIZE] total
- Budget: $[AMOUNT] per month
- Timeline: Deploy by [DATE]
```

#### Security Hardening
```
Apply security hardening to [SCOPE] with:
- Encryption: [Customer-managed keys | Platform-managed | Both]
- Networking: [Private endpoints | VNet integration | Public with restrictions]
- Authentication: [Azure AD only | Managed Identity | Service Principal]
- Compliance: [FedRAMP | NIST | ISO 27001]
- Monitoring: [Enable all security logs | Basic | Custom]
- Remediation: [Auto-fix | Report only | Manual approval]
```

#### Cost Optimization
```
Optimize costs for [SCOPE]:
- Current spend: $[AMOUNT]/month
- Target: $[AMOUNT]/month ([PERCENTAGE]% reduction)
- Priorities: [Performance | Availability | Cost] (rank in order)
- Constraints: [Must maintain HA | Can't reduce security | etc.]
- Implementation: [Immediate | Phased over X weeks]
```

---

## 🎓 Learning Path

### Beginner (Week 1)
- ✅ Deploy first service using simple prompt
- ✅ Use chat interface for onboarding
- ✅ Review generated templates
- ✅ Deploy to development environment

### Intermediate (Week 2-3)
- ✅ Use VS Code extension @platform
- ✅ Multi-service deployments
- ✅ Add compliance requirements
- ✅ Cost optimization prompts

### Advanced (Week 4+)
- ✅ Context chaining for complex architectures
- ✅ Template inheritance and customization
- ✅ Batch operations
- ✅ Advanced security hardening

---

## 📞 Getting Help

### Common Error Messages & Solutions

**Quick Error Reference**:

| Error Message | Cause | Solution |
|--------------|-------|----------|
| `Subscription with name 'X' not found` | Invalid subscription name | Use subscription ID or verify name with `az account list` |
| `DefaultAzureCredential failed` | Not authenticated | Run `az login` or `az login --use-device-code` |
| `Resource group 'X' not found` | Invalid resource group | Verify with `az group list --subscription "ID"` |
| `Client does not have authorization` | Insufficient permissions | Request Reader + Policy Reader roles from admin |
| `Policy not found` | Policy doesn't exist in subscription | Check policy assignments with `az policy assignment list` |
| `The resource type 'X' is not supported` | Unsupported resource type | Check Azure Policy supports this resource type |

### Getting Additional Support

If prompts aren't working as expected:

1. **Check syntax**: Review examples in this guide
2. **Add more context**: More details = better results
3. **Use progressive disclosure**: Build up complexity gradually
4. **Check logs**: View → Output → "Platform Engineering MCP"
5. **Verify Azure connectivity**: Run `az account show` to confirm authentication
6. **Check permissions**: Ensure you have required Azure roles (Reader, Policy Reader, etc.)
7. **File issue**: Include prompt, error message, and expected vs actual behavior

### Debug Mode

Enable detailed logging for troubleshooting:

```
@platform enable debug logging for next request

@platform check policies for subscription "YOUR-SUB-ID" with verbose output
```

This will show:
- API calls being made
- Authentication details
- Response payloads
- Performance metrics
- Error stack traces

---

**Pro Tip**: The AI learns from feedback. If a prompt doesn't work well, try rephrasing with more specific details. The system improves with clearer inputs!

**Azure Tip**: Always verify your Azure authentication and permissions before troubleshooting complex issues. Run `az account show` and `az role assignment list` to confirm your access.

---

*Last Updated: October 17, 2025*
