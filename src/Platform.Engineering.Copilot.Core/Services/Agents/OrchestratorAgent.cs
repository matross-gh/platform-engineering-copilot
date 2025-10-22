#pragma warning disable SKEXP0010 // ResponseFormat is experimental

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Platform.Engineering.Copilot.Core.Interfaces.Agents;
using Platform.Engineering.Copilot.Core.Models.Agents;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;
using Platform.Engineering.Copilot.Core.Interfaces;
using System.Text.Json;

namespace Platform.Engineering.Copilot.Core.Services.Agents;

/// <summary>
/// Orchestrator agent that coordinates and plans execution across specialized agents
/// This is the "brain" of the multi-agent system
/// </summary>
public class OrchestratorAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly Dictionary<AgentType, ISpecializedAgent> _agents;
    private readonly SharedMemory _sharedMemory;
    private readonly ExecutionPlanValidator _planValidator;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(
        ISemanticKernelService semanticKernelService,
        IEnumerable<ISpecializedAgent> agents,
        SharedMemory sharedMemory,
        ExecutionPlanValidator planValidator,
        ILogger<OrchestratorAgent> logger)
    {
        _logger = logger;
        _sharedMemory = sharedMemory;
        _planValidator = planValidator;

        // Create orchestrator's own kernel
        _kernel = semanticKernelService.CreateSpecializedKernel(AgentType.Orchestrator);
        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        // Build agent registry
        _agents = agents.ToDictionary(a => a.AgentType, a => a);

        _logger.LogInformation("🎼 OrchestratorAgent initialized with {AgentCount} specialized agents",
            _agents.Count);
    }

    /// <summary>
    /// Process a user request by coordinating specialized agents
    /// </summary>
    public async Task<OrchestratedResponse> ProcessRequestAsync(
        string userMessage,
        string conversationId,
        ConversationContext? existingContext = null,  // ACCEPT EXISTING CONTEXT WITH HISTORY
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🎼 Orchestrator processing request: {Message} [ConversationId: {ConversationId}]", 
            userMessage, conversationId);

        // Use existing context if provided (preserves message history), otherwise create new
        var context = existingContext ?? new ConversationContext
        {
            ConversationId = conversationId,
            LastActivityAt = DateTime.UtcNow
        };
        
        // Update activity timestamp
        context.LastActivityAt = DateTime.UtcNow;

        // Store context in shared memory so agents can access message history
        _sharedMemory.StoreContext(conversationId, context);

        try
        {
            // Step 1: Analyze intent and create execution plan
            var plan = await CreateExecutionPlanAsync(userMessage, context);

            _logger.LogInformation("📋 Execution plan created: {Pattern} with {TaskCount} tasks",
                plan.ExecutionPattern, plan.Tasks.Count);

            // Step 2: Execute plan based on pattern
            List<AgentResponse> responses;
            switch (plan.ExecutionPattern)
            {
                case ExecutionPattern.Sequential:
                    responses = await ExecuteSequentialAsync(plan.Tasks, context.ConversationId);
                    break;

                case ExecutionPattern.Parallel:
                    responses = await ExecuteParallelAsync(plan.Tasks, context.ConversationId);
                    break;

                case ExecutionPattern.Collaborative:
                    responses = await ExecuteCollaborativeAsync(userMessage, context.ConversationId, plan);
                    break;

                default:
                    throw new ArgumentException($"Unknown execution pattern: {plan.ExecutionPattern}");
            }

            // Step 3: Synthesize final response
            var finalResponse = await SynthesizeResponseAsync(userMessage, responses, context);

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Build orchestrated response
            var orchestratedResponse = new OrchestratedResponse
            {
                FinalResponse = finalResponse,
                PrimaryIntent = plan.PrimaryIntent,
                AgentsInvoked = responses.Select(r => r.AgentType).Distinct().ToList(),
                ExecutionPattern = plan.ExecutionPattern,
                TotalAgentCalls = responses.Count,
                ExecutionTimeMs = executionTime,
                Success = responses.All(r => r.Success),
                RequiresFollowUp = DetermineIfFollowUpNeeded(responses),
                FollowUpPrompt = GenerateFollowUpPrompt(responses),
                MissingFields = ExtractMissingFields(responses),
                QuickReplies = GenerateQuickReplies(plan.PrimaryIntent, responses),
                Metadata = CombineMetadata(responses),
                Errors = responses.SelectMany(r => r.Errors).ToList()
            };

            _logger.LogInformation("✅ Orchestration complete in {ExecutionTime}ms: {AgentCount} agents invoked",
                executionTime, orchestratedResponse.AgentsInvoked.Count);

            return orchestratedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in orchestrator processing");

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new OrchestratedResponse
            {
                FinalResponse = $"I encountered an error while processing your request: {ex.Message}",
                PrimaryIntent = "error",
                Success = false,
                ExecutionTimeMs = executionTime,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Create an execution plan by analyzing the user's request
    /// </summary>
    private async Task<ExecutionPlan> CreateExecutionPlanAsync(
        string userMessage,
        ConversationContext context)
    {
        _logger.LogDebug("📋 Creating execution plan for: {Message}", userMessage);

        var availableAgents = string.Join("\n", _agents.Keys.Select(a =>
            $"- {a}: {GetAgentDescription(a)}"));

        // Include recent conversation history for context
        var conversationHistory = context.MessageHistory.Count > 0 
            ? "\n\nRecent conversation history:\n" + string.Join("\n", context.MessageHistory.TakeLast(5).Select(m => $"{m.Role}: {m.Content}"))
            : "";

        var planningPrompt = $@"
You are an expert orchestrator for a multi-agent platform engineering system.

Available specialized agents:
{availableAgents}
{conversationHistory}

Current user message: ""{userMessage}""

Analyze this request and create an execution plan in JSON format with:
1. **primaryIntent**: Main category (infrastructure, compliance, cost, environment, discovery, onboarding, or mixed)
2. **tasks**: Array of tasks, each with:
   - agentType: Which agent to use (Infrastructure, Compliance, CostManagement, Environment, Discovery, Onboarding)
   - description: What the agent should do
   - priority: Number (higher = more important)
   - isCritical: Boolean (if task failure should stop execution)
3. **executionPattern**: How to execute tasks:
   - ""Sequential"": Tasks depend on each other (do them in order)
   - ""Parallel"": Tasks are independent (do them simultaneously)
   - ""Collaborative"": Agents need to iterate and refine together
4. **estimatedTimeSeconds**: How long you think this will take

CRITICAL Guidelines - READ CAREFULLY:

**🚨 COMPLIANCE ASSESSMENT vs COMPLIANT INFRASTRUCTURE - CRITICAL DISTINCTION:**
- **""check compliance"" / ""run assessment"" / ""scan"" = ComplianceAgent ONLY** (scan existing resources)
- **""create compliant"" / ""generate template"" = InfrastructureAgent ONLY** (generate new templates)
- **NEVER use InfrastructureAgent for compliance scanning!**

Examples:
- ❌ WRONG: ""check compliance"" → Infrastructure → generates templates
- ✅ CORRECT: ""check compliance"" → Compliance → scans existing resources
- ❌ WRONG: ""create NIST-compliant storage"" → Compliance → tries to scan
- ✅ CORRECT: ""create NIST-compliant storage"" → Infrastructure → generates template

- **Check conversation context FIRST** - if the conversation history shows the user is answering follow-up questions about template generation, this is STILL template generation!
  * Look for patterns: User asked to generate templates → Assistant asked clarifying questions → User provided answers
  * **If last assistant message asked questions and user is now answering** → This is a continuation of the original request
  * Example: Assistant asked ""What region?"" and user says ""us gov virginia, dev, yes"" → Still template generation, use ONLY Infrastructure
  
- **Be CONSERVATIVE with agent invocation** - only invoke agents when the user explicitly requests ACTION on EXISTING resources

- **Distinguish between FOUR types of requests**:
  
  1. **COMPLIANCE SCANNING/ASSESSMENT** (Analyzing existing resources) - MUST use Compliance agent:
     * User says: ""check compliance"", ""run a compliance assessment"", ""scan my subscription"", ""compliance status"", ""security assessment""
     * User mentions checking EXISTING resources for compliance (""my cluster"", ""my subscription"", ""current environment"")
     * Keywords: ""assess"", ""scan"", ""check"", ""validate"", ""audit"", ""evaluate"" + compliance/security
     * Examples: ""Check if my subscription is compliant"", ""Run a NIST assessment on my resources"", ""Scan my AKS cluster for compliance""
     * **Action**: Use ONLY ComplianceAgent to scan existing resources
     * **DO NOT EVER**: Use InfrastructureAgent for compliance assessments - that's for generating NEW compliant templates
     * **IMPORTANT**: ""compliance assessment"" = scan existing, NOT generate new templates
     * **primaryIntent**: ""compliance"" (NOT ""infrastructure"")
  
  2. **TEMPLATE GENERATION** (Infrastructure-as-Code design - DEFAULT for safety):
     * User says: ""deploy"", ""create"", ""set up"", ""I need"" infrastructure WITHOUT ""actually""/""provision""/""make it live""
     * User asks for ""template"", ""Bicep"", ""Terraform"", ""ARM template"", ""IaC"", ""infrastructure code""
     * User asks for ""compliant infrastructure"", ""FedRAMP template"", ""NIST-compliant cluster""
     * Examples: ""Deploy an AKS cluster"", ""Create a storage account"", ""I need infrastructure for X""
     * User confirms after requirements gathering: ""yes"", ""proceed"", ""sounds good""
     * **Action**: Use ONLY InfrastructureAgent to generate templates/code
     * **DO NOT**: Invoke Environment, Compliance, Discovery, or CostManagement agents
     * **IMPORTANT**: This is the SAFE default - generates CODE only, creates NO real Azure resources
     * **Safety**: Prevents accidental resource creation and unexpected costs
     * **primaryIntent**: ""infrastructure""
  
  3. **ACTUAL PROVISIONING** (Deploying real Azure resources - REQUIRES explicit intent):
     * User EXPLICITLY says: ""actually provision"", ""make it live"", ""deploy the template"", ""create the resources now""
     * User says: ""execute the deployment"", ""provision for real"", ""I want to deploy this now""
     * **Action**: Sequential execution - Infrastructure → Environment → Compliance → Discovery → Cost
     * **WARNING**: This creates REAL Azure resources and incurs REAL costs
     * **IMPORTANT**: Only trigger this when user explicitly confirms deployment intent
  
  4. **INFORMATIONAL** (Questions/guidance):
     * User asks: ""What are..."", ""How do I..."", ""Best practices..."", ""Show me examples""
     * **Action**: Use ONLY the relevant agent for guidance
     * **DO NOT**: Invoke any other agents
  
- **For existing resource queries**:
  * Only scan resources that actually exist (user mentions ""my cluster"", ""my subscription"", etc.)
  * Do NOT assume resources exist based on hypothetical questions
  
- **Execution patterns**:
  * Use Sequential when tasks depend on each other (e.g., provision THEN scan)
  * Use Parallel when tasks are independent on EXISTING resources (e.g., list resources AND check compliance)
  * Use Collaborative when quality requires iteration on ACTUAL deployments
  * **For template generation**: Always use Sequential with ONLY Infrastructure agent

- **Estimated time**:
  * Template generation (Infrastructure only - DEFAULT): 10-30 seconds (creates code, no Azure deployment)
  * Compliance scanning: 30-60 seconds (scans existing Azure resources)
  * Actual provisioning (EXPLICIT intent only): 60-180 seconds (creates REAL Azure resources + costs)
  * Resource discovery: 30-60 seconds
  
- **Key indicators - READ CAREFULLY**:
  * **COMPLIANCE SCANNING (use Compliance agent)**: ""check compliance"", ""run assessment"", ""scan my subscription"", ""is my cluster compliant"", ""compliance status"", ""assess security""
  * **TEMPLATE GENERATION (use Infrastructure agent)**: ""deploy"", ""create"", ""set up"", ""I need"", ""generate template"", ""create compliant infrastructure""
  * **ACTUAL PROVISIONING (use all 5 agents)**: ""actually provision"", ""make it live"", ""execute deployment"", ""provision for real""
  * **INFORMATIONAL (use single agent)**: ""What are..."", ""How do I..."", ""Best practices...""
  * **CONFIRMATION (use Infrastructure agent)**: ""yes"", ""proceed"" after template questions

**🚨 CRITICAL DECISION TREE for ""compliance"" keyword:**
1. Does user say ""check"", ""scan"", ""assess"", or ""run assessment""? → YES = Compliance agent
2. Does user say ""create"", ""generate"", ""deploy"", or ""I need""? → YES = Infrastructure agent
3. User message: ""I need to check if my Azure subscription is compliant""
   - Contains ""check"" + ""compliant"" → Compliance agent (scan existing)
   - primaryIntent: ""compliance""
4. User message: ""Create a NIST-compliant storage account""  
   - Contains ""create"" + ""compliant"" → Infrastructure agent (generate template)
   - primaryIntent: ""infrastructure""

Respond ONLY with valid JSON, no other text.";

        try
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are an expert at planning multi-agent execution strategies. Always respond with valid JSON.");
            chatHistory.AddUserMessage(planningPrompt);

            var result = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = "json_object",
                    Temperature = 0.3,
                    MaxTokens = 2000
                });

            var planJson = result.Content ?? "{}";
            _logger.LogInformation("📋 RAW PLAN JSON FROM LLM: {PlanJson}", planJson);

            var plan = JsonSerializer.Deserialize<ExecutionPlanDto>(planJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (plan == null || plan.Tasks == null || !plan.Tasks.Any())
            {
                _logger.LogWarning("⚠️  Invalid plan generated, falling back to single infrastructure task");
                return CreateFallbackPlan(userMessage);
            }

            // Convert DTO to domain model
            var executionPlan = new ExecutionPlan
            {
                PrimaryIntent = plan.PrimaryIntent ?? "unknown",
                ExecutionPattern = ParseExecutionPattern(plan.ExecutionPattern),
                EstimatedTimeSeconds = plan.EstimatedTimeSeconds ?? 30,
                Tasks = plan.Tasks.Select(t => new AgentTask
                {
                    AgentType = ParseAgentType(t.AgentType),
                    Description = t.Description ?? userMessage,
                    Priority = t.Priority ?? 0,
                    IsCritical = t.IsCritical ?? false,
                    ConversationId = context.ConversationId
                }).ToList()
            };

            _logger.LogInformation("📋 Plan created: {Intent} → {Pattern} → {TaskCount} tasks",
                executionPlan.PrimaryIntent,
                executionPlan.ExecutionPattern,
                executionPlan.Tasks.Count);

            // Validate and potentially correct the plan
            var validatedPlan = _planValidator.ValidateAndCorrect(executionPlan, userMessage, context.ConversationId);

            return validatedPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating execution plan, using fallback");
            return CreateFallbackPlan(userMessage);
        }
    }

    /// <summary>
    /// Execute tasks sequentially (one after another)
    /// </summary>
    private async Task<List<AgentResponse>> ExecuteSequentialAsync(
        List<AgentTask> tasks,
        string conversationId)
    {
        _logger.LogInformation("▶️  Executing {TaskCount} tasks sequentially", tasks.Count);

        var responses = new List<AgentResponse>();
        // Sort by Priority ASCENDING (1, 2, 3, 4, 5) so Priority 1 executes FIRST
        var sortedTasks = tasks.OrderBy(t => t.Priority).ToList();

        foreach (var task in sortedTasks)
        {
            if (!_agents.TryGetValue(task.AgentType, out var agent))
            {
                _logger.LogWarning("⚠️  No agent found for type: {AgentType}", task.AgentType);
                continue;
            }

            _logger.LogInformation("🤖 Executing task with {AgentType}: {Description}",
                task.AgentType, task.Description);

            // Add previous results to shared memory
            if (responses.Any())
            {
                var context = _sharedMemory.GetContext(conversationId);
                context.PreviousResults = responses;
                _sharedMemory.StoreContext(conversationId, context);
            }

            var response = await agent.ProcessAsync(task, _sharedMemory);
            responses.Add(response);

            _logger.LogInformation("✅ Task completed: {AgentType} → Success: {Success}",
                task.AgentType, response.Success);

            // Stop if critical task failed
            if (!response.Success && task.IsCritical)
            {
                _logger.LogWarning("⛔ Critical task failed, stopping execution");
                break;
            }
        }

        return responses;
    }

    /// <summary>
    /// Execute tasks in parallel (simultaneously)
    /// </summary>
    private async Task<List<AgentResponse>> ExecuteParallelAsync(
        List<AgentTask> tasks,
        string conversationId)
    {
        _logger.LogInformation("⚡ Executing {TaskCount} tasks in parallel", tasks.Count);

        var agentTasks = tasks
            .Where(t => _agents.ContainsKey(t.AgentType))
            .Select(async task =>
            {
                var agent = _agents[task.AgentType];
                _logger.LogInformation("🤖 Starting parallel task: {AgentType}", task.AgentType);

                var response = await agent.ProcessAsync(task, _sharedMemory);

                _logger.LogInformation("✅ Parallel task completed: {AgentType} → Success: {Success}",
                    task.AgentType, response.Success);

                return response;
            })
            .ToList();

        var responses = await Task.WhenAll(agentTasks);

        return responses.ToList();
    }

    /// <summary>
    /// Execute tasks collaboratively (agents iterate and refine)
    /// </summary>
    private async Task<List<AgentResponse>> ExecuteCollaborativeAsync(
        string userMessage,
        string conversationId,
        ExecutionPlan plan)
    {
        _logger.LogInformation("🔄 Executing collaborative workflow with {TaskCount} tasks",
            plan.Tasks.Count);

        var responses = new List<AgentResponse>();
        var maxRounds = 3;
        var currentRound = 0;
        var allApproved = false;

        while (!allApproved && currentRound < maxRounds)
        {
            currentRound++;
            _logger.LogInformation("🔄 Collaboration round {Round}/{MaxRounds}", currentRound, maxRounds);

            // Execute all tasks in this round
            foreach (var task in plan.Tasks)
            {
                if (!_agents.TryGetValue(task.AgentType, out var agent))
                    continue;

                // Update task description with previous round feedback
                if (currentRound > 1)
                {
                    task.Description = $"{task.Description}\n\nPrevious feedback:\n{GetFeedbackSummary(responses)}";
                }

                var response = await agent.ProcessAsync(task, _sharedMemory);
                responses.Add(response);

                _logger.LogInformation("🤖 {AgentType} completed round {Round}: Success={Success}",
                    task.AgentType, currentRound, response.Success);
            }

            // Check if all agents are satisfied
            var latestResponses = responses.Skip(responses.Count - plan.Tasks.Count).ToList();
            allApproved = CheckCollaborativeApproval(latestResponses);

            if (allApproved)
            {
                _logger.LogInformation("✅ Collaborative workflow approved after {Round} rounds", currentRound);
            }
            else if (currentRound < maxRounds)
            {
                _logger.LogInformation("🔄 Needs refinement, starting round {NextRound}", currentRound + 1);
            }
        }

        if (!allApproved)
        {
            _logger.LogWarning("⚠️  Collaborative workflow completed {MaxRounds} rounds without full approval", maxRounds);
        }

        return responses;
    }

    /// <summary>
    /// Synthesize a final response from multiple agent responses
    /// </summary>
    private async Task<string> SynthesizeResponseAsync(
        string userMessage,
        List<AgentResponse> responses,
        ConversationContext context)
    {
        _logger.LogDebug("🎨 Synthesizing final response from {ResponseCount} agent responses", responses.Count);

        if (!responses.Any())
        {
            return "I couldn't process your request. Please try rephrasing it.";
        }

        // If only one agent responded, return its content directly
        if (responses.Count == 1)
        {
            return responses[0].Content;
        }

        // Combine multiple agent responses
        var agentOutputs = string.Join("\n\n", responses.Select(r =>
            $"**{r.AgentType} Agent:**\n{r.Content}"));

        var synthesisPrompt = $@"
User asked: ""{userMessage}""

Multiple specialized agents have processed this request. Synthesize their outputs into ONE comprehensive, user-friendly response.

Agent outputs:
{agentOutputs}

Your task:
1. Create a cohesive response that directly answers the user's question
2. Integrate insights from all agents seamlessly (don't list them separately)
3. Highlight key information:
   - Resource IDs and Azure Portal links
   - Compliance scores and security findings
   - Cost estimates and optimization opportunities
   - Any warnings or important notes
4. Use clear formatting (bullet points, sections, emojis for visual clarity)
5. Be concise but complete
6. Suggest logical next steps if appropriate

Important:
- Write in a natural, conversational tone
- Don't say ""Agent X said..."" - integrate the information naturally
- If agents provided conflicting information, reconcile it
- If something failed, explain clearly and suggest alternatives

Synthesized response:";

        try
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are an expert at synthesizing technical information into clear, actionable responses.");
            chatHistory.AddUserMessage(synthesisPrompt);

            var result = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.5,
                    MaxTokens = 2000
                });

            return result.Content ?? agentOutputs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error synthesizing response, returning raw outputs");
            return agentOutputs;
        }
    }

    // ========== Helper Methods ==========

    private string GetAgentDescription(AgentType agentType) => agentType switch
    {
        AgentType.Infrastructure => "Generate Infrastructure-as-Code templates (Bicep/Terraform), design network topology, analyze predictive scaling, optimize auto-scaling. Use for: creating NEW compliant infrastructure templates, designing networks, generating IaC code",
        AgentType.Compliance => "Scan and assess EXISTING resources for compliance (NIST 800-53, FedRAMP, DoD IL5), run security assessments, generate eMASS ATO packages. Use for: checking current compliance status, auditing existing infrastructure, validating security controls",
        AgentType.CostManagement => "Analyze costs of existing resources, estimate costs for planned deployments, optimize spending, track budgets",
        AgentType.Environment => "Manage environment lifecycle, clone environments, track deployments",
        AgentType.Discovery => "Discover and inventory existing resources, monitor health status, scan subscriptions",
        AgentType.Onboarding => "Onboard new missions and teams, gather requirements for new projects",
        _ => "General platform engineering tasks"
    };

    private ExecutionPlan CreateFallbackPlan(string userMessage)
    {
        _logger.LogInformation("📋 Creating fallback plan for: {Message}", userMessage);

        // Simple heuristic to determine agent
        var agentType = DetermineAgentFromMessage(userMessage);

        return new ExecutionPlan
        {
            PrimaryIntent = agentType.ToString().ToLowerInvariant(),
            ExecutionPattern = ExecutionPattern.Sequential,
            EstimatedTimeSeconds = 30,
            Tasks = new List<AgentTask>
            {
                new AgentTask
                {
                    AgentType = agentType,
                    Description = userMessage,
                    Priority = 1,
                    IsCritical = true
                }
            }
        };
    }

    private AgentType DetermineAgentFromMessage(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        if (lowerMessage.Contains("provision") || lowerMessage.Contains("create") ||
            lowerMessage.Contains("deploy") || lowerMessage.Contains("bicep") ||
            lowerMessage.Contains("terraform"))
            return AgentType.Infrastructure;

        if (lowerMessage.Contains("compliance") || lowerMessage.Contains("nist") ||
            lowerMessage.Contains("security") || lowerMessage.Contains("ato") ||
            lowerMessage.Contains("emass"))
            return AgentType.Compliance;

        if (lowerMessage.Contains("cost") || lowerMessage.Contains("budget") ||
            lowerMessage.Contains("price") || lowerMessage.Contains("optimize"))
            return AgentType.CostManagement;

        if (lowerMessage.Contains("environment") || lowerMessage.Contains("clone") ||
            lowerMessage.Contains("scale"))
            return AgentType.Environment;

        if (lowerMessage.Contains("list") || lowerMessage.Contains("find") ||
            lowerMessage.Contains("discover") || lowerMessage.Contains("inventory"))
            return AgentType.Discovery;

        if (lowerMessage.Contains("onboard") || lowerMessage.Contains("mission") ||
            lowerMessage.Contains("setup"))
            return AgentType.Onboarding;

        // Default to infrastructure for resource-related queries
        return AgentType.Infrastructure;
    }

    private ExecutionPattern ParseExecutionPattern(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return ExecutionPattern.Sequential;

        return pattern.ToLowerInvariant() switch
        {
            "parallel" => ExecutionPattern.Parallel,
            "collaborative" => ExecutionPattern.Collaborative,
            _ => ExecutionPattern.Sequential
        };
    }

    private AgentType ParseAgentType(string? agentType)
    {
        if (string.IsNullOrEmpty(agentType))
            return AgentType.Infrastructure;

        return agentType.ToLowerInvariant() switch
        {
            "infrastructure" => AgentType.Infrastructure,
            "compliance" => AgentType.Compliance,
            "costmanagement" => AgentType.CostManagement,
            "environment" => AgentType.Environment,
            "discovery" => AgentType.Discovery,
            "onboarding" => AgentType.Onboarding,
            _ => AgentType.Infrastructure
        };
    }

    private bool DetermineIfFollowUpNeeded(List<AgentResponse> responses)
    {
        // Follow-up needed if any agent had warnings or partial success
        return responses.Any(r => r.Warnings.Any() || !r.Success);
    }

    private string? GenerateFollowUpPrompt(List<AgentResponse> responses)
    {
        var failedAgents = responses.Where(r => !r.Success).ToList();
        if (failedAgents.Any())
        {
            return $"Some operations didn't complete successfully. Would you like me to retry or try a different approach?";
        }

        var warnings = responses.SelectMany(r => r.Warnings).ToList();
        if (warnings.Any())
        {
            return $"I completed your request with some warnings. Would you like more details?";
        }

        return null;
    }

    private List<string> ExtractMissingFields(List<AgentResponse> responses)
    {
        var missingFields = new List<string>();

        foreach (var response in responses)
        {
            // Check metadata for missing fields
            if (response.Metadata.TryGetValue("MissingFields", out var fields))
            {
                if (fields is List<string> fieldList)
                {
                    missingFields.AddRange(fieldList);
                }
            }
        }

        return missingFields.Distinct().ToList();
    }

    private List<string> GenerateQuickReplies(string intent, List<AgentResponse> responses)
    {
        var replies = new List<string>();

        // Generate contextual quick replies based on intent and results
        if (intent.Contains("infrastructure") || intent.Contains("provision"))
        {
            replies.Add("Check compliance status");
            replies.Add("Estimate costs");
            replies.Add("View in Azure Portal");
        }
        else if (intent.Contains("compliance"))
        {
            replies.Add("Generate remediation plan");
            replies.Add("Create eMASS package");
            replies.Add("View detailed findings");
        }
        else if (intent.Contains("cost"))
        {
            replies.Add("Show optimization suggestions");
            replies.Add("Set up budget alerts");
            replies.Add("Compare pricing tiers");
        }

        return replies;
    }

    private Dictionary<string, object> CombineMetadata(List<AgentResponse> responses)
    {
        var combinedMetadata = new Dictionary<string, object>();

        foreach (var response in responses)
        {
            foreach (var kvp in response.Metadata)
            {
                var key = $"{response.AgentType}_{kvp.Key}";
                combinedMetadata[key] = kvp.Value;
            }
        }

        return combinedMetadata;
    }

    private string GetFeedbackSummary(List<AgentResponse> responses)
    {
        var recentResponses = responses.TakeLast(3).ToList();
        return string.Join("\n", recentResponses.Select(r =>
            $"- {r.AgentType}: {(r.Success ? "✅ Approved" : "❌ Needs changes")} - {r.Warnings.FirstOrDefault() ?? "No issues"}"));
    }

    private bool CheckCollaborativeApproval(List<AgentResponse> latestResponses)
    {
        // All agents must succeed
        if (!latestResponses.All(r => r.Success))
            return false;

        // Check agent-specific approval criteria
        foreach (var response in latestResponses)
        {
            if (response.AgentType == AgentType.Compliance && response.IsApproved == false)
                return false;

            if (response.AgentType == AgentType.CostManagement && response.IsWithinBudget == false)
                return false;
        }

        return true;
    }

    // DTO for JSON deserialization
    private class ExecutionPlanDto
    {
        public string? PrimaryIntent { get; set; }
        public List<TaskDto>? Tasks { get; set; }
        public string? ExecutionPattern { get; set; }
        public int? EstimatedTimeSeconds { get; set; }
    }

    private class TaskDto
    {
        public string? AgentType { get; set; }
        public string? Description { get; set; }
        public int? Priority { get; set; }
        public bool? IsCritical { get; set; }
    }
}
