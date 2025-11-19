# Agent RAG Integration Examples

## Overview

This guide shows **how existing agents integrate with the new RAG completion service** to enhance their responses with relevant documentation and context while tracking token usage for cost optimization.

## How It Works

The RAG (Retrieval-Augmented Generation) service integrates seamlessly with your existing agent architecture:

```
┌─────────────┐
│ User Query  │
└──────┬──────┘
       │
       ▼
┌──────────────────┐      ┌───────────────────┐
│ InfrastructureAgent│     │ Vector Search     │
│ ComplianceAgent    │────▶│ (Knowledge Base)  │
│ CostManagementAgent│     └─────────┬─────────┘
└──────┬───────────┘                │
       │                            │
       │                    ┌───────▼────────┐
       │                    │  RAG Results   │
       │                    │  (Top 5 docs)  │
       │                    └───────┬────────┘
       │                            │
       ▼                            │
┌──────────────────────────────────▼┐
│   TokenManagementHelper            │
│   .GetRagCompletionAsync()         │
│                                    │
│   Combines:                        │
│   • Agent's system prompt          │
│   • RAG context (docs)            │
│   • Conversation history          │
│   • User question                 │
│                                    │
│   Tracks tokens separately:        │
│   • System: 500 tokens            │
│   • RAG: 1200 tokens              │
│   • History: 800 tokens           │
│   • User: 150 tokens              │
└──────────────┬─────────────────────┘
               │
               ▼
       ┌───────────────┐
       │ AI Response   │
       │ + Token Stats │
       └───────────────┘
```

## Integration Pattern

### Step 1: Inject TokenManagementHelper

Your agents already have dependencies injected. Just add `TokenManagementHelper`:

```csharp
public class InfrastructureAgent : ISpecializedAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<InfrastructureAgent> _logger;
    private readonly InfrastructurePlugin _infrastructurePlugin;
    private readonly TokenManagementHelper _tokenHelper;  // ✅ ADD THIS

    public InfrastructureAgent(
        ISemanticKernelService semanticKernelService,
        ILogger<InfrastructureAgent> logger,
        ILoggerFactory loggerFactory,
        // ... existing dependencies ...
        TokenManagementHelper tokenHelper)  // ✅ ADD THIS PARAMETER
    {
        _logger = logger;
        _tokenHelper = tokenHelper;  // ✅ STORE IT
        
        // ... rest of initialization ...
    }
}
```

### Step 2: Add Vector Search to Your Agent

Before calling RAG completion, search your knowledge base for relevant docs:

```csharp
public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
{
    _logger.LogInformation("🏗️ Infrastructure Agent processing task: {TaskId}", task.TaskId);

    try
    {
        // ✅ NEW: Search knowledge base for relevant documentation
        var ragResults = await SearchKnowledgeBase(task.UserPrompt);
        
        // ✅ NEW: Build RAG-powered completion request
        var ragRequest = new ChatCompletionRequest
        {
            SystemPrompt = GetInfrastructureSystemPrompt(),
            UserPrompt = task.UserPrompt,
            RagResults = ragResults,
            ConversationContext = task.ConversationContext,
            ModelName = "gpt-4o",
            Temperature = 0.7,
            MaxTokens = 4096,
            IncludeConversationHistory = true,
            MaxHistoryTokens = 3000,
            MaxHistoryMessages = 10
        };
        
        // ✅ NEW: Get completion with RAG context and token tracking
        var ragResponse = await _tokenHelper.GetRagCompletionAsync(ragRequest);
        
        if (!ragResponse.Success)
        {
            _logger.LogError("RAG completion failed: {Error}", ragResponse.ErrorMessage);
            // Fall back to original agent logic
            return await ProcessWithoutRAG(task, memory);
        }
        
        // ✅ NEW: Log token breakdown for cost analysis
        _logger.LogInformation("Token usage: {TokenSummary}", 
            ragResponse.TokenUsage.GetCompactSummary());
        
        // ✅ NEW: Return enhanced response
        return new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.Infrastructure,
            Success = true,
            Content = ragResponse.Content,
            ExecutionTimeMs = ragResponse.ProcessingTimeMs,
            Metadata = new Dictionary<string, object>
            {
                ["tokenUsage"] = ragResponse.TokenUsage,
                ["estimatedCost"] = ragResponse.TokenUsage.CalculateEstimatedCost(),
                ["ragResultsUsed"] = ragResponse.IncludedRagResults,
                ["historyMessagesUsed"] = ragResponse.IncludedHistory
            }
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Infrastructure Agent failed on task: {TaskId}", task.TaskId);
        return CreateErrorResponse(task.TaskId, ex.Message);
    }
}

private async Task<List<VectorSearchResult>> SearchKnowledgeBase(string query)
{
    // Use your existing vector search service
    // This example assumes you have a vector search implementation
    var results = await _vectorSearchService.SearchAsync(
        query: query,
        indexName: "azure-infrastructure-docs",
        top: 5  // Get top 5 most relevant documents
    );
    
    return results;
}
```

