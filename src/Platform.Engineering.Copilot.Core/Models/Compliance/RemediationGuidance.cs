namespace Platform.Engineering.Copilot.Core.Models.Compliance;

/// <summary>
/// Natural language remediation guidance from GPT-4
/// </summary>
public class RemediationGuidance
{
    public required string FindingId { get; set; }
    public required string Explanation { get; set; }
    
    /// <summary>
    /// Technical remediation plan from ATORemediationEngine
    /// AI translates this into user-friendly guidance
    /// </summary>
    public RemediationPlan? TechnicalPlan { get; set; }
    
    public double Confidence { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}
