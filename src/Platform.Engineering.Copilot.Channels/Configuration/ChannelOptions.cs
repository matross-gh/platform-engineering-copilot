namespace Platform.Engineering.Copilot.Channels.Configuration;

/// <summary>
/// Configuration options for channels.
/// </summary>
public class ChannelOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Channels";

    /// <summary>
    /// Enable SignalR channel.
    /// </summary>
    public bool EnableSignalR { get; set; } = true;

    /// <summary>
    /// SignalR hub path.
    /// </summary>
    public string SignalRHubPath { get; set; } = "/hubs/chat";

    /// <summary>
    /// Enable connection logging.
    /// </summary>
    public bool EnableConnectionLogging { get; set; } = true;

    /// <summary>
    /// Idle connection timeout.
    /// </summary>
    public TimeSpan IdleConnectionTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Maximum connections per user.
    /// </summary>
    public int MaxConnectionsPerUser { get; set; } = 5;

    /// <summary>
    /// Enable message acknowledgment.
    /// </summary>
    public bool EnableMessageAcknowledgment { get; set; } = true;

    /// <summary>
    /// Streaming options.
    /// </summary>
    public StreamingOptions Streaming { get; set; } = new();
}

/// <summary>
/// Streaming configuration.
/// </summary>
public class StreamingOptions
{
    /// <summary>
    /// Enable streaming responses.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum chunk size in characters.
    /// </summary>
    public int MaxChunkSize { get; set; } = 100;

    /// <summary>
    /// Minimum delay between chunks (for rate limiting).
    /// </summary>
    public TimeSpan MinChunkDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Stream timeout.
    /// </summary>
    public TimeSpan StreamTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
