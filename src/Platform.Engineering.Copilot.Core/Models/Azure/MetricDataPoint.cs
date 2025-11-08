namespace Platform.Engineering.Copilot.Core.Models.Azure;

/// <summary>
/// Represents a single Azure Monitor metric data point
/// </summary>
public class MetricDataPoint
{
    /// <summary>
    /// Timestamp of the metric data point
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The metric value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// The metric name
    /// </summary>
    public string? MetricName { get; set; }

    /// <summary>
    /// The unit of measurement
    /// </summary>
    public string? Unit { get; set; }
}
