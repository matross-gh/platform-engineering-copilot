using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Services.Compliance;

/// <summary>
/// AI-enhanced remediation plan generator using Azure OpenAI.
/// Optional service that provides advanced AI features when configured.
/// </summary>
public class AiRemediationPlanGenerator : IAiRemediationPlanGenerator
{
    private readonly IChatClient? _chatClient;
    private readonly INistControlsService? _nistService;
    private readonly INistRemediationStepsService? _remediationStepsService;
    private readonly ILogger<AiRemediationPlanGenerator> _logger;

    public bool IsAvailable => _chatClient != null;

    public AiRemediationPlanGenerator(
        ILogger<AiRemediationPlanGenerator> logger,
        IChatClient? chatClient = null,
        INistControlsService? nistService = null,
        INistRemediationStepsService? remediationStepsService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatClient = chatClient;
        _nistService = nistService;
        _remediationStepsService = remediationStepsService;

        if (_chatClient != null)
        {
            _logger.LogInformation("AI Remediation Plan Generator initialized with Azure OpenAI");
        }
        else
        {
            _logger.LogInformation("AI Remediation Plan Generator initialized without AI (Azure OpenAI not configured)");
        }
    }

    public async Task<RemediationPlan?> GenerateAiEnhancedPlanAsync(
        AtoFinding finding,
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            _logger.LogWarning("AI features not available - Azure OpenAI not configured");
            return null;
        }

        _logger.LogInformation("Generating AI-enhanced remediation plan for finding {FindingId}", finding.Id);

        try
        {
            var controlId = finding.AffectedControls.FirstOrDefault() ?? "Unknown";
            var control = _nistService != null ? await _nistService.GetControlAsync(controlId, cancellationToken) : null;
            var availableActions = _remediationStepsService != null 
                ? await _remediationStepsService.GetAvailableActionsAsync(controlId)
                : Array.Empty<RemediationAction>();
            var resourceState = await GetResourceStateAsync(finding.ResourceId, cancellationToken);

            var prompt = $@"Generate a detailed remediation plan for this Azure compliance finding:

Control: {controlId} - {control?.Title ?? controlId}
Finding: {finding.Description}
Severity: {finding.Severity}
Resource: {finding.ResourceId}

**Current Resource State:**
{JsonSerializer.Serialize(resourceState, new JsonSerializerOptions { WriteIndented = true })}

**Available Actions:**
{string.Join("\n", availableActions.Select(a => $"- {a.Action}: {a.Description}"))}

Generate:
1. Exact Azure CLI commands to remediate (one per line)
2. Impact assessment (what will change)
3. Potential risks
4. Estimated duration in minutes

Return as JSON:
{{
  ""commands"": [""az ....."", ""az ...""],
  ""impact"": ""description"",
  ""risks"": [""risk1"", ""risk2""],
  ""estimatedMinutes"": 30
}}";

            var chatHistory = new List<ChatMessage>
            {
                new(ChatRole.System, "You are an Azure compliance remediation expert. Generate precise, production-ready remediation plans. Always return valid JSON."),
                new(ChatRole.User, prompt)
            };

            var response = await _chatClient.GetResponseAsync(chatHistory, cancellationToken: cancellationToken);
            var aiPlan = JsonSerializer.Deserialize<AiRemediationPlanResponse>(
                ExtractJsonFromResponse(response.Text ?? "{}"));

            if (aiPlan == null)
            {
                _logger.LogWarning("Failed to parse AI remediation plan response");
                return null;
            }

            // Convert AI response to RemediationPlan
            var plan = new RemediationPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                SubscriptionId = finding.SubscriptionId,
                CreatedAt = DateTimeOffset.UtcNow,
                TotalFindings = 1,
                RemediationItems = new List<RemediationItem>
                {
                    new RemediationItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        FindingId = finding.Id,
                        ControlId = controlId,
                        Title = $"AI-Enhanced Remediation for {finding.Title}",
                        ResourceId = finding.ResourceId,
                        Priority = aiPlan.Impact ?? "Medium",
                        Status = AtoRemediationStatus.NotStarted,
                        IsAutomated = aiPlan.Commands?.Any() == true,
                        AutomationAvailable = aiPlan.Commands?.Any() == true,
                        EstimatedEffort = TimeSpan.FromMinutes(aiPlan.EstimatedMinutes > 0 ? aiPlan.EstimatedMinutes : 30),
                        Steps = aiPlan.Commands?.Select((cmd, idx) => new RemediationStep
                        {
                            Order = idx + 1,
                            Description = cmd,
                            Command = cmd
                        }).ToList(),
                        ValidationSteps = aiPlan.Risks ?? new List<string>()
                    }
                },
                EstimatedEffort = TimeSpan.FromMinutes(aiPlan.EstimatedMinutes > 0 ? aiPlan.EstimatedMinutes : 30),
                Priority = aiPlan.Impact ?? "Medium"
            };

            _logger.LogInformation("Successfully generated AI-enhanced remediation plan for {FindingId}", finding.Id);
            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI-enhanced remediation plan for {FindingId}", finding.Id);
            return null;
        }
    }

    public async Task<RemediationScript> GenerateRemediationScriptAsync(
        AtoFinding finding,
        string scriptType = "AzureCLI",
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            throw new InvalidOperationException("AI features not available - Azure OpenAI not configured");
        }

        _logger.LogInformation("Generating {ScriptType} remediation script for finding {FindingId}",
            scriptType, finding.Id);

        var controlId = finding.AffectedControls.FirstOrDefault() ?? "Unknown";
        var control = _nistService != null ? await _nistService.GetControlAsync(controlId, cancellationToken) : null;
        var availableActions = _remediationStepsService != null
            ? await _remediationStepsService.GetAvailableActionsAsync(controlId)
            : Array.Empty<RemediationAction>();
        var resourceState = await GetResourceStateAsync(finding.ResourceId, cancellationToken);

        var remediationOptions = string.Join("\n", availableActions.Select(r =>
            $"- {r.Action}: {r.Description} (Risk: {r.Risk})"));

        var prompt = $@"You are an Azure security expert. Generate a remediation script for:

