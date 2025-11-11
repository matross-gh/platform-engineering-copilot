using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Services.Generators.KeyVault;

namespace Platform.Engineering.Copilot.Core.Services.Generators.Adapters;

/// <summary>
/// Adapter for Terraform Key Vault module generator
/// </summary>
public class TerraformKeyVaultModuleAdapter : IModuleGenerator
{
    private readonly TerraformKeyVaultModuleGenerator _generator;

    public TerraformKeyVaultModuleAdapter()
    {
        _generator = new TerraformKeyVaultModuleGenerator();
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
