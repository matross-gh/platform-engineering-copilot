namespace Platform.Engineering.Copilot.Core.Models.Jobs;

/// <summary>
/// Represents a background job that can execute long-running operations asynchronously
/// </summary>
public class BackgroundJob
{
    /// <summary>
    /// Unique identifier for this job
    /// </summary>
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Conversation ID this job is associated with
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID who initiated this job
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of job (e.g., "IntelligentChat", "Deployment", "ComplianceScan")
    /// </summary>
    public string JobType { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the job
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Queued;
    
    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the job started executing (null if not started)
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// When the job completed or failed (null if still running)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// When the job results will expire and be cleaned up
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Input message that triggered this job
    /// </summary>
    public string InputMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional input context for the job
    /// </summary>
    public Dictionary<string, object> InputContext { get; set; } = new();
    
    /// <summary>
    /// Current progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;
    
    /// <summary>
    /// Description of the current step being executed
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;
    
    /// <summary>
    /// List of completed steps
    /// </summary>
    public List<string> CompletedSteps { get; set; } = new();
    
    /// <summary>
    /// Result of the job (populated when Status is Completed)
    /// </summary>
    public object? Result { get; set; }
    
    /// <summary>
    /// Error message if the job failed
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Additional metadata about the job
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
