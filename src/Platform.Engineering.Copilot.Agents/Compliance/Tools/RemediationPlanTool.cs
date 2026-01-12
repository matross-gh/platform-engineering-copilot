using System.Text;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for generating comprehensive, prioritized remediation plans to fix compliance violations.
/// </summary>
public class RemediationPlanTool : BaseTool
{
    private readonly IAtoComplianceEngine _complianceEngine;
    private readonly IRemediationEngine _remediationEngine;

    public override string Name => "generate_remediation_plan";

    public override string Description =>
        "Generate a comprehensive, prioritized remediation plan with actionable steps to fix compliance violations and security findings. " +
        "Creates a detailed action plan with effort estimates, priorities, and implementation guidance. " +
        "Use this when user requests: 'remediation plan', 'action plan', 'fix plan', 'create plan to fix findings', " +
        "'generate remediation steps', 'how to fix violations', 'prioritized remediation', 'remediation roadmap'. " +
        "Returns: Prioritized violations, remediation steps per finding, effort estimates, dependencies, implementation order. " +
        "Accepts either a subscription GUID or friendly name (e.g., 'production', 'dev', 'staging'). " +
        "If no subscription is specified, uses the most recent assessment from the last used subscription. " +
        "Can be scoped to a specific resource group. " +
        "Example user requests: 'generate a remediation plan for this assessment', 'create an action plan to fix these violations', " +
        "'I need detailed remediation steps', 'show me how to fix the compliance gaps', 'create a prioritized fix plan'.";

