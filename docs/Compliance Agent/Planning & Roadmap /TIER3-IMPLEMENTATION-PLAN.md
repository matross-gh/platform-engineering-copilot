# TIER 3: AI-Enhanced Features - Implementation Plan

## Executive Summary

This document outlines the implementation plan for adding AI-enhanced compliance capabilities to the Platform Engineering Copilot. The plan leverages **Azure OpenAI Service** to transform the Compliance Agent from a detection tool into an intelligent remediation assistant.

**Timeline:** 3-4 weeks  
**Effort:** 13 days  
**Dependencies:** Azure OpenAI Service, IChatCompletionService (Semantic Kernel)

---

## Architecture Changes

### Current Architecture
```
┌─────────────────────────────────────────────────────────────┐
│              Compliance Agent (Current)                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ComplianceEngine → AssessmentResults → Static Templates    │
│  (Rule-based)       (JSON findings)     (Placeholders)      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### TIER 3 Architecture
```
┌───────────────────────────────────────────────────────────────────────┐
│              Compliance Agent (TIER 3 - AI-Enhanced)                  │
├───────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  ┌──────────────────┐      ┌─────────────────────────────────────┐  │
│  │ ComplianceEngine │─────▶│ ATORemediationEngine (Existing)     │  │
│  │ (Existing)       │      │ - Actual Azure API remediation      │  │
│  │                  │      │ - Validation & execution            │  │
│  └──────────────────┘      │ - Remediation state tracking        │  │
│           │                 └──────────┬──────────────────────────┘  │
│           │                            │                             │
│           │                            │ Queries capabilities        │
│           │                            ▼                             │
│           │                 ┌────────────────────────────────────┐  │
│           │                 │ AiRemediationService (NEW)         │  │
│           │                 │ - GenerateRemediationScript        │  │
│           │                 │ - GetNaturalLanguageGuidance       │  │
│           │                 │ - PrioritizeFindings               │  │
│           │                 │                                    │  │
│           │                 │ Uses ATORemediationEngine for:     │  │
│           │                 │ • Available remediation actions    │  │
│           │                 │ • Current resource state           │  │
│           │                 │ • Recommended remediation plans    │  │
│           │                 └────────────┬───────────────────────┘  │
│           │                              │                           │
│           │                              ▼                           │
│           │                 ┌──────────────────────────┐            │
│           │                 │ IChatCompletionService   │            │
│           │                 │ (Azure OpenAI - GPT-4)   │            │
│           │                 └──────────────────────────┘            │
│           │                              ▲                           │
│           ▼                              │                           │
│  ┌──────────────────────────────────────┴──────────────────┐       │
│  │  ComplianceDocumentGenerator (NEW)                      │       │
│  │  - AI-powered control narratives                        │       │
│  │  - Evidence synthesis & explanation                     │       │
│  │  - Smart POA&M generation                               │       │
│  │  - Risk assessment narratives                           │       │
│  └─────────────────────────────────────────────────────────┘       │
│                              │                                       │
│                              ▼                                       │
│  ┌─────────────────────────────────────────────────────────┐       │
│  │  ComplianceMonitoringService (NEW)                      │       │
│  │  - Real-time dashboard data                             │       │
│  │  - Trend analysis                                       │       │
│  │  - Automated alerts                                     │       │
│  │  - SignalR push notifications                           │       │
│  └─────────────────────────────────────────────────────────┘       │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Tasks

### ✅ **Task 1: Fix AI Chat Completion Service Registration** (2 days)
**Status:** Critical - Fixes existing warning  
**Files Modified:** 1  
**Files Created:** 0

#### Changes Required

**File:** `src/Platform.Engineering.Copilot.Compliance.Agent/Program.cs`

```csharp
// BEFORE (Missing registration)
var builder = WebApplication.CreateBuilder(args);

// AFTER (Add Azure OpenAI registration)
var builder = WebApplication.CreateBuilder(args);

// Register Azure OpenAI Chat Completion Service
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4-turbo",
    endpoint: builder.Configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured"),
    credentials: new DefaultAzureCredential() // Use Managed Identity
);

// Add to Semantic Kernel
builder.Services.AddTransient<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    
    // Add Azure OpenAI chat completion
    kernelBuilder.Services.AddAzureOpenAIChatCompletion(
        deploymentName: builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4-turbo",
        endpoint: builder.Configuration["AzureOpenAI:Endpoint"]!,
        credentials: new DefaultAzureCredential()
    );
    
    // Register plugins
    kernelBuilder.Plugins.AddFromType<CompliancePlugin>();
    
    return kernelBuilder.Build();
});
```

**Configuration Update:**

**File:** `appsettings.json`

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://pec-compliance-openai.openai.azure.com/",
    "DeploymentName": "gpt-4-turbo",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

