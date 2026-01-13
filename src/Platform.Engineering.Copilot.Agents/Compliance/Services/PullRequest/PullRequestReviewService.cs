using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Models.PullRequest;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Services.PullRequest;

/// <summary>
/// Service for reviewing pull requests for compliance violations
/// Analyzes IaC files (Bicep, Terraform, ARM, Kubernetes) for NIST 800-53, STIG, and DoD compliance
/// </summary>
public class PullRequestReviewService
{
    private readonly ILogger<PullRequestReviewService> _logger;
    private readonly ICodeScanningEngine _codeScanningEngine;

    public PullRequestReviewService(
        ILogger<PullRequestReviewService> logger,
        ICodeScanningEngine codeScanningEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _codeScanningEngine = codeScanningEngine ?? throw new ArgumentNullException(nameof(codeScanningEngine));
    }

    /// <summary>
    /// Review all IaC files in a pull request for compliance violations
    /// </summary>
    public async Task<PullRequestComplianceReview> ReviewPullRequestAsync(
        PullRequestDetails pr,
        Dictionary<string, string> fileContents,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reviewing PR #{Number}: {Title} ({FileCount} files)", 
            pr.Number, pr.Title, fileContents.Count);

        var review = new PullRequestComplianceReview();
        var findings = new List<ComplianceFinding>();

        foreach (var kvp in fileContents)
        {
            var filename = kvp.Key;
            var content = kvp.Value;

            _logger.LogDebug("Analyzing {Filename} ({Length} bytes)", filename, content.Length);

            // Determine file type and scan accordingly
            if (filename.EndsWith(".bicep", StringComparison.OrdinalIgnoreCase))
            {
                findings.AddRange(await ScanBicepFileAsync(filename, content, cancellationToken));
            }
            else if (filename.EndsWith(".tf", StringComparison.OrdinalIgnoreCase))
            {
                findings.AddRange(await ScanTerraformFileAsync(filename, content, cancellationToken));
            }
            else if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && 
                     (filename.Contains("template") || filename.Contains("arm")))
            {
                findings.AddRange(await ScanArmTemplateAsync(filename, content, cancellationToken));
            }
            else if ((filename.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || 
                      filename.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
            {
                findings.AddRange(await ScanKubernetesFileAsync(filename, content, cancellationToken));
            }
        }

        // Categorize findings by severity
        review.Findings = findings;
        review.CriticalFindings = findings.Count(f => f.Severity == ComplianceSeverity.Critical);
        review.HighFindings = findings.Count(f => f.Severity == ComplianceSeverity.High);
        review.MediumFindings = findings.Count(f => f.Severity == ComplianceSeverity.Medium);
        review.LowFindings = findings.Count(f => f.Severity == ComplianceSeverity.Low);

        review.Summary = GenerateReviewSummary(review);

        _logger.LogInformation("Review complete: {Total} findings ({Critical} critical, {High} high, {Medium} medium, {Low} low)",
            review.TotalFindings, review.CriticalFindings, review.HighFindings, review.MediumFindings, review.LowFindings);

        return review;
    }