    public RemediationPlanTool(
        ILogger<RemediationPlanTool> logger,
        IAtoComplianceEngine complianceEngine,
        IRemediationEngine remediationEngine) : base(logger)
    {
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));
        _remediationEngine = remediationEngine ?? throw new ArgumentNullException(nameof(remediationEngine));

        Parameters.Add(new ToolParameter("subscription_id",
            "Azure subscription ID (GUID) or friendly name (e.g., 'production', 'dev', 'staging'). " +
            "Optional - if not provided, uses the last assessed subscription.", false));
        Parameters.Add(new ToolParameter("resource_group_name",
            "Optional resource group name to limit scope", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptionIdOrName = GetOptionalString(arguments, "subscription_id");
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name");

            // If no subscription provided, try to get the last used subscription
            if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
            {
                subscriptionIdOrName = GetLastUsedSubscription();
                if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
                {
                    Logger.LogWarning("No subscription specified and no previous subscription found in cache");
                    return ToJson(new
                    {
                        success = false,
                        error = "No subscription specified",
                        message = "Please specify a subscription ID or run a compliance assessment first to establish context.",
                        suggestedActions = new[]
                        {
                            "Run 'assess compliance for subscription <subscription-id>' first",
                            "Or specify the subscription: 'generate remediation plan for subscription <subscription-id>'"
                        }
                    });
                }

                Logger.LogInformation("Using last assessed subscription from cache: {SubscriptionId}", subscriptionIdOrName);
            }

            // Resolve subscription name to GUID
            var subscriptionId = await ResolveSubscriptionIdAsync(subscriptionIdOrName);

            var scope = string.IsNullOrWhiteSpace(resourceGroupName)
                ? $"subscription {subscriptionId}"
                : $"resource group '{resourceGroupName}' in subscription {subscriptionId}";

            Logger.LogInformation("Generating remediation plan for {Scope} (input: {Input})",
                scope, subscriptionIdOrName ?? "last used");

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Could not resolve subscription ID"
                });
            }

            // Get latest assessment from database (no time restriction - use most recent)
            var assessment = await _complianceEngine.GetLatestAssessmentAsync(
                subscriptionId, cancellationToken);

            if (assessment == null)
            {
                Logger.LogWarning("⚠️ No assessment found in database for subscription {SubscriptionId}. Please run an assessment first.", subscriptionId);
                return ToJson(new
                {
                    success = false,
                    error = $"No compliance assessment found for subscription {subscriptionId}",
                    message = "Please run a compliance assessment first using 'run compliance assessment' before generating a remediation plan.",
                    subscriptionId
                });
            }

            var assessmentAge = (DateTime.UtcNow - assessment.EndTime.UtcDateTime).TotalHours;
            Logger.LogInformation("✅ Using assessment from {Time} ({Age:F1} hours ago, {FindingCount} findings)",
                assessment.EndTime, assessmentAge,
                assessment.ControlFamilyResults.Sum(cf => cf.Value.Findings.Count));

            var findings = assessment.ControlFamilyResults
                .SelectMany(cf => cf.Value.Findings)
                .ToList();

            if (!findings.Any())
            {
                return ToJson(new
                {
                    success = true,
                    message = "No findings to remediate - subscription is compliant!",
                    subscriptionId
                });
            }

            var plan = await _remediationEngine.GenerateRemediationPlanAsync(
                subscriptionId,
                findings,
                cancellationToken);

            var autoRemediable = findings.Count(f => f.IsAutoRemediable);
            var manual = findings.Count - autoRemediable;

            // Generate pre-formatted display text for chat UI
            var displayText = GenerateRemediationPlanDisplayText(plan, autoRemediable, manual, subscriptionId);

            return ToJson(new
            {
                success = true,
                planId = plan.PlanId,
                subscriptionId = plan.SubscriptionId,
                createdAt = plan.CreatedAt,

                // Pre-formatted text ready for direct display
                displayText,

                summary = new
                {
                    totalFindings = plan.TotalFindings,
                    autoRemediable,
                    manualRequired = manual,
                    estimatedEffort = plan.EstimatedEffort,
                    priority = plan.Priority,
                    riskReduction = Math.Round(plan.ProjectedRiskReduction, 2)
                },
                remediationItems = plan.RemediationItems.Take(20).Select(item => new
                {
                    findingId = item.FindingId,
                    controlId = item.ControlId,
                    resourceId = item.ResourceId,
                    priority = item.Priority,
                    effort = item.EstimatedEffort,
                    automationAvailable = item.AutomationAvailable,

                    actionSummary = item.AutomationAvailable
                        ? $"✨ AUTO-REMEDIATION: Will automatically execute {item.Steps?.Count ?? 0} step(s) when you run remediation"
                        : $"🔧 MANUAL REMEDIATION: Requires {item.Steps?.Count ?? 0} manual step(s)",

                    automatedActions = item.AutomationAvailable && item.Steps != null && item.Steps.Any()
                        ? item.Steps.Select((step, idx) => new
                        {
                            step = idx + 1,
                            action = step.Description,
                            actionType = !string.IsNullOrEmpty(step.Command) ? "Configuration Change" : "System Update"
                        }).ToList()
                        : null,

                    manualSteps = !item.AutomationAvailable && item.Steps != null && item.Steps.Any()
                        ? item.Steps.Select((step, idx) => new
                        {
                            step = idx + 1,
                            description = step.Description,
                            command = step.Command,
                            script = step.AutomationScript
                        }).ToList()
                        : null,

                    steps = item.Steps?.Select(step => new
                    {
                        order = step.Order,
                        description = step.Description,
                        command = item.AutomationAvailable ? null : step.Command,
                        automationScript = item.AutomationAvailable ? null : step.AutomationScript,
                        isAutomated = item.AutomationAvailable
                    }).ToList(),

                    validationSteps = item.ValidationSteps,
                    dependencies = item.Dependencies,

                    summary = item.AutomationAvailable && item.Steps != null && item.Steps.Any()
                        ? $"✨ {string.Join(" → ", item.Steps.Select(s => s.Description))}"
                        : item.Steps != null && item.Steps.Any()
                        ? $"🔧 {string.Join(" → ", item.Steps.Select(s => s.Description))}"
                        : "Review resource and apply manual remediation"
                }),
                timeline = plan.Timeline != null ? new
                {
                    startDate = plan.Timeline.StartDate,
                    endDate = plan.Timeline.EndDate,
                    milestones = plan.Timeline.Milestones
                } : null,
                executiveSummary = plan.ExecutiveSummary,

                displayInstructions = new
                {
                    instruction = "IMPORTANT: Display the 'displayText' field directly to the user. Do NOT reformat or regenerate the output.",
                    format = "The displayText contains pre-formatted markdown with all remediation details including auto-remediable actions with specific steps.",
                    autoRemediableDisplay = "For auto-remediable findings, the displayText shows numbered automated actions that will execute.",
                    manualDisplay = "For manual findings, the displayText shows step-by-step instructions with commands."
                },

                nextSteps = new[]
                {
                    "📋 DISPLAY: Show the 'displayText' field to the user - it contains the complete formatted remediation plan",
                    autoRemediable > 0
                        ? $"⚡ EXECUTE: User can say 'execute the remediation plan' to automatically fix {autoRemediable} finding(s)"
                        : null,
                    "📊 TRACK: User can say 'show me the remediation progress' to monitor completion"
                }.Where(s => s != null)
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating remediation plan (input: {Input})", 
                GetOptionalString(arguments, "subscription_id"));
            return ToJson(new
            {
                success = false,
                error = $"Failed to generate remediation plan: {ex.Message}"
            });
        }
    }

    private string GenerateRemediationPlanDisplayText(RemediationPlan plan, int autoRemediable, int manual, string subscriptionId)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# 🔧 REMEDIATION PLAN");
        sb.AppendLine();
        sb.AppendLine($"**Subscription:** `{subscriptionId}`");
        sb.AppendLine($"**Generated:** {plan.CreatedAt:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"**Plan ID:** `{plan.PlanId}`");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 📊 SUMMARY");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| **Total Findings** | {plan.TotalFindings} |");
        sb.AppendLine($"| ✨ **Auto-Remediable** | {autoRemediable} |");
        sb.AppendLine($"| 🔧 **Manual Required** | {manual} |");
        sb.AppendLine($"| ⏱️ **Estimated Effort** | {plan.EstimatedEffort} |");
        sb.AppendLine($"| 📉 **Risk Reduction** | {Math.Round(plan.ProjectedRiskReduction, 1)}% |");
        sb.AppendLine();

        if (autoRemediable > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## ✨ AUTO-REMEDIABLE FINDINGS");
            sb.AppendLine();
            sb.AppendLine("These findings can be automatically fixed:");
            sb.AppendLine();

            var autoItems = plan.RemediationItems.Where(i => i.AutomationAvailable).Take(10).ToList();
            foreach (var item in autoItems)
            {
                sb.AppendLine($"### {item.ControlId} - {item.Priority} Priority");
                sb.AppendLine($"- **Resource:** `{item.ResourceId}`");
                sb.AppendLine($"- **Finding:** {item.FindingId}");
                if (item.Steps != null && item.Steps.Any())
                {
                    sb.AppendLine("- **Automated Actions:**");
                    foreach (var (step, idx) in item.Steps.Select((s, i) => (s, i)))
                    {
                        sb.AppendLine($"  {idx + 1}. {step.Description}");
                    }
                }
                sb.AppendLine();
            }

            if (plan.RemediationItems.Count(i => i.AutomationAvailable) > 10)
            {
                sb.AppendLine($"*...and {plan.RemediationItems.Count(i => i.AutomationAvailable) - 10} more auto-remediable findings*");
                sb.AppendLine();
            }
        }

        if (manual > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 🔧 MANUAL REMEDIATION REQUIRED");
            sb.AppendLine();
            sb.AppendLine("These findings require manual intervention:");
            sb.AppendLine();

            var manualItems = plan.RemediationItems.Where(i => !i.AutomationAvailable).Take(10).ToList();
            foreach (var item in manualItems)
            {
                sb.AppendLine($"### {item.ControlId} - {item.Priority} Priority");
                sb.AppendLine($"- **Resource:** `{item.ResourceId}`");
                sb.AppendLine($"- **Finding:** {item.FindingId}");
                if (item.Steps != null && item.Steps.Any())
                {
                    sb.AppendLine("- **Steps:**");
                    foreach (var (step, idx) in item.Steps.Select((s, i) => (s, i)))
                    {
                        sb.AppendLine($"  {idx + 1}. {step.Description}");
                        if (!string.IsNullOrEmpty(step.Command))
                        {
                            sb.AppendLine($"     ```");
                            sb.AppendLine($"     {step.Command}");
                            sb.AppendLine($"     ```");
                        }
                    }
                }
                sb.AppendLine();
            }

            if (plan.RemediationItems.Count(i => !i.AutomationAvailable) > 10)
            {
                sb.AppendLine($"*...and {plan.RemediationItems.Count(i => !i.AutomationAvailable) - 10} more manual findings*");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 💡 NEXT STEPS");
        sb.AppendLine();
        if (autoRemediable > 0)
        {
            sb.AppendLine($"1. Say **'execute remediation plan'** to automatically fix {autoRemediable} finding(s)");
        }
        if (manual > 0)
        {
            sb.AppendLine($"{(autoRemediable > 0 ? "2" : "1")}. Review and apply manual remediations above");
        }
        sb.AppendLine($"{(autoRemediable > 0 && manual > 0 ? "3" : autoRemediable > 0 || manual > 0 ? "2" : "1")}. Re-run compliance assessment to verify fixes");

        return sb.ToString();
    }

    private string? GetLastUsedSubscription()
    {
        try
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".platform-copilot", "last-subscription.txt");

            if (File.Exists(configPath))
            {
                return File.ReadAllText(configPath).Trim();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read last used subscription");
        }
        return null;
    }

    private async Task<string?> ResolveSubscriptionIdAsync(string? subscriptionIdOrName)
    {
        if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
        {
            // Try to get from persistent configuration
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".platform-copilot", "config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    var configJson = await File.ReadAllTextAsync(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(configJson);
                    if (config?.TryGetValue("subscription_id", out var savedId) == true)
                    {
                        Logger.LogInformation("Using subscription from persistent config: {SubscriptionId}", savedId);
                        return savedId;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to read config file");
                }
            }
            return null;
        }

        // Check if it's already a GUID
        if (Guid.TryParse(subscriptionIdOrName, out _))
        {
            return subscriptionIdOrName;
        }

        // Return as-is for friendly names
        return subscriptionIdOrName;
    }
}
