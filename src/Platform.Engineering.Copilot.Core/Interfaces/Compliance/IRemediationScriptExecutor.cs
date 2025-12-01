using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

/// <summary>
/// Service for executing remediation scripts (Azure CLI, PowerShell, Terraform).
/// Handles script sanitization, execution, timeout, and result capture.
/// </summary>
public interface IRemediationScriptExecutor
{
    /// <summary>
    /// Executes a remediation script with sanitization and timeout protection
    /// </summary>
    /// <param name="script">The script to execute</param>
    /// <param name="options">Execution options (timeout, retry, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Script execution result with output and status</returns>
    Task<ScriptExecutionResult> ExecuteScriptAsync(
        RemediationScript script,
        ScriptExecutionOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a script without executing it (syntax check)
    /// </summary>
    Task<ScriptValidationResult> ValidateScriptAsync(
        RemediationScript script,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for script execution
/// </summary>
public record ScriptExecutionOptions
{
    public int TimeoutSeconds { get; init; } = 300; // 5 minutes
    public int MaxRetryAttempts { get; init; } = 3;
    public bool EnableSanitization { get; init; } = true;
    public bool CaptureOutput { get; init; } = true;
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
}
