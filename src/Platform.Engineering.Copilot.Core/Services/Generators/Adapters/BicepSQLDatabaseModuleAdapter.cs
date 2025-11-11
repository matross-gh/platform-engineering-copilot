using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Services.Generators.Database;

namespace Platform.Engineering.Copilot.Core.Services.Generators.Adapters;

/// <summary>
/// Adapter for Bicep SQL Database module generator
/// </summary>
public class BicepSQLDatabaseModuleAdapter : IModuleGenerator
{
    private readonly BicepSQLDatabaseModuleGenerator _generator;

    public BicepSQLDatabaseModuleAdapter()
    {
        _generator = new BicepSQLDatabaseModuleGenerator();
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
