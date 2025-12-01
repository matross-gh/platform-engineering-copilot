using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services.Compliance;

/// <summary>
/// Service for executing remediation scripts (Azure CLI, PowerShell, Terraform).
/// Handles script sanitization, execution with timeout, and result capture.
/// </summary>
public class RemediationScriptExecutor : IRemediationScriptExecutor
{
    private readonly IScriptSanitizationService? _sanitizationService;
    private readonly ILogger<RemediationScriptExecutor> _logger;

    private const int DefaultTimeoutSeconds = 300; // 5 minutes
    private const int MaxRetryAttempts = 3;

    public RemediationScriptExecutor(
        ILogger<RemediationScriptExecutor> logger,
        IScriptSanitizationService? sanitizationService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sanitizationService = sanitizationService;
    }

    public async Task<ScriptExecutionResult> ExecuteScriptAsync(
        RemediationScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing {ScriptType} script for finding {FindingId}",
            script.ScriptType, script.FindingId);

        var startedAt = DateTimeOffset.UtcNow;
        var warnings = new List<string>();

        try
        {
            // Sanitize script if enabled
            string sanitizedScript = script.Script;
            if (options.EnableSanitization && _sanitizationService != null)
            {
                // Create a minimal finding for validation (RemediationScript doesn't have ResourceId)
                var validationResult = await _sanitizationService.ValidateScriptAsync(
                    script.Script, 
                    script.ScriptType, 
                    new AtoFinding { ResourceId = "" });
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Script validation detected errors: {Errors}",
                        string.Join(", ", validationResult.Errors));
                    
                    return new ScriptExecutionResult
                    {
                        ScriptType = script.ScriptType,
                        Success = false,
                        StartedAt = startedAt,
                        CompletedAt = DateTimeOffset.UtcNow,
                        ExitCode = -1,
                        Error = "Script validation failed: " + string.Join("; ", validationResult.Errors),
                        Message = $"Script validation failed with {validationResult.Errors.Count} error(s)",
                        SanitizationWarnings = validationResult.Errors.ToList()
                    };
                }

