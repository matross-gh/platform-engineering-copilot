using FluentAssertions;
using Platform.Engineering.Copilot.Mcp.Models;
using Xunit;

namespace Platform.Engineering.Copilot.Mcp.Tests.Models;

public class McpChatResponseTests
{
    [Fact]
    public void Defaults_AreEmptyAndUnsuccessful()
    {
        var response = new McpChatResponse();

        response.Success.Should().BeFalse();
        response.Response.Should().BeEmpty();
        response.ConversationId.Should().BeEmpty();
        response.ToolsExecuted.Should().BeEmpty();
    }

    [Fact]
    public void CanRepresentASuccessfulResponse()
    {
        var response = new McpChatResponse
        {
            Success = true,
            Response = "Deployment plan created.",
            ConversationId = "conv-123",
            AgentName = "Infrastructure"
        };

        response.Success.Should().BeTrue();
        response.AgentName.Should().Be("Infrastructure");
    }
}