    /// <summary>
    /// Scan Bicep file for compliance violations
    /// </summary>
    private async Task<List<ComplianceFinding>> ScanBicepFileAsync(string filename, string content, CancellationToken cancellationToken)
    {
        var findings = new List<ComplianceFinding>();
        var lines = content.Split('\n');

        // Check for public IP addresses (AC-4 violation)
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            // Public IP check
            if (line.Contains("publicIPAddress", StringComparison.OrdinalIgnoreCase) && 
                !line.TrimStart().StartsWith("//"))
            {
                findings.Add(new ComplianceFinding
                {
                    FilePath = filename,
                    Line = lineNumber,
                    Severity = ComplianceSeverity.High,
                    Title = "Public IP address enabled",
                    Description = "Virtual machines should not have public IP addresses in IL5/IL6 environments",
                    NistControl = "AC-4 (Information Flow Enforcement)",
                    StigId = "V-219187",
                    DoDInstruction = "DoDI 8500.01 - Network boundary protection",
                    Remediation = "Remove publicIPAddress property or use private networking",
                    CodeSnippet = line.Trim(),
                    FixedCodeSnippet = "// publicIPAddress: null  // Remove public IP for IL5/IL6 compliance"
                });
            }

            // Storage encryption check
            if (line.Contains("kind: 'Storage'", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("kind: 'BlobStorage'", StringComparison.OrdinalIgnoreCase))
            {
                // Check next 20 lines for encryption setting
                bool hasEncryption = false;
                for (int j = i; j < Math.Min(i + 20, lines.Length); j++)
                {
                    if (lines[j].Contains("encryption:", StringComparison.OrdinalIgnoreCase) &&
                        lines[j].Contains("enabled: true", StringComparison.OrdinalIgnoreCase))
                    {
                        hasEncryption = true;
                        break;
                    }
                }

                if (!hasEncryption)
                {
                    findings.Add(new ComplianceFinding
                    {
                        FilePath = filename,
                        Line = lineNumber,
                        Severity = ComplianceSeverity.Critical,
                        Title = "Storage encryption not enabled",
                        Description = "Storage account must have encryption at rest enabled for IL5/IL6 compliance",
                        NistControl = "SC-28 (Protection of Information at Rest)",
                        StigId = "V-220001",
                        DoDInstruction = "DoDI 8500.01 - Encryption at rest requirement",
                        Remediation = "Add encryption configuration with enabled: true",
                        CodeSnippet = line.Trim(),
                        FixedCodeSnippet = @"properties: {
  encryption: {
    services: {
      blob: { enabled: true }
    }
  }
}"
                    });
                }
            }

            // TLS 1.2 minimum check
            if (line.Contains("minimumTlsVersion", StringComparison.OrdinalIgnoreCase))
            {
                if (!line.Contains("TLS1_2", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new ComplianceFinding
                    {
                        FilePath = filename,
                        Line = lineNumber,
                        Severity = ComplianceSeverity.High,
                        Title = "TLS 1.2 not enforced",
                        Description = "Storage accounts must enforce minimum TLS 1.2 for transit encryption",
                        NistControl = "SC-8 (Transmission Confidentiality)",
                        StigId = "V-220002",
                        DoDInstruction = "DoDI 8500.01 - Encryption in transit",
                        Remediation = "Set minimumTlsVersion to 'TLS1_2'",
                        CodeSnippet = line.Trim(),
                        FixedCodeSnippet = "minimumTlsVersion: 'TLS1_2'"
                    });
                }
            }

            // Public blob access check
            if (line.Contains("allowBlobPublicAccess", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains("true", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new ComplianceFinding
                    {
                        FilePath = filename,
                        Line = lineNumber,
                        Severity = ComplianceSeverity.Critical,
                        Title = "Public blob access enabled",
                        Description = "Storage accounts must not allow public blob access in IL5/IL6 environments",
                        NistControl = "AC-3 (Access Enforcement)",
                        StigId = "V-220003",
                        DoDInstruction = "DoDI 8500.01 - Access control requirements",
                        Remediation = "Set allowBlobPublicAccess to false",
                        CodeSnippet = line.Trim(),
                        FixedCodeSnippet = "allowBlobPublicAccess: false"
                    });
                }
            }
        }