                // Apply sanitization to remove dangerous patterns
                sanitizedScript = _sanitizationService.SanitizeScript(script.Script, script.ScriptType);
                warnings.AddRange(validationResult.Warnings);
            }

            // Execute based on script type
            ScriptExecutionResult result = script.ScriptType.ToUpperInvariant() switch
            {
                "AZURECLI" => await ExecuteAzureCliScriptAsync(sanitizedScript, options, cancellationToken),
                "POWERSHELL" => await ExecutePowerShellScriptAsync(sanitizedScript, options, cancellationToken),
                "TERRAFORM" => await ExecuteTerraformScriptAsync(sanitizedScript, options, cancellationToken),
                _ => throw new NotSupportedException($"Script type {script.ScriptType} is not supported")
            };

            // Add sanitization warnings to result
            if (warnings.Any())
            {
                result.SanitizationWarnings = warnings;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script execution failed for {ScriptType}", script.ScriptType);
            
            return new ScriptExecutionResult
            {
                ScriptType = script.ScriptType,
                Success = false,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                ExitCode = -1,
                Error = ex.Message,
                Message = $"Script execution failed: {ex.Message}"
            };
        }
    }

    public async Task<ScriptValidationResult> ValidateScriptAsync(
        RemediationScript script,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var blockedCommands = new List<string>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(script.Script))
        {
            errors.Add("Script content is empty");
        }

        // Sanitization check
        if (_sanitizationService != null)
        {
            var validationResult = await _sanitizationService.ValidateScriptAsync(
                script.Script,
                script.ScriptType,
                new AtoFinding { ResourceId = "" });
            
            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors);
            }

            warnings.AddRange(validationResult.Warnings);
        }

        return new ScriptValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings,
            BlockedCommands = blockedCommands
        };
    }

    private async Task<ScriptExecutionResult> ExecuteAzureCliScriptAsync(
        string script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing Azure CLI script");
        
        var startedAt = DateTimeOffset.UtcNow;
        
        // Save script to temp file
        var scriptPath = Path.Combine(Path.GetTempPath(), $"remediation-{Guid.NewGuid()}.sh");
        await File.WriteAllTextAsync(scriptPath, script, cancellationToken);

        try
        {
            var (exitCode, output, error) = await ExecuteProcessAsync(
                "bash",
                scriptPath,
                options.TimeoutSeconds,
                options.EnvironmentVariables,
                cancellationToken);

            return new ScriptExecutionResult
            {
                ScriptType = "AzureCLI",
                Success = exitCode == 0,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                ExitCode = exitCode,
                Output = output,
                Error = error,
                Message = exitCode == 0 ? "Azure CLI script executed successfully" : $"Azure CLI script failed with exit code {exitCode}"
            };
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }
    }

    private async Task<ScriptExecutionResult> ExecutePowerShellScriptAsync(
        string script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing PowerShell script");
        
        var startedAt = DateTimeOffset.UtcNow;
        
        // Save script to temp file
        var scriptPath = Path.Combine(Path.GetTempPath(), $"remediation-{Guid.NewGuid()}.ps1");
        await File.WriteAllTextAsync(scriptPath, script, cancellationToken);

        try
        {
            var (exitCode, output, error) = await ExecuteProcessAsync(
                "pwsh",
                $"-File \"{scriptPath}\"",
                options.TimeoutSeconds,
                options.EnvironmentVariables,
                cancellationToken);

            return new ScriptExecutionResult
            {
                ScriptType = "PowerShell",
                Success = exitCode == 0,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                ExitCode = exitCode,
                Output = output,
                Error = error,
                Message = exitCode == 0 ? "PowerShell script executed successfully" : $"PowerShell script failed with exit code {exitCode}"
            };
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }
    }

    private async Task<ScriptExecutionResult> ExecuteTerraformScriptAsync(
        string script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing Terraform script");
        
        var startedAt = DateTimeOffset.UtcNow;
        
        // Create temp directory for Terraform files
        var tempDir = Path.Combine(Path.GetTempPath(), $"terraform-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        var scriptPath = Path.Combine(tempDir, "main.tf");
        await File.WriteAllTextAsync(scriptPath, script, cancellationToken);

        try
        {
            // Run terraform init
            var (initExitCode, initOutput, initError) = await ExecuteProcessAsync(
                "terraform",
                "init",
                options.TimeoutSeconds / 2,
                options.EnvironmentVariables,
                cancellationToken,
                tempDir);

            if (initExitCode != 0)
            {
                return new ScriptExecutionResult
                {
                    ScriptType = "Terraform",
                    Success = false,
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    ExitCode = initExitCode,
                    Output = initOutput,
                    Error = initError,
                    Message = "Terraform init failed"
                };
            }

            // Run terraform apply -auto-approve
            var (applyExitCode, applyOutput, applyError) = await ExecuteProcessAsync(
                "terraform",
                "apply -auto-approve",
                options.TimeoutSeconds,
                options.EnvironmentVariables,
                cancellationToken,
                tempDir);

            return new ScriptExecutionResult
            {
                ScriptType = "Terraform",
                Success = applyExitCode == 0,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                ExitCode = applyExitCode,
                Output = $"{initOutput}\n\n{applyOutput}",
                Error = $"{initError}\n{applyError}".Trim(),
                Message = applyExitCode == 0 ? "Terraform applied successfully" : $"Terraform apply failed with exit code {applyExitCode}"
            };
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private async Task<(int ExitCode, string Output, string Error)> ExecuteProcessAsync(
        string fileName,
        string arguments,
        int timeoutSeconds,
        Dictionary<string, string> environmentVariables,
        CancellationToken cancellationToken,
        string? workingDirectory = null)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            }
        };

        // Add environment variables
        foreach (var (key, value) in environmentVariables)
        {
            process.StartInfo.EnvironmentVariables[key] = value;
        }

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

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
            _logger.LogWarning("Process execution timed out after {Timeout} seconds", timeoutSeconds);
            
            try
            {
                process.Kill(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill timed-out process");
            }

            throw new TimeoutException($"Script execution timed out after {timeoutSeconds} seconds");
        }

        var output = outputBuilder.ToString().Trim();
        var error = errorBuilder.ToString().Trim();

        return (process.ExitCode, output, error);
    }
}
