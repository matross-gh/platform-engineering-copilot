# File Handoff Implementation - SharedMemory Integration

## Overview
Implemented SharedMemory integration in EnvironmentManagementPlugin to enable file handoff between InfrastructureAgent and EnvironmentAgent during multi-agent provisioning workflows.

## Problem Statement
Previously, when executing "Actually provision" workflows:
1. **InfrastructureAgent** (Priority 1) would generate Bicep templates and store them in SharedMemory
2. **EnvironmentAgent** (Priority 2) would attempt to deploy, but had NO access to the generated files
3. This caused deployments to fail or use default templates instead of AI-generated ones

## Solution Architecture

### How File Handoff Works

```
┌─────────────────────────────────────────────────────────────────┐
│                    PROVISIONING WORKFLOW                        │
└─────────────────────────────────────────────────────────────────┘

Step 1: InfrastructureAgent (Priority 1)
┌──────────────────────────┐
│ InfrastructurePlugin     │
│ - Generate Bicep files   │──┐
│ - Store in SharedMemory  │  │
└──────────────────────────┘  │
                               │
                               ▼
                    ┌──────────────────┐
                    │   SharedMemory   │
                    │                  │
                    │  Files by Conv:  │
                    │  - main.bicep    │
                    │  - modules/*.    │
                    └──────────────────┘
                               │
                               │
Step 2: EnvironmentAgent (Priority 2)  │
┌──────────────────────────┐  │
│ EnvironmentPlugin        │  │
│ - SetConversationId()    │◄─┘
│ - Retrieve files from SM │
│ - Populate TemplateContent│
│ - Deploy via Engine      │
└──────────────────────────┘
```

## Code Changes

### 1. EnvironmentManagementPlugin.cs

**Added SharedMemory Integration:**

```csharp
// Added private field
private readonly SharedMemory _sharedMemory;
private string? _currentConversationId;

// Updated constructor
public EnvironmentManagementPlugin(
    ILogger<EnvironmentManagementPlugin> logger,
    Kernel kernel,
    IEnvironmentManagementEngine environmentEngine,
    IOnboardingService onboardingService,
    EnvironmentStorageService environmentStorage,
    SharedMemory sharedMemory) // ← NEW
{
    _sharedMemory = sharedMemory ?? throw new ArgumentNullException(nameof(sharedMemory));
}

// Added SetConversationId method
public void SetConversationId(string conversationId)
{
    _currentConversationId = conversationId;
    _logger.LogInformation("🆔 EnvironmentManagementPlugin: ConversationId set to: {ConversationId}", conversationId);
}
```

**Added File Retrieval Logic in CreateEnvironmentAsync():**

```csharp
// RIGHT BEFORE calling _environmentEngine.CreateEnvironmentAsync()

if (!string.IsNullOrEmpty(_currentConversationId))
{
    var availableFiles = _sharedMemory.GetGeneratedFileNames(_currentConversationId);
    
    if (availableFiles != null && availableFiles.Count > 0)
    {
        // Find main template (main.bicep or aks.bicep)
        var mainTemplate = availableFiles.FirstOrDefault(f => 
            f.EndsWith("main.bicep", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith("aks.bicep", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".bicep", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(mainTemplate))
        {
            // Retrieve main template content
            var templateContent = _sharedMemory.GetGeneratedFile(_currentConversationId, mainTemplate);
            creationRequest.TemplateContent = templateContent;
            
            // Retrieve additional module files
            var additionalFiles = availableFiles
                .Where(f => f != mainTemplate && f.EndsWith(".bicep"))
                .ToList();

            foreach (var fileName in additionalFiles)
            {
                var fileContent = _sharedMemory.GetGeneratedFile(_currentConversationId, fileName);
                creationRequest.TemplateFiles.Add(new ServiceTemplateFile
                {
                    FileName = fileName,
                    Content = fileContent,
                    IsEntryPoint = false
                });
            }
        }
    }
}
```

### 2. EnvironmentAgent.cs

**Added Plugin Reference and ConversationId Call:**

