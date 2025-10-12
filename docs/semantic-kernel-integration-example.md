# Semantic Kernel Auto Function Calling Integration Example

This example demonstrates how to use Semantic Kernel's **Auto Function Calling** feature to automatically extract entities from natural language prompts and invoke the OnboardingPlugin with structured data.

> **Note**: This uses Semantic Kernel's modern auto function calling approach (via `ToolCallBehavior.AutoInvokeKernelFunctions`), not the older planner patterns which are now deprecated.

## Prerequisites

```bash
dotnet add package Microsoft.SemanticKernel
# That's it! No additional planner packages needed for auto function calling
```

## Complete Working Example

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Platform.Engineering.Copilot.Core.Plugins;
using Platform.Engineering.Copilot.Core.Interfaces;

public class OnboardingChatInterface
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<OnboardingChatInterface> _logger;

    public OnboardingChatInterface(
        string azureOpenAIEndpoint,
        string azureOpenAIKey,
        string deploymentName,
        IOnboardingService onboardingService,
        ILogger<OnboardingChatInterface> logger)
    {
        _logger = logger;

        // 1. Build Semantic Kernel with Azure OpenAI
        var builder = Kernel.CreateBuilder();
        
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: deploymentName,
            endpoint: azureOpenAIEndpoint,
            apiKey: azureOpenAIKey);

        builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));

        _kernel = builder.Build();
        
        // 2. Register OnboardingPlugin
        var onboardingPlugin = new OnboardingPlugin(
            logger: logger,
            kernel: _kernel,
            onboardingService: onboardingService);

        _kernel.Plugins.AddFromObject(onboardingPlugin, "OnboardingPlugin");

        _chatService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    /// <summary>
    /// Process natural language onboarding request with automatic entity extraction
    /// </summary>
    public async Task<string> ProcessOnboardingRequestAsync(string userPrompt)
    {
        _logger.LogInformation("Processing onboarding request: {Prompt}", userPrompt);

        // Build chat history with system prompt for entity extraction guidance
        var chatHistory = new ChatHistory();
        
        chatHistory.AddSystemMessage(@"
You are an AI assistant for Navy Flankspeed mission onboarding. When users describe 
onboarding requirements, you should extract structured information and use the available 
onboarding plugin functions.

When creating or updating onboarding requests, extract these fields:
- missionName: Name of the mission/project
- missionOwner: Full name of the mission owner
- missionOwnerEmail: Email address (.mil domain)
- missionOwnerRank: Military rank (CDR, LCDR, GS-14, etc.)
- command: Navy command (NAVWAR, SPAWAR, NIWC, etc.)
- classificationLevel: UNCLASS, SECRET, or TS
- requestedSubscriptionName: Desired Azure subscription name
- requestedVNetCidr: VNet CIDR block (e.g., 10.100.0.0/16)
- requiredServices: Array of Azure services needed
- businessJustification: Why this mission needs the environment
- region: Azure Government region (usgovvirginia, usgovtexas)

Use the process_onboarding_query function with the additionalContext parameter to pass 
extracted data as JSON.
");

        chatHistory.AddUserMessage(userPrompt);

        // Enable automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.1, // Low temperature for consistent extraction
            MaxTokens = 2000
        };

        // Let SK automatically extract entities and call plugin
        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            _kernel);

        _logger.LogInformation("Onboarding response: {Response}", response.Content);
        
        return response.Content ?? "No response generated";
    }

    /// <summary>
    /// Multi-turn conversation example with context retention
    /// </summary>
    public async Task<List<string>> ProcessConversationAsync(List<string> userMessages)
    {
        var responses = new List<string>();
        var chatHistory = new ChatHistory();

        chatHistory.AddSystemMessage(@"
You are an AI assistant for Navy Flankspeed mission onboarding. 
Help users through the multi-step onboarding process by extracting information 
incrementally and building up the request details across multiple turns.

Available functions:
- process_onboarding_query: Main function for all onboarding operations
  - Supports: draft creation, updates, submission, approval, status checks, reporting

Remember request IDs from previous turns and use them in subsequent operations.
");

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.1,
            MaxTokens = 2000
        };

        foreach (var userMessage in userMessages)
        {
            _logger.LogInformation("User: {Message}", userMessage);
            
            chatHistory.AddUserMessage(userMessage);

            var response = await _chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);

            var assistantMessage = response.Content ?? "No response";
            chatHistory.AddAssistantMessage(assistantMessage);
            
            responses.Add(assistantMessage);
            _logger.LogInformation("Assistant: {Response}", assistantMessage);
        }

        return responses;
    }
}
```

## Usage Example 1: Single Rich Prompt

```csharp
var chatInterface = new OnboardingChatInterface(
    azureOpenAIEndpoint: "https://your-openai.openai.azure.com/",
    azureOpenAIKey: "your-api-key",
    deploymentName: "gpt-4",
    onboardingService: onboardingService,
    logger: logger);

