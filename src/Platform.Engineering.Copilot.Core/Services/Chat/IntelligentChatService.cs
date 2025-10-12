using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;
using Platform.Engineering.Copilot.Core.Plugins;
using Platform.Engineering.Copilot.Core.Services.AzureServices;
using Platform.Engineering.Copilot.Core.Services.Cache;
using Platform.Engineering.Copilot.Core.Services.Compliance;
using Platform.Engineering.Copilot.Core.Services.Infrastructure;

namespace Platform.Engineering.Copilot.Core.Services;

/// <summary>
/// AI-powered intelligent chat service using Semantic Kernel automatic function calling
/// Simplified architecture using SK plugins instead of manual intent classification
/// </summary>
public class IntelligentChatService : IIntelligentChatService
{
    private readonly ISemanticKernelService _semanticKernel;
    private readonly ILogger<IntelligentChatService> _logger;
    private readonly Kernel _kernel;
    private readonly IChatCompletionService? _chatCompletion;
    private readonly IIntelligentChatCacheService? _cacheService;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private bool _pluginsRegistered = false;
    private readonly object _pluginLock = new();
    
    // In-memory conversation store (replace with distributed cache in production)
    private static readonly Dictionary<string, ConversationContext> _conversations = new();
    private static readonly object _conversationLock = new();

    public IntelligentChatService(
        ISemanticKernelService semanticKernel,
        Kernel kernel,
        ILogger<IntelligentChatService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IIntelligentChatCacheService? cacheService = null)
    {
        _semanticKernel = semanticKernel;
        _kernel = kernel;
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _cacheService = cacheService;
        
        // Get chat completion service from kernel
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        
        // Register plugins now that we have access to serviceProvider
        EnsurePluginsRegistered();
    }
    
