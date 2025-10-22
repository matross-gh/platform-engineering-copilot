namespace Platform.Engineering.Copilot.API.Models;

/// <summary>
/// Response when starting an asynchronous job
/// </summary>
public class AsyncJobResponse
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string JobId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the job
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable message about the job
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// URL to poll for job status
    /// </summary>
    public string StatusUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response containing job status and results
/// </summary>
public class JobStatusResponse
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string JobId { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the job
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }
    
    /// <summary>
    /// Description of current step being executed
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;
    
    /// <summary>
    /// List of completed steps
    /// </summary>
    public List<string> CompletedSteps { get; set; } = new();
    
    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the job started executing
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// When the job completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Error message if the job failed
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Result of the job (only included when Status is Completed)
    /// </summary>
    public object? Result { get; set; }
}
