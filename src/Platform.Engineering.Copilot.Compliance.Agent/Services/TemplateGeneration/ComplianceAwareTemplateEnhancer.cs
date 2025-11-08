using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;
using Platform.Engineering.Copilot.Compliance.Agent.Services;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Compliance.Core.Data.Entities;

namespace Platform.Engineering.Copilot.Core.Services.TemplateGeneration;

/// <summary>
/// Enhances template generation with compliance-aware controls for FedRAMP High, DoD IL5, and NIST 800-53
/// Wraps existing DynamicTemplateGeneratorService and injects security/compliance settings
/// </summary>
public class ComplianceAwareTemplateEnhancer : IComplianceAwareTemplateEnhancer
{
    private readonly ILogger<ComplianceAwareTemplateEnhancer> _logger;
    private readonly IDynamicTemplateGenerator _templateGenerator;
    private readonly IAzurePolicyService _policyService;
    private readonly INistControlsService _nistService;

    // Compliance framework mappings
    private static readonly Dictionary<string, List<string>> ComplianceFrameworkControls = new()
    {
        ["FedRAMP-High"] = new() { "AC-3", "AC-6", "AU-2", "AU-3", "SC-7", "SC-8", "SC-13", "SC-28", "IA-2", "IA-5" },
        ["DoD-IL5"] = new() { "AC-3", "AC-6", "AU-2", "AU-3", "SC-7", "SC-8", "SC-13", "SC-28", "IA-2", "IA-5", "PE-3", "PE-6" },
        ["NIST-800-53"] = new() { "AC-1", "AC-2", "AC-3", "AC-6", "AU-2", "AU-3", "SC-7", "SC-8", "SC-13", "SC-28" },
        ["SOC2"] = new() { "CC6.1", "CC6.6", "CC6.7", "CC7.2" }, // SOC2 Trust Services Criteria
        ["GDPR"] = new() { "Art-32", "Art-33", "Art-34" } // GDPR Articles
    };

    public ComplianceAwareTemplateEnhancer(
        ILogger<ComplianceAwareTemplateEnhancer> logger,
        IDynamicTemplateGenerator templateGenerator,
        IAzurePolicyService policyService,
        INistControlsService nistService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateGenerator = templateGenerator ?? throw new ArgumentNullException(nameof(templateGenerator));
        _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
        _nistService = nistService ?? throw new ArgumentNullException(nameof(nistService));
    }

    public async Task<TemplateGenerationResult> EnhanceWithComplianceAsync(
        TemplateGenerationRequest request,
        string complianceFramework = "FedRAMP-High",
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        
        if (string.IsNullOrWhiteSpace(complianceFramework))
            throw new ArgumentException("Compliance framework cannot be null or empty", nameof(complianceFramework));

        _logger.LogInformation("Enhancing template for {ServiceName} with {Framework} compliance", 
            request.ServiceName, complianceFramework);

        try
        {
            // Step 1: Get required NIST controls for this compliance framework
            var requiredControls = await GetRequiredControlsAsync(complianceFramework, cancellationToken);
            _logger.LogInformation("Retrieved {Count} required controls for {Framework}", 
                requiredControls.Count, complianceFramework);

            // Step 2: Inject compliance-aware security settings into request
            InjectComplianceSettings(request, complianceFramework, requiredControls);

            // Step 3: Generate enhanced template using existing template generator
            var result = await _templateGenerator.GenerateTemplateAsync(request, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Template generation failed: {Error}", result.ErrorMessage);
                return result;
            }

            // Step 4: Validate generated template meets compliance requirements
            // Get the first generated file content for validation (usually the main template)
            var templateContent = result.Files.Values.FirstOrDefault() ?? string.Empty;
            var validationResult = await ValidateComplianceAsync(
                templateContent, 
                complianceFramework, 
                cancellationToken);

            // Step 5: Add compliance metadata to summary
            var complianceSummary = $"\n\n📋 Compliance Framework: {complianceFramework}\n" +
                                   $"📌 Required Controls: {string.Join(", ", requiredControls.Select(c => c.Id ?? "Unknown"))}\n" +
                                   $"✅ Validation: {(validationResult.IsValid ? "PASSED" : "FAILED")}\n" +
                                   $"🔍 Findings: {validationResult.Findings.Count}";

            result.Summary += complianceSummary;

            if (!validationResult.IsValid)
            {
                result.Summary += $"\n\n⚠️ Compliance validation found {validationResult.Findings.Count} issues:\n";
                foreach (var finding in validationResult.Findings.Take(5))
                {
                    result.Summary += $"  - {finding.ControlId}: {finding.Description}\n";
                }
            }

            _logger.LogInformation("Template enhanced with {Framework} compliance. Validation: {Status}", 
                complianceFramework, validationResult.IsValid ? "PASSED" : "FAILED");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing template with compliance controls");
            return new TemplateGenerationResult
            {
                Success = false,
                ErrorMessage = $"Compliance enhancement failed: {ex.Message}"
            };
        }
    }

