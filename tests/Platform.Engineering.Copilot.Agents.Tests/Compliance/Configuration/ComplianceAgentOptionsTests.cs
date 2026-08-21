using FluentAssertions;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Xunit;

namespace Platform.Engineering.Copilot.Agents.Tests.Compliance.Configuration;

public class ComplianceAgentOptionsTests
{
    [Fact]
    public void Defaults_MatchFedRampHighFocusedConfiguration()
    {
        var options = new ComplianceAgentOptions();

        options.Enabled.Should().BeTrue();
        options.Temperature.Should().Be(0.2);
        options.MaxTokens.Should().Be(6000);
        options.EnableAutomatedRemediation.Should().BeTrue();
        options.DefaultFramework.Should().Be("NIST80053");
        options.DefaultBaseline.Should().Be("FedRAMPHigh");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void Temperature_AcceptsValidRange(double temperature)
    {
        var options = new ComplianceAgentOptions { Temperature = temperature };

        options.Temperature.Should().Be(temperature);
    }
}
