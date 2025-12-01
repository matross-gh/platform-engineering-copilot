using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Audits;
using Platform.Engineering.Copilot.Core.Models.Audits;

namespace Platform.Engineering.Copilot.Mcp.Middleware;

/// <summary>
/// Middleware to audit all compliance-related API requests with user context.
/// Logs all access attempts to compliance endpoints for security and compliance tracking.
/// </summary>
public class ComplianceAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ComplianceAuthorizationMiddleware> _logger;

    public ComplianceAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<ComplianceAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuditLoggingService auditService)
    {
        // Only audit compliance-related endpoints
        if (IsComplianceEndpoint(context.Request.Path))
        {
            var user = context.User;
            var isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

            // Extract user information
            var actorId = user?.FindFirst("oid")?.Value 
                ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? "Anonymous";
            var actorName = user?.Identity?.Name ?? "Anonymous";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Log the access attempt
            try
            {
                await auditService.LogAsync(new AuditLogEntry
                {
                    EventType = "ComplianceApiAccess",
                    EventCategory = "ApiAccess",
                    ActorId = actorId,
                    ActorName = actorName,
                    ActorType = isAuthenticated ? "AuthenticatedUser" : "Anonymous",
                    Action = context.Request.Method,
                    ResourceId = context.Request.Path.ToString(),
                    ResourceType = "ComplianceApi",
                    Description = $"Access to compliance endpoint: {context.Request.Method} {context.Request.Path}",
                    Result = "Pending", // Will be updated after request completes
                    Severity = AuditSeverity.Informational,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Endpoint"] = context.Request.Path.ToString(),
                        ["Method"] = context.Request.Method,
                        ["QueryString"] = context.Request.QueryString.ToString(),
                        ["IsAuthenticated"] = isAuthenticated,
                        ["UserRoles"] = user?.FindAll("roles").Select(c => c.Value).ToArray() ?? Array.Empty<string>()
                    }.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? string.Empty),
                    ComplianceContext = new ComplianceContext
                    {
                        RequiresReview = !isAuthenticated, // Flag anonymous access for review
                        ControlIds = new List<string> { "AC-2", "AU-2", "AU-3" } // Access control and audit controls
                    }
                });
            }
            catch (Exception ex)
            {
                // Don't fail the request if auditing fails, but log the error
                _logger.LogError(ex, "Failed to audit compliance API access for {Path}", context.Request.Path);
            }
        }

        // Continue processing the request
        await _next(context);
    }

    /// <summary>
    /// Determines if the request path is a compliance-related endpoint.
    /// </summary>
    private static bool IsComplianceEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        return pathValue.Contains("/compliance") ||
               pathValue.Contains("/assessment") ||
               pathValue.Contains("/remediation") ||
               pathValue.Contains("/evidence") ||
               pathValue.Contains("/findings") ||
               pathValue.Contains("/ato");
    }
}

/// <summary>
/// Extension methods for registering the compliance authorization middleware.
/// </summary>
public static class ComplianceAuthorizationMiddlewareExtensions
{
    /// <summary>
    /// Adds the compliance authorization middleware to the application pipeline.
    /// Should be added after authentication and authorization middleware.
    /// </summary>
    public static IApplicationBuilder UseComplianceAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ComplianceAuthorizationMiddleware>();
    }
}
