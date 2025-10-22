using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces;

namespace Platform.Engineering.Copilot.Core.Services.Jobs;

/// <summary>
/// Background service that periodically cleans up expired jobs
/// Runs every hour and removes jobs older than 24 hours
/// </summary>
public class JobCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    
    public JobCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<JobCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job cleanup background service started. Cleanup runs every {Interval}", _cleanupInterval);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                // Create a scope to get the job service
                using var scope = _serviceProvider.CreateScope();
                var jobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                
                var cleanedCount = await jobService.CleanupExpiredJobsAsync(stoppingToken);
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Job cleanup completed. Removed {Count} expired jobs", cleanedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during job cleanup");
            }
        }
        
        _logger.LogInformation("Job cleanup background service stopped");
    }
}
