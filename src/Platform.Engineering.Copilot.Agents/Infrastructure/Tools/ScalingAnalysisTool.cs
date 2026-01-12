using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Infrastructure.Configuration;
using Platform.Engineering.Copilot.Agents.Infrastructure.State;
using Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;
using Platform.Engineering.Copilot.Core.Models.PredictiveScaling;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.Tools;

/// <summary>
/// Tool for predictive scaling analysis and optimization recommendations.
/// Uses IPredictiveScalingEngine for AI-powered predictions.
/// </summary>
public class ScalingAnalysisTool : BaseTool
{
    private readonly InfrastructureStateAccessors _stateAccessors;
    private readonly InfrastructureAgentOptions _options;
    private readonly IPredictiveScalingEngine? _scalingEngine;

    public override string Name => "analyze_scaling";

    public override string Description =>
        "Analyzes Azure resource metrics and predicts scaling needs. Provides AI-powered " +
        "recommendations for autoscaling, capacity planning, and cost optimization.";

    public ScalingAnalysisTool(
        ILogger<ScalingAnalysisTool> logger,
        InfrastructureStateAccessors stateAccessors,
        IOptions<InfrastructureAgentOptions> options,
        IPredictiveScalingEngine? scalingEngine = null) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new InfrastructureAgentOptions();
        _scalingEngine = scalingEngine;

