using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Infrastructure.Configuration;
using Platform.Engineering.Copilot.Agents.Infrastructure.State;
using Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.Tools;

/// <summary>
/// Tool for deleting Azure resource groups and their contents.
/// Uses InfrastructureProvisioningService for actual deletion.
/// </summary>
public class ResourceDeletionTool : BaseTool
{
    private readonly InfrastructureStateAccessors _stateAccessors;
    private readonly InfrastructureAgentOptions _options;
    private readonly IInfrastructureProvisioningService _provisioningService;

    public override string Name => "delete_resource_group";

    public override string Description =>
        "Deletes an Azure resource group and all resources within it. " +
        "This is a destructive operation and cannot be undone.";

    public ResourceDeletionTool(
        ILogger<ResourceDeletionTool> logger,
        InfrastructureStateAccessors stateAccessors,
        IOptions<InfrastructureAgentOptions> options,
        IInfrastructureProvisioningService provisioningService) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new InfrastructureAgentOptions();
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));

        Parameters.Add(new ToolParameter("resource_group_name", "Name of resource group to delete", true));
        Parameters.Add(new ToolParameter("subscription_id", "Azure subscription ID", false));
        Parameters.Add(new ToolParameter("force", "Skip confirmation. Default: false", false));
        Parameters.Add(new ToolParameter("conversation_id", "Conversation ID for state tracking", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name")
                ?? throw new ArgumentException("resource_group_name is required");
            var force = GetOptionalBool(arguments, "force", false);
            var conversationId = GetOptionalString(arguments, "conversation_id") ?? Guid.NewGuid().ToString();

            var subscriptionId = GetOptionalString(arguments, "subscription_id")
                ?? await _stateAccessors.GetCurrentSubscriptionAsync(conversationId, cancellationToken)
                ?? _options.DefaultSubscriptionId;

            if (string.IsNullOrEmpty(subscriptionId))
                return ToJson(new { success = false, error = "Subscription ID is required" });

            Logger.LogWarning("Deleting resource group: {RG}", resourceGroupName);

            // Require confirmation unless force=true
            if (!force && _options.Provisioning.RequireConfirmation)
            {
                return ToJson(new
                {
                    success = false,
                    requiresConfirmation = true,
                    message = $"Deletion of '{resourceGroupName}' requires confirmation. Set force=true to proceed.",
                    warning = "This operation permanently deletes all resources and cannot be undone.",
                    resourceGroup = resourceGroupName,
                    subscriptionId
                });
            }

            // Execute actual deletion via provisioning service
            var deleted = await _provisioningService.DeleteResourceGroupAsync(resourceGroupName, cancellationToken);

            // Track the operation
            await _stateAccessors.TrackInfrastructureOperationAsync(
                conversationId, "delete_resource_group", "resource_group",
                subscriptionId, deleted, DateTime.UtcNow - startTime, cancellationToken);

            if (deleted)
            {
                // Record deletion in history
                var record = new DeploymentRecord
                {
                    DeploymentId = $"delete-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ResourceGroupName = resourceGroupName,
                    TemplateName = "DELETION",
                    ResourceType = "resource_group",
                    Success = true,
                    DeployedAt = DateTime.UtcNow,
                    Duration = DateTime.UtcNow - startTime,
                    Resources = new Dictionary<string, string> { ["action"] = "deleted" }
                };
                await _stateAccessors.AddDeploymentToHistoryAsync(conversationId, record, cancellationToken);

                return ToJson(new
                {
                    success = true,
                    deletion = new
                    {
                        resourceGroup = resourceGroupName,
                        subscriptionId,
                        status = "Deleted",
                        deletedAt = DateTime.UtcNow
                    },
                    message = $"Successfully deleted resource group '{resourceGroupName}' and all its resources"
                });
            }
            else
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Failed to delete resource group '{resourceGroupName}'. It may not exist or you may lack permissions.",
                    resourceGroup = resourceGroupName,
                    subscriptionId
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during resource group deletion");
            return ToJson(new { success = false, error = ex.Message });
        }
    }
}
