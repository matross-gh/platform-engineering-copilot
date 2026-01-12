using Platform.Engineering.Copilot.Core.Models.PredictiveScaling;

namespace Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;

public interface IPredictiveScalingEngine
    {
        Task<PredictiveScalingRecommendation> GeneratePredictionAsync(string resourceId, DateTime targetTime);
        Task<List<MetricPrediction>> PredictMetricsAsync(string resourceId, List<string> metrics, int horizonHours);
        Task<bool> ApplyScalingRecommendationAsync(PredictiveScalingRecommendation recommendation);
        Task<ScalingPerformanceMetrics> AnalyzeScalingPerformanceAsync(string resourceId, DateTime startDate, DateTime endDate);
        Task<PredictiveScalingConfiguration> OptimizeScalingConfigurationAsync(string resourceId);
    }