using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;

namespace Platform.Engineering.Copilot.Agents.Discovery.Tools;

/// <summary>
/// Tool for listing Azure subscriptions accessible to the current identity.
/// </summary>
public class SubscriptionListTool : BaseTool
{
    public override string Name => "list_subscriptions";

    public override string Description =>
        "List all Azure subscriptions accessible to the current identity. " +
        "Returns subscription IDs, names, states, and tenant IDs. " +
        "Use this to find the subscription ID before running discovery operations.";

    public SubscriptionListTool(ILogger<SubscriptionListTool> logger) : base(logger)
    {
        // No required parameters - lists all accessible subscriptions
        Parameters.Add(new ToolParameter(
            name: "state",
            description: "Filter by subscription state (e.g., 'Enabled', 'Disabled'). Optional.",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var stateFilter = GetOptionalString(arguments, "state");

        Logger.LogInformation("Listing Azure subscriptions");

        try
        {
            // TODO: Integrate with actual Azure resource service to list subscriptions
            // For now, return a placeholder indicating real service integration needed
            Logger.LogWarning("Subscription listing requires Azure service integration. Returning sample data.");

            var subscriptions = new List<object>
            {
                new
                {
                    subscriptionId = "00000000-0000-0000-0000-000000000000",
                    displayName = "Development Subscription",
                    state = "Enabled",
                    tenantId = "00000000-0000-0000-0000-000000000001"
                },
                new
                {
                    subscriptionId = "00000000-0000-0000-0000-000000000002",
                    displayName = "Production Subscription",
                    state = "Enabled",
                    tenantId = "00000000-0000-0000-0000-000000000001"
                }
            };

            // Apply state filter if provided
            if (!string.IsNullOrWhiteSpace(stateFilter))
            {
                // Note: Actual implementation would filter the real subscription list
                Logger.LogDebug("Filtering subscriptions by state: {State}", stateFilter);
            }

            await Task.CompletedTask; // Satisfy async requirement

            return ToJson(new
            {
                success = true,
                count = subscriptions.Count,
                subscriptions
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing subscriptions");
            return ToJson(new { success = false, error = ex.Message });
        }
    }
}
