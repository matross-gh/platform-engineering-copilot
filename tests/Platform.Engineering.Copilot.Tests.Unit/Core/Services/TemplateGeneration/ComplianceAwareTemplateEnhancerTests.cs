using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Platform.Engineering.Copilot.Core.Interfaces;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Services;
using Platform.Engineering.Copilot.Core.Services.TemplateGeneration;
using Xunit;

namespace Platform.Engineering.Copilot.Tests.Unit.Core.Services.TemplateGeneration;

/// <summary>
/// Unit tests for ComplianceAwareTemplateEnhancer
/// Tests compliance framework mapping, control injection, and validation
/// </summary>
public class ComplianceAwareTemplateEnhancerTests
{
    private readonly Mock<ILogger<ComplianceAwareTemplateEnhancer>> _mockLogger;
    private readonly Mock<IDynamicTemplateGenerator> _mockTemplateGenerator;
    private readonly Mock<IAzurePolicyService> _mockPolicyService;
    private readonly Mock<INistControlsService> _mockNistService;
    private readonly ComplianceAwareTemplateEnhancer _service;

    public ComplianceAwareTemplateEnhancerTests()
    {
        _mockLogger = new Mock<ILogger<ComplianceAwareTemplateEnhancer>>();
        _mockTemplateGenerator = new Mock<IDynamicTemplateGenerator>();
        _mockPolicyService = new Mock<IAzurePolicyService>();
        _mockNistService = new Mock<INistControlsService>();

        _service = new ComplianceAwareTemplateEnhancer(
            _mockLogger.Object,
            _mockTemplateGenerator.Object,
            _mockPolicyService.Object,
            _mockNistService.Object
        );
    }

    #region Helper Methods

    private TemplateGenerationRequest CreateBasicRequest()
    {
        return new TemplateGenerationRequest
        {
            ServiceName = "test-service",
            Description = "Test service",
            TemplateType = "microservice",
            Application = new ApplicationSpec
            {
                Language = ProgrammingLanguage.NodeJS,
                Framework = "express",
                Type = ApplicationType.WebAPI,
                Port = 8080
            },
            Infrastructure = new InfrastructureSpec
            {
                Format = InfrastructureFormat.Bicep,
                Provider = CloudProvider.Azure,
                Region = "eastus",
                ComputePlatform = ComputePlatform.AKS,
                Tags = new Dictionary<string, string>()
            },
            Deployment = new DeploymentSpec
            {
                Replicas = 3,
                AutoScaling = true,
                MinReplicas = 1,
                MaxReplicas = 10
            },
            Security = new SecuritySpec
            {
                NetworkPolicies = false,
                RBAC = false,
                TLS = false,
                ComplianceStandards = new List<string>()
            },
            Observability = new ObservabilitySpec
            {
                Prometheus = false,
                StructuredLogging = false
            }
        };
    }

    private NistControl CreateMockNistControl(string id, string title)
    {
        return new NistControl { Id = id, Title = title };
    }

    #endregion

    #region Framework Mapping Tests

    [Theory]
    [InlineData("FedRAMP-High")]
    [InlineData("DoD-IL5")]
    [InlineData("NIST-800-53")]
    public async Task EnhanceWithComplianceAsync_WithValidFramework_CallsTemplateGenerator(string framework)
    {
        // Arrange
        var request = CreateBasicRequest();
        
        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        _mockTemplateGenerator
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateGenerationResult
            {
                Success = true,
                Files = new Dictionary<string, string> { { "main.bicep", "resource content" } },
                Summary = "Template generated"
            });

        // Act
        var result = await _service.EnhanceWithComplianceAsync(request, framework);

        // Assert
        Assert.True(result.Success);
        _mockTemplateGenerator.Verify(
            x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task EnhanceWithComplianceAsync_FedRAMPHigh_FetchesNistControls()
    {
        // Arrange
        var request = CreateBasicRequest();
        var controlCallCount = 0;
        
        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => controlCallCount++)
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        _mockTemplateGenerator
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateGenerationResult { Success = true });

        // Act
        await _service.EnhanceWithComplianceAsync(request, "FedRAMP-High");

