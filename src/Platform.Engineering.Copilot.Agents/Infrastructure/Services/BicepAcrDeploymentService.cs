using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Deployment;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.Services;

/// <summary>
/// Deploys ACR-hosted Bicep modules by shelling out to the Azure CLI ("az deployment group
/// create"/"--what-if"), which is what actually resolves and pulls a "br:" OCI module reference
/// at deployment time. The Azure Resource Manager SDK cannot deploy a bare module reference -
/// it needs a compiled ARM template - so a thin wrapper Bicep file (declaring the module's
/// parameters and passing them through) is generated per-deployment and compiled by the CLI.
/// Targets whatever subscription/resource group is requested, as long as the credential the
/// "az" CLI is logged in as has access to it (any subscription in the same tenant).
/// </summary>
public class BicepAcrDeploymentService : IBicepAcrDeploymentService
{
    private readonly ILogger<BicepAcrDeploymentService> _logger;

    private static readonly Regex SubscriptionIdRegex = new(
        @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
    private static readonly Regex ResourceGroupNameRegex = new(@"^[-\w\.\(\)]{1,90}$", RegexOptions.Compiled);
    private static readonly Regex DeploymentNameRegex = new(@"^[-\w\.\(\)]{1,64}$", RegexOptions.Compiled);
    private static readonly Regex BicepIdentifierRegex = new(@"^[A-Za-z_]\w*$", RegexOptions.Compiled);
    private static readonly Regex BicepTypeRegex = new(@"^[\w<>\[\]\.]+$", RegexOptions.Compiled);

    public BicepAcrDeploymentService(ILogger<BicepAcrDeploymentService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<BicepAcrDeploymentResult> PreviewAsync(BicepAcrDeploymentRequest request, CancellationToken cancellationToken = default)
        => RunAsync(request, whatIf: true, cancellationToken);

    public Task<BicepAcrDeploymentResult> DeployAsync(BicepAcrDeploymentRequest request, CancellationToken cancellationToken = default)
        => RunAsync(request, whatIf: false, cancellationToken);

    private async Task<BicepAcrDeploymentResult> RunAsync(BicepAcrDeploymentRequest request, bool whatIf, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError != null)
        {
            return new BicepAcrDeploymentResult { Success = false, WhatIf = whatIf, Message = validationError };
        }

        var deploymentName = request.DeploymentName ?? $"bicep-deploy-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var wrapperTemplate = BuildWrapperTemplate(request.AcrModuleReference, deploymentName, request.ModuleParameters);
        var parametersJson = request.ParametersJson ?? BuildParametersJson(request.ParameterValues);

        var tempDir = Path.Combine(Path.GetTempPath(), $"bicep-acr-deploy-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var templatePath = Path.Combine(tempDir, "wrapper.bicep");
        var parametersPath = Path.Combine(tempDir, "params.json");

        try
        {
            await File.WriteAllTextAsync(templatePath, wrapperTemplate, cancellationToken);
            await File.WriteAllTextAsync(parametersPath, parametersJson, cancellationToken);

            var argumentList = new List<string>
            {
                "deployment", "group", "create",
                "--only-show-errors",
                "--output", "json",
                "--subscription", request.SubscriptionId,
                "--resource-group", request.ResourceGroupName,
                "--name", deploymentName,
                "--template-file", templatePath,
                "--parameters", $"@{parametersPath}"
            };
            if (whatIf)
            {
                argumentList.Add("--what-if");
            }

            var (exitCode, output, error) = await ExecuteProcessAsync("az", argumentList, request.TimeoutSeconds, cancellationToken);

            return new BicepAcrDeploymentResult
            {
                Success = exitCode == 0,
                WhatIf = whatIf,
                DeploymentName = deploymentName,
                ExitCode = exitCode,
                Output = output,
                Error = error,
                GeneratedTemplate = wrapperTemplate,
                Message = exitCode == 0
                    ? whatIf ? "What-If preview completed successfully." : "Deployment completed successfully."
                    : $"{(whatIf ? "What-If preview" : "Deployment")} failed with exit code {exitCode}."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Bicep ACR deployment {DeploymentName}", deploymentName);
            return new BicepAcrDeploymentResult
            {
                Success = false,
                WhatIf = whatIf,
                DeploymentName = deploymentName,
                GeneratedTemplate = wrapperTemplate,
                Message = $"Error running deployment: {ex.Message}"
            };
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temp deployment directory {TempDir}", tempDir);
            }
        }
    }

    private static string? Validate(BicepAcrDeploymentRequest request)
    {
        if (!SubscriptionIdRegex.IsMatch(request.SubscriptionId))
        {
            return $"'{request.SubscriptionId}' is not a valid Azure subscription ID (expected a GUID).";
        }
        if (!ResourceGroupNameRegex.IsMatch(request.ResourceGroupName))
        {
            return $"'{request.ResourceGroupName}' is not a valid Azure resource group name.";
        }
        if (string.IsNullOrWhiteSpace(request.AcrModuleReference) ||
            request.AcrModuleReference.Contains('\'') || request.AcrModuleReference.Contains('\n') || request.AcrModuleReference.Contains('\r'))
        {
            return "The ACR module reference is missing or contains invalid characters.";
        }
        if (request.DeploymentName != null && !DeploymentNameRegex.IsMatch(request.DeploymentName))
        {
            return $"'{request.DeploymentName}' is not a valid Azure deployment name.";
        }
        foreach (var p in request.ModuleParameters)
        {
            if (!BicepIdentifierRegex.IsMatch(p.Name))
            {
                return $"Module parameter name '{p.Name}' is not a valid Bicep identifier.";
            }
            if (!BicepTypeRegex.IsMatch(p.Type))
            {
                return $"Module parameter '{p.Name}' has an unrecognized type '{p.Type}'.";
            }
        }

        return null;
    }

    /// <summary>
    /// Generates a thin resource-group-scoped Bicep file that declares one top-level parameter
    /// per module parameter and passes them straight through to the ACR module reference.
    /// </summary>
    private static string BuildWrapperTemplate(string acrModuleReference, string deploymentName, IReadOnlyList<BicepModuleParameterSpec> moduleParameters)
    {
        var sb = new StringBuilder();
        sb.AppendLine("targetScope = 'resourceGroup'");
        sb.AppendLine();

        foreach (var p in moduleParameters)
        {
            sb.AppendLine($"param {p.Name} {p.Type}");
        }

        sb.AppendLine();
        sb.AppendLine($"module deployedModule '{acrModuleReference}' = {{");
        sb.AppendLine($"  name: '{deploymentName}'");
        sb.AppendLine("  params: {");
        foreach (var p in moduleParameters)
        {
            sb.AppendLine($"    {p.Name}: {p.Name}");
        }
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("output deployedModuleOutputs object = deployedModule.outputs");

        return sb.ToString();
    }

    /// <summary>
    /// Builds a standard ARM deployment parameters file ("$schema"/"parameters" shape) from
    /// conversationally-supplied values, normalizing any JsonElement-boxed values first.
    /// </summary>
    private static string BuildParametersJson(Dictionary<string, object?> parameterValues)
    {
        var normalized = parameterValues.ToDictionary(kv => kv.Key, kv => new { value = NormalizeValue(kv.Value) });

        // "$schema" isn't a valid C# identifier, so build the wrapper manually.
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("$schema", "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#");
            writer.WriteString("contentVersion", "1.0.0.0");
            writer.WritePropertyName("parameters");
            JsonSerializer.Serialize(writer, normalized);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(e => NormalizeValue(e)).ToList(),
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => NormalizeValue(p.Value)),
                _ => element.ToString()
            };
        }

        return value;
    }

    private async Task<(int ExitCode, string Output, string Error)> ExecuteProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning("Bicep ACR deployment process timed out after {Timeout}s", timeoutSeconds);
            try { process.Kill(true); } catch (Exception ex) { _logger.LogError(ex, "Failed to kill timed-out az process"); }
            throw new TimeoutException($"Deployment timed out after {timeoutSeconds} seconds");
        }

        return (process.ExitCode, outputBuilder.ToString().Trim(), errorBuilder.ToString().Trim());
    }
}
