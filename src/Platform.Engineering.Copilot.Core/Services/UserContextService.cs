using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Platform.Engineering.Copilot.Core.Services;

/// <summary>
/// Service for accessing current user context from HTTP requests.
/// Extracts user identity, roles, and permissions from JWT tokens issued by Azure AD.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current user's object ID (OID) from Azure AD.
    /// This is the unique identifier for the user in Azure AD.
    /// </summary>
    string GetCurrentUserId();

    /// <summary>
    /// Gets the current user's display name or UPN.
    /// </summary>
    string GetCurrentUserName();

    /// <summary>
    /// Gets the current user's email address.
    /// </summary>
    string? GetCurrentUserEmail();

    /// <summary>
    /// Checks if the current user has a specific permission claim.
    /// </summary>
    bool HasPermission(string permission);

    /// <summary>
    /// Checks if the current user is in a specific role.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Gets all roles assigned to the current user.
    /// </summary>
    IEnumerable<string> GetUserRoles();

    /// <summary>
    /// Gets the current user's tenant ID.
    /// </summary>
    string? GetTenantId();

    /// <summary>
    /// Checks if a user is authenticated.
    /// </summary>
    bool IsAuthenticated();
}

/// <summary>
/// Implementation of user context service using HttpContextAccessor.
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserContextService> _logger;

    public UserContextService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string GetCurrentUserId()
    {
        // Try Azure AD OID claim first
        var oid = User?.FindFirst("oid")?.Value;
        if (!string.IsNullOrEmpty(oid))
            return oid;

        // Fallback to standard NameIdentifier claim
        var nameIdentifier = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(nameIdentifier))
            return nameIdentifier;

        _logger.LogWarning("Unable to determine user ID from claims. User may not be authenticated.");
        return "System";
    }

    public string GetCurrentUserName()
    {
        // Try name claim
        var name = User?.FindFirst("name")?.Value;
        if (!string.IsNullOrEmpty(name))
            return name;

        // Try preferred_username (UPN)
        var preferredUsername = User?.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrEmpty(preferredUsername))
            return preferredUsername;

        // Try Identity.Name
        var identityName = User?.Identity?.Name;
        if (!string.IsNullOrEmpty(identityName))
            return identityName;

        return "Unknown";
    }

    public string? GetCurrentUserEmail()
    {
        // Try email claim
        var email = User?.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
            return email;

        // Try preferred_username as fallback (often contains email)
        var preferredUsername = User?.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrEmpty(preferredUsername) && preferredUsername.Contains("@"))
            return preferredUsername;

        return null;
    }

    public bool HasPermission(string permission)
    {
        if (User == null)
            return false;

        // Check if user has a claim matching the permission
        return User.HasClaim(c => c.Value == permission);
    }

    public bool IsInRole(string role)
    {
        if (User == null)
            return false;

        return User.IsInRole(role);
    }

    public IEnumerable<string> GetUserRoles()
    {
        if (User == null)
            return Enumerable.Empty<string>();

        // Azure AD puts roles in "roles" claim
        var roleClaims = User.FindAll("roles").Select(c => c.Value);
        
        // Also check standard role claim type
        var standardRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);

        return roleClaims.Concat(standardRoles).Distinct();
    }

    public string? GetTenantId()
    {
        // Azure AD tenant ID is in "tid" claim
        return User?.FindFirst("tid")?.Value;
    }

    public bool IsAuthenticated()
    {
        return User?.Identity?.IsAuthenticated ?? false;
    }
}
