namespace Platform.Engineering.Copilot.Core.Authorization;

/// <summary>
/// Defines granular permissions for compliance operations.
/// These can be used as custom claims in authorization policies.
/// </summary>
public static class CompliancePermissions
{
    // Assessment permissions
    public const string RunAssessment = "Compliance.Assessment.Run";
    public const string ViewAssessment = "Compliance.Assessment.View";
    public const string DeleteAssessment = "Compliance.Assessment.Delete";
    public const string ExportAssessment = "Compliance.Assessment.Export";

    // Remediation permissions
    public const string ExecuteRemediation = "Compliance.Remediation.Execute";
    public const string ApproveRemediation = "Compliance.Remediation.Approve";
    public const string ViewRemediation = "Compliance.Remediation.View";

    // Evidence permissions
    public const string CollectEvidence = "Compliance.Evidence.Collect";
    public const string ExportEvidence = "Compliance.Evidence.Export";
    public const string DeleteEvidence = "Compliance.Evidence.Delete";
    public const string ViewEvidence = "Compliance.Evidence.View";

    // Document permissions
    public const string GenerateDocuments = "Compliance.Documents.Generate";
    public const string ExportDocuments = "Compliance.Documents.Export";
    public const string ViewDocuments = "Compliance.Documents.View";

    // Finding permissions
    public const string ViewFindings = "Compliance.Findings.View";
    public const string UpdateFindings = "Compliance.Findings.Update";
    public const string DeleteFindings = "Compliance.Findings.Delete";

    /// <summary>
    /// Gets all defined compliance permissions.
    /// </summary>
    public static readonly string[] AllPermissions = new[]
    {
        RunAssessment, ViewAssessment, DeleteAssessment, ExportAssessment,
        ExecuteRemediation, ApproveRemediation, ViewRemediation,
        CollectEvidence, ExportEvidence, DeleteEvidence, ViewEvidence,
        GenerateDocuments, ExportDocuments, ViewDocuments,
        ViewFindings, UpdateFindings, DeleteFindings
    };
}
