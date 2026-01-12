namespace Platform.Engineering.Copilot.Mcp.Prompts;

/// <summary>
/// MCP Prompts for cost management domain operations.
/// These prompts guide AI assistants in using the platform's cost management capabilities.
/// </summary>
public static class CostManagementPrompts
{
    public static readonly McpPrompt AnalyzeCosts = new()
    {
        Name = "analyze_costs",
        Description = "Analyze Azure costs with breakdown by resource, service, or tag",
        Arguments = new[]
        {
            new PromptArgument { Name = "subscription_id", Description = "Azure subscription ID", Required = true },
            new PromptArgument { Name = "time_range", Description = "Time range: 7d, 30d, 90d, or custom dates", Required = false },
            new PromptArgument { Name = "group_by", Description = "Group by: resource, service, location, or tag", Required = false }
        }
    };

    public static readonly McpPrompt GetOptimizations = new()
    {
        Name = "get_cost_optimizations",
        Description = "Get actionable cost optimization recommendations",
        Arguments = new[]
        {
            new PromptArgument { Name = "subscription_id", Description = "Azure subscription ID", Required = true },
            new PromptArgument { Name = "minimum_savings", Description = "Minimum monthly savings threshold ($)", Required = false }
        }
    };

    public static readonly McpPrompt ForecastCosts = new()
    {
        Name = "forecast_costs",
        Description = "Forecast future Azure costs based on historical trends",
        Arguments = new[]
        {
            new PromptArgument { Name = "subscription_id", Description = "Azure subscription ID", Required = true },
            new PromptArgument { Name = "forecast_months", Description = "Number of months to forecast (default: 3)", Required = false }
        }
    };

    public static readonly McpPrompt ManageBudgets = new()
    {
        Name = "manage_budgets",
        Description = "Create, update, or monitor Azure budgets",
        Arguments = new[]
        {
            new PromptArgument { Name = "operation", Description = "Operation: create, update, delete, or list", Required = true },
            new PromptArgument { Name = "subscription_id", Description = "Azure subscription ID", Required = true },
            new PromptArgument { Name = "budget_name", Description = "Budget name", Required = false },
            new PromptArgument { Name = "amount", Description = "Budget amount in USD", Required = false }
        }
    };

    public static readonly McpPrompt DetectAnomalies = new()
    {
        Name = "detect_cost_anomalies",
        Description = "Detect unusual spending patterns and cost anomalies",
        Arguments = new[]
        {
            new PromptArgument { Name = "subscription_id", Description = "Azure subscription ID", Required = true },
            new PromptArgument { Name = "lookback_days", Description = "Days to analyze (default: 30)", Required = false }
        }
    };

    public static readonly McpPrompt ModelScenarios = new()
    {
        Name = "model_cost_scenario",
        Description = "Model and compare different cost scenarios for capacity planning",
        Arguments = new[]
        {
            new PromptArgument { Name = "scenario_type", Description = "Scenario: reserved-instance, spot-vm, or scale-out", Required = true },
            new PromptArgument { Name = "subscription_id", Description = "Azure subscription ID", Required = true }
        }
    };

    /// <summary>
    /// Get all cost management prompts
    /// </summary>
    public static IEnumerable<McpPrompt> GetAll() => new[]
    {
        AnalyzeCosts,
        GetOptimizations,
        ForecastCosts,
        ManageBudgets,
        DetectAnomalies,
        ModelScenarios
    };
}
