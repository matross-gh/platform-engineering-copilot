using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Engineering.Copilot.Compliance.Core.Data.Entities;

/// <summary>
/// Represents a complete ATO compliance assessment
/// </summary>
[Table("ComplianceAssessments")]
public class ComplianceAssessment
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(100)]
    public string SubscriptionId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ResourceGroupName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AssessmentType { get; set; } = "NIST-800-53"; // NIST-800-53, FedRAMP, DoD SRG, etc.
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "InProgress"; // InProgress, Completed, Failed, Cancelled
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal ComplianceScore { get; set; } // 0.00 to 100.00
    
    public int TotalFindings { get; set; }
    public int CriticalFindings { get; set; }
    public int HighFindings { get; set; }
    public int MediumFindings { get; set; }
    public int LowFindings { get; set; }
    public int InformationalFindings { get; set; }
    
    public string? ExecutiveSummary { get; set; }
    public string? RiskProfile { get; set; } // JSON serialized risk assessment
    public string? Results { get; set; } // JSON detailed assessment results
    public string? Recommendations { get; set; } // JSON remediation recommendations
    public string? Metadata { get; set; } // JSON additional assessment metadata
    
    [Required]
    [MaxLength(100)]
    public string InitiatedBy { get; set; } = string.Empty;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    
    // Navigation properties
    public virtual ICollection<ComplianceFinding> Findings { get; set; } = new List<ComplianceFinding>();
}

/// <summary>
/// Individual compliance finding within an assessment
/// </summary>
[Table("ComplianceFindings")]
public class ComplianceFinding
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string AssessmentId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FindingId { get; set; } = string.Empty; // Unique within assessment
    
    [Required]
    [MaxLength(100)]
    public string RuleId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low, Informational
    
    [Required]
    [MaxLength(30)]
    public string ComplianceStatus { get; set; } = string.Empty; // Compliant, NonCompliant, PartiallyCompliant, etc.
    
    [Required]
    [MaxLength(30)]
    public string FindingType { get; set; } = string.Empty; // Security, Configuration, etc.
    
    [MaxLength(500)]
    public string? ResourceId { get; set; }
    
    [MaxLength(100)]
    public string? ResourceType { get; set; }
    
    [MaxLength(200)]
    public string? ResourceName { get; set; }
    
    [MaxLength(100)]
    public string? ControlId { get; set; } // NIST control ID, ISO control ID, etc.
    
    public string? ComplianceFrameworks { get; set; } // JSON array of frameworks
    public string? AffectedNistControls { get; set; } // JSON array of NIST control IDs
    public string? Evidence { get; set; } // JSON evidence data
    public string? Remediation { get; set; } // Remediation guidance
    public string? Metadata { get; set; } // JSON additional finding metadata
    
    public bool IsRemediable { get; set; } = false;
    public bool IsAutomaticallyFixable { get; set; } = false;
    
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("AssessmentId")]
    public virtual ComplianceAssessment Assessment { get; set; } = null!;
}