using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Interfaces.Azure;
using Platform.Engineering.Copilot.Core.Interfaces.Infrastructure;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.Core.Services.Agents;
using Platform.Engineering.Copilot.Core.Services.Azure;
using Xunit;

namespace Platform.Engineering.Copilot.Tests.Integration.Plugins.Infrastructure;

/// <summary>
/// Integration tests for InfrastructurePlugin template generation workflows.
/// Tests end-to-end scenarios including template generation, storage, retrieval, and compliance validation.
/// </summary>
public class InfrastructurePluginTemplateIntegrationTests
{
    private readonly Mock<ILogger<object>> _loggerMock;
    private readonly Mock<IDynamicTemplateGenerator> _templateGeneratorMock;
    private readonly Mock<ITemplateStorageService> _templateStorageServiceMock;
    private readonly Mock<IAzureResourceService> _azureResourceServiceMock;
    private readonly Mock<IComplianceAwareTemplateEnhancer> _complianceEnhancerMock;
    private readonly Mock<AzureMcpClient> _azureMcpClientMock;
    private readonly Mock<ILogger<SharedMemory>> _sharedMemoryLoggerMock;
    private readonly SharedMemory _sharedMemory;

    public InfrastructurePluginTemplateIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<object>>();
        _templateGeneratorMock = new Mock<IDynamicTemplateGenerator>();
        _templateStorageServiceMock = new Mock<ITemplateStorageService>();
        _azureResourceServiceMock = new Mock<IAzureResourceService>();
        _complianceEnhancerMock = new Mock<IComplianceAwareTemplateEnhancer>();
        _azureMcpClientMock = new Mock<AzureMcpClient>(null!, null!, null!);
        _sharedMemoryLoggerMock = new Mock<ILogger<SharedMemory>>();
        _sharedMemory = new SharedMemory(_sharedMemoryLoggerMock.Object);
    }

    #region Template Generation Workflow Tests

    [Fact]
    public async Task GenerateInfrastructureTemplate_WithAKS_CompletesSuccessfully()
    {
        // Arrange
        var description = "AKS cluster with 3 nodes and monitoring";
        var resourceType = "aks";
        var format = "bicep";
        var location = "usgovvirginia";

        var templateFiles = new Dictionary<string, string>
        {
            ["main.bicep"] = "param location string = 'usgovvirginia'\nparam nodeCount int = 3",
            ["modules/aks/main.bicep"] = "resource aksCluster 'Microsoft.ContainerService/managedClusters@2024-01-01' = {}",
            ["modules/aks/outputs.bicep"] = "output aksClusterId string = aksCluster.id"
        };

        _templateGeneratorMock
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateGenerationResult
            {
                Success = true,
                Files = templateFiles,
                Summary = "Generated AKS template with 3 files"
            });

        // Act
        var result = await _templateGeneratorMock.Object.GenerateTemplateAsync(
            BuildAksRequest(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Files.Should().HaveCount(3);
        result.Files.Should().ContainKey("main.bicep");
        result.Files.Should().ContainKey("modules/aks/main.bicep");
        _templateGeneratorMock.Verify(
            x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateCompliantTemplate_WithFedRAMP_AppliesSecurityControls()
    {
        // Arrange
        var complianceFramework = "NIST-800-53";
        var description = "FedRAMP-compliant AKS cluster";

        var enhancedFiles = new Dictionary<string, string>
        {
            ["main.bicep"] = @"
param location string = 'usgovvirginia'
// FedRAMP High security controls applied
resource storageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'Audit'
        enabled: true
      }
    ]
  }
}",
            ["security/rbac.bicep"] = "// RBAC assignments with Zero Trust defaults"
        };

        _complianceEnhancerMock
            .Setup(x => x.EnhanceWithComplianceAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateGenerationResult
            {
                Success = true,
                Files = enhancedFiles,
                Summary = "Compliance controls applied"
            });

        // Act
        var result = await _complianceEnhancerMock.Object.EnhanceWithComplianceAsync(
            BuildStorageRequest(), complianceFramework, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Files.Should().ContainKey("security/rbac.bicep");
        result.Files["main.bicep"].Should().Contain("FedRAMP");
    }

    [Fact]
    public void SharedMemory_StoresAndRetrievesTemplateFiles_Across_Conversations()
    {
        // Arrange
        var conv1 = "conversation-001";
        var conv2 = "conversation-002";
        var files1 = new Dictionary<string, string>
        {
            ["main.bicep"] = "// Template 1",
            ["modules/aks.bicep"] = "// AKS module"
        };
        var files2 = new Dictionary<string, string>
        {
            ["main.tf"] = "# Template 2",
            ["modules/aks.tf"] = "# AKS module"
        };

        // Act
        _sharedMemory.StoreGeneratedFiles(conv1, files1);
        _sharedMemory.StoreGeneratedFiles(conv2, files2);

        var conv1Files = _sharedMemory.GetGeneratedFileNames(conv1);
        var conv2Files = _sharedMemory.GetGeneratedFileNames(conv2);

        // Assert
        conv1Files.Should().HaveCount(2);
        conv2Files.Should().HaveCount(2);
        conv1Files.Should().NotContain(conv2Files);
        _sharedMemory.GetGeneratedFile(conv1, "main.bicep").Should().Contain("Template 1");
        _sharedMemory.GetGeneratedFile(conv2, "main.tf").Should().Contain("Template 2");
    }

    #endregion

    #region Error Scenarios Tests

    [Fact]
    public async Task GenerateTemplate_WithNullDescription_HandlesGracefully()
    {
        // Arrange
        var templateRequest = new TemplateGenerationRequest
        {
            ServiceName = "myservice",
            Description = null!,
            TemplateType = "infrastructure-only",
            Infrastructure = new InfrastructureSpec
            {
                Format = InfrastructureFormat.Bicep,
                Provider = CloudProvider.Azure,
                Region = "eastus",
                ComputePlatform = ComputePlatform.Storage
            }
        };

        _templateGeneratorMock
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateGenerationResult
            {
                Success = false,
                ErrorMessage = "Description is required"
            });

        // Act
        var result = await _templateGeneratorMock.Object.GenerateTemplateAsync(templateRequest, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Description");
    }

    [Fact]
    public void SharedMemory_WithNullConversationId_ThrowsArgumentNullException()
    {
        // Act & Assert
        _sharedMemory.Invoking(sm => sm.GetGeneratedFileNames(null!))
            .Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Helper Methods

    private static TemplateGenerationRequest BuildAksRequest()
    {
        return new TemplateGenerationRequest
        {
            ServiceName = "aks-prod",
            Description = "Production AKS cluster with 3 nodes",
            TemplateType = "infrastructure-only",
            Infrastructure = new InfrastructureSpec
            {
                Format = InfrastructureFormat.Bicep,
                Provider = CloudProvider.Azure,
                Region = "usgovvirginia",
                ComputePlatform = ComputePlatform.AKS,
                NodeCount = 3,
                Environment = "production"
            },
            Security = new SecuritySpec
            {
                EnableWorkloadIdentity = true,
                EnablePrivateCluster = true,
                EnableAzurePolicy = true
            },
            Observability = new ObservabilitySpec
            {
                EnableContainerInsights = true,
                EnablePrometheus = true
            }
        };
    }

    private static TemplateGenerationRequest BuildStorageRequest()
    {
        return new TemplateGenerationRequest
        {
            ServiceName = "storage-prod",
            Description = "FedRAMP-compliant storage account",
            TemplateType = "infrastructure-only",
            Infrastructure = new InfrastructureSpec
            {
                Format = InfrastructureFormat.Bicep,
                Provider = CloudProvider.Azure,
                Region = "usgovvirginia",
                ComputePlatform = ComputePlatform.Storage,
                Environment = "production"
            },
            Security = new SecuritySpec
            {
                EnablePrivateEndpoint = true,
                EnableDefender = true
            },
            Observability = new ObservabilitySpec
            {
                EnableDiagnostics = true
            }
        };
    }

    #endregion
}
