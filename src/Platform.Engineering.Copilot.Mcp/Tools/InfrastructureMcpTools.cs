using System.ComponentModel;
using Platform.Engineering.Copilot.Agents.Infrastructure.Tools;

namespace Platform.Engineering.Copilot.Mcp.Tools;

/// <summary>
/// MCP tools for infrastructure operations. Wraps Agent Framework infrastructure tools
/// for exposure via the MCP protocol (GitHub Copilot, Claude Desktop, etc.)
/// </summary>
public class InfrastructureMcpTools
{
    private readonly TemplateGenerationTool _templateGenerationTool;
    private readonly ResourceProvisioningTool _resourceProvisioningTool;
    private readonly ResourceDeletionTool _resourceDeletionTool;
    private readonly ScalingAnalysisTool _scalingAnalysisTool;
    private readonly AzureArcTool _azureArcTool;

    public InfrastructureMcpTools(
        TemplateGenerationTool templateGenerationTool,
        ResourceProvisioningTool resourceProvisioningTool,
        ResourceDeletionTool resourceDeletionTool,
        ScalingAnalysisTool scalingAnalysisTool,
        AzureArcTool azureArcTool)
    {
        _templateGenerationTool = templateGenerationTool;
        _resourceProvisioningTool = resourceProvisioningTool;
        _resourceDeletionTool = resourceDeletionTool;
        _scalingAnalysisTool = scalingAnalysisTool;
        _azureArcTool = azureArcTool;
    }

    /// <summary>
    /// Generate Infrastructure as Code templates
    /// </summary>
    [Description("Generate Infrastructure as Code templates (Bicep, Terraform, ARM) for Azure resources. Includes DoD IL5/IL6 hardening, compliance controls, and best practices.")]
    public async Task<string> GenerateTemplateAsync(
        string resourceType,
        string templateFormat = "bicep",
        string? complianceLevel = null,
        string? location = null,
        string? resourceName = null,
        string? additionalRequirements = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["resource_type"] = resourceType,
            ["template_format"] = templateFormat,
            ["compliance_level"] = complianceLevel,
            ["location"] = location,
            ["resource_name"] = resourceName,
            ["additional_requirements"] = additionalRequirements
        };
        return await _templateGenerationTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Provision Azure resources from templates
    /// </summary>
    [Description("Deploy Azure resources using Infrastructure as Code templates. Supports Bicep, Terraform, and ARM templates with validation.")]
    public async Task<string> ProvisionResourcesAsync(
        string templatePath,
        string subscriptionId,
        string resourceGroup,
        string? location = null,
        Dictionary<string, object>? parameters = null,
        bool validateOnly = false,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["template_path"] = templatePath,
            ["subscription_id"] = subscriptionId,
            ["resource_group"] = resourceGroup,
            ["location"] = location,
            ["parameters"] = parameters,
            ["validate_only"] = validateOnly
        };
        return await _resourceProvisioningTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Delete Azure resources
    /// </summary>
    [Description("Safely delete Azure resources with dependency checking. Supports dry-run mode and force deletion with proper cleanup.")]
    public async Task<string> DeleteResourcesAsync(
        string resourceId,
        bool dryRun = true,
        bool force = false,
        bool deleteAssociatedResources = false,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["resource_id"] = resourceId,
            ["dry_run"] = dryRun,
            ["force"] = force,
            ["delete_associated_resources"] = deleteAssociatedResources
        };
        return await _resourceDeletionTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Analyze scaling options for resources
    /// </summary>
    [Description("Analyze scaling options and recommendations for Azure resources. Provides right-sizing suggestions based on usage patterns.")]
    public async Task<string> AnalyzeScalingAsync(
        string resourceId,
        int analysisWindowDays = 30,
        bool includeRecommendations = true,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["resource_id"] = resourceId,
            ["analysis_window_days"] = analysisWindowDays,
            ["include_recommendations"] = includeRecommendations
        };
        return await _scalingAnalysisTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Manage Azure Arc connected resources
    /// </summary>
    [Description("Manage Azure Arc for hybrid and multi-cloud scenarios. Connect, configure, and monitor on-premises or other cloud resources.")]
    public async Task<string> ManageAzureArcAsync(
        string operation,
        string? resourceId = null,
        string? machineName = null,
        string? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["resource_id"] = resourceId,
            ["machine_name"] = machineName,
            ["subscription_id"] = subscriptionId
        };
        return await _azureArcTool.ExecuteAsync(args, cancellationToken);
    }
}
