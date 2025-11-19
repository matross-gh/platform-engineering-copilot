namespace Platform.Engineering.Copilot.Core.Models.Compliance;

/// <summary>
/// Represents a finding from Defender for Cloud
/// </summary>
public class DefenderFinding
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RemediationSteps { get; set; }
    public string? AffectedResource { get; set; }
    public string? AssessmentType { get; set; }
}

/// <summary>
/// Represents Defender for Cloud secure score
/// </summary>
public class DefenderSecureScore
{
    public double CurrentScore { get; set; }
    public double MaxScore { get; set; }
    public double Percentage { get; set; }
    public string SubscriptionId { get; set; } = string.Empty;
}
