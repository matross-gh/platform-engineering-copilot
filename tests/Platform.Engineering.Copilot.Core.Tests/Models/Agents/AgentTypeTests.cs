using FluentAssertions;
using Platform.Engineering.Copilot.Core.Models.Agents;
using Xunit;

namespace Platform.Engineering.Copilot.Core.Tests.Models.Agents;

public class AgentTypeTests
{
    [Fact]
    public void AgentType_ContainsExpectedCoreValues()
    {
        Enum.GetNames<AgentType>().Should().Contain(new[]
        {
            nameof(AgentType.Orchestrator),
            nameof(AgentType.Infrastructure)
        });
    }

    [Fact]
    public void AgentType_ValuesAreDistinct()
    {
        var values = Enum.GetValues<AgentType>();
        values.Should().OnlyHaveUniqueItems();
    }
}
