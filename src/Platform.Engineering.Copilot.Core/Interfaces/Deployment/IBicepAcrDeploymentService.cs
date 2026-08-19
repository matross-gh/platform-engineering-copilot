namespace Platform.Engineering.Copilot.Core.Interfaces.Deployment;

/// <summary>
/// A Bicep module parameter's name and declared type, used to generate a thin wrapper Bicep
/// file that declares matching top-level parameters and passes them through to the ACR-hosted
/// module (Azure Resource Manager cannot deploy a "br:" module reference directly - it must be
/// wrapped in a template/module file that references it).
/// </summary>
public class BicepModuleParameterSpec
{
    public required string Name { get; set; }
    public required string Type { get; set; }
}

/// <summary>
/// Request to preview or execute a deployment of a single Bicep module published to an ACR OCI
/// registry, into a resource group within a subscription. The target subscription may be any
/// subscription in the same Microsoft Entra tenant that the host's Azure credential can access
/// (not limited to a single fixed subscription).
/// </summary>
public class BicepAcrDeploymentRequest
{
    /// <summary>Target Azure subscription ID. May be any subscription in the same tenant.</summary>
    public required string SubscriptionId { get; set; }

    /// <summary>Target resource group name. Must already exist.</summary>
    public required string ResourceGroupName { get; set; }

    /// <summary>The ACR OCI module reference, e.g. "br:myregistry.azurecr.io/microsoft.storage/storageaccounts:1.0.2".</summary>
    public required string AcrModuleReference { get; set; }

    /// <summary>Name for the ARM deployment. Auto-generated if not provided.</summary>
    public string? DeploymentName { get; set; }

    /// <summary>The module's declared parameters (name + Bicep type), used to build the wrapper template.</summary>
    public IReadOnlyList<BicepModuleParameterSpec> ModuleParameters { get; set; } = Array.Empty<BicepModuleParameterSpec>();

    /// <summary>Parameter values supplied conversationally (e.g. filled in by chat/LLM).</summary>
    public Dictionary<string, object?> ParameterValues { get; set; } = new();

    /// <summary>
    /// Optional pre-built ARM deployment parameters file content (JSON, "$schema"/"parameters" shape).
    /// When provided, takes precedence over <see cref="ParameterValues"/>.
    /// </summary>
    public string? ParametersJson { get; set; }

    public int TimeoutSeconds { get; set; } = 600;
}

/// <summary>Result of a Bicep ACR module deployment preview (What-If) or execution.</summary>
public class BicepAcrDeploymentResult
{
    public bool Success { get; set; }
    public bool WhatIf { get; set; }
    public string? DeploymentName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int ExitCode { get; set; }

    /// <summary>The generated wrapper Bicep template, returned for transparency/debugging.</summary>
    public string? GeneratedTemplate { get; set; }
}

/// <summary>
/// Executes deployments of ACR-hosted Bicep modules via the Azure CLI, targeting any
/// resource group/subscription within the same tenant that the host's Azure credential
/// (az CLI login / managed identity) has access to.
/// </summary>
public interface IBicepAcrDeploymentService
{
    /// <summary>Runs a What-If preview - reports what would change without deploying anything.</summary>
    Task<BicepAcrDeploymentResult> PreviewAsync(BicepAcrDeploymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Executes the deployment for real.</summary>
    Task<BicepAcrDeploymentResult> DeployAsync(BicepAcrDeploymentRequest request, CancellationToken cancellationToken = default);
}
