using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Services.Generators.KeyVault;

namespace Platform.Engineering.Copilot.Core.Services.Generators.Adapters;

/// <summary>
/// Adapter for Bicep Key Vault module generator
/// </summary>
public class BicepKeyVaultModuleAdapter : IModuleGenerator
{
    private readonly BicepKeyVaultModuleGenerator _generator;

    public BicepKeyVaultModuleAdapter()
    {
        _generator = new BicepKeyVaultModuleGenerator();
    }

    public InfrastructureFormat Format => _generator.Format;
    public ComputePlatform Platform => _generator.Platform;
    public CloudProvider Provider => _generator.Provider;

    public Dictionary<string, string> GenerateModule(TemplateGenerationRequest request)
    {
        return _generator.GenerateModule(request);
    }

    public bool CanGenerate(TemplateGenerationRequest request)
    {
        return _generator.CanHandle(request);
    }
}
