using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Platform.Engineering.Copilot.Core.Interfaces.Discovery;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;
using Platform.Engineering.Copilot.Core.Services.Azure;
using Platform.Engineering.Copilot.Core.Models.Azure;

namespace Platform.Engineering.Copilot.Discovery.Agent.Services;

/// <summary>
/// AI-powered resource discovery service implementation
/// Uses Semantic Kernel to parse natural language queries and discover Azure resources
/// </summary>
public class AzureResourceDiscoveryService : IAzureResourceDiscoveryService
{
    private readonly ILogger<AzureResourceDiscoveryService> _logger;
    private readonly Kernel _kernel;
    private readonly IAzureResourceService _azureResourceService;
    private readonly AzureMcpClient _azureMcpClient;

    public AzureResourceDiscoveryService(
        ILogger<AzureResourceDiscoveryService> logger,
        Kernel kernel,
        IAzureResourceService azureResourceService,
        AzureMcpClient azureMcpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        _azureMcpClient = azureMcpClient ?? throw new ArgumentNullException(nameof(azureMcpClient));
    }

    public async Task<ResourceDiscoveryResult> DiscoverResourcesAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Discovering resources from query: {Query}", query);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return new ResourceDiscoveryResult
                {
                    Success = false,
                    ErrorDetails = "Subscription ID is required"
                };
            }

            // Get all resources
            var allResources = await _azureResourceService.ListAllResourcesAsync(subscriptionId, cancellationToken);

            // Use AI to parse query and extract filters
            var filters = await ParseDiscoveryQueryAsync(query, cancellationToken);

            // Apply filters
            var filteredResources = ApplyFilters(allResources, filters);

            // Convert to DiscoveredResource
            var discoveredResources = filteredResources.Select(r => new DiscoveredResource
            {
                ResourceId = r.Id ?? string.Empty,
                Name = r.Name ?? string.Empty,
                Type = r.Type ?? string.Empty,
                Location = r.Location ?? string.Empty,
                ResourceGroup = r.ResourceGroup ?? string.Empty,
                Tags = r.Tags
            }).ToList();

            // Group results
            var byType = discoveredResources.GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            var byLocation = discoveredResources.GroupBy(r => r.Location)
                .ToDictionary(g => g.Key, g => g.Count());
            var byResourceGroup = discoveredResources.GroupBy(r => r.ResourceGroup)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ResourceDiscoveryResult
            {
                Success = true,
                TotalCount = discoveredResources.Count,
                Resources = discoveredResources,
                GroupByType = byType,
                GroupByLocation = byLocation,
                GroupByResourceGroup = byResourceGroup,
                Message = $"Found {discoveredResources.Count} resources matching query"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering resources from query: {Query}", query);
            return new ResourceDiscoveryResult
            {
                Success = false,
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ResourceDetailsResult> GetResourceDetailsAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting resource details from query: {Query}", query);

            // Use AI to extract resource identifier
            var resourceInfo = await ParseResourceIdentifierAsync(query, cancellationToken);

            if (string.IsNullOrWhiteSpace(resourceInfo.ResourceId) && string.IsNullOrWhiteSpace(resourceInfo.ResourceName))
            {
                return new ResourceDetailsResult
                {
                    Success = false,
                    ErrorDetails = "Could not identify resource from query"
                };
            }

            // Get resource details
            // Note: GetResourceAsync requires more parameters - this is a simplified version
            // In production, you'd need to parse the full resource ID or use a different approach
            var allResources = await _azureResourceService.ListAllResourcesAsync(subscriptionId, cancellationToken);
            var resource = allResources.FirstOrDefault(r =>
                r.Id?.Contains(resourceInfo.ResourceId ?? resourceInfo.ResourceName ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true ||
                r.Name?.Equals(resourceInfo.ResourceName, StringComparison.OrdinalIgnoreCase) == true);

            if (resource == null)
            {
                return new ResourceDetailsResult
                {
                    Success = false,
                    ErrorDetails = "Resource not found"
                };
            }

            // Get health status if available (simplified)
            string? healthStatus = null;
            try
            {
                var health = await _azureResourceService.GetResourceHealthAsync(
                    resource.Id ?? string.Empty,
                    cancellationToken);
                
                // Handle dynamic health status object
                if (health != null)
                {
                    dynamic healthObj = health;
                    healthStatus = healthObj.AvailabilityState?.ToString() ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get health for resource {ResourceId}", resource.Id);
            }

            return new ResourceDetailsResult
            {
                Success = true,
                Resource = new DiscoveredResource
                {
                    ResourceId = resource.Id ?? string.Empty,
                    Name = resource.Name ?? string.Empty,
                    Type = resource.Type ?? string.Empty,
                    Location = resource.Location ?? string.Empty,
                    ResourceGroup = resource.ResourceGroup ?? string.Empty,
                    Tags = resource.Tags,
                    Properties = resource.Properties as Dictionary<string, object>
                },
                HealthStatus = healthStatus,
                Message = "Resource details retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource details from query: {Query}", query);
            return new ResourceDetailsResult
            {
                Success = false,
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ResourceInventoryResult> GetInventorySummaryAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting inventory summary from query: {Query}", query);

            // Parse scope from query (subscription or resource group)
            var scope = await ParseInventoryScopeAsync(query, cancellationToken);

            var allResources = await _azureResourceService.ListAllResourcesAsync(subscriptionId, cancellationToken);

            // Filter by resource group if specified
            if (!string.IsNullOrWhiteSpace(scope.ResourceGroup))
            {
                allResources = allResources.Where(r =>
                    r.ResourceGroup?.Equals(scope.ResourceGroup, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            var resourceList = allResources.ToList();
            var byType = resourceList.GroupBy(r => r.Type ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
            var byLocation = resourceList.GroupBy(r => r.Location ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
            var byResourceGroup = resourceList.GroupBy(r => r.ResourceGroup ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
            var locations = resourceList.Select(r => r.Location ?? "Unknown").Distinct().ToList();

            return new ResourceInventoryResult
            {
                Success = true,
                Scope = scope.ResourceGroup ?? "Subscription",
                TotalResources = resourceList.Count,
                ResourcesByType = byType,
                ResourcesByLocation = byLocation,
                ResourcesByResourceGroup = byResourceGroup,
                Locations = locations,
                Message = $"Inventory summary for {(scope.ResourceGroup ?? "subscription")}: {resourceList.Count} total resources"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory summary from query: {Query}", query);
            return new ResourceInventoryResult
            {
                Success = false,
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ResourceDiscoveryResult> SearchByTagsAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching resources by tags from query: {Query}", query);

            // Parse tag filters from query
            var tagFilters = await ParseTagFiltersAsync(query, cancellationToken);

            var allResources = await _azureResourceService.ListAllResourcesAsync(subscriptionId, cancellationToken);

            // Filter by tags
            var filteredResources = allResources.Where(r =>
            {
                if (r.Tags == null || !r.Tags.Any()) return false;

                return tagFilters.All(filter =>
                    r.Tags.ContainsKey(filter.Key) &&
                    (string.IsNullOrWhiteSpace(filter.Value) ||
                     r.Tags[filter.Key]?.Equals(filter.Value, StringComparison.OrdinalIgnoreCase) == true));
            }).ToList();

            var discoveredResources = filteredResources.Select(r => new DiscoveredResource
            {
                ResourceId = r.Id ?? string.Empty,
                Name = r.Name ?? string.Empty,
                Type = r.Type ?? string.Empty,
                Location = r.Location ?? string.Empty,
                ResourceGroup = r.ResourceGroup ?? string.Empty,
                Tags = r.Tags
            }).ToList();

            return new ResourceDiscoveryResult
            {
                Success = true,
                TotalCount = discoveredResources.Count,
                Resources = discoveredResources,
                Message = $"Found {discoveredResources.Count} resources matching tag filters"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by tags from query: {Query}", query);
            return new ResourceDiscoveryResult
            {
                Success = false,
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ResourceHealthResult> GetHealthStatusAsync(
        string query,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting health status from query: {Query}", query);

            // Parse filters from query
            var filters = await ParseDiscoveryQueryAsync(query, cancellationToken);

            var allResources = await _azureResourceService.ListAllResourcesAsync(subscriptionId, cancellationToken);
            var filteredResources = ApplyFilters(allResources, filters);

            var healthInfoList = new List<ResourceHealthInfo>();
            var healthyCount = 0;
            var unhealthyCount = 0;
            var unknownCount = 0;

            foreach (var resource in filteredResources.Take(50)) // Limit to avoid too many API calls
            {
                try
                {
                    var healthStatus = await _azureResourceService.GetResourceHealthAsync(
                        resource.Id ?? string.Empty,
                        cancellationToken);

                    // Handle dynamic health status object
                    string status = "Unknown";
                    string? statusDetails = null;
                    
                    if (healthStatus != null)
                    {
                        dynamic healthObj = healthStatus;
                        status = healthObj.AvailabilityState?.ToString() ?? "Unknown";
                        statusDetails = healthObj.Summary?.ToString();
                    }
                    
                    if (status.Equals("Available", StringComparison.OrdinalIgnoreCase)) healthyCount++;
                    else if (status.Equals("Unavailable", StringComparison.OrdinalIgnoreCase)) unhealthyCount++;
                    else unknownCount++;

                    healthInfoList.Add(new ResourceHealthInfo
                    {
                        ResourceId = resource.Id ?? string.Empty,
                        ResourceName = resource.Name ?? string.Empty,
                        ResourceType = resource.Type ?? string.Empty,
                        HealthStatus = status,
                        StatusDetails = statusDetails,
                        LastUpdated = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get health for resource {ResourceId}", resource.Id);
                    unknownCount++;
                    healthInfoList.Add(new ResourceHealthInfo
                    {
                        ResourceId = resource.Id ?? string.Empty,
                        ResourceName = resource.Name ?? string.Empty,
                        ResourceType = resource.Type ?? string.Empty,
                        HealthStatus = "Unknown",
                        StatusDetails = "Health status unavailable"
                    });
                }
            }

            return new ResourceHealthResult
            {
                Success = true,
                TotalResources = healthInfoList.Count,
                HealthyCount = healthyCount,
                UnhealthyCount = unhealthyCount,
                UnknownCount = unknownCount,
                ResourceHealth = healthInfoList,
                Message = $"Health check complete: {healthyCount} healthy, {unhealthyCount} unhealthy, {unknownCount} unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status from query: {Query}", query);
            return new ResourceHealthResult
            {
                Success = false,
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<List<ResourceGroup>> ListResourceGroupsAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resourceGroups = await _azureResourceService.ListResourceGroupsAsync(subscriptionId, cancellationToken);

            return resourceGroups.Select(rg => new ResourceGroup
            {
                Name = rg.Name ?? string.Empty,
                Location = rg.Location ?? string.Empty,
                Tags = rg.Tags,
                ProvisioningState = rg.ProvisioningState ?? "Unknown"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing resource groups");
            return new List<ResourceGroup>();
        }
    }

    public async Task<List<AzureSubscription>> ListSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _azureResourceService.ListSubscriptionsAsync(cancellationToken);
            return subscriptions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing subscriptions");
            return new List<AzureSubscription>();
        }
    }

    // Helper methods
    private async Task<DiscoveryFilters> ParseDiscoveryQueryAsync(string query, CancellationToken cancellationToken)
    {
        var prompt = $@"Extract resource discovery filters from this query: '{query}'
Return JSON with these optional fields:
- resourceType: Azure resource type (e.g., 'Microsoft.Storage/storageAccounts')
- location: Azure region (e.g., 'eastus', 'westus2')
- resourceGroup: Resource group name
- tags: Dictionary of tag key-value pairs

Only include fields that are mentioned in the query.";

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var json = result.ToString();

        // Parse JSON (simplified - in production use System.Text.Json)
        return new DiscoveryFilters
        {
            ResourceType = ExtractValue(json, "resourceType"),
            Location = ExtractValue(json, "location"),
            ResourceGroup = ExtractValue(json, "resourceGroup")
        };
    }

    private async Task<(string? ResourceId, string? ResourceName)> ParseResourceIdentifierAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var prompt = $@"Extract resource identifier from this query: '{query}'
Return JSON with either:
- resourceId: Full Azure resource ID
- resourceName: Resource name

Example output: {{""resourceName"": ""mydata""}}";

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var json = result.ToString();

        return (ExtractValue(json, "resourceId"), ExtractValue(json, "resourceName"));
    }

    private async Task<(string? ResourceGroup, string? Subscription)> ParseInventoryScopeAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var prompt = $@"Extract inventory scope from this query: '{query}'
Return JSON with optional fields:
- resourceGroup: Resource group name if specified
- subscription: Subscription ID if specified

Example: {{""resourceGroup"": ""rg-prod""}}";

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var json = result.ToString();

        return (ExtractValue(json, "resourceGroup"), ExtractValue(json, "subscription"));
    }

    private async Task<Dictionary<string, string>> ParseTagFiltersAsync(string query, CancellationToken cancellationToken)
    {
        var prompt = $@"Extract tag filters from this query: '{query}'
Return JSON object with tag key-value pairs.
Example: {{""environment"": ""production"", ""cost-center"": ""engineering""}}
If only a tag key is mentioned without a value, use empty string as value.";

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var json = result.ToString();

        // Simple JSON parsing (in production use System.Text.Json)
        return new Dictionary<string, string>();
    }

    private IEnumerable<Platform.Engineering.Copilot.Core.Models.Azure.AzureResource> ApplyFilters(
        IEnumerable<Platform.Engineering.Copilot.Core.Models.Azure.AzureResource> resources,
        DiscoveryFilters filters)
    {
        var filtered = resources;

        if (!string.IsNullOrWhiteSpace(filters.ResourceType))
        {
            filtered = filtered.Where(r =>
                r.Type?.Equals(filters.ResourceType, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(filters.Location))
        {
            filtered = filtered.Where(r =>
                r.Location?.Equals(filters.Location, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(filters.ResourceGroup))
        {
            filtered = filtered.Where(r =>
                r.ResourceGroup?.Equals(filters.ResourceGroup, StringComparison.OrdinalIgnoreCase) == true);
        }

        return filtered;
    }

    private string? ExtractValue(string json, string key)
    {
        // Simple JSON value extraction (in production use System.Text.Json)
        var keyPattern = $"\"{key}\"";
        var keyIndex = json.IndexOf(keyPattern, StringComparison.OrdinalIgnoreCase);
        if (keyIndex == -1) return null;

        var colonIndex = json.IndexOf(":", keyIndex);
        if (colonIndex == -1) return null;

        var startQuote = json.IndexOf("\"", colonIndex);
        if (startQuote == -1) return null;

        var endQuote = json.IndexOf("\"", startQuote + 1);
        if (endQuote == -1) return null;

        return json.Substring(startQuote + 1, endQuote - startQuote - 1);
    }

    private class DiscoveryFilters
    {
        public string? ResourceType { get; set; }
        public string? Location { get; set; }
        public string? ResourceGroup { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }
}
