using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Agents.Compliance.State;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for fetching and analyzing Microsoft Defender for Cloud findings.
/// Retrieves security assessments and secure score from DFC and maps them to NIST 800-53 controls.
/// </summary>
public class DefenderForCloudTool : BaseTool
{
    private readonly ComplianceStateAccessors _stateAccessors;
    private readonly ComplianceAgentOptions _options;
    private readonly IDefenderForCloudService _defenderForCloudService;
    private readonly IAtoComplianceEngine _complianceEngine;

    public override string Name => "get_defender_findings";

    public override string Description =>
        "Fetches security findings and recommendations from Microsoft Defender for Cloud (DFC). " +
        "Returns security assessments, secure score, and maps findings to NIST 800-53 controls. " +
        "Use when user asks: 'show defender findings', 'get secure score', 'DFC recommendations', " +
        "'security center findings', 'defender for cloud status', 'what does defender say'. " +
        "Can be used standalone or to enrich compliance assessments with DFC data. " +
        "Requires DefenderForCloud to be enabled in configuration.";

    public DefenderForCloudTool(
        ILogger<DefenderForCloudTool> logger,
        ComplianceStateAccessors stateAccessors,
        IOptions<ComplianceAgentOptions> options,
        IDefenderForCloudService defenderForCloudService,
        IAtoComplianceEngine complianceEngine) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new ComplianceAgentOptions();
        _defenderForCloudService = defenderForCloudService ?? throw new ArgumentNullException(nameof(defenderForCloudService));
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));

        Parameters.Add(new ToolParameter("subscription_id", "Azure subscription ID to query DFC findings for", false));
        Parameters.Add(new ToolParameter("resource_group", "Optional resource group to filter findings", false));
        Parameters.Add(new ToolParameter("include_secure_score", "Include secure score details (default: true)", false));
        Parameters.Add(new ToolParameter("severity_filter", "Filter findings by severity: 'critical', 'high', 'medium', 'low', 'all' (default: 'all')", false));
        Parameters.Add(new ToolParameter("map_to_nist", "Map DFC findings to NIST 800-53 controls (default: true)", false));
        Parameters.Add(new ToolParameter("conversation_id", "Conversation ID for state tracking", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Check if DFC is enabled
            if (!_options.DefenderForCloud.Enabled)
            {
                return ToJson(new
                {
                    success = false,
                    error = "Defender for Cloud integration is disabled",
                    message = "Enable DFC in configuration by setting AgentConfiguration:ComplianceAgent:DefenderForCloud:Enabled to true",
                    configuration = new
                    {
                        enabled = false,
                        configPath = "AgentConfiguration:ComplianceAgent:DefenderForCloud"
                    }
                });
            }

            var conversationId = GetOptionalString(arguments, "conversation_id") ?? Guid.NewGuid().ToString();
            var resourceGroup = GetOptionalString(arguments, "resource_group");
            var includeSecureScore = GetOptionalBool(arguments, "include_secure_score", true);
            var severityFilter = GetOptionalString(arguments, "severity_filter") ?? "all";
            var mapToNist = GetOptionalBool(arguments, "map_to_nist", true);

            // Get subscription from argument or state
            var subscriptionId = GetOptionalString(arguments, "subscription_id")
                ?? await _stateAccessors.GetCurrentSubscriptionAsync(conversationId, cancellationToken)
                ?? _options.DefenderForCloud.SubscriptionId
                ?? _options.DefaultSubscriptionId;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Subscription ID is required. Use 'Set my subscription to <id>' first or provide subscription_id parameter."
                });
            }

            Logger.LogInformation("Fetching Defender for Cloud findings: Subscription={Subscription}, ResourceGroup={ResourceGroup}",
                subscriptionId, resourceGroup ?? "all");

            // Fetch DFC findings
            var defenderFindings = await _defenderForCloudService.GetSecurityAssessmentsAsync(
                subscriptionId, resourceGroup, cancellationToken);

            // Filter by severity if specified
            var filteredFindings = FilterBySeverity(defenderFindings, severityFilter);

            // Get secure score if requested
            DefenderSecureScore? secureScore = null;
            if (includeSecureScore)
            {
                secureScore = await _defenderForCloudService.GetSecureScoreAsync(subscriptionId, cancellationToken);
            }

            // Map to NIST controls if requested
            List<AtoFinding>? nistFindings = null;
            if (mapToNist && filteredFindings.Count > 0)
            {
                nistFindings = _defenderForCloudService.MapDefenderFindingsToNistControls(filteredFindings, subscriptionId);
            }

            // Calculate summary statistics
            var summary = new
            {
                totalFindings = filteredFindings.Count,
                bySeverity = new
                {
                    critical = filteredFindings.Count(f => f.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)),
                    high = filteredFindings.Count(f => f.Severity.Equals("High", StringComparison.OrdinalIgnoreCase)),
                    medium = filteredFindings.Count(f => f.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase)),
                    low = filteredFindings.Count(f => f.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase))
                },
                healthyCount = filteredFindings.Count(f => f.Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase)),
                unhealthyCount = filteredFindings.Count(f => !f.Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase))
            };

            // Group findings by category for better presentation
            var groupedFindings = filteredFindings
                .GroupBy(f => f.AssessmentType ?? "Unknown")
                .Select(g => new
                {
                    category = g.Key,
                    count = g.Count(),
                    findings = g.Take(10).Select(f => new
                    {
                        id = f.Id,
                        name = f.DisplayName,
                        severity = f.Severity,
                        status = f.Status,
                        resource = f.AffectedResource,
                        description = f.Description?.Length > 200 
                            ? f.Description[..200] + "..." 
                            : f.Description,
                        remediation = f.RemediationSteps?.Length > 200 
                            ? f.RemediationSteps[..200] + "..." 
                            : f.RemediationSteps
                    }).ToList()
                })
                .OrderByDescending(g => g.count)
                .ToList();

            // Build NIST control summary if mapped
            object? nistSummary = null;
            if (nistFindings != null && nistFindings.Count > 0)
            {
                nistSummary = new
                {
                    totalNistFindings = nistFindings.Count,
                    controlFamiliesAffected = nistFindings
                        .SelectMany(f => f.AffectedNistControls ?? new List<string>())
                        .Select(c => c.Length >= 2 ? c[..2] : c)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList(),
                    byControlFamily = nistFindings
                        .SelectMany(f => (f.AffectedNistControls ?? new List<string>())
                            .Select(c => new { Control = c, Finding = f }))
                        .GroupBy(x => x.Control.Length >= 2 ? x.Control[..2] : x.Control)
                        .Select(g => new
                        {
                            family = g.Key,
                            count = g.Count(),
                            controls = g.Select(x => x.Control).Distinct().Take(5).ToList()
                        })
                        .OrderByDescending(g => g.count)
                        .Take(10)
                        .ToList()
                };
            }

            var duration = DateTime.UtcNow - startTime;

            // Track operation
            await _stateAccessors.TrackComplianceOperationAsync(
                conversationId, "defender_findings", "DFC", subscriptionId,
                true, filteredFindings.Count, duration, cancellationToken);

            return ToJson(new
            {
                success = true,
                subscriptionId,
                resourceGroup = resourceGroup ?? "all",
                severityFilter,
                summary,
                secureScore = secureScore != null ? new
                {
                    current = secureScore.CurrentScore,
                    max = secureScore.MaxScore,
                    percentage = Math.Round(secureScore.Percentage, 1)
                } : null,
                nistMapping = nistSummary,
                findingsByCategory = groupedFindings,
                metadata = new
                {
                    source = "Microsoft Defender for Cloud",
                    fetchedAt = DateTime.UtcNow,
                    durationMs = duration.TotalMilliseconds,
                    dfcEnabled = _options.DefenderForCloud.Enabled,
                    mapToNistControls = _options.DefenderForCloud.MapToNistControls
                },
                nextSteps = filteredFindings.Count > 0 ? new[]
                {
                    "Use 'batch_remediation' to remediate auto-remediable findings",
                    "Use 'generate_remediation_plan' for detailed remediation steps",
                    "Use 'run_compliance_assessment' for full NIST 800-53 assessment including DFC findings"
                } : new[]
                {
                    "No unhealthy findings detected - great security posture!",
                    "Use 'run_compliance_assessment' for comprehensive compliance check"
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching Defender for Cloud findings");
            return ToJson(new
            {
                success = false,
                error = ex.Message,
                troubleshooting = new[]
                {
                    "Ensure Azure credentials have Security Reader role",
                    "Verify subscription ID is correct",
                    "Check if Defender for Cloud is enabled for the subscription",
                    "Verify DefenderForCloud.Enabled is true in appsettings.json"
                }
            });
        }
    }

    private List<DefenderFinding> FilterBySeverity(List<DefenderFinding> findings, string severityFilter)
    {
        if (string.IsNullOrEmpty(severityFilter) || severityFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return findings;
        }

        return findings.Where(f => 
            f.Severity.Equals(severityFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