**Testing:**
- Verify `IChatCompletionService` is available in ComplianceAgent
- Confirm no warning: "Compliance Agent initialized without AI chat completion service"
- Test basic chat completion call

---

### ✅ **Task 2: Create AiRemediationService** (3 days)
**Status:** New service  
**Files Modified:** 0  
**Files Created:** 3

#### Files to Create

##### **File 1:** `src/Platform.Engineering.Copilot.Compliance.Agent/Services/AiRemediationService.cs`

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services;

public class AiRemediationService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly INistControlsService _nistService;
    private readonly IATORemediationEngine _remediationEngine; // ADDED: Use existing engine
    private readonly ILogger<AiRemediationService> _logger;
    
    public AiRemediationService(
        IChatCompletionService chatCompletion,
        INistControlsService nistService,
        IATORemediationEngine remediationEngine, // ADDED: Inject remediation engine
        ILogger<AiRemediationService> logger)
    {
        _chatCompletion = chatCompletion;
        _nistService = nistService;
        _remediationEngine = remediationEngine; // ADDED
        _logger = logger;
    }
    
    /// <summary>
    /// Generate context-aware remediation script using GPT-4
    /// Queries ATORemediationEngine for available remediation actions and current state
    /// </summary>
    public async Task<RemediationScript> GenerateRemediationScriptAsync(
        AtoFinding finding,
        string scriptType = "AzureCLI",
        CancellationToken cancellationToken = default)
    {
        // 1. Get control requirements
        var control = await _nistService.GetControlAsync(finding.ControlId, cancellationToken);
        
        // 2. Query ATORemediationEngine for available remediation actions
        var availableRemediations = await _remediationEngine.GetAvailableRemediationsAsync(
            finding, cancellationToken);
        
        // 3. Get current resource state from remediation engine
        var resourceState = await _remediationEngine.GetResourceStateAsync(
            finding.ResourceId, cancellationToken);
        
        // 4. Build enriched prompt with actual remediation capabilities
        var remediationOptions = string.Join("\n", availableRemediations.Select(r => 
            $"- {r.Action}: {r.Description} (Risk: {r.Risk})"));
        
        var prompt = $@"You are an Azure security expert. Generate a remediation script for:

Control: {finding.ControlId} - {control.Title}
Severity: {finding.Severity}
Resource: {finding.ResourceId}
Finding: {finding.Description}

**Current Resource State (from ATORemediationEngine):**
{resourceState.ToJson()}

**Available Remediation Actions (from ATORemediationEngine):**
{remediationOptions}

**Recommended Remediation Plan:**
{finding.RemediationPlan}

Generate a {scriptType} script that:
1. Validates the current state matches the resource state above
2. Implements ONE of the available remediation actions
3. Uses the exact Azure CLI/PowerShell commands that ATORemediationEngine would use
4. Verifies the fix
5. Logs all actions
6. Handles errors gracefully

Include comments explaining each step. Make the script idempotent.";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(GetSystemPrompt(scriptType));
        chatHistory.AddUserMessage(prompt);
        
        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    ["temperature"] = 0.2,
                    ["max_tokens"] = 2000
                }
            },
            cancellationToken: cancellationToken);
        
        return new RemediationScript
        {
            FindingId = finding.FindingId,
            ControlId = finding.ControlId,
            ScriptType = scriptType,
            Script = ExtractCodeFromResponse(response.Content),
            AvailableRemediations = availableRemediations, // Include engine's options
            RecommendedAction = availableRemediations.FirstOrDefault()?.Action,
            GeneratedAt = DateTimeOffset.UtcNow,
            GeneratedBy = "AI-GPT4",
            RequiresApproval = finding.Severity is AtoFindingSeverity.Critical or AtoFindingSeverity.High
        };
    }
    
    /// <summary>
    /// Generate natural language guidance for remediation
    /// Uses ATORemediationEngine's remediation plan as technical foundation
    /// </summary>
    public async Task<RemediationGuidance> GetNaturalLanguageGuidanceAsync(
        AtoFinding finding,
        CancellationToken cancellationToken = default)
    {
        // Get remediation plan from ATORemediationEngine
        var remediationPlan = await _remediationEngine.GenerateRemediationPlanAsync(
            finding, cancellationToken);
        
        var prompt = $@"Explain how to remediate this compliance finding for a cloud engineer:

Control: {finding.ControlId}
Finding: {finding.Description}
Risk: {finding.RiskLevel}

**Technical Remediation Plan (from ATORemediationEngine):**
Action: {remediationPlan.Action}
Commands: {string.Join(", ", remediationPlan.Commands)}
Impact: {remediationPlan.Impact}

Translate this technical plan into a friendly, step-by-step guide:

1. What's wrong (2-3 sentences)
2. Why it matters (security/compliance impact)
3. Step-by-step remediation (numbered list, using the commands above)
4. How to verify the fix
5. Estimated time to remediate

Keep explanations clear and actionable. Reference the exact commands from the remediation plan.";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a patient cloud security mentor helping engineers fix compliance issues.");
        chatHistory.AddUserMessage(prompt);
        
        var response = await _chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        
        return new RemediationGuidance
        {
            FindingId = finding.FindingId,
            Explanation = response.Content ?? string.Empty,
            TechnicalPlan = remediationPlan, // Include engine's plan
            Confidence = 0.9,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
    
    /// <summary>
    /// AI-powered risk prioritization with business context
    /// </summary>
    public async Task<List<PrioritizedFinding>> PrioritizeFindingsAsync(
        List<AtoFinding> findings,
        string businessContext = "",
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"Prioritize these {findings.Count} compliance findings based on:
- Security risk
- Business impact
- Ease of remediation
- Compliance deadlines

Business Context: {businessContext}

Findings:
{string.Join("\n", findings.Select((f, i) => $"{i + 1}. {f.ControlId}: {f.Description} (Severity: {f.Severity})"))}

Return a JSON array with FindingId, Priority (1-5), Reasoning.";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a compliance risk analyst. Prioritize findings for maximum security impact with minimal disruption.");
        chatHistory.AddUserMessage(prompt);
        
        var response = await _chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        var prioritized = JsonSerializer.Deserialize<List<PrioritizedFinding>>(ExtractJsonFromResponse(response.Content ?? "[]"));
        
        return prioritized ?? new List<PrioritizedFinding>();
    }
    
    private string GetSystemPrompt(string scriptType) => scriptType switch
    {
        "PowerShell" => "You are an Azure PowerShell automation expert. Generate production-ready PowerShell scripts using Az modules. Follow best practices: error handling, parameter validation, idempotency, logging.",
        "AzureCLI" => "You are an Azure CLI expert. Generate bash scripts using az commands. Follow best practices: error handling, validation, idempotency, JSON parsing with jq.",
        "Terraform" => "You are a Terraform IaC expert. Generate HCL code for Azure resources. Use azurerm provider, follow HashiCorp style guide, include variables and outputs.",
        _ => "You are an Azure automation expert."
    };
    
    private string ExtractCodeFromResponse(string response)
    {
        // Extract code from markdown code blocks
        var codeBlockPattern = @"```(?:bash|powershell|terraform|hcl)?\s*\n(.*?)\n```";
        var match = Regex.Match(response, codeBlockPattern, RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : response;
    }
    
    private string ExtractJsonFromResponse(string response)
    {
        var jsonPattern = @"\[.*\]|\{.*\}";
        var match = Regex.Match(response, jsonPattern, RegexOptions.Singleline);
        return match.Success ? match.Value : "[]";
    }
}
```

##### **File 2:** `src/Platform.Engineering.Copilot.Core/Models/Compliance/RemediationScript.cs`

```csharp
namespace Platform.Engineering.Copilot.Core.Models.Compliance;