        return await Task.FromResult(findings);
    }

    /// <summary>
    /// Scan Terraform file for compliance violations
    /// </summary>
    private async Task<List<ComplianceFinding>> ScanTerraformFileAsync(string filename, string content, CancellationToken cancellationToken)
    {
        var findings = new List<ComplianceFinding>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            // Public IP check
            if (Regex.IsMatch(line, @"associate_public_ip_address\s*=\s*true", RegexOptions.IgnoreCase))
            {
                findings.Add(new ComplianceFinding
                {
                    FilePath = filename,
                    Line = lineNumber,
                    Severity = ComplianceSeverity.High,
                    Title = "Public IP association enabled",
                    Description = "EC2/VM instances should not have public IP addresses in IL5/IL6 environments",
                    NistControl = "AC-4 (Information Flow Enforcement)",
                    StigId = "V-219187",
                    Remediation = "Set associate_public_ip_address = false",
                    CodeSnippet = line.Trim(),
                    FixedCodeSnippet = "associate_public_ip_address = false"
                });
            }

            // Encryption at rest check
            if (line.Contains("resource \"aws_s3_bucket\"") || line.Contains("resource \"azurerm_storage_account\""))
            {
                bool hasEncryption = false;
                for (int j = i; j < Math.Min(i + 30, lines.Length); j++)
                {
                    if (lines[j].Contains("server_side_encryption_configuration") ||
                        lines[j].Contains("encryption {"))
                    {
                        hasEncryption = true;
                        break;
                    }
                }

                if (!hasEncryption)
                {
                    findings.Add(new ComplianceFinding
                    {
                        FilePath = filename,
                        Line = lineNumber,
                        Severity = ComplianceSeverity.Critical,
                        Title = "Encryption at rest not configured",
                        Description = "Storage resources must have encryption at rest enabled",
                        NistControl = "SC-28 (Protection of Information at Rest)",
                        StigId = "V-220001",
                        Remediation = "Add server_side_encryption_configuration block",
                        CodeSnippet = line.Trim()
                    });
                }
            }
        }

        return await Task.FromResult(findings);
    }

    /// <summary>
    /// Scan ARM template for compliance violations
    /// </summary>
    private async Task<List<ComplianceFinding>> ScanArmTemplateAsync(string filename, string content, CancellationToken cancellationToken)
    {
        var findings = new List<ComplianceFinding>();
        
        // Check for public endpoints, encryption, etc. in ARM JSON
        if (content.Contains("\"publicIPAddress\"") && !content.Contains("//"))
        {
            findings.Add(new ComplianceFinding
            {
                FilePath = filename,
                Line = 1, // JSON parsing would be needed for exact line
                Severity = ComplianceSeverity.High,
                Title = "Public IP address detected in ARM template",
                Description = "ARM template contains public IP address configuration",
                NistControl = "AC-4 (Information Flow Enforcement)",
                StigId = "V-219187",
                Remediation = "Remove public IP address from template"
            });
        }

        return await Task.FromResult(findings);
    }

    /// <summary>
    /// Scan Kubernetes YAML for compliance violations
    /// </summary>
    private async Task<List<ComplianceFinding>> ScanKubernetesFileAsync(string filename, string content, CancellationToken cancellationToken)
    {
        var findings = new List<ComplianceFinding>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            // Check for privileged containers
            if (line.Contains("privileged: true", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new ComplianceFinding
                {
                    FilePath = filename,
                    Line = lineNumber,
                    Severity = ComplianceSeverity.Critical,
                    Title = "Privileged container detected",
                    Description = "Containers should not run in privileged mode",
                    NistControl = "CM-7 (Least Functionality)",
                    StigId = "V-242376",
                    Remediation = "Set privileged: false or remove the property",
                    CodeSnippet = line.Trim(),
                    FixedCodeSnippet = "privileged: false"
                });
            }

            // Check for hostNetwork
            if (line.Contains("hostNetwork: true", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new ComplianceFinding
                {
                    FilePath = filename,
                    Line = lineNumber,
                    Severity = ComplianceSeverity.High,
                    Title = "Host network mode enabled",
                    Description = "Pods should not use host network namespace",
                    NistControl = "SC-7 (Boundary Protection)",
                    StigId = "V-242377",
                    Remediation = "Remove hostNetwork or set to false",
                    CodeSnippet = line.Trim()
                });
            }
        }

        return await Task.FromResult(findings);
    }

    /// <summary>
    /// Generate review summary text
    /// </summary>
    private string GenerateReviewSummary(PullRequestComplianceReview review)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 🔒 Compliance Review - Platform Engineering Copilot");
        sb.AppendLine();

        if (review.Approved)
        {
            sb.AppendLine("**Status:** ✅ **Approved** - No critical compliance violations");
        }
        else
        {
            sb.AppendLine("**Status:** ⚠️ **Changes Requested** - Critical violations must be fixed");
        }

        sb.AppendLine();
        sb.AppendLine($"**Issues Found:** {review.TotalFindings} total");
        sb.AppendLine($"- 🔴 Critical: {review.CriticalFindings}");
        sb.AppendLine($"- 🟠 High: {review.HighFindings}");
        sb.AppendLine($"- 🟡 Medium: {review.MediumFindings}");
        sb.AppendLine($"- ⚪ Low: {review.LowFindings}");
        sb.AppendLine();

        if (review.CriticalFindings > 0)
        {
            sb.AppendLine("### 🔴 Critical Issues (Must Fix)");
            sb.AppendLine();
            foreach (var finding in review.Findings.Where(f => f.Severity == ComplianceSeverity.Critical).Take(5))
            {
                sb.AppendLine($"**{finding.Title}**");
                sb.AppendLine($"- File: `{finding.FilePath}` Line {finding.Line}");
                sb.AppendLine($"- Control: {finding.NistControl}");
                if (!string.IsNullOrEmpty(finding.StigId))
                    sb.AppendLine($"- STIG: {finding.StigId}");
                sb.AppendLine($"- Fix: {finding.Remediation}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine("*This review is advisory only (Phase 1 compliance). Manual approval required before merge.*");

        return sb.ToString();
    }

    /// <summary>
    /// Generate inline PR comment for a specific finding
    /// </summary>
    public string GenerateFindingComment(ComplianceFinding finding)
    {
        var sb = new StringBuilder();
        
        // Severity emoji
        var emoji = finding.Severity switch
        {
            ComplianceSeverity.Critical => "🔴",
            ComplianceSeverity.High => "🟠",
            ComplianceSeverity.Medium => "🟡",
            ComplianceSeverity.Low => "⚪",
            _ => "ℹ️"
        };

        sb.AppendLine($"{emoji} **{finding.Title}**");
        sb.AppendLine();
        sb.AppendLine(finding.Description);
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(finding.NistControl))
        {
            sb.AppendLine($"**NIST Control:** {finding.NistControl}");
        }
        
        if (!string.IsNullOrEmpty(finding.StigId))
        {
            sb.AppendLine($"**STIG:** {finding.StigId}");
        }
        
        if (!string.IsNullOrEmpty(finding.DoDInstruction))
        {
            sb.AppendLine($"**DoD Instruction:** {finding.DoDInstruction}");
        }

        sb.AppendLine();
        sb.AppendLine("**How to fix:**");
        sb.AppendLine($"{finding.Remediation}");

        if (!string.IsNullOrEmpty(finding.FixedCodeSnippet))
        {
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(finding.FixedCodeSnippet);
            sb.AppendLine("```");
        }

        return sb.ToString();
    }
}
