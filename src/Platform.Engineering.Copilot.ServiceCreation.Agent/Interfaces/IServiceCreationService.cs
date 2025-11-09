using Platform.Engineering.Copilot.Core.Models.ServiceCreation;
using Platform.Engineering.Copilot.Core.Data.Entities;
using ServiceCreationRequest = Platform.Engineering.Copilot.Data.Entities.ServiceCreationRequest;
using ServiceCreationValidationResult = Platform.Engineering.Copilot.Core.Models.ServiceCreation.ServiceCreationPhase;
using ServiceCreationWorkflowConfig = Platform.Engineering.Copilot.Core.Models.ServiceCreation.ServiceCreationPhase;

namespace Platform.Engineering.Copilot.ServiceCreation.Core.Interfaces;

/// <summary>
/// Service interface for managing Navy Flankspeed mission owner service creation requests
/// </summary>
public interface IServiceCreationService
{
    #region Request Management
    
    /// <summary>
    /// Creates a new draft service creation request
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created request ID</returns>
    Task<string> CreateDraftRequestAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a draft service creation request with new data
    /// </summary>
    /// <param name="requestId">The request ID</param>
    /// <param name="updates">Dictionary of field updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateDraftAsync(string requestId, object updates, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Submits a draft request for NNWC review
    /// </summary>
    /// <param name="requestId">The request ID</param>
    /// <param name="submittedBy">Email of the user submitting the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> SubmitRequestAsync(string requestId, string? submittedBy = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a request for submission and returns any validation errors
    /// </summary>
    /// <param name="requestId">The request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    Task<List<string>> ValidateForSubmissionAsync(string requestId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific service creation request by ID
    /// </summary>
    /// <param name="requestId">The request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The service creation request or null if not found</returns>
    Task<ServiceCreationRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pending service creation requests awaiting NNWC review
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending requests</returns>
    Task<List<ServiceCreationRequest>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all service creation requests for a specific mission owner
    /// </summary>
    /// <param name="email">Mission owner email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of requests</returns>
    Task<List<ServiceCreationRequest>> GetRequestsByOwnerAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels a pending request
    /// </summary>
    /// <param name="requestId">The request ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> CancelRequestAsync(string requestId, string reason, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Approval Workflow
    
    /// <summary>
    /// Approves an service creation request and triggers provisioning
    /// </summary>
    /// <param name="requestId">The request ID</param>
    /// <param name="approvedBy">Name/ID of approver</param>
    /// <param name="comments">Optional approval comments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provisioning result with job ID</returns>
    Task<ProvisioningResult> ApproveRequestAsync(
        string requestId, 
        string approvedBy, 
        string? comments = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rejects an service creation request
    /// </summary>
    /// <param name="requestId">The request ID</param>
    /// <param name="rejectedBy">Name/ID of person rejecting</param>
    /// <param name="reason">Rejection reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> RejectRequestAsync(
        string requestId, 
        string rejectedBy, 
        string reason, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Provisioning Status
    
    /// <summary>
    /// Gets the status of a provisioning job
    /// </summary>
    /// <param name="jobId">The provisioning job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provisioning status</returns>
    Task<ProvisioningStatus> GetProvisioningStatusAsync(string jobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all requests currently being provisioned
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of requests in provisioning state</returns>
    Task<List<ServiceCreationRequest>> GetProvisioningRequestsAsync(CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Statistics & Reporting
    
    /// <summary>
    /// Gets service creation statistics for dashboard
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistics summary</returns>
    Task<ServiceCreationStats> GetStatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets service creation history for a specific time period
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of requests in the time period</returns>
    Task<List<ServiceCreationRequest>> GetHistoryAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    #endregion
}

/// <summary>
/// Result of a provisioning operation
/// </summary>
public class ProvisioningResult
{
    public bool Success { get; set; }
    public string? JobId { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string> Errors { get; set; } = new();
}

/// <summary>
/// Status of an ongoing provisioning job
/// </summary>
public class ProvisioningStatus
{
    public string JobId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // InProgress, Completed, Failed
    public int PercentComplete { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public List<string> CompletedSteps { get; set; } = new();
    public List<string> FailedSteps { get; set; } = new();
    public Dictionary<string, string> ProvisionedResources { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// ServiceCreation statistics for dashboard
/// </summary>
public class ServiceCreationStats
{
    public int TotalRequests { get; set; }
    public int PendingReview { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int InProvisioning { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public double AverageApprovalTimeHours { get; set; }
    public double AverageProvisioningTimeHours { get; set; }
    public double SuccessRate { get; set; }
    public List<ServiceCreationTrend> Trends { get; set; } = new();
}

/// <summary>
/// Trend data for service creation metrics
/// </summary>
public class ServiceCreationTrend
{
    public DateTime Date { get; set; }
    public int RequestsSubmitted { get; set; }
    public int RequestsCompleted { get; set; }
    public int RequestsRejected { get; set; }
}
