# Environment Agent Test Suite

**Last Updated:** November 13, 2025  
**Agent:** Environment  
**Plugin Functions:** 9 total  
**Purpose:** Comprehensive testing of all Environment Agent capabilities

## 📋 Overview

The Environment Agent handles environment lifecycle management, cloning, scaling, validation, and best practices guidance with Azure MCP integration.

**Key Capabilities:**
- **Environment Creation**: Dev, staging, production environment setup
- **Environment Cloning**: Replicate environments for testing
- **Environment Scaling**: Scale resources up/down
- **Environment Validation**: Configuration compliance and best practices checks
- **Environment Management**: List, status, delete operations
- **Tagging & Naming**: Best practices for organization
- **AKS Optimization**: AKS-specific best practices

## 🎯 Quick Test Commands

```bash
# Test environment creation
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Create a production environment for webapp-api"}' | jq .
```

## 🧪 Test Cases by Function Category

### 1️⃣ Environment Lifecycle Functions (4 functions)

#### Test ENV-1.1: Create Environment
```
Create a production environment for webapp-api in usgovvirginia
```
**Expected Function:** `create_environment`  
**Expected Output:**
- ✅ Environment created
- ✅ Resource group created
- ✅ Tags applied (Environment, Owner, etc.)
- ✅ Naming conventions followed
- ✅ Configuration summary
- ✅ Next steps guidance

**Validation:**
- ✅ Agent: Environment ONLY
- ✅ Intent: "environment"
- ⏱️ Time: 30-60 seconds
- ✅ Real Azure environment created
- ⚠️ **WARNING:** Creates real resources

---

#### Test ENV-1.2: Clone Environment
```
Clone my production environment to create a staging environment
```
**Expected Function:** `clone_environment`  
**Expected Output:**
- ✅ Cloning process initiated
- ✅ Source environment analyzed
- ✅ Target environment created
- ✅ Resources replicated (with appropriate modifications)
- ✅ Configuration differences noted
- ✅ Clone completion status

**Validation:**
- ✅ Source environment preserved
- ✅ Target environment matches source (with env-specific changes)
- ⏱️ Time: 60-180 seconds
- ⚠️ **WARNING:** Creates real resources

---

#### Test ENV-1.3: Delete Environment
```
Delete the development environment dev-webapp-api
```
**Expected Function:** `delete_environment`  
**Expected Output:**
- ✅ Confirmation prompt (if enabled)
- ✅ Resources to be deleted listed
- ✅ Deletion process initiated
- ✅ Cleanup completion status
- ✅ Cost impact noted

**Validation:**
- ✅ Environment deleted from Azure
- ⏱️ Time: 60-120 seconds
- ⚠️ **DESTRUCTIVE** operation

---

#### Test ENV-1.4: List Environments
```
Show me all my environments
```
**Expected Function:** `list_environments`  
**Expected Output:**
- ✅ All environments listed
- ✅ Environment types (dev, staging, production)
- ✅ Resource counts per environment
- ✅ Health status per environment
- ✅ Cost summary per environment

**Validation:**
- ✅ Complete environment inventory
- ✅ Accurate status information
- ⏱️ Time: 10-20 seconds

---

### 2️⃣ Environment Status & Scaling (2 functions)

#### Test ENV-2.1: Get Environment Status
```
What's the status of my production environment?
```
**Expected Function:** `get_environment_status`  
**Expected Output:**
- ✅ Overall environment health
- ✅ Resource status summary
- ✅ Configuration compliance
- ✅ Recent changes/deployments
- ✅ Issues and recommendations

**Validation:**
- ✅ Accurate status information
- ✅ Real-time data from Azure
- ⏱️ Time: 10-20 seconds

---

#### Test ENV-2.2: Scale Environment
```
Scale up my staging environment to handle more traffic
```
**Expected Function:** `scale_environment`  
**Expected Output:**
- ✅ Current scaling configuration
- ✅ Proposed scaling changes
- ✅ Scaling process initiated
- ✅ Resource adjustments made
- ✅ Cost impact estimation
- ✅ Completion status

**Validation:**
- ✅ Resources scaled appropriately
- ✅ Cost implications shown
- ⏱️ Time: 30-90 seconds
- ⚠️ **WARNING:** Modifies resource sizes

---

### 3️⃣ Validation & Best Practices (3 functions)

#### Test ENV-3.1: Validate Environment Configuration (Basic)
```
Validate my development environment configuration
```
**Expected Function:** `validate_environment_configuration` (basic level)  
**Expected Output:**
- ✅ Basic validation checks (6 total):
  - Naming conventions ✓/✗
  - Required tags ✓/✗
  - RBAC configuration ✓/✗
  - Basic networking ✓/✗
  - Security baseline ✓/✗
  - Monitoring enabled ✓/✗
- ✅ Overall: Passed/Failed
- ✅ Critical issues: Count
- ✅ Warnings: Count
- ✅ Recommendations

**Validation:**
- ✅ Basic validation only
- ✅ Quick check (< 30 seconds)
- ⏱️ Time: 15-30 seconds

