using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Infrastructure.Configuration;
using Platform.Engineering.Copilot.Agents.Infrastructure.State;
using Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.Tools;

/// <summary>
/// Tool for provisioning Azure infrastructure resources.
/// Uses InfrastructureProvisioningService for actual resource creation.
/// </summary>
public class ResourceProvisioningTool : BaseTool
{
    private readonly InfrastructureStateAccessors _stateAccessors;
    private readonly InfrastructureAgentOptions _options;
    private readonly IInfrastructureProvisioningService _provisioningService;

    public override string Name => "provision_infrastructure";

    public override string Description =>
        "Provisions Azure infrastructure resources using natural language queries. " +
        "Supports storage accounts, key vaults, VNets, NSGs, and more. " +
        "Example: 'Create a storage account named mydata in eastus'";

    public ResourceProvisioningTool(
        ILogger<ResourceProvisioningTool> logger,
        InfrastructureStateAccessors stateAccessors,
        IOptions<InfrastructureAgentOptions> options,
        IInfrastructureProvisioningService provisioningService) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new InfrastructureAgentOptions();
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));

        Parameters.Add(new ToolParameter("query", "Natural language description of infrastructure to provision (e.g., 'Create a storage account named mydata in eastus')", true));
        Parameters.Add(new ToolParameter("resource_type", "Resource type: storage-account, keyvault, vnet, nsg, managed-identity, log-analytics, app-insights", false));
        Parameters.Add(new ToolParameter("resource_name", "Name for the resource", false));
        Parameters.Add(new ToolParameter("resource_group_name", "Resource group name (created if needed)", false));
        Parameters.Add(new ToolParameter("location", "Azure region. Default: eastus", false));
        Parameters.Add(new ToolParameter("subscription_id", "Azure subscription ID", false));
        Parameters.Add(new ToolParameter("estimate_cost", "Only estimate cost without provisioning. Default: false", false));
        Parameters.Add(new ToolParameter("conversation_id", "Conversation ID for state tracking", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        string? conversationId = null;

        try
        {
            // Build the query from parameters
            var query = GetOptionalString(arguments, "query");
            var resourceType = GetOptionalString(arguments, "resource_type");
            var resourceName = GetOptionalString(arguments, "resource_name");
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name");
            var location = GetOptionalString(arguments, "location") ?? _options.DefaultRegion;
            var estimateCost = GetOptionalBool(arguments, "estimate_cost", false);
            
            conversationId = GetOptionalString(arguments, "conversation_id") ?? Guid.NewGuid().ToString();

            var subscriptionId = GetOptionalString(arguments, "subscription_id")
                ?? await _stateAccessors.GetCurrentSubscriptionAsync(conversationId, cancellationToken)
                ?? _options.DefaultSubscriptionId;

            // Build natural language query if not provided directly
            if (string.IsNullOrEmpty(query))
            {
                if (string.IsNullOrEmpty(resourceType))
                {
                    return ToJson(new
                    {
                        success = false,
                        error = "Either 'query' or 'resource_type' is required"
                    });
                }

                // Build query from individual parameters
                var queryParts = new List<string> { $"Create a {resourceType}" };
                if (!string.IsNullOrEmpty(resourceName))
                    queryParts.Add($"named {resourceName}");
                if (!string.IsNullOrEmpty(location))
                    queryParts.Add($"in {location}");
                if (!string.IsNullOrEmpty(resourceGroupName))
                    queryParts.Add($"in resource group {resourceGroupName}");
                
                query = string.Join(" ", queryParts);
            }

            Logger.LogInformation("Provisioning infrastructure: {Query}, EstimateCost={EstimateCost}", query, estimateCost);

            // Cost estimation only
            if (estimateCost)
            {
                var costEstimate = await _provisioningService.EstimateCostAsync(query, cancellationToken);
                
                return ToJson(new
                {
                    success = true,
                    estimateOnly = true,
                    costEstimate = new
                    {
                        resourceType = costEstimate.ResourceType,
                        location = costEstimate.Location,
                        monthlyEstimate = costEstimate.MonthlyEstimate,
                        annualEstimate = costEstimate.AnnualEstimate,
                        currency = costEstimate.Currency,
                        notes = costEstimate.Notes
                    },
                    message = $"Estimated cost for {costEstimate.ResourceType}: ${costEstimate.MonthlyEstimate:F2}/month (${costEstimate.AnnualEstimate:F2}/year)"
                });
            }

            // Track provisioning status
            var status = new ProvisioningStatus
            {
                DeploymentId = $"provision-{DateTime.UtcNow:yyyyMMddHHmmss}",
                ResourceGroupName = resourceGroupName ?? "auto-created",
                ResourceType = resourceType ?? "infrastructure",
                ResourceName = resourceName ?? "resource",
                Status = "InProgress",
                StartedAt = DateTime.UtcNow
            };
            await _stateAccessors.UpdateProvisioningStatusAsync(conversationId, status, cancellationToken);

            // Execute provisioning
            var result = await _provisioningService.ProvisionInfrastructureAsync(query, cancellationToken);

            // Update status based on result
            status.Status = result.Success ? "Succeeded" : "Failed";
            status.CompletedAt = DateTime.UtcNow;
            status.ResourceId = result.ResourceId;
            await _stateAccessors.UpdateProvisioningStatusAsync(conversationId, status, cancellationToken);

            if (result.Success)
            {
                // Record deployment in history
                var record = new DeploymentRecord
                {
                    DeploymentId = status.DeploymentId,
                    ResourceGroupName = resourceGroupName ?? ExtractResourceGroupFromId(result.ResourceId),
                    TemplateName = "natural-language-provision",
                    ResourceType = result.ResourceType,
                    Location = location,
                    Success = true,
                    DeployedAt = DateTime.UtcNow,
                    Duration = DateTime.UtcNow - startTime,
                    Resources = result.Properties ?? new Dictionary<string, string>()
                };
                await _stateAccessors.AddDeploymentToHistoryAsync(conversationId, record, cancellationToken);

                await _stateAccessors.TrackInfrastructureOperationAsync(
                    conversationId, "provision", result.ResourceType,
                    subscriptionId ?? "unknown", true, DateTime.UtcNow - startTime, cancellationToken);

                return ToJson(new
                {
                    success = true,
                    resource = new
                    {
                        id = result.ResourceId,
                        name = result.ResourceName,
                        type = result.ResourceType,
                        status = result.Status,
                        properties = result.Properties,
                        provisionedAt = result.ProvisionedAt
                    },
                    message = result.Message
                });
            }
            else
            {
                await _stateAccessors.TrackInfrastructureOperationAsync(
                    conversationId, "provision", resourceType ?? "unknown",
                    subscriptionId ?? "unknown", false, DateTime.UtcNow - startTime, cancellationToken);

                return ToJson(new
                {
                    success = false,
                    error = result.ErrorDetails ?? result.Message,
                    status = result.Status,
                    message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during infrastructure provisioning");
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private string ExtractResourceGroupFromId(string resourceId)
    {
        // Extract resource group name from resource ID
        // /subscriptions/{sub}/resourceGroups/{rg}/providers/...
        var match = System.Text.RegularExpressions.Regex.Match(
            resourceId, 
            @"/resourceGroups/([^/]+)/", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value : "unknown";
    }
}
