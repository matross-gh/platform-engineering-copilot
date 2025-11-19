using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Compliance.Core.Data.Entities;
using Platform.Engineering.Copilot.Core.Data.Context;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using System.Text.Json;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services.Data;

/// <summary>
/// Service for managing compliance assessments and findings using PlatformEngineeringCopilotContext
/// </summary>
public class AssessmentService
{
    private readonly PlatformEngineeringCopilotContext _context;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(
        PlatformEngineeringCopilotContext context,
        ILogger<AssessmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Saves a compliance assessment to the database
    /// </summary>
    public async Task<string> SaveAssessmentAsync(AtoComplianceAssessment assessment, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving compliance assessment {AssessmentId} for subscription {SubscriptionId}", 
                assessment.AssessmentId, assessment.SubscriptionId);

            // Map domain model to entity
            var entity = new ComplianceAssessment
            {
                Id = assessment.AssessmentId,
                SubscriptionId = assessment.SubscriptionId,
                AssessmentType = "NIST-800-53",
                Status = string.IsNullOrEmpty(assessment.Error) ? "Completed" : "Failed",
                ComplianceScore = (decimal)assessment.OverallComplianceScore,
                TotalFindings = assessment.TotalFindings,
                CriticalFindings = assessment.CriticalFindings,
                HighFindings = assessment.HighFindings,
                MediumFindings = assessment.MediumFindings,
                LowFindings = assessment.LowFindings,
                InformationalFindings = 0, // Not tracked in AtoComplianceAssessment
                ExecutiveSummary = assessment.ExecutiveSummary,
                RiskProfile = assessment.RiskProfile != null ? JsonSerializer.Serialize(assessment.RiskProfile) : null,
                Results = JsonSerializer.Serialize(assessment.ControlFamilyResults),
                Recommendations = null, // Not tracked in AtoComplianceAssessment
                Metadata = JsonSerializer.Serialize(new { assessment.Error }),
                InitiatedBy = "System", // Not tracked in AtoComplianceAssessment
                StartedAt = assessment.StartTime.UtcDateTime,
                CompletedAt = assessment.EndTime.UtcDateTime,
                Duration = assessment.Duration.Ticks // Convert TimeSpan to ticks (long)
            };

            // Map findings
            foreach (var familyResult in assessment.ControlFamilyResults.Values)
            {
                foreach (var finding in familyResult.Findings)
                {
                    entity.Findings.Add(MapFindingToEntity(assessment.AssessmentId, finding));
                }
            }

            // Check if assessment already exists
            var existing = await _context.ComplianceAssessments
                .Include(a => a.Findings)
                .FirstOrDefaultAsync(a => a.Id == assessment.AssessmentId, cancellationToken);

            if (existing != null)
            {
                _logger.LogInformation("Updating existing assessment {AssessmentId}", assessment.AssessmentId);
                _context.Entry(existing).CurrentValues.SetValues(entity);
                
                // Remove old findings
                _context.ComplianceFindings.RemoveRange(existing.Findings);
                
                // Add new findings
                foreach (var finding in entity.Findings)
                {
                    existing.Findings.Add(finding);
                }
            }
            else
            {
                _logger.LogInformation("Creating new assessment {AssessmentId}", assessment.AssessmentId);
                await _context.ComplianceAssessments.AddAsync(entity, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Successfully saved assessment {AssessmentId} with {FindingCount} findings", 
                assessment.AssessmentId, entity.Findings.Count);

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving assessment {AssessmentId}", assessment.AssessmentId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a compliance assessment by ID
    /// </summary>
    public async Task<AtoComplianceAssessment?> GetAssessmentAsync(string assessmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving assessment {AssessmentId}", assessmentId);

            var entity = await _context.ComplianceAssessments
                .Include(a => a.Findings)
                .FirstOrDefaultAsync(a => a.Id == assessmentId, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Assessment {AssessmentId} not found", assessmentId);
                return null;
            }

            return MapEntityToAssessment(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assessment {AssessmentId}", assessmentId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all assessments for a subscription
    /// </summary>
    public async Task<IEnumerable<AtoComplianceAssessment>> GetAssessmentsAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving assessments for subscription {SubscriptionId}", subscriptionId);

            var entities = await _context.ComplianceAssessments
                .Include(a => a.Findings)
                .Where(a => a.SubscriptionId == subscriptionId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} assessments for subscription {SubscriptionId}", 
                entities.Count, subscriptionId);

            return entities.Select(MapEntityToAssessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assessments for subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Gets a summary of assessments for a subscription
    /// </summary>
    public async Task<AtoScanSummary> GetAssessmentSummaryAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting assessment summary for subscription {SubscriptionId}", subscriptionId);

            var assessments = await _context.ComplianceAssessments
                .Where(a => a.SubscriptionId == subscriptionId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync(cancellationToken);

            if (!assessments.Any())
            {
                return new AtoScanSummary
                {
                    TotalResourcesScanned = 0,
                    TotalFindings = 0,
                    CriticalFindings = 0,
                    HighFindings = 0,
                    MediumFindings = 0,
                    LowFindings = 0,
                    InformationalFindings = 0,
                    RemediableFindings = 0,
                    NonCompliantResources = 0,
                    ComplianceScore = 0,
                    FindingsByType = new Dictionary<string, int>(),
                    FindingsByFramework = new Dictionary<string, int>()
                };
            }

            var latestAssessment = assessments.First();
            var findings = await _context.ComplianceFindings
                .Where(f => f.AssessmentId == latestAssessment.Id)
                .ToListAsync(cancellationToken);

            // Calculate findings by type
            var findingsByType = findings
                .GroupBy(f => f.FindingType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate findings by framework
            var findingsByFramework = new Dictionary<string, int>();
            foreach (var finding in findings)
            {
                if (!string.IsNullOrEmpty(finding.ComplianceFrameworks))
                {
                    var frameworks = DeserializeList(finding.ComplianceFrameworks);
                    foreach (var framework in frameworks)
                    {
                        if (findingsByFramework.ContainsKey(framework))
                            findingsByFramework[framework]++;
                        else
                            findingsByFramework[framework] = 1;
                    }
                }
            }

            return new AtoScanSummary
            {
                TotalResourcesScanned = findings.Select(f => f.ResourceId).Distinct().Count(),
                TotalFindings = latestAssessment.TotalFindings,
                CriticalFindings = latestAssessment.CriticalFindings,
                HighFindings = latestAssessment.HighFindings,
                MediumFindings = latestAssessment.MediumFindings,
                LowFindings = latestAssessment.LowFindings,
                InformationalFindings = latestAssessment.InformationalFindings,
                RemediableFindings = findings.Count(f => f.IsRemediable),
                NonCompliantResources = findings.Where(f => f.ComplianceStatus == "NonCompliant")
                    .Select(f => f.ResourceId)
                    .Distinct()
                    .Count(),
                ComplianceScore = (double)latestAssessment.ComplianceScore,
                FindingsByType = findingsByType,
                FindingsByFramework = findingsByFramework
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assessment summary for subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Deletes an assessment and all its findings
    /// </summary>
    public async Task<bool> DeleteAssessmentAsync(string assessmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting assessment {AssessmentId}", assessmentId);

            var assessment = await _context.ComplianceAssessments
                .Include(a => a.Findings)
                .FirstOrDefaultAsync(a => a.Id == assessmentId, cancellationToken);

            if (assessment == null)
            {
                _logger.LogWarning("Assessment {AssessmentId} not found for deletion", assessmentId);
                return false;
            }

            _context.ComplianceAssessments.Remove(assessment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted assessment {AssessmentId}", assessmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assessment {AssessmentId}", assessmentId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all findings for an assessment
    /// </summary>
    public async Task<IEnumerable<AtoFinding>> GetFindingsAsync(string assessmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving findings for assessment {AssessmentId}", assessmentId);

            var findings = await _context.ComplianceFindings
                .Where(f => f.AssessmentId == assessmentId)
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.Title)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} findings for assessment {AssessmentId}", 
                findings.Count, assessmentId);

            return findings.Select(MapEntityToFinding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving findings for assessment {AssessmentId}", assessmentId);
            throw;
        }
    }

    /// <summary>
    /// Marks a finding as resolved
    /// </summary>
    public async Task<bool> ResolveFindingAsync(string assessmentId, string findingId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resolving finding {FindingId} in assessment {AssessmentId}", 
                findingId, assessmentId);

            var finding = await _context.ComplianceFindings
                .FirstOrDefaultAsync(f => f.AssessmentId == assessmentId && f.FindingId == findingId, 
                    cancellationToken);

            if (finding == null)
            {
                _logger.LogWarning("Finding {FindingId} not found in assessment {AssessmentId}", 
                    findingId, assessmentId);
                return false;
            }

            finding.ResolvedAt = DateTime.UtcNow;
            finding.ComplianceStatus = "Resolved";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully resolved finding {FindingId}", findingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving finding {FindingId} in assessment {AssessmentId}", 
                findingId, assessmentId);
            throw;
        }
    }

    #region Private Mapping Methods

    private static ComplianceFinding MapFindingToEntity(string assessmentId, AtoFinding finding)
    {
        return new ComplianceFinding
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            FindingId = finding.Id,
            RuleId = finding.RuleId,
            Title = finding.Title,
            Description = finding.Description,
            Severity = finding.Severity.ToString(),
            ComplianceStatus = finding.ComplianceStatus.ToString(),
            FindingType = finding.FindingType.ToString(),
            ResourceId = finding.ResourceId,
            ResourceType = finding.ResourceType,
            ResourceName = finding.ResourceName,
            ControlId = finding.AffectedControls.FirstOrDefault(),
            ComplianceFrameworks = JsonSerializer.Serialize(finding.ComplianceFrameworks),
            AffectedNistControls = JsonSerializer.Serialize(finding.AffectedNistControls),
            Evidence = finding.Evidence,
            Remediation = finding.RemediationGuidance,
            Metadata = JsonSerializer.Serialize(finding.Metadata),
            IsRemediable = finding.IsRemediable,
            IsAutomaticallyFixable = finding.IsAutoRemediable,
            DetectedAt = finding.DetectedAt,
            ResolvedAt = null
        };
    }

    private static AtoFinding MapEntityToFinding(ComplianceFinding entity)
    {
        return new AtoFinding
        {
            Id = entity.FindingId,
            ResourceId = entity.ResourceId ?? string.Empty,
            ResourceName = entity.ResourceName ?? string.Empty,
            ResourceType = entity.ResourceType ?? string.Empty,
            SubscriptionId = string.Empty, // Not stored in entity
            ResourceGroupName = string.Empty, // Not stored in entity
            FindingType = Enum.TryParse<AtoFindingType>(entity.FindingType, out var type) 
                ? type : AtoFindingType.Security,
            Severity = Enum.TryParse<AtoFindingSeverity>(entity.Severity, out var severity) 
                ? severity : AtoFindingSeverity.Medium,
            ComplianceStatus = Enum.TryParse<AtoComplianceStatus>(entity.ComplianceStatus, out var status) 
                ? status : AtoComplianceStatus.NonCompliant,
            Title = entity.Title,
            Description = entity.Description,
            Recommendation = entity.Remediation ?? string.Empty,
            RuleId = entity.RuleId,
            RemediationGuidance = entity.Remediation ?? string.Empty,
            IsAutoRemediable = entity.IsAutomaticallyFixable,
            AffectedControls = entity.ControlId != null ? new List<string> { entity.ControlId } : new List<string>(),
            AffectedNistControls = DeserializeList(entity.AffectedNistControls),
            ComplianceFrameworks = DeserializeList(entity.ComplianceFrameworks),
            DetectedAt = entity.DetectedAt,
            Evidence = entity.Evidence,
            Metadata = DeserializeDictionary(entity.Metadata),
            IsRemediable = entity.IsRemediable,
            RemediationActions = new List<AtoRemediationAction>(),
            RemediationStatus = entity.ResolvedAt.HasValue 
                ? AtoRemediationStatus.Completed 
                : AtoRemediationStatus.NotStarted
        };
    }

    private AtoComplianceAssessment MapEntityToAssessment(ComplianceAssessment entity)
    {
        var controlFamilyResults = new Dictionary<string, ControlFamilyAssessment>();
        
        if (!string.IsNullOrEmpty(entity.Results))
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<Dictionary<string, ControlFamilyAssessment>>(entity.Results);
                if (deserialized != null)
                {
                    controlFamilyResults = deserialized;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize control family results for assessment {AssessmentId}", entity.Id);
            }
        }

        return new AtoComplianceAssessment
        {
            AssessmentId = entity.Id,
            SubscriptionId = entity.SubscriptionId,
            StartTime = new DateTimeOffset(entity.StartedAt),
            EndTime = entity.CompletedAt.HasValue 
                ? new DateTimeOffset(entity.CompletedAt.Value) 
                : DateTimeOffset.UtcNow,
            Duration = entity.Duration.HasValue ? TimeSpan.FromTicks(entity.Duration.Value) : TimeSpan.Zero,
            ControlFamilyResults = controlFamilyResults,
            OverallComplianceScore = (double)entity.ComplianceScore,
            TotalFindings = entity.TotalFindings,
            CriticalFindings = entity.CriticalFindings,
            HighFindings = entity.HighFindings,
            MediumFindings = entity.MediumFindings,
            LowFindings = entity.LowFindings,
            ExecutiveSummary = entity.ExecutiveSummary,
            RiskProfile = DeserializeRiskProfile(entity.RiskProfile),
            Error = null
        };
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static Dictionary<string, object> DeserializeDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    private RiskProfile? DeserializeRiskProfile(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<RiskProfile>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize risk profile");
            return null;
        }
    }

    #endregion
}