Control: {controlId} - {control?.Title ?? controlId}
Severity: {finding.Severity}
Resource: {finding.ResourceId}
Finding: {finding.Description}

**Current Resource State:**
{JsonSerializer.Serialize(resourceState, new JsonSerializerOptions { WriteIndented = true })}

**Available Remediation Actions:**
{remediationOptions}

Generate a {scriptType} script that:
1. Validates the current state
2. Implements ONE of the available remediation actions
3. Verifies the fix
4. Logs all actions
5. Handles errors gracefully

Include comments explaining each step. Make the script idempotent.";

        var chatHistory = new List<ChatMessage>
        {
            new(ChatRole.System, GetSystemPrompt(scriptType)),
            new(ChatRole.User, prompt)
        };

        var options = new ChatOptions
        {
            Temperature = 0.2f,
            MaxOutputTokens = 2000
        };

        var response = await _chatClient.GetResponseAsync(
            chatHistory,
            options,
            cancellationToken);

        var script = new RemediationScript
        {
            FindingId = finding.Id,
            ControlId = controlId,
            ScriptType = scriptType,
            Script = ExtractCodeFromResponse(response.Text ?? string.Empty),
            AvailableRemediations = availableActions.ToList(),
            RecommendedAction = availableActions.FirstOrDefault()?.Action,
            GeneratedAt = DateTimeOffset.UtcNow,
            GeneratedBy = "AI-GPT4",
            RequiresApproval = finding.Severity is AtoFindingSeverity.Critical or AtoFindingSeverity.High
        };

        return script;
    }

    public async Task<RemediationGuidance> GetRemediationGuidanceAsync(
        AtoFinding finding,
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            throw new InvalidOperationException("AI features not available - Azure OpenAI not configured");
        }

        _logger.LogInformation("Generating natural language guidance for finding {FindingId}", finding.Id);

        var controlId = finding.AffectedControls.FirstOrDefault() ?? "Unknown";
        var remediationSteps = _remediationStepsService != null
            ? await _remediationStepsService.GetRemediationStepsAsync(controlId)
            : null;

        var prompt = $@"Explain how to remediate this compliance finding for a cloud engineer:

Control: {controlId}
Finding: {finding.Description}
Severity: {finding.Severity}

**Technical Remediation Plan:**
{(remediationSteps?.Steps.Any() == true ? string.Join("\n", remediationSteps.Steps.Select(s => $"- {s.Description}")) : "No specific steps available")}
Estimated Effort: {remediationSteps?.EstimatedEffort.TotalMinutes ?? 30} minutes