```csharp
// Added private field to store plugin reference
private readonly EnvironmentManagementPlugin _environmentPlugin;

// Updated constructor to store plugin
public EnvironmentAgent(
    ISemanticKernelService semanticKernelService,
    ILogger<EnvironmentAgent> logger,
    EnvironmentManagementPlugin environmentPlugin)
{
    _environmentPlugin = environmentPlugin; // ← Store reference
}

// Added SetConversationId call in ProcessAsync
public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
{
    // ...existing code...
    
    // 🔥 Set conversation ID BEFORE executing chat completion
    _environmentPlugin.SetConversationId(task.ConversationId ?? "default");
    
    // Execute chat completion (which calls plugin functions)
    var result = await _chatCompletion.GetChatMessageContentAsync(...);
}
```

## Execution Flow

### Complete Provisioning Workflow

1. **User Request:** "Actually provision this template"

2. **OrchestratorAgent** creates 5-task plan:
   - Infrastructure (Priority 1)
   - Environment (Priority 2)
   - Discovery (Priority 3)
   - Compliance (Priority 4)
   - CostManagement (Priority 5)

3. **InfrastructureAgent Execution:**
   ```
   - SetConversationId("765d84a4-...")
   - Generate 12 Bicep files
   - Store in SharedMemory: _sharedMemory.StoreGeneratedFiles(conversationId, files)
   ```

4. **EnvironmentAgent Execution:**
   ```
   - SetConversationId("765d84a4-...")  ← Same conversation ID!
   - Retrieve files from SharedMemory
   - Populate EnvironmentCreationRequest.TemplateContent
   - Populate EnvironmentCreationRequest.TemplateFiles
   - Call _environmentEngine.CreateEnvironmentAsync(request)
   ```

5. **EnvironmentManagementEngine:**
   ```
   - Receives request with populated templates
   - Calls DeployFromTemplateAsync(template)
   - Routes to DeployBicepTemplateAsync()
   - Calls DeploymentOrchestrationService.DeployBicepTemplateAsync()
   - Deploys to Azure
   ```

## Expected Log Output

### Successful File Handoff

```
info: Platform.Engineering.Copilot.Core.Plugins.InfrastructurePlugin[0]
      🆔 InfrastructurePlugin: ConversationId set to: 765d84a4-be80-4ed9-a5ab-dc6d88cac155
      
info: Platform.Engineering.Copilot.Core.Plugins.InfrastructurePlugin[0]
      📦 Stored 12 files in SharedMemory for conversation 765d84a4-be80-4ed9-a5ab-dc6d88cac155

info: Platform.Engineering.Copilot.Core.Services.Agents.EnvironmentAgent[0]
      🔗 EnvironmentAgent: ConversationId set to 765d84a4-be80-4ed9-a5ab-dc6d88cac155 for SharedMemory file retrieval

info: Platform.Engineering.Copilot.Core.Plugins.EnvironmentManagementPlugin[0]
      📂 Retrieved 12 generated files from SharedMemory for conversation 765d84a4-be80-4ed9-a5ab-dc6d88cac155
      
info: Platform.Engineering.Copilot.Core.Plugins.EnvironmentManagementPlugin[0]
      ✅ Main Bicep template retrieved: main.bicep (23456 bytes)
      
info: Platform.Engineering.Copilot.Core.Plugins.EnvironmentManagementPlugin[0]
      📄 Additional module retrieved: modules/aks-cluster.bicep (5678 bytes)
      
info: Platform.Engineering.Copilot.Core.Plugins.EnvironmentManagementPlugin[0]
      ✅ Retrieved 11 additional Bicep modules from SharedMemory
      
info: Platform.Engineering.Copilot.Core.Plugins.EnvironmentManagementPlugin[0]
      🚀 Environment deployment will use generated Bicep template from InfrastructureAgent
```

### Fallback Behavior (No Files Found)

```
info: Platform.Engineering.Copilot.Core.Plugins.EnvironmentManagementPlugin[0]
      ℹ️ No generated files found in SharedMemory for conversation 765d84a4-be80-4ed9-a5ab-dc6d88cac155 - will use default templates
```

## Testing

### Test Case 1.6: Actually Provision AKS Cluster

**Workflow:**
1. Generate template: "I need a dev AKS cluster"
2. Provide details: "3 nodes, Standard_D2s_v3, subscription 453c2549-..."
3. **Trigger provisioning: "Actually provision this template"**

**Expected:**
- ✅ All 5 agents execute in order
- ✅ Infrastructure stores 12 files in SharedMemory
- ✅ Environment retrieves all 12 files
- ✅ Deployment uses AI-generated template (not defaults)
- ✅ Logs show "Retrieved X files from SharedMemory"

