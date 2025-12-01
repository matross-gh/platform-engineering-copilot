namespace Platform.Engineering.Copilot.Core.Authorization;

/// <summary>
/// Defines role-based access control roles for compliance operations.
/// These roles should be configured as App Roles in Azure AD App Registration.
/// </summary>
public static class ComplianceRoles
{
    /// <summary>
    /// Full administrative access to all compliance operations.
    /// Can execute remediations, delete assessments, export evidence, and manage all compliance features.
    /// </summary>
    public const string Administrator = "Compliance.Administrator";

    /// <summary>
    /// Can view and audit compliance data, export evidence packages.
    /// Cannot execute remediations or delete assessments.
    /// </summary>
    public const string Auditor = "Compliance.Auditor";

    /// <summary>
    /// Can run assessments and execute approved remediations.
    /// Cannot delete assessments or export sensitive evidence.
    /// </summary>
    public const string Analyst = "Compliance.Analyst";

    /// <summary>
    /// Read-only access to compliance reports and findings.
    /// Cannot execute any modifications or exports.
    /// </summary>
    public const string ReadOnly = "Compliance.ReadOnly";

    /// <summary>
    /// Gets all defined compliance roles.
    /// </summary>
    public static readonly string[] AllRoles = new[]
    {
        Administrator,
        Auditor,
        Analyst,
        ReadOnly
    };
}
