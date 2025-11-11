using System;
using System.Collections.Generic;

namespace Platform.Engineering.Copilot.Core.Models.KnowledgeBase;

/// <summary>
/// Represents Risk Management Framework (RMF) process information
/// </summary>
public class RmfProcess
{
    public string Step { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Activities { get; set; } = new();
    public List<string> Outputs { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public string DodInstruction { get; set; } = string.Empty;
}

/// <summary>
/// Represents a STIG (Security Technical Implementation Guide) control
/// </summary>
public class StigControl
{
    public string StigId { get; set; } = string.Empty;
    public string VulnId { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StigSeverity Severity { get; set; }
    public string CheckText { get; set; } = string.Empty;
    public string FixText { get; set; } = string.Empty;
    public List<string> NistControls { get; set; } = new();
    public List<string> CciRefs { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public string StigFamily { get; set; } = string.Empty;
    public StigServiceType ServiceType { get; set; }
    public Dictionary<string, string> AzureImplementation { get; set; } = new();
}

/// <summary>
/// STIG severity levels
/// </summary>
public enum StigSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Azure service type categories for STIG organization
/// </summary>
public enum StigServiceType
{
    Compute,
    Network,
    Storage,
    Database,
    Identity,
    Monitoring,
    Security,
    Platform,
    Integration,
    Analytics,
    Containers,
    Other
}

/// <summary>
/// Represents a DoD Instruction reference
/// </summary>
public class DoDInstruction
{
    public string InstructionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime PublicationDate { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> RelatedNistControls { get; set; } = new();
    public List<string> RelatedStigIds { get; set; } = new();
    public string Applicability { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed NIST control mappings with section references and requirements
    /// </summary>
    public List<DoDControlMapping> ControlMappings { get; set; } = new();
}

/// <summary>
/// Mapping between a DoD Instruction and specific NIST 800-53 controls
/// </summary>
public class DoDControlMapping
{
    /// <summary>
    /// NIST 800-53 control ID (e.g., "AC-2", "SC-7")
    /// </summary>
    public string NistControlId { get; set; } = string.Empty;
    
    /// <summary>
    /// Specific section within the DoD Instruction (e.g., "Enclosure 3, Section 2.a")
    /// </summary>
    public string Section { get; set; } = string.Empty;
    
    /// <summary>
    /// The specific requirement text from the instruction
    /// </summary>
    public string Requirement { get; set; } = string.Empty;
    
    /// <summary>
    /// Impact levels where this requirement applies (e.g., "ALL", "IL4,IL5,IL6")
    /// </summary>
    public string ImpactLevel { get; set; } = "ALL";
}

/// <summary>
/// Represents Navy/DoD workflow documentation
/// </summary>
public class DoDWorkflow
{
    public string WorkflowId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DoDOrganization Organization { get; set; }
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<string> RequiredDocuments { get; set; } = new();
    public List<string> ApprovalAuthorities { get; set; } = new();
    public string ImpactLevel { get; set; } = string.Empty;
}

/// <summary>
/// DoD organizations
/// </summary>
public enum DoDOrganization
{
    Navy,
    PMW,
    SPAWAR,
    NAVWAR,
    NIWC,
    DISA,
    CYBERCOM,
    Other
}

/// <summary>
/// Represents a workflow step
/// </summary>
public class WorkflowStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Deliverables { get; set; } = new();
    public string EstimatedDuration { get; set; } = string.Empty;
    public List<string> Prerequisites { get; set; } = new();
}

/// <summary>
/// Impact Level (IL) classification information
/// </summary>
public class ImpactLevel
{
    public string Level { get; set; } = string.Empty; // IL2, IL4, IL5, IL6
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public List<string> NistBaseline { get; set; } = new();
    public List<string> MandatoryControls { get; set; } = new();
    public Dictionary<string, string> AzureConfigurations { get; set; } = new();
}

/// <summary>
/// Control mapping between NIST, STIG, and DoD instructions
/// </summary>
public class ControlMapping
{
    public string NistControlId { get; set; } = string.Empty;
    public List<string> StigIds { get; set; } = new();
    public List<string> CciIds { get; set; } = new();
    public List<string> DoDInstructions { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> ImplementationGuidance { get; set; } = new();
}

/// <summary>
/// RMF authorization (ATO) package information
/// </summary>
public class AtoPackageRequirement
{
    public string DocumentType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string ImpactLevel { get; set; } = string.Empty;
    public List<string> Sections { get; set; } = new();
    public string Template { get; set; } = string.Empty;
    public string ResponsibleRole { get; set; } = string.Empty;
}

/// <summary>
/// eMASS (Enterprise Mission Assurance Support Service) integration data
/// </summary>
public class EmassRequirement
{
    public string SystemId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string RegistrationType { get; set; } = string.Empty;
    public List<string> RequiredArtifacts { get; set; } = new();
    public List<string> TestProcedures { get; set; } = new();
    public Dictionary<string, string> ComplianceMapping { get; set; } = new();
}

/// <summary>
/// CCRI (Cloud Computing Risk and Information) assessment data
/// </summary>
public class CcriRequirement
{
    public string RequirementId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> TestProcedures { get; set; } = new();
    public List<string> EvidenceTypes { get; set; } = new();
    public List<string> RelatedNistControls { get; set; } = new();
    public string AutomationAvailable { get; set; } = string.Empty;
}

/// <summary>
/// Knowledge base search result
/// </summary>
public class KnowledgeBaseSearchResult
{
    public string Type { get; set; } = string.Empty; // "RMF", "STIG", "DoD Instruction", "Workflow"
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// IL5/IL6 boundary protection requirements
/// </summary>
public class BoundaryProtectionRequirement
{
    public string ImpactLevel { get; set; } = string.Empty;
    public string RequirementId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> MandatoryControls { get; set; } = new();
    public List<string> NetworkRequirements { get; set; } = new();
    public List<string> EncryptionRequirements { get; set; } = new();
    public Dictionary<string, string> AzureImplementation { get; set; } = new();
}
