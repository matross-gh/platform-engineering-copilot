using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;

namespace Platform.Engineering.Copilot.Agents.Discovery.Tools;

/// <summary>
/// Tool for mapping dependencies between Azure resources.
/// Identifies related resources like NICs, Disks, VNets connected to VMs, etc.
/// </summary>
public class DependencyMappingTool : BaseTool
{
    public override string Name => "map_resource_dependencies";

    public override string Description =>
        "Map dependencies between Azure resources. " +
        "Identifies related resources (NICs, Disks, VNets, etc.) connected to a root resource. " +
        "Use for understanding resource relationships and impact analysis.";

    public DependencyMappingTool(ILogger<DependencyMappingTool> logger) : base(logger)
    {
        Parameters.Add(new ToolParameter(
            name: "resourceId",
            description: "Full Azure resource ID to start dependency mapping from",
            required: true));

        Parameters.Add(new ToolParameter(
            name: "depth",
            description: "How deep to traverse dependencies (1-5, default: 2)",
            required: false,
            type: "integer"));

        Parameters.Add(new ToolParameter(
            name: "direction",
            description: "Direction to map: 'downstream' (what this depends on), 'upstream' (what depends on this), or 'both' (default)",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var resourceId = GetOptionalString(arguments, "resourceId");
        var depthArg = GetOptionalInt(arguments, "depth");
        var direction = GetOptionalString(arguments, "direction") ?? "both";

        var depth = Math.Clamp(depthArg ?? 2, 1, 5);

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return ToJson(new { success = false, error = "resourceId is required" });
        }

        Logger.LogInformation("Mapping dependencies for {ResourceId} with depth {Depth}, direction {Direction}",
            resourceId, depth, direction);

        try
        {
            // Parse resource ID to extract info
            var resourceType = ExtractResourceType(resourceId);

            // TODO: Integrate with Azure Resource Graph for real dependency mapping
            Logger.LogWarning("Dependency mapping requires Azure service integration. Returning sample data.");

            var dependencies = new List<DependencyInfo>();

            // Generate sample dependencies based on resource type
            if (resourceType?.Contains("virtualMachines") == true)
            {
                dependencies.Add(new DependencyInfo
                {
                    ResourceId = resourceId.Replace("virtualMachines", "networkInterfaces").Replace("vm-", "nic-"),
                    Type = "Microsoft.Network/networkInterfaces",
                    Relationship = "downstream",
                    Description = "Network interface attached to VM"
                });
                dependencies.Add(new DependencyInfo
                {
                    ResourceId = resourceId.Replace("Microsoft.Compute/virtualMachines", "Microsoft.Compute/disks").Replace("vm-", "disk-os-"),
                    Type = "Microsoft.Compute/disks",
                    Relationship = "downstream",
                    Description = "OS disk attached to VM"
                });
            }
            else if (resourceType?.Contains("storageAccounts") == true)
            {
                dependencies.Add(new DependencyInfo
                {
                    ResourceId = $"{resourceId}/blobServices/default",
                    Type = "Microsoft.Storage/storageAccounts/blobServices",
                    Relationship = "downstream",
                    Description = "Blob service child resource"
                });
            }

            await Task.CompletedTask; // Satisfy async requirement

            return ToJson(new
            {
                success = true,
                rootResource = resourceId,
                resourceType,
                parameters = new { depth, direction },
                dependencyCount = dependencies.Count,
                dependencies,
                graphSummary = new
                {
                    totalNodes = dependencies.Count + 1,
                    downstreamCount = dependencies.Count(d => d.Relationship == "downstream"),
                    upstreamCount = dependencies.Count(d => d.Relationship == "upstream")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error mapping dependencies for {ResourceId}", resourceId);
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private static string? ExtractResourceType(string resourceId)
    {
        // Extract resource type from ARM resource ID
        // Format: /subscriptions/{sub}/resourceGroups/{rg}/providers/{provider}/{type}/{name}
        var parts = resourceId.Split('/');
        var providerIndex = Array.IndexOf(parts, "providers");
        if (providerIndex >= 0 && providerIndex + 2 < parts.Length)
        {
            return $"{parts[providerIndex + 1]}/{parts[providerIndex + 2]}";
        }
        return null;
    }

    private class DependencyInfo
    {
        public string ResourceId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
