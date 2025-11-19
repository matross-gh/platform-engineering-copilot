namespace Platform.Engineering.Copilot.Core.Models.DiagramGeneration;

/// <summary>
/// Supported diagram types for Mermaid rendering
/// </summary>
public enum DiagramType
{
    /// <summary>
    /// C4 Context diagram - System context and external actors
    /// </summary>
    C4Context,
    
    /// <summary>
    /// C4 Container diagram - High-level technology choices and responsibilities
    /// </summary>
    C4Container,
    
    /// <summary>
    /// C4 Component diagram - Components within a container
    /// </summary>
    C4Component,
    
    /// <summary>
    /// Flowchart - Process flows and decision trees
    /// </summary>
    Flowchart,
    
    /// <summary>
    /// Sequence diagram - Interaction between systems/components over time
    /// </summary>
    Sequence,
    
    /// <summary>
    /// Entity-Relationship diagram - Database schema and relationships
    /// </summary>
    EntityRelationship,
    
    /// <summary>
    /// Gantt chart - Project timeline and milestones
    /// </summary>
    Gantt,
    
    /// <summary>
    /// State diagram - State machine transitions
    /// </summary>
    State,
    
    /// <summary>
    /// Class diagram - Object-oriented design
    /// </summary>
    Class
}

/// <summary>
/// Supported image formats for diagram rendering
/// </summary>
public enum ImageFormat
{
    /// <summary>
    /// Portable Network Graphics - Raster image format
    /// </summary>
    Png,
    
    /// <summary>
    /// Scalable Vector Graphics - Vector image format
    /// </summary>
    Svg
}

/// <summary>
/// Request for diagram generation
/// </summary>
public class DiagramRequest
{
    /// <summary>
    /// Type of diagram to generate
    /// </summary>
    public DiagramType DiagramType { get; set; }
    
    /// <summary>
    /// Diagram title
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Input data for diagram generation (JSON, resource inventory, workflow description)
    /// </summary>
    public string InputData { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to render to image (PNG) or return Mermaid markdown
    /// Phase 1: Optional rendering for presentations/documents
    /// </summary>
    public bool RenderImage { get; set; } = false;
    
    /// <summary>
    /// Image format if RenderImage is true (default: PNG)
    /// </summary>
    public ImageFormat ImageFormat { get; set; } = ImageFormat.Png;
    
    /// <summary>
    /// Optional: Resource group name for Azure resource diagrams
    /// </summary>
    public string? ResourceGroup { get; set; }
    
    /// <summary>
    /// Optional: Subscription ID for Azure resource diagrams
    /// </summary>
    public string? SubscriptionId { get; set; }
}

/// <summary>
/// Result of diagram generation
/// </summary>
public class DiagramResult
{
    /// <summary>
    /// Mermaid markdown syntax
    /// </summary>
    public string MermaidCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Rendered image bytes (if RenderImage was true)
    /// </summary>
    public byte[]? ImageData { get; set; }
    
    /// <summary>
    /// Image format (PNG, SVG)
    /// </summary>
    public string? ImageFormat { get; set; }
    
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if unsuccessful
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Diagram title
    /// </summary>
    public string Title { get; set; } = string.Empty;
}
