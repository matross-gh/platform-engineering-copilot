namespace Platform.Engineering.Copilot.Core.Configuration;

/// <summary>
/// Configuration for GitHub integration (PR reviews, webhooks)
/// </summary>
public class GitHubConfiguration
{
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool EnablePrReviews { get; set; } = true;
    public bool AutoApproveOnSuccess { get; set; } = false; // Phase 1: Always false
    public int MaxFileSizeKb { get; set; } = 1024; // 1MB max
}
