using System.ComponentModel.DataAnnotations;

namespace Platform.Engineering.Copilot.Infrastructure.Agent.Configuration;

/// <summary>
/// Configuration options for the Infrastructure Agent
/// </summary>
public class InfrastructureAgentOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "InfrastructureAgent";

    /// <summary>
    /// Temperature for AI responses (0.0 - 2.0)
    /// Lower = more focused and deterministic, Higher = more creative
    /// Default: 0.4 (balanced for infrastructure code generation)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.4;

    /// <summary>
    /// Maximum tokens for chat completion requests
    /// Default: 8000 (sufficient for complex infrastructure templates)
    /// </summary>
    [Range(1, 128000)]
    public int MaxTokens { get; set; } = 8000;

    /// <summary>
    /// Default Azure region for resource provisioning
    /// Used when user doesn't specify a region
    /// Default: "eastus"
    /// </summary>
    public string DefaultRegion { get; set; } = "eastus";

    /// <summary>
    /// Enable compliance-aware template enhancement
    /// When true, automatically enhances templates with compliance controls
    /// Default: true
    /// </summary>
    public bool EnableComplianceEnhancement { get; set; } = true;

    /// <summary>
    /// Default compliance framework to apply
    /// Options: "FedRAMPHigh", "DoD IL5", "NIST80053", "SOC2", "GDPR"
    /// Default: "FedRAMPHigh"
    /// </summary>
    public string DefaultComplianceFramework { get; set; } = "FedRAMPHigh";

    /// <summary>
    /// Enable predictive scaling analysis and recommendations
    /// When true, provides AI-powered scaling suggestions
    /// Default: true
    /// </summary>
    public bool EnablePredictiveScaling { get; set; } = true;

    /// <summary>
    /// Enable network topology design capabilities
    /// When true, provides subnet calculation and network design
    /// Default: true
    /// </summary>
    public bool EnableNetworkDesign { get; set; } = true;
}