### Step 3: Vector Search Results

Your vector search should return results in this format:

```csharp
public class VectorSearchResult
{
    public string Content { get; set; }      // The actual documentation text
    public double Score { get; set; }        // Relevance score (0.0-1.0)
    public string Source { get; set; }       // Source reference (file path, URL)
    public Dictionary<string, object>? Metadata { get; set; }  // Optional metadata
}

// Example result:
var ragResults = new List<VectorSearchResult>
{
    new VectorSearchResult 
    { 
        Content = @"Azure Kubernetes Service (AKS) is a managed container orchestration service...
                   Best practices include enabling RBAC, using Azure AD integration, 
                   implementing network policies, and enabling Azure Policy for compliance...",
        Score = 0.94,
        Source = "docs/azure/aks-best-practices.md"
    },
    new VectorSearchResult 
    { 
        Content = @"For production AKS clusters, consider enabling:
                   - Azure Monitor Container Insights for observability
                   - Azure Defender for container security scanning
                   - Workload Identity for pod-to-Azure authentication...",
        Score = 0.89,
        Source = "docs/azure/aks-production-checklist.md"
    }
};
```

## Complete Example: Infrastructure Agent with RAG

Here's a complete implementation showing RAG integration:

```csharp
using Platform.Engineering.Copilot.Core.Interfaces.Chat;
using Platform.Engineering.Copilot.Core.Interfaces.Search;
using Platform.Engineering.Copilot.Core.Models.Chat;
using Platform.Engineering.Copilot.Core.Services.TokenManagement;

namespace Platform.Engineering.Copilot.Infrastructure.Core;

public class InfrastructureAgent : ISpecializedAgent
{
    public AgentType AgentType => AgentType.Infrastructure;

    private readonly Kernel _kernel;
    private readonly ILogger<InfrastructureAgent> _logger;
    private readonly TokenManagementHelper _tokenHelper;
    private readonly IVectorSearchService _vectorSearch;
    private readonly InfrastructurePlugin _infrastructurePlugin;

    public InfrastructureAgent(
        ISemanticKernelService semanticKernelService,
        ILogger<InfrastructureAgent> logger,
        TokenManagementHelper tokenHelper,
        IVectorSearchService vectorSearchService,
        InfrastructurePlugin infrastructurePlugin)
    {
        _logger = logger;
        _tokenHelper = tokenHelper;
        _vectorSearch = vectorSearchService;
        _infrastructurePlugin = infrastructurePlugin;
        
        _kernel = semanticKernelService.CreateSpecializedKernel(AgentType.Infrastructure);
        _kernel.Plugins.AddFromObject(_infrastructurePlugin, "Infrastructure");
    }

    public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🏗️ Infrastructure Agent processing: {Query}", task.UserPrompt);

        try
        {
            // Step 1: Determine if this is a documentation/guidance query or a provisioning request
            if (IsDocumentationQuery(task.UserPrompt))
            {
                return await ProcessWithRAG(task, memory);
            }
            else
            {
                return await ProcessWithFunctionCalling(task, memory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Infrastructure Agent error");
            return CreateErrorResponse(task, ex);
        }
    }

    /// <summary>
    /// Process documentation/guidance queries using RAG
    /// </summary>
    private async Task<AgentResponse> ProcessWithRAG(AgentTask task, SharedMemory memory)
    {
        _logger.LogInformation("📚 Using RAG for documentation query");

        // 1. Search knowledge base for relevant docs
        var ragResults = await _vectorSearch.SearchAsync(
            query: task.UserPrompt,
            indexName: "azure-infrastructure-docs",
            top: 5
        );

        _logger.LogInformation("📄 Found {Count} relevant documents", ragResults.Count);

        // 2. Build RAG completion request
        var request = new ChatCompletionRequest
        {
            SystemPrompt = GetDocumentationSystemPrompt(),
            UserPrompt = task.UserPrompt,
            RagResults = ragResults,
            ConversationContext = task.ConversationContext,
            ModelName = "gpt-4o",
            Temperature = 0.7,
            MaxTokens = 3000,
            IncludeConversationHistory = true,
            MaxHistoryTokens = 2000,
            MaxHistoryMessages = 8
        };

        // 3. Get RAG-enhanced completion
        var response = await _tokenHelper.GetRagCompletionAsync(request);

        if (!response.Success)
        {
            _logger.LogError("RAG completion failed: {Error}", response.ErrorMessage);
            return CreateErrorResponse(task, new Exception(response.ErrorMessage));
        }

        // 4. Log cost metrics
        var cost = response.TokenUsage.CalculateEstimatedCost();
        _logger.LogInformation(
            "💰 Token usage: Total={Total}, Cost=${Cost:F4}, RAG={RAG}, History={History}",
            response.TokenUsage.TotalTokens,
            cost,
            response.TokenUsage.RagContextTokens,
            response.TokenUsage.ConversationHistoryTokens
        );

        // 5. Return enhanced response
        return new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.Infrastructure,
            Success = true,
            Content = response.Content,
            ExecutionTimeMs = response.ProcessingTimeMs,
            Metadata = new Dictionary<string, object>
            {
                ["mode"] = "rag",
                ["tokenUsage"] = response.TokenUsage.GetSummary(),
                ["estimatedCost"] = cost,
                ["ragSources"] = ragResults.Select(r => r.Source).ToList(),
                ["ragResultsIncluded"] = response.IncludedRagResults,
                ["historyMessagesIncluded"] = response.IncludedHistory
            }
        };
    }

    /// <summary>
    /// Process provisioning requests using function calling
    /// </summary>
    private async Task<AgentResponse> ProcessWithFunctionCalling(AgentTask task, SharedMemory memory)
    {
        _logger.LogInformation("🔧 Using function calling for provisioning request");
        
        // Use existing function calling logic
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(GetProvisioningSystemPrompt());
        chatHistory.AddUserMessage(task.UserPrompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 4096,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var result = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory, 
            settings, 
            _kernel);

        return new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.Infrastructure,
            Success = true,
            Content = result.Content,
            Metadata = new Dictionary<string, object>
            {
                ["mode"] = "function-calling"
            }
        };
    }

    private bool IsDocumentationQuery(string query)
    {
        var documentationKeywords = new[]
        {
            "what is", "how do", "explain", "difference between",
            "best practice", "recommend", "should i", "when to",
            "guide", "tutorial", "example", "comparison"
        };

        var lowerQuery = query.ToLower();
        return documentationKeywords.Any(keyword => lowerQuery.Contains(keyword));
    }

    private string GetDocumentationSystemPrompt()
    {
        return @"You are an Azure infrastructure expert providing guidance and documentation.

Use the provided documentation to answer questions about:
- Azure services and their capabilities
- Best practices and design patterns
- Security and compliance recommendations
- Cost optimization strategies
- Troubleshooting and diagnostics

Always cite sources from the provided documentation.
Provide practical, production-ready recommendations.
Consider security, scalability, and cost in your guidance.";
    }

    private string GetProvisioningSystemPrompt()
    {
        return @"You are an Azure infrastructure specialist that provisions resources.

Use the available functions to:
- Generate infrastructure templates (Bicep/Terraform)
- Deploy resources to Azure
- Configure networking and security
- Implement compliance controls

Always call the appropriate functions - never write manual code.";
    }
}
```