var userPrompt = @"
I need to onboard a new mission called 'Tactical Edge Platform' for NAVWAR. 
I'm Commander Sarah Johnson (sarah.johnson@navy.mil) from the Navy. 
We need to deploy a microservices architecture with AKS, Azure SQL, and Redis. 
Classification level is SECRET. The VNet CIDR should be 10.200.0.0/16 and the 
subscription should be called 'tactical-edge-prod'. This is for a mission-critical 
tactical data sharing platform that will support real-time intelligence operations.
";

var response = await chatInterface.ProcessOnboardingRequestAsync(userPrompt);
Console.WriteLine(response);
```

### Expected Execution Flow

1. **SK Chat Service analyzes prompt** and determines `process_onboarding_query` function is needed
2. **Automatic entity extraction** from the prompt:
   ```json
   {
     "missionName": "Tactical Edge Platform",
     "command": "NAVWAR",
     "missionOwner": "Commander Sarah Johnson",
     "missionOwnerEmail": "sarah.johnson@navy.mil",
     "missionOwnerRank": "CDR",
     "requiredServices": ["AKS", "Azure SQL", "Redis"],
     "classificationLevel": "SECRET",
     "requestedVNetCidr": "10.200.0.0/16",
     "requestedSubscriptionName": "tactical-edge-prod",
     "businessJustification": "Mission-critical tactical data sharing platform for real-time intelligence operations",
     "region": "usgovvirginia"
   }
   ```
3. **Function call** to OnboardingPlugin:
   ```csharp
   await process_onboarding_query(
       query: "Create new onboarding for Tactical Edge Platform",
       additionalContext: "{...extracted JSON...}"
   )
   ```
4. **Plugin routes** to `CreateDraft` or `UpdateDraft` based on intent
5. **Response** returned to user: "✅ Created new onboarding draft request with ID `<guid>`."

## Usage Example 2: Multi-Turn Conversation

```csharp
var conversation = new List<string>
{
    "I need to start a new onboarding request",
    "It's called Operation Lighthouse",
    "The mission owner is LCDR Martinez at martinez@navy.mil",
    "We need AKS and Azure SQL Database",
    "Classification is SECRET",
    "Can you submit the request now?"
};

var responses = await chatInterface.ProcessConversationAsync(conversation);

// Turn 1: "I need to start a new onboarding request"
// Response: "✅ Created new onboarding draft request with ID `12345...`. What's the mission name?"

// Turn 2: "It's called Operation Lighthouse"
// SK extracts: {"missionName": "Operation Lighthouse"}
// Function call: process_onboarding_query(requestId="12345...", additionalContext="{\"missionName\":...}")
// Response: "✅ Updated request. Who is the mission owner?"

// Turn 3: "The mission owner is LCDR Martinez at martinez@navy.mil"
// SK extracts: {"missionOwner": "LCDR Martinez", "missionOwnerEmail": "martinez@navy.mil"}
// Response: "✅ Updated mission owner. What services do you need?"

