using System.Text.Json.Serialization;

namespace Platform.Engineering.Copilot.Core.Models.Compliance;

/// <summary>
/// AI-prioritized compliance finding
/// </summary>
public class PrioritizedFinding
{
    [JsonPropertyName("FindingId")]
    public required string FindingId { get; set; }
    
    [JsonPropertyName("ControlId")]
    public required string ControlId { get; set; }
    
    [JsonPropertyName("Priority")]
    public int Priority { get; set; } // 1 = highest, 5 = lowest
    
    [JsonPropertyName("Reasoning")]
    public required string Reasoning { get; set; }
}