public class RemediationScript
{
    public string FindingId { get; set; } = string.Empty;
    public string ControlId { get; set; } = string.Empty;
    public string ScriptType { get; set; } = string.Empty; // AzureCLI, PowerShell, Terraform
    public string Script { get; set; } = string.Empty;
    public List<RemediationAction>? AvailableRemediations { get; set; } // ADDED: From ATORemediationEngine
    public string? RecommendedAction { get; set; } // ADDED: From ATORemediationEngine
    public DateTimeOffset GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
}

public class RemediationGuidance
{
    public string FindingId { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public RemediationPlan? TechnicalPlan { get; set; } // ADDED: Include engine's plan
    public double Confidence { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}

public class PrioritizedFinding
{
    public string FindingId { get; set; } = string.Empty;
    public string ControlId { get; set; } = string.Empty;
    public int Priority { get; set; } // 1 (highest) to 5 (lowest)
    public string Reasoning { get; set; } = string.Empty;
}
```

##### **File 3:** `src/Platform.Engineering.Copilot.Compliance.Agent/Plugins/CompliancePlugin.cs` (MODIFY)

Add new kernel functions:

```csharp
// ADD to existing CompliancePlugin class

private readonly AiRemediationService _aiRemediationService;

// Add to constructor
public CompliancePlugin(
    IComplianceEngine complianceEngine,
    AiRemediationService aiRemediationService, // NEW
    ILogger<CompliancePlugin> logger)
{
    _complianceEngine = complianceEngine;
    _aiRemediationService = aiRemediationService; // NEW
    _logger = logger;
}

[KernelFunction("generate_ai_remediation_script")]
[Description("Use AI to generate a custom remediation script based on ATORemediationEngine capabilities")]
public async Task<string> GenerateAiRemediationScriptAsync(
    [Description("Finding ID to remediate")] string findingId,
    [Description("Script type: AzureCLI, PowerShell, or Terraform")] string scriptType = "AzureCLI",
    CancellationToken cancellationToken = default)
{
    var finding = await _complianceEngine.GetFindingAsync(findingId, cancellationToken);
    
    // AI service queries ATORemediationEngine and generates friendly script
    var script = await _aiRemediationService.GenerateRemediationScriptAsync(
        finding, scriptType, cancellationToken);
    
    return $@"**AI-Generated Remediation Script**
**Based on ATORemediationEngine capabilities**

**Finding:** {finding.Description}
**Control:** {finding.ControlId}
**Script Type:** {scriptType}
**Requires Approval:** {script.RequiresApproval}

**Available Remediation Options (from ATORemediationEngine):**
{string.Join("\n", script.AvailableRemediations?.Select(r => $"- {r.Action} (Risk: {r.Risk})") ?? new[] { "No options available" })}

**Recommended Action:** {script.RecommendedAction ?? "None"}

```{scriptType.ToLower()}
{script.Script}
```

**To execute this remediation automatically:**
Use: `execute_remediation --finding-id {findingId}`
(This will use ATORemediationEngine to apply the fix)

**To apply manually:**
Review the script above and execute step-by-step.";
}

[KernelFunction("execute_remediation")]
[Description("Execute automated remediation using ATORemediationEngine")]
public async Task<string> ExecuteRemediationAsync(
    [Description("Finding ID")] string findingId,
    [Description("Dry run mode (validate only)")] bool dryRun = true,
    CancellationToken cancellationToken = default)
{
    // This uses ATORemediationEngine for actual execution
    var result = await _remediationEngine.RemediateFindingAsync(
        findingId, dryRun, cancellationToken);
    
    return result.Success 
        ? $"✅ Remediation successful: {result.Message}"
        : $"❌ Remediation failed: {result.ErrorMessage}";
}

[KernelFunction("get_remediation_guidance")]
[Description("Get natural language guidance for remediating a compliance finding")]
public async Task<string> GetRemediationGuidanceAsync(
    [Description("Finding ID")] string findingId,
    CancellationToken cancellationToken = default)
{
    var finding = await _complianceEngine.GetFindingAsync(findingId, cancellationToken);
    var guidance = await _aiRemediationService.GetNaturalLanguageGuidanceAsync(finding, cancellationToken);
    
    return guidance.Explanation;
}

[KernelFunction("prioritize_findings")]
[Description("Use AI to prioritize compliance findings based on business context")]
public async Task<string> PrioritizeFindingsAsync(
    [Description("Subscription ID")] string subscriptionId,
    [Description("Business context (e.g., 'Production e-commerce, ATO deadline in 30 days')")] string businessContext = "",
    CancellationToken cancellationToken = default)
{
    var assessment = await _complianceEngine.GetLatestAssessmentAsync(subscriptionId, cancellationToken);
    var prioritized = await _aiRemediationService.PrioritizeFindingsAsync(
        assessment.Findings, businessContext, cancellationToken);
    
    var output = new StringBuilder();
    output.AppendLine("**🎯 AI-Prioritized Findings**");
    output.AppendLine();
    
    foreach (var pf in prioritized.OrderBy(p => p.Priority))
    {
        var finding = assessment.Findings.First(f => f.FindingId == pf.FindingId);
        output.AppendLine($"**Priority {pf.Priority}: {finding.ControlId}**");
        output.AppendLine($"Finding: {finding.Description}");
        output.AppendLine($"Reasoning: {pf.Reasoning}");
        output.AppendLine();
    }
    
    return output.ToString();
}
```

**Service Registration:**

**File:** `src/Platform.Engineering.Copilot.Compliance.Agent/Program.cs`

```csharp
// Add service registration
builder.Services.AddScoped<AiRemediationService>();
```

---

### ✅ **Task 3: Create ComplianceDocumentGenerator** (4 days)
**Status:** New service for AI-powered document generation  
**Files Modified:** 1  
**Files Created:** 2

#### Files to Create

##### **File 1:** `src/Platform.Engineering.Copilot.Compliance.Agent/Services/ComplianceDocumentGenerator.cs`

```csharp
using Microsoft.SemanticKernel.ChatCompletion;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services;

/// <summary>
/// AI-powered compliance document generator
/// Replaces static templates with intelligent, context-aware content
/// </summary>
public class ComplianceDocumentGenerator
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly IComplianceEngine _complianceEngine;
    private readonly INistControlsService _nistService;
    private readonly IDistributedCache _cache; // For caching AI-generated content
    private readonly ILogger<ComplianceDocumentGenerator> _logger;
    
    public ComplianceDocumentGenerator(
        IChatCompletionService chatCompletion,
        IComplianceEngine complianceEngine,
        INistControlsService nistService,
        IDistributedCache cache,
        ILogger<ComplianceDocumentGenerator> logger)
    {
        _chatCompletion = chatCompletion;
        _complianceEngine = complianceEngine;
        _nistService = nistService;
        _cache = cache;
        _logger = logger;
    }
    
    /// <summary>
    /// Generate AI-powered control implementation narrative
    /// </summary>
    public async Task<string> GenerateControlNarrativeAsync(
        string controlId,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"narrative:{controlId}:{subscriptionId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for control narrative: {ControlId}", controlId);
            return cached;
        }
        
