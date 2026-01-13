using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Channels.Abstractions;

namespace Platform.Engineering.Copilot.Channels.Services;

/// <summary>
/// Default implementation of IStreamingHandler.
/// </summary>
public class StreamingHandler : IStreamingHandler
{
    private readonly IChannelManager _channelManager;
    private readonly ILogger<StreamingHandler> _logger;

    public StreamingHandler(IChannelManager channelManager, ILogger<StreamingHandler> logger)
    {
        _channelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IStreamContext> BeginStreamAsync(string conversationId, string? agentType = null, CancellationToken cancellationToken = default)
    {
        var context = new StreamContext(conversationId, agentType, _channelManager, _logger);
        _logger.LogDebug("Started stream {StreamId} for conversation {ConversationId}", context.StreamId, conversationId);
        return Task.FromResult<IStreamContext>(context);
    }
}

/// <summary>
/// Stream context implementation.
/// </summary>
internal class StreamContext : IStreamContext
{
    private readonly IChannelManager _channelManager;
    private readonly ILogger _logger;
    private int _sequenceNumber;
    private bool _isCompleted;
    private readonly StringBuilder _buffer = new();

    public string StreamId { get; } = Guid.NewGuid().ToString();
    public string ConversationId { get; }
    public string? AgentType { get; }

    public StreamContext(string conversationId, string? agentType, IChannelManager channelManager, ILogger logger)
    {
        ConversationId = conversationId;
        AgentType = agentType;
        _channelManager = channelManager;
        _logger = logger;
    }

    public async Task WriteAsync(string content, CancellationToken cancellationToken = default)
    {
        if (_isCompleted) return;

        _buffer.Append(content);
        _sequenceNumber++;

        var message = new ChannelMessage
        {
            ConversationId = ConversationId,
            Type = MessageType.StreamChunk,
            Content = content,
            AgentType = AgentType,
            IsStreaming = true,
            IsComplete = false,
            Metadata = new Dictionary<string, object>
            {
                ["streamId"] = StreamId,
                ["sequenceNumber"] = _sequenceNumber
            }
        };

        await _channelManager.SendToConversationAsync(ConversationId, message, cancellationToken);
    }

    public async Task WriteAsync(StreamChunk chunk, CancellationToken cancellationToken = default)
    {
        if (_isCompleted) return;

        _buffer.Append(chunk.Content);
        _sequenceNumber++;
        chunk.SequenceNumber = _sequenceNumber;

        var message = new ChannelMessage
        {
            ConversationId = ConversationId,
            Type = MessageType.StreamChunk,
            Content = chunk.Content,
            AgentType = AgentType,
            IsStreaming = true,
            IsComplete = false,
            Metadata = new Dictionary<string, object>
            {
                ["streamId"] = StreamId,
                ["sequenceNumber"] = _sequenceNumber,
                ["chunkType"] = chunk.Type.ToString()
            }
        };

        await _channelManager.SendToConversationAsync(ConversationId, message, cancellationToken);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_isCompleted) return;
        _isCompleted = true;

        var message = new ChannelMessage
        {
            ConversationId = ConversationId,
            Type = MessageType.AgentResponse,
            Content = _buffer.ToString(),
            AgentType = AgentType,
            IsStreaming = false,
            IsComplete = true,
            Metadata = new Dictionary<string, object>
            {
                ["streamId"] = StreamId,
                ["totalChunks"] = _sequenceNumber
            }
        };

        await _channelManager.SendToConversationAsync(ConversationId, message, cancellationToken);
        _logger.LogDebug("Completed stream {StreamId} with {TotalChunks} chunks", StreamId, _sequenceNumber);
    }

    public async Task AbortAsync(string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        if (_isCompleted) return;
        _isCompleted = true;

        var message = new ChannelMessage
        {
            ConversationId = ConversationId,
            Type = MessageType.Error,
            Content = errorMessage ?? "Stream aborted",
            AgentType = AgentType,
            IsStreaming = false,
            IsComplete = true,
            Metadata = new Dictionary<string, object>
            {
                ["streamId"] = StreamId,
                ["aborted"] = true
            }
        };

        await _channelManager.SendToConversationAsync(ConversationId, message, cancellationToken);
        _logger.LogWarning("Aborted stream {StreamId}: {ErrorMessage}", StreamId, errorMessage);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isCompleted)
        {
            await CompleteAsync();
        }
    }
}

/// <summary>
/// StringBuilder for stream buffer.
/// </summary>
internal class StringBuilder
{
    private readonly List<string> _parts = new();

    public void Append(string value) => _parts.Add(value);

    public override string ToString() => string.Concat(_parts);
}
