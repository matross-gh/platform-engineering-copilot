using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Channels.Abstractions;
using Platform.Engineering.Copilot.State.Abstractions;
using Platform.Engineering.Copilot.State.Models;

namespace Platform.Engineering.Copilot.Channels.Services;

/// <summary>
/// Default message handler that stores messages and can route to agents.
/// </summary>
public class DefaultMessageHandler : IMessageHandler
{
    private readonly IConversationStateManager _conversationStateManager;
    private readonly IChannelManager _channelManager;
    private readonly ILogger<DefaultMessageHandler> _logger;

    // Delegate for agent invocation (to be set by the orchestrator)
    public Func<IncomingMessage, CancellationToken, Task<ChannelMessage>>? AgentInvoker { get; set; }

    public DefaultMessageHandler(
        IConversationStateManager conversationStateManager,
        IChannelManager channelManager,
        ILogger<DefaultMessageHandler> logger)
    {
        _conversationStateManager = conversationStateManager ?? throw new ArgumentNullException(nameof(conversationStateManager));
        _channelManager = channelManager ?? throw new ArgumentNullException(nameof(channelManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChannelMessage> HandleAsync(IncomingMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Handling message {MessageId} for conversation {ConversationId}", 
            message.MessageId, message.ConversationId);

        try
        {
            // Store the user message
            await _conversationStateManager.AddMessageAsync(
                message.ConversationId,
                new ConversationMessage
                {
                    MessageId = message.MessageId,
                    Role = MessageRole.User,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    Metadata = message.Metadata
                },
                cancellationToken);

            // Send "thinking" indicator
            await _channelManager.SendToConversationAsync(
                message.ConversationId,
                new ChannelMessage
                {
                    ConversationId = message.ConversationId,
                    Type = MessageType.AgentThinking,
                    Content = "Processing your request...",
                },
                cancellationToken);

            // Invoke agent if available
            ChannelMessage response;
            if (AgentInvoker != null)
            {
                response = await AgentInvoker(message, cancellationToken);
            }
            else
            {
                // Default echo response if no agent is configured
                response = new ChannelMessage
                {
                    ConversationId = message.ConversationId,
                    Type = MessageType.AgentResponse,
                    Content = $"Received: {message.Content}",
                    IsComplete = true
                };
            }

            // Store the assistant message
            await _conversationStateManager.AddMessageAsync(
                message.ConversationId,
                new ConversationMessage
                {
                    MessageId = response.MessageId,
                    Role = MessageRole.Assistant,
                    Content = response.Content,
                    Timestamp = response.Timestamp,
                    AgentType = response.AgentType,
                    Metadata = response.Metadata
                },
                cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message {MessageId}", message.MessageId);

            return new ChannelMessage
            {
                ConversationId = message.ConversationId,
                Type = MessageType.Error,
                Content = $"Error processing message: {ex.Message}",
                IsComplete = true
            };
        }
    }
}
