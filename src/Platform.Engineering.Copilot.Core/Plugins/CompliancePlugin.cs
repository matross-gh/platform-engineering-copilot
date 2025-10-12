using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;
using System.ComponentModel;

namespace Platform.Engineering.Copilot.Core.Plugins;

/// <summary>
/// Plugin for ATO Compliance operations including assessments, remediation, and evidence collection.
/// Supports NIST 800-53 compliance framework with automated remediation capabilities.
/// </summary>
public class CompliancePlugin : BaseSupervisorPlugin
{
    private readonly IAtoComplianceEngine _complianceEngine;
    private readonly IAtoRemediationEngine _remediationEngine;

    public CompliancePlugin(
        ILogger<CompliancePlugin> logger,
        Kernel kernel,
        IAtoComplianceEngine complianceEngine,
        IAtoRemediationEngine remediationEngine) : base(logger, kernel)
    {
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));
        _remediationEngine = remediationEngine ?? throw new ArgumentNullException(nameof(remediationEngine));
    }

    // ========== COMPLIANCE ASSESSMENT FUNCTIONS ==========

    [KernelFunction("run_compliance_assessment")]
    [Description("Run a comprehensive NIST 800-53 compliance assessment for an Azure subscription. " +
                 "Scans all resources and generates detailed findings with severity ratings. " +
                 "Essential for ATO compliance verification.")]
    public async Task<string> RunComplianceAssessmentAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running comprehensive compliance assessment for subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID is required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var assessment = await _complianceEngine.RunComprehensiveAssessmentAsync(
                subscriptionId, 
                null,
                cancellationToken);

            // Extract all findings from control families
            var allFindings = assessment.ControlFamilyResults
                .SelectMany(cf => cf.Value.Findings)
                .ToList();

            // Get top 10 most critical findings
            var topFindings = allFindings
                .OrderByDescending(f => f.Severity)
                .Take(10)
                .Select(f => new
                {
                    id = f.Id,
                    severity = f.Severity.ToString(),
                    title = f.Title,
                    resourceType = f.ResourceType,
                    isAutoRemediable = f.IsAutoRemediable
                });

            return JsonSerializer.Serialize(new
            {
                success = true,
                assessmentId = assessment.AssessmentId,
                subscriptionId = assessment.SubscriptionId,
                timestamp = assessment.EndTime,
                duration = assessment.Duration,
                overallScore = new
                {
                    score = Math.Round(assessment.OverallComplianceScore, 2),
                    grade = GetComplianceGrade(assessment.OverallComplianceScore),
                    status = assessment.OverallComplianceScore >= 90 ? "Compliant" :
                            assessment.OverallComplianceScore >= 70 ? "Partially Compliant" : "Non-Compliant"
                },
                findings = new
                {
                    total = assessment.TotalFindings,
                    critical = assessment.CriticalFindings,
                    high = assessment.HighFindings,
                    medium = assessment.MediumFindings,
                    low = assessment.LowFindings
                },
                controlFamilies = assessment.ControlFamilyResults.Select(cf => new
                {
                    family = cf.Key,
                    name = GetControlFamilyName(cf.Key),
                    score = Math.Round(cf.Value.ComplianceScore, 2),
                    totalControls = cf.Value.TotalControls,
                    passedControls = cf.Value.PassedControls,
                    findingsCount = cf.Value.Findings.Count
                }),
                topFindings = topFindings,
                executiveSummary = assessment.ExecutiveSummary,
                nextSteps = new[]
                {
                    $"{assessment.CriticalFindings} critical findings require immediate attention - review them above.",
                    $"{allFindings.Count(f => f.IsAutoRemediable)} findings can be auto-remediated. Say 'generate a remediation plan for this assessment' to create an action plan.",
                    "Say 'collect compliance evidence for this subscription' to gather documentation for audits.",
                    "Say 'show me the current compliance status' for real-time monitoring and trend analysis."
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running compliance assessment for subscription {SubscriptionId}", subscriptionId);
            return CreateErrorResponse("run compliance assessment", ex);
        }
    }

    [KernelFunction("get_compliance_status")]
    [Description("Get real-time compliance status with continuous monitoring data. " +
                 "Shows current score, active alerts, and recent changes. " +
                 "Use this for quick compliance health checks.")]
    public async Task<string> GetComplianceStatusAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting compliance status for subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID is required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var status = await _complianceEngine.GetContinuousComplianceStatusAsync(subscriptionId, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                subscriptionId = status.SubscriptionId,
                timestamp = status.Timestamp,
                currentStatus = new
                {
                    score = Math.Round(status.ComplianceScore, 2),
                    grade = GetComplianceGrade(status.ComplianceScore),
                    monitoringEnabled = status.MonitoringEnabled,
                    lastCheck = status.LastCheckTime,
                    nextCheck = status.NextCheckTime
                },
                trend = new
                {
                    direction = status.TrendDirection,
                    driftPercentage = Math.Round(status.ComplianceDriftPercentage, 2)
                },
                alerts = new
                {
                    active = status.ActiveAlerts,
                    resolvedToday = status.ResolvedToday,
                    autoRemediations = status.AutoRemediationCount
                },
                monitoring = new
                {
                    enabled = status.MonitoringEnabled,
                    lastScan = status.LastCheckTime,
                    nextScan = status.NextCheckTime,
                    activeControls = status.ControlStatuses.Count
                },
                quickActions = new[]
                {
                    status.ActiveAlerts > 0 ? $"Review {status.ActiveAlerts} active alerts" : null,
                    status.TrendDirection == "Declining" ? "Investigate compliance drift - trend is declining" : null,
                    "Run 'run_compliance_assessment' for detailed analysis",
                    "Use 'generate_remediation_plan' to address findings"
                }.Where(a => a != null)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compliance status for subscription {SubscriptionId}", subscriptionId);
            return CreateErrorResponse("get compliance status", ex);
        }
    }

    [KernelFunction("collect_evidence")]
    [Description("Collect and package compliance evidence for a specific NIST control family. " +
                 "Gathers configuration data, logs, and metrics for audit purposes. " +
                 "Essential for ATO attestation packages.")]
    public async Task<string> CollectEvidenceAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        [Description("NIST control family (e.g., AC, AU, CM, IA)")] string controlFamily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Collecting evidence for subscription {SubscriptionId}, family {Family}", 
                subscriptionId, controlFamily);

            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(controlFamily))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID and control family are required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var evidencePackage = await _complianceEngine.CollectComplianceEvidenceAsync(
                subscriptionId,
                controlFamily,
                null,
                cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = string.IsNullOrEmpty(evidencePackage.Error),
                packageId = evidencePackage.PackageId,
                subscriptionId = evidencePackage.SubscriptionId,
                controlFamily = new
                {
                    code = evidencePackage.ControlFamily,
                    name = GetControlFamilyName(evidencePackage.ControlFamily)
                },
                collection = new
                {
                    collectionDate = evidencePackage.CollectionDate,
                    duration = evidencePackage.CollectionDuration,
                    totalItems = evidencePackage.TotalItems,
                    completenessScore = Math.Round(evidencePackage.CompletenessScore, 2)
                },
                evidence = new
                {
                    totalItems = evidencePackage.Evidence.Count,
                    byType = evidencePackage.Evidence
                        .GroupBy(e => e.EvidenceType)
                        .Select(g => new { type = g.Key, count = g.Count() }),
                    items = evidencePackage.Evidence.Take(20).Select(e => new
                    {
                        evidenceId = e.EvidenceId,
                        controlId = e.ControlId,
                        type = e.EvidenceType,
                        resourceId = e.ResourceId,
                        collectedAt = e.CollectedAt
                    })
                },
                summary = evidencePackage.Summary,
                attestation = evidencePackage.AttestationStatement,
                error = evidencePackage.Error,
                nextSteps = new[]
                {
                    "Review the evidence items above for completeness before submitting to auditors.",
                    evidencePackage.CompletenessScore < 100 ? 
                        $"Evidence collection is {evidencePackage.CompletenessScore}% complete - you may need additional data. Say 'collect more evidence for this control' to gather missing items." : null,
                    "Include this evidence package in your ATO compliance documentation.",
                    "Say 'run a compliance assessment for this subscription' to verify control compliance and identify any gaps."
                }.Where(s => s != null)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting evidence for subscription {SubscriptionId}, family {Family}", 
                subscriptionId, controlFamily);
            return CreateErrorResponse("collect evidence", ex);
        }
    }

    // ========== REMEDIATION FUNCTIONS ==========

    [KernelFunction("generate_remediation_plan")]
    [Description("Generate a comprehensive remediation plan for compliance findings. " +
                 "Analyzes findings and creates a prioritized action plan. " +
                 "Essential for planning compliance improvements.")]
    public async Task<string> GenerateRemediationPlanAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating remediation plan for subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID is required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Get latest assessment findings
            var assessment = await _complianceEngine.RunComprehensiveAssessmentAsync(
                subscriptionId, null, cancellationToken);
            
            var findings = assessment.ControlFamilyResults
                .SelectMany(cf => cf.Value.Findings)
                .ToList();

            if (!findings.Any())
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "No findings to remediate - subscription is compliant!",
                    subscriptionId = subscriptionId
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var plan = await _complianceEngine.GenerateRemediationPlanAsync(
                subscriptionId,
                findings,
                cancellationToken);

            var autoRemediable = findings.Count(f => f.IsAutoRemediable);
            var manual = findings.Count - autoRemediable;

            return JsonSerializer.Serialize(new
            {
                success = true,
                planId = plan.PlanId,
                subscriptionId = plan.SubscriptionId,
                createdAt = plan.CreatedAt,
                summary = new
                {
                    totalFindings = plan.TotalFindings,
                    autoRemediable = autoRemediable,
                    manualRequired = manual,
                    estimatedEffort = plan.EstimatedEffort,
                    priority = plan.Priority,
                    riskReduction = Math.Round(plan.ProjectedRiskReduction, 2)
                },
                remediationItems = plan.RemediationItems.Take(20).Select(item => new
                {
                    findingId = item.FindingId,
                    priority = item.Priority,
                    effort = item.EstimatedEffort,
                    isAutomated = item.IsAutomated,
                    dependencies = item.Dependencies
                }),
                timeline = plan.Timeline != null ? new
                {
                    startDate = plan.Timeline.StartDate,
                    endDate = plan.Timeline.EndDate,
                    milestones = plan.Timeline.Milestones
                } : null,
                executiveSummary = plan.ExecutiveSummary,
                nextSteps = new[]
                {
                    $"{autoRemediable} findings can be automatically fixed. Say 'execute the remediation plan' to start auto-remediation.",
                    manual > 0 ? $"{manual} findings require manual remediation - review the plan items above and assign to your team." : null,
                    "Review the plan items above and prioritize them by risk level and effort required.",
                    "Say 'show me the remediation progress' to track execution status and completion percentage."
                }.Where(s => s != null)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating remediation plan for subscription {SubscriptionId}", subscriptionId);
            return CreateErrorResponse("generate remediation plan", ex);
        }
    }

    [KernelFunction("execute_remediation")]
    [Description("Execute automated remediation for a specific compliance finding. " +
                 "Use dry-run mode first to preview changes. Supports rollback on failure.")]
    public async Task<string> ExecuteRemediationAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        [Description("Finding ID to remediate")] string findingId,
        [Description("Dry run mode - preview changes without applying (true/false, default: true)")] bool dryRun = true,
        [Description("Require approval before executing (true/false, default: false)")] bool requireApproval = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing remediation for finding {FindingId}, dry-run: {DryRun}", 
                findingId, dryRun);

            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(findingId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID and finding ID are required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Get the finding
            var assessment = await _complianceEngine.RunComprehensiveAssessmentAsync(
                subscriptionId, null, cancellationToken);
            
            var finding = assessment.ControlFamilyResults
                .SelectMany(cf => cf.Value.Findings)
                .FirstOrDefault(f => f.Id == findingId);

            if (finding == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Finding {findingId} not found",
                    suggestion = "Use 'run_compliance_assessment' to get valid finding IDs"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            if (!finding.IsAutoRemediable)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "This finding cannot be automatically remediated",
                    findingId = findingId,
                    recommendation = finding.Recommendation,
                    manualGuidance = finding.RemediationGuidance
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var options = new RemediationExecutionOptions
            {
                DryRun = dryRun,
                RequireApproval = requireApproval
            };

            var execution = await _remediationEngine.ExecuteRemediationAsync(
                subscriptionId,
                finding,
                options,
                cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = execution.Success,
                executionId = execution.ExecutionId,
                mode = dryRun ? "DRY RUN (no changes applied)" : "LIVE EXECUTION",
                finding = new
                {
                    id = finding.Id,
                    title = finding.Title,
                    severity = finding.Severity.ToString()
                },
                result = new
                {
                    status = execution.Status.ToString(),
                    message = execution.Message,
                    duration = execution.Duration,
                    changesApplied = execution.ChangesApplied
                },
                backupCreated = !string.IsNullOrEmpty(execution.BackupId),
                backupId = execution.BackupId,
                error = execution.Error,
                nextSteps = dryRun ? new[]
                {
                    "Review the changes that would be applied",
                    "If satisfied, re-run with dryRun=false to apply changes",
                    "Changes can be rolled back if needed"
                } : new[]
                {
                    execution.Success ? "Remediation completed successfully" : "Remediation failed - review error",
                    "Use 'validate_remediation' to verify the fix",
                    "Use 'get_compliance_status' to see updated score"
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing remediation for finding {FindingId}", findingId);
            return CreateErrorResponse("execute remediation", ex);
        }
    }

    [KernelFunction("validate_remediation")]
    [Description("Validate that a remediation was successful. " +
                 "Performs post-remediation checks to ensure fixes were effective.")]
    public async Task<string> ValidateRemediationAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        [Description("Finding ID that was remediated")] string findingId,
        [Description("Execution ID from remediation")] string executionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating remediation for execution {ExecutionId}", executionId);

            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(findingId) || string.IsNullOrWhiteSpace(executionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID, finding ID, and execution ID are required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Note: Validation requires both finding and execution objects
            // For now, return a simplified response indicating manual validation is needed
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = "Automatic validation requires integration with execution tracking",
                executionId = executionId,
                findingId = findingId,
                recommendation = "Say 'run a compliance assessment for this subscription' to verify the finding is resolved",
                nextSteps = new[]
                {
                    "Say 'run a compliance assessment' to check if this finding has been resolved after remediation.",
                    "Verify the resource configuration matches the compliance requirements in the finding details.",
                    "Say 'show me the compliance status' to check for any side effects or new findings that may have appeared."
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating remediation for execution {ExecutionId}", executionId);
            return CreateErrorResponse("validate remediation", ex);
        }
    }

    [KernelFunction("get_remediation_progress")]
    [Description("Track progress of remediation activities. " +
                 "Shows active remediations and completion status.")]
    public async Task<string> GetRemediationProgressAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting remediation progress for subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID is required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var progress = await _remediationEngine.GetRemediationProgressAsync(
                subscriptionId,
                null,
                cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                subscriptionId = progress.SubscriptionId,
                timestamp = progress.Timestamp,
                summary = new
                {
                    totalActivities = progress.TotalActivities,
                    inProgress = progress.InProgressCount,
                    completed = progress.CompletedCount,
                    failed = progress.FailedCount,
                    successRate = Math.Round(progress.SuccessRate, 2)
                },
                recentActivities = progress.RecentActivities.Take(10).Select(activity => new
                {
                    executionId = activity.ExecutionId,
                    findingId = activity.FindingId,
                    status = activity.Status.ToString(),
                    startedAt = activity.StartedAt,
                    completedAt = activity.CompletedAt
                }),
                nextSteps = new[]
                {
                    progress.InProgressCount > 0 ? $"{progress.InProgressCount} remediations currently in progress." : null,
                    progress.FailedCount > 0 ? $"{progress.FailedCount} failed remediations need your attention - review the error details above." : null,
                    "Say 'run a compliance assessment for this subscription' to see the updated compliance status after remediation."
                }.Where(s => s != null)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remediation progress for subscription {SubscriptionId}", subscriptionId);
            return CreateErrorResponse("get remediation progress", ex);
        }
    }

    // ========== REPORTING FUNCTIONS ==========

    [KernelFunction("perform_risk_assessment")]
    [Description("Perform comprehensive risk assessment for compliance posture. " +
                 "Analyzes risk levels and provides risk mitigation recommendations.")]
    public async Task<string> PerformRiskAssessmentAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing risk assessment for subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID is required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var riskAssessment = await _complianceEngine.PerformRiskAssessmentAsync(subscriptionId, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                subscriptionId = riskAssessment.SubscriptionId,
                assessmentDate = riskAssessment.AssessmentDate,
                overallRisk = new
                {
                    level = riskAssessment.OverallRiskLevel.ToString(),
                    score = Math.Round(riskAssessment.OverallRiskScore, 2),
                    rating = riskAssessment.RiskRating
                },
                riskCategories = riskAssessment.RiskCategories.Select(rc => new
                {
                    category = rc.Key,
                    level = rc.Value.RiskLevel,
                    score = Math.Round(rc.Value.Score, 2),
                    findingCount = rc.Value.FindingCount,
                    topRisks = rc.Value.TopRisks.Take(5)
                }),
                recommendations = riskAssessment.MitigationRecommendations?.Take(10),
                executiveSummary = riskAssessment.ExecutiveSummary,
                nextSteps = new[]
                {
                    $"Priority action: Address the {riskAssessment.RiskCategories.Count(r => r.Value.RiskLevel == "Critical")} critical risk categories first.",
                    "Say 'generate a remediation plan for this assessment' to create a comprehensive mitigation strategy.",
                    "Review the top risks in each category above and prioritize based on your organization's risk tolerance."
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing risk assessment for subscription {SubscriptionId}", subscriptionId);
            return CreateErrorResponse("perform risk assessment", ex);
        }
    }

    [KernelFunction("get_compliance_timeline")]
    [Description("Get compliance timeline showing historical trends and changes. " +
                 "Useful for tracking compliance improvements over time.")]
    public async Task<string> GetComplianceTimelineAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        [Description("Days to look back (default: 30)")] int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting compliance timeline for subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID is required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-daysBack);

            var timeline = await _complianceEngine.GetComplianceTimelineAsync(
                subscriptionId, 
                startDate, 
                endDate, 
                cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                subscriptionId = timeline.SubscriptionId,
                period = new
                {
                    startDate = timeline.StartDate,
                    endDate = timeline.EndDate,
                    daysAnalyzed = daysBack
                },
                trends = new
                {
                    currentScore = Math.Round(timeline.CurrentScore, 2),
                    previousScore = Math.Round(timeline.PreviousScore, 2),
                    change = Math.Round(timeline.ScoreChange, 2),
                    trend = timeline.TrendDirection
                },
                dataPoints = timeline.DataPoints.Select(dp => new
                {
                    date = dp.Date,
                    score = Math.Round(dp.Score, 2),
                    findingsCount = dp.FindingsCount
                }),
                majorEvents = timeline.MajorEvents.Take(10).Select(evt => new
                {
                    date = evt.Date,
                    type = evt.EventType,
                    description = evt.Description,
                    impact = evt.Impact
                }),
                insights = timeline.Insights,
                nextSteps = new[]
                {
                    timeline.TrendDirection == "Improving" ? "Great news - your compliance is improving! Continue your current efforts and maintain momentum." : null,
                    timeline.TrendDirection == "Declining" ? "Attention needed - compliance is declining. Say 'show me recent compliance changes' to investigate what happened." : null,
                    "Say 'run a compliance assessment for this subscription' to get the current detailed compliance analysis."
                }.Where(s => s != null)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compliance timeline for subscription {SubscriptionId}", subscriptionId);
            return CreateErrorResponse("get compliance timeline", ex);
        }
    }

    [KernelFunction("generate_compliance_certificate")]
    [Description("Generate a compliance certificate for ATO package. " +
                 "Creates official compliance attestation document.")]
    public async Task<string> GenerateComplianceCertificateAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating compliance certificate for subscription {SubscriptionId}", subscriptionId);

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID is required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var certificate = await _complianceEngine.GenerateComplianceCertificateAsync(subscriptionId, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                certificateId = certificate.CertificateId,
                subscriptionId = certificate.SubscriptionId,
                issuedDate = certificate.IssuedDate,
                expirationDate = certificate.ExpirationDate,
                complianceStatus = certificate.ComplianceStatus.ToString(),
                certificationLevel = certificate.CertificationLevel,
                complianceScore = Math.Round(certificate.ComplianceScore, 2),
                frameworks = certificate.CertifiedFrameworks,
                controlsCertified = new
                {
                    total = certificate.TotalControls,
                    certified = certificate.CertifiedControls,
                    certificationRate = Math.Round((double)certificate.CertifiedControls / certificate.TotalControls * 100, 2)
                },
                attestation = certificate.AttestationStatement,
                signatoryInformation = certificate.SignatoryInformation,
                validityPeriod = certificate.ValidityPeriod,
                nextSteps = new[]
                {
                    "Include this compliance certificate in your ATO package submission.",
                    $"Important: This certificate expires on {certificate.ExpirationDate:yyyy-MM-dd} - mark your calendar!",
                    "Say 'run a compliance assessment' before the expiration date to renew the certificate.",
                    "Say 'collect compliance evidence for this subscription' to gather all supporting documentation for auditors."
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance certificate for subscription {SubscriptionId}", subscriptionId);
            return CreateErrorResponse("generate compliance certificate", ex);
        }
    }

    [KernelFunction("get_remediation_guide")]
    [Description("Generate manual remediation guidance for findings that cannot be auto-remediated. " +
                 "Provides step-by-step instructions, prerequisites, and validation steps.")]
    public async Task<string> GetRemediationGuideAsync(
        [Description("Azure subscription ID")] string subscriptionId,
        [Description("Finding ID to get remediation guidance for")] string findingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating remediation guide for finding {FindingId}", findingId);

            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(findingId))
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Subscription ID and finding ID are required"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            // Get the finding
            var assessment = await _complianceEngine.RunComprehensiveAssessmentAsync(
                subscriptionId, null, cancellationToken);
            
            var finding = assessment.ControlFamilyResults
                .SelectMany(cf => cf.Value.Findings)
                .FirstOrDefault(f => f.Id == findingId);

            if (finding == null)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Finding {findingId} not found",
                    suggestion = "Use 'run_compliance_assessment' to get valid finding IDs"
                }, new JsonSerializerOptions { WriteIndented = true });
            }

            var guide = await _remediationEngine.GenerateManualRemediationGuideAsync(
                finding,
                cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                guideId = guide.GuideId,
                findingId = guide.FindingId,
                title = guide.Title,
                overview = guide.Overview,
                skillLevel = guide.SkillLevel,
                estimatedDuration = guide.EstimatedDuration,
                prerequisites = guide.Prerequisites,
                requiredPermissions = guide.RequiredPermissions,
                steps = guide.Steps.Select(step => new
                {
                    order = step.Order,
                    description = step.Description,
                    command = step.Command,
                    automationScript = step.AutomationScript
                }),
                validationSteps = guide.ValidationSteps,
                rollbackPlan = guide.RollbackPlan != null ? new
                {
                    description = guide.RollbackPlan.Description,
                    steps = guide.RollbackPlan.Steps,
                    estimatedTime = guide.RollbackPlan.EstimatedRollbackTime
                } : null,
                references = guide.References,
                nextSteps = new[]
                {
                    "Step 1: Review the prerequisites above and ensure you have all required permissions and tools.",
                    "Step 2: Follow the remediation steps in order - don't skip any steps.",
                    "Step 3: Use the validation steps to confirm the remediation was successful.",
                    "Step 4: If any issues occur during remediation, use the rollback plan immediately.",
                    "Step 5: Say 'run a compliance assessment for this subscription' to verify the finding is now resolved."
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating remediation guide for finding {FindingId}", findingId);
            return CreateErrorResponse("generate remediation guide", ex);
        }
    }

    // ========== HELPER METHODS ==========

    private string GetComplianceGrade(double score)
    {
        return score >= 95 ? "A+" :
               score >= 90 ? "A" :
               score >= 85 ? "A-" :
               score >= 80 ? "B+" :
               score >= 75 ? "B" :
               score >= 70 ? "B-" :
               score >= 65 ? "C+" :
               score >= 60 ? "C" :
               score >= 55 ? "C-" :
               score >= 50 ? "D" : "F";
    }

    private string GetControlFamilyName(string familyCode)
    {
        return familyCode switch
        {
            "AC" => "Access Control",
            "AU" => "Audit and Accountability",
            "CM" => "Configuration Management",
            "CP" => "Contingency Planning",
            "IA" => "Identification and Authentication",
            "IR" => "Incident Response",
            "MA" => "Maintenance",
            "MP" => "Media Protection",
            "PE" => "Physical and Environmental Protection",
            "PL" => "Planning",
            "PS" => "Personnel Security",
            "RA" => "Risk Assessment",
            "SA" => "System and Services Acquisition",
            "CA" => "Security Assessment and Authorization",
            "AT" => "Awareness and Training",
            "PM" => "Program Management",
            "SC" => "System and Communications Protection",
            "SI" => "System and Information Integrity",
            _ => familyCode
        };
    }
}
