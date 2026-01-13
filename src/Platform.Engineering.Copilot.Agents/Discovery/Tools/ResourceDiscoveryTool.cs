using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Discovery.State;

namespace Platform.Engineering.Copilot.Agents.Discovery.Tools;

/// <summary>
/// Tool for discovering Azure resources across subscriptions.
/// Provides filtering by resource group, type, location, and tags.
/// </summary>
public class ResourceDiscoveryTool : BaseTool
{
    private readonly DiscoveryStateAccessors _stateAccessors;

    public override string Name => "discover_azure_resources";

    public override string Description =>
        "Discover and list Azure resources with comprehensive filtering. " +
        "Search by subscription, resource group, type, location, or tags. " +
        "Use for resource inventory, discovery, and finding specific resources.";

    public ResourceDiscoveryTool(
        ILogger<ResourceDiscoveryTool> logger,
        DiscoveryStateAccessors stateAccessors) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));

        // Define parameters
        Parameters.Add(new ToolParameter(
            name: "subscriptionId",
            description: "Azure subscription ID. Required for resource discovery.",
            required: true));

        Parameters.Add(new ToolParameter(
            name: "resourceGroup",
            description: "Resource group name to filter by (optional)",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "resourceType",
            description: "Resource type to filter by (e.g., 'Microsoft.Storage/storageAccounts', optional)",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "location",
            description: "Location/region to filter by (e.g., 'eastus', 'usgovvirginia', optional)",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "tagFilter",
            description: "Tag filter in format 'key=value' (optional)",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var subscriptionId = GetOptionalString(arguments, "subscriptionId");
        var resourceGroup = GetOptionalString(arguments, "resourceGroup");
        var resourceType = GetOptionalString(arguments, "resourceType");
        var location = GetOptionalString(arguments, "location");
        var tagFilter = GetOptionalString(arguments, "tagFilter");

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return ToJson(new { success = false, error = "Subscription ID is required" });
        }

        Logger.LogInformation("Discovering Azure resources in subscription {SubscriptionId}", subscriptionId);

        try
        {
            // Check cache first
            var cached = _stateAccessors.GetCachedResources(subscriptionId);
            var resources = cached?.Resources ?? new List<DiscoveredResourceSummary>();
            var fromCache = cached != null;

            if (!fromCache)
            {
                // TODO: Integrate with actual Azure resource service
                // For now, return a placeholder indicating real service integration needed
                Logger.LogWarning("Resource discovery requires Azure service integration. Returning sample data.");
                
                resources = new List<DiscoveredResourceSummary>
                {
                    new() { ResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/rg-sample/providers/Microsoft.Compute/virtualMachines/vm-sample", Name = "vm-sample", Type = "Microsoft.Compute/virtualMachines", Location = "eastus", ResourceGroup = "rg-sample" }
                };
            }

            // Apply filters
            var filteredResources = resources.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(resourceGroup))
            {
                filteredResources = filteredResources.Where(r =>
                    r.ResourceGroup.Equals(resourceGroup, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(resourceType))
            {
                filteredResources = filteredResources.Where(r =>
                    r.Type.Equals(resourceType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                filteredResources = filteredResources.Where(r =>
                    r.Location.Equals(location, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tagFilter) && tagFilter.Contains('='))
            {
                var parts = tagFilter.Split('=', 2);
                var tagKey = parts[0];
                var tagValue = parts.Length > 1 ? parts[1] : "";

                filteredResources = filteredResources.Where(r =>
                    r.Tags != null &&
                    r.Tags.TryGetValue(tagKey, out var value) &&
                    value.Equals(tagValue, StringComparison.OrdinalIgnoreCase));
            }

            var resultList = filteredResources.ToList();

            // Group results
            var byType = resultList.GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            var byLocation = resultList.GroupBy(r => r.Location)
                .ToDictionary(g => g.Key, g => g.Count());
            var byResourceGroup = resultList.GroupBy(r => r.ResourceGroup)
                .ToDictionary(g => g.Key, g => g.Count());

            await Task.CompletedTask; // Satisfy async requirement

            return ToJson(new
            {
                success = true,
                totalCount = resultList.Count,
                fromCache,
                filters = new
                {
                    subscriptionId,
                    resourceGroup,
                    resourceType,
                    location,
                    tagFilter
                },
                summary = new
                {
                    byType,
                    byLocation,
                    byResourceGroup
                },
                resources = resultList.Take(100).Select(r => new
                {
                    r.ResourceId,
                    r.Name,
                    r.Type,
                    r.Location,
                    r.ResourceGroup
                })
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error discovering resources in subscription {SubscriptionId}", subscriptionId);
            return ToJson(new { success = false, error = ex.Message });
        }
    }
}
