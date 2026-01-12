using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Infrastructure.Configuration;
using Platform.Engineering.Copilot.Agents.Infrastructure.State;
using Platform.Engineering.Copilot.Agents.Infrastructure.Tools;
using Platform.Engineering.Copilot.Channels.Abstractions;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.Agents;

/// <summary>
/// Infrastructure Agent for generating, deploying, and managing Azure infrastructure.
/// Handles IaC template generation, resource provisioning, scaling analysis, and Azure Arc onboarding.
/// </summary>
public class InfrastructureAgent : BaseAgent
{
    private readonly InfrastructureAgentOptions _options;
    private readonly InfrastructureStateAccessors _stateAccessors;
    private readonly IChannelManager? _channelManager;
    private readonly IStreamingHandler? _streamingHandler;

    public override string AgentId => "infrastructure";
    public override string AgentName => "Infrastructure Agent";
    public override string Description =>
        "Specialized agent for Azure infrastructure management. Generates Bicep/Terraform templates, " +
        "provisions resources, analyzes scaling needs, generates Azure Arc onboarding scripts, and manages " +
        "resource lifecycle with compliance framework integration.";

    protected override float Temperature => (float)_options.Temperature;
    protected override int MaxTokens => _options.MaxTokens;

    public InfrastructureAgent(
        IChatClient chatClient,
        ILogger<InfrastructureAgent> logger,
        IOptions<InfrastructureAgentOptions> options,
        InfrastructureStateAccessors stateAccessors,
        TemplateGenerationTool templateGenerationTool,
        TemplateRetrievalTool templateRetrievalTool,
        ResourceProvisioningTool resourceProvisioningTool,
        ScalingAnalysisTool scalingAnalysisTool,
        AzureArcTool azureArcTool,
        ResourceDeletionTool resourceDeletionTool,
        IAgentStateManager? agentStateManager = null,
        ISharedMemory? sharedMemory = null,
        IChannelManager? channelManager = null,
        IStreamingHandler? streamingHandler = null)
        : base(chatClient, logger, agentStateManager, sharedMemory)
    {
        _options = options?.Value ?? new InfrastructureAgentOptions();
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _channelManager = channelManager;
        _streamingHandler = streamingHandler;

        // Register all infrastructure tools
        RegisterTool(templateGenerationTool);
        RegisterTool(templateRetrievalTool);
        RegisterTool(resourceProvisioningTool);
        RegisterTool(scalingAnalysisTool);
        RegisterTool(azureArcTool);
        RegisterTool(resourceDeletionTool);

        Logger.LogInformation("✅ Infrastructure Agent initialized (Temperature: {Temperature}, " +
            "Compliance: {Compliance}, PredictiveScaling: {Scaling}, AzureArc: {Arc})",
            _options.Temperature, _options.EnableComplianceEnhancement,
            _options.EnablePredictiveScaling, _options.EnableAzureArc);
    }

    protected override string GetSystemPrompt()
    {
        var complianceInfo = _options.EnableComplianceEnhancement
            ? $"Always apply {_options.DefaultComplianceFramework} compliance controls by default. "
            : "";

        var scalingInfo = _options.EnablePredictiveScaling
            ? "Offer predictive scaling analysis when discussing resource optimization. "
            : "";

        var arcInfo = _options.EnableAzureArc
            ? "Offer Azure Arc onboarding for hybrid scenarios when users mention on-premises or multi-cloud servers. "
            : "";

        return $@"You are an expert Azure Infrastructure Agent specializing in:
- **IaC Template Generation**: Create Bicep or Terraform templates with compliance enhancement
- **Resource Provisioning**: Deploy and manage Azure resources with validation
- **Scaling Analysis**: Predict scaling needs and optimize capacity
- **Azure Arc**: Generate onboarding scripts for hybrid infrastructure
- **Resource Management**: Handle resource group lifecycle operations

{complianceInfo}{scalingInfo}{arcInfo}

**Key Behaviors**:
1. Always ask clarifying questions if resource requirements are unclear
2. Default to secure, compliant configurations
3. Validate template parameters before provisioning
4. Warn users before destructive operations (deletions)
5. Track all operations for audit and rollback capability

**Default Configuration**:
- Region: {_options.DefaultRegion}
- Template Format: {_options.TemplateGeneration.DefaultFormat}
- Compliance Framework: {_options.DefaultComplianceFramework}

When generating templates, include all necessary outputs and use proper naming conventions.
When provisioning, offer dry-run validation first unless user requests immediate deployment.";
    }

