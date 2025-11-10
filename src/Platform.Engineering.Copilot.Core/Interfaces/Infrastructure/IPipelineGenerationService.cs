using Platform.Engineering.Copilot.Core.Models.Infrastructure;

namespace Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;

/// <summary>
/// Service for generating CI/CD pipelines with embedded security and compliance checks
/// </summary>
public interface IPipelineGenerationService
{
    /// <summary>
    /// Generate a complete CI/CD pipeline with security checks
    /// </summary>
    Task<GeneratedPipeline> GeneratePipelineAsync(
        PipelineGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate GitHub Actions workflow
    /// </summary>
    Task<GeneratedPipeline> GenerateGitHubActionsWorkflowAsync(
        PipelineGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate Azure DevOps pipeline
    /// </summary>
    Task<GeneratedPipeline> GenerateAzureDevOpsPipelineAsync(
        PipelineGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available pipeline templates
    /// </summary>
    Task<List<PipelineTemplate>> GetAvailableTemplatesAsync(
        PipelineType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add security scanning stage to pipeline
    /// </summary>
    Task<string> AddSecurityScanningStageAsync(
        string pipelineContent,
        PipelineType type,
        List<SecurityTool> tools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add compliance gate to pipeline
    /// </summary>
    Task<string> AddComplianceGateAsync(
        string pipelineContent,
        PipelineType type,
        ComplianceGate gate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate pipeline configuration
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidatePipelineAsync(
        string pipelineContent,
        PipelineType type,
        CancellationToken cancellationToken = default);
}