        Parameters.Add(new ToolParameter("resource_id", "Azure resource ID to analyze", true));
        Parameters.Add(new ToolParameter("analysis_type", "Type: 'predict', 'optimize', 'performance', or 'all'. Default: predict", false));
        Parameters.Add(new ToolParameter("prediction_hours", "Hours ahead to predict. Default: 24", false));
        Parameters.Add(new ToolParameter("include_cost_analysis", "Include cost impact. Default: true", false));
        Parameters.Add(new ToolParameter("min_confidence", "Min confidence threshold (0-1). Default: 0.7", false));
        Parameters.Add(new ToolParameter("apply_recommendation", "Apply the scaling recommendation automatically. Default: false", false));
        Parameters.Add(new ToolParameter("conversation_id", "Conversation ID for state tracking", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!_options.EnablePredictiveScaling)
                return ToJson(new { success = false, error = "Predictive scaling is disabled in configuration" });

            var resourceId = GetOptionalString(arguments, "resource_id")
                ?? throw new ArgumentException("resource_id is required");
            var analysisType = GetOptionalString(arguments, "analysis_type") ?? "predict";
            var predictionHours = GetOptionalInt(arguments, "prediction_hours") ?? _options.Scaling.DefaultPredictionHours;
            var includeCostAnalysis = GetOptionalBool(arguments, "include_cost_analysis", true);
            var minConfidence = GetOptionalDecimal(arguments, "min_confidence") ?? (decimal)_options.Scaling.MinConfidenceThreshold;
            var applyRecommendation = GetOptionalBool(arguments, "apply_recommendation", false);
            var conversationId = GetOptionalString(arguments, "conversation_id") ?? Guid.NewGuid().ToString();

            Logger.LogInformation("Analyzing scaling for {ResourceId}, Type={Type}, Hours={Hours}", 
                resourceId, analysisType, predictionHours);

            var resourceType = ParseResourceType(resourceId);
            if (resourceType == null)
                return ToJson(new { success = false, error = "Could not determine resource type from resource ID" });

            // Use PredictiveScalingEngine if available
            if (_scalingEngine != null)
            {
                return await ExecuteWithScalingEngineAsync(
                    resourceId, resourceType, analysisType, predictionHours, 
                    includeCostAnalysis, (double)minConfidence, applyRecommendation,
                    conversationId, startTime, cancellationToken);
            }

            // Fallback to simulated predictions
            Logger.LogWarning("PredictiveScalingEngine not available, using simulated predictions");
            return await ExecuteWithSimulatedDataAsync(
                resourceId, resourceType, analysisType, predictionHours,
                includeCostAnalysis, (double)minConfidence, conversationId, 
                startTime, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during scaling analysis");
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private async Task<string> ExecuteWithScalingEngineAsync(
        string resourceId, string resourceType, string analysisType, int predictionHours,
        bool includeCostAnalysis, double minConfidence, bool applyRecommendation,
        string conversationId, DateTime startTime, CancellationToken cancellationToken)
    {
        var result = new StringBuilder();
        PredictiveScalingRecommendation? prediction = null;
        ScalingPerformanceMetrics? performance = null;
        PredictiveScalingConfiguration? optimizedConfig = null;

        // Generate prediction
        if (analysisType is "predict" or "all")
        {
            var targetTime = DateTime.UtcNow.AddHours(predictionHours);
            prediction = await _scalingEngine!.GeneratePredictionAsync(resourceId, targetTime);
            
            result.AppendLine("## 🔮 Scaling Prediction Analysis");
            result.AppendLine();
            result.AppendLine($"📍 **Resource:** `{resourceId}`");
            result.AppendLine($"⏰ **Prediction Window:** {predictionHours} hours ahead");
            result.AppendLine($"📊 **Confidence Score:** {prediction.ConfidenceScore:P1}");
            result.AppendLine();
            result.AppendLine("### 📈 Current State");
            result.AppendLine($"- **Current Instances:** {prediction.CurrentInstances}");
            result.AppendLine($"- **Predicted Load:** {prediction.PredictedLoad:F1}%");
            result.AppendLine();
            result.AppendLine("### 🎯 Recommendation");
            result.AppendLine($"- **Action:** {GetActionEmoji(prediction.RecommendedAction)} {prediction.RecommendedAction}");
            result.AppendLine($"- **Target Instances:** {prediction.RecommendedInstances}");
            result.AppendLine();
            
            if (!string.IsNullOrEmpty(prediction.Reasoning))
            {
                result.AppendLine("### 💡 Reasoning");
                result.AppendLine(prediction.Reasoning);
                result.AppendLine();
            }

            // Show metric predictions
            if (prediction.MetricPredictions.Any())
            {
                result.AppendLine("### 📊 Metric Predictions");
                foreach (var metric in prediction.MetricPredictions)
                {
                    var latestPrediction = metric.Predictions.LastOrDefault();
                    if (latestPrediction != null)
                    {
                        result.AppendLine($"- **{metric.MetricName}:** {latestPrediction.Value:F1} " +
                            $"(range: {latestPrediction.LowerBound:F1} - {latestPrediction.UpperBound:F1})");
                    }
                }
                result.AppendLine();
            }
        }

        // Analyze performance history
        if (analysisType is "performance" or "all")
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);
            performance = await _scalingEngine!.AnalyzeScalingPerformanceAsync(resourceId, startDate, endDate);
            
            result.AppendLine("### 📈 Scaling Performance (Last 30 Days)");
            result.AppendLine($"- **Total Scaling Events:** {performance.TotalScalingEvents}");
            result.AppendLine($"- **Success Rate:** {(performance.TotalScalingEvents > 0 ? (double)performance.SuccessfulScalingEvents / performance.TotalScalingEvents : 0):P1}");
            result.AppendLine($"- **Avg Response Time:** {performance.AverageResponseTime:F1}s");
            result.AppendLine($"- **Over-provisioned:** {performance.OverProvisioningPercentage:F1}% of time");
            result.AppendLine($"- **Under-provisioned:** {performance.UnderProvisioningPercentage:F1}% of time");
            result.AppendLine($"- **Cost Savings Achieved:** {performance.CostSavingsPercentage:F1}%");
            result.AppendLine();
        }

        // Get optimization recommendations
        if (analysisType is "optimize" or "all")
        {
            optimizedConfig = await _scalingEngine!.OptimizeScalingConfigurationAsync(resourceId);
            
            result.AppendLine("### ⚙️ Optimized Configuration");
            result.AppendLine($"- **Strategy:** {optimizedConfig.Strategy}");
            result.AppendLine($"- **Min Instances:** {optimizedConfig.Constraints.MinimumInstances}");
            result.AppendLine($"- **Max Instances:** {optimizedConfig.Constraints.MaximumInstances}");
            result.AppendLine($"- **Scale Up Threshold:** {optimizedConfig.Thresholds.ScaleUpThreshold}%");
            result.AppendLine($"- **Scale Down Threshold:** {optimizedConfig.Thresholds.ScaleDownThreshold}%");
            result.AppendLine($"- **Cooldown Period:** {optimizedConfig.Thresholds.CooldownMinutes} minutes");
            result.AppendLine();
        }

        // Apply recommendation if requested
        bool applied = false;
        if (applyRecommendation && prediction != null && prediction.ConfidenceScore >= minConfidence)
        {
            if (prediction.RecommendedAction != ScalingAction.None)
            {
                applied = await _scalingEngine!.ApplyScalingRecommendationAsync(prediction);
                result.AppendLine($"### ✅ Recommendation Applied: {(applied ? "Success" : "Failed")}");
                result.AppendLine();
            }
        }

        // Track operation
        await _stateAccessors.TrackInfrastructureOperationAsync(
            conversationId, "scaling_analysis", resourceType,
            ExtractSubscriptionId(resourceId) ?? "", true,
            DateTime.UtcNow - startTime, cancellationToken);

        return ToJson(new
        {
            success = true,
            markdown = result.ToString(),
            analysis = new
            {
                resourceId,
                resourceType,
                analysisType,
                predictionHorizonHours = predictionHours
            },
            prediction = prediction != null ? new
            {
                currentInstances = prediction.CurrentInstances,
                recommendedInstances = prediction.RecommendedInstances,
                recommendedAction = prediction.RecommendedAction.ToString(),
                predictedLoad = prediction.PredictedLoad,
                confidenceScore = prediction.ConfidenceScore,
                reasoning = prediction.Reasoning,
                meetsConfidenceThreshold = prediction.ConfidenceScore >= minConfidence
            } : null,
            performance = performance != null ? new
            {
                totalEvents = performance.TotalScalingEvents,
                successRate = performance.TotalScalingEvents > 0 
                    ? (double)performance.SuccessfulScalingEvents / performance.TotalScalingEvents 
                    : 0,
                overProvisioningPct = performance.OverProvisioningPercentage,
                underProvisioningPct = performance.UnderProvisioningPercentage,
                costSavingsPct = performance.CostSavingsPercentage
            } : null,
            optimizedConfig = optimizedConfig != null ? new
            {
                strategy = optimizedConfig.Strategy.ToString(),
                minInstances = optimizedConfig.Constraints.MinimumInstances,
                maxInstances = optimizedConfig.Constraints.MaximumInstances,
                scaleUpThreshold = optimizedConfig.Thresholds.ScaleUpThreshold,
                scaleDownThreshold = optimizedConfig.Thresholds.ScaleDownThreshold
            } : null,
            applied,
            engineUsed = "PredictiveScalingEngine"
        });
    }

