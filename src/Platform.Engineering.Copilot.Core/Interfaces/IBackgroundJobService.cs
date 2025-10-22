using Platform.Engineering.Copilot.Core.Models.Jobs;

namespace Platform.Engineering.Copilot.Core.Interfaces;

/// <summary>
/// Service for managing background jobs that execute long-running operations asynchronously
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Start a new background job
    /// </summary>
    /// <param name="jobType">Type of job (e.g., "IntelligentChat", "Deployment")</param>
    /// <param name="conversationId">Conversation ID this job is associated with</param>
    /// <param name="userId">User ID who initiated the job</param>
    /// <param name="inputMessage">Input message that triggered the job</param>
    /// <param name="inputContext">Additional input context</param>
    /// <param name="workload">The async workload to execute. Receives progress reporter and cancellation token.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created background job</returns>
    Task<BackgroundJob> StartJobAsync(
        string jobType,
        string conversationId,
        string userId,
        string inputMessage,
        Dictionary<string, object>? inputContext,
        Func<IProgress<JobProgressUpdate>, CancellationToken, Task<object>> workload,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a job by its ID
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The job if found, null otherwise</returns>
    Task<BackgroundJob?> GetJobAsync(string jobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all jobs for a conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of jobs for the conversation</returns>
    Task<List<BackgroundJob>> GetJobsByConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a running job
    /// </summary>
    /// <param name="jobId">Job ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if job was cancelled, false if not found or already completed</returns>
    Task<bool> CancelJobAsync(string jobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clean up expired jobs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of jobs cleaned up</returns>
    Task<int> CleanupExpiredJobsAsync(CancellationToken cancellationToken = default);
}
