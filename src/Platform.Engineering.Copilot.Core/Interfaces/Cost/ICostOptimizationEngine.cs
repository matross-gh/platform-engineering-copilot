using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Platform.Engineering.Copilot.Core.Models.CostOptimization;

namespace Platform.Engineering.Copilot.Core.Interfaces.Cost;

public interface ICostOptimizationEngine
{
    Task<CostAnalysisResult> AnalyzeSubscriptionAsync(string subscriptionId);
    Task<List<CostOptimizationRecommendation>> GenerateRecommendationsAsync(string resourceId);
    Task<ResourceUsagePattern> AnalyzeUsagePatternsAsync(string resourceId, string metricName, DateTime startDate, DateTime endDate);
    Task<bool> ApplyRecommendationAsync(string recommendationId, Dictionary<string, object>? parameters = null);
    Task<Dictionary<string, decimal>> CalculateSavingsPotentialAsync(List<CostOptimizationRecommendation> recommendations);
    
    // Anomaly detection and forecasting
    Task<List<Models.CostAnomaly>> DetectCostAnomaliesAsync(string subscriptionId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    Task<Models.CostForecast> GetCostForecastAsync(string subscriptionId, int forecastDays, CancellationToken cancellationToken = default);
    Task<Models.CostMonitoringDashboard> GetCostDashboardAsync(string subscriptionId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
}
