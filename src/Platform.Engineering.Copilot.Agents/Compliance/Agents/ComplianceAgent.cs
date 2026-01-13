using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Channels.Abstractions;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Configuration.Tools;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Agents.Compliance.State;
using Platform.Engineering.Copilot.Agents.Compliance.Tools;
using Platform.Engineering.Copilot.State.Abstractions;

namespace Platform.Engineering.Copilot.Agents.Compliance.Agents;

/// <summary>
/// Specialized agent for NIST 800-53 compliance assessment, remediation, and documentation.
/// Handles FedRAMP, DoD IL5, and other federal compliance frameworks.
/// </summary>
public class ComplianceAgent : BaseAgent
{
    private readonly ComplianceAgentOptions _options;
    private readonly ComplianceStateAccessors _stateAccessors;
    private readonly IChannelManager? _channelManager;
    private readonly IStreamingHandler? _streamingHandler;

    public override string AgentId => "compliance";
    public override string AgentName => "Compliance Agent";

    public override string Description =>
        "Specialized agent for NIST 800-53 compliance assessment and remediation. " +
        "Performs automated compliance scans, provides remediation guidance, generates " +
        "compliance documentation (SSP, SAR, POA&M), and collects evidence for audits. " +
        "Supports FedRAMP High, FedRAMP Moderate, and DoD IL5 baselines.";

    protected override float Temperature => (float)_options.Temperature;
    protected override int MaxTokens => _options.MaxTokens;

    public ComplianceAgent(
        IChatClient chatClient,
        ILogger<ComplianceAgent> logger,
        IOptions<ComplianceAgentOptions> options,
        ComplianceStateAccessors stateAccessors,
        ComplianceAssessmentTool assessmentTool,
        RemediationExecuteTool remediationTool,
        BatchRemediationTool batchRemediationTool,
        ControlFamilyTool controlFamilyTool,
        EvidenceCollectionTool evidenceTool,
        DocumentGenerationTool documentTool,
        DefenderForCloudTool defenderForCloudTool,
        ConfigurationTool configurationTool,
        IAgentStateManager? agentStateManager = null,
        ISharedMemory? sharedMemory = null,
        IChannelManager? channelManager = null,
        IStreamingHandler? streamingHandler = null)
        : base(chatClient, logger, agentStateManager, sharedMemory)
    {
        _options = options?.Value ?? new ComplianceAgentOptions();
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _channelManager = channelManager;
        _streamingHandler = streamingHandler;

        // Register configuration tool first (for subscription setup)
        RegisterTool(configurationTool);
        
        // Register all compliance tools
        RegisterTool(assessmentTool);
        RegisterTool(remediationTool);
        RegisterTool(batchRemediationTool);
        RegisterTool(controlFamilyTool);
        RegisterTool(evidenceTool);
        RegisterTool(documentTool);
        RegisterTool(defenderForCloudTool);

        Logger.LogInformation("ComplianceAgent initialized with {ToolCount} tools. Framework: {Framework}, Baseline: {Baseline}, DFC Enabled: {DfcEnabled}",
            RegisteredTools.Count, _options.DefaultFramework, _options.DefaultBaseline, _options.DefenderForCloud.Enabled);
    }

