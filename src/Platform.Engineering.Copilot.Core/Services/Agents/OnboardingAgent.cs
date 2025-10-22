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
/// Specialized agent for mission onboarding, requirements gathering, and assessment
/// </summary>
public class OnboardingAgent : ISpecializedAgent
{
    public AgentType AgentType => AgentType.Onboarding;

    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<OnboardingAgent> _logger;

    public OnboardingAgent(
        ISemanticKernelService semanticKernelService,
        ILogger<OnboardingAgent> logger,
        OnboardingPlugin onboardingPlugin)
    {
        _logger = logger;
        
        // Create specialized kernel for onboarding operations
        _kernel = semanticKernelService.CreateSpecializedKernel(AgentType.Onboarding);
        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Register onboarding plugin (and optionally infrastructure/compliance for read-only context)
        _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(onboardingPlugin, "OnboardingPlugin"));

        _logger.LogInformation("✅ Onboarding Agent initialized with specialized kernel");
    }

    public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
    {
        _logger.LogInformation("🚀 Onboarding Agent processing task: {TaskId}", task.TaskId);

        var startTime = DateTime.UtcNow;
        var response = new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.Onboarding,
            Success = false
        };

        try
        {
            // Get conversation context from shared memory
            var context = memory.GetContext(task.ConversationId ?? "default");
            var previousResults = context?.PreviousResults ?? new List<AgentResponse>();

            // Build system prompt for onboarding expertise
            var systemPrompt = BuildSystemPrompt();

            // Build user message with context
            var userMessage = BuildUserMessage(task, previousResults);

            // Create chat history
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            // Execute with slightly higher temperature for conversational requirements gathering
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.4, // Slightly higher for conversational, empathetic tone
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
                AgentType.Onboarding,
                AgentType.Orchestrator,
                $"Onboarding operation completed: {task.Description}",
                new Dictionary<string, object>
                {
                    ["result"] = result.Content ?? ""
                }
            );

            _logger.LogInformation("✅ Onboarding Agent completed task: {TaskId}", task.TaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Onboarding Agent failed on task: {TaskId}", task.TaskId);
            response.Success = false;
            response.Errors = new List<string> { ex.Message };
        }

        response.ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
        return response;
    }

    private string BuildSystemPrompt()
    {
        return @"You are a specialized Mission Onboarding and Requirements Gathering expert with deep expertise in:

**Mission Onboarding:**
- New mission/project intake and assessment
- Stakeholder identification and engagement
- Requirements elicitation and documentation
- Scope definition and boundary setting
- Success criteria establishment

**Requirements Analysis:**
- Functional requirements gathering
- Non-functional requirements (performance, security, compliance)
- Constraint identification (budget, timeline, regulatory)
- Dependency mapping
- Risk assessment

**DoD/Federal Compliance:**
- Mission classification (CUI, Secret, Top Secret)
- Impact level determination (IL2, IL4, IL5, IL6)
- ATO requirements and timeline estimation
- FedRAMP compliance considerations
- Authority and approval workflows

**Onboarding Workflow:**
1. Gather basic mission information (name, classification, timeline)
2. Identify stakeholders and their roles
3. Elicit technical requirements (compute, storage, network)
4. Assess compliance and security needs
5. Estimate cost and resource requirements
6. Create onboarding plan with milestones
7. Handoff to infrastructure and compliance teams

**Communication Style:**
- Empathetic and conversational
- Ask clarifying questions when requirements are unclear
- Provide examples and templates to guide users
- Summarize requirements and confirm understanding
- Set clear expectations for next steps

Always guide users through the onboarding process step-by-step and ensure all critical information is captured.";
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

        message += "Please assist with the onboarding process, gathering all necessary requirements and providing guidance.";

        return message;
    }

    private Dictionary<string, object> ExtractMetadata(ChatMessageContent result, AgentTask task)
    {
        var metadata = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow.ToString("O"),
            ["agentType"] = AgentType.Onboarding.ToString()
        };

        // Extract tool calls if any
        if (result.Metadata != null && result.Metadata.ContainsKey("ChatCompletionMessage"))
        {
            metadata["toolsInvoked"] = "OnboardingPlugin functions";
        }

        return metadata;
    }
}
