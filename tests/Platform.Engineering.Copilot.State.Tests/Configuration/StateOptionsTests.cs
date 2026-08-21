using FluentAssertions;
using Platform.Engineering.Copilot.State.Configuration;
using Xunit;

namespace Platform.Engineering.Copilot.State.Tests.Configuration;

public class StateOptionsTests
{
    [Fact]
    public void Defaults_UseInMemoryProvider()
    {
        var options = new StateOptions();

        options.Provider.Should().Be(StateProvider.Memory);
        options.RedisConnectionString.Should().BeNull();
        options.RedisInstanceName.Should().Be("platform-copilot:");
    }

    [Fact]
    public void SectionName_MatchesConfigurationConvention()
    {
        StateOptions.SectionName.Should().Be("StateManagement");
    }
}
