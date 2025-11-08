using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;
using Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;

namespace Platform.Engineering.Copilot.Infrastructure.Core.Services;

/// <summary>
/// Infrastructure provisioning service for Azure resources
/// Handles actual resource provisioning through Azure Resource Manager
/// Note: The InfrastructureAgent handles AI-powered query parsing
/// This service focuses on executing the provisioning with validated parameters
/// </summary>
public class InfrastructureProvisioningService : IInfrastructureProvisioningService
{
    private readonly ILogger<InfrastructureProvisioningService> _logger;
    private readonly IAzureResourceService _azureResourceService;

    public InfrastructureProvisioningService(
        ILogger<InfrastructureProvisioningService> logger,
        IAzureResourceService azureResourceService)
    {
        _logger = logger;
        _azureResourceService = azureResourceService;
    }

    public async Task<InfrastructureProvisionResult> ProvisionInfrastructureAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing infrastructure provisioning query: {Query}", query);

        try
        {
            // Simple pattern-based parsing (AI parsing happens in InfrastructureAgent)
            // This is a fallback for direct API calls
            var intent = ParseQuerySimple(query);

            if (!intent.Success)
            {
                return new InfrastructureProvisionResult
                {
                    Success = false,
                    Status = "Failed",
                    ErrorDetails = intent.ErrorMessage ?? "Failed to parse query",
                    Message = $"❌ Could not understand query: {query}"
                };
            }

            // Route to appropriate provisioning method
            return await ProvisionResourceAsync(intent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing infrastructure query: {Query}", query);
            return new InfrastructureProvisionResult
            {
                Success = false,
                Status = "Failed",
                ErrorDetails = ex.Message,
                Message = "❌ Failed to provision infrastructure"
            };
        }
    }

    public async Task<InfrastructureCostEstimate> EstimateCostAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Estimating cost for query: {Query}", query);

