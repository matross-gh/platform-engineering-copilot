using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Discovery;
using Platform.Engineering.Copilot.Core.Models.DiagramGeneration;
using Platform.Engineering.Copilot.Core.Interfaces.Services.DiagramGeneration;

namespace Platform.Engineering.Copilot.Core.Services.DiagramGeneration;

/// <summary>
/// Service for generating Mermaid diagrams from Azure resources and other sources
/// </summary>
public class MermaidDiagramService : IMermaidDiagramService
{
    private readonly ILogger<MermaidDiagramService> _logger;
    private readonly IAzureResourceDiscoveryService _discoveryService;

    public MermaidDiagramService(
        ILogger<MermaidDiagramService> logger,
        IAzureResourceDiscoveryService discoveryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
    }

    public async Task<string> GenerateC4ContainerDiagramAsync(
        string resourceGroup,
        string? subscriptionId = null,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating C4 container diagram for resource group {ResourceGroup}", resourceGroup);

        try
        {
            // Get actual resources from Azure
            var inventory = await _discoveryService.GetInventorySummaryAsync(
                $"Show me inventory for resource group {resourceGroup}",
                subscriptionId ?? string.Empty,
                cancellationToken);

            var diagramTitle = title ?? $"Architecture - {resourceGroup}";
            
            var sb = new StringBuilder();
            sb.AppendLine("```mermaid");
            sb.AppendLine("C4Container");
            sb.AppendLine($"    title {diagramTitle}");
            sb.AppendLine();
            sb.AppendLine("    Person(user, \"Platform Engineer\", \"System operator\")");
            sb.AppendLine();
            sb.AppendLine($"    System_Boundary({SanitizeId(resourceGroup)}, \"{resourceGroup}\") {{");
            
            // Group resources by type for better diagram organization
            if (inventory.ResourcesByType.Any())
            {
                foreach (var resourceType in inventory.ResourcesByType.Take(10)) // Limit to avoid diagram clutter
                {
                    var sanitizedType = SanitizeId(resourceType.Key);
                    var displayName = GetFriendlyResourceTypeName(resourceType.Key);
                    var shape = GetMermaidShapeForResourceType(resourceType.Key);
                    
                    sb.AppendLine($"        {shape}({sanitizedType}, \"{displayName}\", \"{resourceType.Key}\", \"{resourceType.Value} instance(s)\")");
                }
            }
            else
            {
                // Fallback to sample structure if no resources found
                sb.AppendLine("        Container(app, \"Application\", \"Container/VM\", \"Runs workload\")");
                sb.AppendLine("        ContainerDb(db, \"Database\", \"Azure SQL/Cosmos\", \"Stores data\")");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    System_Ext(azure, \"Azure Services\", \"Platform services\")");
            sb.AppendLine();
            
            // Add relationships
            sb.AppendLine("    Rel(user, " + (inventory.ResourcesByType.Any() ? SanitizeId(inventory.ResourcesByType.First().Key) : "app") + ", \"Manages\")");
            sb.AppendLine("```");

            _logger.LogInformation("Generated C4 container diagram with {ResourceTypeCount} resource types", inventory.ResourcesByType.Count);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query Azure resources, generating sample diagram");
            
            // Fallback to sample diagram on error
            var diagramTitle = title ?? $"Architecture - {resourceGroup}";
            var sb = new StringBuilder();
            sb.AppendLine("```mermaid");
            sb.AppendLine("C4Container");
            sb.AppendLine($"    title {diagramTitle}");
            sb.AppendLine();
            sb.AppendLine("    Person(user, \"Platform Engineer\", \"Navy developer\")");
            sb.AppendLine();
            sb.AppendLine($"    System_Boundary({SanitizeId(resourceGroup)}, \"{resourceGroup}\") {{");
            sb.AppendLine("        Container(app, \"Application\", \"Container/VM\", \"Runs workload\")");
            sb.AppendLine("        ContainerDb(db, \"Database\", \"Azure SQL/Cosmos\", \"Stores data\")");
            sb.AppendLine("        Container(cache, \"Cache\", \"Azure Redis\", \"Caching layer\")");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    System_Ext(azure, \"Azure Services\", \"Platform services\")");
            sb.AppendLine();
            sb.AppendLine("    Rel(user, app, \"Uses\")");
            sb.AppendLine("    Rel(app, db, \"Reads/Writes\")");
            sb.AppendLine("    Rel(app, cache, \"Caches\")");
            sb.AppendLine("    Rel(app, azure, \"Consumes\")");
            sb.AppendLine("```");

            return sb.ToString();
        }
    }

    public async Task<string> GenerateC4ContextDiagramAsync(
        string systemDescription,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating C4 context diagram");

        var diagramTitle = title ?? "System Context";
        
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("C4Context");
        sb.AppendLine($"    title {diagramTitle}");
        sb.AppendLine();
        sb.AppendLine("    Person(user, \"User\", \"System user\")");
        sb.AppendLine("    System(system, \"System\", \"Main system\")");
        sb.AppendLine("    System_Ext(external, \"External System\", \"External dependency\")");
        sb.AppendLine();
        sb.AppendLine("    Rel(user, system, \"Uses\")");
        sb.AppendLine("    Rel(system, external, \"Integrates with\")");
        sb.AppendLine("```");

        return await Task.FromResult(sb.ToString());
    }

    public async Task<string> GenerateSequenceDiagramAsync(
        string workflowDescription,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating sequence diagram");

        var diagramTitle = title ?? "Sequence Diagram";
        
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("sequenceDiagram");
        sb.AppendLine($"    title {diagramTitle}");
        sb.AppendLine();
        sb.AppendLine("    participant User");
        sb.AppendLine("    participant System");
        sb.AppendLine("    participant Database");
        sb.AppendLine();
        sb.AppendLine("    User->>System: Request");
        sb.AppendLine("    System->>Database: Query");
        sb.AppendLine("    Database-->>System: Data");
        sb.AppendLine("    System-->>User: Response");
        sb.AppendLine("```");

        return await Task.FromResult(sb.ToString());
    }

    public async Task<string> GenerateEntityRelationshipDiagramAsync(
        string schemaJson,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating entity-relationship diagram");

        var diagramTitle = title ?? "Entity-Relationship Diagram";
        
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("erDiagram");
        sb.AppendLine($"    title {diagramTitle}");
        sb.AppendLine();
        sb.AppendLine("    USER ||--o{ ORDER : places");
        sb.AppendLine("    ORDER ||--|{ LINE-ITEM : contains");
        sb.AppendLine("    PRODUCT ||--o{ LINE-ITEM : \"ordered in\"");
        sb.AppendLine();
        sb.AppendLine("    USER {");
        sb.AppendLine("        int id PK");
        sb.AppendLine("        string email");
        sb.AppendLine("        string name");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    ORDER {");
        sb.AppendLine("        int id PK");
        sb.AppendLine("        int user_id FK");
        sb.AppendLine("        datetime created_at");
        sb.AppendLine("    }");
        sb.AppendLine("```");

        return await Task.FromResult(sb.ToString());
    }

    public async Task<string> GenerateFlowchartAsync(
        string processDescription,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating flowchart");

        var diagramTitle = title ?? "Process Flow";
        
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("flowchart TD");
        sb.AppendLine($"    title[\"{diagramTitle}\"]");
        sb.AppendLine();
        sb.AppendLine("    Start([Start])");
        sb.AppendLine("    Process[Process Step]");
        sb.AppendLine("    Decision{Decision?}");
        sb.AppendLine("    ActionA[Action A]");
        sb.AppendLine("    ActionB[Action B]");
        sb.AppendLine("    End([End])");
        sb.AppendLine();
        sb.AppendLine("    Start --> Process");
        sb.AppendLine("    Process --> Decision");
        sb.AppendLine("    Decision -->|Yes| ActionA");
        sb.AppendLine("    Decision -->|No| ActionB");
        sb.AppendLine("    ActionA --> End");
        sb.AppendLine("    ActionB --> End");
        sb.AppendLine("```");

        return await Task.FromResult(sb.ToString());
    }

    public async Task<DiagramResult> GenerateDiagramAsync(
        DiagramRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string mermaidCode = request.DiagramType switch
            {
                DiagramType.C4Container => await GenerateC4ContainerDiagramAsync(
                    request.ResourceGroup ?? "default",
                    request.SubscriptionId,
                    request.Title,
                    cancellationToken),
                
                DiagramType.C4Context => await GenerateC4ContextDiagramAsync(
                    request.InputData,
                    request.Title,
                    cancellationToken),
                
                DiagramType.Sequence => await GenerateSequenceDiagramAsync(
                    request.InputData,
                    request.Title,
                    cancellationToken),
                
                DiagramType.EntityRelationship => await GenerateEntityRelationshipDiagramAsync(
                    request.InputData,
                    request.Title,
                    cancellationToken),
                
                DiagramType.Flowchart => await GenerateFlowchartAsync(
                    request.InputData,
                    request.Title,
                    cancellationToken),
                
                _ => throw new NotSupportedException($"Diagram type {request.DiagramType} not yet implemented")
            };

            return new DiagramResult
            {
                MermaidCode = mermaidCode,
                Success = true,
                Title = request.Title
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diagram of type {DiagramType}", request.DiagramType);
            
            return new DiagramResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Convert Azure resource type to friendly display name
    /// </summary>
    private static string GetFriendlyResourceTypeName(string resourceType)
    {
        var parts = resourceType.Split('/');
        if (parts.Length >= 2)
        {
            // Return just the resource type without provider (e.g., "storageAccounts" from "Microsoft.Storage/storageAccounts")
            return parts[^1];
        }
        return resourceType;
    }

    /// <summary>
    /// Map Azure resource type to appropriate Mermaid C4 shape
    /// </summary>
    private static string GetMermaidShapeForResourceType(string resourceType)
    {
        var lowerType = resourceType.ToLowerInvariant();
        
        // Database resources
        if (lowerType.Contains("sql") || lowerType.Contains("cosmos") || lowerType.Contains("postgresql") || 
            lowerType.Contains("mysql") || lowerType.Contains("mariadb") || lowerType.Contains("redis"))
        {
            return "ContainerDb";
        }
        
        // Queue/messaging resources
        if (lowerType.Contains("queue") || lowerType.Contains("servicebus") || lowerType.Contains("eventhub"))
        {
            return "ContainerQueue";
        }
        
        // Default to Container for all other resources
        return "Container";
    }

    private static string SanitizeId(string input)
    {
        return input.Replace("-", "_").Replace(" ", "_").ToLowerInvariant();
    }
}
