using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for getting real-time compliance status with continuous monitoring data.
/// </summary>
public class ComplianceStatusTool : BaseTool
{
    private readonly IAtoComplianceEngine _complianceEngine;

    public override string Name => "get_compliance_status";

    public override string Description =>
        "Get real-time compliance status with continuous monitoring data. " +
        "Shows current score, active alerts, and recent changes. " +
        "Use this for quick compliance health checks. Can scope to a specific resource group. " +
        "Accepts either a subscription GUID or friendly name (e.g., 'production', 'dev', 'staging'). " +
        "If no subscription is provided, uses the default subscription from persistent configuration (set via configure_subscription).";

    public ComplianceStatusTool(
        ILogger<ComplianceStatusTool> logger,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));

        Parameters.Add(new ToolParameter("subscription_id", 
            "Azure subscription ID (GUID) or friendly name (e.g., 'production', 'dev', 'staging'). " +
            "OPTIONAL - if not provided, uses the default subscription from configuration.", false));
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

            // Resolve subscription - use provided or get from config
            var subscriptionId = await ResolveSubscriptionIdAsync(subscriptionIdOrName);

            var scope = string.IsNullOrWhiteSpace(resourceGroupName)
                ? $"subscription {subscriptionId}"
                : $"resource group '{resourceGroupName}' in subscription {subscriptionId}";

            Logger.LogInformation("Getting compliance status for {Scope} (input: {Input})",
                scope, subscriptionIdOrName);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Subscription ID is required. Either provide subscription_id parameter or set default using configure_subscription tool."
                });
            }

            var status = await _complianceEngine.GetContinuousComplianceStatusAsync(subscriptionId, cancellationToken);

            return ToJson(new
            {
                success = true,
                subscriptionId = status.SubscriptionId,
                timestamp = status.Timestamp,
                currentStatus = new
                {
                    score = Math.Round(status.ComplianceScore, 2),
                    grade = GetComplianceGrade(status.ComplianceScore),
                    monitoringEnabled = status.MonitoringEnabled,
                    lastCheck = status.LastCheckTime,
                    nextCheck = status.NextCheckTime
                },
                trend = new
                {
                    direction = status.TrendDirection,
                    driftPercentage = Math.Round(status.ComplianceDriftPercentage, 2)
                },
                alerts = new
                {
                    active = status.ActiveAlerts,
                    resolvedToday = status.ResolvedToday,
                    autoRemediations = status.AutoRemediationCount
                },
                monitoring = new
                {
                    enabled = status.MonitoringEnabled,
                    lastScan = status.LastCheckTime,
                    nextScan = status.NextCheckTime,
                    activeControls = status.ControlStatuses.Count
                },
                quickActions = GetQuickActions(status)
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting compliance status");
            return ToJson(new { success = false, error = ex.Message });
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

        // Try to resolve friendly name to subscription ID
        var friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // These would be loaded from configuration in production
            { "production", "" },
            { "prod", "" },
            { "development", "" },
            { "dev", "" },
            { "staging", "" },
            { "stage", "" },
            { "test", "" }
        };

        if (friendlyNames.TryGetValue(subscriptionIdOrName, out var resolvedId) && !string.IsNullOrEmpty(resolvedId))
        {
            Logger.LogInformation("Resolved friendly name '{Name}' to subscription {Id}", subscriptionIdOrName, resolvedId);
            return resolvedId;
        }

        // If not found in mappings, return as-is (might be a partial GUID or unknown name)
        Logger.LogWarning("Could not resolve subscription name '{Name}', using as-is", subscriptionIdOrName);
        return subscriptionIdOrName;
    }

    private static string GetComplianceGrade(double score)
    {
        return score switch
        {
            >= 95 => "A+",
            >= 90 => "A",
            >= 85 => "B+",
            >= 80 => "B",
            >= 75 => "C+",
            >= 70 => "C",
            >= 65 => "D+",
            >= 60 => "D",
            _ => "F"
        };
    }

    private static List<string> GetQuickActions(Core.Models.Compliance.ContinuousComplianceStatus status)
    {
        var actions = new List<string>();

        if (status.ActiveAlerts > 0)
        {
            actions.Add($"Review {status.ActiveAlerts} active alerts");
        }

        if (status.TrendDirection == "Declining")
        {
            actions.Add("Investigate compliance drift - trend is declining");
        }

        if (status.ComplianceScore < 70)
        {
            actions.Add("Critical: Compliance score below 70% - immediate remediation needed");
        }
        else if (status.ComplianceScore < 80)
        {
            actions.Add("Warning: Compliance score below 80% - review high-severity findings");
        }

        actions.Add("Run 'run_compliance_assessment' for detailed analysis");
        actions.Add("Use 'get_control_family_details' to drill into specific control families");

        return actions;
    }
}