    private static string GetActionEmoji(ScalingAction action) => action switch
    {
        ScalingAction.ScaleUp => "⬆️",
        ScalingAction.ScaleDown => "⬇️",
        ScalingAction.EmergencyScale => "🚨",
        ScalingAction.Scheduled => "📅",
        _ => "➖"
    };

    private async Task<string> ExecuteWithSimulatedDataAsync(
        string resourceId, string resourceType, string analysisType, int predictionHours,
        bool includeCostAnalysis, double minConfidence, string conversationId,
        DateTime startTime, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulate async work

        var predictions = GenerateSimulatedPredictions(resourceType, predictionHours);
        var recommendations = GenerateSimulatedRecommendations(resourceType, minConfidence);
        var costAnalysis = includeCostAnalysis ? AnalyzeCostImpact() : null;

        await _stateAccessors.TrackInfrastructureOperationAsync(
            conversationId, "scaling_analysis", resourceType,
            ExtractSubscriptionId(resourceId) ?? "", true,
            DateTime.UtcNow - startTime, cancellationToken);

        return ToJson(new
        {
            success = true,
            analysis = new { resourceId, resourceType, analysisType, predictionHorizonHours = predictionHours },
            predictions,
            recommendations = recommendations.Where(r => r.Confidence >= minConfidence),
            costImpact = costAnalysis,
            summary = $"Found {predictions.Count} predictions and {recommendations.Count} recommendations",
            engineUsed = "Simulated",
            warning = "PredictiveScalingEngine not available - results are simulated"
        });
    }

