using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Engineering.Copilot.Core.Data.Entities;

/// <summary>
/// Environment metrics entity for performance tracking
/// </summary>
[Table("EnvironmentMetrics")]
public class EnvironmentMetrics
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid DeploymentId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string MetricType { get; set; } = string.Empty; // cpu, memory, requests, errors, latency
    
    [Required]
    [StringLength(50)]
    public string MetricName { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }
    
    [StringLength(20)]
    public string? Unit { get; set; } // %, MB, ms, count
    
    public DateTime Timestamp { get; set; }
    
    [StringLength(50)]
    public string? Source { get; set; } // azure-monitor, application-insights, custom
    
    public string? Labels { get; set; } // JSON key-value pairs for metric labels
    
    // Navigation properties
    [ForeignKey("DeploymentId")]
    public virtual EnvironmentDeployment Deployment { get; set; } = null!;
}
