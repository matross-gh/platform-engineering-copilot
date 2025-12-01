namespace Platform.Engineering.Copilot.Core.Models.Compliance;

/// <summary>
/// AI-generated remediation script (Azure CLI, PowerShell, Terraform)
/// </summary>
public class RemediationScript
{
    public required string FindingId { get; set; }
    public required string ControlId { get; set; }
    public required string ScriptType { get; set; } // AzureCLI, PowerShell, Terraform
    public required string Script { get; set; }
    
    /// <summary>
    /// Available remediation actions from ATORemediationEngine
    /// AI uses this to generate the script
    /// </summary>
    public List<RemediationAction> AvailableRemediations { get; set; } = new();
    
    /// <summary>
    /// AI's recommended action from available remediations
    /// </summary>
    public string? RecommendedAction { get; set; }
    
    public DateTimeOffset GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = "AI-GPT4";
    public bool RequiresApproval { get; set; }
}

/// <summary>
/// Result of executing an AI-generated remediation script
/// </summary>
public class ScriptExecutionResult
{
    public required string ScriptType { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? Output { get; set; }
    public List<string> ChangesApplied { get; set; } = new();
    public int ExitCode { get; set; }
    public List<string> SanitizationWarnings { get; set; } = new();
}
