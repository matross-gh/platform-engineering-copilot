using System.ComponentModel;
using Platform.Engineering.Copilot.Agents.CostManagement.Tools;

namespace Platform.Engineering.Copilot.Mcp.Tools;

/// <summary>
/// MCP tools for cost management operations. Wraps Agent Framework cost tools
/// for exposure via the MCP protocol (GitHub Copilot, Claude Desktop, etc.)
/// </summary>
public class CostManagementMcpTools
{
    private readonly CostAnalysisTool _costAnalysisTool;
    private readonly CostOptimizationTool _costOptimizationTool;
    private readonly CostForecastTool _costForecastTool;
    private readonly BudgetManagementTool _budgetManagementTool;
    private readonly CostAnomalyTool _costAnomalyTool;
    private readonly CostScenarioTool _costScenarioTool;

    public CostManagementMcpTools(
        CostAnalysisTool costAnalysisTool,
        CostOptimizationTool costOptimizationTool,
        CostForecastTool costForecastTool,
        BudgetManagementTool budgetManagementTool,
        CostAnomalyTool costAnomalyTool,
        CostScenarioTool costScenarioTool)
    {
        _costAnalysisTool = costAnalysisTool;
        _costOptimizationTool = costOptimizationTool;
        _costForecastTool = costForecastTool;
        _budgetManagementTool = budgetManagementTool;
        _costAnomalyTool = costAnomalyTool;
        _costScenarioTool = costScenarioTool;
    }

    /// <summary>
    /// Analyze Azure costs
    /// </summary>
    [Description("Analyze Azure costs with breakdown by resource, service, location, or tags. Supports historical analysis and trend identification.")]
    public async Task<string> AnalyzeCostsAsync(
        string subscriptionId,
        string? resourceGroup = null,
        string? timeRange = null,
        string? groupBy = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["resource_group"] = resourceGroup,
            ["time_range"] = timeRange,
            ["group_by"] = groupBy
        };
        return await _costAnalysisTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Get cost optimization recommendations
    /// </summary>
    [Description("Get actionable cost optimization recommendations for Azure resources. Identifies unused resources, right-sizing opportunities, and reserved instance savings.")]
    public async Task<string> GetOptimizationRecommendationsAsync(
        string subscriptionId,
        string? resourceGroup = null,
        string? resourceType = null,
        decimal? minimumSavings = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["resource_group"] = resourceGroup,
            ["resource_type"] = resourceType,
            ["minimum_savings"] = minimumSavings
        };
        return await _costOptimizationTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Forecast future Azure costs
    /// </summary>
    [Description("Forecast future Azure costs based on historical trends and growth patterns. Includes confidence intervals and scenario modeling.")]
    public async Task<string> ForecastCostsAsync(
        string subscriptionId,
        int forecastMonths = 3,
        bool includeSeasonality = true,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["forecast_months"] = forecastMonths,
            ["include_seasonality"] = includeSeasonality
        };
        return await _costForecastTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Manage Azure budgets
    /// </summary>
    [Description("Create, update, and monitor Azure budgets. Set up alerts for budget thresholds and track spending against limits.")]
    public async Task<string> ManageBudgetAsync(
        string operation,
        string subscriptionId,
        string? budgetName = null,
        decimal? amount = null,
        string? timeGrain = null,
        decimal? alertThreshold = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["subscription_id"] = subscriptionId,
            ["budget_name"] = budgetName,
            ["amount"] = amount,
            ["time_grain"] = timeGrain,
            ["alert_threshold"] = alertThreshold
        };
        return await _budgetManagementTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Detect cost anomalies
    /// </summary>
    [Description("Detect unusual spending patterns and cost anomalies. Uses machine learning to identify unexpected cost spikes or drops.")]
    public async Task<string> DetectAnomaliesAsync(
        string subscriptionId,
        string? resourceGroup = null,
        int lookbackDays = 30,
        decimal sensitivityThreshold = 0.8m,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["subscription_id"] = subscriptionId,
            ["resource_group"] = resourceGroup,
            ["lookback_days"] = lookbackDays,
            ["sensitivity_threshold"] = sensitivityThreshold
        };
        return await _costAnomalyTool.ExecuteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Model cost scenarios
    /// </summary>
    [Description("Model and compare different cost scenarios. Useful for capacity planning, reserved instance analysis, and 'what-if' analysis.")]
    public async Task<string> ModelCostScenarioAsync(
        string scenarioType,
        string subscriptionId,
        Dictionary<string, object>? scenarioParameters = null,
        CancellationToken cancellationToken = default)
    {
        var args = new Dictionary<string, object?>
        {
            ["scenario_type"] = scenarioType,
            ["subscription_id"] = subscriptionId,
            ["scenario_parameters"] = scenarioParameters
        };
        return await _costScenarioTool.ExecuteAsync(args, cancellationToken);
    }
}
