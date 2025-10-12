using Microsoft.AspNetCore.Mvc;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models.IntelligentChat;
using Platform.Engineering.Copilot.API.Models;

namespace Platform.Engineering.Copilot.API.Controllers;

/// <summary>
/// REST API controller for AI-powered chat interactions and natural language processing.
/// Provides endpoints for processing chat queries, receiving AI-generated recommendations,
/// and integrating with platform tools through conversational interfaces.
/// Supports natural language platform management and intelligent tool suggestions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IIntelligentChatService _intelligentChat;

    /// <summary>
    /// Initializes a new instance of the ChatController.
    /// </summary>
    /// <param name="logger">Logger for chat API operation diagnostics</param>
    /// <param name="intelligentChat">Intelligent chat service for AI-powered intent classification</param>
    public ChatController(
        ILogger<ChatController> logger, 
        IIntelligentChatService intelligentChat)
    {
        _logger = logger;
        _intelligentChat = intelligentChat;
    }

    /// <summary>
    /// Process user message with AI-powered intent classification and tool execution
    /// This is the new intelligent chat endpoint that uses Azure OpenAI for intent classification,
    /// supports multi-step tool chaining, and provides proactive suggestions.
    /// </summary>
    /// <param name="request">Intelligent chat request with message and conversation context</param>
    /// <returns>Intelligent chat response with AI classification, tool results, and suggestions</returns>
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
    /// Classify user intent without executing tools
    /// Useful for testing intent classification or pre-flight analysis.
    /// </summary>
    /// <param name="request">Message and context for classification</param>
    /// <returns>Intent classification result with confidence scores</returns>
    [HttpPost("classify-intent")]
    public async Task<ActionResult<IntentClassificationResult>> ClassifyIntentAsync(
        [FromBody] IntelligentChatRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message is required");
            }

            _logger.LogInformation("Classifying intent: {Message}", request.Message);

            var intent = await _intelligentChat.ClassifyIntentAsync(
                request.Message,
                request.Context,
                HttpContext.RequestAborted);

            return Ok(intent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying intent: {Message}", request.Message);
            return StatusCode(500, new { error = "Failed to classify intent" });
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
}