        try
        {
            var intent = ParseQuerySimple(query);

            // Simple cost estimation based on resource type
            var monthlyCost = intent.ResourceType?.ToLowerInvariant() switch
            {
                "vnet" or "virtual-network" => 0.00m,
                "storage-account" => 20.00m,
                "keyvault" or "key-vault" => 0.03m,
                "nsg" => 0.00m,
                "load-balancer" => 25.00m,
                "managed-identity" => 0.00m,
                "log-analytics" => 2.30m,
                "app-insights" => 2.88m,
                _ => 10.00m
            };

            return new InfrastructureCostEstimate
            {
                ResourceType = intent.ResourceType ?? "Unknown",
                Location = intent.Location ?? "eastus",
                MonthlyEstimate = monthlyCost,
                AnnualEstimate = monthlyCost * 12,
                Currency = "USD",
                Notes = "Estimated cost based on standard configuration"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating cost");
            return new InfrastructureCostEstimate
            {
                ResourceType = "Error",
                MonthlyEstimate = 0,
                AnnualEstimate = 0,
                Notes = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<List<string>> ListResourceGroupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing Resource Groups");

        try
        {
            var resourceGroups = await _azureResourceService.ListResourceGroupsAsync(subscriptionId: null, cancellationToken);
            var result = new List<string>();
            
            foreach (var rg in resourceGroups)
            {
                dynamic rgData = rg;
                result.Add(rgData.name?.ToString() ?? "Unknown");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Resource Groups");
            return new List<string>();
        }
    }

    public async Task<bool> DeleteResourceGroupAsync(
        string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting Resource Group: {ResourceGroupName}", resourceGroupName);

        try
        {
            await _azureResourceService.DeleteResourceGroupAsync(
                resourceGroupName, 
                subscriptionId: null, 
                cancellationToken);
            
            _logger.LogInformation("Successfully deleted resource group {ResourceGroupName}", resourceGroupName);
            return true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Resource group {ResourceGroupName} not found", resourceGroupName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Resource Group: {ResourceGroupName}", resourceGroupName);
            return false;
        }
    }

    #region Simple Query Parsing

    /// <summary>
    /// Simple pattern-based query parser (not AI-powered)
    /// Note: The InfrastructureAgent already does AI parsing, this is a fallback
    /// </summary>
    private InfrastructureIntent ParseQuerySimple(string query)
    {
        _logger.LogDebug("Parsing query with pattern matching: {Query}", query);

        try
        {
            var lowerQuery = query.ToLowerInvariant();
            
            // Extract resource type
            string? resourceType = null;
            if (lowerQuery.Contains("storage account") || lowerQuery.Contains("storage-account"))
                resourceType = "storage-account";
            else if (lowerQuery.Contains("key vault") || lowerQuery.Contains("keyvault"))
                resourceType = "keyvault";
            else if (lowerQuery.Contains("vnet") || lowerQuery.Contains("virtual network"))
                resourceType = "vnet";
            else if (lowerQuery.Contains("blob container"))
                resourceType = "blob-container";
            else if (lowerQuery.Contains("nsg") || lowerQuery.Contains("network security"))
                resourceType = "nsg";
            else if (lowerQuery.Contains("managed identity"))
                resourceType = "managed-identity";
            else if (lowerQuery.Contains("log analytics"))
                resourceType = "log-analytics";
            else if (lowerQuery.Contains("app insights") || lowerQuery.Contains("application insights"))
                resourceType = "app-insights";

            if (string.IsNullOrEmpty(resourceType))
            {
                return new InfrastructureIntent
                {
                    Success = false,
                    ErrorMessage = "Could not determine resource type from query"
                };
            }

            // Extract resource name (look for "named X" or "name X")
            var nameMatch = Regex.Match(query, @"named?\s+([a-zA-Z0-9\-]+)", RegexOptions.IgnoreCase);
            var resourceName = nameMatch.Success ? nameMatch.Groups[1].Value : $"{resourceType}-default";

            // Extract location
            var location = "eastus"; // default
            if (lowerQuery.Contains("usgovvirginia") || lowerQuery.Contains("us gov virginia"))
                location = "usgovvirginia";
            else if (lowerQuery.Contains("westus2") || lowerQuery.Contains("west us 2"))
                location = "westus2";
            else if (lowerQuery.Contains("centralus") || lowerQuery.Contains("central us"))
                location = "centralus";
            else if (lowerQuery.Contains("eastus"))
                location = "eastus";

            // Extract SKU if mentioned
            string? sku = null;
            if (lowerQuery.Contains("standard_lrs"))
                sku = "Standard_LRS";
            else if (lowerQuery.Contains("premium_lrs"))
                sku = "Premium_LRS";
            else if (lowerQuery.Contains("standard"))
                sku = "standard";

            // Build parameters
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(sku))
                parameters["sku"] = sku;

            return new InfrastructureIntent
            {
                Success = true,
                ResourceType = resourceType,
                ResourceName = resourceName,
                ResourceGroupName = $"rg-{resourceName}",
                Location = location,
                Parameters = parameters
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing query");
            return new InfrastructureIntent
            {
                Success = false,
                ErrorMessage = $"Parsing error: {ex.Message}"
            };
        }
    }

    #endregion

    #region Resource Provisioning

    private async Task<InfrastructureProvisionResult> ProvisionResourceAsync(
        InfrastructureIntent intent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Provisioning {ResourceType}: {ResourceName} in {ResourceGroup}", 
            intent.ResourceType, intent.ResourceName, intent.ResourceGroupName);

        var location = intent.Location ?? "eastus";
        
        try
        {
            // Ensure resource group exists
            var resourceGroup = await _azureResourceService.GetResourceGroupAsync(
                intent.ResourceGroupName, 
                subscriptionId: null, 
                cancellationToken);
            
            if (resourceGroup == null)
            {
                _logger.LogInformation("Resource group {ResourceGroup} does not exist, creating it", intent.ResourceGroupName);
                resourceGroup = await _azureResourceService.CreateResourceGroupAsync(
                    intent.ResourceGroupName,
                    location,
                    subscriptionId: null,
                    tags: new Dictionary<string, string>
                    {
                        { "ManagedBy", "SupervisorPlatform" },
                        { "CreatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd") }
                    },
                    cancellationToken);
            }

            // Route to specialized resource creation methods
            object result;
            
            switch (intent.ResourceType?.ToLowerInvariant())
            {
                case "storage-account":
                    result = await CreateStorageAccountAsync(intent, location, cancellationToken);
                    break;
                    
                case "keyvault":
                case "key-vault":
                    result = await CreateKeyVaultAsync(intent, location, cancellationToken);
                    break;
                    
                case "vnet":
                case "virtual-network":
                    result = await CreateVirtualNetworkAsync(intent, location, cancellationToken);
                    break;
                    
                case "blob-container":
                    result = await CreateBlobContainerAsync(intent, location, cancellationToken);
                    break;
                    
                default:
                    // Generic resource creation
                    result = await CreateGenericResourceAsync(intent, location, cancellationToken);
                    break;
            }

            // Parse the result
            dynamic resultData = result;
            var providerType = GetProviderType(intent.ResourceType);
            
            // Safely extract properties with null checks
            bool success = true;
            string? resourceId = null;
            string status = "Succeeded";
            string? message = null;
            
            try
            {
                var resultType = result.GetType();
                
                // Try to get success property
                var successProp = resultType.GetProperty("success");
                if (successProp != null)
                    success = (bool)(successProp.GetValue(result) ?? true);
                
                // Try to get resourceId property
                var resourceIdProp = resultType.GetProperty("resourceId");
                if (resourceIdProp != null)
                    resourceId = resourceIdProp.GetValue(result)?.ToString();
                
                // Try storageAccountId as fallback
                if (string.IsNullOrEmpty(resourceId))
                {
                    var storageIdProp = resultType.GetProperty("storageAccountId");
                    if (storageIdProp != null)
                        resourceId = storageIdProp.GetValue(result)?.ToString();
                }
                
                // Try to get status property (may not exist)
                var statusProp = resultType.GetProperty("status");
                if (statusProp != null)
                    status = statusProp.GetValue(result)?.ToString() ?? "Succeeded";
                
                // Try to get message property
                var messageProp = resultType.GetProperty("message");
                if (messageProp != null)
                    message = messageProp.GetValue(result)?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing result properties, using defaults");
            }
            
            return new InfrastructureProvisionResult
            {
                Success = success,
                ResourceId = resourceId ?? $"/subscriptions/{{sub}}/resourceGroups/{intent.ResourceGroupName}/providers/{providerType}/{intent.ResourceName}",
                ResourceName = intent.ResourceName,
                ResourceType = providerType,
                Status = status,
                Message = message ?? $"✅ {GetFriendlyResourceType(intent.ResourceType)} '{intent.ResourceName}' provisioned in {location}",
                Properties = ExtractPropertiesFromResult(result, location, intent.ResourceGroupName)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision {ResourceType}: {ResourceName}", 
                intent.ResourceType, intent.ResourceName);
            
            return new InfrastructureProvisionResult
            {
                Success = false,
                ResourceName = intent.ResourceName,
                ResourceType = GetProviderType(intent.ResourceType),
                Status = "Failed",
                ErrorDetails = ex.Message,
                Message = $"❌ Failed to provision {GetFriendlyResourceType(intent.ResourceType)}: {ex.Message}"
            };
        }
    }

