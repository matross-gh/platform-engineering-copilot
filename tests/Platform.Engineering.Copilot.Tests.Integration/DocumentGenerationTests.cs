using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Platform.Engineering.Copilot.Tests.Integration;

/// <summary>
/// Integration tests for AI-enhanced document generation service
/// Tests both AI-enhanced and template-based document generation
/// Run these against a real Azure subscription to verify document generation
/// </summary>
public class DocumentGenerationTests
{
    private readonly ITestOutputHelper _output;

    public DocumentGenerationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task GenerateControlNarrative_WithValidControl_ReturnsNarrative()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Note: This would need full DI setup from ServiceCollectionExtensions
        // For now, this is a template for manual testing
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var narrative = await documentService.GenerateControlNarrativeAsync("AC-2");

        // Assert
        Assert.NotNull(narrative);
        Assert.Equal("AC-2", narrative.ControlId);
        Assert.NotEmpty(narrative.What);
        Assert.NotEmpty(narrative.How);
        
        _output.WriteLine($"Generated narrative for {narrative.ControlId}:");
        _output.WriteLine($"Title: {narrative.ControlTitle}");
        _output.WriteLine($"Status: {narrative.ImplementationStatus}");
        _output.WriteLine($"What: {narrative.What?.Substring(0, Math.Min(100, narrative.What?.Length ?? 0))}...");
        _output.WriteLine($"How: {narrative.How?.Substring(0, Math.Min(100, narrative.How?.Length ?? 0))}...");
        
