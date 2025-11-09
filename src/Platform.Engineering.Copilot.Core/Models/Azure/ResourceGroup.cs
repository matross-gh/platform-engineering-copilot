namespace Platform.Engineering.Copilot.Core.Models.Azure;

/// <summary>
/// Resource group information
/// </summary>
public class ResourceGroup
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public Dictionary<string, string>? Tags { get; set; }
    public string ProvisioningState { get; set; } = string.Empty;
    public int ResourceCount { get; set; }
}