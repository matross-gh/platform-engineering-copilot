namespace Platform.Engineering.Copilot.Core.Models.Azure;

/// <summary>
/// Summary of resource health across a subscription or resource group
/// </summary>
public class ResourceHealthSummary
{
    /// <summary>
    /// Subscription ID
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Resource group name (optional, null if subscription-level)
    /// </summary>
    public string? ResourceGroup { get; set; }

    /// <summary>
    /// Total number of resources
    /// </summary>
    public int TotalResources { get; set; }

    /// <summary>
    /// Number of healthy resources
    /// </summary>
    public int HealthyResources { get; set; }

    /// <summary>
    /// Number of resources with warnings
    /// </summary>
    public int WarningResources { get; set; }

    /// <summary>
    /// Number of unhealthy resources
    /// </summary>
    public int UnhealthyResources { get; set; }

    /// <summary>
    /// Number of resources with unknown health
    /// </summary>
    public int UnknownResources { get; set; }

    /// <summary>
    /// Overall health percentage (0-100)
    /// </summary>
    public double HealthPercentage => TotalResources > 0 
        ? (double)HealthyResources / TotalResources * 100 
        : 0;

    /// <summary>
    /// Timestamp of the summary
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// List of resource health statuses
    /// </summary>
    public List<ResourceHealthStatus> Resources { get; set; } = new();
}

/// <summary>
/// Health status of an individual Azure resource
/// </summary>
public class ResourceHealthStatus
{
    /// <summary>
    /// Resource ID
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Resource name
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// Resource type
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Resource location
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Health state (Available, Degraded, Unavailable, Unknown)
    /// </summary>
    public string HealthState { get; set; } = "Unknown";

    /// <summary>
    /// Detailed status message
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Reason for current health state
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Timestamp of last status check
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the resource is in a healthy state
    /// </summary>
    public bool IsHealthy => HealthState == "Available";

    /// <summary>
    /// Related alerts for this resource
    /// </summary>
    public List<ResourceHealthAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Alert or issue affecting resource health
/// </summary>
public class ResourceHealthAlert
{
    /// <summary>
    /// Alert ID
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    /// Alert severity (Critical, Warning, Informational)
    /// </summary>
    public string Severity { get; set; } = "Informational";

    /// <summary>
    /// Alert title/summary
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed alert description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the alert was triggered
    /// </summary>
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the alert is still active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Recommended action to resolve the alert
    /// </summary>
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Dashboard view of resource health across multiple dimensions
/// </summary>
public class ResourceHealthDashboard
{
    /// <summary>
    /// Overall summary
    /// </summary>
    public ResourceHealthSummary Summary { get; set; } = new();

    /// <summary>
    /// Health breakdown by resource type
    /// </summary>
    public Dictionary<string, ResourceHealthSummary> ByResourceType { get; set; } = new();

    /// <summary>
    /// Health breakdown by location
    /// </summary>
    public Dictionary<string, ResourceHealthSummary> ByLocation { get; set; } = new();

    /// <summary>
    /// Active alerts across all resources
    /// </summary>
    public List<ResourceHealthAlert> ActiveAlerts { get; set; } = new();

    /// <summary>
    /// Trending data (health over time)
    /// </summary>
    public List<HealthTrendDataPoint> HealthTrend { get; set; } = new();

    /// <summary>
    /// Timestamp of dashboard generation
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Data point for health trending
/// </summary>
public class HealthTrendDataPoint
{
    /// <summary>
    /// Timestamp of the data point
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Health percentage at this point in time
    /// </summary>
    public double HealthPercentage { get; set; }

    /// <summary>
    /// Number of total resources
    /// </summary>
    public int TotalResources { get; set; }

    /// <summary>
    /// Number of healthy resources
    /// </summary>
    public int HealthyResources { get; set; }
}