### Validation Checklist

- [ ] ConversationId matches between Infrastructure and Environment agents
- [ ] SharedMemory contains files before Environment agent runs
- [ ] EnvironmentManagementPlugin retrieves main.bicep successfully
- [ ] EnvironmentManagementPlugin retrieves all module files
- [ ] EnvironmentCreationRequest.TemplateContent is populated
- [ ] EnvironmentCreationRequest.TemplateFiles contains modules
- [ ] DeployBicepTemplateAsync receives populated template
- [ ] Deployment executes with AI-generated Bicep (not defaults)

## Error Handling

### Missing ConversationId

```csharp
if (string.IsNullOrEmpty(_currentConversationId))
{
    _logger.LogWarning(
        "⚠️ ConversationId not set - cannot retrieve generated Bicep files from SharedMemory. " +
        "Ensure SetConversationId() is called before CreateEnvironmentAsync()");
}
```

### No Files in SharedMemory

```csharp
if (availableFiles == null || availableFiles.Count == 0)
{
    _logger.LogInformation(
        "ℹ️ No generated files found in SharedMemory for conversation {ConversationId} - will use default templates",
        _currentConversationId);
}
```

### No .bicep Files Found

```csharp
if (string.IsNullOrEmpty(mainTemplate))
{
    _logger.LogWarning(
        "⚠️ No .bicep template found in SharedMemory. Available files: {Files}",
        string.Join(", ", availableFiles));
}
```

### File Retrieval Exception

```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, 
        "⚠️ Failed to retrieve files from SharedMemory - deployment will continue with default templates");
}
```

## Benefits

### Before This Fix
- ❌ Environment agent couldn't access generated templates
- ❌ Deployments used default/hardcoded templates
- ❌ AI-generated templates were discarded
- ❌ Multi-file Bicep modules not supported
- ❌ No visibility into file handoff process

### After This Fix
- ✅ Environment agent retrieves AI-generated templates
- ✅ Deployments use exactly what Infrastructure agent created
- ✅ Multi-file Bicep modules fully supported
- ✅ Complete file handoff visibility in logs
- ✅ Graceful fallback to defaults if files missing
- ✅ Proper inter-agent communication pattern established

## Related Changes

### Execution Order Fix
This file handoff implementation complements the execution order fix in `ExecutionPlanValidator.cs`:

**Correct Order:**
1. Infrastructure (Priority 1) - Generate & Store
2. Environment (Priority 2) - Retrieve & Deploy
3. Discovery (Priority 3) - Verify Resources
4. Compliance (Priority 4) - Scan New RG
5. CostManagement (Priority 5) - Estimate Costs

### Future Enhancements

1. **Resource Group Scoping:**
   - Store RG name in SharedMemory after Environment deployment
   - Compliance agent retrieves RG scope
   - Scans only new resources (not entire subscription)

2. **Deployment Result Sharing:**
   - Store deployment output in SharedMemory
   - Discovery agent retrieves resource IDs
   - More targeted resource verification

3. **Cost Estimation Input:**
   - CostManagement retrieves template from SharedMemory
   - Provides accurate cost estimates before deployment
   - Can warn about expensive configurations

## Troubleshooting

### Issue: Environment agent not finding files

**Symptoms:**
```
ℹ️ No generated files found in SharedMemory
```

**Diagnosis:**
1. Check if InfrastructureAgent stored files:
   ```
   grep "Stored.*files in SharedMemory" logs
   ```
2. Verify conversation IDs match:
   ```
   grep "ConversationId set to" logs | grep -E "(Infrastructure|Environment)"
   ```

**Fix:**
- Ensure both agents use same `task.ConversationId`
- Verify SharedMemory is singleton (not scoped)

### Issue: Wrong template deployed

**Symptoms:**
- Deployment uses defaults instead of generated template

**Diagnosis:**
1. Check if TemplateContent was populated:
   ```
   grep "Main Bicep template retrieved" logs
   ```
2. Verify files exist in SharedMemory:
   ```
   grep "Retrieved.*files from SharedMemory" logs
   ```

**Fix:**
- Ensure Infrastructure task completes before Environment task
- Verify Sequential execution pattern
- Check priority values (Infrastructure=1, Environment=2)

## Implementation Date
October 22, 2025

## Authors
- GitHub Copilot (AI Assistant)
- John Spinella (Platform Engineering)
