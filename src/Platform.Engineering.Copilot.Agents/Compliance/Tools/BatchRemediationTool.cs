using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for executing batch remediation for multiple compliance findings at once.
/// Supports severity filtering (e.g., "remediate all high-priority issues").
/// Uses cached assessment from previous turn - does NOT re-run assessment.
/// </summary>
public class BatchRemediationTool : BaseTool
{
    private readonly ComplianceAgentOptions _options;
    private readonly IRemediationEngine _remediationEngine;
    private readonly IAtoComplianceEngine _complianceEngine;

    public override string Name => "batch_remediation";

    public override string Description =>
        "Execute remediation for multiple compliance findings at once based on severity or control family. " +
        "IMPORTANT: Uses the existing assessment from conversation context - does NOT run a new assessment. " +
        "Use this when user says: 'start remediation', 'fix high-priority issues', 'remediate critical findings', " +
        "'execute automated remediation', 'fix all violations'. " +
        "Accepts severity filter (critical, high, medium, low, all) and optional control family filter. " +
        "Supports dry-run mode (default) to preview changes before applying them. " +
        "Accepts either a subscription GUID or friendly name (e.g., 'production', 'dev', 'staging'). " +
        "If no subscription specified, uses the last assessed subscription from conversation context.";

    public BatchRemediationTool(
        ILogger<BatchRemediationTool> logger,
        IOptions<ComplianceAgentOptions> options,
        IRemediationEngine remediationEngine,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _options = options?.Value ?? new ComplianceAgentOptions();
        _remediationEngine = remediationEngine ?? throw new ArgumentNullException(nameof(remediationEngine));
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));

        Parameters.Add(new ToolParameter("severity_filter",
            "Severity level to remediate: 'critical', 'high', 'medium', 'low', or 'all'. Default: 'high' to target high-priority issues.", false));
        Parameters.Add(new ToolParameter("subscription_id",
            "Azure subscription ID (GUID) or friendly name. Optional - uses last assessed subscription if not provided.", false));
        Parameters.Add(new ToolParameter("resource_group_name",
            "Optional resource group name to limit scope", false));
        Parameters.Add(new ToolParameter("control_family",
            "Optional control family to filter (e.g., 'AC', 'AU', 'SC')", false));
        Parameters.Add(new ToolParameter("dry_run",
            "Dry run mode - preview changes without applying (true/false, default: true)", false));
        Parameters.Add(new ToolParameter("max_findings",
            "Maximum number of findings to remediate in this batch (default: 10)", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var severityFilter = GetOptionalString(arguments, "severity_filter") ?? "high";
            var subscriptionIdOrName = GetOptionalString(arguments, "subscription_id");
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name");
            var controlFamily = GetOptionalString(arguments, "control_family");
            var dryRun = GetOptionalBool(arguments, "dry_run", true);
            var maxFindings = GetOptionalInt(arguments, "max_findings", 10);

            // If no subscription provided, try to get the last used subscription
            if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
            {
                subscriptionIdOrName = GetLastUsedSubscription();
                if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
                {
                    Logger.LogWarning("No subscription specified and no previous subscription found");
                    return ToJson(new
                    {
                        success = false,
                        error = "No subscription specified",
                        message = "Please run a compliance assessment first or specify a subscription ID.",
                        suggestedActions = new[]
                        {
                            "Run 'check compliance for subscription <subscription-id>' first",
                            "Or specify the subscription: 'start remediation for subscription <subscription-id>'"
                        }
                    });
                }

                Logger.LogInformation("Using last assessed subscription from cache: {SubscriptionId}", subscriptionIdOrName);
            }

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

            Logger.LogInformation("Starting batch remediation for {Subscription}, severity: {Severity}, dry-run: {DryRun}",
                subscriptionId, severityFilter, dryRun);

            // Get latest assessment from database - DO NOT run a new one
            var assessment = await _complianceEngine.GetLatestAssessmentAsync(subscriptionId, cancellationToken);

            if (assessment == null)
            {
                Logger.LogWarning("⚠️ No assessment found in database for subscription {SubscriptionId}", subscriptionId);
                return ToJson(new
                {
                    success = false,
                    error = $"No compliance assessment found for subscription {subscriptionId}",
                    message = "Please run a compliance assessment first. Use 'check compliance for subscription <id>' before starting remediation.",
                    subscriptionId
                });
            }

            var assessmentAge = (DateTime.UtcNow - assessment.EndTime.UtcDateTime).TotalHours;
            Logger.LogInformation("✅ Using existing assessment from {Time} ({Age:F1} hours ago)",
                assessment.EndTime, assessmentAge);

            // Get all findings from assessment
            var allFindings = assessment.ControlFamilyResults
                .SelectMany(cf => cf.Value.Findings)
                .ToList();

            if (!allFindings.Any())
            {
                return ToJson(new
                {
                    success = true,
                    message = "No findings to remediate - subscription is compliant!",
                    subscriptionId,
                    complianceScore = assessment.OverallComplianceScore
                });
            }

            // Filter findings by severity
            var targetSeverity = ParseSeverityFilter(severityFilter);
            var filteredFindings = FilterFindingsBySeverity(allFindings, targetSeverity);

            // Filter by control family if specified
            if (!string.IsNullOrWhiteSpace(controlFamily))
            {
                filteredFindings = filteredFindings
                    .Where(f => f.AffectedNistControls.Any(c => c.StartsWith(controlFamily.ToUpperInvariant())))
                    .ToList();
            }

            // Filter by resource group if specified
            if (!string.IsNullOrWhiteSpace(resourceGroupName))
            {
                filteredFindings = filteredFindings
                    .Where(f => f.ResourceId?.Contains($"resourceGroups/{resourceGroupName}", StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            // Filter to only auto-remediable findings
            var remediableFindings = filteredFindings
                .Where(f => f.IsAutoRemediable)
                .Take(maxFindings)
                .ToList();

            if (!remediableFindings.Any())
            {
                var nonRemediableCount = filteredFindings.Count(f => !f.IsAutoRemediable);
                return ToJson(new
                {
                    success = true,
                    message = $"No auto-remediable findings found for severity '{severityFilter}'",
                    totalFindings = filteredFindings.Count,
                    manualRemediationRequired = nonRemediableCount,
                    suggestion = nonRemediableCount > 0
                        ? $"Found {nonRemediableCount} findings that require manual remediation. Use 'generate_remediation_plan' to get step-by-step guidance."
                        : "No findings match the specified criteria."
                });
            }

            // Check if automated remediation is enabled
            if (!_options.EnableAutomatedRemediation)
            {
                Logger.LogWarning("⚠️ Automated remediation is disabled in configuration");
                return ToJson(new
                {
                    success = false,
                    error = "Automated remediation is disabled",
                    findingsReady = remediableFindings.Count,
                    configurationSetting = "ComplianceAgent.EnableAutomatedRemediation",
                    currentValue = false,
                    recommendation = "Enable automated remediation in configuration, or use 'generate_remediation_plan' for manual steps",
                    previewFindings = remediableFindings.Take(5).Select(f => new
                    {
                        id = f.Id,
                        title = f.Title,
                        severity = f.Severity.ToString(),
                        controls = f.AffectedNistControls
                    })
                });
            }

            // Execute batch remediation
            var options = new BatchRemediationOptions
            {
                FailFast = false,
                MaxConcurrentRemediations = 3,
                ContinueOnError = true,
                ExecutionOptions = new RemediationExecutionOptions
                {
                    DryRun = dryRun,
                    RequireApproval = false,
                    AutoRollbackOnFailure = true
                }
            };

            var result = await _remediationEngine.ExecuteBatchRemediationAsync(
                subscriptionId,
                remediableFindings,
                options,
                cancellationToken);

            return ToJson(new
            {
                success = true,
                batchId = result.BatchId,
                mode = dryRun ? "DRY RUN (no changes applied)" : "LIVE EXECUTION",
                subscriptionId = result.SubscriptionId,
                
                assessmentUsed = new
                {
                    assessmentId = assessment.AssessmentId,
                    assessedAt = assessment.EndTime,
                    ageHours = Math.Round(assessmentAge, 1)
                },

                filters = new
                {
                    severity = severityFilter,
                    controlFamily,
                    resourceGroup = resourceGroupName,
                    maxFindings
                },

                summary = new
                {
                    totalFindings = allFindings.Count,
                    matchingFindings = filteredFindings.Count,
                    autoRemediable = remediableFindings.Count,
                    attempted = result.TotalRemediations,
                    successful = result.SuccessfulRemediations,
                    failed = result.FailedRemediations,
                    skipped = result.SkippedRemediations,
                    duration = result.Duration.ToString(@"mm\:ss")
                },

                details = new
                {
                    successRate = result.Summary.SuccessRate,
                    criticalRemediated = result.Summary.CriticalFindingsRemediated,
                    highRemediated = result.Summary.HighFindingsRemediated,
                    estimatedRiskReduction = Math.Round(result.Summary.EstimatedRiskReduction, 2),
                    controlFamiliesAffected = result.Summary.ControlFamiliesAffected
                },

                executions = result.Executions.Select(e => new
                {
                    findingId = e.FindingId,
                    status = e.Status.ToString(),
                    success = e.Success,
                    message = e.Message ?? e.ErrorMessage,
                    changesApplied = e.ChangesApplied
                }),

                nextSteps = dryRun ? new[]
                {
                    $"Review the {result.SuccessfulRemediations} changes that would be applied",
                    "If satisfied, re-run with dry_run=false to apply changes: 'execute batch remediation with dry_run=false'",
                    "Changes can be rolled back if needed using the backup"
                } : new[]
                {
                    result.SuccessfulRemediations > 0 
                        ? $"Successfully remediated {result.SuccessfulRemediations} findings" 
                        : "No remediations completed successfully",
                    "Use 'validate_remediation' to verify the fixes",
                    "Use 'check compliance status' to see the updated score"
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing batch remediation");
            return ToJson(new
            {
                success = false,
                error = $"Failed to execute batch remediation: {ex.Message}"
            });
        }
    }

    private List<AtoFindingSeverity> ParseSeverityFilter(string filter)
    {
        return filter.ToLowerInvariant() switch
        {
            "critical" => new List<AtoFindingSeverity> { AtoFindingSeverity.Critical },
            "high" => new List<AtoFindingSeverity> { AtoFindingSeverity.Critical, AtoFindingSeverity.High },
            "medium" => new List<AtoFindingSeverity> { AtoFindingSeverity.Critical, AtoFindingSeverity.High, AtoFindingSeverity.Medium },
            "low" => new List<AtoFindingSeverity> { AtoFindingSeverity.Critical, AtoFindingSeverity.High, AtoFindingSeverity.Medium, AtoFindingSeverity.Low },
            "all" => Enum.GetValues<AtoFindingSeverity>().ToList(),
            _ => new List<AtoFindingSeverity> { AtoFindingSeverity.Critical, AtoFindingSeverity.High } // Default to high-priority
        };
    }

    private List<AtoFinding> FilterFindingsBySeverity(List<AtoFinding> findings, List<AtoFindingSeverity> severities)
    {
        return findings
            .Where(f => severities.Contains(f.Severity))
            .OrderByDescending(f => f.Severity) // Critical first
            .ThenBy(f => f.AffectedNistControls.FirstOrDefault() ?? f.Title)
            .ToList();
    }

    private string? GetLastUsedSubscription()
    {
        // Try to get from persistent configuration
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".platform-copilot", "config.json");

        if (File.Exists(configPath))
        {
            try
            {
                var configJson = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(configJson);
                if (config?.TryGetValue("subscription_id", out var savedId) == true)
                {
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

    private async Task<string?> ResolveSubscriptionIdAsync(string? subscriptionIdOrName)
    {
        if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
        {
            return GetLastUsedSubscription();
        }

        // Check if it's already a GUID
        if (Guid.TryParse(subscriptionIdOrName, out _))
        {
            return subscriptionIdOrName;
        }

        // Return as-is for friendly names (will be resolved by engine)
        return subscriptionIdOrName;
    }

    private int GetOptionalInt(IDictionary<string, object?> arguments, string key, int defaultValue)
    {
        if (arguments.TryGetValue(key, out var value) && value != null)
        {
            if (value is int intVal) return intVal;
            if (value is long longVal) return (int)longVal;
            if (value is string strVal && int.TryParse(strVal, out var parsed)) return parsed;
        }
        return defaultValue;
    }
}
