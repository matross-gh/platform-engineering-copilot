using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Data.Context;
using Platform.Engineering.Copilot.Core.Data.Entities;

namespace Platform.Engineering.Copilot.Core.Data.Repositories;

/// <summary>
/// EF Core implementation of environment deployment repository.
/// Manages EnvironmentDeployments and related DeploymentHistory and EnvironmentMetrics.
/// </summary>
public class EnvironmentDeploymentRepository : IEnvironmentDeploymentRepository
{
    private readonly PlatformEngineeringCopilotContext _context;
    private readonly ILogger<EnvironmentDeploymentRepository> _logger;

    public EnvironmentDeploymentRepository(
        PlatformEngineeringCopilotContext context,
        ILogger<EnvironmentDeploymentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== Deployment Operations ====================

    public async Task<EnvironmentDeployment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EnvironmentDeployments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
    }

    public async Task<EnvironmentDeployment?> GetByIdWithRelatedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Cosmos containers are independent - the EF Cosmos provider doesn't support
        // cross-container Include(). Fetch the root document, then fetch each related
        // container filtered by DeploymentId and assemble in memory instead.
        var deployment = await _context.EnvironmentDeployments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

        if (deployment == null)
            return null;

        await PopulateTemplateAsync(deployment, cancellationToken);

        deployment.History = await _context.DeploymentHistory
            .Where(h => h.DeploymentId == id)
            .ToListAsync(cancellationToken);

        deployment.Metrics = await _context.EnvironmentMetrics
            .Where(m => m.DeploymentId == id)
            .ToListAsync(cancellationToken);

        deployment.ScalingPolicies = await _context.ScalingPolicies
            .Where(s => s.DeploymentId == id)
            .ToListAsync(cancellationToken);

