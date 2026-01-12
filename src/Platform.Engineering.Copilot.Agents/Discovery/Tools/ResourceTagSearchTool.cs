using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Discovery.Configuration;
using Platform.Engineering.Copilot.Agents.Discovery.State;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;

namespace Platform.Engineering.Copilot.Agents.Discovery.Tools;

/// <summary>
/// Tool for searching Azure resources by tags.
/// Finds resources with specific tag keys or key-value pairs for tag-based discovery,
/// compliance checks, and resource organization.
/// </summary>
public class ResourceTagSearchTool : BaseTool
{
    private readonly DiscoveryStateAccessors _stateAccessors;
    private readonly IAzureResourceService _azureResourceService;
    private readonly DiscoveryAgentOptions _options;

    public override string Name => "search_resources_by_tag";

    public override string Description =>
        "Search for Azure resources using tags. " +
        "Find resources with specific tag keys or key-value pairs. " +
        "Use for tag-based discovery, compliance checks, and resource organization.";

    public ResourceTagSearchTool(
        ILogger<ResourceTagSearchTool> logger,
        DiscoveryStateAccessors stateAccessors,
        IAzureResourceService azureResourceService,
        IOptions<DiscoveryAgentOptions> options) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        _options = options?.Value ?? new DiscoveryAgentOptions();

        // Define parameters
        Parameters.Add(new ToolParameter(
            name: "subscription_id",
            description: "Azure subscription ID to search in",
            required: true));

        Parameters.Add(new ToolParameter(
            name: "tag_key",
            description: "Tag key to search for (e.g., 'Environment', 'Owner', 'CostCenter')",
            required: true));

        Parameters.Add(new ToolParameter(
            name: "tag_value",
            description: "Tag value to match (optional - finds all resources with the tag key if not specified)",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var subscriptionId = GetOptionalString(arguments, "subscription_id");
        var tagKey = GetOptionalString(arguments, "tag_key");
        var tagValue = GetOptionalString(arguments, "tag_value");

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return ToJson(new { success = false, error = "Subscription ID is required" });
        }

        if (string.IsNullOrWhiteSpace(tagKey))
        {
            return ToJson(new { success = false, error = "Tag key is required" });
        }

        Logger.LogInformation("Searching resources by tag {TagKey}={TagValue} in subscription {SubscriptionId}",
            tagKey, tagValue ?? "any", subscriptionId);

        try
        {
            // Get all resources from Azure
            var allResources = await _azureResourceService.ListAllResourcesAsync(subscriptionId, cancellationToken);

            // Filter by tag
            var matchedResources = allResources.Where(r =>
            {
                if (r.Tags == null) return false;

                if (!r.Tags.ContainsKey(tagKey)) return false;

                if (string.IsNullOrWhiteSpace(tagValue)) return true;

                return r.Tags[tagKey]?.Equals(tagValue, StringComparison.OrdinalIgnoreCase) == true;
            }).ToList();

            // Apply configuration: MaxResourcesPerQuery
            if (_options.Discovery.MaxResourcesPerQuery > 0)
            {
                matchedResources = matchedResources.Take(_options.Discovery.MaxResourcesPerQuery).ToList();
            }

            // Group by resource type
            var byType = matchedResources
                .GroupBy(r => r.Type ?? "Unknown")
                .Select(g => new { type = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            // Group by tag value
            var byTagValue = matchedResources
                .Where(r => r.Tags != null && r.Tags.ContainsKey(tagKey))
                .GroupBy(r => r.Tags![tagKey] ?? "null")
                .Select(g => new { tagValue = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            // Build next steps based on results
            var nextSteps = new List<string>();
            if (matchedResources.Count == 0)
            {
                nextSteps.Add($"No resources found with tag '{tagKey}'. Try searching for a different tag or check your tag naming.");
            }
            if (matchedResources.Count > 100)
            {
                nextSteps.Add("Results limited to 100 resources - consider filtering by tag value to narrow results.");
            }
            nextSteps.Add("Say 'show me details for resource <resource-id>' to inspect specific resources.");
            nextSteps.Add("Consider adding tags to untagged resources by saying 'I need to tag resources in this subscription'.");

            return ToJson(new
            {
                success = true,
                subscriptionId,
                search = new
                {
                    tagKey,
                    tagValue = tagValue ?? "any value",
                    matchType = string.IsNullOrWhiteSpace(tagValue) ? "key only" : "key and value"
                },
                summary = new
                {
                    totalMatches = matchedResources.Count,
                    uniqueTypes = byType.Count,
                    uniqueValues = byTagValue.Count
                },
                breakdown = new
                {
                    byType,
                    byTagValue
                },
                resources = matchedResources.Take(100).Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    type = r.Type,
                    resourceGroup = r.ResourceGroup,
                    location = r.Location,
                    tagValue = r.Tags?.GetValueOrDefault(tagKey),
                    allTags = r.Tags
                }),
                nextSteps
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching resources by tag in subscription {SubscriptionId}", subscriptionId);
            return ToJson(new { success = false, error = ex.Message });
        }
    }
}
