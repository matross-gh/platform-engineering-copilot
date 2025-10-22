namespace Platform.Engineering.Copilot.Core.Models.Jobs;

/// <summary>
/// Represents the status of a background job
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job has been created but not yet started
    /// </summary>
    Queued,
    
    /// <summary>
    /// Job is currently executing
    /// </summary>
    Running,
    
    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Job failed with an error
    /// </summary>
    Failed,
    
    /// <summary>
    /// Job was cancelled by user or system
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Job results have expired and been cleaned up
    /// </summary>
    Expired
}
