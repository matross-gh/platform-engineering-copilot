using System.ComponentModel.DataAnnotations;

namespace Platform.Engineering.Copilot.Agents.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the Infrastructure Agent.
/// </summary>
public class InfrastructureAgentOptions
{
    public const string SectionName = "AgentConfiguration:InfrastructureAgent";

    /// <summary>
    /// Whether this agent is enabled. When false, the agent will not be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Temperature for AI responses (0.0 - 2.0).
    /// Lower = more focused and deterministic, Higher = more creative.
    /// Default: 0.4 (balanced for infrastructure code generation).
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.4;

    /// <summary>
    /// Maximum tokens for chat completion requests.
    /// Default: 8000 (sufficient for complex infrastructure templates).
    /// </summary>
    [Range(1, 128000)]
    public int MaxTokens { get; set; } = 8000;

    /// <summary>
    /// Default Azure region for resource provisioning.
    /// Used when user doesn't specify a region.
    /// </summary>
    public string DefaultRegion { get; set; } = "eastus";

    /// <summary>
    /// Default Azure subscription ID to use when not specified.
    /// </summary>
    public string? DefaultSubscriptionId { get; set; }

    /// <summary>
    /// Enable compliance-aware template enhancement.
    /// When true, automatically enhances templates with compliance controls.
    /// </summary>
    public bool EnableComplianceEnhancement { get; set; } = true;

    /// <summary>
    /// Default compliance framework to apply.
    /// Options: "FedRAMPHigh", "DoD IL5", "NIST80053", "SOC2", "GDPR".
    /// </summary>
    public string DefaultComplianceFramework { get; set; } = "FedRAMPHigh";

    /// <summary>
    /// Enable predictive scaling analysis and recommendations.
    /// When true, provides AI-powered scaling suggestions.
    /// </summary>
    public bool EnablePredictiveScaling { get; set; } = true;

    /// <summary>
    /// Enable network topology design capabilities.
    /// When true, provides subnet calculation and network design.
    /// </summary>
    public bool EnableNetworkDesign { get; set; } = true;

    /// <summary>
    /// Enable Azure Arc onboarding capabilities.
    /// </summary>
    public bool EnableAzureArc { get; set; } = true;

    /// <summary>
    /// Template generation settings.
    /// </summary>
    public TemplateGenerationOptions TemplateGeneration { get; set; } = new();

    /// <summary>
    /// Provisioning settings.
    /// </summary>
    public ProvisioningOptions Provisioning { get; set; } = new();

    /// <summary>
    /// Scaling settings.
    /// </summary>
    public ScalingOptions Scaling { get; set; } = new();
}

/// <summary>
/// Template generation settings.
/// </summary>
public class TemplateGenerationOptions
{
    /// <summary>
    /// Default template format: "bicep" or "terraform".
    /// </summary>
    public string DefaultFormat { get; set; } = "bicep";

    /// <summary>
    /// Cache duration for generated templates in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Include best practices from Azure MCP in generated templates.
    /// </summary>
    public bool IncludeBestPractices { get; set; } = true;

    /// <summary>
    /// Include documentation comments in generated templates.
    /// </summary>
    public bool IncludeDocumentation { get; set; } = true;
}

/// <summary>
/// Provisioning settings.
/// </summary>
public class ProvisioningOptions
{
    /// <summary>
    /// Require explicit confirmation before provisioning resources.
    /// </summary>
    public bool RequireConfirmation { get; set; } = true;

    /// <summary>
    /// Enable dry-run mode by default (show what would be created without creating).
    /// </summary>
    public bool DryRunByDefault { get; set; } = false;

    /// <summary>
    /// Default resource tags to apply to all provisioned resources.
    /// </summary>
    public Dictionary<string, string> DefaultTags { get; set; } = new()
    {
        ["ManagedBy"] = "PlatformEngineeringCopilot",
        ["CreatedBy"] = "InfrastructureAgent"
    };
}

/// <summary>
/// Scaling settings.
/// </summary>
public class ScalingOptions
{
    /// <summary>
    /// Default prediction horizon in hours.
    /// </summary>
    [Range(1, 720)]
    public int DefaultPredictionHours { get; set; } = 24;

    /// <summary>
    /// Minimum confidence threshold for scaling recommendations (0.0-1.0).
    /// </summary>
    [Range(0.0, 1.0)]
    public double MinConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// Enable automatic scaling recommendations.
    /// </summary>
    public bool EnableAutoRecommendations { get; set; } = true;
}