// ... continues through all turns
```

## Usage Example 3: Manual Entity Extraction (Alternative)

If you prefer explicit control over entity extraction:

```csharp
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure;

public class ManualOnboardingOrchestrator
{
    private readonly OpenAIClient _openAIClient;
    private readonly OnboardingPlugin _onboardingPlugin;

    public async Task<string> ProcessWithManualExtractionAsync(string userPrompt)
    {
        // Step 1: Call LLM to extract entities with structured output
        var extractionPrompt = $@"
Extract onboarding information from this user request and return ONLY a JSON object:

User request: {userPrompt}

JSON schema:
{{
  ""missionName"": ""string"",
  ""missionOwner"": ""string"",
  ""missionOwnerEmail"": ""string"",
  ""command"": ""string"",
  ""classificationLevel"": ""string"",
  ""requestedSubscriptionName"": ""string"",
  ""requestedVNetCidr"": ""string"",
  ""requiredServices"": [""string""],
  ""businessJustification"": ""string"",
  ""region"": ""string""
}}

Return only valid JSON, no explanation.
";

        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4",
            Messages =
            {
                new ChatRequestSystemMessage("You are a data extraction assistant. Return only valid JSON."),
                new ChatRequestUserMessage(extractionPrompt)
            },
            Temperature = 0.1f,
            ResponseFormat = ChatCompletionsResponseFormat.JsonObject
        };

        var completionResponse = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        var extractedJson = completionResponse.Value.Choices[0].Message.Content;

        // Step 2: Call OnboardingPlugin with extracted data
        var response = await _onboardingPlugin.ProcessOnboardingQueryAsync(
            query: "Create new onboarding request",
            additionalContext: extractedJson);

        return response;
    }
}
```

## Advanced: Using Handlebars Planner (Optional)

> **Note**: The Handlebars planner is an older approach. For most scenarios, use auto function calling shown above.

If you have complex multi-step workflows that require planning ahead of time:

```csharp
using Microsoft.SemanticKernel.Planning.Handlebars;

public class PlanningOnboardingOrchestrator
{
    private readonly Kernel _kernel;
    private readonly HandlebarsPlanner _planner;

    public PlanningOnboardingOrchestrator(Kernel kernel)
    {
        _kernel = kernel;
        _planner = new HandlebarsPlanner(new HandlebarsPlannerOptions
        {
            AllowLoops = false,
            AllowedFunctions = new List<string> { "OnboardingPlugin-process_onboarding_query" }
        });
    }

    public async Task<string> ProcessWithPlanningAsync(string userPrompt)
    {
        // Generate plan
        var plan = await _planner.CreatePlanAsync(_kernel, userPrompt);
        
        Console.WriteLine($"Generated Plan:\n{plan}");

        // Execute plan
        var result = await plan.InvokeAsync(_kernel);

        return result.ToString();
    }
}
```

**When to use planners vs. auto function calling:**
- **Auto Function Calling (Recommended)**: Single-step or conversational multi-turn scenarios where the LLM decides which functions to call in real-time
- **Handlebars Planner**: Complex workflows requiring predetermined execution order, loops, or conditional logic

For the OnboardingPlugin, **auto function calling is the recommended approach** as it provides:
- Natural conversational flow
- Better entity extraction from complex prompts  
- Easier debugging and observability
- Lower token usage

## Debugging Tips

### Enable Function Call Logging

```csharp
builder.Services.AddLogging(c =>
{
    c.AddConsole();
    c.AddFilter("Microsoft.SemanticKernel", LogLevel.Debug);
    c.AddFilter("Microsoft.SemanticKernel.Functions", LogLevel.Trace);
});
```

### Inspect Function Call Details

```csharp
var executionSettings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    
    // Custom hook to inspect function calls
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new()
    {
        AllowConcurrentInvocation = false,
        AllowParallelCalls = false
    })
};