---

#### Test ENV-3.2: Validate Environment Configuration (Standard)
```
Run standard validation on my staging environment
```
**Expected Function:** `validate_environment_configuration` (standard level)  
**Expected Output:**
- ✅ Standard validation checks (12 total = Basic + 6 additional):
  - All basic checks
  - Backup configuration ✓/✗
  - Disaster recovery ✓/✗
  - Cost management tags ✓/✗
  - Security hardening ✓/✗
  - Performance monitoring ✓/✗
  - Compliance basics ✓/✗
- ✅ Overall: Passed with warnings/Failed
- ✅ Remediation scripts provided
- ✅ Prioritized next steps

**Validation:**
- ✅ More thorough than basic
- ⏱️ Time: 30-60 seconds

---

#### Test ENV-3.3: Validate Environment Configuration (Comprehensive)
```
Run comprehensive validation on my production environment
```
**Expected Function:** `validate_environment_configuration` (comprehensive level)  
**Expected Output:**
- ✅ Comprehensive validation checks (16+ total = Standard + 4+ additional):
  - All standard checks
  - Azure Policy compliance ✓/✗
  - Defender for Cloud findings ✓/✗
  - Azure Advisor recommendations ✓/✗
  - Advanced security controls ✓/✗
  - Documentation completeness ✓/✗
  - Change management process ✓/✗
- ✅ Azure MCP best practices integration
- ✅ Detailed remediation guidance
- ✅ Full compliance report

**Validation:**
- ✅ Production-grade validation
- ✅ Azure Policy, Defender, Advisor integrated
- ⏱️ Time: 45-90 seconds

---

#### Test ENV-3.4: Get Environment Best Practices (Tagging)
```
What are the best practices for tagging my production environment?
```
**Expected Function:** `get_environment_best_practices_tagging`  
**Expected Output:**
- ✅ Mandatory tags list (Environment, Owner, CostCenter, Application)
- ✅ Recommended tags (ManagedBy, Criticality, DataClassification)
- ✅ Tag value examples
- ✅ Naming patterns
- ✅ Azure Well-Architected Framework guidance
- ✅ Implementation steps

**Validation:**
- ✅ Azure MCP integration
- ✅ Actionable tagging guidance
- ⏱️ Time: 10-15 seconds

---

#### Test ENV-3.5: Get AKS Best Practices
```
What are the best practices for my production AKS cluster?
```
**Expected Function:** `get_aks_best_practices`  
**Expected Output:**
- ✅ AKS-specific best practices
- ✅ Security recommendations
- ✅ Performance optimization
- ✅ Cost optimization
- ✅ High availability configuration
- ✅ Monitoring and logging
- ✅ Azure MCP guidance

**Validation:**
- ✅ Comprehensive AKS guidance
- ✅ Production-ready recommendations
- ⏱️ Time: 10-20 seconds

---

## 🔄 Multi-Turn Conversation Tests

### Test ENV-4.1: Environment Creation → Validation Workflow
```
Turn 1: "Create a production environment for my webapp"
Turn 2: (Agent asks about app name, location, configuration)
Turn 3: "webapp-api, usgovvirginia, enterprise configuration"
Turn 4: (Agent creates environment)
Turn 5: "Validate this environment"
Turn 6: (Agent validates with comprehensive checks)
```
**Expected Behavior:**
- Progressive environment setup
- Context maintained (environment name)
- Validation uses created environment

**Validation:**
- ✅ Context preserved across turns
- ✅ No redundant questions
- ✅ Logical workflow progression

---

## 🎯 Edge Cases & Error Handling

### Test ENV-5.1: Environment Already Exists
```
Create an environment that already exists
```
**Expected:** Error message, suggest alternative names or update option

---

### Test ENV-5.2: Invalid Environment Name
```
Create an environment with invalid name "prod@webapp#123"
```
**Expected:** Validation error, naming conventions guidance

---

## 🚨 Anti-Patterns (Should NOT Work This Way)

### ❌ Test ENV-6.1: Compliance Scan to Environment
```
Check NIST compliance for my environment
```
**Expected:** Routes to **Compliance Agent**, NOT Environment  
(Environment validation is configuration/best practices, not compliance scanning)

---

## 📊 Validation Checklist

- [ ] `agentType: "Environment"` in plan
- [ ] Intent: "environment"
- [ ] Environment operations modify Azure resources
- [ ] Validation checks are thorough and accurate
- [ ] Best practices aligned with Azure Well-Architected
- ⏱️ Create: 30-60 seconds
- ⏱️ Clone: 60-180 seconds
- ⏱️ Validate: 15-90 seconds (depends on level)

---

## 📖 Related Documentation

- **Azure Well-Architected Framework:** [Azure Well-Architected Docs](https://learn.microsoft.com/en-us/azure/well-architected/)
- **Azure Tagging:** [Azure Tagging Best Practices](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/tag-resources)

---

**Last Updated:** November 13, 2025  
**Test Coverage:** 9 functions, 20+ test cases  
**Status:** Ready for comprehensive testing
