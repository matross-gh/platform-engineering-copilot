using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.Deployment;
using Platform.Engineering.Copilot.Core.Interfaces.GitHub;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool that deploys a Bicep module published to the ACR OCI registry backing the GitHub
/// module registry. Reads the module's source from GitHub to derive its ACR reference and
/// parameter signature (the same source of truth used by <see cref="BicepModuleExplainerTool"/>),
/// then deploys it into the requested subscription/resource group. Any subscription in the
/// same Microsoft Entra tenant is supported - deployment is not limited to a single fixed
/// subscription. Defaults to a What-If preview; the caller must explicitly opt in to a real
/// deployment via the "whatIf": false argument.
/// </summary>
public class BicepModuleDeployTool : BaseTool
{
    private readonly IGitHubServices _gitHubServices;
    private readonly IBicepAcrDeploymentService _deploymentService;
    private readonly BicepRegistryOptions _options;

    public override string Name => "deploy_bicep_module";

    public override string Description =>
        "🚀 Deploy a Bicep module from the GitHub-backed ACR module registry into an Azure subscription/resource " +
        "group (any subscription in the same tenant the host is authorized for). Defaults to a What-If preview - " +
        "pass \"whatIf\": false to actually deploy. Accepts parameter values conversationally (\"parameters\") " +
        "and/or a pre-built ARM parameters file (\"parametersJson\").";

    public BicepModuleDeployTool(
        ILogger<BicepModuleDeployTool> logger,
        IGitHubServices gitHubServices,
        IBicepAcrDeploymentService deploymentService,
        IOptions<BicepRegistryOptions> options) : base(logger)
    {
        _gitHubServices = gitHubServices ?? throw new ArgumentNullException(nameof(gitHubServices));
        _deploymentService = deploymentService ?? throw new ArgumentNullException(nameof(deploymentService));
        _options = options?.Value ?? new BicepRegistryOptions();

        Parameters.Add(new ToolParameter("path", "Full repository path to the module's .bicep file (e.g. 'Modules/Microsoft.Storage/storageaccounts.bicep').", true));
        Parameters.Add(new ToolParameter("subscriptionId", "Target Azure subscription ID (GUID). May be any subscription in the same tenant.", true));
        Parameters.Add(new ToolParameter("resourceGroupName", "Target resource group name. Must already exist.", true));
        Parameters.Add(new ToolParameter("owner", "GitHub repository owner (user or org). Defaults to the configured Bicep registry repo.", false));
        Parameters.Add(new ToolParameter("repo", "GitHub repository name. Defaults to the configured Bicep registry repo.", false));
        Parameters.Add(new ToolParameter("ref", "Branch, tag, or commit SHA to read the module from. Defaults to the repository's default branch.", false));
        Parameters.Add(new ToolParameter("deploymentName", "Name for the ARM deployment. Auto-generated if not provided.", false));
        Parameters.Add(new ToolParameter("parameters", "Object of module parameter values to deploy with (conversationally supplied).", false));
        Parameters.Add(new ToolParameter("parametersJson", "Pre-built ARM deployment parameters file content (JSON). Takes precedence over 'parameters' if both are given.", false));
        Parameters.Add(new ToolParameter("whatIf", "When true (default), only preview the change (What-If) without deploying. Set to false to actually deploy.", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var path = GetRequiredString(arguments, "path");
        var subscriptionId = GetRequiredString(arguments, "subscriptionId");
        var resourceGroupName = GetRequiredString(arguments, "resourceGroupName");
        var owner = GetOptionalString(arguments, "owner") ?? _options.DefaultOwner;
        var repo = GetOptionalString(arguments, "repo") ?? _options.DefaultRepo;
        var reference = GetOptionalString(arguments, "ref") ?? _options.DefaultBranch;
        var deploymentName = GetOptionalString(arguments, "deploymentName");
        var parametersJson = GetOptionalString(arguments, "parametersJson");
        var whatIf = GetOptionalBool(arguments, "whatIf", defaultValue: true);

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            return ToJson(new { success = false, error = "No Bicep module registry repository configured or provided. Specify 'owner' and 'repo'." });
        }

        try
        {
            var file = await _gitHubServices.GetFileAsync(owner, repo, path, reference, cancellationToken);
            if (file == null || string.IsNullOrEmpty(file.Content))
            {
                return ToJson(new { success = false, error = $"Could not read '{path}' from {owner}/{repo}. Check the path/ref." });
            }

            var moduleInfo = BicepModuleMetadataParser.ParseModuleInfo(file.Content);
            var acrModuleReference = BicepModuleMetadataParser.ComputeAcrModuleReference(moduleInfo, path, _options.AcrRegistry);
            if (string.IsNullOrEmpty(acrModuleReference) || !acrModuleReference.StartsWith("br:", StringComparison.Ordinal))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Could not derive an ACR module reference for this module (missing 'parent'/'version' metadata, or no AcrRegistry configured)."
                });
            }

            var moduleParameters = BicepModuleMetadataParser.ParseParameters(file.Content)
                .Select(p => new BicepModuleParameterSpec { Name = p.Name, Type = p.Type })
                .ToList();

            var parameterValues = ParseParametersArgument(arguments);

            var request = new BicepAcrDeploymentRequest
            {
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                AcrModuleReference = acrModuleReference,
                DeploymentName = deploymentName,
                ModuleParameters = moduleParameters,
                ParameterValues = parameterValues,
                ParametersJson = parametersJson
            };

            var result = whatIf
                ? await _deploymentService.PreviewAsync(request, cancellationToken)
                : await _deploymentService.DeployAsync(request, cancellationToken);

            return ToJson(new
            {
                success = result.Success,
                whatIf = result.WhatIf,
                deploymentName = result.DeploymentName,
                acrModuleReference,
                message = result.Message,
                output = result.Output,
                error = result.Error,
                exitCode = result.ExitCode,
                generatedTemplate = result.GeneratedTemplate
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deploying Bicep module {Path} from {Owner}/{Repo}", path, owner, repo);
            return ToJson(new { success = false, error = $"Error deploying '{path}' from {owner}/{repo}: {ex.Message}" });
        }
    }

    private static Dictionary<string, object?> ParseParametersArgument(IDictionary<string, object?> arguments)
    {
        if (!arguments.TryGetValue("parameters", out var raw) || raw == null)
        {
            return new Dictionary<string, object?>();
        }

        if (raw is Dictionary<string, object?> typed)
        {
            return typed;
        }

        if (raw is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            return element.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value);
        }

        if (raw is IDictionary<string, object?> untyped)
        {
            return new Dictionary<string, object?>(untyped);
        }

        return new Dictionary<string, object?>();
    }
}
