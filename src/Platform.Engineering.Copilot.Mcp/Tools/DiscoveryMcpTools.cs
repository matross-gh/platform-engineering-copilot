using System.ComponentModel;
using Platform.Engineering.Copilot.Agents.Discovery.Tools;

namespace Platform.Engineering.Copilot.Mcp.Tools;

/// <summary>
/// MCP tools for Azure resource discovery operations. Wraps Agent Framework discovery tools
/// for exposure via the MCP protocol (GitHub Copilot, Claude Desktop, etc.)
/// </summary>
public class DiscoveryMcpTools
{
    private readonly ResourceDiscoveryTool _resourceDiscoveryTool;
    private readonly ResourceDetailsTool _resourceDetailsTool;
    private readonly ResourceHealthTool _resourceHealthTool;
    private readonly DependencyMappingTool _dependencyMappingTool;
    private readonly SubscriptionListTool _subscriptionListTool;

    public DiscoveryMcpTools(
        ResourceDiscoveryTool resourceDiscoveryTool,
        ResourceDetailsTool resourceDetailsTool,
        ResourceHealthTool resourceHealthTool,
        DependencyMappingTool dependencyMappingTool,
        SubscriptionListTool subscriptionListTool)
    {
        _resourceDiscoveryTool = resourceDiscoveryTool;
        _resourceDetailsTool = resourceDetailsTool;
        _resourceHealthTool = resourceHealthTool;
        _dependencyMappingTool = dependencyMappingTool;
        _subscriptionListTool = subscriptionListTool;
    }

    /// <summary>
    /// Discover Azure resources across subscriptions
    /// </summary>
    [Description("Discover and list Azure resources with comprehensive filtering. Search by subscription, resource group, type, location, or tags.")]
    public async Task<string> DiscoverResourcesAsync(
        string subscriptionId,
        string? resourceGroup = null,
        string? resourceType = null,
        string? location = null,
        string? tagFilter = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscriptionId"] = subscriptionId,
            ["resourceGroup"] = resourceGroup,
            ["resourceType"] = resourceType,
            ["location"] = location,
            ["tagFilter"] = tagFilter
        };
        return await _resourceDiscoveryTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Get detailed information about a specific Azure resource
    /// </summary>
    [Description("Get comprehensive details about a specific Azure resource including properties, configuration, metrics, and compliance status.")]
    public async Task<string> GetResourceDetailsAsync(
        string resourceId,
        bool includeMetrics = false,
        bool includeCompliance = false,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["resource_id"] = resourceId,
            ["include_metrics"] = includeMetrics,
            ["include_compliance"] = includeCompliance
        };
        return await _resourceDetailsTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Check the health status of Azure resources
    /// </summary>
    [Description("Check health and availability status of Azure resources. Returns current health state, any issues, and recommendations.")]
    public async Task<string> GetResourceHealthAsync(
        string? resourceId = null,
        string? subscriptionId = null,
        string? resourceGroup = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["resource_id"] = resourceId,
            ["subscription_id"] = subscriptionId,
            ["resource_group"] = resourceGroup
        };
        return await _resourceHealthTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Map dependencies between Azure resources
    /// </summary>
    [Description("Analyze and map dependencies between Azure resources. Useful for understanding resource relationships and impact analysis.")]
    public async Task<string> MapResourceDependenciesAsync(
        string resourceId,
        int depth = 2,
        bool includeNetwork = true,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["resource_id"] = resourceId,
            ["depth"] = depth,
            ["include_network"] = includeNetwork
        };
        return await _dependencyMappingTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// List available Azure subscriptions
    /// </summary>
    [Description("List all Azure subscriptions accessible to the current identity. Returns subscription IDs, names, and states.")]
    public async Task<string> ListSubscriptionsAsync(
        bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["include_disabled"] = includeDisabled
        };
        return await _subscriptionListTool.ExecuteAsync(args, cancellationToken);
    }
}