    private async Task<object> CreateStorageAccountAsync(
        InfrastructureIntent intent,
        string location,
        CancellationToken cancellationToken)
    {
        var storageSettings = new Dictionary<string, object>
        {
            { "sku", intent.Parameters?.GetValueOrDefault("sku", "Standard_LRS") ?? "Standard_LRS" },
            { "environment", intent.Parameters?.GetValueOrDefault("environment", "development") ?? "development" }
        };

        return await _azureResourceService.CreateStorageAccountAsync(
            intent.ResourceName,
            intent.ResourceGroupName,
            location,
            storageSettings,
            subscriptionId: null,
            cancellationToken);
    }

    private async Task<object> CreateKeyVaultAsync(
        InfrastructureIntent intent,
        string location,
        CancellationToken cancellationToken)
    {
        var keyVaultSettings = new Dictionary<string, object>
        {
            { "sku", intent.Parameters?.GetValueOrDefault("sku", "standard") ?? "standard" },
            { "enableSoftDelete", intent.Parameters?.GetValueOrDefault("enableSoftDelete", true) ?? true },
            { "enablePurgeProtection", intent.Parameters?.GetValueOrDefault("enablePurgeProtection", true) ?? true },
            { "environment", intent.Parameters?.GetValueOrDefault("environment", "development") ?? "development" }
        };

        return await _azureResourceService.CreateKeyVaultAsync(
            intent.ResourceName,
            intent.ResourceGroupName,
            location,
            keyVaultSettings,
            subscriptionId: null,
            cancellationToken);
    }

    private async Task<object> CreateVirtualNetworkAsync(
        InfrastructureIntent intent,
        string location,
        CancellationToken cancellationToken)
    {
        // VNet creation would go through generic resource creation
        var properties = new
        {
            addressSpace = new
            {
                addressPrefixes = new[] { intent.Parameters?.GetValueOrDefault("addressSpace", "10.0.0.0/16")?.ToString() ?? "10.0.0.0/16" }
            }
        };

        return await _azureResourceService.CreateResourceAsync(
            intent.ResourceGroupName,
            "Microsoft.Network/virtualNetworks",
            intent.ResourceName,
            properties,
            subscriptionId: null,
            location,
            tags: new Dictionary<string, string> { { "ManagedBy", "SupervisorPlatform" } },
            cancellationToken);
    }

