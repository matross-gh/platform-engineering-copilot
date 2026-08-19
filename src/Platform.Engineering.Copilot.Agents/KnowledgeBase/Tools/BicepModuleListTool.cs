using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.GitHub;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for browsing the folder structure of a GitHub-hosted Bicep module registry
/// (the source of truth behind an ACR OCI Bicep registry, whose artifacts cannot be
/// introspected for metadata directly).
/// </summary>
public class BicepModuleListTool : BaseTool
{
    private readonly IGitHubServices _gitHubServices;
    private readonly BicepRegistryOptions _options;

    public override string Name => "list_bicep_modules";

    public override string Description =>
        "📁 List the folders and Bicep module files under a path in the GitHub repository that is the " +
        "source of truth for an Azure Container Registry (ACR) Bicep module registry. Use this to browse " +
        "the module tree (e.g. by resource provider) before fetching or explaining a specific module.";

    public BicepModuleListTool(
        ILogger<BicepModuleListTool> logger,
        IGitHubServices gitHubServices,
        IOptions<BicepRegistryOptions> options) : base(logger)
    {
        _gitHubServices = gitHubServices ?? throw new ArgumentNullException(nameof(gitHubServices));
        _options = options?.Value ?? new BicepRegistryOptions();

        Parameters.Add(new ToolParameter("path", "Directory path to list within the repository. Defaults to the registry's modules root (e.g. 'Modules').", false));
        Parameters.Add(new ToolParameter("owner", "GitHub repository owner (user or org). Defaults to the configured Bicep registry repo.", false));
        Parameters.Add(new ToolParameter("repo", "GitHub repository name. Defaults to the configured Bicep registry repo.", false));
        Parameters.Add(new ToolParameter("ref", "Branch, tag, or commit SHA to read from. Defaults to the repository's default branch.", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var owner = GetOptionalString(arguments, "owner") ?? _options.DefaultOwner;
        var repo = GetOptionalString(arguments, "repo") ?? _options.DefaultRepo;
        var reference = GetOptionalString(arguments, "ref") ?? _options.DefaultBranch;
        var path = GetOptionalString(arguments, "path") ?? _options.DefaultModulesPath;

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
            var contents = await _gitHubServices.GetDirectoryContentsAsync(owner, repo, path, reference, cancellationToken);

            if (contents == null)
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Could not list '{path}' in {owner}/{repo}. Check the path/ref and repository access."
                });
            }

            var entries = contents
                .OrderBy(c => c.Type == Octokit.ContentType.Dir ? 0 : 1)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => new
                {
                    name = c.Name,
                    path = c.Path,
                    type = c.Type == Octokit.ContentType.Dir ? "directory" : "file",
                    isBicepModule = c.Name.EndsWith(".bicep", StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            return ToJson(new
            {
                success = true,
                owner,
                repo,
                @ref = reference ?? "default branch",
                path,
                entries
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error listing Bicep registry path {Path} in {Owner}/{Repo}", path, owner, repo);
            return ToJson(new
            {
                success = false,
                error = $"Error listing '{path}' in {owner}/{repo}: {ex.Message}"
            });
        }
    }
}
