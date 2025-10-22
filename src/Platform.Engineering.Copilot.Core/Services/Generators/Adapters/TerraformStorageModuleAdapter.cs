using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Services.Generators.Storage;

namespace Platform.Engineering.Copilot.Core.Services.Generators.Adapters;

/// <summary>
/// Adapter for Terraform Storage module generator
/// </summary>
public class TerraformStorageModuleAdapter : IModuleGenerator
{
    private readonly TerraformStorageModuleGenerator _generator;

    public TerraformStorageModuleAdapter()
    {
        _generator = new TerraformStorageModuleGenerator();
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
