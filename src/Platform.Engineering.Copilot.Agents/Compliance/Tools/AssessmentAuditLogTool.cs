using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for viewing the audit trail of compliance assessments.
/// Shows who ran assessments, when, and their results.
/// </summary>
public class AssessmentAuditLogTool : BaseTool
{
    private readonly IAtoComplianceEngine _complianceEngine;

    public override string Name => "get_assessment_audit_log";

    public override string Description =>
        "View audit trail of compliance assessments showing who ran them and when. " +
        "Useful for audit compliance, accountability, and tracking assessment frequency. " +
        "Example: 'Who ran compliance assessments this week?' or 'Show me audit log'";

    public AssessmentAuditLogTool(
        ILogger<AssessmentAuditLogTool> logger,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));

        Parameters.Add(new ToolParameter("subscription_id",
            "Azure subscription ID (GUID) or friendly name. If not provided, uses default subscription.", false));
        Parameters.Add(new ToolParameter("days",
            "Number of days of audit history to retrieve (default: 7, max: 90)", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptionIdOrName = GetOptionalString(arguments, "subscription_id");
            var daysStr = GetOptionalString(arguments, "days");
            var days = 7;
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
            days = Math.Min(Math.Max(days, 1), 90);
            
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            
            // Query audit log via engine
            var auditLog = await _complianceEngine.GetAssessmentAuditLogAsync(
                subscriptionId,
                cutoffDate,
                DateTime.UtcNow,
                cancellationToken);

            var auditLogList = auditLog.ToList();

            if (auditLogList.Count == 0)
            {
                return ToJson(new
                {
                    success = true,
                    message = $"No assessments found in the last {days} days",
                    subscriptionId,
                    hint = "Run a compliance assessment to start building audit history"
                });
            }

            // Group by user
            var byUser = auditLogList.GroupBy(a => a.InitiatedBy ?? "Unknown")
                .Select(g => new
                {
                    user = g.Key,
                    count = g.Count(),
                    lastRun = g.Max(a => a.CompletedAt)
                })
                .OrderByDescending(u => u.count)
                .ToList();

            // Calculate statistics
            var totalAssessments = auditLogList.Count;
            var avgDuration = auditLogList.Where(a => a.Duration.HasValue)
                .Select(a => TimeSpan.FromTicks(a.Duration!.Value).TotalSeconds)
                .DefaultIfEmpty(0)
                .Average();
            var completedCount = auditLogList.Count(a => a.Status == "Completed");
            var failedCount = auditLogList.Count(a => a.Status != "Completed");

            var output = $@"
# 📋 COMPLIANCE ASSESSMENT AUDIT LOG

**Subscription:** `{subscriptionId}`
**Period:** Last {days} days
**Total Assessments:** {totalAssessments}

---

## 📊 SUMMARY

| Metric | Value |
|--------|-------|
| ✅ **Completed** | {completedCount} |
| ❌ **Failed** | {failedCount} |
| ⏱️ **Avg Duration** | {Math.Round(avgDuration, 1)}s |
| 👥 **Unique Users** | {byUser.Count} |

---

## 👥 ASSESSMENTS BY USER

{string.Join("\n", byUser.Select(u => 
    $"- **{u.user}**: {u.count} assessment{(u.count > 1 ? "s" : "")} (last: {u.lastRun:MMM dd, HH:mm})"))}

---

## 📝 RECENT ASSESSMENTS

{string.Join("\n", auditLogList.Take(10).Select(a => 
    $@"### {a.CompletedAt:yyyy-MM-dd HH:mm} UTC
- **ID:** `{a.Id}`
- **User:** {a.InitiatedBy ?? "Unknown"}
- **Status:** {(a.Status == "Completed" ? "✅" : "❌")} {a.Status}
- **Score:** {Math.Round(a.ComplianceScore, 1)}% ({a.TotalFindings} findings: {a.CriticalFindings} critical, {a.HighFindings} high)
- **Scope:** {(string.IsNullOrEmpty(a.ResourceGroupName) ? "Full subscription" : $"Resource group: {a.ResourceGroupName}")}
- **Duration:** {(a.Duration.HasValue ? $"{Math.Round(TimeSpan.FromTicks(a.Duration.Value).TotalSeconds, 1)}s" : "N/A")}
"))}

{(auditLogList.Count > 10 ? $"\n*Showing 10 of {auditLogList.Count} assessments*" : "")}

---

## 💡 INSIGHTS

{(totalAssessments < 5 ? "📌 Low assessment frequency - consider running weekly checks to track compliance drift." : "")}
{(failedCount > 0 ? $"⚠️ {failedCount} assessment{(failedCount > 1 ? "s" : "")} failed - review logs for errors." : "")}
{(avgDuration > 30 ? "⏱️ Assessments taking longer than expected - consider scoping to resource groups." : "")}
{(byUser.Count == 1 ? "👤 Single user running assessments - consider sharing responsibility across team." : "")}

**Next Steps:**
- Run 'get compliance history' to see score trends
- Run 'get compliance trends' for detailed analytics
";

            return ToJson(new
            {
                success = true,
                formatted_output = output,
                subscriptionId,
                period = new { days, startDate = cutoffDate, endDate = DateTime.UtcNow },
                statistics = new
                {
                    totalAssessments,
                    completed = completedCount,
                    failed = failedCount,
                    averageDuration = Math.Round(avgDuration, 1),
                    uniqueUsers = byUser.Count
                },
                byUser = byUser.Select(u => new
                {
                    user = u.user,
                    assessmentCount = u.count,
                    lastRun = u.lastRun
                }),
                recentAssessments = auditLogList.Take(20).Select(a => new
                {
                    id = a.Id,
                    date = a.CompletedAt,
                    user = a.InitiatedBy,
                    status = a.Status,
                    score = Math.Round(a.ComplianceScore, 1),
                    findings = a.TotalFindings,
                    duration = a.Duration.HasValue ? TimeSpan.FromTicks(a.Duration.Value).TotalSeconds : (double?)null,
                    scope = string.IsNullOrEmpty(a.ResourceGroupName) ? "subscription" : a.ResourceGroupName
                })
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving audit log");
            return ToJson(new
            {
                success = false,
                error = $"Failed to retrieve audit log: {ex.Message}"
            });
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
}
