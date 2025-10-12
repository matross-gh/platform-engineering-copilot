# Navy Flankspeed Onboarding - Direct Deployment Flow

## Overview

This document describes the complete onboarding workflow for Navy missions, now optimized for **direct deployment** without intermediate template storage.

## Key Changes Made

### ✅ 1. Removed Template Storage for Onboarding

**Previous Behavior:**
- Generated templates were saved to `EnvironmentTemplate` database table
- Required database lookup before deployment
- Added unnecessary complexity for one-time onboarding deployments

**New Behavior:**
- Templates generated on-the-fly and deployed directly
- **Audit logging** replaces database storage
- Reduces latency and database overhead
- Simplified deployment path

**Code Location:** `FlankspeedOnboardingService.cs` - Lines 428-448

```csharp
// STEP 2: AUDIT LOG GENERATED TEMPLATE (NO STORAGE FOR ONBOARDING)
var auditLog = new {
    RequestId = request.Id,
    MissionName = request.MissionName,
    TemplateId = templateId,
    GeneratedAt = DateTime.UtcNow,
    FileCount = generationResult.Files.Count,
    Purpose = "Onboarding - Direct deployment without template storage"
};
_logger.LogInformation("📝 AUDIT: Onboarding template generated {@AuditLog}", auditLog);
```

---

### ✅ 2. Added Pre-Deployment Validation

**Validation Checks:**
- ✅ Bicep template syntax (entry point exists, not empty)
- ✅ Required parameters (region, VNet CIDR, resource group name length)
- ✅ Classification-specific requirements (SECRET → DoD IL5 + Gov cloud)
- ✅ Resource naming conventions (max 90 chars for RG names)

**Code Location:** `FlankspeedOnboardingService.cs` - Lines 450-522

**Validation Examples:**

```csharp
// Classification validation
if (request.ClassificationLevel.Contains("SECRET")) {
    if (!request.ComplianceFrameworks.Contains("DoD IL5")) {
        validationErrors.Add("SECRET classification requires DoD IL5 compliance");
    }
    if (!request.Region.Contains("gov")) {
        validationErrors.Add("SECRET classification requires Azure Government region");
    }
}
```

---

### ✅ 3. Enhanced Notifications

**Old Notifications:**
- Basic status updates
- Minimal context
- No resource details

**New Notifications:**
- ✅ **Deployment Progress:** Environment details, template info, services, estimated time
- ✅ **Validation Results:** Detailed pass/fail with specific errors
- ✅ **Deployment Metrics:** Resource count, duration, classification, region
- ✅ **Success Summary:** Resource list, environment IDs, next steps

**Code Locations:**
- Validation logging: Lines 518-522
- Deployment progress: Lines 541-551
- Resource logging: Lines 622-632
- Completion metrics: Lines 694-707

---

## Complete Deployment Flow (5 Steps)

### **Step 1: Auto-Generate Infrastructure Template** 🔨

**Input:** OnboardingRequest with mission requirements  
**Process:**
- `DynamicTemplateGenerator.GenerateTemplateAsync()`
- Creates Bicep files from requirements
- Returns `TemplateGenerationResult` with files dictionary

**Output:** Dictionary<string, string> of Bicep files

**Code:** Lines 386-425

---

### **Step 2: Audit Log Template** 📋

**Input:** Generated template files  
**Process:**
- Create audit log object with metadata
- Log to structured logging (JSON)
- **NO DATABASE STORAGE**

**Output:** Audit trail in logs

**Code:** Lines 428-448

---

### **Step 3: Pre-Deployment Validation** 🔍

**Input:** Template files + OnboardingRequest  
**Process:**
- Validate Bicep syntax
- Check required parameters
- Validate classification requirements
- Check resource naming limits

**Output:** Pass/fail with error list

**Code:** Lines 450-522

---

### **Step 4: Deploy Infrastructure to Azure** 🚀

**Input:** Validated template + parameters  
**Process:**

