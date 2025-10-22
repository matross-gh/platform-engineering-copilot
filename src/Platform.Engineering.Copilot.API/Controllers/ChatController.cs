using Microsoft.AspNetCore.Mvc;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;
using Platform.Engineering.Copilot.Core.Models.Jobs;
using Platform.Engineering.Copilot.API.Models;

namespace Platform.Engineering.Copilot.API.Controllers;

/// <summary>
/// REST API controller for AI-powered chat interactions using pure multi-agent orchestration.
/// All requests are delegated to the OrchestratorAgent, which coordinates 6 specialized agents:
/// - InfrastructureAgent: Azure resource provisioning and management
/// - ComplianceAgent: NIST 800-53 compliance and ATO automation
/// - CostManagementAgent: Cost analysis and optimization
/// - EnvironmentAgent: Environment lifecycle management
/// - DiscoveryAgent: Resource inventory and health monitoring
/// - OnboardingAgent: Mission onboarding and requirements gathering
/// 
/// The orchestrator automatically determines intent, plans execution, routes to appropriate agents,
/// and synthesizes cohesive responses. No manual intent classification or tool selection needed.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IIntelligentChatService _intelligentChat;
    private readonly IBackgroundJobService _backgroundJobService;

    /// <summary>
    /// Initializes a new instance of the ChatController.
    /// </summary>
    /// <param name="logger">Logger for chat API operation diagnostics</param>
    /// <param name="intelligentChat">Intelligent chat service that delegates to OrchestratorAgent for pure multi-agent processing</param>
    /// <param name="backgroundJobService">Background job service for long-running operations</param>
    public ChatController(
        ILogger<ChatController> logger, 
        IIntelligentChatService intelligentChat,
        IBackgroundJobService backgroundJobService)
    {
        _logger = logger;
        _intelligentChat = intelligentChat;
        _backgroundJobService = backgroundJobService;
    }

    /// <summary>
    /// Process user message with pure multi-agent orchestration.
    /// The OrchestratorAgent automatically:
    /// 1. Analyzes user intent and requirements
    /// 2. Creates an execution plan (sequential, parallel, or collaborative)
    /// 3. Routes to appropriate specialized agents
    /// 4. Coordinates agent execution and context sharing
    /// 5. Synthesizes a cohesive response from all agent results
    /// 
    /// This is the PRIMARY endpoint for all chat interactions - intent classification,
    /// agent selection, and orchestration are all handled automatically.
    /// </summary>
    /// <param name="request">Intelligent chat request with message and conversation context</param>
    /// <returns>Intelligent chat response with orchestrated results from multiple agents</returns>
    [HttpPost("intelligent-query")]
    public async Task<ActionResult<IntelligentChatApiResponse>> ProcessIntelligentQueryAsync(
        [FromBody] IntelligentChatRequest request)
    {
        try
        {
            _logger.LogInformation("🔵 CONTROLLER: Received request to /api/chat/intelligent-query");
            
            if (string.IsNullOrEmpty(request.Message))
            {
                _logger.LogWarning("🔵 CONTROLLER: Message is null or empty");
                return BadRequest(new IntelligentChatApiResponse
                {
                    Success = false,
                    Error = "Message is required"
                });
            }

            if (string.IsNullOrEmpty(request.ConversationId))
            {
                _logger.LogWarning("🔵 CONTROLLER: ConversationId is null or empty");
                return BadRequest(new IntelligentChatApiResponse
                {
                    Success = false,
                    Error = "ConversationId is required"
                });
            }

            _logger.LogInformation(
                "🔵 CONTROLLER: Processing intelligent chat query. ConversationId: {ConversationId}, Message: {Message}", 
                request.ConversationId, 
                request.Message);

            var response = await _intelligentChat.ProcessMessageAsync(
                request.Message,
                request.ConversationId,
                request.Context,
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "Intelligent chat query processed successfully. Intent: {IntentType}, ToolExecuted: {ToolExecuted}", 
                response.Intent.IntentType, 
                response.ToolExecuted);

            return Ok(new IntelligentChatApiResponse
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing intelligent chat query: {Message}", request.Message);
            return StatusCode(500, new IntelligentChatApiResponse
            {
                Success = false,
                Error = "Internal server error processing intelligent chat query",
                ErrorDetails = new Dictionary<string, string>
                {
                    ["ExceptionType"] = ex.GetType().Name,
                    ["Message"] = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get conversation context for a specific conversation
    /// Retrieves the current state of a conversation including message history,
    /// used tools, and mentioned resources.
    /// </summary>
    /// <param name="conversationId">Unique conversation identifier</param>
    /// <returns>Conversation context with history and state</returns>
    [HttpGet("context/{conversationId}")]
    public async Task<ActionResult<ConversationContext>> GetContextAsync(string conversationId)
    {
        try
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                return BadRequest("ConversationId is required");
            }

            _logger.LogInformation("Retrieving conversation context: {ConversationId}", conversationId);

            var context = await _intelligentChat.GetOrCreateContextAsync(
                conversationId,
                cancellationToken: HttpContext.RequestAborted);

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation context: {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to retrieve conversation context" });
        }
    }

    /// <summary>
    /// Generate proactive suggestions based on conversation context
    /// Uses AI to recommend next actions the user might want to take.
    /// </summary>
    /// <param name="conversationId">Unique conversation identifier</param>
    /// <returns>List of AI-generated proactive suggestions</returns>
    [HttpGet("suggestions/{conversationId}")]
    public async Task<ActionResult<List<ProactiveSuggestion>>> GetSuggestionsAsync(string conversationId)
    {
        try
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                return BadRequest("ConversationId is required");
            }

            _logger.LogInformation("Generating proactive suggestions: {ConversationId}", conversationId);

            var context = await _intelligentChat.GetOrCreateContextAsync(
                conversationId,
                cancellationToken: HttpContext.RequestAborted);

            var suggestions = await _intelligentChat.GenerateProactiveSuggestionsAsync(
                conversationId,
                context,
                HttpContext.RequestAborted);

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggestions: {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to generate suggestions" });
        }
    }

    /// <summary>
    /// Process onboarding-specific message with domain-specific system prompt
    /// This is a convenience endpoint that wraps intelligent-query with onboarding context.
    /// The Chat App can call this or the generic intelligent-query endpoint.
    /// </summary>
    /// <param name="request">Onboarding chat request</param>
    /// <returns>Intelligent chat response with onboarding-specific context</returns>
    [HttpPost("onboarding")]
    public async Task<ActionResult<IntelligentChatApiResponse>> ProcessOnboardingQueryAsync(
        [FromBody] IntelligentChatRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new IntelligentChatApiResponse
                {
                    Success = false,
                    Error = "Message is required"
                });
            }

            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return BadRequest(new IntelligentChatApiResponse
                {
                    Success = false,
                    Error = "ConversationId is required"
                });
            }

            _logger.LogInformation(
                "Processing onboarding query. ConversationId: {ConversationId}", 
                request.ConversationId);

            // Enhance context with onboarding-specific system prompt
            var context = request.Context ?? new ConversationContext
            {
                ConversationId = request.ConversationId
            };

            // Set onboarding domain hints
            context.CurrentTopic = "onboarding";
            context.SessionMetadata["domain"] = "onboarding";
            
            if (!context.SessionMetadata.ContainsKey("systemPrompt"))
            {
                context.SessionMetadata["systemPrompt"] = GetOnboardingSystemPrompt();
            }

            var response = await _intelligentChat.ProcessMessageAsync(
                request.Message,
                request.ConversationId,
                context,
                HttpContext.RequestAborted);

            _logger.LogInformation(
                "Onboarding query processed. Intent: {IntentType}, ToolExecuted: {ToolExecuted}", 
                response.Intent.IntentType, 
                response.ToolExecuted);

            return Ok(new IntelligentChatApiResponse
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing onboarding query: {Message}", request.Message);
            return StatusCode(500, new IntelligentChatApiResponse
            {
                Success = false,
                Error = "Internal server error processing onboarding query",
                ErrorDetails = new Dictionary<string, string>
                {
                    ["ExceptionType"] = ex.GetType().Name,
                    ["Message"] = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get onboarding-specific system prompt for entity extraction
    /// </summary>
    private static string GetOnboardingSystemPrompt()
    {
        return @"You are an AI assistant for Navy Flankspeed mission onboarding. Your role is to help Navy personnel onboard new missions to the Flankspeed cloud platform.

## Available Functions

You have access to the `process_onboarding_query` function which handles all onboarding operations. When users describe their onboarding needs, extract information and call this function with appropriate parameters.

## Entity Extraction Guidelines

When users describe onboarding requirements, extract these fields and pass them in the `additionalContext` parameter as JSON:

**Mission Details:**
- missionName: Name of the mission/project
- missionOwner: Full name of the mission owner
- missionOwnerEmail: Email address (.mil domain)
- missionOwnerRank: Military rank (CDR, LCDR, GS-14, etc.)
- command: Navy command (NAVWAR, SPAWAR, NIWC, NAVAIR, etc.)

**Technical Requirements:**
- requestedSubscriptionName: Desired Azure subscription name
- requestedVNetCidr: VNet CIDR block (e.g., 10.100.0.0/16)
- requiredServices: Array of Azure services (e.g., [""AKS"", ""Azure SQL"", ""Redis""])
- region: Azure Government region (usgovvirginia, usgovtexas, usgovarizona)

**Security & Compliance:**
- classificationLevel: UNCLASS, CUI, SECRET, or TS
- requiresPki: Boolean - PKI certificates required
- requiresCac: Boolean - CAC authentication required
- requiresAto: Boolean - ATO required
- complianceFrameworks: Array of frameworks (e.g., [""NIST 800-53"", ""DISA STIG""])

**Business Context:**
- businessJustification: Why this mission needs the environment
- useCase: Detailed use case description
- estimatedUserCount: Expected number of users
- fundingSource: Funding source (OPTAR, RDT&E, O&M)

## Conversation Flow

1. For new onboarding requests, create a draft first, then update with additional details
2. Remember request IDs from previous messages to update the same request
3. Ask clarifying questions if critical information is missing
4. Confirm destructive actions (submit, approve, reject) before executing
5. Provide clear, actionable responses in a professional but friendly tone

## Important Rules

- Always extract as much information as possible from user messages
- Use the additionalContext parameter to pass structured JSON
- For multi-turn conversations, accumulate information across turns
- Be conversational and helpful, not robotic
- Validate email addresses end with .mil domain
- Default region to ""usgovvirginia"" if not specified
- Default classificationLevel to ""UNCLASS"" if not specified
- Format responses with clear structure (use markdown, bullets, emojis)
- Include request IDs in responses so users can reference them

## Response Style

✅ Good: ""Created draft request #abc-123 for Tactical Edge Platform. What VNet CIDR would you like?""
❌ Bad: ""Request created successfully.""

Be specific, helpful, and guide users through the onboarding process step-by-step.";
    }

    /// <summary>
    /// Start long-running intelligent chat query asynchronously.
    /// Returns immediately with job ID for status polling.
    /// Use this endpoint for operations that may take longer than 30 seconds
    /// (e.g., Azure deployments, comprehensive compliance scans, complex multi-agent orchestration).
    /// </summary>
    /// <param name="request">Intelligent chat request with message and conversation context</param>
    /// <returns>202 Accepted with job ID and status URL</returns>
    [HttpPost("intelligent-query-async")]
    public async Task<ActionResult<AsyncJobResponse>> ProcessIntelligentQueryAsyncAsync(
        [FromBody] IntelligentChatRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            if (string.IsNullOrEmpty(request.ConversationId))
            {
                return BadRequest(new { error = "ConversationId is required" });
            }

            _logger.LogInformation(
                "Starting async intelligent chat query. ConversationId: {ConversationId}, Message: {Message}",
                request.ConversationId,
                request.Message);

            // Extract user ID from context if available
            var userId = request.Context?.SessionMetadata.GetValueOrDefault("userId")?.ToString() ?? "anonymous";

            // Start background job
            var job = await _backgroundJobService.StartJobAsync(
                jobType: "IntelligentChat",
                conversationId: request.ConversationId,
                userId: userId,
                inputMessage: request.Message,
                inputContext: request.Context != null ? new Dictionary<string, object>
                {
                    ["context"] = request.Context
                } : null,
                workload: async (progress, ct) =>
                {
                    // Report initial progress
                    ((IProgress<JobProgressUpdate>)progress).Report(new JobProgressUpdate
                    {
                        JobId = string.Empty, // Will be set by service
                        ProgressPercentage = 10,
                        CurrentStep = "Analyzing user intent and planning execution"
                    });

                    // Get or create context
                    var context = request.Context ?? await _intelligentChat.GetOrCreateContextAsync(
                        request.ConversationId,
                        cancellationToken: ct);

                    ((IProgress<JobProgressUpdate>)progress).Report(new JobProgressUpdate
                    {
                        ProgressPercentage = 20,
                        CurrentStep = "Orchestrating multi-agent execution"
                    });

                    // Execute intelligent chat processing
                    var response = await _intelligentChat.ProcessMessageAsync(
                        request.Message,
                        request.ConversationId,
                        context,
                        ct);

                    ((IProgress<JobProgressUpdate>)progress).Report(new JobProgressUpdate
                    {
                        ProgressPercentage = 90,
                        CurrentStep = "Finalizing response"
                    });

                    return response;
                },
                HttpContext.RequestAborted);

            // Return 202 Accepted with job details
            var statusUrl = $"/api/chat/jobs/{job.JobId}";
            Response.Headers.Append("Location", statusUrl);

            return Accepted(new AsyncJobResponse
            {
                JobId = job.JobId,
                Status = job.Status.ToString(),
                Message = "Job started successfully. Use the jobId to check status and retrieve results.",
                StatusUrl = statusUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting async intelligent chat query");
            return StatusCode(500, new { error = "Failed to start async job", details = ex.Message });
        }
    }

    /// <summary>
    /// Get status of a background job.
    /// Poll this endpoint to check progress and retrieve results when complete.
    /// </summary>
    /// <param name="jobId">Job identifier returned from async endpoint</param>
    /// <returns>Job status with progress, current step, and results (if complete)</returns>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<JobStatusResponse>> GetJobStatusAsync(string jobId)
    {
        try
        {
            var job = await _backgroundJobService.GetJobAsync(jobId, HttpContext.RequestAborted);

            if (job == null)
            {
                return NotFound(new { error = $"Job {jobId} not found" });
            }

            var response = new JobStatusResponse
            {
                JobId = job.JobId,
                Status = job.Status.ToString(),
                ProgressPercentage = job.ProgressPercentage,
                CurrentStep = job.CurrentStep,
                CompletedSteps = job.CompletedSteps,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                Error = job.Error
            };

            // Include result if completed
            if (job.Status == JobStatus.Completed && job.Result != null)
            {
                response.Result = job.Result;
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retrieve job status" });
        }
    }

    /// <summary>
    /// List all jobs for a conversation.
    /// Useful for showing job history and finding recent operations.
    /// </summary>
    /// <param name="conversationId">Conversation identifier</param>
    /// <returns>List of jobs for the conversation, ordered by creation time (newest first)</returns>
    [HttpGet("conversations/{conversationId}/jobs")]
    public async Task<ActionResult<List<JobStatusResponse>>> GetConversationJobsAsync(string conversationId)
    {
        try
        {
            var jobs = await _backgroundJobService.GetJobsByConversationAsync(
                conversationId,
                HttpContext.RequestAborted);

            var responses = jobs.Select(j => new JobStatusResponse
            {
                JobId = j.JobId,
                Status = j.Status.ToString(),
                ProgressPercentage = j.ProgressPercentage,
                CurrentStep = j.CurrentStep,
                CompletedSteps = j.CompletedSteps,
                CreatedAt = j.CreatedAt,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                Error = j.Error,
                // Don't include full results in list view for performance
                Result = null
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing jobs for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to list conversation jobs" });
        }
    }

    /// <summary>
    /// Cancel a running job.
    /// Attempts to gracefully cancel the job execution.
    /// </summary>
    /// <param name="jobId">Job identifier to cancel</param>
    /// <returns>Success message if cancelled, error if job not found or already completed</returns>
    [HttpDelete("jobs/{jobId}")]
    public async Task<ActionResult> CancelJobAsync(string jobId)
    {
        try
        {
            var cancelled = await _backgroundJobService.CancelJobAsync(jobId, HttpContext.RequestAborted);

            if (!cancelled)
            {
                return NotFound(new { error = $"Job {jobId} not found or already completed" });
            }

            _logger.LogInformation("User cancelled job {JobId}", jobId);

            return Ok(new { message = $"Job {jobId} cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to cancel job" });
        }
    }
}