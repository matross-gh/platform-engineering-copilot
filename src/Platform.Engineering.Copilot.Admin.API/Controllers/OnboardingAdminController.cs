using Microsoft.AspNetCore.Mvc;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models.ServiceCreation;
using Platform.Engineering.Copilot.Core.Data.Entities;
using Platform.Engineering.Copilot.Core.Interfaces.ServiceCreation;

namespace Platform.Engineering.Copilot.Admin.Controllers;

/// <summary>
/// API controller for managing Navy Flankspeed ServiceCreation requests
/// </summary>
[ApiController]
[Route("api/admin/ServiceCreation")]
[Produces("application/json")]
public class ServiceCreationAdminController : ControllerBase
{
    private readonly IServiceCreationService _ServiceCreationService;
    private readonly ILogger<ServiceCreationAdminController> _logger;

    public ServiceCreationAdminController(
        IServiceCreationService ServiceCreationService,
        ILogger<ServiceCreationAdminController> logger)
    {
        _ServiceCreationService = ServiceCreationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all pending ServiceCreation requests awaiting NNWC review
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<ServiceCreationRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCreationRequest>>> GetPendingRequests(
        CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _ServiceCreationService.GetPendingRequestsAsync(cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending ServiceCreation requests");
            return StatusCode(500, new { error = "Failed to retrieve pending requests" });
        }
    }

    /// <summary>
    /// Gets a specific ServiceCreation request by ID
    /// </summary>
    [HttpGet("{requestId}")]
    [ProducesResponseType(typeof(ServiceCreationRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCreationRequest>> GetRequest(
        string requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await _ServiceCreationService.GetRequestAsync(requestId, cancellationToken);
            
            if (request == null)
            {
                return NotFound(new { error = $"Request {requestId} not found" });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ServiceCreation request {RequestId}", requestId);
            return StatusCode(500, new { error = "Failed to retrieve request" });
        }
    }

    /// <summary>
    /// Gets all ServiceCreation requests for a specific mission owner
    /// </summary>
    [HttpGet("owner/{email}")]
    [ProducesResponseType(typeof(List<ServiceCreationRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCreationRequest>>> GetRequestsByOwner(
        string email,
        CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _ServiceCreationService.GetRequestsByOwnerAsync(email, cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving requests for owner {Email}", email);
            return StatusCode(500, new { error = "Failed to retrieve requests" });
        }
    }

    /// <summary>
    /// Approves an ServiceCreation request and triggers automated provisioning
    /// </summary>
    [HttpPost("{requestId}/approve")]
    [ProducesResponseType(typeof(ServiceCreationApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCreationApprovalResponse>> ApproveRequest(
        string requestId,
        [FromBody] ServiceCreationApprovalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Approving ServiceCreation request {RequestId} by {ApprovedBy}",
                requestId, request.ApprovedBy);

            var result = await _ServiceCreationService.ApproveRequestAsync(
                requestId,
                request.ApprovedBy,
                request.Comments,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new ServiceCreationApprovalResponse
                {
                    Success = false,
                    Message = result.Message ?? "Failed to approve request",
                    RequestId = requestId
                });
            }

            return Ok(new ServiceCreationApprovalResponse
            {
                Success = true,
                Message = "ServiceCreation request approved. Provisioning will begin shortly.",
                RequestId = requestId,
                ProvisioningJobId = result.JobId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving ServiceCreation request {RequestId}", requestId);
            return StatusCode(500, new ServiceCreationApprovalResponse
            {
                Success = false,
                Message = $"Error approving request: {ex.Message}",
                RequestId = requestId
            });
        }
    }

    /// <summary>
    /// Rejects an ServiceCreation request
    /// </summary>
    [HttpPost("{requestId}/reject")]
    [ProducesResponseType(typeof(ServiceCreationApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCreationApprovalResponse>> RejectRequest(
        string requestId,
        [FromBody] ServiceCreationRejectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Rejecting ServiceCreation request {RequestId} by {RejectedBy}",
                requestId, request.RejectedBy);

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new ServiceCreationApprovalResponse
                {
                    Success = false,
                    Message = "Rejection reason is required",
                    RequestId = requestId
                });
            }

            var success = await _ServiceCreationService.RejectRequestAsync(
                requestId,
                request.RejectedBy,
                request.Reason,
                cancellationToken);

            if (!success)
            {
                return BadRequest(new ServiceCreationApprovalResponse
                {
                    Success = false,
                    Message = "Failed to reject request. It may not exist or may already be processed.",
                    RequestId = requestId
                });
            }

            return Ok(new ServiceCreationApprovalResponse
            {
                Success = true,
                Message = "ServiceCreation request rejected. Mission owner will be notified.",
                RequestId = requestId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting ServiceCreation request {RequestId}", requestId);
            return StatusCode(500, new ServiceCreationApprovalResponse
            {
                Success = false,
                Message = $"Error rejecting request: {ex.Message}",
                RequestId = requestId
            });
        }
    }

    /// <summary>
    /// Gets the status of a provisioning job
    /// </summary>
    [HttpGet("provisioning/{jobId}")]
    [ProducesResponseType(typeof(ProvisioningStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProvisioningStatus>> GetProvisioningStatus(
        string jobId,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await _ServiceCreationService.GetProvisioningStatusAsync(jobId, cancellationToken);
            
            if (status.Status == "NotFound")
            {
                return NotFound(new { error = $"Provisioning job {jobId} not found" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provisioning status for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retrieve provisioning status" });
        }
    }

    /// <summary>
    /// Gets all requests currently being provisioned
    /// </summary>
    [HttpGet("provisioning")]
    [ProducesResponseType(typeof(List<ServiceCreationRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCreationRequest>>> GetProvisioningRequests(
        CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _ServiceCreationService.GetProvisioningRequestsAsync(cancellationToken);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provisioning requests");
            return StatusCode(500, new { error = "Failed to retrieve provisioning requests" });
        }
    }

    /// <summary>
    /// Gets ServiceCreation statistics for the dashboard
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ServiceCreationStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceCreationStats>> GetStats(
        CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _ServiceCreationService.GetStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ServiceCreation statistics");
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }

    /// <summary>
    /// Gets ServiceCreation history for a specific time period
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<ServiceCreationRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceCreationRequest>>> GetHistory(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var history = await _ServiceCreationService.GetHistoryAsync(start, end, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ServiceCreation history");
            return StatusCode(500, new { error = "Failed to retrieve history" });
        }
    }
}

#region Request/Response DTOs

/// <summary>
/// Request to approve an ServiceCreation
/// </summary>
public class ServiceCreationApprovalRequest
{
    /// <summary>
    /// Name/ID of the person approving
    /// </summary>
    public string ApprovedBy { get; set; } = string.Empty;

    /// <summary>
    /// Optional comments for the approval
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Request to reject an ServiceCreation
/// </summary>
public class ServiceCreationRejectionRequest
{
    /// <summary>
    /// Name/ID of the person rejecting
    /// </summary>
    public string RejectedBy { get; set; } = string.Empty;

    /// <summary>
    /// Reason for rejection (required)
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response from approval/rejection action
/// </summary>
public class ServiceCreationApprovalResponse
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The request ID
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Provisioning job ID (if approval triggered provisioning)
    /// </summary>
    public string? ProvisioningJobId { get; set; }
}

#endregion