Translate this into a friendly, step-by-step guide:

1. What's wrong (2-3 sentences)
2. Why it matters (security/compliance impact)
3. Step-by-step remediation (numbered list, using the commands above)
4. How to verify the fix
5. Estimated time to remediate

Keep explanations clear and actionable.";

        var chatHistory = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a patient cloud security mentor helping engineers fix compliance issues."),
            new(ChatRole.User, prompt)
        };

        var response = await _chatClient.GetResponseAsync(chatHistory, cancellationToken: cancellationToken);

        return new RemediationGuidance
        {
            FindingId = finding.Id,
            Explanation = response.Text ?? string.Empty,
            Confidence = 0.9,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<List<PrioritizedFinding>> PrioritizeFindingsWithAiAsync(
        List<AtoFinding> findings,
        string businessContext = "",
        CancellationToken cancellationToken = default)
    {
        if (_chatClient == null)
        {
            throw new InvalidOperationException("AI features not available - Azure OpenAI not configured");
        }

        _logger.LogInformation("AI-prioritizing {Count} findings with business context", findings.Count);

        var prompt = $@"Prioritize these {findings.Count} compliance findings based on:
- Security risk
- Business impact
- Ease of remediation
- Compliance deadlines

Business Context: {businessContext}

Findings:
{string.Join("\n", findings.Select((f, i) => $"{i + 1}. {f.AffectedControls.FirstOrDefault() ?? "Unknown"}: {f.Description} (Severity: {f.Severity})"))}

Return a JSON array with FindingId, ControlId, Priority (1-5), Reasoning.
Example: [{{""FindingId"": ""F-001"", ""ControlId"": ""AC-2"", ""Priority"": 1, ""Reasoning"": ""Critical security gap""}}]";

        var chatHistory = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a compliance risk analyst. Prioritize findings for maximum security impact with minimal disruption. Always return valid JSON."),
            new(ChatRole.User, prompt)
        };

        var response = await _chatClient.GetResponseAsync(chatHistory, cancellationToken: cancellationToken);
        var prioritized = JsonSerializer.Deserialize<List<PrioritizedFinding>>(
            ExtractJsonFromResponse(response.Text ?? "[]")) ?? new List<PrioritizedFinding>();

        return prioritized;
    }

    // Helper methods

    private async Task<object> GetResourceStateAsync(string resourceId, CancellationToken cancellationToken)
    {
        // Simplified resource state extraction
        var parts = resourceId.Split('/');
        if (parts.Length < 9)
        {
            return new { error = "Invalid resource ID format" };
        }

        return new
        {
            resourceId,
            subscriptionId = parts[2],
            resourceGroup = parts[4],
            resourceType = $"{parts[6]}/{parts[7]}",
            resourceName = parts[8],
            state = "retrieved"
        };
    }

    private string GetSystemPrompt(string scriptType)
    {
        return scriptType.ToUpperInvariant() switch
        {
            "AZURECLI" => "You are an Azure CLI expert. Generate secure, idempotent bash scripts using Azure CLI commands. Include error handling and validation.",
            "POWERSHELL" => "You are a PowerShell expert. Generate secure, idempotent PowerShell scripts for Azure. Use proper error handling with try-catch blocks.",
            "TERRAFORM" => "You are a Terraform expert. Generate idempotent Terraform HCL code for Azure resources. Include proper provider configuration and state management.",
            _ => "You are a cloud automation expert. Generate secure, idempotent scripts with proper error handling."
        };
    }

    private string ExtractCodeFromResponse(string response)
    {
        // Extract code from markdown code blocks
        var codeBlockPattern = @"```(?:bash|powershell|hcl|terraform)?\s*\n(.*?)\n```";
        var match = Regex.Match(response, codeBlockPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // If no code block found, return the whole response
        return response.Trim();
    }

    private string ExtractJsonFromResponse(string response)
    {
        // Extract JSON from markdown code blocks or raw response
        var jsonBlockPattern = @"```json\s*\n(.*?)\n```";
        var match = Regex.Match(response, jsonBlockPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Try to find JSON array or object
        var jsonPattern = @"(\[.*\]|\{.*\})";
        match = Regex.Match(response, jsonPattern, RegexOptions.Singleline);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return response.Trim();
    }
}