// Access function call metadata in chat history
foreach (var message in chatHistory)
{
    if (message is OpenAIChatMessageContent openAIMessage)
    {
        foreach (var toolCall in openAIMessage.ToolCalls)
        {
            Console.WriteLine($"Function: {toolCall.FunctionName}");
            Console.WriteLine($"Arguments: {toolCall.FunctionArguments}");
        }
    }
}
```

### Validate Extracted JSON

```csharp
using System.Text.Json.Nodes;

public string ValidateAndFixExtractedContext(string contextJson)
{
    try
    {
        var contextObject = JsonNode.Parse(contextJson);
        
        // Validate required fields
        var requiredFields = new[] { "missionName", "command", "missionOwner" };
        foreach (var field in requiredFields)
        {
            if (contextObject?[field] == null)
            {
                _logger.LogWarning("Missing required field: {Field}", field);
            }
        }

        // Apply defaults for missing optional fields
        if (contextObject?["region"] == null)
        {
            contextObject!["region"] = "usgovvirginia";
        }
        if (contextObject?["classificationLevel"] == null)
        {
            contextObject!["classificationLevel"] = "UNCLASS";
        }

        return contextObject.ToJsonString();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to parse context JSON");
        return "{}";
    }
}
```

## Performance Considerations

### Token Usage Optimization

```csharp
var executionSettings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    MaxTokens = 1000,  // Limit response size
    Temperature = 0.1,   // Deterministic extraction
    
    // Only include onboarding plugin in function list
    EnabledFunctions = new List<string> 
    { 
        "OnboardingPlugin-process_onboarding_query" 
    }
};
```

### Caching Extracted Entities

```csharp
using Microsoft.Extensions.Caching.Memory;

public class CachedOnboardingOrchestrator
{
    private readonly IMemoryCache _cache;
    private readonly OnboardingChatInterface _chatInterface;

    public async Task<string> ProcessWithCachingAsync(string userId, string userPrompt)
    {
        var cacheKey = $"onboarding_context_{userId}";
        
        // Check if we have cached entity extraction from previous turns
        if (_cache.TryGetValue(cacheKey, out string? cachedContext))
        {
            // Merge new info with cached context
            var mergedContext = MergeContexts(cachedContext!, userPrompt);
            _cache.Set(cacheKey, mergedContext, TimeSpan.FromMinutes(30));
            
            return await _chatInterface.ProcessOnboardingRequestAsync(
                $"Update onboarding with: {mergedContext}");
        }

        // First time - do full extraction
        var response = await _chatInterface.ProcessOnboardingRequestAsync(userPrompt);
        return response;
    }
}
```

## Testing the Integration

```csharp
using Xunit;
using Moq;

public class OnboardingChatInterfaceTests
{
    [Fact]
    public async Task ProcessOnboardingRequest_WithRichPrompt_ExtractsEntitiesAndCreatesRequest()
    {
        // Arrange
        var mockService = new Mock<IOnboardingService>();
        mockService
            .Setup(s => s.CreateDraftRequestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-request-id");

        var chatInterface = new OnboardingChatInterface(
            azureOpenAIEndpoint: "test-endpoint",
            azureOpenAIKey: "test-key",
            deploymentName: "gpt-4",
            onboardingService: mockService.Object,
            logger: Mock.Of<ILogger<OnboardingChatInterface>>());

        var richPrompt = @"I need to onboard Tactical Edge Platform for NAVWAR. 
                          Commander Sarah Johnson, sarah.johnson@navy.mil. 
                          Need AKS and Azure SQL. Classification: SECRET.";

        // Act
        var response = await chatInterface.ProcessOnboardingRequestAsync(richPrompt);

        // Assert
        response.Should().Contain("new-request-id");
        mockService.Verify(
            s => s.CreateDraftRequestAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
```

## Related Documentation

- [onboarding-plugin-architecture.md](./onboarding-plugin-architecture.md) - Architecture overview
- [Microsoft Semantic Kernel Docs](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Function Calling Best Practices](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling)
