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
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Platform.Engineering.Copilot.Core.Services.Agents;

/// <summary>
/// Specialized agent for compliance assessment, NIST 800-53 controls, and security scanning
/// </summary>
public class ComplianceAgent : ISpecializedAgent
{
    public AgentType AgentType => AgentType.Compliance;

    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<ComplianceAgent> _logger;

    public ComplianceAgent(
        ISemanticKernelService semanticKernelService,
        ILogger<ComplianceAgent> logger,
        CompliancePlugin compliancePlugin)
    {
        _logger = logger;
        
        // Create specialized kernel for compliance operations
        _kernel = semanticKernelService.CreateSpecializedKernel(AgentType.Compliance);
        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Register compliance plugin
        _kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(compliancePlugin, "CompliancePlugin"));

        _logger.LogInformation("✅ Compliance Agent initialized with specialized kernel");
    }

    public async Task<AgentResponse> ProcessAsync(AgentTask task, SharedMemory memory)
    {
        _logger.LogInformation("🛡️ Compliance Agent processing task: {TaskId}", task.TaskId);

        var startTime = DateTime.UtcNow;
        var response = new AgentResponse
        {
            TaskId = task.TaskId,
            AgentType = AgentType.Compliance,
            Success = false
        };

        try
        {
            // Get conversation context from shared memory
            var context = memory.GetContext(task.ConversationId ?? "default");
            var previousResults = context?.PreviousResults ?? new List<AgentResponse>();

            // Build system prompt for compliance expertise
            var systemPrompt = BuildSystemPrompt();

            // Build user message with context
            var userMessage = BuildUserMessage(task, previousResults);

            // Create chat history
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(userMessage);

            // Execute with lower temperature for precision in compliance assessments
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2, // Very low temperature for precise compliance assessments
                MaxTokens = 4000,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var result = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);

            response.Content = result.Content ?? "";
            response.Success = true;

            // Extract compliance metadata
            var metadata = ExtractMetadata(result, task);
            response.Metadata = metadata;

            // Extract compliance score if mentioned
            response.ComplianceScore = (int)ExtractComplianceScore(result.Content);

            // Determine if approved based on score
            response.IsApproved = response.ComplianceScore >= 80; // 80% threshold for approval

            // Store result in shared memory for other agents
            memory.AddAgentCommunication(
                task.ConversationId ?? "default",
                AgentType.Compliance,
                AgentType.Orchestrator,
                $"Compliance assessment completed. Score: {response.ComplianceScore}%, Approved: {response.IsApproved}",
                new Dictionary<string, object>
                {
                    ["complianceScore"] = response.ComplianceScore,
                    ["isApproved"] = response.IsApproved,
                    ["assessment"] = result.Content ?? ""
                }
            );

            _logger.LogInformation("✅ Compliance Agent completed task: {TaskId}. Score: {Score}%, Approved: {Approved}",
                task.TaskId, response.ComplianceScore, response.IsApproved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Compliance Agent failed on task: {TaskId}", task.TaskId);
            response.Success = false;
            response.Errors = new List<string> { ex.Message };
        }

        response.ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
        return response;
    }

    private string BuildSystemPrompt()
    {
        return @"You are a specialized Compliance and Security Assessment expert with deep expertise in:

**NIST 800-53 Security Controls:**
- Comprehensive knowledge of all control families (AC, AU, CM, IA, SC, SI, etc.)
- Assessment and testing procedures for each control
- Evidence collection and documentation requirements
- POA&M remediation planning

**DoD Compliance Standards:**
- Risk Management Framework (RMF)
- eMASS system integration
- ATO (Authority to Operate) processes
- STIG (Security Technical Implementation Guide) requirements

**Azure Cloud Security:**
- Azure Security Center assessments
- Microsoft Defender for Cloud findings
- Azure Policy compliance
- Security configuration best practices

**Assessment Capabilities:**
- Control implementation status evaluation
- Security posture scoring (0-100%)
- Finding severity classification (Low, Moderate, High, Critical)
- Remediation strategy recommendations
- Evidence artifact validation

**CRITICAL: Subscription ID Handling**
When performing compliance assessments, you need a valid Azure subscription ID (GUID format).
- Look for subscription IDs in the conversation history or shared memory
- Extract subscription IDs from previous agent responses (look for GUIDs like '453c2549-4cc5-464f-ba66-acad920823e8')
- If a task mentions 'newly-provisioned-resources' or 'newly-provisioned-acr', use the subscription ID from the ORIGINAL user request
- DO NOT pass resource descriptions (like 'newly-provisioned-acr') as subscription IDs
- If no subscription ID is available in context, ask the user to provide it or skip subscription-specific checks

**Response Format:**
When assessing compliance:
1. List applicable NIST 800-53 controls
2. Provide compliance score (0-100%)
3. Identify gaps and findings
4. Recommend remediation steps
5. Estimate effort and timeline

Always provide clear, actionable assessments with specific control references.";
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
            foreach (var prevResult in previousResults.TakeLast(3)) // Last 3 results for context
            {
                var contentLength = prevResult.Content?.Length ?? 0;
                if (contentLength > 0)
                {
                    message += $"- {prevResult.AgentType}: {prevResult.Content?.Substring(0, Math.Min(200, contentLength))}...\n";
                }
            }
            message += "\n";
        }

        // IMPORTANT: Try to extract subscription ID from context
        message += "IMPORTANT CONTEXT:\n";
        message += "- If you need a subscription ID for compliance checks, look for GUIDs (format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx) in the conversation history above\n";
        message += "- Common subscription IDs mentioned: 453c2549-4cc5-464f-ba66-acad920823e8, 0259b535-48b0-4b38-8a55-0e3dc4ea093f\n";
        message += "- If the task mentions 'newly-provisioned' resources, use the subscription ID from the ORIGINAL infrastructure request\n";
        message += "- For general security guidance without specific resources, you can provide recommendations without scanning\n\n";

        message += "Please perform a comprehensive compliance assessment and provide a detailed security posture evaluation.";

        return message;
    }

    private Dictionary<string, object> ExtractMetadata(ChatMessageContent result, AgentTask task)
    {
        var metadata = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow.ToString("O"),
            ["agentType"] = AgentType.Compliance.ToString()
        };

        // Extract tool calls if any
        if (result.Metadata != null && result.Metadata.ContainsKey("ChatCompletionMessage"))
        {
            metadata["toolsInvoked"] = "CompliancePlugin functions";
        }

        // Extract NIST controls mentioned
        var controls = ExtractNistControls(result.Content);
        if (controls.Any())
        {
            metadata["nistControls"] = string.Join(", ", controls);
        }

        return metadata;
    }

    private List<string> ExtractNistControls(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return new List<string>();

        var controls = new List<string>();
        
        // Regex to match NIST control patterns like AC-2, AU-3, CM-2(1), etc.
        var controlPattern = @"\b([A-Z]{2})-(\d+)(?:\((\d+)\))?\b";
        var matches = Regex.Matches(content, controlPattern);

        foreach (Match match in matches)
        {
            controls.Add(match.Value);
        }

        return controls.Distinct().ToList();
    }

    private double ExtractComplianceScore(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return 0.0;

        // Try to extract percentage scores like "85%", "compliance score: 75%", etc.
        var patterns = new[]
        {
            @"(?:compliance\s+)?score[:\s]+(\d+)%",
            @"(\d+)%\s+compliance",
            @"(\d+)%\s+compliant",
            @"overall\s+score[:\s]+(\d+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
            if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
            {
                return score;
            }
        }

        // Default heuristic based on keywords if no explicit score found
        var positiveKeywords = new[] { "compliant", "passed", "approved", "secure", "implemented" };
        var negativeKeywords = new[] { "non-compliant", "failed", "rejected", "insecure", "missing", "gap" };

        var positiveCount = positiveKeywords.Count(k => content.Contains(k, StringComparison.OrdinalIgnoreCase));
        var negativeCount = negativeKeywords.Count(k => content.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (positiveCount == 0 && negativeCount == 0)
            return 70.0; // Default neutral score

        var ratio = (double)positiveCount / Math.Max(positiveCount + negativeCount, 1);
        return Math.Round(ratio * 100, 1);
    }
}