        // AI-enhanced fields
        if (!string.IsNullOrEmpty(narrative.Evidence))
        {
            _output.WriteLine($"Evidence (AI-generated): {narrative.Evidence?.Substring(0, Math.Min(100, narrative.Evidence?.Length ?? 0))}...");
        }
        if (!string.IsNullOrEmpty(narrative.Gaps))
        {
            _output.WriteLine($"Gaps (AI-identified): {narrative.Gaps}");
        }
        if (!string.IsNullOrEmpty(narrative.ResponsibleParty))
        {
            _output.WriteLine($"Responsible Party (AI-determined): {narrative.ResponsibleParty}");
        }
    }
    
    [Fact(Skip = "Manual test - requires real Azure subscription with AI")]
    public async Task GenerateControlNarrative_WithAI_ReturnsEnhancedNarrative()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Semantic Kernel with Azure OpenAI (requires configuration)
        // This test validates AI-enhanced narrative generation
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var narrative = await documentService.GenerateControlNarrativeAsync("AC-2", "test-subscription-id");

        // Assert - AI-enhanced fields should be populated
        Assert.NotNull(narrative);
        Assert.Equal("AC-2", narrative.ControlId);
        Assert.NotEmpty(narrative.What);
        Assert.NotEmpty(narrative.How);
        Assert.NotEmpty(narrative.Evidence); // AI-generated
        Assert.NotNull(narrative.Gaps); // AI-identified (may be empty if compliant)
        Assert.NotEmpty(narrative.ResponsibleParty); // AI-determined
        Assert.NotEmpty(narrative.ImplementationDetails); // AI-generated
        
        _output.WriteLine("=== AI-Enhanced Control Narrative ===");
        _output.WriteLine($"Control: {narrative.ControlId} - {narrative.ControlTitle}");
        _output.WriteLine($"\nWhat (AI): {narrative.What}");
        _output.WriteLine($"\nHow (AI): {narrative.How}");
        _output.WriteLine($"\nEvidence (AI): {narrative.Evidence}");
        _output.WriteLine($"\nGaps (AI): {narrative.Gaps ?? "None identified"}");
        _output.WriteLine($"\nResponsible Party (AI): {narrative.ResponsibleParty}");
        _output.WriteLine($"\nImplementation Details (AI): {narrative.ImplementationDetails}");
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task GenerateSSP_WithValidParameters_ReturnsDocument()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        var sspParams = new SspParameters
        {
            SystemName = "Test Azure Government System",
            SystemDescription = "Test system for document generation validation",
            ImpactLevel = "IL4",
            SystemOwner = "Test Owner",
            AuthorizingOfficial = "Test AO",
            Classification = "UNCLASSIFIED",
            Environment = "Production"
        };

        // Act
        var document = await documentService.GenerateSSPAsync(
            "test-subscription-id", 
            sspParams);

        // Assert
        Assert.NotNull(document);
        Assert.Equal("SSP", document.DocumentType);
        Assert.NotEmpty(document.Content);
        Assert.Contains("System Security Plan", document.Title);
        
        // Check for evidence metadata
        Assert.True(document.Metadata.ContainsKey("EvidencePackageId"));
        Assert.True(document.Metadata.ContainsKey("EvidenceCount"));
        
        _output.WriteLine($"Generated SSP:");
        _output.WriteLine($"Document ID: {document.DocumentId}");
        _output.WriteLine($"Title: {document.Title}");
        _output.WriteLine($"Generated: {document.GeneratedDate}");
        _output.WriteLine($"Content length: {document.Content.Length} chars");
        _output.WriteLine($"Evidence Package: {document.Metadata.GetValueOrDefault("EvidencePackageId")}");
        _output.WriteLine($"Evidence Count: {document.Metadata.GetValueOrDefault("EvidenceCount")}");
    }
    
    [Fact(Skip = "Manual test - requires real Azure subscription with AI")]
    public async Task GenerateSSP_WithAI_ReturnsEnhancedDocument()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Semantic Kernel with Azure OpenAI for AI-enhanced SSP
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        var sspParams = new SspParameters
        {
            SystemName = "Azure Government Cloud Platform",
            SystemDescription = "Secure cloud infrastructure for federal government workloads requiring FedRAMP High protection",
            ImpactLevel = "High",
            SystemOwner = "Federal CIO",
            AuthorizingOfficial = "Agency CISO",
            Classification = "FedRAMP High",
            Environment = "Production"
        };

        // Act
        var document = await documentService.GenerateSSPAsync(
            "test-subscription-id", 
            sspParams);

        // Assert - AI-enhanced executive summary
        Assert.NotNull(document);
        Assert.Contains("Executive Summary", document.Content);
        
        // AI-enhanced executive summary should be more than template (longer and more detailed)
        var execSummaryStart = document.Content.IndexOf("## 1. Executive Summary");
        var execSummaryEnd = document.Content.IndexOf("## 2. System Description");
        var execSummaryLength = execSummaryEnd - execSummaryStart;
        
        Assert.True(execSummaryLength > 500, "AI-enhanced executive summary should be detailed (>500 chars)");
        
        _output.WriteLine("=== AI-Enhanced SSP Generated ===");
        _output.WriteLine($"Document ID: {document.DocumentId}");
        _output.WriteLine($"Executive Summary Length: {execSummaryLength} chars");
        _output.WriteLine($"\nExecutive Summary Preview:");
        _output.WriteLine(document.Content.Substring(execSummaryStart, Math.Min(500, execSummaryLength)));
        _output.WriteLine("...");
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task GenerateSAR_WithValidSubscription_ReturnsReport()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var document = await documentService.GenerateSARAsync(
            "test-subscription-id",
            "test-assessment-id");

        // Assert
        Assert.NotNull(document);
        Assert.Equal("SAR", document.DocumentType);
        Assert.NotEmpty(document.Content);
        Assert.Contains("Security Assessment Report", document.Title);
        
        _output.WriteLine($"Generated SAR:");
        _output.WriteLine($"Document ID: {document.DocumentId}");
        _output.WriteLine($"Title: {document.Title}");
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task GeneratePOAM_WithValidSubscription_ReturnsDocument()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var document = await documentService.GeneratePOAMAsync("test-subscription-id");

        // Assert
        Assert.NotNull(document);
        Assert.Equal("POAM", document.DocumentType);
        Assert.NotEmpty(document.Content);
        Assert.Contains("Plan of Action", document.Title);
        
        // Check for finding metadata
        if (document.Metadata.ContainsKey("FindingCount"))
        {
            var findingCount = int.Parse(document.Metadata["FindingCount"]);
            _output.WriteLine($"Found {findingCount} compliance findings in POA&M");
        }
        
        _output.WriteLine($"Generated POA&M:");
        _output.WriteLine($"Document ID: {document.DocumentId}");
        _output.WriteLine($"Title: {document.Title}");
    }
    
    [Fact(Skip = "Manual test - requires real Azure subscription with AI")]
    public async Task GeneratePOAM_WithAI_ReturnsEnhancedDocument()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Semantic Kernel with Azure OpenAI for AI-enhanced POA&M
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var document = await documentService.GeneratePOAMAsync("test-subscription-id");

        // Assert - AI-enhanced findings with risk narratives and milestones
        Assert.NotNull(document);
        Assert.Contains("Plan of Action", document.Title);
        
        // AI-enhanced POA&M should have detailed risk narratives
        Assert.Contains("Risk Description", document.Content);
        Assert.Contains("Milestones", document.Content);
        
        // Check metadata for AI enhancements
        Assert.True(document.Metadata.ContainsKey("FindingCount"));
        
        var findingCount = int.Parse(document.Metadata["FindingCount"]);
        _output.WriteLine("=== AI-Enhanced POA&M Generated ===");
        _output.WriteLine($"Document ID: {document.DocumentId}");
        _output.WriteLine($"Finding Count: {findingCount}");
        _output.WriteLine($"Content Length: {document.Content.Length} chars");
        
        // Output sample finding if present
        if (document.Content.Contains("### Finding"))
        {
            var findingStart = document.Content.IndexOf("### Finding");
            var findingPreview = document.Content.Substring(findingStart, Math.Min(800, document.Content.Length - findingStart));
            _output.WriteLine($"\nSample AI-Enhanced Finding:");
            _output.WriteLine(findingPreview);
        }
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task ExportDocument_AsMarkdown_ReturnsBytes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var bytes = await documentService.ExportDocumentAsync(
            "test-doc-id",
            ComplianceDocumentFormat.Markdown);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        
        var content = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.NotEmpty(content);
        
        _output.WriteLine($"Exported document:");
        _output.WriteLine($"Size: {bytes.Length} bytes");
        _output.WriteLine($"Preview: {content.Substring(0, Math.Min(200, content.Length))}...");
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task ExportDocument_AsHtml_ReturnsBytes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var bytes = await documentService.ExportDocumentAsync(
            "test-doc-id",
            ComplianceDocumentFormat.HTML);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        
        var content = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("</html>", content);
        
        _output.WriteLine($"Exported HTML document:");
        _output.WriteLine($"Size: {bytes.Length} bytes");
    }

    [Fact(Skip = "Manual test - will throw NotImplementedException")]
    public async Task ExportDocument_AsDocx_ThrowsNotImplementedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await documentService.ExportDocumentAsync(
                "test-doc-id",
                ComplianceDocumentFormat.DOCX);
        });
    }

    [Fact(Skip = "Manual test - will throw NotImplementedException")]
    public async Task ExportDocument_AsPdf_ThrowsNotImplementedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await documentService.ExportDocumentAsync(
                "test-doc-id",
                ComplianceDocumentFormat.PDF);
        });
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task FormatDocument_WithNistStandard_ReturnsFormattedDocument()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        var document = new GeneratedDocument
        {
            DocumentId = "test-doc",
            Title = "Test Document",
            Content = "# Test\n\nThis is a test document."
        };

        // Act
        var formattedDoc = await documentService.FormatDocumentAsync(
            document,
            FormattingStandard.NIST);

        // Assert
        Assert.NotNull(formattedDoc);
        Assert.Equal("NIST SP 800-53 Rev 5", formattedDoc.Metadata.GetValueOrDefault("FormattingStandard"));
        
        _output.WriteLine($"Formatted document:");
        _output.WriteLine($"Standard: {formattedDoc.Metadata.GetValueOrDefault("FormattingStandard")}");
    }

    [Fact(Skip = "Manual test - requires real Azure subscription")]
    public async Task ListDocuments_WithPackageId_ReturnsDocuments()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act
        var documents = await documentService.ListDocumentsAsync("test-package-id");

        // Assert
        Assert.NotNull(documents);
        Assert.NotEmpty(documents);
        
        foreach (var doc in documents)
        {
            _output.WriteLine($"Document: {doc.Title} ({doc.DocumentType})");
            _output.WriteLine($"  ID: {doc.DocumentId}");
            _output.WriteLine($"  Status: {doc.Status}");
            _output.WriteLine($"  Version: {doc.Version}");
        }
    }
    
    [Fact(Skip = "Manual test - requires mocked AI service")]
    public async Task GenerateDocument_WithoutAI_UsesTemplateFallback()
    {
        // Arrange - Create service WITHOUT Semantic Kernel (no AI)
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Register services WITHOUT kernel - forcing template fallback
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act - Generate control narrative without AI
        var narrative = await documentService.GenerateControlNarrativeAsync("AC-2", "test-subscription-id");

        // Assert - Should get template-based narrative
        Assert.NotNull(narrative);
        Assert.NotEmpty(narrative.What);
        Assert.NotEmpty(narrative.How);
        
        // Template fallback should NOT have AI-enhanced fields
        Assert.Null(narrative.Evidence);
        Assert.Null(narrative.Gaps);
        Assert.Empty(narrative.ResponsibleParty);
        
        _output.WriteLine("=== Template-Based Control Narrative (No AI) ===");
        _output.WriteLine($"Control: {narrative.ControlId}");
        _output.WriteLine($"What: {narrative.What}");
        _output.WriteLine($"How: {narrative.How}");
        _output.WriteLine($"Evidence: {narrative.Evidence ?? "[None - Template Fallback]"}");
        _output.WriteLine($"Gaps: {narrative.Gaps ?? "[None - Template Fallback]"}");
        _output.WriteLine($"Template fallback working as expected!");
    }
    
    [Fact(Skip = "Manual test - requires mocked AI service with failure")]
    public async Task GenerateDocument_WhenAIFails_FallsBackToTemplate()
    {
        // Arrange - Mock IChatCompletionService to throw exception
        var mockChatCompletion = new Mock<IChatCompletionService>();
        mockChatCompletion
            .Setup(x => x.GetChatMessageContentAsync(
                It.IsAny<ChatHistory>(), 
                It.IsAny<PromptExecutionSettings>(), 
                It.IsAny<Kernel>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Build service with failing AI
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        // Act - Should catch exception and fall back to template
        var narrative = await documentService.GenerateControlNarrativeAsync("AC-2", "test-subscription-id");

        // Assert - Should still return valid narrative (template-based)
        Assert.NotNull(narrative);
        Assert.NotEmpty(narrative.What);
        Assert.NotEmpty(narrative.How);
        
        // AI fields should be null/empty due to graceful degradation
        Assert.Null(narrative.Evidence);
        Assert.Null(narrative.Gaps);
        
        _output.WriteLine("=== Graceful AI Failure - Template Fallback ===");
        _output.WriteLine($"Control: {narrative.ControlId}");
        _output.WriteLine($"What: {narrative.What}");
        _output.WriteLine($"How: {narrative.How}");
        _output.WriteLine($"AI failed gracefully, template fallback successful!");
    }
    
    [Fact(Skip = "Manual test - requires mocked AI service")]
    public async Task GenerateSSP_WhenAIUnavailable_StillGeneratesDocument()
    {
        // Arrange - No AI service configured
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentGenerationService>();

        var sspParams = new SspParameters
        {
            SystemName = "Test System Without AI",
            SystemDescription = "Testing graceful degradation",
            ImpactLevel = "Moderate",
            SystemOwner = "Test Owner",
            AuthorizingOfficial = "Test AO",
            Classification = "UNCLASSIFIED"
        };

        // Act - Should generate SSP with template executive summary
        var document = await documentService.GenerateSSPAsync("test-subscription-id", sspParams);

        // Assert - Document should still be generated
        Assert.NotNull(document);
        Assert.Equal("SSP", document.DocumentType);
        Assert.Contains("Executive Summary", document.Content);
        
        // Template-based executive summary should be shorter than AI-enhanced
        var execSummaryStart = document.Content.IndexOf("## 1. Executive Summary");
        var execSummaryEnd = document.Content.IndexOf("## 2. System Description");
        var execSummaryLength = execSummaryEnd - execSummaryStart;
        
        _output.WriteLine("=== SSP Without AI (Template-Based) ===");
        _output.WriteLine($"Document ID: {document.DocumentId}");
        _output.WriteLine($"Executive Summary Length: {execSummaryLength} chars");
        _output.WriteLine($"Document generated successfully without AI!");
    }
}