        // Get control requirements
        var control = await _nistService.GetControlAsync(controlId, cancellationToken);
        
        // Get actual Azure configurations
        var resources = await _complianceEngine.GetRelevantResourcesAsync(
            subscriptionId, controlId, cancellationToken);
        
        var resourceSummary = string.Join("\n", resources.Select(r => 
            $"- {r.Type}: {r.Name} (Config: {r.CurrentConfiguration})"));
        
        var prompt = $@"You are a compliance documentation expert. Write a detailed 
implementation narrative for NIST control {controlId} based on actual Azure configurations.

**Control Requirements:**
{control.Title}
{control.Description}

**Current Azure Implementation:**
{resourceSummary}

Write a professional narrative (2-3 paragraphs) explaining:
1. HOW this control is implemented in Azure
2. WHAT specific services/configurations enforce it
3. WHY this approach satisfies the control requirements

Use present tense, third person. Be specific about Azure resources.";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(@"You are a FedRAMP compliance expert with deep 
Azure knowledge. Write clear, audit-ready control implementation narratives.");
        chatHistory.AddUserMessage(prompt);
        
        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    ["temperature"] = 0.3,
                    ["max_tokens"] = 500
                }
            },
            cancellationToken: cancellationToken);
        
        var narrative = response.Content ?? string.Empty;
        
        // Cache for 30 days
        await _cache.SetStringAsync(
            cacheKey,
            narrative,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            },
            cancellationToken);
        
        return narrative;
    }
    
    /// <summary>
    /// Generate AI-powered evidence narrative explaining why evidence proves compliance
    /// </summary>
    public async Task<string> GenerateEvidenceNarrativeAsync(
        List<ComplianceEvidence> evidence,
        string controlId,
        CancellationToken cancellationToken = default)
    {
        var evidenceSummary = string.Join("\n", evidence.Select(e => 
            $"- {e.Type}: {e.Name} ({e.CollectedDate:yyyy-MM-dd})"));
        
        var prompt = $@"Explain how the following evidence proves compliance with 
NIST control {controlId}. Write for auditors who need to understand WHY this 
evidence matters.

**Evidence Collected:**
{evidenceSummary}

Write a clear explanation (1-2 paragraphs) covering:
1. What this evidence demonstrates
2. How it satisfies control requirements
3. Why an auditor should accept this as proof";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(@"You are helping auditors understand compliance 
evidence. Explain technical artifacts in business terms.");
        chatHistory.AddUserMessage(prompt);
        
        var response = await _chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        return response.Content ?? string.Empty;
    }
    
    /// <summary>
    /// Generate comprehensive POA&M entry with AI analysis
    /// </summary>
    public async Task<PoamEntry> GenerateSmartPoamEntryAsync(
        AtoFinding finding,
        CancellationToken cancellationToken = default)
    {
        var prompt = $@"Create a detailed Plan of Action & Milestones (POA&M) entry:

**Finding:** {finding.Description}
**Control:** {finding.ControlId}
**Severity:** {finding.Severity}
**Resource:** {finding.ResourceId}

Generate:
1. Root Cause Analysis (1 paragraph)
2. Detailed Remediation Steps (numbered list, 5-7 steps)
3. Resource Requirements (who needs to do what)
4. Realistic Timeline (with milestones)
5. Verification Criteria (how to confirm it's fixed)
6. Risk if not remediated

Format as JSON matching this schema:
{{
  'rootCause': 'string',
  'remediationSteps': ['step1', 'step2', ...],
  'resources': ['role or team'],
  'timeline': {{ 'start': 'date', 'milestones': [], 'completion': 'date' }},
  'verification': 'string',
  'residualRisk': 'string'
}}";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a compliance officer creating POA&M entries. Be thorough and realistic.");
        chatHistory.AddUserMessage(prompt);
        
        var response = await _chatCompletion.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        var poamData = JsonSerializer.Deserialize<PoamData>(ExtractJson(response.Content ?? "{}"));
        
        return new PoamEntry
        {
            FindingId = finding.FindingId,
            ControlId = finding.ControlId,
            Description = finding.Description,
            RootCause = poamData?.RootCause ?? "Unknown",
            RemediationSteps = poamData?.RemediationSteps ?? new List<string>(),
            AssignedTo = poamData?.Resources ?? new List<string>(),
            Timeline = poamData?.Timeline ?? new PoamTimeline(),
            VerificationCriteria = poamData?.Verification ?? "Manual verification required",
            RiskStatement = poamData?.ResidualRisk ?? "Unknown risk",
            GeneratedBy = "AI-GPT4",
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
    
    /// <summary>
    /// Populate template with AI-generated content
    /// </summary>
    public async Task<Dictionary<string, string>> PopulateDocumentTemplateAsync(
        AssessmentResult assessment,
        string templateType, // SSP, SAR, POAM
        CancellationToken cancellationToken = default)
    {
        var files = new Dictionary<string, string>();
        
        // Generate content for each control
        foreach (var control in assessment.Controls)
        {
            var narrative = await GenerateControlNarrativeAsync(
                control.ControlId, assessment.SubscriptionId, cancellationToken);
            
            var evidenceNarrative = control.Evidence.Any()
                ? await GenerateEvidenceNarrativeAsync(control.Evidence, control.ControlId, cancellationToken)
                : "No evidence collected.";
            
            // Build document section
            var section = $@"
## {control.ControlId}: {control.Title}

**Implementation Status:** {control.Status}

### Implementation Description
{narrative}

### Evidence
{evidenceNarrative}

### Findings
{(control.Findings.Any() ? string.Join("\n", control.Findings.Select(f => $"- {f.Description}")) : "No findings identified. Control fully compliant.")}
";
            
            files[$"{control.ControlId}.md"] = section;
        }
        
        return files;
    }
    
    private string ExtractJson(string response)
    {
        var jsonPattern = @"\{.*\}";
        var match = Regex.Match(response, jsonPattern, RegexOptions.Singleline);
        return match.Success ? match.Value : "{}";
    }
}

// Supporting classes
public class PoamData
{
    public string RootCause { get; set; } = string.Empty;
    public List<string> RemediationSteps { get; set; } = new();
    public List<string> Resources { get; set; } = new();
    public PoamTimeline Timeline { get; set; } = new();
    public string Verification { get; set; } = string.Empty;
    public string ResidualRisk { get; set; } = string.Empty;
}

public class PoamTimeline
{
    public string Start { get; set; } = string.Empty;
    public List<PoamMilestone> Milestones { get; set; } = new();
    public string Completion { get; set; } = string.Empty;
}

public class PoamMilestone
{
    public string Date { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
}

public class PoamEntry
{
    public string FindingId { get; set; } = string.Empty;
    public string ControlId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RootCause { get; set; } = string.Empty;
    public List<string> RemediationSteps { get; set; } = new();
    public List<string> AssignedTo { get; set; } = new();
    public PoamTimeline Timeline { get; set; } = new();
    public string VerificationCriteria { get; set; } = string.Empty;
    public string RiskStatement { get; set; } = string.Empty;
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
}
```

##### **File 2:** Add new plugin functions to `CompliancePlugin.cs`

```csharp
// ADD to CompliancePlugin

private readonly ComplianceDocumentGenerator _documentGenerator;

[KernelFunction("generate_ai_ssp")]
[Description("Generate AI-powered System Security Plan with intelligent control narratives")]
public async Task<string> GenerateAiSspAsync(
    [Description("Subscription ID")] string subscriptionId,
    CancellationToken cancellationToken = default)
{
    var assessment = await _complianceEngine.GetLatestAssessmentAsync(subscriptionId, cancellationToken);
    var documents = await _documentGenerator.PopulateDocumentTemplateAsync(
        assessment, "SSP", cancellationToken);
    
    return $"✅ Generated AI-powered SSP with {documents.Count} control narratives";
}

[KernelFunction("generate_smart_poam")]
[Description("Generate AI-enhanced POA&M with root cause analysis and detailed remediation plans")]
public async Task<string> GenerateSmartPoamAsync(
    [Description("Finding ID")] string findingId,
    CancellationToken cancellationToken = default)
{
    var finding = await _complianceEngine.GetFindingAsync(findingId, cancellationToken);
    var poam = await _documentGenerator.GenerateSmartPoamEntryAsync(finding, cancellationToken);
    
    return $@"**AI-Generated POA&M Entry**

**Finding:** {poam.Description}
**Control:** {poam.ControlId}

**Root Cause:**
{poam.RootCause}

**Remediation Steps:**
{string.Join("\n", poam.RemediationSteps.Select((s, i) => $"{i + 1}. {s}"))}

**Timeline:** {poam.Timeline.Start} → {poam.Timeline.Completion}

**Verification:** {poam.VerificationCriteria}

**Risk if Not Remediated:** {poam.RiskStatement}";
}
```

**Service Registration:**

```csharp
// In Program.cs
builder.Services.AddScoped<ComplianceDocumentGenerator>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

---

### ✅ **Task 4: Create ComplianceMonitoringService** (4 days)
**Status:** New service for real-time monitoring  
**Files Modified:** 1  
**Files Created:** 4

#### Files to Create

##### **File 1:** `src/Platform.Engineering.Copilot.Compliance.Agent/Services/ComplianceMonitoringService.cs`

```csharp
using Platform.Engineering.Copilot.Core.Data;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services;

public class ComplianceMonitoringService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ComplianceMonitoringService> _logger;
    
    public async Task<ComplianceDashboard> GetDashboardDataAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var latestAssessment = await _dbContext.ComplianceAssessments
            .Where(a => a.SubscriptionId == subscriptionId)
            .OrderByDescending(a => a.AssessmentDate)
            .FirstOrDefaultAsync(cancellationToken);
        
        var trendData = await GetComplianceTrendAsync(subscriptionId, 30, cancellationToken);
        
        var activeFindings = await _dbContext.ComplianceFindings
            .Where(f => f.SubscriptionId == subscriptionId && f.Status != "Resolved")
            .ToListAsync(cancellationToken);
        
        return new ComplianceDashboard
        {
            OverallScore = latestAssessment?.OverallComplianceScore ?? 0,
            ScoreTrend = CalculateTrend(trendData),
            CriticalFindings = activeFindings.Count(f => f.Severity == "Critical"),
            HighFindings = activeFindings.Count(f => f.Severity == "High"),
            MediumFindings = activeFindings.Count(f => f.Severity == "Medium"),
            LowFindings = activeFindings.Count(f => f.Severity == "Low"),
            ControlFamilyScores = GetControlFamilyScores(latestAssessment),
            RecentActivity = await GetRecentActivityAsync(subscriptionId, cancellationToken),
            Alerts = await GenerateAlertsAsync(subscriptionId, trendData, cancellationToken)
        };
    }
    
    private async Task<List<ComplianceAlert>> GenerateAlertsAsync(
        string subscriptionId,
        List<ComplianceTrend> trendData,
        CancellationToken cancellationToken)
    {
        var alerts = new List<ComplianceAlert>();
        
        // Alert on score degradation
        if (trendData.Count >= 2)
        {
            var currentScore = trendData.Last().Score;
            var previousScore = trendData[^2].Score;
            
            if (currentScore < previousScore - 5)
            {
                alerts.Add(new ComplianceAlert
                {
                    Severity = "Warning",
                    Title = "Compliance Score Decreased",
                    Description = $"Score dropped from {previousScore:F1}% to {currentScore:F1}%",
                    ActionRequired = "Review recent changes and new findings"
                });
            }
        }
        
        // Alert on new critical findings
        var newCritical = await _dbContext.ComplianceFindings
            .Where(f => f.SubscriptionId == subscriptionId 
                && f.Severity == "Critical" 
                && f.IdentifiedDate > DateTimeOffset.UtcNow.AddHours(-24))
            .CountAsync(cancellationToken);
        
        if (newCritical > 0)
        {
            alerts.Add(new ComplianceAlert
            {
                Severity = "Critical",
                Title = $"{newCritical} New Critical Findings",
                Description = "Immediate remediation required"
            });
        }
        
        return alerts;
    }
}
```

##### **File 2:** SignalR Hub for real-time updates

```csharp
// src/Platform.Engineering.Copilot.Mcp/Hubs/ComplianceHub.cs
public class ComplianceHub : Hub
{
    private readonly ComplianceMonitoringService _monitoringService;
    
    public async Task SubscribeToSubscription(string subscriptionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"subscription:{subscriptionId}");
        var dashboard = await _monitoringService.GetDashboardDataAsync(subscriptionId);
        await Clients.Caller.SendAsync("DashboardUpdate", dashboard);
    }
}
```

##### **File 3:** Background service for monitoring

```csharp
// src/Platform.Engineering.Copilot.Mcp/Services/ComplianceMonitoringBackgroundService.cs
public class ComplianceMonitoringBackgroundService : BackgroundService
{
    private readonly IHubContext<ComplianceHub> _hubContext;
    private readonly ComplianceMonitoringService _monitoringService;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            
            var subscriptions = await GetMonitoredSubscriptionsAsync(stoppingToken);
            
            foreach (var subscriptionId in subscriptions)
            {
                var dashboard = await _monitoringService.GetDashboardDataAsync(
                    subscriptionId, stoppingToken);
                
                await _hubContext.Clients.Group($"subscription:{subscriptionId}")
                    .SendAsync("DashboardUpdate", dashboard, stoppingToken);
            }
        }
    }
}
```

##### **File 4:** Plugin function

```csharp
// Add to CompliancePlugin
[KernelFunction("get_compliance_dashboard")]
[Description("Get real-time compliance dashboard with scores, trends, and alerts")]
public async Task<string> GetComplianceDashboardAsync(
    [Description("Subscription ID")] string subscriptionId,
    CancellationToken cancellationToken = default)
{
    var dashboard = await _monitoringService.GetDashboardDataAsync(subscriptionId, cancellationToken);
    
    return $@"**📊 Compliance Dashboard**

**Overall Score:** {dashboard.OverallScore:F1}% {GetTrendEmoji(dashboard.ScoreTrend)}

**Active Findings:**
🔴 Critical: {dashboard.CriticalFindings}
🟡 High: {dashboard.HighFindings}
🟨 Medium: {dashboard.MediumFindings}
⚪ Low: {dashboard.LowFindings}

**Alerts:**
{(dashboard.Alerts.Any() ? string.Join("\n", dashboard.Alerts.Select(a => $"- {a.Title}")) : "None")}";
}
```

**Service Registration:**

```csharp
// Program.cs
builder.Services.AddSignalR();
builder.Services.AddScoped<ComplianceMonitoringService>();
builder.Services.AddHostedService<ComplianceMonitoringBackgroundService>();

app.MapHub<ComplianceHub>("/compliance-hub");
```

---

## Summary of Changes

### Component Responsibilities

| Component | Responsibility |
|-----------|----------------|
| **ATORemediationEngine** (Existing) | - Source of truth for remediation capabilities<br>- Executes actual Azure API calls<br>- Validates remediation is possible<br>- Tracks remediation state<br>- Provides resource state information |
| **AiRemediationService** (New) | - Queries ATORemediationEngine for capabilities<br>- Generates user-friendly scripts (PowerShell/CLI/Terraform)<br>- Explains WHY remediation is needed<br>- Provides step-by-step guidance<br>- Prioritizes findings with business context |
| **ComplianceDocumentGenerator** (New) | - AI-powered control narratives<br>- Evidence synthesis & explanation<br>- Smart POA&M generation<br>- Risk assessment narratives |
| **ComplianceMonitoringService** (New) | - Real-time dashboard data<br>- Trend analysis<br>- Automated alerts<br>- SignalR push notifications |

### Files to Create (9)
1. `AiRemediationService.cs`
2. `RemediationScript.cs` (models)
3. `ComplianceDocumentGenerator.cs`
4. `PoamEntry.cs` (models)
5. `ComplianceMonitoringService.cs`
6. `ComplianceDashboard.cs` (models)
7. `ComplianceHub.cs` (SignalR)
8. `ComplianceMonitoringBackgroundService.cs`
9. `TIER3-AI-INTEGRATION.md` (documentation)

### Files to Modify (3)
1. `Program.cs` - Add Azure OpenAI, service registrations, SignalR
2. `CompliancePlugin.cs` - Add 7 new kernel functions
3. `appsettings.json` - Add Azure OpenAI configuration

### New Dependencies
```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.1" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.0.1" />
<PackageReference Include="Azure.Identity" Version="1.10.4" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
```

### Azure Resources Required
1. **Azure OpenAI Service**
   - Deployment: gpt-4-turbo
   - Region: East US 2
   - Managed Identity enabled

2. **Redis Cache** (for AI content caching)
   - SKU: Basic C1
   - Purpose: Cache AI-generated narratives

3. **Key Vault** (TIER 1 dependency)
   - Store OpenAI endpoint URL (if needed)

### Testing Plan
1. **Unit Tests:** AI service mocking, template generation
2. **Integration Tests:** End-to-end document generation
3. **Performance Tests:** AI response times, caching effectiveness
4. **User Acceptance:** Compare AI vs manual narratives

### Cost Estimates
- **Azure OpenAI:** ~$2-5 per SSP generation (first time)
- **Redis Cache:** ~$0.02/hour (~$15/month)
- **Cached SSP regeneration:** ~$0.10 (cache hits)

### Success Metrics
| Metric | Target |
|--------|--------|
| AI narrative quality | 90%+ audit acceptance rate |
| Document generation time | < 2 minutes (vs 2+ hours manual) |
| Cache hit rate | > 70% for unchanged controls |
| POA&M completeness | 100% fields populated |
| User satisfaction | 4.5/5 stars |

---

## Implementation Timeline

### Week 1-2: Core AI Services
- Day 1-2: Fix AI Chat Completion (Task 1)
- Day 3-5: AiRemediationService (Task 2)
- Day 6-7: Testing & validation

### Week 3: Document Generation
- Day 8-11: ComplianceDocumentGenerator (Task 3)
- Day 12-13: Template integration
- Day 14: Testing

### Week 4: Monitoring & Polish
- Day 15-18: ComplianceMonitoringService (Task 4)
- Day 19-20: SignalR hub & background service
- Day 21: Integration testing
- Day 22-23: Documentation
- Day 24-25: User acceptance testing
- Day 26-28: Bug fixes & polish

---

## Next Steps

1. **Review & Approve Plan** - Stakeholder sign-off on approach
2. **Provision Azure Resources** - Create OpenAI service, Redis
3. **Create Feature Branch** - `feature/tier3-ai-enhanced`
4. **Begin Task 1** - Fix AI Chat Completion Service
5. **Daily Standups** - Track progress, blockers

---

*Version: 1.0*  
*Created: November 25, 2025*  
*Owner: Platform Engineering Team*
