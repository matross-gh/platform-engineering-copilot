using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Services.Compliance;

/// <summary>
/// Service for executing Azure ARM-based remediation operations.
/// Eliminates duplicate ARM update patterns across remediation methods.
/// </summary>
public class AzureArmRemediationService : IAzureArmRemediationService
{
    private readonly IAzureResourceService _resourceService;
    private readonly ILogger<AzureArmRemediationService> _logger;

    public AzureArmRemediationService(
        IAzureResourceService resourceService,
        ILogger<AzureArmRemediationService> logger)
    {
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> UpdateResourcePropertiesAsync(
        string resourceId,
        Func<Dictionary<string, object>, Dictionary<string, object>> propertyUpdater,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating properties for resource {ResourceId}", resourceId);

        try
        {
            var armClient = _resourceService.GetArmClient();
            var resourceIdentifier = ResourceIdentifier.Parse(resourceId!);
            var resource = armClient?.GetGenericResource(resourceIdentifier);

            // Get current resource data
            var response = await resource.GetAsync(cancellationToken);
            var resourceData = response.Value.Data;

            // Extract and update properties
            var properties = resourceData.Properties.ToObjectFromJson<Dictionary<string, object>>();
            var updatedProperties = propertyUpdater(properties);

            // Create update payload
            var updateData = new GenericResourceData(resourceData.Location)
            {
                Properties = BinaryData.FromObjectAsJson(updatedProperties)
            };

            // Apply the update
            await resource.UpdateAsync(WaitUntil.Completed, updateData, cancellationToken);

            _logger.LogInformation("Successfully updated properties on {ResourceId}", resourceId);
            return $"Updated properties on {resourceIdentifier.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update properties on {ResourceId}", resourceId);
            throw new InvalidOperationException($"Failed to update resource properties: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateDiagnosticSettingsAsync(
        string resourceId,
        DiagnosticSettingsConfig config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enabling diagnostic settings for resource {ResourceId}", resourceId);

        try
        {
            var armClient = _resourceService.GetArmClient();
            var resourceIdentifier = ResourceIdentifier.Parse(resourceId!);

            // Construct diagnostic settings resource ID
            var diagnosticSettingsId = $"{resourceId}/providers/Microsoft.Insights/diagnosticSettings/compliance-diagnostics";
            var diagnosticIdentifier = new ResourceIdentifier(diagnosticSettingsId!);

            // Create diagnostic settings payload
            var diagnosticSettings = new
            {
                properties = new
                {
                    workspaceId = config.WorkspaceId ?? "/subscriptions/default/resourceGroups/default/providers/Microsoft.OperationalInsights/workspaces/default",
                    logs = config.Logs.Select(l => new
                    {
                        category = l.Category,
                        enabled = l.Enabled,
                        retentionPolicy = new { enabled = true, days = l.RetentionDays }
                    }).ToArray(),
                    metrics = config.Metrics.Select(m => new
                    {
                        category = m.Category,
                        enabled = m.Enabled,
                        retentionPolicy = new { enabled = true, days = m.RetentionDays }
                    }).ToArray()
                }
            };

            // Apply diagnostic settings using ARM REST API
            var genericResource = armClient?.GetGenericResource(diagnosticIdentifier);
            var data = new GenericResourceData(AzureLocation.EastUS)
            {
                Properties = BinaryData.FromObjectAsJson(diagnosticSettings.properties)
            };

            await genericResource.UpdateAsync(WaitUntil.Completed, data, cancellationToken);

            _logger.LogInformation("Successfully enabled diagnostic settings on {ResourceId}", resourceId);
            return $"Enabled diagnostic settings on {resourceIdentifier.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable diagnostic settings on {ResourceId}", resourceId);
            throw new InvalidOperationException($"Failed to enable diagnostic settings: {ex.Message}", ex);
        }
    }

    public async Task<string> CreateAlertRuleAsync(
        string resourceId,
        AlertRuleConfig config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring alert rules for resource {ResourceId}", resourceId);

        try
        {
            var armClient = _resourceService.GetArmClient();
            var resourceIdentifier = ResourceIdentifier.Parse(resourceId!);

            // Create scheduled query rule for audit monitoring
            var alertRuleId = $"{resourceIdentifier.Parent}/providers/Microsoft.Insights/scheduledQueryRules/audit-alert-rule";
            var alertIdentifier = new ResourceIdentifier(alertRuleId!);

            var alertRulePayload = new
            {
                location = "global",
                properties = new
                {
                    displayName = config.DisplayName,
                    description = config.Description,
                    enabled = true,
                    severity = config.Severity,
                    evaluationFrequency = config.EvaluationFrequency,
                    windowSize = config.WindowSize,
                    scopes = new[] { config.WorkspaceId ?? "/subscriptions/default/resourceGroups/default/providers/Microsoft.OperationalInsights/workspaces/default" },
                    criteria = new
                    {
                        allOf = new[]
                        {
                            new
                            {
                                query = config.Query,
                                timeAggregation = "Count",
                                threshold = 10,
                                @operator = "GreaterThan"
                            }
                        }
                    },
                    actions = new { }
                }
            };

            var genericResource = armClient?.GetGenericResource(alertIdentifier);
            var data = new GenericResourceData(AzureLocation.EastUS)
            {
                Properties = BinaryData.FromObjectAsJson(alertRulePayload.properties)
            };

            await genericResource.UpdateAsync(WaitUntil.Completed, data, cancellationToken);

            _logger.LogInformation("Successfully configured alert rules on {ResourceId}", resourceId);
            return $"Configured audit alert rules on {resourceIdentifier.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure alert rules on {ResourceId}", resourceId);
            throw new InvalidOperationException($"Failed to configure alert rules: {ex.Message}", ex);
        }
    }

    public async Task<string> ApplyPolicyAssignmentAsync(
        string subscriptionId,
        string resourceId,
        string? policyDefinitionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying policy assignment to {ResourceId}", resourceId);

        try
        {
            var armClient = _resourceService.GetArmClient();
            var resourceIdentifier = ResourceIdentifier.Parse(resourceId!);

            // Create policy assignment ID
            var assignmentName = $"compliance-policy-{Guid.NewGuid().ToString()[..8]}";
            var assignmentId = $"{resourceId}/providers/Microsoft.Authorization/policyAssignments/{assignmentName}";
            var assignmentIdentifier = new ResourceIdentifier(assignmentId!);

            // Use default policy definition if not provided
            var policyId = policyDefinitionId ??
                $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/policyDefinitions/audit-vm-managed-disks";

            var policyAssignmentPayload = new
            {
                properties = new
                {
                    displayName = $"Compliance Policy Assignment - {assignmentName}",
                    description = "Automated policy assignment for compliance remediation",
                    policyDefinitionId = policyId,
                    scope = resourceId,
                    enforcementMode = "Default",
                    parameters = new { }
                }
            };

            var genericResource = armClient?.GetGenericResource(assignmentIdentifier);
            var data = new GenericResourceData(resourceIdentifier.Location ?? AzureLocation.EastUS)
            {
                Properties = BinaryData.FromObjectAsJson(policyAssignmentPayload.properties)
            };

            await genericResource.UpdateAsync(WaitUntil.Completed, data, cancellationToken);

            _logger.LogInformation("Successfully applied policy assignment to {ResourceId}", resourceId);
            return $"Applied policy assignment to {resourceIdentifier.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply policy assignment to {ResourceId}", resourceId);
            throw new InvalidOperationException($"Failed to apply policy: {ex.Message}", ex);
        }
    }
}
