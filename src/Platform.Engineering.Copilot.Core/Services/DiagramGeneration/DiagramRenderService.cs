using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Services.DiagramGeneration;
using PuppeteerSharp;

namespace Platform.Engineering.Copilot.Core.Services.DiagramGeneration;

/// <summary>
/// Service for rendering Mermaid diagrams to images using PuppeteerSharp
/// IL5/IL6 compliant: Uses local headless Chromium, no external API calls
/// </summary>
public class DiagramRenderService : IDiagramRenderService
{
    private readonly ILogger<DiagramRenderService> _logger;
    private static readonly SemaphoreSlim _browserLock = new(1, 1);
    private static IBrowser? _browser;

    // Mermaid CDN version (can be updated as needed)
    private const string MermaidVersion = "10.6.1";
    private const string MermaidCdn = $"https://cdn.jsdelivr.net/npm/mermaid@{MermaidVersion}/dist/mermaid.min.js";

    public DiagramRenderService(ILogger<DiagramRenderService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]> RenderToPngAsync(
        string mermaidCode,
        int width = 1920,
        int height = 1080,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rendering Mermaid diagram to PNG ({Width}x{Height})", width, height);

        try
        {
            var browser = await GetBrowserAsync(cancellationToken);
            var page = await browser.NewPageAsync();

            try
            {
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = width,
                    Height = height
                });

                var cleanMermaidCode = ExtractMermaidCode(mermaidCode);
                var html = GenerateHtmlWithMermaid(cleanMermaidCode);

                await page.SetContentAsync(html);

                // Wait for Mermaid to render
                await page.WaitForSelectorAsync("#mermaid-diagram svg", new WaitForSelectorOptions
                {
                    Timeout = 10000
                });

                // Get the SVG element for screenshot
                var svgElement = await page.QuerySelectorAsync("#mermaid-diagram svg");
                if (svgElement == null)
                {
                    throw new InvalidOperationException("Failed to render Mermaid diagram - SVG element not found");
                }

                // Take screenshot of the SVG element
                var screenshot = await svgElement.ScreenshotDataAsync(new ElementScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    OmitBackground = false
                });

                _logger.LogInformation("Successfully rendered Mermaid diagram to PNG");
                return screenshot;
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering Mermaid diagram to PNG");
            throw;
        }
    }

    public async Task<string> RenderToSvgAsync(
        string mermaidCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rendering Mermaid diagram to SVG");

        try
        {
            var browser = await GetBrowserAsync(cancellationToken);
            var page = await browser.NewPageAsync();

            try
            {
                var cleanMermaidCode = ExtractMermaidCode(mermaidCode);
                var html = GenerateHtmlWithMermaid(cleanMermaidCode);

                await page.SetContentAsync(html);

                // Wait for Mermaid to render
                await page.WaitForSelectorAsync("#mermaid-diagram svg", new WaitForSelectorOptions
                {
                    Timeout = 10000
                });

                // Extract SVG content
                var svgContent = await page.EvaluateExpressionAsync<string>(
                    "document.querySelector('#mermaid-diagram svg').outerHTML"
                );

                _logger.LogInformation("Successfully rendered Mermaid diagram to SVG");
                return svgContent;
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering Mermaid diagram to SVG");
            throw;
        }
    }

    public async Task<bool> IsBrowserReadyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var browserFetcher = new BrowserFetcher();
            var installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();
            return installedBrowser != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking browser installation status");
            return false;
        }
    }

    public async Task EnsureBrowserInstalledAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking Chromium browser installation");

        try
        {
            var browserFetcher = new BrowserFetcher();
            var installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();

            if (installedBrowser == null)
            {
                _logger.LogInformation("Chromium browser not found, downloading...");
                await browserFetcher.DownloadAsync();
                _logger.LogInformation("Chromium browser downloaded successfully");
            }
            else
            {
                _logger.LogInformation("Chromium browser already installed at {Path}", installedBrowser.GetExecutablePath());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring Chromium browser installation");
            throw;
        }
    }

    /// <summary>
    /// Get or create browser instance (singleton pattern for performance)
    /// </summary>
    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        await _browserLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser == null || !_browser.IsConnected)
            {
                _logger.LogInformation("Launching headless Chromium browser");

                await EnsureBrowserInstalledAsync(cancellationToken);

                var options = new LaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu"
                    }
                };

                _browser = await Puppeteer.LaunchAsync(options);
                _logger.LogInformation("Chromium browser launched successfully");
            }

            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// Extract clean Mermaid code from markdown code fence if present
    /// </summary>
    private static string ExtractMermaidCode(string input)
    {
        // Remove code fence if present (```mermaid ... ```)
        var match = Regex.Match(input, @"```(?:mermaid)?\s*\n?(.*?)\n?```", RegexOptions.Singleline);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Return as-is if no code fence found
        return input.Trim();
    }

    /// <summary>
    /// Generate HTML page with Mermaid diagram
    /// </summary>
    private static string GenerateHtmlWithMermaid(string mermaidCode)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <script src=""{MermaidCdn}""></script>
    <style>
        body {{
            margin: 0;
            padding: 20px;
            background: white;
            font-family: Arial, sans-serif;
        }}
        #mermaid-diagram {{
            display: inline-block;
        }}
    </style>
</head>
<body>
    <div id=""mermaid-diagram"">
        <pre class=""mermaid"">
{mermaidCode}
        </pre>
    </div>
    <script>
        mermaid.initialize({{ 
            startOnLoad: true,
            theme: 'default',
            securityLevel: 'loose'
        }});
    </script>
</body>
</html>";
    }
}