    private async Task<object> CreateBlobContainerAsync(
        InfrastructureIntent intent,
        string location,
        CancellationToken cancellationToken)
    {
        var storageAccountName = intent.Parameters?.GetValueOrDefault("storageAccountName")?.ToString();
        
        if (string.IsNullOrEmpty(storageAccountName))
        {
            return new
            {
                success = false,
                status = "Failed",
                message = "Blob container creation requires 'storageAccountName' parameter"
            };
        }

        var containerSettings = new Dictionary<string, object>
        {
            { "publicAccess", intent.Parameters?.GetValueOrDefault("publicAccess", "None") ?? "None" }
        };

        return await _azureResourceService.CreateBlobContainerAsync(
            intent.ResourceName,
            storageAccountName,
            intent.ResourceGroupName,
            containerSettings,
            subscriptionId: null,
            cancellationToken);
    }

    private async Task<object> CreateGenericResourceAsync(
        InfrastructureIntent intent,
        string location,
        CancellationToken cancellationToken)
    {
        var providerType = GetProviderType(intent.ResourceType);
        
        return await _azureResourceService.CreateResourceAsync(
            intent.ResourceGroupName,
            providerType,
            intent.ResourceName,
            intent.Parameters ?? new Dictionary<string, object>(),
            subscriptionId: null,
            location,
            tags: new Dictionary<string, string> { { "ManagedBy", "SupervisorPlatform" } },
            cancellationToken);
    }

    private Dictionary<string, string> ExtractPropertiesFromResult(object resultData, string location, string resourceGroup)
    {
        var properties = new Dictionary<string, string>
        {
            { "location", location },
            { "resourceGroup", resourceGroup }
        };

        // Try to extract common properties from the result using reflection
        try
        {
            var resultType = resultData.GetType();
            
            var skuProp = resultType.GetProperty("sku");
            if (skuProp != null)
            {
                var skuValue = skuProp.GetValue(resultData);
                if (skuValue != null)
                    properties["sku"] = skuValue.ToString() ?? "";
            }
            
            var accessTierProp = resultType.GetProperty("accessTier");
            if (accessTierProp != null)
            {
                var accessTierValue = accessTierProp.GetValue(resultData);
                if (accessTierValue != null)
                    properties["accessTier"] = accessTierValue.ToString() ?? "";
            }
            
            var httpsOnlyProp = resultType.GetProperty("httpsOnly");
            if (httpsOnlyProp != null)
            {
                var httpsOnlyValue = httpsOnlyProp.GetValue(resultData);
                if (httpsOnlyValue != null)
                    properties["httpsOnly"] = httpsOnlyValue.ToString() ?? "";
            }
        }
        catch (Exception ex)
        {
            // Ignore property extraction errors
            _logger.LogDebug(ex, "Could not extract all properties from result");
        }

        return properties;
    }

    private string GetProviderType(string? resourceType)
    {
        return resourceType?.ToLowerInvariant() switch
        {
            "vnet" or "virtual-network" => "Microsoft.Network/virtualNetworks",
            "subnet" => "Microsoft.Network/virtualNetworks/subnets",
            "nsg" => "Microsoft.Network/networkSecurityGroups",
            "load-balancer" => "Microsoft.Network/loadBalancers",
            "storage-account" => "Microsoft.Storage/storageAccounts",
            "blob-container" => "Microsoft.Storage/storageAccounts/blobServices/containers",
            "file-share" => "Microsoft.Storage/storageAccounts/fileServices/shares",
            "keyvault" or "key-vault" => "Microsoft.KeyVault/vaults",
            "managed-identity" => "Microsoft.ManagedIdentity/userAssignedIdentities",
            "log-analytics" => "Microsoft.OperationalInsights/workspaces",
            "app-insights" => "Microsoft.Insights/components",
            _ => "Unknown"
        };
    }

    private string GetFriendlyResourceType(string? resourceType)
    {
        return resourceType?.ToLowerInvariant() switch
        {
            "vnet" or "virtual-network" => "Virtual Network",
            "subnet" => "Subnet",
            "nsg" => "Network Security Group",
            "load-balancer" => "Load Balancer",
            "storage-account" => "Storage Account",
            "blob-container" => "Blob Container",
            "file-share" => "File Share",
            "keyvault" or "key-vault" => "Key Vault",
            "managed-identity" => "Managed Identity",
            "log-analytics" => "Log Analytics Workspace",
            "app-insights" => "Application Insights",
            _ => resourceType ?? "Resource"
        };
    }

    #endregion
}

/// <summary>
/// Parsed infrastructure intent from natural language query
/// </summary>
internal class InfrastructureIntent
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResourceType { get; set; }
    public string ResourceGroupName { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}
