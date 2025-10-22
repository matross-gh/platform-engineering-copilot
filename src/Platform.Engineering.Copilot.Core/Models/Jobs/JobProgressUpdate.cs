namespace Platform.Engineering.Copilot.Core.Models.Jobs;

/// <summary>
/// Represents a progress update for a background job
/// </summary>
public class JobProgressUpdate
{
    /// <summary>
    /// The ID of the job being updated
    /// </summary>
    public string JobId { get; set; } = string.Empty;
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }
    
    /// <summary>
    /// Description of the current step being executed
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp of this progress update
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional additional metadata about this step
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