## Complete Example: Compliance Agent with RAG

```csharp
public class ComplianceAgent : ISpecializedAgent
{
    public AgentType AgentType => AgentType.Compliance;

    private readonly Kernel _kernel;
    private readonly ILogger<ComplianceAgent> _logger;
    private readonly TokenManagementHelper _tokenHelper;
    private readonly IVectorSearchService _vectorSearch;

    public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
    {
        _logger.LogInformation("🛡️ Compliance Agent processing: {Query}", task.UserPrompt);

        // Search compliance documentation
        var ragResults = await _vectorSearch.SearchAsync(
            query: task.UserPrompt,
            indexName: "compliance-policies",  // Compliance-specific knowledge base
            top: 10  // More results for comprehensive compliance check
        );

        var request = new ChatCompletionRequest
        {
            SystemPrompt = GetComplianceSystemPrompt(),
            UserPrompt = task.UserPrompt,
            RagResults = ragResults,
            ConversationContext = task.ConversationContext,
            ModelName = "gpt-4o",
            Temperature = 0.3,  // Lower temperature for compliance (more deterministic)
            MaxTokens = 4000,
            IncludeConversationHistory = true,
            MaxHistoryTokens = 2500
        };

        var response = await _tokenHelper.GetRagCompletionAsync(request);

        if (!response.Success)
        {
            return CreateErrorResponse(task, new Exception(response.ErrorMessage));
        }

        // Extract compliance score from response
        var complianceScore = ExtractComplianceScore(response.Content);

        return new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.Compliance,
            Success = true,
            Content = response.Content,
            ComplianceScore = complianceScore,
            IsApproved = complianceScore >= 80,
            ExecutionTimeMs = response.ProcessingTimeMs,
            Metadata = new Dictionary<string, object>
            {
                ["tokenUsage"] = response.TokenUsage.GetCompactSummary(),
                ["estimatedCost"] = response.TokenUsage.CalculateEstimatedCost(),
                ["policyDocumentsReviewed"] = response.IncludedRagResults,
                ["complianceFrameworks"] = ragResults
                    .Select(r => ExtractFramework(r.Source))
                    .Distinct()
                    .ToList()
            }
        };
    }

    private string GetComplianceSystemPrompt()
    {
        return @"You are a compliance and security assessment expert.

Use the provided compliance policies and standards to:
- Assess infrastructure configurations against NIST 800-53, FedRAMP, DoD IL5
- Identify security vulnerabilities and misconfigurations
- Generate compliance scores (0-100%)
- Recommend remediation steps
- Validate evidence and documentation

Provide detailed findings with:
- Control number references
- Severity ratings (Low/Moderate/High/Critical)
- Specific remediation guidance
- Evidence requirements

Always cite specific policy sections from provided documentation.";
    }
}
```