        return deployment;
    }

    /// <summary>
    /// Populate the (NotMapped) Template navigation property for a single deployment via a
    /// separate query, since Cosmos doesn't support cross-container Include().
    /// </summary>
    private async Task PopulateTemplateAsync(EnvironmentDeployment deployment, CancellationToken cancellationToken)
    {
        if (deployment.TemplateId is not Guid templateId)
            return;

        deployment.Template = await _context.EnvironmentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
    }

    /// <summary>
    /// Batch-populate the (NotMapped) Template navigation property for a list of deployments
    /// via a single query, since Cosmos doesn't support cross-container Include().
    /// </summary>
    private async Task PopulateTemplatesAsync(IReadOnlyList<EnvironmentDeployment> deployments, CancellationToken cancellationToken)
    {
        var templateIds = deployments
            .Where(d => d.TemplateId.HasValue)
            .Select(d => d.TemplateId!.Value)
            .Distinct()
            .ToList();

        if (templateIds.Count == 0)
            return;

        var templates = await _context.EnvironmentTemplates
            .Where(t => templateIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var templatesById = templates.ToDictionary(t => t.Id);

        foreach (var deployment in deployments)
        {
            if (deployment.TemplateId is Guid templateId && templatesById.TryGetValue(templateId, out var template))
            {
                deployment.Template = template;
            }
        }
    }

    public async Task<EnvironmentDeployment?> GetByNameAndResourceGroupAsync(string name, string resourceGroup, CancellationToken cancellationToken = default)
    {
        return await _context.EnvironmentDeployments
            .FirstOrDefaultAsync(d => d.Name == name && d.ResourceGroupName == resourceGroup && !d.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<EnvironmentDeployment>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var deployments = await _context.EnvironmentDeployments
            .Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        await PopulateTemplatesAsync(deployments, cancellationToken);
        return deployments;
    }

    public async Task<IReadOnlyList<EnvironmentDeployment>> GetByTypeAsync(string environmentType, CancellationToken cancellationToken = default)
    {
        var deployments = await _context.EnvironmentDeployments
            .Where(d => d.EnvironmentType == environmentType && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        await PopulateTemplatesAsync(deployments, cancellationToken);
        return deployments;
    }

    public async Task<IReadOnlyList<EnvironmentDeployment>> GetByResourceGroupAsync(string resourceGroup, CancellationToken cancellationToken = default)
    {
        var deployments = await _context.EnvironmentDeployments
            .Where(d => d.ResourceGroupName == resourceGroup && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        await PopulateTemplatesAsync(deployments, cancellationToken);
        return deployments;
    }

    public async Task<IReadOnlyList<EnvironmentDeployment>> GetByStatusAsync(DeploymentStatus status, CancellationToken cancellationToken = default)
    {
        var deployments = await _context.EnvironmentDeployments
            .Where(d => d.Status == status && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        await PopulateTemplatesAsync(deployments, cancellationToken);
        return deployments;
    }

    public async Task<IReadOnlyList<EnvironmentDeployment>> GetBySubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        var deployments = await _context.EnvironmentDeployments
            .Where(d => d.SubscriptionId == subscriptionId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        await PopulateTemplatesAsync(deployments, cancellationToken);
        return deployments;
    }

    public async Task<IReadOnlyList<EnvironmentDeployment>> GetWithActivePollingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EnvironmentDeployments
            .Where(d => d.IsPollingActive && !d.IsDeleted)
            .OrderBy(d => d.LastPolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnvironmentDeployment>> SearchAsync(
        string? environmentType = null,
        string? resourceGroup = null,
        DeploymentStatus? status = null,
        string? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EnvironmentDeployments
            .Where(d => !d.IsDeleted);

        if (!string.IsNullOrEmpty(environmentType))
        {
            query = query.Where(d => d.EnvironmentType == environmentType);
        }

        if (!string.IsNullOrEmpty(resourceGroup))
        {
            query = query.Where(d => d.ResourceGroupName == resourceGroup);
        }

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(subscriptionId))
        {
            query = query.Where(d => d.SubscriptionId == subscriptionId);
        }

        var deployments = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        await PopulateTemplatesAsync(deployments, cancellationToken);
        return deployments;
    }

    public async Task<bool> ExistsAsync(string name, string resourceGroup, CancellationToken cancellationToken = default)
    {
        return await _context.EnvironmentDeployments
            .AnyAsync(d => d.Name == name && d.ResourceGroupName == resourceGroup && !d.IsDeleted, cancellationToken);
    }

    public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EnvironmentDeployments
            .CountAsync(d => !d.IsDeleted, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(DeploymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.EnvironmentDeployments
            .CountAsync(d => d.Status == status && !d.IsDeleted, cancellationToken);
    }

    public async Task<EnvironmentDeployment> AddAsync(EnvironmentDeployment deployment, CancellationToken cancellationToken = default)
    {
        deployment.Id = deployment.Id == Guid.Empty ? Guid.NewGuid() : deployment.Id;
        deployment.CreatedAt = DateTime.UtcNow;
        deployment.UpdatedAt = DateTime.UtcNow;

        _context.EnvironmentDeployments.Add(deployment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created EnvironmentDeployment {DeploymentId}: {DeploymentName}", deployment.Id, deployment.Name);

        return deployment;
    }

    public async Task<EnvironmentDeployment> UpdateAsync(EnvironmentDeployment deployment, CancellationToken cancellationToken = default)
    {
        deployment.UpdatedAt = DateTime.UtcNow;

        _context.EnvironmentDeployments.Update(deployment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated EnvironmentDeployment {DeploymentId}: {DeploymentName}", deployment.Id, deployment.Name);

        return deployment;
    }

    public async Task<bool> UpdateStatusAsync(Guid deploymentId, DeploymentStatus status, CancellationToken cancellationToken = default)
    {
        var deployment = await _context.EnvironmentDeployments.FindAsync(new object[] { deploymentId }, cancellationToken);
        if (deployment == null)
        {
            _logger.LogWarning("Deployment {DeploymentId} not found for status update", deploymentId);
            return false;
        }

        deployment.Status = status;
        deployment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated deployment {DeploymentId} status to {Status}", deploymentId, status);
        return true;
    }

    public async Task<bool> UpdatePollingStatusAsync(
        Guid deploymentId,
        bool isPollingActive,
        int? pollingAttempts = null,
        TimeSpan? currentPollingInterval = null,
        int? progressPercentage = null,
        TimeSpan? estimatedTimeRemaining = null,
        CancellationToken cancellationToken = default)
    {
        var deployment = await _context.EnvironmentDeployments.FindAsync(new object[] { deploymentId }, cancellationToken);
        if (deployment == null)
        {
            _logger.LogWarning("Deployment {DeploymentId} not found for polling status update", deploymentId);
            return false;
        }

        deployment.IsPollingActive = isPollingActive;
        deployment.LastPolledAt = DateTime.UtcNow;
        
        if (pollingAttempts.HasValue)
            deployment.PollingAttempts = pollingAttempts.Value;
        
        if (currentPollingInterval.HasValue)
            deployment.CurrentPollingInterval = currentPollingInterval.Value;
        
        if (progressPercentage.HasValue)
            deployment.ProgressPercentage = progressPercentage.Value;
        
        if (estimatedTimeRemaining.HasValue)
            deployment.EstimatedTimeRemaining = estimatedTimeRemaining.Value;
        
        deployment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated deployment {DeploymentId} polling status: Active={IsActive}", deploymentId, isPollingActive);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deployment = await _context.EnvironmentDeployments.FindAsync(new object[] { id }, cancellationToken);
        if (deployment == null)
            return false;

        deployment.IsDeleted = true;
        deployment.DeletedAt = DateTime.UtcNow;
        deployment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Soft deleted EnvironmentDeployment {DeploymentId}", id);
        return true;
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deployment = await _context.EnvironmentDeployments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (deployment == null)
            return false;

        // Cosmos has no cross-container cascade delete - each related container must be
        // cleaned up manually (was previously done via .Include() + RemoveRange()).
        var history = await _context.DeploymentHistory.Where(h => h.DeploymentId == id).ToListAsync(cancellationToken);
        if (history.Count > 0)
        {
            _context.DeploymentHistory.RemoveRange(history);
        }

        var metrics = await _context.EnvironmentMetrics.Where(m => m.DeploymentId == id).ToListAsync(cancellationToken);
        if (metrics.Count > 0)
        {
            _context.EnvironmentMetrics.RemoveRange(metrics);
        }

        var scalingPolicies = await _context.ScalingPolicies.Where(s => s.DeploymentId == id).ToListAsync(cancellationToken);
        if (scalingPolicies.Count > 0)
        {
            _context.ScalingPolicies.RemoveRange(scalingPolicies);
        }

        _context.EnvironmentDeployments.Remove(deployment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Hard deleted EnvironmentDeployment {DeploymentId} with all related entities", id);
        return true;
    }

    // ==================== Deployment History Operations ====================

    public async Task<IReadOnlyList<DeploymentHistory>> GetHistoryAsync(Guid deploymentId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.DeploymentHistory
            .Where(h => h.DeploymentId == deploymentId)
            .OrderByDescending(h => h.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeploymentHistory?> GetLatestHistoryAsync(Guid deploymentId, CancellationToken cancellationToken = default)
    {
        return await _context.DeploymentHistory
            .Where(h => h.DeploymentId == deploymentId)
            .OrderByDescending(h => h.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DeploymentHistory> AddHistoryAsync(DeploymentHistory history, CancellationToken cancellationToken = default)
    {
        history.Id = history.Id == Guid.Empty ? Guid.NewGuid() : history.Id;

        _context.DeploymentHistory.Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Added DeploymentHistory {HistoryId} for deployment {DeploymentId}: {Action}", 
            history.Id, history.DeploymentId, history.Action);

        return history;
    }

    public async Task<DeploymentHistory> RecordActionAsync(
        Guid deploymentId,
        string action,
        string initiatedBy,
        bool success,
        string? details = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var history = new DeploymentHistory
        {
            Id = Guid.NewGuid(),
            DeploymentId = deploymentId,
            Action = action,
            Status = success ? "succeeded" : "failed",
            InitiatedBy = initiatedBy,
            Details = details,
            ErrorMessage = errorMessage,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Duration = TimeSpan.Zero
        };

        return await AddHistoryAsync(history, cancellationToken);
    }

    // ==================== Environment Metrics Operations ====================

    public async Task<IReadOnlyList<EnvironmentMetrics>> GetMetricsAsync(
        Guid deploymentId,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? metricType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EnvironmentMetrics
            .Where(m => m.DeploymentId == deploymentId);

        if (startTime.HasValue)
        {
            query = query.Where(m => m.Timestamp >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            query = query.Where(m => m.Timestamp <= endTime.Value);
        }

        if (!string.IsNullOrEmpty(metricType))
        {
            query = query.Where(m => m.MetricType == metricType);
        }

        return await query
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, MetricsSummary>> GetMetricsSummaryAsync(Guid deploymentId, CancellationToken cancellationToken = default)
    {
        // The Cosmos EF provider has limited/unreliable translation support for GroupBy
        // combined with multiple aggregate projections (Average/Min/Max) in a single query.
        // Materialize the (already deployment-scoped) metrics and aggregate client-side instead.
        var metrics = await _context.EnvironmentMetrics
            .Where(m => m.DeploymentId == deploymentId)
            .ToListAsync(cancellationToken);

        var summary = new Dictionary<string, MetricsSummary>();
        foreach (var group in metrics.GroupBy(m => m.MetricType))
        {
            summary[group.Key] = new MetricsSummary
            {
                Count = group.Count(),
                AvgValue = group.Average(m => m.Value),
                MinValue = group.Min(m => m.Value),
                MaxValue = group.Max(m => m.Value),
                LastTimestamp = group.Max(m => m.Timestamp)
            };
        }

        return summary;
    }

    public async Task<EnvironmentMetrics> AddMetricAsync(EnvironmentMetrics metric, CancellationToken cancellationToken = default)
    {
        metric.Id = metric.Id == Guid.Empty ? Guid.NewGuid() : metric.Id;
        metric.Timestamp = metric.Timestamp == default ? DateTime.UtcNow : metric.Timestamp;

        _context.EnvironmentMetrics.Add(metric);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Added EnvironmentMetric {MetricId} for deployment {DeploymentId}: {MetricType}/{MetricName}", 
            metric.Id, metric.DeploymentId, metric.MetricType, metric.MetricName);

        return metric;
    }

    public async Task<IReadOnlyList<EnvironmentMetrics>> AddMetricsAsync(IEnumerable<EnvironmentMetrics> metrics, CancellationToken cancellationToken = default)
    {
        var metricList = metrics.ToList();
        foreach (var metric in metricList)
        {
            metric.Id = metric.Id == Guid.Empty ? Guid.NewGuid() : metric.Id;
            metric.Timestamp = metric.Timestamp == default ? DateTime.UtcNow : metric.Timestamp;
        }

        await _context.EnvironmentMetrics.AddRangeAsync(metricList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Added {Count} EnvironmentMetrics", metricList.Count);

        return metricList;
    }

    public async Task<int> DeleteMetricsOlderThanAsync(Guid deploymentId, DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldMetrics = await _context.EnvironmentMetrics
            .Where(m => m.DeploymentId == deploymentId && m.Timestamp < cutoffDate)
            .ToListAsync(cancellationToken);

        if (!oldMetrics.Any())
            return 0;

        _context.EnvironmentMetrics.RemoveRange(oldMetrics);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Deleted {Count} metrics older than {CutoffDate} for deployment {DeploymentId}", 
            oldMetrics.Count, cutoffDate, deploymentId);

        return oldMetrics.Count;
    }
}
