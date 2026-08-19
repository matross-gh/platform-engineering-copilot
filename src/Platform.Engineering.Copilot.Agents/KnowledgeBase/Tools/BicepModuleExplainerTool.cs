using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.GitHub;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool that reads a Bicep module's source from GitHub (the source of truth for an ACR OCI
/// Bicep registry whose artifacts cannot be introspected for metadata) and explains its
/// purpose, parameters, and outputs so users know how to correctly consume the module.
/// </summary>
public class BicepModuleExplainerTool : BaseTool
{
    private readonly IGitHubServices _gitHubServices;
    private readonly BicepRegistryOptions _options;

    public override string Name => "explain_bicep_module";

    public override string Description =>
        "🧩 Explain how to use a Bicep module hosted in the GitHub repository backing an ACR Bicep module " +
        "registry - its purpose, required/optional parameters (with descriptions, types, defaults, allowed " +
        "values), and outputs. Use this instead of trying to inspect the published ACR OCI artifact directly.";

    public BicepModuleExplainerTool(
        ILogger<BicepModuleExplainerTool> logger,
        IGitHubServices gitHubServices,
        IOptions<BicepRegistryOptions> options) : base(logger)
    {
        _gitHubServices = gitHubServices ?? throw new ArgumentNullException(nameof(gitHubServices));
        _options = options?.Value ?? new BicepRegistryOptions();

        Parameters.Add(new ToolParameter("path", "Full repository path to the .bicep file (e.g. 'Modules/Microsoft.Storage/storageaccounts.bicep').", true));
        Parameters.Add(new ToolParameter("owner", "GitHub repository owner (user or org). Defaults to the configured Bicep registry repo.", false));
        Parameters.Add(new ToolParameter("repo", "GitHub repository name. Defaults to the configured Bicep registry repo.", false));
        Parameters.Add(new ToolParameter("ref", "Branch, tag, or commit SHA to read from. Defaults to the repository's default branch.", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var path = GetRequiredString(arguments, "path");
        var owner = GetOptionalString(arguments, "owner") ?? _options.DefaultOwner;
        var repo = GetOptionalString(arguments, "repo") ?? _options.DefaultRepo;
        var reference = GetOptionalString(arguments, "ref") ?? _options.DefaultBranch;

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            return ToJson(new
            {
                success = false,
                error = "No Bicep module registry repository configured or provided. Specify 'owner' and 'repo'."
            });
        }

        try
        {
            var file = await _gitHubServices.GetFileAsync(owner, repo, path, reference, cancellationToken);

            if (file == null || string.IsNullOrEmpty(file.Content))
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Could not read '{path}' from {owner}/{repo}. Check the path/ref, or the file may be too large to fetch inline."
                });
            }

            var source = file.Content;
            var moduleInfo = BicepModuleMetadataParser.ParseModuleInfo(source);
            var parameters = BicepModuleMetadataParser.ParseParameters(source);
            var outputs = BicepModuleMetadataParser.ParseOutputs(source);
            var acrModuleReference = BicepModuleMetadataParser.ComputeAcrModuleReference(moduleInfo, path, _options.AcrRegistry);

            return ToJson(new
            {
                success = true,
                owner,
                repo,
                @ref = reference ?? "default branch",
                path,
                htmlUrl = file.HtmlUrl,
                moduleInfo,
                acrModuleReference,
                acrModuleReferenceNote = "Best-effort inference from the module's 'parent'/'version' metadata and file name - verify against your bicepconfig.json module alias before use.",
                parameters,
                outputs
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error explaining Bicep module {Path} from {Owner}/{Repo}", path, owner, repo);
            return ToJson(new
            {
                success = false,
                error = $"Error explaining '{path}' from {owner}/{repo}: {ex.Message}"
            });
        }
    }
}