    public async Task<ComplianceValidationResult> ValidateComplianceAsync(
        string templateContent,
        string complianceFramework,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating template compliance for {Framework}", complianceFramework);

        var result = new ComplianceValidationResult
        {
            Framework = complianceFramework,
            IsValid = true
        };

        try
        {
            var requiredControls = await GetRequiredControlsAsync(complianceFramework, cancellationToken);

            // Validate encryption at rest (SC-28)
            if (requiredControls.Any(c => c.Id == "SC-28"))
            {
                if (!templateContent.Contains("encryption") && !templateContent.Contains("Encryption"))
                {
                    result.Findings.Add(new ValidationFinding
                    {
                        ControlId = "SC-28",
                        Severity = "High",
                        Description = "Encryption at rest not configured",
                        Remediation = "Enable encryption for all data stores (Storage, SQL, Cosmos, etc.)"
                    });
                    result.IsValid = false;
                }
            }

            // Validate encryption in transit (SC-8)
            if (requiredControls.Any(c => c.Id == "SC-8"))
            {
                if (!templateContent.Contains("https") && !templateContent.Contains("tls") && !templateContent.Contains("ssl"))
                {
                    result.Findings.Add(new ValidationFinding
                    {
                        ControlId = "SC-8",
                        Severity = "High",
                        Description = "Encryption in transit not enforced",
                        Remediation = "Enable HTTPS/TLS for all network communications"
                    });
                    result.IsValid = false;
                }
            }

            // Validate audit logging (AU-2, AU-3)
            if (requiredControls.Any(c => c.Id == "AU-2" || c.Id == "AU-3"))
            {
                if (!templateContent.Contains("diagnosticSettings") && 
                    !templateContent.Contains("logAnalytics") && 
                    !templateContent.Contains("logging"))
                {
                    result.Findings.Add(new ValidationFinding
                    {
                        ControlId = "AU-2",
                        Severity = "Medium",
                        Description = "Audit logging not configured",
                        Remediation = "Enable diagnostic settings and Log Analytics workspace"
                    });
                    result.IsValid = false;
                }
            }

            // Validate network security (SC-7)
            if (requiredControls.Any(c => c.Id == "SC-7"))
            {
                if (!templateContent.Contains("networkSecurityGroup") && 
                    !templateContent.Contains("nsg") && 
                    !templateContent.Contains("firewall"))
                {
                    result.Findings.Add(new ValidationFinding
                    {
                        ControlId = "SC-7",
                        Severity = "High",
                        Description = "Network boundary protection not configured",
                        Remediation = "Configure NSG rules and Azure Firewall"
                    });
                    result.IsValid = false;
                }
            }

            // Validate access control (AC-3, AC-6)
            if (requiredControls.Any(c => c.Id == "AC-3" || c.Id == "AC-6"))
            {
                if (!templateContent.Contains("identity") && 
                    !templateContent.Contains("rbac") && 
                    !templateContent.Contains("managedIdentity"))
                {
                    result.Findings.Add(new ValidationFinding
                    {
                        ControlId = "AC-3",
                        Severity = "Medium",
                        Description = "Access control mechanisms not configured",
                        Remediation = "Enable Managed Identity and RBAC assignments"
                    });
                    result.IsValid = false;
                }
            }

            _logger.LogInformation("Compliance validation complete: {Status}, {FindingCount} findings", 
                result.IsValid ? "PASSED" : "FAILED", result.Findings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating compliance");
            return new ComplianceValidationResult
            {
                Framework = complianceFramework,
                IsValid = false,
                Findings = new List<ValidationFinding>
                {
                    new ValidationFinding
                    {
                        ControlId = "VALIDATION-ERROR",
                        Severity = "Critical",
                        Description = $"Compliance validation failed: {ex.Message}",
                        Remediation = "Review template and retry validation"
                    }
                }
            };
        }
    }

    private async Task<List<NistControl>> GetRequiredControlsAsync(
        string complianceFramework,
        CancellationToken cancellationToken)
    {
        var controls = new List<NistControl>();

        if (!ComplianceFrameworkControls.ContainsKey(complianceFramework))
        {
            _logger.LogWarning("Unknown compliance framework: {Framework}, using FedRAMP-High defaults", complianceFramework);
            complianceFramework = "FedRAMP-High";
        }

        var controlIds = ComplianceFrameworkControls[complianceFramework];

        foreach (var controlId in controlIds)
        {
            try
            {
                // For NIST controls, use the NIST service
                if (controlId.Contains("-") && char.IsLetter(controlId[0]))
                {
                    var control = await _nistService.GetControlAsync(controlId, cancellationToken);
                    if (control != null)
                    {
                        controls.Add(control);
                    }
                    else
                    {
                        _logger.LogWarning("NIST control {ControlId} not found", controlId);
                    }
                }
                else
                {
                    // For other frameworks (SOC2, GDPR), create placeholder controls
                    controls.Add(new NistControl
                    {
                        Id = controlId,
                        Title = $"{complianceFramework} Control {controlId}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve control {ControlId}", controlId);
            }
        }

        return controls;
    }

    private void InjectComplianceSettings(
        TemplateGenerationRequest request,
        string complianceFramework,
        List<NistControl> requiredControls)
    {
        _logger.LogDebug("Injecting compliance settings for {Framework}", complianceFramework);

        // Initialize security spec if not present
        request.Security ??= new SecuritySpec();

        // Apply FedRAMP High / DoD IL5 / NIST 800-53 requirements
        if (complianceFramework.Contains("FedRAMP") || 
            complianceFramework.Contains("DoD") || 
            complianceFramework.Contains("NIST"))
        {
            // Security settings using actual SecuritySpec properties
            request.Security.NetworkPolicies = true;
            request.Security.PodSecurityPolicies = true;
            request.Security.ServiceAccount = true;
            request.Security.RBAC = true;
            request.Security.TLS = true;
            request.Security.SecretsManagement = true;
            
            // Add compliance standards
            request.Security.ComplianceStandards = new List<string> { complianceFramework };
        }

        // Apply infrastructure-level compliance settings
        if (request.Infrastructure != null)
        {
            // Enable private cluster for AKS (AC-3, SC-7)
            request.Infrastructure.EnablePrivateCluster = true;
            request.Infrastructure.EnableWorkloadIdentity = true;

            // Enable Azure Policy for governance (CM-2, CM-6)
            request.Infrastructure.EnableAzurePolicy = true;

            // Disk encryption (SC-28)
            if (string.IsNullOrEmpty(request.Infrastructure.DiskEncryptionSetId))
            {
                request.Infrastructure.DiskEncryptionSetId = "customer-managed-key"; // Placeholder
            }

            // App Service security (SC-8, SC-28)
            request.Infrastructure.HttpsOnly = true;
            request.Infrastructure.MinTlsVersion = "1.2";
            request.Infrastructure.EnableVnetIntegration = true;
            request.Infrastructure.EnableManagedIdentity = true;

            // Network configuration
            if (request.Infrastructure.NetworkConfig != null)
            {
                request.Infrastructure.NetworkConfig.EnableDDoSProtection = true;
                request.Infrastructure.NetworkConfig.EnablePrivateEndpoint = true;
            }

            // Resource tags for compliance tracking
            request.Infrastructure.Tags ??= new Dictionary<string, string>();
            request.Infrastructure.Tags["Compliance"] = complianceFramework; // Simplified compliance tag
            request.Infrastructure.Tags["ComplianceFramework"] = complianceFramework;
            request.Infrastructure.Tags["RequiresCompliance"] = "true";
            request.Infrastructure.Tags["DataClassification"] = "Sensitive"; // Default to sensitive
        }

        // Apply observability settings for compliance (AU-2, AU-3, AU-12)
        if (request.Observability != null)
        {
            request.Observability.Prometheus = true;
            request.Observability.StructuredLogging = true;
            request.Observability.ApplicationInsights = request.Infrastructure?.Provider == CloudProvider.Azure;
            request.Observability.DistributedTracing = true;
        }

        _logger.LogInformation("Injected compliance settings: TLS={TLS}, RBAC={RBAC}, NetworkPolicies={Network}, SecretsManagement={Secrets}",
            request.Security.TLS,
            request.Security.RBAC,
            request.Security.NetworkPolicies,
            request.Security.SecretsManagement);
    }
}
