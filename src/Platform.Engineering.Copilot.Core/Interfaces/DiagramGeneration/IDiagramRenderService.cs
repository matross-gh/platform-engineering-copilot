namespace Platform.Engineering.Copilot.Core.Interfaces.Services.DiagramGeneration;

/// <summary>
/// Service for rendering Mermaid diagrams to image formats (PNG, SVG)
/// Uses PuppeteerSharp for offline rendering with headless Chromium
/// IL5/IL6 compliant: Local execution only, no external API calls
/// </summary>
public interface IDiagramRenderService
{
    /// <summary>
    /// Render Mermaid diagram markdown to PNG image
    /// </summary>
    /// <param name="mermaidCode">Mermaid diagram code (with or without code fences)</param>
    /// <param name="width">Image width in pixels (default: 1920)</param>
    /// <param name="height">Image height in pixels (default: 1080)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PNG image as byte array</returns>
    Task<byte[]> RenderToPngAsync(
        string mermaidCode,
        int width = 1920,
        int height = 1080,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Render Mermaid diagram markdown to SVG image
    /// </summary>
    /// <param name="mermaidCode">Mermaid diagram code (with or without code fences)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SVG markup as string</returns>
    Task<string> RenderToSvgAsync(
        string mermaidCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if Chromium browser is downloaded and ready
    /// </summary>
    Task<bool> IsBrowserReadyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Download Chromium browser if not already present
    /// Required for first-time setup
    /// </summary>
    Task EnsureBrowserInstalledAsync(CancellationToken cancellationToken = default);
}
