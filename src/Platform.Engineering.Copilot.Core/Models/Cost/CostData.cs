namespace Platform.Engineering.Copilot.Core.Models.Cost;

/// <summary>
/// Cost data for Azure resources
/// </summary>
public class CostData
{
    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost breakdown by service
    /// </summary>
    public Dictionary<string, decimal> ServiceCosts { get; set; } = new();

    /// <summary>
    /// Cost breakdown by resource group
    /// </summary>
    public Dictionary<string, decimal> ResourceGroupCosts { get; set; } = new();
}
