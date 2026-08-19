namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;

/// <summary>
/// Configuration for the GitHub-backed Bicep module registry used as a knowledge
/// source (the registry's OCI artifacts in ACR cannot be introspected for metadata,
/// so module source/parameters are read from the GitHub repo that is their source of truth).
/// </summary>
public class BicepRegistryOptions
{
    public const string SectionName = "AgentConfiguration:KnowledgeBaseAgent:BicepRegistry";

    /// <summary>
    /// Default GitHub repository owner (user or org) hosting the Bicep module source.
    /// Can be overridden per-request.
    /// </summary>
    public string? DefaultOwner { get; set; }

    /// <summary>
    /// Default GitHub repository name hosting the Bicep module source.
    /// Can be overridden per-request.
    /// </summary>
    public string? DefaultRepo { get; set; }

    /// <summary>
    /// Default branch, tag, or commit reference to read from. Falls back to the repo's default branch when null.
    /// </summary>
    public string? DefaultBranch { get; set; }

    /// <summary>
    /// Default root path within the repository where Bicep modules live.
    /// </summary>
    public string DefaultModulesPath { get; set; } = "Modules";

    /// <summary>
    /// ACR login server (or bicepconfig.json module alias registry) the modules are published to,
    /// used only to construct an informational "br:" module reference in explanations.
    /// </summary>
    public string? AcrRegistry { get; set; }
}