```
EnvironmentManagementEngine.CreateEnvironmentAsync()
  ↓
EnvironmentManagementEngine.DeployFromTemplateAsync() [private]
  ↓
EnvironmentManagementEngine.DeployBicepTemplateAsync() [private]
  ↓
DeploymentOrchestrationService.DeployBicepTemplateAsync()
  ↓
Azure.ResourceManager.Resources SDK
  ↓
Azure Resource Manager API
  ↓
Azure Resources Created ✅
```

**Output:** EnvironmentCreationResult with:
- Environment ID
- Resource Group
- Created Resources list
- Deployment ID

**Code:** Lines 524-653

**Key Implementation:**

```csharp
var environmentRequest = new EnvironmentCreationRequest {
    Name = environmentName,
    ResourceGroup = resourceGroupName,
    Location = region,
    
    // DIRECT DEPLOYMENT: Pass files inline (no template ID)
    TemplateContent = generationResult.Files.Values.FirstOrDefault(),
    TemplateFiles = generationResult.Files.Select(f => new ServiceTemplateFile {
        FileName = f.Key,
        Content = f.Value,
        IsEntryPoint = f.Key.Contains("main")
    }).ToList(),
    
    Tags = {
        { "DeploymentType", "Onboarding-Direct" } // Mark as direct deployment
    }
};

var deploymentResult = await _environmentEngine.CreateEnvironmentAsync(
    environmentRequest, 
    cancellationToken);
```

---

### **Step 5: Update Request & Final Audit** 📊

**Input:** Deployment results  
**Process:**
- Update OnboardingRequest.ProvisionedResources
- Mark status as Completed
- Log comprehensive audit with metrics
- Send completion notifications

**Output:**
- Updated database record
- Final audit log
- Teams/email notifications

**Code:** Lines 655-716

**Audit Example:**

```csharp
var completionAudit = new {
    RequestId = request.Id,
    Status = "Completed",
    EnvironmentId = deploymentResult.EnvironmentId,
    ResourceCount = deploymentResult.CreatedResources?.Count ?? 0,
    TemplateFileCount = generationResult.Files.Count,
    DeploymentMethod = "Direct-Onboarding-NoStorage", // Key marker
    Duration = (DateTime.UtcNow - request.CreatedAt).TotalMinutes
};
_logger.LogInformation("📝 AUDIT: Onboarding deployment completed {@CompletionAudit}", completionAudit);
```

---

## Deployment Orchestration Details

### `DeploymentOrchestrationService.DeployBicepTemplateAsync()`

**Location:** `src/Platform.Engineering.Copilot.Core/Services/Infrastructure/DeploymentOrchestrationService.cs`

**Function:**
- Writes Bicep files to temp directory
- Compiles Bicep → ARM JSON
- Submits deployment to Azure Resource Manager
- Polls for completion
- Returns resource list

**Azure SDK Used:**
```csharp
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

var deployment = await resourceGroup.GetArmDeployments()
    .CreateOrUpdateAsync(
        WaitUntil.Completed,
        deploymentName,
        new ArmDeploymentContent(...)
    );
```

---

## Audit Trail & Logging

### Structured Logging

All audit logs use structured logging with JSON objects:

```csharp
_logger.LogInformation("📝 AUDIT: Onboarding template generated {@AuditLog}", auditLog);
_logger.LogInformation("📝 AUDIT: Onboarding deployment completed {@CompletionAudit}", completionAudit);
```

### Searchable Fields

- `RequestId`: Unique onboarding request ID
- `MissionName`: Navy mission name
- `TemplateId`: Generated template GUID (audit reference)
- `DeploymentMethod`: "Direct-Onboarding-NoStorage"
- `EnvironmentId`: Azure deployment ID
- `ResourceCount`: Number of resources created
- `Duration`: Total time in minutes

### Query Examples

**Find all direct deployments:**
```
DeploymentMethod:"Direct-Onboarding-NoStorage"
```

**Find onboarding for specific mission:**
```
MissionName:"Maritime Sensor Integration Platform"
```

**Find deployments by duration:**
```
Duration:>15 AND DeploymentMethod:"Direct-Onboarding-NoStorage"
```

---

## Benefits of Direct Deployment

