using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Platform.Engineering.Copilot.Core.Interfaces.Agents;
using Platform.Engineering.Copilot.Core.Models.Agents;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;
using Platform.Engineering.Copilot.Core.Plugins;
using Platform.Engineering.Copilot.Core.Services.Chat;
using Platform.Engineering.Copilot.Core.Interfaces;

namespace Platform.Engineering.Copilot.Core.Services.Agents;

/// <summary>
/// Specialized agent for resource discovery, inventory, and health monitoring
/// </summary>
public class DiscoveryAgent : ISpecializedAgent
{
    public AgentType AgentType => AgentType.Discovery;

    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<DiscoveryAgent> _logger;

    public DiscoveryAgent(
        ISemanticKernelService semanticKernelService,
        ILogger<DiscoveryAgent> logger,
        ResourceDiscoveryPlugin resourceDiscoveryPlugin)
    {
        _logger = logger;
        
        // Create specialized kernel for discovery operations
        _kernel = semanticKernelService.CreateSpecializedKernel(AgentType.Discovery);
        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Register resource discovery plugin
        _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(resourceDiscoveryPlugin, "ResourceDiscoveryPlugin"));

        _logger.LogInformation("✅ Discovery Agent initialized with specialized kernel");
    }

    public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
    {
        _logger.LogInformation("🔍 Discovery Agent processing task: {TaskId}", task.TaskId);

        var startTime = DateTime.UtcNow;
        var response = new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.Discovery,
            Success = false
        };

        try
        {
            // Get conversation context from shared memory
            var context = memory.GetContext(task.ConversationId ?? "default");
            var previousResults = context?.PreviousResults ?? new List<AgentResponse>();

            // Build system prompt for discovery expertise
            var systemPrompt = BuildSystemPrompt();

            // Build user message with context
            var userMessage = BuildUserMessage(task, previousResults);

            // Create chat history
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            // Execute with moderate temperature for discovery operations
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3, // Moderate temperature for analytical discovery
                MaxTokens = 4000,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var result = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);

            response.Content = result.Content ?? "";
            response.Success = true;

            // Extract metadata
            response.Metadata = ExtractMetadata(result, task);

            // Store result in shared memory for other agents
            memory.AddAgentCommunication(
                task.ConversationId ?? "default",
                AgentType.Discovery,
                AgentType.Orchestrator,
                $"Discovery operation completed: {task.Description}",
                new Dictionary<string, object>
                {
                    ["result"] = result.Content ?? ""
                }
            );

            _logger.LogInformation("✅ Discovery Agent completed task: {TaskId}", task.TaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Discovery Agent failed on task: {TaskId}", task.TaskId);
            response.Success = false;
            response.Errors = new List<string> { ex.Message };
        }

        response.ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
        return response;
    }

    private string BuildSystemPrompt()
    {
        return @"You are a specialized Resource Discovery and Inventory expert with deep expertise in:

**Azure Resource Discovery:**
- Comprehensive resource inventory across subscriptions
- Resource hierarchy mapping (Management Groups, Subscriptions, Resource Groups)
- Resource tagging analysis and validation
- Resource dependency mapping and visualization
- Orphaned and unused resource identification

**Health and Performance Monitoring:**
- Resource health status assessment
- Performance metrics collection and analysis
- Availability and uptime tracking
- Alert and incident correlation
- Capacity planning based on usage trends

**Discovery Operations:**
- Discover all resources in subscription/resource group
- Find resources by type, tag, location, or name
- Map resource dependencies (VNets, NICs, Disks, etc.)
- Identify security misconfigurations
- Detect cost optimization opportunities

**Reporting Capabilities:**
- Resource inventory reports (JSON, CSV, Excel)
- Compliance and tagging reports
- Cost allocation by tag/resource group
- Resource lifecycle analysis (creation date, modification date)
- Dependency diagrams and architecture views

**Best Practices:**
- Automated discovery scheduling
- Change detection and drift analysis
- Resource metadata enrichment
- Integration with CMDB/CMDB tools
- Discovery scope optimization

Always provide structured data with resource counts, types, and key findings.";
    }

    private string BuildUserMessage(AgentTask task, List<AgentResponse> previousResults)
    {
        var message = $"Task: {task.Description}\n\n";

        // Add parameters if provided
        if (task.Parameters != null && task.Parameters.Any())
        {
            message += "Parameters:\n";
            foreach (var param in task.Parameters)
            {
                message += $"- {param.Key}: {param.Value}\n";
            }
            message += "\n";
        }

        // Add context from previous agent results
        if (previousResults.Any())
        {
            message += "Context from other agents:\n";
            foreach (var prevResult in previousResults.TakeLast(3))
            {
                var contentLength = prevResult.Content?.Length ?? 0;
                if (contentLength > 0)
                {
                    message += $"- {prevResult.AgentType}: {prevResult.Content?.Substring(0, Math.Min(200, contentLength))}...\n";
                }
            }
            message += "\n";
        }

        message += "Please perform comprehensive resource discovery with detailed findings and recommendations.";

        return message;
    }

    private Dictionary<string, object> ExtractMetadata(ChatMessageContent result, AgentTask task)
    {
        var metadata = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow.ToString("O"),
            ["agentType"] = AgentType.Discovery.ToString()
        };

        // Extract tool calls if any
        if (result.Metadata != null && result.Metadata.ContainsKey("ChatCompletionMessage"))
        {
            metadata["toolsInvoked"] = "ResourceDiscoveryPlugin functions";
        }

        return metadata;
    }
}
