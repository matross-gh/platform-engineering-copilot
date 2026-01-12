using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for executing automated remediation for compliance findings.
/// Supports dry-run mode, rollback on failure, and resource group scoping.
/// </summary>
public class RemediationExecuteTool : BaseTool
{
    private readonly ComplianceAgentOptions _options;
    private readonly IRemediationEngine _remediationEngine;
    private readonly IAtoComplianceEngine _complianceEngine;

    public override string Name => "execute_remediation";

    public override string Description =>
        "Execute automated remediation for a specific compliance finding. " +
        "Use dry-run mode first to preview changes. Supports rollback on failure. Can scope to a specific resource group. " +
        "Accepts either a subscription GUID or friendly name (e.g., 'production', 'dev', 'staging'). " +
        "RBAC: Requires Compliance.Administrator or Compliance.Analyst role.";

    public RemediationExecuteTool(
        ILogger<RemediationExecuteTool> logger,
        IOptions<ComplianceAgentOptions> options,
        IRemediationEngine remediationEngine,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _options = options?.Value ?? new ComplianceAgentOptions();
        _remediationEngine = remediationEngine ?? throw new ArgumentNullException(nameof(remediationEngine));
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));

        Parameters.Add(new ToolParameter("subscription_id",
            "Azure subscription ID (GUID) or friendly name (e.g., 'production', 'dev', 'staging')", true));
        Parameters.Add(new ToolParameter("finding_id",
            "Finding ID to remediate", true));
        Parameters.Add(new ToolParameter("resource_group_name",
            "Optional resource group name to limit scope", false));
        Parameters.Add(new ToolParameter("dry_run",
            "Dry run mode - preview changes without applying (true/false, default: true)", false));
        Parameters.Add(new ToolParameter("require_approval",
            "Require approval before executing (true/false, default: false)", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptionIdOrName = GetRequiredString(arguments, "subscription_id");
            var findingId = GetRequiredString(arguments, "finding_id");
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name");
            var dryRun = GetOptionalBool(arguments, "dry_run", true);
            var requireApproval = GetOptionalBool(arguments, "require_approval", false);

            // Resolve subscription name to GUID
            var subscriptionId = await ResolveSubscriptionIdAsync(subscriptionIdOrName);
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Could not resolve subscription ID"
                });
            }

            var scope = string.IsNullOrWhiteSpace(resourceGroupName)
                ? $"subscription {subscriptionId}"
                : $"resource group '{resourceGroupName}' in subscription {subscriptionId}";

            Logger.LogInformation("Executing remediation for {Scope} (input: {Input}), finding {FindingId}, dry-run: {DryRun}",
                scope, subscriptionIdOrName, findingId, dryRun);

            // Get the finding from latest assessment
            var assessment = await _complianceEngine.GetLatestAssessmentAsync(
                subscriptionId, cancellationToken);

            if (assessment == null)
            {
                return ToJson(new
                {
                    success = false,
                    error = $"No compliance assessment found for subscription {subscriptionId}",
                    suggestion = "Run 'run_compliance_assessment' first to identify findings"
                });
            }

            var finding = assessment.ControlFamilyResults
                .SelectMany(cf => cf.Value.Findings)
                .FirstOrDefault(f => f.Id == findingId);

            if (finding == null)
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Finding {findingId} not found",
                    suggestion = "Use 'run_compliance_assessment' to get valid finding IDs"
                });
            }

            if (!finding.IsAutoRemediable)
            {
                return ToJson(new
                {
                    success = false,
                    error = "This finding cannot be automatically remediated",
                    findingId,
                    recommendation = finding.Recommendation,
                    manualGuidance = finding.RemediationGuidance
                });
            }

            // Check if automated remediation is enabled in configuration
            if (!_options.EnableAutomatedRemediation)
            {
                Logger.LogWarning("⚠️ Automated remediation is disabled in configuration (EnableAutomatedRemediation=false)");
                return ToJson(new
                {
                    success = false,
                    error = "Automated remediation is disabled",
                    findingId,
                    configurationSetting = "ComplianceAgent.EnableAutomatedRemediation",
                    currentValue = false,
                    recommendation = "Set EnableAutomatedRemediation to true in ComplianceAgent configuration to enable automated remediation",
                    manualGuidance = finding.RemediationGuidance
                });
            }

            var options = new RemediationExecutionOptions
            {
                DryRun = dryRun,
                RequireApproval = requireApproval
            };

            var execution = await _remediationEngine.ExecuteRemediationAsync(
                subscriptionId,
                finding,
                options,
                cancellationToken);

            return ToJson(new
            {
                success = execution.Success,
                executionId = execution.ExecutionId,
                mode = dryRun ? "DRY RUN (no changes applied)" : "LIVE EXECUTION",
                finding = new
                {
                    id = finding.Id,
                    title = finding.Title,
                    severity = finding.Severity.ToString()
                },
                result = new
                {
                    status = execution.Status.ToString(),
                    message = execution.Message,
                    duration = execution.Duration,
                    changesApplied = execution.ChangesApplied
                },
                backupCreated = !string.IsNullOrEmpty(execution.BackupId),
                backupId = execution.BackupId,
                error = execution.Error,
                nextSteps = dryRun ? new[]
                {
                    "Review the changes that would be applied",
                    "If satisfied, re-run with dry_run=false to apply changes",
                    "Changes can be rolled back if needed"
                } : new[]
                {
                    execution.Success ? "Remediation completed successfully" : "Remediation failed - review error",
                    "Use 'validate_remediation' to verify the fix",
                    "Use 'get_compliance_status' to see updated score"
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing remediation for finding {FindingId}",
                GetOptionalString(arguments, "finding_id"));
            return ToJson(new
            {
                success = false,
                error = $"Failed to execute remediation: {ex.Message}"
            });
        }
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