    public override async Task<AgentResponse> ProcessAsync(
        AgentConversationContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Notify via channel that infrastructure processing is starting
        await NotifyChannelAsync(context.ConversationId, MessageType.AgentThinking,
            "Analyzing infrastructure request...", cancellationToken);

        try
        {
            Logger.LogInformation("Infrastructure Agent processing: {ConversationId}", context.ConversationId);

            // Check for subscription context
            var currentSubscription = await _stateAccessors.GetCurrentSubscriptionAsync(
                context.ConversationId, cancellationToken);

            if (!string.IsNullOrEmpty(context.SubscriptionId) && context.SubscriptionId != currentSubscription)
            {
                await _stateAccessors.SetCurrentSubscriptionAsync(
                    context.ConversationId, context.SubscriptionId, null, cancellationToken);
                currentSubscription = context.SubscriptionId;
            }
            else if (string.IsNullOrEmpty(currentSubscription) && !string.IsNullOrEmpty(_options.DefaultSubscriptionId))
            {
                await _stateAccessors.SetCurrentSubscriptionAsync(
                    context.ConversationId, _options.DefaultSubscriptionId, null, cancellationToken);
                currentSubscription = _options.DefaultSubscriptionId;
            }

            // Get user message for intent analysis
            var userMessage = context.MessageHistory.LastOrDefault(m => m.IsUser)?.Content ?? "";
            var intent = AnalyzeIntent(userMessage);

            // Notify progress
            await NotifyChannelAsync(context.ConversationId, MessageType.ProgressUpdate,
                $"Processing {intent.PrimaryOperation} request...", cancellationToken);

            // Call base implementation for actual processing
            var response = await base.ProcessAsync(context, cancellationToken);

            // Track operation
            var duration = DateTime.UtcNow - startTime;
            await _stateAccessors.TrackInfrastructureOperationAsync(
                context.ConversationId,
                intent.PrimaryOperation,
                intent.ResourceType ?? "general",
                currentSubscription ?? "",
                response.Success,
                duration,
                cancellationToken);

            // Notify completion via channel
            await NotifyChannelAsync(context.ConversationId, MessageType.AgentResponse,
                JsonSerializer.Serialize(new
                {
                    agentName = AgentName,
                    success = response.Success,
                    operation = intent.PrimaryOperation,
                    durationMs = (int)duration.TotalMilliseconds,
                    toolsUsed = response.ToolsExecuted?.Select(t => t.ToolName).ToList() ?? new List<string>()
                }), cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Infrastructure Agent failed");

            await NotifyChannelAsync(context.ConversationId, MessageType.Error,
                $"Infrastructure operation failed: {ex.Message}", cancellationToken);

            return new AgentResponse
            {
                AgentId = AgentId,
                AgentName = AgentName,
                Content = $"Error: {ex.Message}",
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task NotifyChannelAsync(string conversationId, MessageType messageType, string content, CancellationToken cancellationToken)
    {
        if (_channelManager == null) return;

        try
        {
            var message = new ChannelMessage
            {
                ConversationId = conversationId,
                Type = messageType,
                Content = content,
                AgentType = AgentId,
                Timestamp = DateTime.UtcNow
            };
            await _channelManager.SendToConversationAsync(conversationId, message, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to publish channel message");
        }
    }

    private InfrastructureIntent AnalyzeIntent(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        var intent = new InfrastructureIntent();

        // Determine primary operation
        if (lowerMessage.Contains("generate") || lowerMessage.Contains("create template") ||
            lowerMessage.Contains("bicep") || lowerMessage.Contains("terraform"))
        {
            intent.PrimaryOperation = "generate";
        }
        else if (lowerMessage.Contains("deploy") || lowerMessage.Contains("provision") ||
                 lowerMessage.Contains("create resource"))
        {
            intent.PrimaryOperation = "provision";
        }
        else if (lowerMessage.Contains("scale") || lowerMessage.Contains("scaling") ||
                 lowerMessage.Contains("capacity") || lowerMessage.Contains("autoscale"))
        {
            intent.PrimaryOperation = "scaling";
        }
        else if (lowerMessage.Contains("arc") || lowerMessage.Contains("hybrid") ||
                 lowerMessage.Contains("on-prem") || lowerMessage.Contains("onboard"))
        {
            intent.PrimaryOperation = "arc";
        }
        else if (lowerMessage.Contains("delete") || lowerMessage.Contains("remove") ||
                 lowerMessage.Contains("destroy"))
        {
            intent.PrimaryOperation = "delete";
        }
        else
        {
            intent.PrimaryOperation = "general";
        }

        // Detect resource types
        var resourcePatterns = new Dictionary<string, string[]>
        {
            ["aks"] = new[] { "aks", "kubernetes", "k8s", "container cluster" },
            ["storage"] = new[] { "storage", "blob", "storage account" },
            ["keyvault"] = new[] { "keyvault", "key vault", "secrets", "vault" },
            ["appservice"] = new[] { "app service", "web app", "webapp", "app plan" },
            ["sql"] = new[] { "sql", "database", "sql server", "azure sql" },
            ["vnet"] = new[] { "vnet", "network", "virtual network", "subnet" },
            ["vm"] = new[] { "virtual machine", " vm ", "compute" }
        };

        foreach (var (resourceType, patterns) in resourcePatterns)
        {
            if (patterns.Any(p => lowerMessage.Contains(p)))
            {
                intent.ResourceType = resourceType;
                break;
            }
        }

        return intent;
    }

    private class InfrastructureIntent
    {
        public string PrimaryOperation { get; set; } = "general";
        public string? ResourceType { get; set; }
    }
}
