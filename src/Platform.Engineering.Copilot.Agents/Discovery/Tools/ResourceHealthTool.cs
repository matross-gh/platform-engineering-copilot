using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;

namespace Platform.Engineering.Copilot.Agents.Discovery.Tools;

/// <summary>
/// Tool for getting resource health status and alerts.
/// </summary>
public class ResourceHealthTool : BaseTool
{
    public override string Name => "get_resource_health";

    public override string Description =>
        "Get health status and alerts for Azure resources. " +
        "Returns availability state, recent health events, and recommendations. " +
        "Use for monitoring and troubleshooting resource issues.";

    public ResourceHealthTool(ILogger<ResourceHealthTool> logger) : base(logger)
    {
        Parameters.Add(new ToolParameter(
            name: "resourceId",
            description: "Full Azure resource ID to check health for",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "subscriptionId",
            description: "Subscription ID to get health summary for all resources",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "resourceType",
            description: "Filter health by resource type (e.g., 'Microsoft.Compute/virtualMachines')",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var resourceId = GetOptionalString(arguments, "resourceId");
        var subscriptionId = GetOptionalString(arguments, "subscriptionId");
        var resourceType = GetOptionalString(arguments, "resourceType");

        if (string.IsNullOrWhiteSpace(resourceId) && string.IsNullOrWhiteSpace(subscriptionId))
        {
            return ToJson(new { success = false, error = "Either resourceId or subscriptionId is required" });
        }

        Logger.LogInformation("Getting resource health for {ResourceId} / subscription {SubscriptionId}",
            resourceId, subscriptionId);

        try
        {
            // TODO: Integrate with Azure Resource Health API
            Logger.LogWarning("Resource health requires Azure service integration. Returning sample data.");

            await Task.CompletedTask; // Satisfy async requirement

            if (!string.IsNullOrWhiteSpace(resourceId))
            {
                // Single resource health
                return ToJson(new
                {
                    success = true,
                    resource = resourceId,
                    health = new
                    {
                        availabilityState = "Available",
                        summary = "The resource is healthy and responding normally.",
                        reasonType = (string?)null,
                        occurredTime = DateTime.UtcNow.AddHours(-1),
                        reportedTime = DateTime.UtcNow
                    },
                    recentEvents = Array.Empty<object>(),
                    recommendations = new[]
                    {
                        new
                        {
                            category = "HighAvailability",
                            impact = "Low",
                            recommendation = "Consider enabling availability zones for improved resilience."
                        }
                    }
                });
            }
            else
            {
                // Subscription-level health summary
                return ToJson(new
                {
                    success = true,
                    subscriptionId,
                    resourceTypeFilter = resourceType,
                    summary = new
                    {
                        totalResources = 45,
                        available = 42,
                        degraded = 2,
                        unavailable = 1
                    },
                    issues = new[]
                    {
                        new
                        {
                            resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/rg-prod/providers/Microsoft.Compute/virtualMachines/vm-web-01",
                            availabilityState = "Degraded",
                            summary = "High CPU utilization detected"
                        },
                        new
                        {
                            resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/rg-prod/providers/Microsoft.Sql/servers/sql-prod/databases/db-main",
                            availabilityState = "Unavailable",
                            summary = "Database is undergoing maintenance"
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting resource health");
            return ToJson(new { success = false, error = ex.Message });
        }
    }
}
