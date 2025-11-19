using System.ComponentModel.DataAnnotations;

namespace Platform.Engineering.Copilot.CostManagement.Core.Configuration;

/// <summary>
/// Configuration options for the Cost Management Agent
/// </summary>
public class CostManagementAgentOptions
{
    public const string SectionName = "CostManagementAgent";

    /// <summary>
    /// Temperature setting for AI model (0.0-1.0). Lower = more deterministic, higher = more creative
    /// </summary>
    [Range(0.0, 1.0)]
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Maximum tokens for AI model responses
    /// </summary>
    [Range(100, 128000)]
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Default currency for cost reporting (e.g., USD, EUR, GBP)
    /// </summary>
    [Required]
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>
    /// Default timeframe for cost analysis (e.g., MonthToDate, LastMonth, Custom)
    /// </summary>
    [Required]
    public string DefaultTimeframe { get; set; } = "MonthToDate";

    /// <summary>
    /// Enable automatic anomaly detection for cost spikes
    /// </summary>
    public bool EnableAnomalyDetection { get; set; } = true;

    /// <summary>
    /// Enable optimization recommendations
    /// </summary>
    public bool EnableOptimizationRecommendations { get; set; } = true;

    /// <summary>
    /// Cost management specific settings
    /// </summary>
    public CostManagementOptions CostManagement { get; set; } = new();

    /// <summary>
    /// Budget management settings
    /// </summary>
    public BudgetOptions Budgets { get; set; } = new();
}

/// <summary>
/// Cost management specific settings
/// </summary>
public class CostManagementOptions
{
    /// <summary>
    /// How often to refresh cost data (in minutes)
    /// </summary>
    [Range(1, 1440)]
    public int RefreshIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Percentage threshold for anomaly detection (e.g., 50% increase triggers anomaly)
    /// </summary>
    [Range(1, 1000)]
    public int AnomalyThresholdPercentage { get; set; } = 50;

    /// <summary>
    /// Minimum savings amount to include in recommendations (in default currency)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal MinimumSavingsThreshold { get; set; } = 100.00m;

    /// <summary>
    /// Number of days to forecast costs into the future
    /// </summary>
    [Range(1, 365)]
    public int ForecastDays { get; set; } = 30;
}

/// <summary>
/// Budget management settings
/// </summary>
public class BudgetOptions
{
    /// <summary>
    /// Default alert thresholds as percentages of budget (e.g., [50, 80, 100, 120])
    /// </summary>
    public List<int> DefaultAlertThresholds { get; set; } = new() { 50, 80, 100, 120 };

    /// <summary>
    /// Enable email notifications for budget alerts
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// List of email addresses to receive budget notifications
    /// </summary>
    public List<string> NotificationEmails { get; set; } = new();
}
