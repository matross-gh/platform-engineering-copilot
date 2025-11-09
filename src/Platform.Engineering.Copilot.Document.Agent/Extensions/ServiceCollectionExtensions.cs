using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Document.Agent.Plugins;
using Platform.Engineering.Copilot.Document.Agent.Services.Analyzers;
using Platform.Engineering.Copilot.Services.DocumentProcessing;

namespace Platform.Engineering.Copilot.Document.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentAgent(this IServiceCollection services)
    {
        // Register Architecture Diagram Analyzer
        services.AddScoped<IArchitectureDiagramAnalyzer, ArchitectureDiagramAnalyzer>();
        
        // Register Document Processing Service
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
        
        // Register Document Plugin
        services.AddScoped<DocumentPlugin>();
        
        // TODO: Complete other analyzers
        // services.AddScoped<NavyFlankspeedAnalyzer>();
        
        return services;
    }
}