    /// <summary>
    /// Registers all plugins on the kernel using the service provider.
    /// This is done here instead of in the Kernel factory to avoid circular dependencies.
    /// </summary>
    private void EnsurePluginsRegistered()
    {
        if (_pluginsRegistered) return;
        
        lock (_pluginLock)
        {
            if (_pluginsRegistered) return;
            
            var pluginsRegistered = 0;
            _logger.LogInformation("🔧 Registering plugins on Kernel...");
            
            // OnboardingPlugin
            try
            {
                var onboardingService = _serviceProvider.GetService<IOnboardingService>();
                if (onboardingService != null)
                {
                    _kernel.Plugins.AddFromObject(
                        new OnboardingPlugin(
                            _serviceProvider.GetRequiredService<ILogger<OnboardingPlugin>>(),
                            _kernel,
                            onboardingService),
                        "Onboarding");
                    pluginsRegistered++;
                    _logger.LogInformation("✅ Registered OnboardingPlugin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to register OnboardingPlugin");
            }
            
            // InfrastructurePlugin
            try
            {
                var infraService = _serviceProvider.GetService<IInfrastructureProvisioningService>();
                if (infraService != null)
                {
                    _kernel.Plugins.AddFromObject(
                        new InfrastructurePlugin(
                            _serviceProvider.GetRequiredService<ILogger<InfrastructurePlugin>>(),
                            _kernel,
                            infraService),
                        "Infrastructure");
                    pluginsRegistered++;
                    _logger.LogInformation("✅ Registered InfrastructurePlugin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to register InfrastructurePlugin");
            }
            
            // CompliancePlugin
            try
            {
                var complianceEngine = _serviceProvider.GetService<IAtoComplianceEngine>();
                var remediationEngine = _serviceProvider.GetService<IAtoRemediationEngine>();
                if (complianceEngine != null && remediationEngine != null)
                {
                    _kernel.Plugins.AddFromObject(
                        new CompliancePlugin(
                            _serviceProvider.GetRequiredService<ILogger<CompliancePlugin>>(),
                            _kernel,
                            complianceEngine,
                            remediationEngine),
                        "Compliance");
                    pluginsRegistered++;
                    _logger.LogInformation("✅ Registered CompliancePlugin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to register CompliancePlugin");
            }
            
            // CostManagementPlugin
            try
            {
                var costManagementService = _serviceProvider.GetService<IAzureCostManagementService>();
                var costOptimizationEngine = _serviceProvider.GetService<ICostOptimizationEngine>();
                if (costManagementService != null && costOptimizationEngine != null)
                {
                    _kernel.Plugins.AddFromObject(
                        new CostManagementPlugin(
                            _serviceProvider.GetRequiredService<ILogger<CostManagementPlugin>>(),
                            _kernel,
                            costOptimizationEngine,
                            costManagementService),
                        "CostManagement");
                    pluginsRegistered++;
                    _logger.LogInformation("✅ Registered CostManagementPlugin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to register CostManagementPlugin");
            }
            
            // EnvironmentManagementPlugin
            try
            {
                var environmentEngine = _serviceProvider.GetService<IEnvironmentManagementEngine>();
                if (environmentEngine != null)
                {
                    var onboardingService = _serviceProvider.GetService<IOnboardingService>();
                    var environmentStorage = _serviceProvider.GetService<EnvironmentStorageService>();
                    
                    if (onboardingService != null && environmentStorage != null)
                    {
                        _kernel.Plugins.AddFromObject(
                            new EnvironmentManagementPlugin(
                                _serviceProvider.GetRequiredService<ILogger<EnvironmentManagementPlugin>>(),
                                _kernel,
                                environmentEngine,
                                onboardingService,
                                environmentStorage),
                            "EnvironmentManagement");
                        pluginsRegistered++;
                        _logger.LogInformation("✅ Registered EnvironmentManagementPlugin with full dependencies");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️  OnboardingService or EnvironmentStorageService not available - skipping EnvironmentManagementPlugin");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to register EnvironmentManagementPlugin");
            }
            
            _logger.LogInformation("✅ All plugins registered: {PluginCount} total", pluginsRegistered);
            _pluginsRegistered = true;
        }
    }

    /// <summary>
    /// Process a user message with AI-powered automatic function calling
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
            _logger.LogInformation("🚀 [STEP 1/6] Processing message for conversation {ConversationId}", conversationId);

            // Get or create conversation context
            _logger.LogInformation("🚀 [STEP 2/6] Getting or creating context...");
            context ??= await GetOrCreateContextAsync(conversationId, cancellationToken: cancellationToken);
            _logger.LogInformation("✅ [STEP 2/6] Context obtained");

            // Build chat history from context
            _logger.LogInformation("🚀 [STEP 3/6] Building chat history...");
            var chatHistory = BuildChatHistory(context);
            chatHistory.AddUserMessage(message);
            _logger.LogInformation("✅ [STEP 3/6] Chat history built with {MessageCount} messages", chatHistory.Count);

            // Configure Semantic Kernel for function calling
            _logger.LogInformation("🚀 [STEP 4/6] Configuring Semantic Kernel settings...");
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,  // Auto-invoke enabled (circular dependency fixed)
                Temperature = 0.3,  // Lower temperature for faster, more deterministic function selection
                MaxTokens = 4096  // Maximum supported by gpt-4o model (increased from 2000)
            };
            _logger.LogInformation("✅ [STEP 4/6] Settings configured");

            _logger.LogInformation("🚀 [STEP 5/6] Invoking Semantic Kernel with {PluginCount} plugins registered", _kernel.Plugins.Count);
            
            // Log all registered plugins and their functions
            foreach (var plugin in _kernel.Plugins)
            {
                _logger.LogInformation("  Plugin: {PluginName} with {FunctionCount} functions", 
                    plugin.Name, plugin.Count());
                foreach (var function in plugin)
                {
                    _logger.LogInformation("    - {FunctionName}", function.Name);
                }
            }

            // Ensure chat completion service is available
            if (_chatCompletion == null)
            {
                _logger.LogError("Chat completion service is null");
                throw new InvalidOperationException("Azure OpenAI chat completion service not configured.");
            }

            // Semantic Kernel automatically:
            // 1. Discovers available functions from registered plugins
            // 2. Determines which function(s) to call based on user message
            // 3. Extracts parameters from natural language
            // 4. Invokes the function(s)
            // 5. Returns the result
            
            _logger.LogInformation("⏱️ Starting SK GetChatMessageContentAsync...");
            var skStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var result = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: settings,
                kernel: _kernel,
                cancellationToken: cancellationToken);
            
            skStopwatch.Stop();
            _logger.LogInformation("✅ [STEP 6/6] SK execution completed in {ElapsedMs}ms. Response length: {Length} chars", 
                skStopwatch.ElapsedMilliseconds, result.Content?.Length ?? 0);

            // Update context with assistant response
            var userSnapshot = new MessageSnapshot
            {
                Role = "user",
                Content = message,
                Timestamp = DateTime.UtcNow
            };
            await UpdateContextAsync(context, userSnapshot, cancellationToken);

            var assistantSnapshot = new MessageSnapshot
            {
                Role = "assistant",
                Content = result.Content ?? "No response generated",
                Timestamp = DateTime.UtcNow
            };
            await UpdateContextAsync(context, assistantSnapshot, cancellationToken);

            // Extract metadata from SK execution
            var functionCalls = ExtractFunctionCalls(result);
            var toolExecuted = functionCalls?.Count > 0;
            var toolName = toolExecuted ? GetFirstFunctionName(functionCalls) : null;

            if (toolExecuted)
            {
                _logger.LogInformation("Tool executed: {ToolName}, Function calls: {Count}", toolName, functionCalls?.Count);
            }

            // Generate proactive suggestions
            var suggestions = await GenerateProactiveSuggestionsAsync(
                conversationId, context, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Processed message in {ElapsedMs}ms. Tool executed: {ToolExecuted}, Tool: {ToolName}", 
                stopwatch.ElapsedMilliseconds, 
                toolExecuted,
                toolName ?? "none");

            return new IntelligentChatResponse
            {
                Response = result.Content ?? "No response generated",
                Intent = new IntentClassificationResult
                {
                    IntentType = toolExecuted ? "tool_execution" : "conversational",
                    ToolName = toolName,
                    Confidence = 0.95, // SK has high confidence in its function selection
                    RequiresFollowUp = false
                },
                ConversationId = conversationId,
                ToolExecuted = toolExecuted,
                Suggestions = suggestions,
                Context = context,
                Metadata = new ResponseMetadata
                {
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ModelUsed = "gpt-4o" // TODO: Extract from kernel config
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for conversation {ConversationId}", conversationId);
            
            // Return user-friendly error
            return new IntelligentChatResponse
            {
                Response = $"I encountered an error processing your request: {ex.Message}. Please try rephrasing your question or contact support if the issue persists.",
                Intent = new IntentClassificationResult
                {
                    IntentType = "error",
                    Confidence = 1.0
                },
                ConversationId = conversationId,
                ToolExecuted = false,
                Metadata = new ResponseMetadata
                {
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ModelUsed = "gpt-4"
                }
            };
        }
    }

    /// <summary>
    /// Build ChatHistory from ConversationContext with simplified system prompt
    /// </summary>
    private ChatHistory BuildChatHistory(ConversationContext context)
    {
        var history = new ChatHistory();
        
        // Comprehensive system prompt for all platform engineering capabilities
        history.AddSystemMessage(@"You are an expert Azure cloud platform assistant for US Navy Platform Engineering.

**Core Responsibilities:**
- Mission onboarding and environment provisioning
- Infrastructure management and optimization
- ATO (Authority to Operate) compliance and security
- Cost analysis and optimization
- Environment lifecycle operations

**Key Workflows:**

1. **Mission Onboarding** (CRITICAL - Read Carefully):
   STEP 1: Gather requirements
   - Call capture_onboarding_requirements(missionName, organization, details)
   - Show detailed review summary to user
   
   STEP 2: Get user confirmation
   - Wait for user to confirm ('yes', 'proceed', 'confirm', 'go ahead')
   
   STEP 3: Submit for platform team approval
   - Call submit_for_approval(requestId, userEmail)
   - DO NOT call create_environment at this step!
   - The request enters admin approval queue
   
   STEP 4: Platform team reviews in Admin Console
   - Team reviews requirements and approves/denies
   - If approved, provisioning starts AUTOMATICALLY
   
   STEP 5: User receives email notification
   - Notified when approved or if changes needed
   
   IMPORTANT: NEVER call create_environment directly during onboarding!
   The submit_for_approval function handles the approval workflow.
   Only platform team can trigger provisioning via admin console.

2. **Environment Management**:
   - create_environment: Provision new environments (platform team or direct requests)
   - clone_environment: Clone existing environments
   - scale_environment: Scale resources up/down
   - delete_environment: Remove environments (with backup)
   - get_environment_status: Check health and metrics

3. **Infrastructure Operations**:
   - provision_infrastructure: Deploy Azure resources
   - list_resource_groups: View resource groups
   - delete_resource_group: Remove resource groups

4. **ATO Compliance & Security**:
   - process_compliance_query: Run assessments, collect evidence, generate reports
     Examples:
     * 'Run compliance assessment for subscription xyz'
     * 'Collect evidence for AC-2 control'
     * 'Generate FedRAMP report in PDF format'
     * 'Check compliance status for DoD IL5'
     * 'Assess risks for NIST 800-53 controls'
     * 'Generate ATO compliance certificate'
     * 'Monitor continuous compliance'
     * 'Remediate security findings'

5. **Cost Management**:
   - analyze_cost_query: Cost analysis and recommendations

**Important Rules:**
- ALWAYS get user confirmation before creating/deleting environments or provisioning infrastructure
- Use capture_onboarding_requirements FIRST for new mission onboarding
- After user confirms onboarding, use submit_for_approval (NOT create_environment)
- For ATO/compliance requests, use process_compliance_query with natural language
- Be concise, technical, and security-conscious
- Provide clear next steps and estimated timeframes");

        // Add recent conversation history (last 10 messages for context)
        foreach (var msg in context.MessageHistory.TakeLast(10))
        {
            if (msg.Role == "user")
                history.AddUserMessage(msg.Content);
            else if (msg.Role == "assistant")
                history.AddAssistantMessage(msg.Content);
        }

        return history;
    }

    /// <summary>
    /// Extract function calls from SK result metadata
    /// </summary>
    private List<object>? ExtractFunctionCalls(ChatMessageContent result)
    {
        try
        {
            if (result.Metadata == null)
                return null;

            // Try different metadata keys that SK might use
            var possibleKeys = new[] { "FunctionCalls", "ToolCalls", "function_calls", "tool_calls" };
            
            foreach (var key in possibleKeys)
            {
                if (result.Metadata.TryGetValue(key, out var value) && value is IList<object> calls)
                {
                    return calls.ToList();
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract function calls from result metadata");
            return null;
        }
    }

    /// <summary>
    /// Extract first function name from function calls metadata
    /// </summary>
    private string? GetFirstFunctionName(IList<object>? functionCalls)
    {
        if (functionCalls == null || functionCalls.Count == 0)
            return null;
        
        try
        {
            var firstCall = functionCalls[0];
            var type = firstCall.GetType();
            
            // Try common property names for function name
            var possibleProps = new[] { "FunctionName", "Name", "name", "function_name", "ToolName", "tool_name" };
            
            foreach (var propName in possibleProps)
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(firstCall);
                    if (value != null)
                        return value.ToString();
                }
            }

            return firstCall.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract function name");
            return null;
        }
    }

    /// <summary>
    /// Generate proactive suggestions based on conversation context
    /// </summary>
    public async Task<List<ProactiveSuggestion>> GenerateProactiveSuggestionsAsync(
        string conversationId,
        ConversationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            context ??= await GetOrCreateContextAsync(conversationId, cancellationToken: cancellationToken);

            var suggestions = new List<ProactiveSuggestion>();

            // Get recent context
            var recentMessages = context.MessageHistory.TakeLast(5).ToList();
            var recentTools = context.UsedTools.TakeLast(3).ToList();

            // Suggest next steps based on recently used tools
            if (recentTools.Contains("infrastructure_provisioning"))
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Title = "Run Compliance Assessment",
                    Description = "Assess your newly provisioned infrastructure for ATO compliance",
                    ToolName = "run_compliance_assessment",
                    Priority = "high"
                });

                suggestions.Add(new ProactiveSuggestion
                {
                    Title = "Set Up Cost Monitoring",
                    Description = "Configure budget alerts to monitor spending on new resources",
                    ToolName = "process_cost_management_query",
                    Priority = "medium"
                });
            }

            if (recentTools.Contains("ato_compliance"))
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Title = "Apply Security Hardening",
                    Description = "Implement recommended security controls from compliance assessment",
                    ToolName = "apply_security_hardening",
                    Priority = "high"
                });

