using Microsoft.AspNetCore.Mvc;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;
using Platform.Engineering.Copilot.Core.Models;

namespace Platform.Engineering.Copilot.Admin.Controllers;

/// <summary>
/// Admin controller for cost management and cost insights
/// </summary>
[ApiController]
[Route("api/admin/costs")]
[Produces("application/json")]
public class CostAdminController : ControllerBase
{
    private readonly IAzureCostManagementService _costService;
    private readonly ILogger<CostAdminController> _logger;
    private readonly IConfiguration _configuration;

    public CostAdminController(
        ILogger<CostAdminController> logger,
        IConfiguration configuration,
        IAzureCostManagementService costService)
    {
        _costService = costService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get cost data for a specific environment
    /// </summary>
    /// <param name="envId">Environment ID</param>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <param name="subscriptionId">Optional Azure subscription ID (defaults to configured subscription)</param>
    /// <returns>Environment cost data including daily costs and service breakdown</returns>
    [HttpGet("environment/{envId}")]
    [ProducesResponseType(typeof(EnvironmentCostData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnvironmentCostData>> GetEnvironmentCost(
        string envId,
        [FromQuery] int days = 30,
        [FromQuery] string? subscriptionId = null)
    {
        try
        {
            _logger.LogInformation("Admin API: Getting cost data for environment {EnvId} for {Days} days", envId, days);

            if (string.IsNullOrWhiteSpace(envId))
            {
                return BadRequest("Environment ID is required");
            }

            if (days < 1 || days > 365)
            {
                return BadRequest("Days must be between 1 and 365");
            }

            // Use configured subscription if not provided
            var subId = subscriptionId ?? _configuration["Azure:SubscriptionId"];
            if (string.IsNullOrWhiteSpace(subId))
            {
                return BadRequest("No Azure subscription ID configured");
            }

            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-days);

            // Get cost dashboard data
            var dashboard = await _costService.GetCostDashboardAsync(subId, startDate, endDate);
            
            // Get resource-level cost breakdown
            var resourceBreakdown = await _costService.GetResourceCostBreakdownAsync(subId, startDate, endDate);
            
            // Filter resources by environment tag or resource group
            var envResources = resourceBreakdown
                .Where(r => r.ResourceName?.Contains(envId, StringComparison.OrdinalIgnoreCase) == true 
                         || r.ResourceGroup?.Contains(envId, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            // Get cost trends for the period
            var trends = await _costService.GetCostTrendsAsync(subId, startDate, endDate);

            // Build the response
            var response = new EnvironmentCostData
            {
                EnvironmentId = envId,
                EnvironmentName = envId, // TODO: Look up actual environment name
                TotalCost = envResources.Sum(r => r.MonthlyCost),
                Currency = "USD",
                Period = new CostPeriod
                {
                    StartDate = startDate.DateTime,
                    EndDate = endDate.DateTime,
                    Days = days
                },
                DailyCosts = trends.Select(t => new DailyCost
                {
                    Date = t.Date,
                    Cost = t.DailyCost,
                    Currency = "USD"
                }).ToList(),
                ServiceCosts = envResources
                    .GroupBy(r => r.ResourceType ?? "Unknown")
                    .Select(g => new ServiceCost
                    {
                        ServiceName = g.Key,
                        Cost = g.Sum(r => r.MonthlyCost),
                        Currency = "USD",
                        ResourceCount = g.Count()
                    })
                    .OrderByDescending(s => s.Cost)
                    .ToList(),
                Recommendations = (await _costService.GetOptimizationRecommendationsAsync(subId))
                    .Where(r => r.AffectedResources.Any(res => res.Contains(envId, StringComparison.OrdinalIgnoreCase)))
                    .Select(r => r.Description ?? "No description")
                    .ToList()
            };

            _logger.LogInformation(
                "Admin API: Retrieved cost data for environment {EnvId}: Total ${Total:F2}, {ResourceCount} resources",
                envId, response.TotalCost, envResources.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cost data for environment {EnvId}", envId);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving cost data");
        }
    }

    /// <summary>
    /// Get cost summary across all environments
    /// </summary>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <param name="subscriptionId">Optional Azure subscription ID (defaults to configured subscription)</param>
    /// <returns>Summary of costs across all environments</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(CostSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CostSummary>> GetCostSummary(
        [FromQuery] int days = 30,
        [FromQuery] string? subscriptionId = null)
    {
        try
        {
            _logger.LogInformation("Admin API: Getting cost summary for {Days} days", days);

            if (days < 1 || days > 365)
            {
                return BadRequest("Days must be between 1 and 365");
            }

            // Use configured subscription if not provided
            var subId = subscriptionId ?? _configuration["Azure:SubscriptionId"];
            if (string.IsNullOrWhiteSpace(subId))
            {
                return BadRequest("No Azure subscription ID configured");
            }

            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-days);

            // Get comprehensive cost dashboard
            var dashboard = await _costService.GetCostDashboardAsync(subId, startDate, endDate);
        
            // Get cost trends
            var trends = await _costService.GetCostTrendsAsync(subId, startDate, endDate);
            
            // Get optimization recommendations
                var recommendations = await _costService.GetOptimizationRecommendationsAsync(subId);
                
                // Get cost anomalies
                var anomalies = await _costService.DetectCostAnomaliesAsync(subId, startDate, endDate);
                
                // Get resource cost breakdown
                var resourceBreakdown = await _costService.GetResourceCostBreakdownAsync(subId, startDate, endDate);

                // Build the response
                var response = new CostSummary
                {
                    TotalCost = dashboard.Summary.CurrentMonthSpend,
                    Currency = "USD",
                    Period = new CostPeriod
                    {
                        StartDate = startDate.DateTime,
                        EndDate = endDate.DateTime,
                        Days = days
                    },
                    TrendPercentage = dashboard.Summary.MonthOverMonthChangePercent,
                    DailyCosts = trends.Select(t => new DailyCost
                    {
                        Date = t.Date,
                        Cost = t.DailyCost,
                        Currency = "USD"
                    }).ToList(),
                    TopServices = resourceBreakdown
                        .GroupBy(r => r.ResourceType ?? "Unknown")
                        .Select(g => new ServiceCost
                        {
                            ServiceName = g.Key,
                            Cost = g.Sum(r => r.MonthlyCost),
                            Currency = "USD",
                            ResourceCount = g.Count()
                        })
                        .OrderByDescending(s => s.Cost)
                        .Take(10)
                        .ToList(),
                    PotentialSavings = recommendations.Sum(r => r.PotentialMonthlySavings),
                    RecommendationCount = recommendations.Count,
                    AnomalyCount = anomalies.Count,
                    BudgetStatus = dashboard.Budgets?.FirstOrDefault() != null 
                        ? new BudgetStatusSummary
                        {
                            BudgetName = dashboard.Budgets.First().Name ?? "Monthly Budget",
                            BudgetAmount = dashboard.Budgets.First().Amount,
                            CurrentSpend = dashboard.Budgets.First().CurrentSpend,
                            PercentageUsed = dashboard.Budgets.First().UtilizationPercentage,
                            Status = dashboard.Budgets.First().HealthStatus.ToString()
                        }
                        : null
                };

                _logger.LogInformation(
                    "Admin API: Retrieved cost summary: Total ${Total:F2}, {Recommendations} recommendations, ${Savings:F2} potential savings",
                    response.TotalCost, response.RecommendationCount, response.PotentialSavings);

                return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cost summary");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving cost summary");
        }
    }

    /// <summary>
    /// Get cost forecast for the next N days
    /// </summary>
    /// <param name="days">Number of days to forecast (default: 30)</param>
    /// <param name="subscriptionId">Optional Azure subscription ID (defaults to configured subscription)</param>
    /// <returns>Cost forecast data</returns>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(CostForecast), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CostForecast>> GetCostForecast(
        [FromQuery] int days = 30,
        [FromQuery] string? subscriptionId = null)
    {
        try
        {
            _logger.LogInformation("Admin API: Getting cost forecast for {Days} days", days);

            if (days < 1 || days > 365)
            {
                return BadRequest("Days must be between 1 and 365");
            }

            var subId = subscriptionId ?? _configuration["Azure:SubscriptionId"];
            if (string.IsNullOrWhiteSpace(subId))
            {
                _logger.LogWarning("No Azure subscription ID configured");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cost forecasting service not available. Please configure Azure subscription.");
            }

            var forecast = await _costService.GetCostForecastAsync(subId, days);

            _logger.LogInformation("Admin API: Retrieved cost forecast for {Days} days", days);

            return Ok(forecast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cost forecast");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving cost forecast");
        }
    }

    /// <summary>
    /// Get optimization recommendations
    /// </summary>
    /// <param name="subscriptionId">Optional Azure subscription ID (defaults to configured subscription)</param>
    /// <returns>List of cost optimization recommendations</returns>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(List<CostOptimizationRecommendation>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CostOptimizationRecommendation>>> GetOptimizationRecommendations(
        [FromQuery] string? subscriptionId = null)
    {
        try
        {
            _logger.LogInformation("Admin API: Getting cost optimization recommendations");

            var subId = subscriptionId ?? _configuration["Azure:SubscriptionId"];
            if (string.IsNullOrWhiteSpace(subId))
            {
                _logger.LogWarning("No Azure subscription ID configured");
                return Ok(new List<CostOptimizationRecommendation>()); // Return empty list
            }

            var recommendations = await _costService.GetOptimizationRecommendationsAsync(subId);

            _logger.LogInformation("Admin API: Retrieved {Count} optimization recommendations", recommendations.Count);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving optimization recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving optimization recommendations");
        }
    }
}

// Response models for the API
public class EnvironmentCostData
{
    public string EnvironmentId { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "USD";
    public CostPeriod Period { get; set; } = new();
    public List<DailyCost> DailyCosts { get; set; } = new();
    public List<ServiceCost> ServiceCosts { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class CostSummary
{
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "USD";
    public CostPeriod Period { get; set; } = new();
    public decimal TrendPercentage { get; set; }
    public List<DailyCost> DailyCosts { get; set; } = new();
    public List<ServiceCost> TopServices { get; set; } = new();
    public decimal PotentialSavings { get; set; }
    public int RecommendationCount { get; set; }
    public int AnomalyCount { get; set; }
    public BudgetStatusSummary? BudgetStatus { get; set; }
}

public class CostPeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }
}

public class DailyCost
{
    public DateTime Date { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
}

public class ServiceCost
{
    public string ServiceName { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
    public int ResourceCount { get; set; }
}

public class BudgetStatusSummary
{
    public string BudgetName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal CurrentSpend { get; set; }
    public decimal PercentageUsed { get; set; }
    public string Status { get; set; } = string.Empty;
}
