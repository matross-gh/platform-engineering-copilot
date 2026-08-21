using FluentAssertions;
using Platform.Engineering.Copilot.Channels.Abstractions;
using Xunit;

namespace Platform.Engineering.Copilot.Channels.Tests.Abstractions;

public class ChannelMessageTests
{
    [Fact]
    public void New_ChannelMessage_HasGeneratedIdAndDefaults()
    {
        var message = new ChannelMessage();

        message.MessageId.Should().NotBeNullOrWhiteSpace();
        message.Type.Should().Be(MessageType.AgentResponse);
        message.IsStreaming.Should().BeFalse();
        message.IsComplete.Should().BeFalse();
    }

    [Theory]
    [InlineData(ChannelType.SignalR)]
    [InlineData(ChannelType.WebSocket)]
    [InlineData(ChannelType.LongPolling)]
    [InlineData(ChannelType.ServerSentEvents)]
    public void ChannelType_SupportsAllExpectedTransports(ChannelType type)
    {
        Enum.IsDefined(type).Should().BeTrue();
    }
}
