using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Platform.Engineering.Copilot.Core.Models.Azure;

namespace Platform.Engineering.Copilot.Core.Interfaces.Discovery;

/// <summary>
/// AI-powered resource discovery service using natural language queries
/// Supports discovering, querying, and analyzing Azure resources through conversational commands
/// Example: "Find all storage accounts in eastus" or "Show me VMs with Standard_D2s_v3 SKU"
/// </summary>
public interface IAzureResourceDiscoveryService
{
    /// <summary>
    /// Discover resources from natural language query
    /// Uses AI to parse intent, extract filters, and search resources
    /// </summary>
    /// <param name="query">Natural language query describing resources to discover</param>
    /// <param name="subscriptionId">Azure subscription ID to search in</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discovery result with matching resources</returns>
    /// <example>
    /// Examples:
    /// - "Find all storage accounts in eastus"
    /// - "Show me VMs running in westus2"
    /// - "List Key Vaults with soft delete enabled"
    /// - "Find resources tagged with environment=production"
    /// - "Show all AKS clusters"
    /// </example>
    Task<ResourceDiscoveryResult> DiscoverResourcesAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed information about a specific resource from natural language query
    /// </summary>
    /// <param name="query">Natural language query describing the resource</param>
    /// <param name="subscriptionId">Azure subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed resource information</returns>
    /// <example>
    /// Examples:
    /// - "Show me details of storage account mydata"
    /// - "Get configuration for VM web-server-01"
    /// - "What's the status of AKS cluster prod-aks?"
    /// </example>
    Task<ResourceDetailsResult> GetResourceDetailsAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get inventory summary for a subscription or resource group
    /// </summary>
    /// <param name="query">Natural language query for inventory scope</param>
    /// <param name="subscriptionId">Azure subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inventory summary with resource counts and groupings</returns>
    /// <example>
    /// Examples:
    /// - "Show me inventory for resource group rg-prod"
    /// - "Get resource summary for subscription"
    /// - "What resources do I have in eastus?"
    /// </example>
    Task<ResourceInventoryResult> GetInventorySummaryAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search resources by tags using natural language
    /// </summary>
    /// <param name="query">Natural language query with tag filters</param>
    /// <param name="subscriptionId">Azure subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resources matching tag criteria</returns>
    /// <example>
    /// Examples:
    /// - "Find all resources tagged environment=dev"
    /// - "Show production resources"
    /// - "List resources with cost-center tag"
    /// </example>
    Task<ResourceDiscoveryResult> SearchByTagsAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get health status for resources from natural language query
    /// </summary>
    /// <param name="query">Natural language query for health check</param>
    /// <param name="subscriptionId">Azure subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status information</returns>
    /// <example>
    /// Examples:
    /// - "Check health of all VMs in rg-prod"
    /// - "Are there any unhealthy resources?"
    /// - "Show me resource health for eastus"
    /// </example>
    Task<ResourceHealthResult> GetHealthStatusAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all resource groups in a subscription
    /// </summary>
    Task<List<ResourceGroup>> ListResourceGroupsAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all subscriptions accessible to the current identity
    /// </summary>
    Task<List<AzureSubscription>> ListSubscriptionsAsync(
        CancellationToken cancellationToken = default);
}
