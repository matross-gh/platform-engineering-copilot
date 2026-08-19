using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Core.Interfaces.GitHub;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for fetching the raw source of a single Bicep module file from the GitHub
/// repository that is the source of truth for an ACR Bicep module registry.
/// </summary>
public class BicepModuleSourceTool : BaseTool
{
    private readonly IGitHubServices _gitHubServices;
    private readonly BicepRegistryOptions _options;

    public override string Name => "get_bicep_module_source";

    public override string Description =>
        "📄 Fetch the raw .bicep source of a specific module from the GitHub repository backing an ACR " +
        "Bicep module registry. Use 'list_bicep_modules' first to find the file path.";

    public BicepModuleSourceTool(
        ILogger<BicepModuleSourceTool> logger,
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

            return ToJson(new
            {
                success = true,
                owner,
                repo,
                @ref = reference ?? "default branch",
                path,
                sha = file.Sha,
                htmlUrl = file.HtmlUrl,
                content = file.Content
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching Bicep module source {Path} from {Owner}/{Repo}", path, owner, repo);
            return ToJson(new
            {
                success = false,
                error = $"Error fetching '{path}' from {owner}/{repo}: {ex.Message}"
            });
        }
    }
}
