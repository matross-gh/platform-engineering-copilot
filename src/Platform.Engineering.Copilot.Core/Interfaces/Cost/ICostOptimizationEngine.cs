using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Platform.Engineering.Copilot.Core.Models;
using CostAnalysisResult = Platform.Engineering.Copilot.Core.Models.CostOptimization.Analysis.CostAnalysisResult;
using CostOptimizationRecommendation = Platform.Engineering.Copilot.Core.Models.CostOptimization.Analysis.CostOptimizationRecommendation;
using ResourceUsagePattern = Platform.Engineering.Copilot.Core.Models.CostOptimization.Analysis.ResourceUsagePattern;

namespace Platform.Engineering.Copilot.Core.Interfaces.Cost;

public interface ICostOptimizationEngine
{
    Task<CostAnalysisResult> AnalyzeSubscriptionAsync(string subscriptionId);
    Task<List<CostOptimizationRecommendation>> GenerateRecommendationsAsync(string resourceId);
    Task<ResourceUsagePattern> AnalyzeUsagePatternsAsync(string resourceId, string metricName, DateTime startDate, DateTime endDate);
    Task<bool> ApplyRecommendationAsync(string recommendationId, Dictionary<string, object>? parameters = null);
    Task<Dictionary<string, decimal>> CalculateSavingsPotentialAsync(List<CostOptimizationRecommendation> recommendations);
    
    // Anomaly detection and forecasting
    Task<List<CostAnomaly>> DetectCostAnomaliesAsync(string subscriptionId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    Task<CostForecast> GetCostForecastAsync(string subscriptionId, int forecastDays, CancellationToken cancellationToken = default);
    Task<CostMonitoringDashboard> GetCostDashboardAsync(string subscriptionId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
}
