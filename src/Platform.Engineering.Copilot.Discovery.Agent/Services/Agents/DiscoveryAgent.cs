using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Platform.Engineering.Copilot.Core.Interfaces.Agents;
using Platform.Engineering.Copilot.Core.Services.Agents;
using Platform.Engineering.Copilot.Core.Models.Agents;
using Platform.Engineering.Copilot.Core.Services.Azure;
using Platform.Engineering.Copilot.Core.Interfaces.Chat;
using Platform.Engineering.Copilot.Core.Configuration;
using Platform.Engineering.Copilot.Discovery.Agent;
using Platform.Engineering.Copilot.Discovery.Core.Configuration;
using Platform.Engineering.Copilot.Discovery.Agent.Plugins;

namespace Platform.Engineering.Copilot.Discovery.Core;

/// <summary>
/// Specialized agent for resource discovery, inventory, and health monitoring
/// Enhanced with Azure MCP Server integration for comprehensive Azure resource discovery
/// </summary>
public class DiscoveryAgent : ISpecializedAgent
{
    public AgentType AgentType => AgentType.Discovery;

    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<DiscoveryAgent> _logger;
    private readonly AzureMcpClient _azureMcpClient;
    private readonly string? _defaultSubscriptionId;
    private readonly DiscoveryAgentOptions _options;

    public DiscoveryAgent(
        ISemanticKernelService semanticKernelService,
        ILogger<DiscoveryAgent> logger,
        AzureResourceDiscoveryPlugin AzureResourceDiscoveryPlugin,
        AzureMcpClient azureMcpClient,
        IOptions<AzureGatewayOptions> azureOptions,
        Platform.Engineering.Copilot.Core.Plugins.ConfigurationPlugin configurationPlugin,
        IOptions<DiscoveryAgentOptions> options)
    {
        _logger = logger;
        _azureMcpClient = azureMcpClient;
        _defaultSubscriptionId = azureOptions.Value.SubscriptionId;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Create specialized kernel for discovery operations
        _kernel = semanticKernelService.CreateSpecializedKernel(AgentType.Discovery);
        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Register shared configuration plugin (set_azure_subscription, get_azure_subscription, etc.)
        _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(configurationPlugin, "ConfigurationPlugin"));
        
        // Register resource discovery plugin
        _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(AzureResourceDiscoveryPlugin, "AzureResourceDiscoveryPlugin"));

        _logger.LogInformation("✅ Discovery Agent initialized (Temperature: {Temperature}, MaxTokens: {MaxTokens}, HealthMonitoring: {HealthMonitoring}, DependencyMapping: {DependencyMapping})",
            _options.Temperature, _options.MaxTokens, _options.EnableHealthMonitoring, _options.EnableDependencyMapping);
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

            // Execute with configured temperature for discovery operations
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = _options.Temperature,
                MaxTokens = _options.MaxTokens,
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
        var subscriptionInfo = !string.IsNullOrEmpty(_defaultSubscriptionId)
            ? $@"

**🔧 DEFAULT CONFIGURATION:**
- Default Subscription ID: {_defaultSubscriptionId}
- When users don't specify a subscription, automatically use the default subscription ID above
- ALWAYS use the default subscription when available unless user explicitly specifies a different one"
            : "";

        return $@"You are a specialized Resource Discovery and Inventory expert with deep expertise in:
{subscriptionInfo}

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

**🤖 Conversational Requirements Gathering**

When a user asks about resources, inventory, or discovery, use a conversational approach to gather context:

**For Resource Discovery Requests, ask about:**
- **Scope**: ""What resources would you like me to discover?""
  - All resources in a subscription
  - Resources in a specific resource group
  - Specific resource types (VMs, AKS, Storage, Databases, etc.)
  - Resources with specific tags
  - Resources in a specific location
- **Subscription ID**: Use the default subscription ID unless user specifies a different one
- **Output Format**: ""How would you like the results?""
  - Summary (count by type)
  - Detailed list with properties
  - Inventory report (JSON/CSV)
  - Dependency map

**For Resource Search Requests, ask about:**
- **Search Criteria**: ""What are you looking for?""
  - Resource name pattern (e.g., ""*-prod-*"")
  - Resource type (e.g., ""all AKS clusters"")
  - Tag key/value (e.g., ""Environment=Production"")
  - Location (e.g., ""usgovvirginia"")
- **Search Scope**: ""Where should I search?""
  - Specific subscription
  - All subscriptions (if multi-subscription access)
  - Specific resource groups

**For Dependency Mapping Requests, ask about:**
- **Root Resource**: ""Which resource should I start from?""
  - Resource ID
  - Resource name and resource group
- **Depth**: ""How deep should I map dependencies?""
  - Direct dependencies only
  - Full dependency tree
  - Up to N levels deep

**For Orphaned Resource Detection, ask about:**
- **Resource Types**: ""Which types of resources should I check?""
  - Unattached disks
  - Unused NICs
  - Empty NSGs
  - Idle VMs
  - All of the above
- **Retention Period**: ""How long should a resource be unused before flagging?""
  - 7 days
  - 30 days
  - 90 days

**For Tagging Analysis, ask about:**
- **Required Tags**: ""Which tags are required in your organization?""
  - Common: Environment, Owner, CostCenter, Application
  - Custom tags specific to organization
- **Scope**: ""What should I analyze?""
  - All resources in subscription
  - Specific resource types
  - Specific resource groups

**Example Conversation Flow:**

User: ""What resources do I have running?"" or ""List all Azure resources""
You: **[IMMEDIATELY call discover_azure_resources with the default subscription ID - DO NOT ask for subscription if default is configured]**

User: ""Discover resources in subscription 453c...""
You: **[IMMEDIATELY call discover_azure_resources with the specified subscription ID]**

**CRITICAL: Use Available Tools Proactively!**
- If default subscription is configured, USE IT immediately - don't ask
- Call discovery functions directly when you have enough information
- Only ask for clarification on ambiguous requests (e.g., ""which resource type?"")
- DO NOT ask ""Should I proceed?"" or ""Let me know your preferences!"" when you have subscription ID
- DO NOT repeat questions - use smart defaults for minor missing details

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
            metadata["toolsInvoked"] = "AzureResourceDiscoveryPlugin functions";
        }

        return metadata;
    }
}
