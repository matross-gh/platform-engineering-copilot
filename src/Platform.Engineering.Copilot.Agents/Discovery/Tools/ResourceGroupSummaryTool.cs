using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Discovery.Configuration;
using Platform.Engineering.Copilot.Agents.Discovery.State;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;

namespace Platform.Engineering.Copilot.Agents.Discovery.Tools;

/// <summary>
/// Tool to get comprehensive summary and analysis of a specific resource group.
/// Shows resource breakdown by type, location distribution, tag analysis, and health status.
/// </summary>
public class ResourceGroupSummaryTool : BaseTool
{
    private readonly IAzureResourceService _azureResourceService;
    private readonly DiscoveryStateAccessors _stateAccessors;
    private readonly DiscoveryAgentOptions _options;

    public override string Name => "get_resource_group_summary";

    public override string Description =>
        "Get comprehensive summary and analysis of a specific resource group. " +
        "Shows resource breakdown by type, location distribution, tag analysis, and health status. " +
        "Use for resource group inventory, compliance, and optimization analysis.";

    public ResourceGroupSummaryTool(
        ILogger<ResourceGroupSummaryTool> logger,
        IAzureResourceService azureResourceService,
        DiscoveryStateAccessors stateAccessors,
        IOptions<DiscoveryAgentOptions> options) : base(logger)
    {
        _azureResourceService = azureResourceService;
        _stateAccessors = stateAccessors;
        _options = options.Value;

        Parameters.Add(new ToolParameter(
            "resource_group_name",
            "Resource group name to get summary for",
            required: true));

        Parameters.Add(new ToolParameter(
            "subscription_id",
            "Azure subscription ID (optional - uses default if not specified)",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var resourceGroupName = GetRequiredString(arguments, "resource_group_name");
        var subscriptionId = GetOptionalString(arguments, "subscription_id");

        Logger.LogInformation("Getting summary for resource group {ResourceGroup}", resourceGroupName);

        try
        {
            // Use configured subscription if not provided
            if (string.IsNullOrEmpty(subscriptionId))
            {
                subscriptionId = _options.DefaultSubscriptionId;
            }

            // Get resource group details
            var resourceGroup = await _azureResourceService.GetResourceGroupAsync(
                resourceGroupName, subscriptionId, cancellationToken);

            if (resourceGroup == null)
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Resource group not found: {resourceGroupName}",
                    subscriptionId = subscriptionId
                });
            }

            // Get all resources in the resource group
            var resources = await _azureResourceService.ListAllResourcesInResourceGroupAsync(
                subscriptionId, resourceGroupName, cancellationToken);
            var resourceList = resources.ToList();

            // Analyze resources by type
            var byType = resourceList
                .GroupBy(r => GetDynamicProperty(r, "type") ?? "Unknown")
                .Select(g => new { type = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            // Analyze resources by location
            var byLocation = resourceList
                .GroupBy(r => GetDynamicProperty(r, "location") ?? "Unknown")
                .Select(g => new { location = g.Key, count = g.Count() })
                .ToList();

            // Tag analysis
            var taggedCount = resourceList.Count(r => HasTags(r));
            var untaggedCount = resourceList.Count - taggedCount;
            var tagCoverage = resourceList.Count > 0
                ? Math.Round((double)taggedCount / resourceList.Count * 100, 2)
                : 0;

            // Extract resource group properties
            var rgLocation = GetDynamicProperty(resourceGroup, "location");
            var rgTags = GetDynamicTags(resourceGroup);
            var rgProvisioningState = GetNestedProperty(resourceGroup, "properties", "provisioningState");

            // Build next steps suggestions
            var nextSteps = new List<string>();
            
            if (resourceList.Count > 20)
            {
                nextSteps.Add($"Showing first 20 of {resourceList.Count} resources - use discover_azure_resources with resource group filter to see the complete list.");
            }
            
            if (untaggedCount > 0)
            {
                nextSteps.Add($"Found {untaggedCount} resources without tags - consider tagging resources in resource group {resourceGroupName} to improve organization.");
            }
            
            nextSteps.Add("Use get_resource_details with a resource ID to inspect specific resources in this group.");
            nextSteps.Add("Use get_resource_health to check if any resources have health issues.");

            return ToJson(new
            {
                success = true,
                resourceGroup = new
                {
                    name = resourceGroupName,
                    location = rgLocation,
                    tags = rgTags,
                    provisioningState = rgProvisioningState,
                    subscriptionId = subscriptionId
                },
                summary = new
                {
                    totalResources = resourceList.Count,
                    uniqueTypes = byType.Count,
                    uniqueLocations = byLocation.Count,
                    taggedResources = taggedCount,
                    untaggedResources = untaggedCount,
                    tagCoveragePercent = tagCoverage
                },
                breakdown = new
                {
                    byType = byType,
                    byLocation = byLocation
                },
                resources = resourceList.Take(20).Select(r => new
                {
                    id = GetDynamicProperty(r, "id"),
                    name = GetDynamicProperty(r, "name"),
                    type = GetDynamicProperty(r, "type"),
                    location = GetDynamicProperty(r, "location"),
                    tags = GetDynamicTags(r)
                }),
                nextSteps = nextSteps
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting summary for resource group {ResourceGroup}", resourceGroupName);
            return ToJson(new
            {
                success = false,
                error = ex.Message,
                resourceGroup = resourceGroupName
            });
        }
    }

    private static string? GetDynamicProperty(object obj, string propertyName)
    {
        try
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? GetNestedProperty(object obj, string outerProperty, string innerProperty)
    {
        try
        {
            var outer = obj.GetType().GetProperty(outerProperty)?.GetValue(obj);
            if (outer == null) return null;
            return outer.GetType().GetProperty(innerProperty)?.GetValue(outer)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static bool HasTags(object resource)
    {
        try
        {
            var tags = resource.GetType().GetProperty("tags")?.GetValue(resource);
            if (tags == null) return false;
            
            // Check if it's an empty dictionary
            if (tags is IDictionary<string, string> dict)
                return dict.Count > 0;
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object? GetDynamicTags(object resource)
    {
        try
        {
            return resource.GetType().GetProperty("tags")?.GetValue(resource);
        }
        catch
        {
            return null;
        }
    }
}
