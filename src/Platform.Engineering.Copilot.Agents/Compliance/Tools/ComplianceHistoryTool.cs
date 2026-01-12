using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for viewing historical compliance scores and trend analysis.
/// </summary>
public class ComplianceHistoryTool : BaseTool
{
    private readonly IAtoComplianceEngine _complianceEngine;

    public override string Name => "get_compliance_history";

    public override string Description =>
        "View historical compliance scores and how they've changed over time. " +
        "Shows trend analysis to identify if compliance is improving or declining. " +
        "Useful for tracking compliance posture over weeks/months and preparing for audits. " +
        "Example: 'Show me compliance history for the last 30 days' or 'How has compliance changed over time?'";

    public ComplianceHistoryTool(
        ILogger<ComplianceHistoryTool> logger,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));

        Parameters.Add(new ToolParameter("subscription_id",
            "Azure subscription ID (GUID) or friendly name. If not provided, uses default subscription.", false));
        Parameters.Add(new ToolParameter("days",
            "Number of days of history to retrieve (default: 30, max: 365)", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptionIdOrName = GetOptionalString(arguments, "subscription_id");
            var daysStr = GetOptionalString(arguments, "days");
            var days = 30;
            if (!string.IsNullOrEmpty(daysStr) && int.TryParse(daysStr, out var parsedDays))
            {
                days = parsedDays;
            }

            // Resolve subscription
            var subscriptionId = await ResolveSubscriptionIdAsync(subscriptionIdOrName);
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Subscription ID is required. Either provide subscription_id parameter or set default using configure_subscription tool."
                });
            }

            // Limit days to reasonable range
            days = Math.Min(Math.Max(days, 1), 365);

            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            Logger.LogInformation("📊 Retrieving compliance history for subscription {SubscriptionId}, last {Days} days",
                subscriptionId, days);

            // Query historical assessments via engine
            var assessments = await _complianceEngine.GetComplianceHistoryAsync(
                subscriptionId,
                cutoffDate,
                DateTime.UtcNow,
                cancellationToken);

            var assessmentList = assessments.ToList();

            if (assessmentList.Count == 0)
            {
                return ToJson(new
                {
                    success = true,
                    message = $"No compliance assessments found in the last {days} days",
                    subscriptionId,
                    hint = "Run a compliance assessment using 'run_compliance_assessment' to start building historical data"
                });
            }

            // Calculate trend
            var scores = assessmentList.Select(a => (double)a.ComplianceScore).ToList();
            var trend = CalculateTrend(scores);
            var latestScore = (double)assessmentList.Last().ComplianceScore;
            var oldestScore = (double)assessmentList.First().ComplianceScore;
            var scoreChange = latestScore - oldestScore;

            // Find best and worst scores
            var bestAssessment = assessmentList.OrderByDescending(a => a.ComplianceScore).First();
            var worstAssessment = assessmentList.OrderBy(a => a.ComplianceScore).First();

            // Format trend emoji
            var trendEmoji = trend.Direction switch
            {
                "improving" => "📈",
                "declining" => "📉",
                _ => "➡️"
            };

            // Build insights
            var insights = new List<string>();
            if (assessmentList.Count < 3)
            {
                insights.Add("Run more assessments to build a better trend analysis (3+ recommended).");
            }
            if (trend.Direction == "improving" && latestScore < 90)
            {
                insights.Add($"You're on track! Keep improving to reach 90% (currently {Math.Round(90 - latestScore, 1)}% away).");
            }
            if (trend.Direction == "declining")
            {
                insights.Add("Review recent infrastructure changes that may have introduced compliance issues.");
            }
            if (latestScore >= 90)
            {
                insights.Add("Excellent! Maintain your current score with regular monitoring.");
            }

            return ToJson(new
            {
                success = true,
                subscriptionId,
                period = new
                {
                    days,
                    startDate = cutoffDate,
                    endDate = DateTime.UtcNow,
                    assessmentCount = assessmentList.Count
                },
                currentStatus = new
                {
                    score = Math.Round(latestScore, 1),
                    grade = GetComplianceGrade(latestScore),
                    date = assessmentList.Last().CompletedAt,
                    totalFindings = assessmentList.Last().TotalFindings,
                    criticalFindings = assessmentList.Last().CriticalFindings,
                    highFindings = assessmentList.Last().HighFindings
                },
                trend = new
                {
                    direction = trend.Direction,
                    emoji = trendEmoji,
                    changeRate = Math.Round(trend.ChangeRate, 2),
                    scoreChange = Math.Round(scoreChange, 1),
                    interpretation = trend.Direction == "improving" ? "Compliance is improving over time" :
                                   trend.Direction == "declining" ? "Compliance is declining - needs attention" :
                                   "Compliance remains stable"
                },
                statistics = new
                {
                    bestScore = Math.Round((double)bestAssessment.ComplianceScore, 1),
                    bestDate = bestAssessment.CompletedAt,
                    worstScore = Math.Round((double)worstAssessment.ComplianceScore, 1),
                    worstDate = worstAssessment.CompletedAt,
                    variance = Math.Round((double)(bestAssessment.ComplianceScore - worstAssessment.ComplianceScore), 1),
                    averageScore = Math.Round(assessmentList.Average(a => (double)a.ComplianceScore), 1)
                },
                history = assessmentList.Select(a => new
                {
                    id = a.Id,
                    date = a.CompletedAt,
                    score = Math.Round((double)a.ComplianceScore, 1),
                    grade = GetComplianceGrade((double)a.ComplianceScore),
                    totalFindings = a.TotalFindings,
                    criticalFindings = a.CriticalFindings,
                    highFindings = a.HighFindings,
                    mediumFindings = a.MediumFindings,
                    lowFindings = a.LowFindings,
                    initiatedBy = a.InitiatedBy
                }).ToList(),
                insights,
                nextSteps = new[]
                {
                    "Run 'get_compliance_status' for current real-time status",
                    "Run 'get_control_family_details' to drill into specific control families",
                    "Run 'run_compliance_assessment' for a fresh assessment"
                },
                message = $"📊 Retrieved {assessmentList.Count} assessments from the last {days} days. " +
                    $"Current score: {Math.Round(latestScore, 1)}% ({GetComplianceGrade(latestScore)}). " +
                    $"Trend: {trend.Direction} {trendEmoji}"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving compliance history");
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private async Task<string?> ResolveSubscriptionIdAsync(string? subscriptionIdOrName)
    {
        if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
        {
            // Try to get from persistent configuration
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".platform-copilot", "config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    var configJson = await File.ReadAllTextAsync(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(configJson);
                    if (config?.TryGetValue("subscription_id", out var savedId) == true)
                    {
                        Logger.LogInformation("Using subscription from persistent config: {SubscriptionId}", savedId);
                        return savedId;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to read config file");
                }
            }
            return null;
        }

        // Check if it's already a GUID
        if (Guid.TryParse(subscriptionIdOrName, out _))
        {
            return subscriptionIdOrName;
        }

        // Return as-is for friendly names
        return subscriptionIdOrName;
    }

    private static (string Direction, double ChangeRate) CalculateTrend(List<double> scores)
    {
        if (scores.Count < 2)
        {
            return ("stable", 0);
        }

        // Calculate simple linear regression
        var n = scores.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += scores[i];
            sumXY += i * scores[i];
            sumX2 += i * i;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        
        // Determine direction based on slope
        var direction = slope switch
        {
            > 0.5 => "improving",
            < -0.5 => "declining",
            _ => "stable"
        };

        return (direction, slope);
    }

    private static string GetComplianceGrade(double score)
    {
        return score switch
        {
            >= 95 => "A+",
            >= 90 => "A",
            >= 85 => "B+",
            >= 80 => "B",
            >= 75 => "C+",
            >= 70 => "C",
            >= 65 => "D+",
            >= 60 => "D",
            _ => "F"
        };
    }
}
