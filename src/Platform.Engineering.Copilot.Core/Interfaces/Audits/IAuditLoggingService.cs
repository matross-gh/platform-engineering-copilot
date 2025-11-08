using Platform.Engineering.Copilot.Core.Models.Audits;

namespace Platform.Engineering.Copilot.Core.Interfaces.Audits;

/// <summary>
/// Interface for comprehensive audit logging
/// </summary>
public interface IAuditLoggingService
{
    Task<string> LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task<AuditSearchResult> SearchAsync(AuditSearchQuery query, CancellationToken cancellationToken = default);
    Task<ResourceAuditTrail> GetResourceAuditTrailAsync(string resourceId, CancellationToken cancellationToken = default);
    Task<AuditReport> GenerateReportAsync(string reportType, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    Task<List<AuditAnomaly>> DetectAnomaliesAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    Task<bool> ArchiveLogsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);
    Task<AuditConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task UpdateConfigurationAsync(AuditConfiguration configuration, CancellationToken cancellationToken = default);
}