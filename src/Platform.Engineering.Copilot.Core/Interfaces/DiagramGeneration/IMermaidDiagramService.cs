using Platform.Engineering.Copilot.Core.Models.DiagramGeneration;

namespace Platform.Engineering.Copilot.Core.Interfaces.Services.DiagramGeneration;

/// <summary>
/// Service for generating Mermaid diagrams from various data sources
/// </summary>
public interface IMermaidDiagramService
{
    /// <summary>
    /// Generate C4 container diagram from Azure resource inventory
    /// </summary>
    /// <param name="resourceGroup">Azure resource group name</param>
    /// <param name="subscriptionId">Azure subscription ID</param>
    /// <param name="title">Diagram title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mermaid diagram code</returns>
    Task<string> GenerateC4ContainerDiagramAsync(
        string resourceGroup,
        string? subscriptionId = null,
        string? title = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate C4 context diagram from system description
    /// </summary>
    /// <param name="systemDescription">Description of the system and its context</param>
    /// <param name="title">Diagram title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mermaid diagram code</returns>
    Task<string> GenerateC4ContextDiagramAsync(
        string systemDescription,
        string? title = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate sequence diagram from workflow or API specification
    /// </summary>
    /// <param name="workflowDescription">Workflow steps or API interaction description</param>
    /// <param name="title">Diagram title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mermaid diagram code</returns>
    Task<string> GenerateSequenceDiagramAsync(
        string workflowDescription,
        string? title = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate entity-relationship diagram from database schema
    /// </summary>
    /// <param name="schemaJson">Database schema in JSON format</param>
    /// <param name="title">Diagram title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mermaid diagram code</returns>
    Task<string> GenerateEntityRelationshipDiagramAsync(
        string schemaJson,
        string? title = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate flowchart from process description
    /// </summary>
    /// <param name="processDescription">Process steps and decision points</param>
    /// <param name="title">Diagram title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mermaid diagram code</returns>
    Task<string> GenerateFlowchartAsync(
        string processDescription,
        string? title = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate diagram from generic request
    /// </summary>
    /// <param name="request">Diagram request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diagram result</returns>
    Task<DiagramResult> GenerateDiagramAsync(
        DiagramRequest request,
        CancellationToken cancellationToken = default);
}
