using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Plugins;
using Platform.Engineering.Copilot.Core.Data.Entities;
using Xunit;

namespace Platform.Engineering.Copilot.Tests.Unit.Core.Plugins
{
    public class ServiceCreationPluginTests
    {
        private static ServiceCreationPlugin CreatePlugin(Mock<IServiceCreationService> ServiceCreationServiceMock)
        {
            var kernel = Kernel.CreateBuilder().Build();
            var logger = Mock.Of<ILogger<ServiceCreationPlugin>>();
            return new ServiceCreationPlugin(logger, kernel, ServiceCreationServiceMock.Object);
        }

        [Fact]
        public async Task CaptureServiceCreationRequirementsAsync_CreatesDraft()
        {
            var ServiceCreationServiceMock = new Mock<IServiceCreationService>();
            ServiceCreationServiceMock.Setup(s => s.CreateDraftRequestAsync(It.IsAny<CancellationToken>())).ReturnsAsync("req-123");
            var plugin = CreatePlugin(ServiceCreationServiceMock);

            var response = await plugin.CaptureServiceCreationRequirementsAsync("Seawolf", "COMNAVWAR", null);

            response.Should().Contain("req-123");
        }

        [Fact]
        public async Task SubmitForApprovalAsync_ValidRequest_Succeeds()
        {
            var requestId = "11111111-1111-1111-1111-111111111111";
            var ServiceCreationServiceMock = new Mock<IServiceCreationService>();
            ServiceCreationServiceMock.Setup(s => s.GetRequestAsync(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceCreationRequest { Id = requestId, Status = ServiceCreationStatus.Draft, MissionName = "Test" });
            ServiceCreationServiceMock.Setup(s => s.ValidateForSubmissionAsync(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new System.Collections.Generic.List<string>());
            ServiceCreationServiceMock.Setup(s => s.SubmitRequestAsync(requestId, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var plugin = CreatePlugin(ServiceCreationServiceMock);

            var response = await plugin.SubmitForApprovalAsync(requestId);

            response.Should().Contain("submitted");
        }

        [Fact]
        public async Task SubmitForApprovalAsync_InvalidRequest_ReturnsError()
        {
            var ServiceCreationServiceMock = new Mock<IServiceCreationService>();
            ServiceCreationServiceMock.Setup(s => s.GetRequestAsync("bad-id", It.IsAny<CancellationToken>())).ReturnsAsync((ServiceCreationRequest?)null);
            var plugin = CreatePlugin(ServiceCreationServiceMock);

            var response = await plugin.SubmitForApprovalAsync("bad-id");

            response.Should().Contain("not found");
        }
    }
}
