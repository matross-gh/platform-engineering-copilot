using Platform.Engineering.Copilot.Compliance.Core.Data.Entities;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using System.Text.Json;

namespace Platform.Engineering.Copilot.Agents.Compliance.Extensions;

/// <summary>
/// Maps between ComplianceFinding (database entity) and AtoFinding (domain model)
/// </summary>
public static class ComplianceFindingMapper
{
    /// <summary>
    /// Convert database entity to domain model
    /// </summary>
    public static AtoFinding ToModel(this ComplianceFinding entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var model = new AtoFinding
        {
            Id = entity.FindingId,
            ResourceId = entity.ResourceId ?? string.Empty,
            ResourceName = entity.ResourceName ?? string.Empty,
            ResourceType = entity.ResourceType ?? string.Empty,
            Title = entity.Title,
            Description = entity.Description,
            RuleId = entity.RuleId,
            RemediationGuidance = entity.Remediation ?? string.Empty,
            IsAutoRemediable = entity.IsAutomaticallyFixable,
            IsRemediable = entity.IsRemediable,
            DetectedAt = entity.DetectedAt,
            Evidence = entity.Evidence
        };

        // Parse severity
        if (Enum.TryParse<AtoFindingSeverity>(entity.Severity, ignoreCase: true, out var severity))
        {
            model.Severity = severity;
        }

        // Parse compliance status
        if (Enum.TryParse<AtoComplianceStatus>(entity.ComplianceStatus.Replace("NonCompliant", "NonCompliant"), 
            ignoreCase: true, out var status))
        {
            model.ComplianceStatus = status;
        }

        // Parse finding type
        if (Enum.TryParse<AtoFindingType>(entity.FindingType, ignoreCase: true, out var findingType))
        {
            model.FindingType = findingType;
        }

        // Parse affected controls
        if (!string.IsNullOrEmpty(entity.ControlId))
        {
            model.AffectedControls = new List<string> { entity.ControlId };
        }

        if (!string.IsNullOrEmpty(entity.AffectedNistControls))
        {
            try
            {
                var controls = JsonSerializer.Deserialize<List<string>>(entity.AffectedNistControls);
                if (controls != null)
                {
                    model.AffectedNistControls = controls;
                }
            }
            catch
            {
                // Fallback to single control
                if (!string.IsNullOrEmpty(entity.ControlId))
                {
                    model.AffectedNistControls = new List<string> { entity.ControlId };
                }
            }
        }

        // Parse compliance frameworks
        if (!string.IsNullOrEmpty(entity.ComplianceFrameworks))
        {
            try
            {
                var frameworks = JsonSerializer.Deserialize<List<string>>(entity.ComplianceFrameworks);
                if (frameworks != null)
                {
                    model.ComplianceFrameworks = frameworks;
                }
            }
            catch
            {
                // Ignore parse errors
            }
        }

        // Parse metadata
        if (!string.IsNullOrEmpty(entity.Metadata))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Metadata);
                if (metadata != null)
                {
                    model.Metadata = metadata;
                }
            }
            catch
            {
                // Ignore parse errors
            }
        }

        return model;
    }

    /// <summary>
    /// Convert domain model to database entity
    /// </summary>
    public static ComplianceFinding ToEntity(this AtoFinding model, string assessmentId)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var entity = new ComplianceFinding
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            FindingId = model.Id,
            RuleId = model.RuleId,
            Title = model.Title,
            Description = model.Description,
            Severity = model.Severity.ToString(),
            ComplianceStatus = model.ComplianceStatus.ToString(),
            FindingType = model.FindingType.ToString(),
            ResourceId = model.ResourceId,
            ResourceType = model.ResourceType,
            ResourceName = model.ResourceName,
            ControlId = model.AffectedControls.FirstOrDefault(),
            Remediation = model.RemediationGuidance,
            IsRemediable = model.IsRemediable,
            IsAutomaticallyFixable = model.IsAutoRemediable,
            DetectedAt = model.DetectedAt,
            Evidence = model.Evidence
        };

        // Serialize collections to JSON
        if (model.AffectedNistControls.Any())
        {
            entity.AffectedNistControls = JsonSerializer.Serialize(model.AffectedNistControls);
        }

        if (model.ComplianceFrameworks.Any())
        {
            entity.ComplianceFrameworks = JsonSerializer.Serialize(model.ComplianceFrameworks);
        }

        if (model.Metadata.Any())
        {
            entity.Metadata = JsonSerializer.Serialize(model.Metadata);
        }

        return entity;
    }

    /// <summary>
    /// Update entity from model (preserves entity ID and assessment relationship)
    /// </summary>
    public static void UpdateFromModel(this ComplianceFinding entity, AtoFinding model)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        entity.Title = model.Title;
        entity.Description = model.Description;
        entity.Severity = model.Severity.ToString();
        entity.ComplianceStatus = model.ComplianceStatus.ToString();
        entity.FindingType = model.FindingType.ToString();
        entity.Remediation = model.RemediationGuidance;
        entity.IsRemediable = model.IsRemediable;
        entity.IsAutomaticallyFixable = model.IsAutoRemediable;
        entity.Evidence = model.Evidence;

        if (model.AffectedControls.Any())
        {
            entity.ControlId = model.AffectedControls.First();
        }

        if (model.AffectedNistControls.Any())
        {
            entity.AffectedNistControls = JsonSerializer.Serialize(model.AffectedNistControls);
        }

        if (model.ComplianceFrameworks.Any())
        {
            entity.ComplianceFrameworks = JsonSerializer.Serialize(model.ComplianceFrameworks);
        }

        if (model.Metadata.Any())
        {
            entity.Metadata = JsonSerializer.Serialize(model.Metadata);
        }
    }
}