        // Assert - FedRAMP-High should fetch multiple controls
        Assert.True(controlCallCount > 0, "Should fetch NIST controls");
    }

    #endregion

    #region Compliance Injection Tests

    [Fact]
    public async Task EnhanceWithComplianceAsync_InjectsSecuritySettings()
    {
        // Arrange
        var request = CreateBasicRequest();
        TemplateGenerationRequest? capturedRequest = null;
        
        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        _mockTemplateGenerator
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TemplateGenerationRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new TemplateGenerationResult { Success = true });

        // Act
        await _service.EnhanceWithComplianceAsync(request, "FedRAMP-High");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Security.RBAC);
        Assert.True(capturedRequest.Security.TLS);
        Assert.True(capturedRequest.Security.NetworkPolicies);
    }

    [Fact]
    public async Task EnhanceWithComplianceAsync_InjectsObservabilitySettings()
    {
        // Arrange
        var request = CreateBasicRequest();
        TemplateGenerationRequest? capturedRequest = null;
        
        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        _mockTemplateGenerator
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TemplateGenerationRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new TemplateGenerationResult { Success = true });

        // Act
        await _service.EnhanceWithComplianceAsync(request, "FedRAMP-High");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Observability.StructuredLogging);
        Assert.True(capturedRequest.Observability.Prometheus);
    }

    [Fact]
    public async Task EnhanceWithComplianceAsync_AddsComplianceTags()
    {
        // Arrange
        var request = CreateBasicRequest();
        TemplateGenerationRequest? capturedRequest = null;
        
        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        _mockTemplateGenerator
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TemplateGenerationRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new TemplateGenerationResult { Success = true });

        // Act
        await _service.EnhanceWithComplianceAsync(request, "FedRAMP-High");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest!.Infrastructure.Tags);
        Assert.Contains(capturedRequest.Infrastructure.Tags, kvp => kvp.Key == "Compliance" && kvp.Value == "FedRAMP-High");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateComplianceAsync_WithValidTemplate_ReturnsResult()
    {
        // Arrange
        var templateContent = @"
            resource kv 'Microsoft.KeyVault/vaults@2023-02-01' = {
                properties: {
                    enableSoftDelete: true
                    enablePurgeProtection: true
                }
            }";

        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        // Act
        var result = await _service.ValidateComplianceAsync(templateContent, "FedRAMP-High");

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task EnhanceWithComplianceAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.EnhanceWithComplianceAsync(null!, "FedRAMP-High"));
    }

    [Fact]
    public async Task EnhanceWithComplianceAsync_EmptyFramework_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateBasicRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.EnhanceWithComplianceAsync(request, ""));
    }

    [Fact]
    public async Task EnhanceWithComplianceAsync_TemplateGenerationFails_ReturnsFailureResult()
    {
        // Arrange
        var request = CreateBasicRequest();
        
        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        _mockTemplateGenerator
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateGenerationResult
            {
                Success = false,
                ErrorMessage = "Template generation failed"
            });

        // Act
        var result = await _service.EnhanceWithComplianceAsync(request, "FedRAMP-High");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Template generation failed", result.ErrorMessage);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task EnhanceWithComplianceAsync_CompleteFlow_GeneratesCompliantTemplate()
    {
        // Arrange
        var request = CreateBasicRequest();
        TemplateGenerationRequest? enhancedRequest = null;
        
        _mockNistService
            .Setup(x => x.GetControlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockNistControl("SC-28", "Protection of Information at Rest"));

        _mockTemplateGenerator
            .Setup(x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TemplateGenerationRequest, CancellationToken>((req, ct) => enhancedRequest = req)
            .ReturnsAsync(new TemplateGenerationResult
            {
                Success = true,
                Files = new Dictionary<string, string>
                {
                    { "main.bicep", "compliant resource" }
                },
                Summary = "FedRAMP-High compliant template generated"
            });

        // Act
        var result = await _service.EnhanceWithComplianceAsync(request, "FedRAMP-High");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(enhancedRequest);
        
        // Verify security settings were injected
        Assert.True(enhancedRequest!.Security.RBAC);
        Assert.True(enhancedRequest.Security.TLS);
        Assert.True(enhancedRequest.Security.NetworkPolicies);
        
        // Verify observability settings were injected
        Assert.True(enhancedRequest.Observability.StructuredLogging);
        Assert.True(enhancedRequest.Observability.Prometheus);
        
        // Verify compliance tags were added
        Assert.Contains(enhancedRequest.Infrastructure.Tags!, kvp => kvp.Key == "Compliance");
        
        // Verify template generation was called
        _mockTemplateGenerator.Verify(
            x => x.GenerateTemplateAsync(It.IsAny<TemplateGenerationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