## Cost Management Agent with Budget Tracking

```csharp
public class CostManagementAgent : ISpecializedAgent
{
    public AgentType AgentType => AgentType.CostManagement;

    private readonly TokenManagementHelper _tokenHelper;
    private readonly IVectorSearchService _vectorSearch;
    private readonly ILogger<CostManagementAgent> _logger;
    private decimal _dailyTokenBudget = 10.00m;  // $10/day budget
    private decimal _currentSpending = 0m;

    public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
    {
        _logger.LogInformation("💰 Cost Management Agent processing: {Query}", task.UserPrompt);

        // Check budget before processing
        if (_currentSpending >= _dailyTokenBudget)
        {
            _logger.LogWarning("⚠️ Daily token budget exceeded: ${Current:F2} / ${Budget:F2}", 
                _currentSpending, _dailyTokenBudget);
            
            return new AgentResponse
            {
                Success = false,
                Content = "Daily token budget exceeded. Request queued for tomorrow.",
                Metadata = new Dictionary<string, object>
                {
                    ["budgetExceeded"] = true,
                    ["currentSpending"] = _currentSpending,
                    ["dailyBudget"] = _dailyTokenBudget
                }
            };
        }

        // Search cost optimization documentation
        var ragResults = await _vectorSearch.SearchAsync(
            query: task.UserPrompt,
            indexName: "cost-optimization-docs",
            top: 8
        );

        var request = new ChatCompletionRequest
        {
            SystemPrompt = GetCostOptimizationSystemPrompt(),
            UserPrompt = task.UserPrompt,
            RagResults = ragResults,
            ConversationContext = task.ConversationContext,
            ModelName = "gpt-4o",
            Temperature = 0.5,
            MaxTokens = 3000,
            MaxHistoryTokens = 2000
        };

        var response = await _tokenHelper.GetRagCompletionAsync(request);

        if (!response.Success)
        {
            return CreateErrorResponse(task, new Exception(response.ErrorMessage));
        }

        // Track spending
        var requestCost = response.TokenUsage.CalculateEstimatedCost();
        _currentSpending += requestCost;

        _logger.LogInformation(
            "💵 Request cost: ${Cost:F4} | Daily total: ${Total:F4} / ${Budget:F2}",
            requestCost, _currentSpending, _dailyTokenBudget
        );

        return new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.CostManagement,
            Success = true,
            Content = response.Content,
            ExecutionTimeMs = response.ProcessingTimeMs,
            Metadata = new Dictionary<string, object>
            {
                ["requestCost"] = requestCost,
                ["dailySpending"] = _currentSpending,
                ["budgetRemaining"] = _dailyTokenBudget - _currentSpending,
                ["tokenBreakdown"] = response.TokenUsage.GetTokenBreakdownPercentages(),
                ["optimizationRecommendations"] = ExtractOptimizationSteps(response.Content)
            }
        };
    }
}
```

## Token Optimization Strategies by Agent

### Infrastructure Agent

