using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Services.Infrastructure;
using Xunit;

namespace Platform.Engineering.Copilot.Tests.Unit.Core.Services.Infrastructure;

public class InfrastructureProvisioningServiceTests
{
    private readonly Mock<ILogger<InfrastructureProvisioningService>> _mockLogger;
    private readonly Mock<IAzureResourceService> _mockAzureResourceService;
    private readonly InfrastructureProvisioningService _service;

    public InfrastructureProvisioningServiceTests()
    {
        _mockLogger = new Mock<ILogger<InfrastructureProvisioningService>>();
        _mockAzureResourceService = new Mock<IAzureResourceService>();

        _service = new InfrastructureProvisioningService(
            _mockLogger.Object,
            _mockAzureResourceService.Object);
    }

    [Fact]
    public async Task ProvisionInfrastructureAsync_WithStorageAccountQuery_ParsesCorrectlyAsync()
    {
        // Arrange - Pattern-based parsing now handles this
        var query = "Create storage account named teststorage in eastus with Standard_LRS";
        
        // Act - Just verify it doesn't throw
        var result = await _service.ProvisionInfrastructureAsync(query, CancellationToken.None);

        // Assert - Should parse the query successfully
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task EstimateCostAsync_ForStorageAccount_ReturnsCostEstimateAsync()
    {
        // Arrange
        var query = "Estimate cost for a storage account in eastus";

        // Act
        var estimate = await _service.EstimateCostAsync(query, CancellationToken.None);

        // Assert
        estimate.ResourceType.Should().Be("storage-account");
        estimate.MonthlyEstimate.Should().Be(20.00m);
        estimate.AnnualEstimate.Should().Be(240.00m);
        estimate.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ListResourceGroupsAsync_WhenCalled_ReturnsListAsync()
    {
        // Arrange
        _mockAzureResourceService
            .Setup(s => s.ListResourceGroupsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<object>
            {
                new { name = "rg-test1" },
                new { name = "rg-test2" }
            });

        // Act
        var result = await _service.ListResourceGroupsAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("rg-test1");
        result.Should().Contain("rg-test2");
    }

    [Fact]
    public async Task DeleteResourceGroupAsync_WhenSuccessful_ReturnsTrueAsync()
    {
        // Arrange
        _mockAzureResourceService
            .Setup(s => s.DeleteResourceGroupAsync(
                "rg-test",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteResourceGroupAsync("rg-test", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }
}
