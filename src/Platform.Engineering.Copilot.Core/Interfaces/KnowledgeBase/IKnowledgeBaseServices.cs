using Platform.Engineering.Copilot.Core.Models.KnowledgeBase;

namespace Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;

/// <summary>
/// Service for RMF process and documentation guidance
/// </summary>
public interface IRmfKnowledgeService
{
    Task<RmfProcess?> GetRmfStepAsync(string step, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RmfProcess>> GetAllRmfStepsAsync(CancellationToken cancellationToken = default);
    Task<string> ExplainRmfProcessAsync(string? specificStep = null, CancellationToken cancellationToken = default);
    Task<List<string>> GetRmfOutputsForStepAsync(string step, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for STIG controls and compliance checking
/// </summary>
public interface IStigKnowledgeService
{
    Task<StigControl?> GetStigControlAsync(string stigId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StigControl>> GetStigsByNistControlAsync(string nistControlId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StigControl>> SearchStigsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StigControl>> GetStigsBySeverityAsync(StigSeverity severity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StigControl>> GetAllStigsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StigControl>> GetStigsByServiceTypeAsync(StigServiceType serviceType, CancellationToken cancellationToken = default);
    Task<ControlMapping?> GetControlMappingAsync(string nistControlId, CancellationToken cancellationToken = default);
    Task<string> ExplainStigAsync(string stigId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for DoD instructions and policy guidance
/// </summary>
public interface IDoDInstructionService
{
    Task<DoDInstruction?> GetInstructionAsync(string instructionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoDInstruction>> GetInstructionsByControlAsync(string nistControlId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoDInstruction>> SearchInstructionsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<string> ExplainInstructionAsync(string instructionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for Navy/DoD workflow documentation
/// </summary>
public interface IDoDWorkflowService
{
    Task<DoDWorkflow?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoDWorkflow>> GetWorkflowsByOrganizationAsync(DoDOrganization organization, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoDWorkflow>> GetWorkflowsByImpactLevelAsync(string impactLevel, CancellationToken cancellationToken = default);
    Task<string> ExplainWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for Impact Level requirements and guidance
/// </summary>
public interface IImpactLevelService
{
    Task<ImpactLevel?> GetImpactLevelAsync(string level, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImpactLevel>> GetAllImpactLevelsAsync(CancellationToken cancellationToken = default);
    Task<BoundaryProtectionRequirement?> GetBoundaryRequirementsAsync(string impactLevel, CancellationToken cancellationToken = default);
    Task<string> ExplainImpactLevelAsync(string level, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unified knowledge base service for all DoD/Navy compliance information
/// </summary>
public interface IComplianceKnowledgeBaseService
{
    Task<IReadOnlyList<KnowledgeBaseSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<string> ExplainControlAsync(string nistControlId, bool includeRmf = true, bool includeStig = true, bool includeDoDInstructions = true, CancellationToken cancellationToken = default);
    Task<AtoPackageRequirement?> GetAtoRequirementAsync(string documentType, string impactLevel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtoPackageRequirement>> GetAtoPackageRequirementsAsync(string impactLevel, CancellationToken cancellationToken = default);
    Task<EmassRequirement?> GetEmassRequirementsAsync(string systemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CcriRequirement>> GetCcriRequirementsAsync(CancellationToken cancellationToken = default);
}
