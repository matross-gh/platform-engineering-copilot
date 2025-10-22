using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Services.Agents;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;
using Platform.Engineering.Copilot.Core.Models.Agents;

namespace Platform.Engineering.Copilot.Core.Services.Chat;

/// <summary>
/// Pure Multi-Agent Intelligent Chat Service.
/// Delegates all processing to OrchestratorAgent for intelligent multi-agent coordination.
/// Manages conversation context and generates proactive suggestions based on agent interactions.
/// </summary>
public class IntelligentChatService : IIntelligentChatService
{
    private readonly ILogger<IntelligentChatService> _logger;
    private readonly OrchestratorAgent _orchestrator;

    // Conversation storage (TODO: Replace with distributed cache for production)
    private static readonly Dictionary<string, ConversationContext> _conversations = new();
    private static readonly object _conversationLock = new();

    public IntelligentChatService(
        ILogger<IntelligentChatService> logger,
        OrchestratorAgent orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        
        _logger.LogInformation("✅ Pure Multi-Agent IntelligentChatService initialized");
    }

    /// <summary>
    /// Process a user message using pure multi-agent orchestration.
    /// All intelligence, planning, and execution handled by OrchestratorAgent.
    /// </summary>
    public async Task<IntelligentChatResponse> ProcessMessageAsync(
        string message,
        string conversationId,
        ConversationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("🚀 Processing message for conversation {ConversationId}", conversationId);

            // Get or create conversation context
            context ??= await GetOrCreateContextAsync(conversationId, cancellationToken: cancellationToken);

            // Delegate to OrchestratorAgent (pure multi-agent processing) - PASS CONTEXT WITH HISTORY
            var orchestratedResponse = await _orchestrator.ProcessRequestAsync(
                message, 
                conversationId,
                context,  // THIS IS CRITICAL - pass the full context with message history!
                cancellationToken);

            // Update conversation history
            await UpdateConversationHistoryAsync(context, message, orchestratedResponse, cancellationToken);

            // Generate proactive suggestions
            var suggestions = GenerateProactiveSuggestions(context);

            stopwatch.Stop();

            _logger.LogInformation(
                "✅ Processed in {ElapsedMs}ms | Pattern: {Pattern} | Agents: {AgentCount} | Calls: {TotalCalls}", 
                stopwatch.ElapsedMilliseconds,
                orchestratedResponse.ExecutionPattern,
                orchestratedResponse.AgentsInvoked.Count,
                orchestratedResponse.TotalAgentCalls);

            return BuildSuccessResponse(orchestratedResponse, conversationId, context, suggestions, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Request cancelled for conversation {ConversationId}", conversationId);
            return BuildCancelledResponse(conversationId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for conversation {ConversationId}", conversationId);
            return BuildErrorResponse(conversationId, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Generate proactive suggestions based on recently used agents.
    /// </summary>
    public Task<List<ProactiveSuggestion>> GenerateProactiveSuggestionsAsync(
        string conversationId,
        ConversationContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenerateProactiveSuggestions(context));
    }

    /// <summary>
    /// Get or create conversation context.
    /// </summary>
    public Task<ConversationContext> GetOrCreateContextAsync(
        string conversationId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        lock (_conversationLock)
        {
            if (_conversations.TryGetValue(conversationId, out var context))
            {
                context.LastActivityAt = DateTime.UtcNow;
                return Task.FromResult(context);
            }

            var newContext = new ConversationContext
            {
                ConversationId = conversationId,
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                MessageHistory = new List<MessageSnapshot>(),
                UsedTools = new List<string>(),
                WorkflowState = new Dictionary<string, object?>()
            };

            _conversations[conversationId] = newContext;
            _logger.LogInformation("Created conversation context: {ConversationId}", conversationId);
            
            return Task.FromResult(newContext);
        }
    }

    /// <summary>
    /// Update conversation context with new message.
    /// </summary>
    public Task UpdateContextAsync(
        ConversationContext context,
        MessageSnapshot message,
        CancellationToken cancellationToken = default)
    {
        lock (_conversationLock)
        {
            context.MessageHistory.Add(message);
            context.MessageCount++;
            context.LastActivityAt = DateTime.UtcNow;

            // Keep only last 20 messages to avoid context bloat
            if (context.MessageHistory.Count > 20)
            {
                context.MessageHistory = context.MessageHistory.TakeLast(20).ToList();
            }

            // Track used agents
            if (!string.IsNullOrEmpty(message.ToolExecuted) && !context.UsedTools.Contains(message.ToolExecuted))
            {
                context.UsedTools.Add(message.ToolExecuted);
            }
        }

        return Task.CompletedTask;
    }

    // ========== PRIVATE HELPER METHODS ==========

    /// <summary>
    /// Update conversation history with user message and agent response.
    /// </summary>
    private async Task UpdateConversationHistoryAsync(
        ConversationContext context,
        string userMessage,
        OrchestratedResponse orchestratedResponse,
        CancellationToken cancellationToken)
    {
        var userSnapshot = new MessageSnapshot
        {
            Role = "user",
            Content = userMessage,
            Timestamp = DateTime.UtcNow
        };
        await UpdateContextAsync(context, userSnapshot, cancellationToken);

        var agentsInvoked = orchestratedResponse.AgentsInvoked.Count > 0 
            ? string.Join(", ", orchestratedResponse.AgentsInvoked.Select(a => a.ToString())) 
            : null;

        var assistantSnapshot = new MessageSnapshot
        {
            Role = "assistant",
            Content = orchestratedResponse.FinalResponse,
            Timestamp = DateTime.UtcNow,
            ToolExecuted = agentsInvoked
        };
        await UpdateContextAsync(context, assistantSnapshot, cancellationToken);
    }

    /// <summary>
    /// Generate proactive suggestions based on recently used agents.
    /// </summary>
    private List<ProactiveSuggestion> GenerateProactiveSuggestions(ConversationContext context)
    {
        try
        {
            var recentTools = context.UsedTools.TakeLast(3).ToList();
            var suggestions = new List<ProactiveSuggestion>();

            // Define agent-specific suggestion mappings
            var agentSuggestions = new Dictionary<string, List<ProactiveSuggestion>>
            {
                ["Infrastructure"] = new()
                {
                    new() { Title = "Run Compliance Assessment", Description = "Assess your infrastructure for ATO compliance", ToolName = "ComplianceAgent", Priority = "high" },
                    new() { Title = "Set Up Cost Monitoring", Description = "Monitor spending on new resources", ToolName = "CostManagementAgent", Priority = "medium" }
                },
                ["Compliance"] = new()
                {
                    new() { Title = "Review Environment Configuration", Description = "Ensure environment meets security requirements", ToolName = "EnvironmentAgent", Priority = "high" }
                },
                ["Cost"] = new()
                {
                    new() { Title = "Discover Resource Utilization", Description = "Find underutilized resources to optimize costs", ToolName = "DiscoveryAgent", Priority = "medium" }
                },
                ["Onboarding"] = new()
                {
                    new() { Title = "Provision Infrastructure", Description = "Provision infrastructure for your onboarded mission", ToolName = "InfrastructureAgent", Priority = "high" }
                }
            };

            // Add context-specific suggestions
            foreach (var tool in recentTools)
            {
                foreach (var (agentType, agentSuggestionsList) in agentSuggestions)
                {
                    if (tool.Contains(agentType, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.AddRange(agentSuggestionsList);
                    }
                }
            }

            // Default suggestions if no context-specific ones
            if (suggestions.Count == 0)
            {
                suggestions.AddRange(new[]
                {
                    new ProactiveSuggestion { Title = "Discover Azure Resources", Description = "Find and inventory your Azure resources", ToolName = "DiscoveryAgent", Priority = "low" },
                    new ProactiveSuggestion { Title = "Analyze Costs", Description = "Review Azure spending and optimization opportunities", ToolName = "CostManagementAgent", Priority = "low" },
                    new ProactiveSuggestion { Title = "Check Compliance Status", Description = "Assess infrastructure against NIST 800-53", ToolName = "ComplianceAgent", Priority = "low" }
                });
            }

            return suggestions.Take(3).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate proactive suggestions");
            return new List<ProactiveSuggestion>();
        }
    }

    /// <summary>
    /// Build a successful response from orchestrated results.
    /// </summary>
    private static IntelligentChatResponse BuildSuccessResponse(
        OrchestratedResponse orchestratedResponse,
        string conversationId,
        ConversationContext context,
        List<ProactiveSuggestion> suggestions,
        long processingTimeMs)
    {
        var agentsInvoked = string.Join(", ", orchestratedResponse.AgentsInvoked.Select(a => a.ToString()));
        
        return new IntelligentChatResponse
        {
            Response = orchestratedResponse.FinalResponse,
            Intent = new IntentClassificationResult
            {
                IntentType = orchestratedResponse.AgentsInvoked.Count > 0 ? "multi_agent_execution" : "conversational",
                ToolName = orchestratedResponse.AgentsInvoked.Count > 0 ? agentsInvoked : null,
                Confidence = 0.95,
                RequiresFollowUp = orchestratedResponse.RequiresFollowUp
            },
            ConversationId = conversationId,
            ToolExecuted = orchestratedResponse.AgentsInvoked.Count > 0,
            Suggestions = suggestions,
            Context = context,
            Metadata = new ResponseMetadata
            {
                ProcessingTimeMs = processingTimeMs,
                ModelUsed = "gpt-4o (multi-agent)"
            }
        };
    }

    /// <summary>
    /// Build a cancelled response.
    /// </summary>
    private static IntelligentChatResponse BuildCancelledResponse(string conversationId, long processingTimeMs)
    {
        return new IntelligentChatResponse
        {
            Response = "Request cancelled.",
            Intent = new IntentClassificationResult { IntentType = "cancelled", Confidence = 1.0 },
            ConversationId = conversationId,
            ToolExecuted = false,
            Metadata = new ResponseMetadata { ProcessingTimeMs = processingTimeMs, ModelUsed = "gpt-4o (multi-agent)" }
        };
    }

    /// <summary>
    /// Build an error response.
    /// </summary>
    private static IntelligentChatResponse BuildErrorResponse(string conversationId, string errorMessage, long processingTimeMs)
    {
        return new IntelligentChatResponse
        {
            Response = $"I encountered an error processing your request: {errorMessage}. Please try rephrasing your question or contact support if the issue persists.",
            Intent = new IntentClassificationResult { IntentType = "error", Confidence = 1.0 },
            ConversationId = conversationId,
            ToolExecuted = false,
            Metadata = new ResponseMetadata { ProcessingTimeMs = processingTimeMs, ModelUsed = "gpt-4o (multi-agent)" }
        };
    }
}