    protected override string GetSystemPrompt()
    {
        return $@"You are a specialized Compliance Agent for Azure Government infrastructure security and compliance.

## Subscription Configuration
- If a subscription ID is provided in the context or the user's request, USE IT for all operations
- The system automatically loads the user's configured default subscription
- Only ask the user to configure a subscription if NO subscription is available in context

## Core Responsibilities
- Assess Azure resources against NIST 800-53 control families
- Provide remediation guidance for compliance findings
- Generate compliance documentation (SSP, SAR, POA&M)
- Collect and manage audit evidence
- Support FedRAMP and DoD authorization processes

## Compliance Framework Knowledge
Default Framework: {_options.DefaultFramework}
Default Baseline: {_options.DefaultBaseline}

### Supported Frameworks
- NIST 800-53 Rev 5 (all control families)
- FedRAMP High Baseline
- FedRAMP Moderate Baseline
- DoD IL5 (Impact Level 5)

### Control Families You Handle
- AC (Access Control) - Identity, RBAC, conditional access
- AU (Audit) - Logging, monitoring, log analytics
- SC (System Communications) - Network security, encryption
- IA (Identification & Authentication) - MFA, identity providers
- CM (Configuration Management) - Azure Policy, baselines
- RA (Risk Assessment) - Vulnerability scanning, threat modeling
- SI (System & Information Integrity) - Defender, security patches
- CA (Security Assessment) - Continuous monitoring
- CP (Contingency Planning) - Backup, disaster recovery
- MP (Media Protection) - Storage encryption, key management

## Remediation Boundaries
YOU handle configuration-level remediation:
- Tag updates for compliance tracking
- Encryption settings (storage, SQL TDE)
- Firewall and NSG rule updates
- Diagnostic settings for logging
- Azure Policy assignments

Infrastructure Agent handles resource-level changes:
- Creating/deleting resources
- Network topology changes
- Resource provisioning

## High-Risk Control Families
{string.Join(", ", _options.Remediation.HighRiskControlFamilies)} require explicit confirmation before remediation.

## Response Guidelines
1. Always cite specific control IDs (e.g., AC-2, SC-7)
2. Explain compliance gaps in business terms
3. Prioritize findings by risk severity
4. Provide actionable remediation steps
5. Reference Azure-specific implementations

## Dry-Run Mode
Dry-run mode is {(_options.Remediation.DryRunByDefault ? "ENABLED" : "DISABLED")} by default.
Always show what changes would be made before applying them.

## Multi-Turn Conversation Context
CRITICAL: When the user asks follow-up questions after an assessment, USE THE CACHED ASSESSMENT.
- Do NOT re-run assessments when user says ""start remediation"", ""fix issues"", ""remediate findings""
- The cached assessment contains the findings - use batch_remediation or generate_remediation_plan
- Always check for existing assessment before asking user for subscription ID again

## Remediation Tool Selection
Choose the right remediation tool based on user intent:

1. **batch_remediation** - Use when user says:
   - ""Start remediation"", ""Fix high-priority issues"", ""Remediate critical findings""
   - ""Execute automated remediation"", ""Fix all violations""
   - Uses cached assessment, filters by severity, executes batch fixes

2. **generate_remediation_plan** - Use when user says:
   - ""Generate remediation plan"", ""Create action plan"", ""Show remediation steps""
   - ""How do I fix these findings?"", ""What's the remediation roadmap?""
   - Creates detailed step-by-step plan without executing

3. **execute_remediation** - Use only for single finding remediation:
   - ""Remediate finding <finding_id>"", ""Fix finding XYZ""
   - Requires specific finding_id parameter

When asked about compliance, use your tools to:
1. Run assessments with run_compliance_assessment
2. Get control details with get_control_family_details
3. Start batch remediation with batch_remediation (for ""start remediation"" requests)
4. Generate remediation plan with generate_remediation_plan (for planning)
5. Execute single finding with execute_remediation (requires finding_id)
6. Collect evidence with collect_evidence
7. Generate documentation with generate_compliance_document
8. Get Defender for Cloud findings with get_defender_findings (for DFC-specific queries)

## Microsoft Defender for Cloud Integration
DFC Integration Enabled: {_options.DefenderForCloud.Enabled}

Use **get_defender_findings** when user asks:
- ""Show defender findings"", ""Get secure score"", ""DFC recommendations""
- ""What does security center say?"", ""Defender for cloud status""
- ""Security recommendations"", ""Azure security findings""

DFC findings are automatically mapped to NIST 800-53 controls when MapToNistControls is enabled.
The run_compliance_assessment tool also incorporates DFC findings when DFC is enabled.";
    }

