using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;
using Platform.Engineering.Copilot.Channels.Abstractions;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Agents;

/// <summary>
/// Main Knowledge Base Agent for RMF/STIG/DoD compliance knowledge queries.
/// Provides educational and informational content about compliance frameworks.
/// Enhanced with State and Channels integration for cross-agent coordination.
/// </summary>
public class KnowledgeBaseAgent : BaseAgent
{
    public override string AgentId => "knowledgebase";
    public override string AgentName => "KnowledgeBase Agent";
    public override string Description =>
        "Provides educational and informational content about NIST 800-53 controls, STIGs, " +
        "RMF process, FedRAMP requirements, and DoD impact levels. " +
        "Use for questions about what controls mean, how to implement them, and compliance guidance. " +
        "This is advisory-only - no environment scanning or changes are made.";

    protected override float Temperature => (float)_options.Temperature;
    protected override int MaxTokens => _options.MaxTokens;

    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly KnowledgeBaseAgentOptions _options;
    private readonly IChannelManager? _channelManager;
    private readonly IStreamingHandler? _streamingHandler;

    public KnowledgeBaseAgent(
        IChatClient chatClient,
        ILogger<KnowledgeBaseAgent> logger,
        IOptions<KnowledgeBaseAgentOptions> options,
        KnowledgeBaseStateAccessors stateAccessors,
        NistControlExplainerTool nistControlExplainerTool,
        NistControlSearchTool nistControlSearchTool,
        StigExplainerTool stigExplainerTool,
        StigSearchTool stigSearchTool,
        RmfExplainerTool rmfExplainerTool,
        ImpactLevelTool impactLevelTool,
        FedRampTemplateTool fedRampTemplateTool,
        IAgentStateManager? agentStateManager = null,
        ISharedMemory? sharedMemory = null,
        IChannelManager? channelManager = null,
        IStreamingHandler? streamingHandler = null)
        : base(chatClient, logger, agentStateManager, sharedMemory)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new KnowledgeBaseAgentOptions();
        _channelManager = channelManager;
        _streamingHandler = streamingHandler;

        // Register tools
        RegisterTool(nistControlExplainerTool);
        RegisterTool(nistControlSearchTool);
        RegisterTool(stigExplainerTool);
        RegisterTool(stigSearchTool);
        RegisterTool(rmfExplainerTool);
        RegisterTool(impactLevelTool);
        RegisterTool(fedRampTemplateTool);