    private string? ParseResourceType(string resourceId)
    {
        var parts = resourceId.Split('/');
        for (int i = 0; i < parts.Length - 2; i++)
            if (parts[i].Equals("providers", StringComparison.OrdinalIgnoreCase))
                return $"{parts[i + 1]}/{parts[i + 2]}";
        return null;
    }

    private string? ExtractSubscriptionId(string resourceId)
    {
        var parts = resourceId.Split('/');
        for (int i = 0; i < parts.Length - 1; i++)
            if (parts[i].Equals("subscriptions", StringComparison.OrdinalIgnoreCase))
                return parts[i + 1];
        return null;
    }

    private List<object> GenerateSimulatedPredictions(string resourceType, int predictionHours)
    {
        var metrics = resourceType switch
        {
            "Microsoft.ContainerService/managedClusters" => new[] { "CPU Utilization", "Memory Utilization", "Pod Count" },
            "Microsoft.Web/serverfarms" => new[] { "CPU Percentage", "Memory Percentage", "HTTP Queue Length" },
            "Microsoft.Compute/virtualMachineScaleSets" => new[] { "CPU Utilization", "Memory Utilization", "Network In/Out" },
            _ => new[] { "CPU Utilization", "Memory Utilization" }
        };

        return metrics.Select(m => new
        {
            metric = m,
            currentValue = Random.Shared.Next(40, 60),
            predictedValue = Random.Shared.Next(50, 85),
            trend = Random.Shared.Next(0, 2) == 0 ? "increasing" : "stable",
            confidence = 0.75 + Random.Shared.NextDouble() * 0.2,
            predictionHorizon = $"{predictionHours} hours"
        }).Cast<object>().ToList();
    }

    private List<RecommendationModel> GenerateSimulatedRecommendations(string resourceType, double minConfidence)
    {
        var recommendations = new List<RecommendationModel>
        {
            new() { Category = "Autoscaling", Priority = "High", Title = "Enable Cluster Autoscaler",
                Description = "Configure autoscaler for automatic node adjustment based on demand", Confidence = 0.92 },
            new() { Category = "Cost Optimization", Priority = "Medium", Title = "Review Instance Sizing",
                Description = "Current instances may be over-provisioned during off-peak hours", Confidence = 0.78 },
            new() { Category = "Performance", Priority = "Medium", Title = "Pre-scale for Peak Hours",
                Description = "Historical data shows increased load between 9AM-11AM UTC", Confidence = 0.85 }
        };
        return recommendations;
    }

    private object AnalyzeCostImpact()
    {
        var current = Random.Shared.Next(500, 2000);
        var savings = current * Random.Shared.Next(10, 25) / 100.0;
        return new
        {
            currentMonthlyCost = current,
            projectedMonthlyCost = current - savings,
            potentialSavings = Math.Round(savings, 2),
            savingsPercentage = (int)(savings / current * 100),
            recommendation = savings > 100 ? "Consider right-sizing or reserved instances" : "Current sizing is near-optimal"
        };
    }

    private class RecommendationModel
    {
        public string Category { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public double Confidence { get; set; }
    }
}
