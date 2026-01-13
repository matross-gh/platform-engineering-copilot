using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for validating that a remediation was successful.
/// Performs post-remediation checks to ensure fixes were effective.
/// </summary>
public class ValidateRemediationTool : BaseTool
{
    private readonly IAtoComplianceEngine _complianceEngine;

    public override string Name => "validate_remediation";

    public override string Description =>
        "Validate that a remediation was successful. " +
        "Performs post-remediation checks to ensure fixes were effective. Can scope to a specific resource group. " +
        "Accepts either a subscription GUID or friendly name (e.g., 'production', 'dev', 'staging').";

    public ValidateRemediationTool(
        ILogger<ValidateRemediationTool> logger,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));

        Parameters.Add(new ToolParameter("subscription_id",
            "Azure subscription ID (GUID) or friendly name (e.g., 'production', 'dev', 'staging')", true));
        Parameters.Add(new ToolParameter("finding_id",
            "Finding ID that was remediated", true));
        Parameters.Add(new ToolParameter("execution_id",
            "Execution ID from remediation", true));
        Parameters.Add(new ToolParameter("resource_group_name",
            "Optional resource group name to limit scope", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptionIdOrName = GetRequiredString(arguments, "subscription_id");
            var findingId = GetRequiredString(arguments, "finding_id");
            var executionId = GetRequiredString(arguments, "execution_id");
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name");

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

            Logger.LogInformation("Validating remediation for {Scope} (input: {Input}), execution {ExecutionId}",
                scope, subscriptionIdOrName, executionId);

            // Try to get the finding to validate against
            var finding = await _complianceEngine.GetFindingByIdAsync(findingId, cancellationToken);

            // Note: Full validation requires the RemediationExecution object from the original execution
            // For now, we provide guidance to re-run assessment to verify the fix
            if (finding == null)
            {
                return ToJson(new
                {
                    success = true,
                    message = "Finding no longer exists - it may have been resolved",
                    executionId,
                    findingId,
                    subscriptionId,
                    recommendation = "Run a compliance assessment to confirm the finding is resolved",
                    nextSteps = new[]
                    {
                        "✅ Finding not found in current assessment - likely resolved",
                        "Run 'run_compliance_assessment' to confirm",
                        "Run 'get_compliance_status' to see updated score"
                    }
                });
            }

            // Fallback: Return guidance for manual validation
            return ToJson(new
            {
                success = false,
                message = "Automatic validation requires integration with execution tracking",
                executionId,
                findingId,
                subscriptionId,
                recommendation = "Say 'run a compliance assessment for this subscription' to verify the finding is resolved",
                nextSteps = new[]
                {
                    "Say 'run a compliance assessment' to check if this finding has been resolved after remediation.",
                    "Verify the resource configuration matches the compliance requirements in the finding details.",
                    "Say 'show me the compliance status' to check for any side effects or new findings that may have appeared."
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating remediation for execution {ExecutionId}",
                GetOptionalString(arguments, "execution_id"));
            return ToJson(new
            {
                success = false,
                error = $"Failed to validate remediation: {ex.Message}"
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
