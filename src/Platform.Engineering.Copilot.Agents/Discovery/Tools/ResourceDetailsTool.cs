using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;

namespace Platform.Engineering.Copilot.Agents.Discovery.Tools;

/// <summary>
/// Tool for getting detailed information about a specific Azure resource.
/// </summary>
public class ResourceDetailsTool : BaseTool
{
    public override string Name => "get_resource_details";

    public override string Description =>
        "Get comprehensive details about a specific Azure resource by ID or name. " +
        "Returns properties, configuration, SKU, tags, and provider-specific metadata. " +
        "Use for deep inspection of individual resources.";

    public ResourceDetailsTool(ILogger<ResourceDetailsTool> logger) : base(logger)
    {
        Parameters.Add(new ToolParameter(
            name: "resourceId",
            description: "Full Azure resource ID (e.g., /subscriptions/.../resourceGroups/.../providers/...)",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "resourceName",
            description: "Resource name to search for (requires subscriptionId)",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "subscriptionId",
            description: "Subscription ID to search in (required if using resourceName)",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "resourceGroup",
            description: "Resource group to narrow search (optional, used with resourceName)",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var resourceId = GetOptionalString(arguments, "resourceId");
        var resourceName = GetOptionalString(arguments, "resourceName");
        var subscriptionId = GetOptionalString(arguments, "subscriptionId");
        var resourceGroup = GetOptionalString(arguments, "resourceGroup");

        Logger.LogInformation("Getting resource details for {ResourceId} / {ResourceName}", 
            resourceId, resourceName);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(resourceId) && string.IsNullOrWhiteSpace(resourceName))
        {
            return ToJson(new { success = false, error = "Either resourceId or resourceName is required" });
        }

        if (!string.IsNullOrWhiteSpace(resourceName) && string.IsNullOrWhiteSpace(subscriptionId))
        {
            return ToJson(new { success = false, error = "subscriptionId is required when using resourceName" });
        }

        try
        {
            // TODO: Integrate with Azure Resource Graph or ARM API for real resource details
            Logger.LogWarning("Resource details lookup requires Azure service integration. Returning sample data.");

            await Task.CompletedTask; // Satisfy async requirement

            return ToJson(new
            {
                success = true,
                resource = new
                {
                    id = resourceId ?? $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup ?? "rg-sample"}/providers/Microsoft.Compute/virtualMachines/{resourceName}",
                    name = resourceName ?? "sample-vm",
                    type = "Microsoft.Compute/virtualMachines",
                    location = "eastus",
                    resourceGroup = resourceGroup ?? "rg-sample",
                    tags = new Dictionary<string, string>
                    {
                        ["Environment"] = "Development",
                        ["Owner"] = "Platform Team"
                    },
                    properties = new
                    {
                        provisioningState = "Succeeded",
                        vmId = Guid.NewGuid().ToString(),
                        hardwareProfile = new { vmSize = "Standard_D2s_v3" },
                        storageProfile = new
                        {
                            osDisk = new { osType = "Linux", diskSizeGB = 128 }
                        },
                        networkProfile = new
                        {
                            networkInterfaces = new[]
                            {
                                new { id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Network/networkInterfaces/nic-sample" }
                            }
                        }
                    },
                    sku = new { name = "Standard_D2s_v3", tier = "Standard" },
                    kind = (string?)null
                },
                dataSource = "Sample"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting resource details");
            return ToJson(new { success = false, error = ex.Message });
        }
    }
}