                suggestions.Add(new ProactiveSuggestion
                {
                    Title = "Collect Compliance Evidence",
                    Description = "Start collecting evidence for ATO documentation",
                    ToolName = "collect_compliance_evidence",
                    Priority = "medium"
                });
            }

            // General helpful suggestions
            if (suggestions.Count == 0)
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Title = "Discover Azure Resources",
                    Description = "Find and inventory your Azure resources",
                    ToolName = "discover_azure_resources",
                    Priority = "low"
                });

                suggestions.Add(new ProactiveSuggestion
                {
                    Title = "Analyze Costs",
                    Description = "Review your Azure spending and find optimization opportunities",
                    ToolName = "process_cost_management_query",
                    Priority = "low"
                });
            }

            return suggestions.Take(3).ToList(); // Limit to 3 suggestions
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate proactive suggestions");
            return new List<ProactiveSuggestion>();
        }
    }

    /// <summary>
    /// Get or create conversation context
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
            
            _logger.LogInformation("Created new conversation context: {ConversationId}", conversationId);
            
            return Task.FromResult(newContext);
        }
    }

    /// <summary>
    /// Update conversation context with new message
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

            // Track used tools
            if (!string.IsNullOrEmpty(message.ToolExecuted) && !context.UsedTools.Contains(message.ToolExecuted))
            {
                context.UsedTools.Add(message.ToolExecuted);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invalidate cache for a conversation (if caching is enabled)
    /// </summary>
    public async Task InvalidateCacheAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (_cacheService != null)
        {
            // Remove intent classification cache for this conversation
            var cacheKey = _cacheService.GenerateCacheKey("intent", conversationId);
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogInformation("Invalidated cache for conversation {ConversationId}", conversationId);
        }
    }

    /// <summary>
    /// NOTE: ExecuteToolChainAsync is NO LONGER NEEDED in v2
    /// Semantic Kernel handles multi-step operations automatically.
    /// This stub is kept for interface compatibility.
    /// </summary>
    [Obsolete("Use ProcessMessageAsync instead - SK handles tool chains automatically")]
    public Task<ToolChainResult> ExecuteToolChainAsync(
        List<ToolStep> steps,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ExecuteToolChainAsync called but is obsolete in v2. SK handles multi-step operations automatically.");
        
        return Task.FromResult(new ToolChainResult
        {
            Status = "failed",
            Steps = steps ?? new List<ToolStep>()
        });
    }

    /// <summary>
    /// NOTE: ClassifyIntentAsync is NO LONGER NEEDED in v2
    /// Semantic Kernel handles intent classification automatically via function calling.
    /// This stub is kept for interface compatibility if needed.
    /// </summary>
    [Obsolete("Use ProcessMessageAsync instead - SK handles intent classification automatically")]
    public Task<IntentClassificationResult> ClassifyIntentAsync(
        string message,
        ConversationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ClassifyIntentAsync called but is obsolete in v2. Use ProcessMessageAsync instead.");
        
        return Task.FromResult(new IntentClassificationResult
        {
            IntentType = "conversational",
            Confidence = 0.5
        });
    }

}
