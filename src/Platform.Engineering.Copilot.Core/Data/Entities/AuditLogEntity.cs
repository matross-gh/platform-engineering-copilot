using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Platform.Engineering.Copilot.Core.Data.Entities;

/// <summary>
/// Entity Framework entity for persisting audit logs to the database
/// Supports compliance requirements for NIST 800-53 (AU-2, AU-3, AU-9)
/// </summary>
[Table("AuditLogs")]
[Index(nameof(Timestamp), Name = "IX_AuditLogs_Timestamp")]
[Index(nameof(EventType), Name = "IX_AuditLogs_EventType")]
[Index(nameof(ActorId), Name = "IX_AuditLogs_ActorId")]
[Index(nameof(ResourceId), Name = "IX_AuditLogs_ResourceId")]
[Index(nameof(Severity), Name = "IX_AuditLogs_Severity")]
[Index(nameof(CorrelationId), Name = "IX_AuditLogs_CorrelationId")]
public class AuditLogEntity
{
    /// <summary>
    /// Primary key - uniquely identifies each audit log entry
    /// </summary>
    [Key]
    [MaxLength(50)]
    public string EntryId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the audited event occurred (UTC)
    /// NIST 800-53 AU-3: Date and time of event
    /// </summary>
    [Required]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Type of event (e.g., "UserLogin", "ResourceCreated", "ConfigurationChanged")
    /// NIST 800-53 AU-3: Type of event
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Category of event (e.g., "Authentication", "Authorization", "DataAccess")
    /// </summary>
    [MaxLength(50)]
    public string EventCategory { get; set; } = string.Empty;

    /// <summary>
    /// Severity level for alerting and compliance
    /// </summary>
    [Required]
    public int Severity { get; set; } // Stored as int: 0=Informational, 1=Low, 2=Medium, 3=High, 4=Critical

    /// <summary>
    /// User/service/system identifier who performed the action
    /// NIST 800-53 AU-3: Identity of individual/subject
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the actor (user name, service name)
    /// </summary>
    [MaxLength(200)]
    public string ActorName { get; set; } = string.Empty;

    /// <summary>
    /// Type of actor (User, System, Service, Application)
    /// </summary>
    [MaxLength(50)]
    public string ActorType { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the resource affected by the action
    /// NIST 800-53 AU-3: Object identity
    /// </summary>
    [MaxLength(500)]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Type of resource (e.g., "VirtualMachine", "StorageAccount", "ComplianceReport")
    /// </summary>
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the resource
    /// </summary>
    [MaxLength(500)]
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// Action performed (e.g., "Create", "Read", "Update", "Delete", "Execute")
    /// NIST 800-53 AU-3: Event outcome
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what happened
    /// </summary>
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Result of the action (Success, Failed, Partial)
    /// NIST 800-53 AU-3: Event outcome (success or failure)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Reason for failure (if Result = Failed)
    /// </summary>
    [MaxLength(1000)]
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the actor
    /// NIST 800-53 AU-3: Source of event (network address)
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent header (browser/client information)
    /// </summary>
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Session identifier for grouping related actions
    /// </summary>
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for distributed tracing across services
    /// </summary>
    [MaxLength(100)]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata as JSON
    /// Stores custom fields, request details, etc.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Tags for categorization and filtering (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? TagsJson { get; set; }

    /// <summary>
    /// Change details (before/after values) as JSON
    /// Stores old values, new values, changed fields
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ChangeDetailsJson { get; set; }

    /// <summary>
    /// Compliance context as JSON
    /// Stores control IDs, framework, violations, review requirements
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ComplianceContextJson { get; set; }

    /// <summary>
    /// Security context as JSON
    /// Stores threat level, security policies, MFA requirements, permissions
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SecurityContextJson { get; set; }

    /// <summary>
    /// When this audit log was archived (for retention management)
    /// </summary>
    public DateTimeOffset? ArchivedAt { get; set; }

    /// <summary>
    /// Whether this audit log has been archived to cold storage
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Hash of the audit log entry for tamper detection
    /// NIST 800-53 AU-9: Protection of audit information
    /// </summary>
    [MaxLength(64)] // SHA-256 hash
    public string? EntryHash { get; set; }

    /// <summary>
    /// Version number for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