    public override async Task<AgentResponse> ProcessAsync(
        AgentConversationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation("ComplianceAgent processing request for conversation: {ConversationId}",
                context.ConversationId);

            // ✅ CRITICAL: Check for configured subscription FIRST
            var configuredSubscription = await _stateAccessors.GetCurrentSubscriptionAsync(
                context.ConversationId, cancellationToken);
            
            if (!string.IsNullOrEmpty(configuredSubscription) && string.IsNullOrEmpty(context.SubscriptionId))
            {
                // Inject the configured subscription into the context
                context.SubscriptionId = configuredSubscription;
                Logger.LogInformation("📋 Using configured subscription: {SubscriptionId}", configuredSubscription);
                
                // Add system note to WorkflowState so LLM knows the subscription is available
                context.WorkflowState["configuredSubscription"] = configuredSubscription;
                context.WorkflowState["subscriptionNote"] = 
                    $"NOTE: User has a configured default subscription: {configuredSubscription}. " +
                    "Use this subscription for compliance operations unless user specifies otherwise.";
            }

            // Track the operation
            await _stateAccessors.TrackComplianceOperationAsync(
                context.ConversationId,
                "process_request",
                _options.DefaultFramework,
                context.SubscriptionId ?? "",
                true,
                0,
                TimeSpan.Zero,
                cancellationToken);

            // Notify channel about processing start
            await NotifyChannelAsync(context.ConversationId, MessageType.ProgressUpdate, "Analyzing compliance request...", cancellationToken);

            // Analyze intent for specialized handling
            var userMessage = context.MessageHistory.LastOrDefault(m => m.IsUser)?.Content ?? "";
            var intent = AnalyzeComplianceIntent(userMessage);

            Logger.LogDebug("Detected compliance intent: {Intent}", intent);

            // Stream intent analysis if handler available
            if (_streamingHandler != null)
            {
                await using var stream = await _streamingHandler.BeginStreamAsync(context.ConversationId, AgentId, cancellationToken);
                await stream.WriteAsync($"[Compliance] Detected intent: {intent}", cancellationToken);
            }

            // Use base agent processing with tool execution
            var response = await base.ProcessAsync(context, cancellationToken);

            // Notify channel about completion
            await NotifyChannelAsync(context.ConversationId, MessageType.AgentResponse,
                $"Compliance analysis complete. Intent: {intent}", cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in ComplianceAgent.ProcessAsync");
            return new AgentResponse
            {
                Success = false,
                AgentId = AgentId,
                Content = $"An error occurred during compliance processing: {ex.Message}"
            };
        }
    }

    private string AnalyzeComplianceIntent(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        // Document generation intents
        if (lowerMessage.Contains("ssp") || lowerMessage.Contains("system security plan"))
            return "generate_ssp";
        if (lowerMessage.Contains("sar") || lowerMessage.Contains("security assessment"))
            return "generate_sar";
        if (lowerMessage.Contains("poam") || lowerMessage.Contains("plan of action"))
            return "generate_poam";
        if (lowerMessage.Contains("document") || lowerMessage.Contains("generate"))
            return "document_generation";

        // Assessment intents
        if (lowerMessage.Contains("assess") || lowerMessage.Contains("scan") || lowerMessage.Contains("check compliance"))
            return "compliance_assessment";
        if (lowerMessage.Contains("audit") || lowerMessage.Contains("review"))
            return "compliance_audit";

        // Remediation intents
        if (lowerMessage.Contains("remediate") || lowerMessage.Contains("fix") || lowerMessage.Contains("resolve"))
            return "remediation";

        // Control lookup intents
        if (lowerMessage.Contains("control") || ContainsControlId(lowerMessage))
            return "control_lookup";

        // Evidence intents
        if (lowerMessage.Contains("evidence") || lowerMessage.Contains("collect"))
            return "evidence_collection";

        // Framework-specific intents
        if (lowerMessage.Contains("fedramp"))
            return "fedramp_assessment";
        if (lowerMessage.Contains("nist") || lowerMessage.Contains("800-53"))
            return "nist_assessment";
        if (lowerMessage.Contains("il5") || lowerMessage.Contains("dod"))
            return "dod_assessment";

        return "general_compliance";
    }

    private bool ContainsControlId(string message)
    {
        // Check for control ID patterns like AC-2, AU-6, SC-7, etc.
        var controlFamilies = new[] { "ac", "au", "sc", "ia", "cm", "ra", "si", "ca", "cp", "mp", "at", "pe", "ps", "ir" };
        return controlFamilies.Any(f => System.Text.RegularExpressions.Regex.IsMatch(message, $@"\b{f}-\d+\b"));
    }

    /// <summary>
    /// Override to inject subscription context into the chat messages.
    /// </summary>
    protected override List<ChatMessage> BuildChatMessages(AgentConversationContext context)
    {
        var messages = base.BuildChatMessages(context);
        
        // If we have a configured subscription, inject a system note about it
        if (context.WorkflowState.TryGetValue("subscriptionNote", out var note) && note != null)
        {
            // Insert after the main system prompt
            messages.Insert(1, new ChatMessage(ChatRole.System, note.ToString()!));
        }
        
        return messages;
    }

    private async Task NotifyChannelAsync(string conversationId, MessageType messageType, string message, CancellationToken cancellationToken)
    {
        if (_channelManager == null) return;

        try
        {
            var channelMessage = new ChannelMessage
            {
                Type = messageType,
                ConversationId = conversationId,
                AgentType = AgentId,
                Content = message,
                Timestamp = DateTime.UtcNow
            };
            await _channelManager.SendToConversationAsync(conversationId, channelMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send channel notification");
        }
    }
}
