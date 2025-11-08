using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Services.Generators.Base;
using Platform.Engineering.Copilot.Core.Services.Generators.Infrastructure;

namespace Platform.Engineering.Copilot.Core.Services.Generators.Adapters;

/// <summary>
/// Adapter for Bicep Network Module Generator
/// Handles pure network infrastructure (VNet, Subnets, NSG, DDoS, Peering) without compute
/// </summary>
public class BicepNetworkModuleAdapter : ModuleGeneratorBase
{
    private readonly BicepNetworkModuleGenerator _generator = new();

    public override InfrastructureFormat Format => InfrastructureFormat.Bicep;
    public override ComputePlatform Platform => ComputePlatform.Networking;  // Changed from Network to Networking
    public override CloudProvider Provider => CloudProvider.Azure;

    public override Dictionary<string, string> GenerateModule(TemplateGenerationRequest request)
    {
        return _generator.GenerateModule(request);
    }
}
