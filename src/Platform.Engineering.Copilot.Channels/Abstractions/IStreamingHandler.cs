namespace Platform.Engineering.Copilot.Channels.Abstractions;

/// <summary>
/// Handles streaming responses from agents.
/// </summary>
public interface IStreamingHandler
{
    /// <summary>
    /// Begin a streaming response.
    /// </summary>
    Task<IStreamContext> BeginStreamAsync(string conversationId, string? agentType = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for streaming response.
/// </summary>
public interface IStreamContext : IAsyncDisposable
{
    /// <summary>
    /// Stream ID.
    /// </summary>
    string StreamId { get; }

    /// <summary>
    /// Conversation ID.
    /// </summary>
    string ConversationId { get; }

    /// <summary>
    /// Write a chunk to the stream.
    /// </summary>
    Task WriteAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write a structured chunk to the stream.
    /// </summary>
    Task WriteAsync(StreamChunk chunk, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete the stream.
    /// </summary>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Abort the stream with an error.
    /// </summary>
    Task AbortAsync(string? errorMessage = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// A chunk in a streaming response.
/// </summary>
public class StreamChunk
{
    public string ChunkId { get; set; } = Guid.NewGuid().ToString();
    public int SequenceNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public StreamChunkType Type { get; set; } = StreamChunkType.Text;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Type of stream chunk.
/// </summary>
public enum StreamChunkType
{
    Text,
    Code,
    Markdown,
    Json,
    Tool,
    Progress
}
