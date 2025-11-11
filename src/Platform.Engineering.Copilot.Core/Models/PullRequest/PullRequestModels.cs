namespace Platform.Engineering.Copilot.Core.Models.PullRequest;

/// <summary>
/// Pull request details from GitHub
/// </summary>
public class PullRequestDetails
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = string.Empty;
    public string HeadBranch { get; set; } = string.Empty;
    public string HeadSha { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<PullRequestFile> Files { get; set; } = new();
}

/// <summary>
/// File changed in a pull request
/// </summary>
public class PullRequestFile
{
    public string Filename { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // added, removed, modified, renamed
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int Changes { get; set; }
    public string? Patch { get; set; }
    public string RawUrl { get; set; } = string.Empty;
    public string ContentsUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Check if this is an Infrastructure as Code file
    /// </summary>
    public bool IsIaCFile()
    {
        var ext = Path.GetExtension(Filename).ToLowerInvariant();
        return ext == ".bicep" || 
               ext == ".tf" || 
               ext == ".json" && (Filename.Contains("template") || Filename.Contains("arm")) ||
               ext == ".yaml" || ext == ".yml" && Filename.Contains("k8s") ||
               Filename.EndsWith(".yaml") || Filename.EndsWith(".yml");
    }
}

/// <summary>
/// Review comment on a specific line of code
/// </summary>
public class PullRequestReviewComment
{
    public string Path { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Pull request review submission
/// </summary>
public class PullRequestReview
{
    public string Body { get; set; } = string.Empty;
    public ReviewEvent Event { get; set; }
    public List<PullRequestReviewComment>? Comments { get; set; }
}

/// <summary>
/// Review event types
/// </summary>
public enum ReviewEvent
{
    Approve,
    RequestChanges,
    Comment
}

/// <summary>
/// Commit status check
/// </summary>
public class CommitStatus
{
    public string State { get; set; } = string.Empty; // success, failure, error, pending
    public string? TargetUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Context { get; set; }
}

/// <summary>
/// Compliance finding on a specific line
/// </summary>
public class ComplianceFinding
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public ComplianceSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NistControl { get; set; }
    public string? StigId { get; set; }
    public string? DoDInstruction { get; set; }
    public string? Remediation { get; set; }
    public string? CodeSnippet { get; set; }
    public string? FixedCodeSnippet { get; set; }
}

/// <summary>
/// Severity level for compliance findings
/// </summary>
public enum ComplianceSeverity
{
    Critical,  // Blocks merge
    High,      // Requires review
    Medium,    // Warning
    Low        // Informational
}

/// <summary>
/// Result of PR compliance review
/// </summary>
public class PullRequestComplianceReview
{
    public int CriticalFindings { get; set; }
    public int HighFindings { get; set; }
    public int MediumFindings { get; set; }
    public int LowFindings { get; set; }
    public List<ComplianceFinding> Findings { get; set; } = new();
    public bool Approved => CriticalFindings == 0;
    public string Summary { get; set; } = string.Empty;
    
    public int TotalFindings => CriticalFindings + HighFindings + MediumFindings + LowFindings;
}
