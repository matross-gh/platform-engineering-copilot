using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Mcp.Tools;
using System.Text.Json;

namespace Platform.Engineering.Copilot.Mcp.Server;

/// <summary>
/// HTTP bridge for MCP server - exposes MCP tools via REST endpoints
/// Provides a generic HTTP interface that any web application can use
/// 
/// Dual-mode operation:
/// 1. Stdio mode: GitHub Copilot/Claude spawn this as subprocess, use stdin/stdout
/// 2. HTTP mode: Any web app calls HTTP endpoints
/// </summary>
public class McpHttpBridge
{
    private readonly ILogger<McpHttpBridge> _logger;

    public McpHttpBridge(ILogger<McpHttpBridge> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Configure HTTP endpoints for MCP tools using WebApplication minimal APIs
    /// </summary>
    public void MapHttpEndpoints(WebApplication app)
    {
        // Process chat request through multi-agent orchestrator
        app.MapPost("/mcp/chat", async (HttpContext context, PlatformEngineeringCopilotTools chatTool) =>
        {
            try
            {
                var requestBody = await JsonSerializer.DeserializeAsync<ChatRequest>(
                    context.Request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (requestBody == null || string.IsNullOrEmpty(requestBody.Message))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Message is required" });
                    return;
                }

                var logger = context.RequestServices.GetRequiredService<ILogger<McpHttpBridge>>();
                logger.LogInformation("HTTP: Processing chat request for conversation: {ConversationId}", 
                    requestBody.ConversationId ?? "new");

                // Process file attachments if present
                List<string>? uploadedFilePaths = null;
                if (requestBody.Attachments != null && requestBody.Attachments.Any())
                {
                    logger.LogInformation("Processing {Count} file attachments", requestBody.Attachments.Count);
                    uploadedFilePaths = await ProcessFileAttachmentsAsync(requestBody.Attachments, logger);
                    
                    // Add file paths to context
                    requestBody.Context ??= new Dictionary<string, object>();
                    requestBody.Context["uploaded_files"] = uploadedFilePaths;
                    
                    // Enhance message with file information
                    var fileInfo = string.Join(", ", uploadedFilePaths.Select(Path.GetFileName));
                    requestBody.Message = $"[Uploaded files: {fileInfo}]\n\n{requestBody.Message}";
                }

                try
                {
                    var result = await chatTool.ProcessRequestAsync(
                        requestBody.Message,
                        requestBody.ConversationId,
                        requestBody.Context,
                        context.RequestAborted);

                    await context.Response.WriteAsJsonAsync(result);
                }
                finally
                {
                    // Clean up uploaded files
                    if (uploadedFilePaths != null)
                    {
                        foreach (var filePath in uploadedFilePaths)
                        {
                            try
                            {
                                if (File.Exists(filePath))
                                {
                                    File.Delete(filePath);
                                    logger.LogDebug("Cleaned up temporary file: {FilePath}", filePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Failed to delete temporary file: {FilePath}", filePath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.RequestServices.GetRequiredService<ILogger<McpHttpBridge>>()
                    .LogError(ex, "Error processing chat request");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        });

        // Health check
        app.MapGet("/health", () => Results.Ok(new 
        { 
            status = "healthy", 
            mode = "dual (http+stdio)",
            server = "Platform Engineering Copilot MCP",
            version = "1.0.0"
        }));

        _logger.LogInformation("✅ MCP HTTP Bridge endpoints configured");
        _logger.LogInformation("   POST   /mcp/chat - Process chat request");
        _logger.LogInformation("   GET    /health - Health check");
    }

    /// <summary>
    /// Process file attachments by saving base64-encoded content to temp directory
    /// </summary>
    private static async Task<List<string>> ProcessFileAttachmentsAsync(
        List<FileAttachment> attachments,
        ILogger logger)
    {
        var uploadedPaths = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-attachments", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        foreach (var attachment in attachments)
        {
            try
            {
                // Validate file size (max 50MB)
                if (attachment.Size > 50 * 1024 * 1024)
                {
                    logger.LogWarning("Skipping attachment {FileName} - size {Size} bytes exceeds 50MB limit",
                        attachment.FileName, attachment.Size);
                    continue;
                }

                // Decode base64 content
                byte[] fileBytes;
                try
                {
                    fileBytes = Convert.FromBase64String(attachment.Base64Content);
                }
                catch (FormatException ex)
                {
                    logger.LogError(ex, "Failed to decode base64 content for {FileName}", attachment.FileName);
                    continue;
                }

                // Sanitize filename
                var sanitizedFileName = Path.GetFileName(attachment.FileName);
                var filePath = Path.Combine(tempDir, sanitizedFileName);

                // Write file to disk
                await File.WriteAllBytesAsync(filePath, fileBytes);

                uploadedPaths.Add(filePath);
                logger.LogInformation("Saved attachment: {FileName} ({Size} bytes) to {Path}",
                    sanitizedFileName, fileBytes.Length, filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing attachment: {FileName}", attachment.FileName);
            }
        }

        return uploadedPaths;
    }
}

// Generic request model
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public Dictionary<string, object>? Context { get; set; }
    public List<FileAttachment>? Attachments { get; set; }
}

// File attachment model (base64-encoded)
public class FileAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string Base64Content { get; set; } = string.Empty;
    public long Size { get; set; }
}