```csharp
// Optimize for infrastructure queries
var request = new ChatCompletionRequest
{
    SystemPrompt = GetOptimizedSystemPrompt(),  // Keep concise
    UserPrompt = task.UserPrompt,
    RagResults = ragResults.Take(5).ToList(),   // Limit to top 5 docs
    MaxHistoryTokens = 2000,                    // Moderate history
    MaxTokens = 3000,                           // Generous completion
    Temperature = 0.7
};

private string GetOptimizedSystemPrompt()
{
    // ✅ OPTIMIZED (40 tokens)
    return "Azure infrastructure expert. Use provided docs for compute, networking, storage, security. Cite sources, optimize for cost/security.";
    
    // ❌ VERBOSE (100 tokens)
    // return @"You are an expert Azure infrastructure architect with extensive experience...
    //          Provide detailed explanations with comprehensive examples...
    //          Always consider all aspects of cloud architecture...";
}
```

### Compliance Agent

```csharp
// Optimize for compliance assessments
var request = new ChatCompletionRequest
{
    SystemPrompt = GetCompliancePrompt(),
    UserPrompt = task.UserPrompt,
    RagResults = ragResults.Take(10).ToList(),  // More docs for compliance
    MaxHistoryTokens = 1500,                    // Less history needed
    MaxTokens = 4000,                           // Detailed compliance reports
    Temperature = 0.3                           // Low temperature for precision
};
```

### Cost Management Agent

```csharp
// Most aggressive optimization
var request = new ChatCompletionRequest
{
    SystemPrompt = GetCostPrompt(),
    UserPrompt = task.UserPrompt,
    RagResults = OptimizeRagResults(ragResults), // Smart truncation
    MaxHistoryTokens = 1000,                     // Minimal history
    MaxTokens = 2000,                            // Focused completion
    Temperature = 0.5
};

private List<VectorSearchResult> OptimizeRagResults(List<VectorSearchResult> results)
{
    // Extract only relevant snippets instead of full documents
    return results.Take(5).Select(r => new VectorSearchResult
    {
        Content = ExtractRelevantSnippet(r.Content, maxLength: 300),
        Score = r.Score,
        Source = r.Source
    }).ToList();
}
```

## Monitoring and Analytics

### Track Token Usage Across Agents

```csharp
public class TokenUsageMonitor
{
    private readonly Dictionary<AgentType, List<TokenUsageMetrics>> _usageByAgent = new();

    public void RecordUsage(AgentType agentType, TokenUsageMetrics metrics)
    {
        if (!_usageByAgent.ContainsKey(agentType))
        {
            _usageByAgent[agentType] = new List<TokenUsageMetrics>();
        }
        
        _usageByAgent[agentType].Add(metrics);
    }

    public void PrintDailySummary()
    {
        Console.WriteLine("📊 Daily Token Usage Summary");
        Console.WriteLine("════════════════════════════");
        
        foreach (var (agentType, usages) in _usageByAgent)
        {
            var totalTokens = usages.Sum(u => u.TotalTokens);
            var totalCost = usages.Sum(u => u.CalculateEstimatedCost());
            var requestCount = usages.Count;
            var avgTokensPerRequest = totalTokens / requestCount;
            
            Console.WriteLine($"\n{agentType} Agent:");
            Console.WriteLine($"  Requests: {requestCount}");
            Console.WriteLine($"  Total Tokens: {totalTokens:N0}");
            Console.WriteLine($"  Avg Tokens/Request: {avgTokensPerRequest:N0}");
            Console.WriteLine($"  Total Cost: ${totalCost:F4}");
            
            // Component breakdown
            var avgSystemTokens = usages.Average(u => u.SystemPromptTokens);
            var avgRagTokens = usages.Average(u => u.RagContextTokens);
            var avgHistoryTokens = usages.Average(u => u.ConversationHistoryTokens);
            
            Console.WriteLine($"  Avg Breakdown:");
            Console.WriteLine($"    System: {avgSystemTokens:N0} tokens");
            Console.WriteLine($"    RAG: {avgRagTokens:N0} tokens");
            Console.WriteLine($"    History: {avgHistoryTokens:N0} tokens");
        }
    }
}
```

## Key Takeaways

1. **RAG is Optional**: Use RAG for documentation/guidance queries, function calling for provisioning
2. **Inject TokenManagementHelper**: Already available through DI, just add to constructor
3. **Search Before Completing**: Always search your knowledge base first for relevant context
4. **Track Everything**: Log token breakdowns for cost optimization insights
5. **Optimize Per Agent**: Different agents have different token budget needs
6. **Monitor Daily Spending**: Set budgets and track usage across all agents
7. **Separate System Prompts**: Use different prompts for RAG vs function calling modes

## Next Steps

1. Add `TokenManagementHelper` to your agent constructors
2. Implement vector search integration for your knowledge bases
3. Add RAG processing logic for documentation queries
4. Monitor token usage and optimize based on metrics
5. Set up daily budget tracking and alerts
