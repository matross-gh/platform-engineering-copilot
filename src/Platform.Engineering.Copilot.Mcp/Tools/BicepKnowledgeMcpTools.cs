using System.ComponentModel;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

namespace Platform.Engineering.Copilot.Mcp.Tools;

/// <summary>
/// MCP tools for reading a GitHub-hosted Bicep module registry as a knowledge source.
/// Wraps Agent Framework tools for exposure via the MCP protocol (Blazor chat client, GitHub Copilot, etc.)
/// This exists because the module registry's ACR OCI artifacts cannot be introspected for metadata directly -
/// GitHub is the source of truth for the module source/parameters.
/// </summary>
public class BicepKnowledgeMcpTools
{
    private readonly BicepModuleListTool _bicepModuleListTool;
    private readonly BicepModuleSourceTool _bicepModuleSourceTool;
    private readonly BicepModuleExplainerTool _bicepModuleExplainerTool;
    private readonly BicepModuleDeployTool _bicepModuleDeployTool;

    public BicepKnowledgeMcpTools(
        BicepModuleListTool bicepModuleListTool,
        BicepModuleSourceTool bicepModuleSourceTool,
        BicepModuleExplainerTool bicepModuleExplainerTool,
        BicepModuleDeployTool bicepModuleDeployTool)
    {
        _bicepModuleListTool = bicepModuleListTool;
        _bicepModuleSourceTool = bicepModuleSourceTool;
        _bicepModuleExplainerTool = bicepModuleExplainerTool;
        _bicepModuleDeployTool = bicepModuleDeployTool;
    }

    /// <summary>
    /// List folders/modules under a path in the Bicep module registry's GitHub repository
    /// </summary>
    [Description("List the folders and Bicep module files under a path in the GitHub repository backing an ACR Bicep module registry. Use to browse the module tree by resource provider.")]
    public async Task<string> ListBicepModulesAsync(
        string? path = null,
        string? owner = null,
        string? repo = null,
        string? gitRef = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = path,
            ["owner"] = owner,
            ["repo"] = repo,
            ["ref"] = gitRef
        };
        return await _bicepModuleListTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Fetch the raw source of a specific Bicep module file
    /// </summary>
    [Description("Fetch the raw .bicep source of a specific module from the GitHub repository backing an ACR Bicep module registry.")]
    public async Task<string> GetBicepModuleSourceAsync(
        string path,
        string? owner = null,
        string? repo = null,
        string? gitRef = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = path,
            ["owner"] = owner,
            ["repo"] = repo,
            ["ref"] = gitRef
        };
        return await _bicepModuleSourceTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Explain how to use a Bicep module (purpose, parameters, outputs)
    /// </summary>
    [Description("Explain how to use a Bicep module hosted in the GitHub repository backing an ACR Bicep module registry - its purpose, parameters (with descriptions, types, defaults, allowed values), and outputs.")]
    public async Task<string> ExplainBicepModuleAsync(
        string path,
        string? owner = null,
        string? repo = null,
        string? gitRef = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = path,
            ["owner"] = owner,
            ["repo"] = repo,
            ["ref"] = gitRef
        };
        return await _bicepModuleExplainerTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Deploy a Bicep module from the ACR registry into an Azure subscription/resource group
    /// </summary>
    [Description("Deploy a Bicep module from the GitHub-backed ACR module registry into an Azure subscription/resource group (any subscription in the same tenant). Defaults to a What-If preview - pass whatIf=false to actually deploy. Accepts parameter values conversationally and/or a pre-built ARM parameters file.")]
    public async Task<string> DeployBicepModuleAsync(
        string path,
        string subscriptionId,
        string resourceGroupName,
        string? owner = null,
        string? repo = null,
        string? gitRef = null,
        string? deploymentName = null,
        Dictionary<string, object?>? parameters = null,
        string? parametersJson = null,
        bool whatIf = true,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = path,
            ["subscriptionId"] = subscriptionId,
            ["resourceGroupName"] = resourceGroupName,
            ["owner"] = owner,
            ["repo"] = repo,
            ["ref"] = gitRef,
            ["deploymentName"] = deploymentName,
            ["parameters"] = parameters,
            ["parametersJson"] = parametersJson,
            ["whatIf"] = whatIf
        };
        return await _bicepModuleDeployTool.ExecuteAsync(args, cancellationToken);
    }
}
