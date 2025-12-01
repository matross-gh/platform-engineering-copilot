namespace Platform.Engineering.Copilot.Core.Models.Compliance;

/// <summary>
/// Represents an available remediation action from ATORemediationEngine
/// </summary>
public class RemediationAction
{
    public required string Action { get; set; }
    public required string Description { get; set; }
    public required string Risk { get; set; } // Low, Medium, High
    public List<string> Prerequisites { get; set; } = new();
    public int EstimatedMinutes { get; set; }
}