### ✅ Performance
- **Reduced latency:** No database round-trip for template lookup
- **Faster approval-to-deployment:** Immediate after approval

### ✅ Simplicity
- **Fewer moving parts:** No template storage service dependency
- **Cleaner code:** Direct pipeline from generation to deployment

### ✅ Security
- **No template persistence:** Reduces attack surface
- **Audit logs only:** Compliance without storage overhead

### ✅ Scalability
- **No database writes:** Reduces DB load for high-volume onboarding
- **Stateless deployment:** Each request fully contained

---

## Service Templates vs. Onboarding Templates

### Service Templates (For Reuse)
- **Purpose:** Mission owners deploying **existing** workloads
- **Storage:** Saved to `EnvironmentTemplate` table
- **Lifecycle:** Create → Store → Deploy multiple times
- **Use Case:** "Deploy the standard AKS cluster"

### Onboarding Templates (One-Time)
- **Purpose:** Initial infrastructure for **new** missions
- **Storage:** Audit logs only (no database)
- **Lifecycle:** Generate → Validate → Deploy once
- **Use Case:** "Onboard NAVWAR Maritime Sensor Platform"

---

## Testing the Flow

### Prerequisites
1. Azure Government subscription
2. Service principal with Contributor role
3. Admin Console running (port 3000)
4. API running (port 7001)

### Test Scenario

**1. Create Onboarding Request:**

```bash
curl -X POST http://localhost:7001/api/chat/intelligent-query \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I need to onboard a new mission for NAVWAR with SECRET classification, AKS cluster, and DoD IL5 compliance",
    "conversationId": "onboarding-test-001"
  }'
```

**2. Navigate to Admin Console:**
```
http://localhost:3000/admin
```

**3. Approve Request:**
- Click "Approve" on pending request
- Provisioning starts automatically

**4. Monitor Logs:**
```bash
# Watch for audit logs
tail -f logs/onboarding.log | grep "AUDIT"

# Expected outputs:
# 📝 AUDIT: Onboarding template generated
# 🔍 Pre-deployment validation passed
# 🚀 Deploying infrastructure directly
# 📝 AUDIT: Onboarding deployment completed
```

**5. Verify Deployment:**
```bash
az group show --name "maritime-sensor-integration-platform-rg"
az resource list --resource-group "maritime-sensor-integration-platform-rg"
```

---

## Troubleshooting

### Issue: Validation Fails

**Symptom:** "SECRET classification requires DoD IL5 compliance"

**Solution:**
- Ensure `ComplianceFrameworks` includes "DoD IL5"
- Verify `Region` contains "gov" (e.g., "usgovvirginia")

### Issue: Deployment Timeout

**Symptom:** Deployment exceeds 30-minute timeout

**Solution:**
- Check Azure portal for deployment status
- Increase timeout in `DeploymentOptions` (Line 583)
- Verify Azure service quotas

### Issue: Resource Already Exists

**Symptom:** "Resource group 'mission-name-rg' already exists"

**Solution:**
- Choose different mission name
- Delete existing resource group
- Use unique naming convention

---

## Next Steps

1. **Test End-to-End:** Deploy sample mission through Admin Console
2. **Monitor Metrics:** Track deployment duration, success rate
3. **Optimize Validation:** Add more comprehensive checks (quota limits, naming conflicts)
4. **Enhance Notifications:** Add Slack/PagerDuty integrations
5. **Add Rollback:** Implement automatic rollback on deployment failure

---

## Files Modified

| File | Lines | Changes |
|------|-------|---------|
| `FlankspeedOnboardingService.cs` | 371-716 | Removed template storage, added validation, enhanced logging |
| `EnvironmentManagementEngine.cs` | 1103-1350 | Direct deployment from inline templates |

## Documentation

- [Architecture Overview](../ARCHITECTURE.md)
- [Deployment Guide](../DEPLOYMENT.md)
- [API Documentation](../docs/api/)

---

**Last Updated:** October 10, 2025  
**Version:** 2.0 (Direct Deployment)  
**Author:** Platform Engineering Team
