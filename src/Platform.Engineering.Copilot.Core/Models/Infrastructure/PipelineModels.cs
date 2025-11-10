using System;
using System.Collections.Generic;

namespace Platform.Engineering.Copilot.Core.Models.Infrastructure;

/// <summary>
/// Pipeline type (GitHub Actions, Azure DevOps, GitLab CI)
/// </summary>
public enum PipelineType
{
    GitHubActions,
    AzureDevOps,
    GitLabCI
}

/// <summary>
/// Pipeline stage type
/// </summary>
public enum PipelineStage
{
    Build,
    Test,
    SecurityScan,
    ComplianceScan,
    Deploy,
    Validate
}

/// <summary>
/// Security scanning tool
/// </summary>
public enum SecurityTool
{
    TfSec,           // Terraform security scanner
    Checkov,         // IaC security scanner (multi-cloud)
    Terrascan,       // IaC security scanner
    TruffleHog,      // Secret scanner
    Gitleaks,        // Secret scanner
    Trivy,           // Container scanner
    AquaSecurity,    // Container scanner
    Snyk,            // Dependency scanner
    SonarQube        // Code quality scanner
}

/// <summary>
/// Pipeline generation request
/// </summary>
public class PipelineGenerationRequest
{
    public PipelineType Type { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public Compliance.ImpactLevel ImpactLevel { get; set; }
    public List<PipelineStage> Stages { get; set; } = new();
    public List<SecurityTool> SecurityTools { get; set; } = new();
    public bool IncludeStigChecks { get; set; }
    public bool IncludeComplianceGates { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public List<string> AzureSubscriptions { get; set; } = new();
}

/// <summary>
/// Generated pipeline
/// </summary>
public class GeneratedPipeline
{
    public PipelineType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<PipelineStage> Stages { get; set; } = new();
    public List<SecurityCheck> SecurityChecks { get; set; } = new();
    public List<ComplianceGate> ComplianceGates { get; set; } = new();
    public string SetupInstructions { get; set; } = string.Empty;
    public Dictionary<string, string> RequiredSecrets { get; set; } = new();
}

/// <summary>
/// Security check in pipeline
/// </summary>
public class SecurityCheck
{
    public string Name { get; set; } = string.Empty;
    public SecurityTool Tool { get; set; }
    public string Description { get; set; } = string.Empty;
    public PipelineStage Stage { get; set; }
    public bool IsBlocking { get; set; }
    public Dictionary<string, string> Configuration { get; set; } = new();
}

/// <summary>
/// Compliance gate in pipeline
/// </summary>
public class ComplianceGate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredChecks { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public List<string> Approvers { get; set; } = new();
    public int MinimumApprovals { get; set; }
}

/// <summary>
/// Pipeline template configuration
/// </summary>
public class PipelineTemplate
{
    public string Name { get; set; } = string.Empty;
    public PipelineType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public List<string> RequiredTools { get; set; } = new();
    public Dictionary<string, string> DefaultVariables { get; set; } = new();
}
