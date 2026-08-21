using FluentAssertions;
using Platform.Engineering.Copilot.Chat.App.Models;
using Xunit;

namespace Platform.Engineering.Copilot.Chat.Tests.Models;

public class ConversationTests
{
    [Fact]
    public void New_Conversation_HasGeneratedIdAndDefaults()
    {
        var conversation = new Conversation();

        conversation.Id.Should().NotBeNullOrWhiteSpace();
        conversation.Title.Should().Be("New Conversation");
        conversation.IsArchived.Should().BeFalse();
        conversation.Messages.Should().BeEmpty();
    }

    [Fact]
    public void ChatMessage_DefaultsToUserRoleAndSentStatus()
    {
        var message = new ChatMessage { ConversationId = "conv-123", Content = "Hello" };

        message.Role.Should().Be(MessageRole.User);
        message.Status.Should().Be(MessageStatus.Sent);
        message.ConversationId.Should().Be("conv-123");
    }
}
