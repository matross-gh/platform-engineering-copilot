using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Configuration;
using Platform.Engineering.Copilot.Core.Services.Audits;

namespace Platform.Engineering.Copilot.Core.Extensions;

/// <summary>
/// Service collection extensions for Governance and Compliance services
/// NOTE: Compliance services have been moved to Platform.Engineering.Copilot.Compliance.Core
/// This extension file may be deprecated in favor of domain-specific registrations
/// </summary>
public static class GovernanceServiceCollectionExtensions
{
    /// <summary>
    /// Add enhanced ATO compliance services with NIST integration
    /// DEPRECATED: Use Platform.Engineering.Copilot.Compliance.Core.Extensions.AddComplianceAgent() instead
    /// </summary>
    [Obsolete("Compliance services have been moved to Platform.Engineering.Copilot.Compliance.Core")]
    public static IServiceCollection AddEnhancedAtoCompliance(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // This method is deprecated - compliance services are now in the Compliance.Core domain project
        // Consumers should call services.AddComplianceAgent() instead
        return services;
    }

    /// <summary>
    /// Add ATO compliance background services
    /// DEPRECATED: Use Platform.Engineering.Copilot.Compliance.Core.Extensions.AddComplianceAgent() instead
    /// </summary>
    [Obsolete("Compliance services have been moved to Platform.Engineering.Copilot.Compliance.Core")]
    public static IServiceCollection AddAtoComplianceBackgroundServices(
        this IServiceCollection services)
    {
        // This method is deprecated - compliance services are now in the Compliance.Core domain project
        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning("Retry {RetryCount}/3 for NIST API call in {Delay}ms. Reason: {Reason}",
                        retryCount, timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    // Could log circuit breaker activation
                },
                onReset: () =>
                {
                    // Could log circuit breaker reset
                });
    }
}

/// <summary>
/// Polly context extensions for logging
/// </summary>
internal static class ContextExtensions
{
    private const string LoggerKey = "ILogger";

    public static Context WithLogger(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        return context.TryGetValue(LoggerKey, out var logger) ? logger as ILogger : null;
    }
}

public class DocumentProcessingOptions
{
    public string UploadsPath { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB
    public int ProcessingTimeoutMinutes { get; set; } = 30;
    public bool EnableAdvancedAnalysis { get; set; } = true;
    public string[] SupportedFileTypes { get; set; } = {
        ".pdf", ".docx", ".doc", ".vsdx", ".vsd", ".pptx", ".ppt", 
        ".xlsx", ".xls", ".txt", ".md", ".png", ".jpg", ".jpeg"
    };
}