using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Platform.Engineering.Copilot.Core.Plugins;

/// <summary>
/// Semantic Kernel plugin for Navy Flankspeed mission owner onboarding
/// </summary>
public class OnboardingPlugin : BaseSupervisorPlugin
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingPlugin(
        ILogger<OnboardingPlugin> logger,
        Kernel kernel,
        IOnboardingService onboardingService) : base(logger, kernel)
    {
        _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
    }

    [KernelFunction("capture_onboarding_requirements")]
    [Description("FIRST STEP for new mission onboarding. Captures requirements and generates review summary. Use when user says 'onboard', 'new mission', or provides mission details. DO NOT call create_environment - this function only creates a draft for review.")]
    public async Task<string> CaptureOnboardingRequirementsAsync(
        [Description("Mission name")] string missionName,
        [Description("Organization")] string organization,
        [Description("All other requirements as JSON string with keys: classificationLevel, environmentType, region, requiredServices, networkRequirements, computeRequirements, databaseRequirements, complianceFrameworks, securityControls, targetDeploymentDate, expectedGoLiveDate")] string? additionalRequirements = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Capturing onboarding requirements for mission: {MissionName}", missionName);

            // Create draft onboarding request
            var requestId = await _onboardingService.CreateDraftRequestAsync(cancellationToken);

            // Build context data from parameters
            var context = new Dictionary<string, object?>
            {
                ["missionName"] = missionName,
                ["organization"] = organization
            };

            // Parse additional requirements if provided
            if (!string.IsNullOrWhiteSpace(additionalRequirements))
            {
                try
                {
                    var additional = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(additionalRequirements);
                    if (additional != null)
                    {
                        foreach (var kvp in additional)
                        {
                            context[kvp.Key] = kvp.Value.ValueKind == JsonValueKind.String 
                                ? kvp.Value.GetString() 
                                : kvp.Value.ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse additional requirements, using raw string");
                    context["additionalRequirements"] = additionalRequirements;
                }
            }

            // Update draft with all requirements
            var updated = await _onboardingService.UpdateDraftAsync(requestId, context, cancellationToken);

            if (!updated)
            {
                return $"❌ Failed to capture requirements for onboarding request {requestId}. Please try again.";
            }

            // Generate detailed review summary
            var summary = new StringBuilder();
            summary.AppendLine($"# 📋 Onboarding Request Review - {missionName}");
            summary.AppendLine();
            summary.AppendLine($"**Request ID:** `{requestId}`");
            summary.AppendLine($"**Status:** Draft (Pending User Confirmation)");
            summary.AppendLine();
            
            summary.AppendLine("## Mission Details");
            summary.AppendLine($"- **Mission Name:** {missionName}");
            summary.AppendLine($"- **Organization:** {organization}");
            
            if (context.TryGetValue("classificationLevel", out var classification))
                summary.AppendLine($"- **Classification:** {classification}");
            if (context.TryGetValue("environmentType", out var envType))
                summary.AppendLine($"- **Environment:** {envType}");
            if (context.TryGetValue("region", out var region))
                summary.AppendLine($"- **Region:** {region}");
            summary.AppendLine();

            if (context.TryGetValue("requiredServices", out var services))
            {
                summary.AppendLine("## Infrastructure to be Provisioned");
                var serviceList = services?.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                foreach (var service in serviceList)
                {
                    summary.AppendLine($"- ✅ **{service.Trim()}**");
                }
                summary.AppendLine();
            }

            if (context.TryGetValue("networkRequirements", out var network) && network != null)
            {
                summary.AppendLine("## Network Configuration");
                summary.AppendLine($"{network}");
                summary.AppendLine();
            }

            if (context.TryGetValue("computeRequirements", out var compute) && compute != null)
            {
                summary.AppendLine("## Compute Resources");
                summary.AppendLine($"{compute}");
                summary.AppendLine();
            }

            if (context.TryGetValue("databaseRequirements", out var database) && database != null)
            {
                summary.AppendLine("## Database Requirements");
                summary.AppendLine($"{database}");
                summary.AppendLine();
            }

            if (context.TryGetValue("complianceFrameworks", out var compliance) && compliance != null)
            {
                summary.AppendLine("## Compliance & Security");
                summary.AppendLine($"**Frameworks:** {compliance}");
                if (context.TryGetValue("securityControls", out var secControls) && secControls != null)
                {
                    summary.AppendLine($"**Security Controls:** {secControls}");
                }
                summary.AppendLine();
            }

            if (context.TryGetValue("targetDeploymentDate", out var deployDate) && deployDate != null)
            {
                summary.AppendLine("## Timeline");
                summary.AppendLine($"- **Target Deployment:** {deployDate}");
                if (context.TryGetValue("expectedGoLiveDate", out var goLive) && goLive != null)
                {
                    summary.AppendLine($"- **Expected Go-Live:** {goLive}");
                }
                summary.AppendLine();
            }

            summary.AppendLine("## Estimated Provisioning Time");
            summary.AppendLine($"- Infrastructure deployment: ~30-45 minutes");
            summary.AppendLine($"- Compliance configuration: ~15-20 minutes");
            summary.AppendLine($"- Total estimated time: ~1 hour");
            summary.AppendLine();

            summary.AppendLine("## Next Steps");
            summary.AppendLine();
            summary.AppendLine("⚠️ **IMPORTANT:** Please review the above configuration carefully.");
            summary.AppendLine();
            summary.AppendLine("**To submit for platform team approval, respond with:**");
            summary.AppendLine("- 'Yes, proceed'");
            summary.AppendLine("- 'Confirm and submit'");
            summary.AppendLine("- 'Go ahead'");
            summary.AppendLine();
            summary.AppendLine("ℹ️ **Note:** Your request will be submitted to the NAVWAR Platform Engineering team for review.");
            summary.AppendLine("Provisioning will begin automatically once approved by the platform team.");
            summary.AppendLine();
            summary.AppendLine("**To make changes, respond with:**");
            summary.AppendLine($"- 'Update request {requestId} with [your changes]'");
            summary.AppendLine();
            summary.AppendLine("**To cancel, respond with:**");
            summary.AppendLine($"- 'Cancel request {requestId}'");

            return summary.ToString();
        }
        catch (Exception ex)
        {
            return CreateErrorResponse("capture onboarding requirements", ex);
        }
    }

    [KernelFunction("submit_for_approval")]
    [Description("Submit an onboarding request for platform team approval. Use this AFTER the user confirms they want to proceed. This will NOT start provisioning - it creates a pending approval request in the admin console for the platform team to review and approve/deny.")]
    public async Task<string> SubmitForApprovalAsync(
        [Description("Request ID from capture_onboarding_requirements (GUID format)")] string requestId,
        [Description("Email of the user submitting the request (typically the mission owner)")] string? submittedBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting onboarding request {RequestId} for approval by {SubmittedBy}", 
                requestId, submittedBy ?? "unknown");

            // Get the request to show details in confirmation
            var request = await _onboardingService.GetRequestAsync(requestId, cancellationToken);
            
            if (request == null)
            {
                return $"❌ **Error:** Onboarding request `{requestId}` not found. Please verify the request ID.";
            }

            // Submit for approval
            var success = await _onboardingService.SubmitRequestAsync(requestId, submittedBy, cancellationToken);

            if (!success)
            {
                return $"❌ **Error:** Failed to submit request `{requestId}` for approval. The request may already be submitted or may have validation errors.";
            }

            // Build success response
            var response = new StringBuilder();
            response.AppendLine("✅ **Onboarding Request Submitted for Approval**");
            response.AppendLine();
            response.AppendLine($"**Request ID:** `{requestId}`");
            response.AppendLine($"**Mission:** {request.MissionName}");
            response.AppendLine($"**Status:** Pending Platform Team Review");
            response.AppendLine($"**Submitted By:** {submittedBy ?? request.MissionOwnerEmail}");
            response.AppendLine();
            response.AppendLine("## What Happens Next?");
            response.AppendLine();
            response.AppendLine("1. **Platform Team Review** - Your request has been submitted to the NAVWAR Platform Engineering team for review");
            response.AppendLine("2. **Admin Console** - The team will review your requirements in the Admin Console");
            response.AppendLine("3. **Approval Decision** - The team will either approve or request changes");
            response.AppendLine("4. **Automatic Provisioning** - Once approved, your environment will be automatically provisioned");
            response.AppendLine("5. **Email Notification** - You'll receive an email when your request is approved or if changes are needed");
            response.AppendLine();
            response.AppendLine("📧 **You will be notified via email** when the platform team makes a decision on your request.");
            response.AppendLine();
            response.AppendLine($"💡 **Track your request:** You can check the status anytime by asking: \"What's the status of request {requestId}?\"");

            return response.ToString();
        }
        catch (Exception ex)
        {
            return CreateErrorResponse("submit onboarding request for approval", ex);
        }
    }

    [KernelFunction("process_onboarding_query")]
    [Description("Process any Navy Flankspeed onboarding query using natural language. Handles draft creation, updates, submission, approval workflow, cancellations, status checks, provisioning updates, reporting, and history requests.")]
    public async Task<string> ProcessOnboardingQueryAsync(
        [Description("Natural language onboarding query (e.g., 'start a new onboarding draft', 'submit request 123', 'show pending onboarding requests').")] string query,
        [Description("Optional onboarding request ID when already known (GUID format). If not supplied, the plugin attempts to extract it from the query text.")] string? requestId = null,
        [Description("Optional JSON blob with extra context (e.g., approvals, rejection reasons, field updates). Useful when structured data is required.")] string? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing onboarding query: {Query}", query);

            var normalizedQuery = query.ToLowerInvariant();
            var intent = DetermineIntent(normalizedQuery);
            var contextData = ParseContext(additionalContext);
            requestId ??= ExtractRequestId(query);

            return intent switch
            {
                OnboardingIntent.CreateDraft => await HandleCreateDraftAsync(cancellationToken),
                OnboardingIntent.UpdateDraft => await HandleUpdateDraftAsync(requestId, contextData, cancellationToken),
                OnboardingIntent.SubmitRequest => await HandleSubmitRequestAsync(requestId, cancellationToken),
                OnboardingIntent.CancelRequest => await HandleCancelRequestAsync(requestId, contextData, cancellationToken),
                OnboardingIntent.CheckStatus => await HandleRequestStatusAsync(requestId, contextData, cancellationToken),
                OnboardingIntent.ListPending => await HandlePendingRequestsAsync(cancellationToken),
                OnboardingIntent.ListOwnerRequests => await HandleOwnerRequestsAsync(normalizedQuery, contextData, cancellationToken),
                OnboardingIntent.ApproveRequest => await HandleApproveRequestAsync(requestId, contextData, cancellationToken),
                OnboardingIntent.RejectRequest => await HandleRejectRequestAsync(requestId, contextData, cancellationToken),
                OnboardingIntent.ProvisioningStatus => await HandleProvisioningStatusAsync(query, contextData, cancellationToken),
                OnboardingIntent.ProvisioningRequests => await HandleProvisioningRequestsAsync(cancellationToken),
                OnboardingIntent.Stats => await HandleStatsAsync(cancellationToken),
                OnboardingIntent.History => await HandleHistoryAsync(contextData, cancellationToken),
                _ => ProvideUsageGuidance()
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse("process onboarding query", ex);
        }
    }

    private async Task<string> HandleCreateDraftAsync(CancellationToken cancellationToken)
    {
        var requestId = await _onboardingService.CreateDraftRequestAsync(cancellationToken);
        return $"✅ Created new onboarding draft request with ID `{requestId}`. You can now provide details or submit when ready.";
    }

    private async Task<string> HandleUpdateDraftAsync(
        string? requestId,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return "Please provide the onboarding request ID to update (e.g., include the GUID in your request).";
        }

        if (context.Count == 0)
        {
            return "No updates were supplied. Provide a JSON payload in `additionalContext` describing the fields to change (e.g., `{ \"missionName\": \"Project Triton\" }`).";
        }

        var updated = await _onboardingService.UpdateDraftAsync(requestId, context, cancellationToken);
        return updated
            ? $"✅ Updated onboarding draft `{requestId}` with the supplied changes."
            : $"Unable to update onboarding draft `{requestId}`. Ensure the request exists and is still in draft status.";
    }

    private async Task<string> HandleSubmitRequestAsync(string? requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return "Please provide the onboarding request ID you want to submit.";
        }

        var submitted = await _onboardingService.SubmitRequestAsync(requestId, submittedBy: null, cancellationToken);
        return submitted
            ? $"✅ Submitted onboarding request `{requestId}` for NNWC review."
            : $"Unable to submit onboarding request `{requestId}`. Verify the request exists and is still a draft.";
    }

    private async Task<string> HandleCancelRequestAsync(
        string? requestId,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return "Please specify the onboarding request ID to cancel.";
        }

        var reason = context.TryGetValue("reason", out var value) ? Convert.ToString(value) : null;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "Cancelling a request requires a reason. Provide `{ \"reason\": \"duplicate submission\" }` in `additionalContext`.";
        }

        var cancelled = await _onboardingService.CancelRequestAsync(requestId, reason!, cancellationToken);
        return cancelled
            ? $"✅ Cancelled onboarding request `{requestId}`.{Environment.NewLine}Reason: {reason}"
            : $"Unable to cancel onboarding request `{requestId}`. It may already be completed or cancelled.";
    }

    private async Task<string> HandleRequestStatusAsync(
        string? requestId,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            var request = await _onboardingService.GetRequestAsync(requestId, cancellationToken);
            return request != null
                ? FormatRequestDetail(request)
                : $"No onboarding request found with ID `{requestId}`.";
        }

        var ownerEmail = ExtractEmailFromContext(context);
        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            var requests = await _onboardingService.GetRequestsByOwnerAsync(ownerEmail!, cancellationToken);
            return requests.Count > 0
                ? FormatRequestList($"Onboarding requests for {ownerEmail}", requests)
                : $"No onboarding requests found for {ownerEmail}.";
        }

        return "Provide a request ID or mission owner email (via `additionalContext` with `ownerEmail`) to check status.";
    }

    private async Task<string> HandlePendingRequestsAsync(CancellationToken cancellationToken)
    {
        var requests = await _onboardingService.GetPendingRequestsAsync(cancellationToken);
        return requests.Count > 0
            ? FormatRequestList("Pending onboarding requests awaiting review", requests)
            : "No onboarding requests are currently pending review.";
    }

    private async Task<string> HandleOwnerRequestsAsync(
        string normalizedQuery,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        var ownerEmail = ExtractEmailFromContext(context) ?? ExtractEmailFromText(normalizedQuery);
        if (string.IsNullOrWhiteSpace(ownerEmail))
        {
            return "Provide the mission owner email to list their onboarding requests (e.g., `additionalContext` with `{ \"ownerEmail\": \"captain@example.mil\" }`).";
        }

        var requests = await _onboardingService.GetRequestsByOwnerAsync(ownerEmail, cancellationToken);
        return requests.Count > 0
            ? FormatRequestList($"Onboarding requests for {ownerEmail}", requests)
            : $"No onboarding requests found for {ownerEmail}.";
    }

    private async Task<string> HandleApproveRequestAsync(
        string? requestId,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return "Provide the onboarding request ID to approve.";
        }

        var approvedBy = context.TryGetValue("approvedBy", out var approverObj)
            ? Convert.ToString(approverObj)
            : "NNWC Reviewer";
        var comments = context.TryGetValue("comments", out var commentsObj)
            ? Convert.ToString(commentsObj)
            : null;

        var result = await _onboardingService.ApproveRequestAsync(requestId, approvedBy ?? "NNWC Reviewer", comments, cancellationToken);

        if (!result.Success)
        {
            return $"Unable to approve onboarding request `{requestId}`. {result.Message ?? "Check that the request is pending review."}";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"✅ Approved onboarding request `{requestId}`.");
        if (!string.IsNullOrWhiteSpace(result.JobId))
        {
            sb.AppendLine($"Provisioning job `{result.JobId}` started. Use 'check provisioning status {result.JobId}' to monitor progress.");
        }
        if (!string.IsNullOrWhiteSpace(comments))
        {
            sb.AppendLine($"Reviewer comments: {comments}");
        }

        return sb.ToString();
    }

    private async Task<string> HandleRejectRequestAsync(
        string? requestId,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return "Provide the onboarding request ID to reject.";
        }

        var rejectedBy = context.TryGetValue("rejectedBy", out var rejectedByObj)
            ? Convert.ToString(rejectedByObj)
            : null;
        var reason = context.TryGetValue("reason", out var reasonObj)
            ? Convert.ToString(reasonObj)
            : null;

        if (string.IsNullOrWhiteSpace(rejectedBy) || string.IsNullOrWhiteSpace(reason))
        {
            return "Rejecting a request requires both `rejectedBy` and `reason` in `additionalContext`.";
        }

        var rejected = await _onboardingService.RejectRequestAsync(requestId, rejectedBy!, reason!, cancellationToken);
        return rejected
            ? $"✅ Rejected onboarding request `{requestId}`. Reason: {reason}"
            : $"Unable to reject onboarding request `{requestId}`. Ensure it is pending review.";
    }

    private async Task<string> HandleProvisioningStatusAsync(
        string query,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        var jobId = context.TryGetValue("jobId", out var jobObj)
            ? Convert.ToString(jobObj)
            : ExtractProvisioningJobId(query);

        if (string.IsNullOrWhiteSpace(jobId))
        {
            return "Provide the provisioning job ID to check status (e.g., include the GUID or `jobId` in `additionalContext`).";
        }

        var status = await _onboardingService.GetProvisioningStatusAsync(jobId!, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"Provisioning status for job `{status.JobId}` (request `{status.RequestId}`):");
        sb.AppendLine($"Status: {status.Status} | {status.PercentComplete}% complete");
        if (!string.IsNullOrWhiteSpace(status.CurrentStep))
        {
            sb.AppendLine($"Current step: {status.CurrentStep}");
        }

        if (status.CompletedSteps.Any())
        {
            sb.AppendLine("Completed steps:");
            foreach (var step in status.CompletedSteps)
            {
                sb.AppendLine($"  - {step}");
            }
        }

        if (status.FailedSteps.Any())
        {
            sb.AppendLine("Failed steps:");
            foreach (var step in status.FailedSteps)
            {
                sb.AppendLine($"  - {step}");
            }
        }

        if (!string.IsNullOrWhiteSpace(status.ErrorMessage))
        {
            sb.AppendLine($"Error: {status.ErrorMessage}");
        }

        if (status.ProvisionedResources.Any())
        {
            sb.AppendLine("Provisioned resources:");
            foreach (var kvp in status.ProvisionedResources)
            {
                sb.AppendLine($"  - {kvp.Key}: {kvp.Value}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> HandleProvisioningRequestsAsync(CancellationToken cancellationToken)
    {
        var requests = await _onboardingService.GetProvisioningRequestsAsync(cancellationToken);
        return requests.Count > 0
            ? FormatRequestList("Requests currently in provisioning", requests)
            : "No onboarding requests are currently provisioning.";
    }

    private async Task<string> HandleStatsAsync(CancellationToken cancellationToken)
    {
        var stats = await _onboardingService.GetStatsAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("📊 Onboarding statistics");
        sb.AppendLine($"Total requests: {stats.TotalRequests} | Pending review: {stats.PendingReview} | Approved: {stats.Approved}");
        sb.AppendLine($"Rejected: {stats.Rejected} | Provisioning: {stats.InProvisioning} | Completed: {stats.Completed} | Failed: {stats.Failed}");
        sb.AppendLine($"Average approval time: {stats.AverageApprovalTimeHours:N1} hrs | Average provisioning time: {stats.AverageProvisioningTimeHours:N1} hrs");
        sb.AppendLine($"Success rate: {stats.SuccessRate:P1}");

        if (stats.Trends.Any())
        {
            sb.AppendLine("Recent trends:");
            foreach (var trend in stats.Trends.Take(5))
            {
                sb.AppendLine($"  {trend.Date:yyyy-MM-dd}: submitted {trend.RequestsSubmitted}, completed {trend.RequestsCompleted}, rejected {trend.RequestsRejected}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> HandleHistoryAsync(
        Dictionary<string, object?> context,
        CancellationToken cancellationToken)
    {
        var endDate = context.TryGetValue("endDate", out var endObj) && TryConvertToDateTime(endObj, out var parsedEnd)
            ? parsedEnd
            : DateTime.UtcNow;

        var startDate = context.TryGetValue("startDate", out var startObj) && TryConvertToDateTime(startObj, out var parsedStart)
            ? parsedStart
            : endDate.AddDays(-90);

        var history = await _onboardingService.GetHistoryAsync(startDate, endDate, cancellationToken);

        if (history.Count == 0)
        {
            return $"No onboarding activity recorded between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.";
        }

        return FormatRequestList($"Onboarding history from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}", history);
    }

    private static OnboardingIntent DetermineIntent(string normalizedQuery)
    {
        if (normalizedQuery.Contains("start onboarding") ||
            normalizedQuery.Contains("begin onboarding") ||
            normalizedQuery.Contains("create") ||
            normalizedQuery.Contains("start a new") ||
            normalizedQuery.Contains("new draft"))
        {
            return OnboardingIntent.CreateDraft;
        }

        if (normalizedQuery.Contains("update") || normalizedQuery.Contains("modify"))
        {
            return OnboardingIntent.UpdateDraft;
        }

        if (normalizedQuery.Contains("submit") || normalizedQuery.Contains("finalize"))
        {
            return OnboardingIntent.SubmitRequest;
        }

        if (normalizedQuery.Contains("cancel"))
        {
            return OnboardingIntent.CancelRequest;
        }

        if (normalizedQuery.Contains("approve"))
        {
            return OnboardingIntent.ApproveRequest;
        }

        if (normalizedQuery.Contains("reject"))
        {
            return OnboardingIntent.RejectRequest;
        }

        if (normalizedQuery.Contains("pending"))
        {
            return OnboardingIntent.ListPending;
        }

        if (normalizedQuery.Contains("owner") || normalizedQuery.Contains("my requests"))
        {
            return OnboardingIntent.ListOwnerRequests;
        }

        if (normalizedQuery.Contains("provisioning status") || normalizedQuery.Contains("job status"))
        {
            return OnboardingIntent.ProvisioningStatus;
        }

        if (normalizedQuery.Contains("provisioning") && normalizedQuery.Contains("requests"))
        {
            return OnboardingIntent.ProvisioningRequests;
        }

        if (normalizedQuery.Contains("stat") || normalizedQuery.Contains("metric") || normalizedQuery.Contains("dashboard"))
        {
            return OnboardingIntent.Stats;
        }

        if (normalizedQuery.Contains("history") || normalizedQuery.Contains("trend"))
        {
            return OnboardingIntent.History;
        }

        if (normalizedQuery.Contains("status") || normalizedQuery.Contains("progress") || normalizedQuery.Contains("check"))
        {
            return OnboardingIntent.CheckStatus;
        }

        return OnboardingIntent.Unknown;
    }

    private static Dictionary<string, object?> ParseContext(string? additionalContext)
    {
        if (string.IsNullOrWhiteSpace(additionalContext))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(additionalContext, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (raw == null)
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            var converted = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in raw)
            {
                converted[kvp.Key] = ConvertJsonElement(kvp.Value);
            }

            return converted;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var str = element.GetString();
                if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateTime))
                {
                    return dateTime;
                }

                if (Guid.TryParse(str, out var guid))
                {
                    return guid.ToString();
                }

                return str;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                {
                    return longValue;
                }

                if (element.TryGetDouble(out var doubleValue))
                {
                    return doubleValue;
                }

                return element.GetDecimal();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Object:
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return dict;
            }
            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }
                return list;
            }
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            default:
                return element.GetRawText();
        }
    }

    private static string? ExtractRequestId(string query)
    {
        var match = Regex.Match(query, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractProvisioningJobId(string query)
    {
        return ExtractRequestId(query);
    }

    private static string? ExtractEmailFromText(string normalizedQuery)
    {
        var match = Regex.Match(normalizedQuery, "[a-z0-9_.+-]+@[a-z0-9.-]+\\.[a-z]{2,}");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractEmailFromContext(Dictionary<string, object?> context)
    {
        return context.TryGetValue("ownerEmail", out var value) ? Convert.ToString(value) : null;
    }

    private static bool TryConvertToDateTime(object? value, out DateTime dateTime)
    {
        switch (value)
        {
            case DateTime dt:
                dateTime = dt;
                return true;
            case string str when DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed):
                dateTime = parsed;
                return true;
            default:
                dateTime = default;
                return false;
        }
    }

    private static string FormatRequestDetail(OnboardingRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Onboarding request `{request.Id}`");
        sb.AppendLine($"Status: {request.Status} | Mission: {request.MissionName}");
        sb.AppendLine($"Owner: {request.MissionOwner} ({request.MissionOwnerEmail}) | Command: {request.Command}");
        sb.AppendLine($"Classification: {request.ClassificationLevel} | Region: {request.Region}");
        sb.AppendLine($"Created: {request.CreatedAt:yyyy-MM-dd} | Last updated: {request.LastUpdatedAt:yyyy-MM-dd}");

        if (!string.IsNullOrWhiteSpace(request.ProvisioningJobId))
        {
            sb.AppendLine($"Provisioning job: {request.ProvisioningJobId}");
        }
        if (!string.IsNullOrWhiteSpace(request.ApprovedBy))
        {
            sb.AppendLine($"Approved by {request.ApprovedBy} at {request.ReviewedAt:yyyy-MM-dd}");
        }
        if (!string.IsNullOrWhiteSpace(request.RejectedBy))
        {
            sb.AppendLine($"Rejected by {request.RejectedBy}: {request.RejectionReason}");
        }
        if (!string.IsNullOrWhiteSpace(request.ProvisioningError))
        {
            sb.AppendLine($"Provisioning error: {request.ProvisioningError}");
        }

        return sb.ToString();
    }

    private static string FormatRequestList(string title, IReadOnlyCollection<OnboardingRequest> requests)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);

        foreach (var request in requests.OrderByDescending(r => r.LastUpdatedAt).Take(10))
        {
            sb.AppendLine($"- `{request.Id}` | {request.Status} | Mission: {request.MissionName} | Owner: {request.MissionOwnerEmail} | Updated {request.LastUpdatedAt:yyyy-MM-dd}");
        }

        if (requests.Count > 10)
        {
            sb.AppendLine($"(+ {requests.Count - 10} more requests)");
        }

        return sb.ToString();
    }

    private static string ProvideUsageGuidance()
    {
        var sb = new StringBuilder();
        sb.AppendLine("I can help with Flankspeed onboarding operations. Try queries like:");
        sb.AppendLine("- 'Start a new onboarding draft'");
        sb.AppendLine("- 'Update request <GUID> with new mission owner email' (include updates in additionalContext)");
        sb.AppendLine("- 'Submit onboarding request <GUID>'");
        sb.AppendLine("- 'Approve onboarding request <GUID>' (include approvedBy)");
        sb.AppendLine("- 'Show pending onboarding requests' or 'Show provisioning status for job <GUID>'");
        sb.AppendLine("- 'Show onboarding stats' or 'Show onboarding history' (include start/end dates if needed)");
        return sb.ToString();
    }

    private enum OnboardingIntent
    {
        Unknown,
        CreateDraft,
        UpdateDraft,
        SubmitRequest,
        CancelRequest,
        CheckStatus,
        ListPending,
        ListOwnerRequests,
        ApproveRequest,
        RejectRequest,
        ProvisioningStatus,
        ProvisioningRequests,
        Stats,
        History
    }
}