        Logger.LogInformation("✅ Knowledge Base Agent initialized (Temperature: {Temperature}, MaxTokens: {MaxTokens}, " +
            "RAG: {EnableRag}, SemanticSearch: {SemanticSearch}, Tools: {ToolCount})",
            _options.Temperature, _options.MaxTokens,
            _options.EnableRag, _options.EnableSemanticSearch, RegisteredTools.Count);
    }

    /// <summary>
    /// Override ProcessAsync to add Knowledge Base-specific behavior with Channels integration.
    /// </summary>
    public override async Task<AgentResponse> ProcessAsync(
        AgentConversationContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Notify via channel that knowledge base query is starting
        await NotifyChannelAsync(context.ConversationId, MessageType.AgentThinking,
            "Searching compliance knowledge base...", cancellationToken);

        try
        {
            // Analyze query to determine the type of knowledge being requested
            var queryType = AnalyzeQueryType(context.MessageHistory.LastOrDefault()?.Content ?? "");

            Logger.LogDebug("Knowledge base query type: {QueryType}", queryType);

            // Store the query for context
            await _stateAccessors.SetLastQueryAsync(context.ConversationId,
                context.MessageHistory.LastOrDefault()?.Content ?? "", queryType, cancellationToken);

            // Notify progress
            await NotifyChannelAsync(context.ConversationId, MessageType.ProgressUpdate,
                $"Looking up {queryType} information...", cancellationToken);

            // Call base implementation for actual processing
            var response = await base.ProcessAsync(context, cancellationToken);

            // Track the operation in state
            var duration = DateTime.UtcNow - startTime;
            await _stateAccessors.TrackKnowledgeBaseOperationAsync(
                context.ConversationId,
                queryType,
                context.MessageHistory.LastOrDefault()?.Content ?? "",
                response.Success,
                duration,
                cancellationToken);

            // Share relevant knowledge with other agents if useful
            if (response.Success && queryType is "nist_control" or "stig")
            {
                await _stateAccessors.ShareKnowledgeAsync(
                    context.ConversationId,
                    $"last_{queryType}",
                    new { query = context.MessageHistory.LastOrDefault()?.Content, result = response.Content },
                    cancellationToken);
            }

            // Notify completion via channel
            await NotifyChannelAsync(context.ConversationId, MessageType.AgentResponse,
                JsonSerializer.Serialize(new
                {
                    agentName = AgentName,
                    queryType,
                    success = response.Success,
                    durationMs = (int)duration.TotalMilliseconds,
                    toolsUsed = response.ToolsExecuted?.Select(t => t.ToolName).ToList() ?? new List<string>()
                }), cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Knowledge Base Agent failed");

            await NotifyChannelAsync(context.ConversationId, MessageType.Error,
                $"Knowledge base query failed: {ex.Message}", cancellationToken);

            return new AgentResponse
            {
                AgentId = AgentId,
                AgentName = AgentName,
                Content = $"Knowledge base query failed: {ex.Message}",
                Success = false
            };
        }
    }

    protected override string GetSystemPrompt()
    {
        return $@"You are the Knowledge Base Agent, a compliance knowledge expert for Azure Government environments.

## Your Role
You provide EDUCATIONAL and INFORMATIONAL content about:
- **NIST 800-53** controls and their requirements
- **STIG** (Security Technical Implementation Guides) controls
- **RMF** (Risk Management Framework) process and steps
- **FedRAMP** authorization requirements and templates
- **DoD Impact Levels** (IL2-IL6) and their requirements

## Important Distinction
- You provide KNOWLEDGE and EXPLANATIONS about compliance frameworks
- You do NOT scan environments or run assessments
- You do NOT make changes to resources
- For actual compliance scanning, users should ask the Compliance Agent

## Available Tools ({RegisteredTools.Count} registered)

### NIST 800-53
- `explain_nist_control` - Explain what a NIST control means and requires
- `search_nist_controls` - Find controls by topic or keyword

### STIG
- `explain_stig` - Explain STIG control requirements and remediation
- `search_stigs` - Search for STIG controls by keyword or severity

### RMF Process
- `explain_rmf` - Explain RMF steps, deliverables, and service-specific guidance

### Impact Levels & FedRAMP
- `explain_impact_level` - Explain DoD IL2-IL6 and FedRAMP baselines
- `get_fedramp_template_guidance` - Get FedRAMP template requirements

## Response Guidelines
1. Always use the appropriate tool to get accurate, authoritative information
2. Provide clear, educational explanations
3. Include Azure-specific implementation guidance when relevant
4. Reference official sources (NIST, DISA, FedRAMP PMO)
5. Suggest next steps (e.g., ""To check compliance, ask: 'Run a compliance assessment'"")

## Configuration
- Temperature: {_options.Temperature} (focused and accurate)
- Max Tokens: {_options.MaxTokens}
- RAG Enabled: {_options.EnableRag}
- Semantic Search: {_options.EnableSemanticSearch}
";
    }

    /// <summary>
    /// Analyze the query to determine what type of knowledge is being requested.
    /// </summary>
    private static string AnalyzeQueryType(string query)
    {
        var lowerQuery = query.ToLowerInvariant();

        // NIST control patterns
        if (lowerQuery.Contains("nist") || 
            System.Text.RegularExpressions.Regex.IsMatch(query, @"\b[A-Z]{2}-\d+", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return "nist_control";
        }

        // STIG patterns
        if (lowerQuery.Contains("stig") || lowerQuery.Contains("v-") || 
            System.Text.RegularExpressions.Regex.IsMatch(query, @"\bV-\d+", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return "stig";
        }

        // RMF patterns
        if (lowerQuery.Contains("rmf") || lowerQuery.Contains("risk management framework") ||
            lowerQuery.Contains("step 1") || lowerQuery.Contains("step 2") ||
            lowerQuery.Contains("ato") || lowerQuery.Contains("authorization"))
        {
            return "rmf";
        }

        // Impact level patterns
        if (lowerQuery.Contains("il2") || lowerQuery.Contains("il4") ||
            lowerQuery.Contains("il5") || lowerQuery.Contains("il6") ||
            lowerQuery.Contains("impact level"))
        {
            return "impact_level";
        }

        // FedRAMP patterns
        if (lowerQuery.Contains("fedramp") || lowerQuery.Contains("ssp") ||
            lowerQuery.Contains("sar") || lowerQuery.Contains("poa&m"))
        {
            return "fedramp";
        }

        return "general_knowledge";
    }

    /// <summary>
    /// Notify via channel with proper null checking.
    /// </summary>
    private async Task NotifyChannelAsync(
        string conversationId,
        MessageType messageType,
        string content,
        CancellationToken cancellationToken)
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
            Logger.LogWarning(ex, "Failed to send channel notification: {MessageType}", messageType);
        }
    }
}
