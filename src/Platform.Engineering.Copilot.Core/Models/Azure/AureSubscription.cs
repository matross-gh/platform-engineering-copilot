namespace Platform.Engineering.Copilot.Core.Models.Azure;

/// <summary>
/// Azure subscription information
/// </summary>
public class AzureSubscription
    {
        public string SubscriptionId { get; set; } = "";
        public string SubscriptionName { get; set; } = "";
        public string State { get; set; } = "";
        public string TenantId { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }