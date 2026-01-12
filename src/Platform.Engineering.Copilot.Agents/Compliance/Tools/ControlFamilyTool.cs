using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Agents.Compliance.State;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for getting detailed findings and recommendations for NIST 800-53 control families.
/// </summary>
public class ControlFamilyTool : BaseTool
{
    private readonly ComplianceStateAccessors _stateAccessors;
    private readonly ComplianceAgentOptions _options;
    private readonly IAtoComplianceEngine _complianceEngine;
    private readonly IRemediationEngine _remediationEngine;

    public override string Name => "get_control_family_details";

    public override string Description =>
        "Get detailed findings and recommendations for a specific NIST control family. " +
        "Shows all findings, their severity, remediation guidance, and whether they can be auto-remediated. " +
        "Essential for drilling down into specific control families to understand and address issues. " +
        "Control families: AC (Access Control), AU (Audit), SC (System Communications), " +
        "SI (System Integrity), CM (Configuration Management), CP (Contingency Planning), " +
        "IA (Identification/Authentication), IR (Incident Response), RA (Risk Assessment), CA (Security Assessment).";

    public ControlFamilyTool(
        ILogger<ControlFamilyTool> logger,
        ComplianceStateAccessors stateAccessors,
        IOptions<ComplianceAgentOptions> options,
        IAtoComplianceEngine complianceEngine,
        IRemediationEngine remediationEngine) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new ComplianceAgentOptions();
        _complianceEngine = complianceEngine ?? throw new ArgumentNullException(nameof(complianceEngine));
        _remediationEngine = remediationEngine ?? throw new ArgumentNullException(nameof(remediationEngine));

        Parameters.Add(new ToolParameter("family", "Control family code (e.g., AC, AU, SC, IA, CM, CP, SI, IR, RA, CA)", true));
        Parameters.Add(new ToolParameter("subscription_id", "Azure subscription ID to get findings for", false));
        Parameters.Add(new ToolParameter("control_id", "Specific control ID (e.g., AC-2, SC-7). Optional.", false));
        Parameters.Add(new ToolParameter("include_enhancements", "Include control enhancements. Default: false", false));
        Parameters.Add(new ToolParameter("severity_filter", "Filter findings by severity: Critical, High, Medium, Low. Optional.", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = GetOptionalString(arguments, "family")?.ToUpper()
                ?? throw new ArgumentException("family is required");
            var subscriptionId = GetOptionalString(arguments, "subscription_id");
            var controlId = GetOptionalString(arguments, "control_id")?.ToUpper();
            var includeEnhancements = GetOptionalBool(arguments, "include_enhancements", false);
            var severityFilter = GetOptionalString(arguments, "severity_filter");

            Logger.LogInformation("Getting control family details: {Family}, Subscription: {SubscriptionId}", family, subscriptionId);

            // Get static control family information
            var familyDetails = GetControlFamilyDetails(family);
            if (familyDetails == null)
            {
                return ToJson(new { success = false, error = $"Unknown control family: {family}" });
            }

            // If specific control requested, get its details
            object? controlDetails = null;
            if (!string.IsNullOrEmpty(controlId))
            {
                controlDetails = GetControlDetails(controlId, includeEnhancements);
            }

            // Get actual findings for this control family if subscription provided
            List<object>? findingsForFamily = null;
            object? summary = null;

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                var findings = await _complianceEngine.GetUnresolvedFindingsAsync(subscriptionId, cancellationToken);
                
                // Filter to this control family
                var familyFindings = findings.Where(f => 
                    f.AffectedNistControls.Any(c => c.StartsWith(family + "-", StringComparison.OrdinalIgnoreCase) || 
                                                     c.Equals(family, StringComparison.OrdinalIgnoreCase))).ToList();

                // Apply specific control filter if provided
                if (!string.IsNullOrEmpty(controlId))
                {
                    familyFindings = familyFindings.Where(f =>
                        f.AffectedNistControls.Any(c => c.Equals(controlId, StringComparison.OrdinalIgnoreCase))).ToList();
                }

                // Apply severity filter if provided
                if (!string.IsNullOrEmpty(severityFilter) && Enum.TryParse<AtoFindingSeverity>(severityFilter, true, out var severity))
                {
                    familyFindings = familyFindings.Where(f => f.Severity == severity).ToList();
                }

                // Generate remediation guidance for findings
                findingsForFamily = new List<object>();
                foreach (var finding in familyFindings.OrderByDescending(f => f.Severity))
                {
                    RemediationGuidance? guidance = null;
                    try
                    {
                        guidance = await _remediationEngine.GetRemediationGuidanceAsync(finding, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Failed to get remediation guidance for finding {FindingId}", finding.Id);
                    }

                    // Get complexity from first remediation action if available
                    var complexity = finding.RemediationActions.FirstOrDefault()?.Complexity.ToString() ?? "Unknown";

                    findingsForFamily.Add(new
                    {
                        findingId = finding.Id,
                        title = finding.Title,
                        description = finding.Description,
                        severity = finding.Severity.ToString(),
                        resourceId = finding.ResourceId,
                        resourceName = finding.ResourceName,
                        resourceType = finding.ResourceType,
                        controls = finding.AffectedNistControls,
                        isAutoRemediable = finding.IsAutoRemediable,
                        remediationComplexity = complexity,
                        recommendation = finding.Recommendation,
                        remediationGuidance = guidance != null ? new
                        {
                            explanation = guidance.Explanation,
                            confidence = guidance.Confidence,
                            hasTechnicalPlan = guidance.TechnicalPlan != null
                        } : null,
                        // Include inline remediation guidance from finding if no AI guidance
                        inlineGuidance = string.IsNullOrEmpty(finding.RemediationGuidance) ? null : finding.RemediationGuidance
                    });
                }

                // Generate summary statistics
                summary = new
                {
                    totalFindings = familyFindings.Count,
                    bySeverity = new
                    {
                        critical = familyFindings.Count(f => f.Severity == AtoFindingSeverity.Critical),
                        high = familyFindings.Count(f => f.Severity == AtoFindingSeverity.High),
                        medium = familyFindings.Count(f => f.Severity == AtoFindingSeverity.Medium),
                        low = familyFindings.Count(f => f.Severity == AtoFindingSeverity.Low),
                        informational = familyFindings.Count(f => f.Severity == AtoFindingSeverity.Informational)
                    },
                    autoRemediableCount = familyFindings.Count(f => f.IsAutoRemediable),
                    manualRemediationCount = familyFindings.Count(f => !f.IsAutoRemediable),
                    affectedResources = familyFindings.Select(f => f.ResourceId).Distinct().Count(),
                    uniqueControls = familyFindings.SelectMany(f => f.AffectedNistControls)
                        .Where(c => c.StartsWith(family + "-", StringComparison.OrdinalIgnoreCase))
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList()
                };
            }

            // Cache for future use
            _stateAccessors.CacheControlFamily(family, familyDetails);

            return ToJson(new
            {
                success = true,
                controlFamily = familyDetails,
                control = controlDetails,
                findings = findingsForFamily,
                summary,
                message = findingsForFamily != null
                    ? $"Found {findingsForFamily.Count} findings for {family} control family"
                    : $"Control family {family} details retrieved. Provide subscription_id to see actual findings."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting control family details");
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private ControlFamilyDetails? GetControlFamilyDetails(string family)
    {
        var families = new Dictionary<string, ControlFamilyDetails>
        {
            ["AC"] = new ControlFamilyDetails
            {
                Family = "AC",
                Name = "Access Control",
                Description = "Controls for managing access to systems and data, including user account management, least privilege, and session controls.",
                TotalControls = 25,
                ControlIds = new List<string> { "AC-1", "AC-2", "AC-3", "AC-4", "AC-5", "AC-6", "AC-7", "AC-8", "AC-11", "AC-12", "AC-14", "AC-17", "AC-18", "AC-19", "AC-20", "AC-21", "AC-22" }
            },
            ["AU"] = new ControlFamilyDetails
            {
                Family = "AU",
                Name = "Audit and Accountability",
                Description = "Controls for audit log generation, protection, and review to ensure accountability and traceability.",
                TotalControls = 16,
                ControlIds = new List<string> { "AU-1", "AU-2", "AU-3", "AU-4", "AU-5", "AU-6", "AU-7", "AU-8", "AU-9", "AU-10", "AU-11", "AU-12" }
            },
            ["SC"] = new ControlFamilyDetails
            {
                Family = "SC",
                Name = "System and Communications Protection",
                Description = "Controls for protecting system boundaries, communications, and cryptographic protections.",
                TotalControls = 44,
                ControlIds = new List<string> { "SC-1", "SC-2", "SC-4", "SC-5", "SC-7", "SC-8", "SC-10", "SC-12", "SC-13", "SC-15", "SC-17", "SC-18", "SC-20", "SC-21", "SC-22", "SC-23", "SC-28", "SC-39" }
            },
            ["IA"] = new ControlFamilyDetails
            {
                Family = "IA",
                Name = "Identification and Authentication",
                Description = "Controls for identifying and authenticating users, devices, and processes.",
                TotalControls = 12,
                ControlIds = new List<string> { "IA-1", "IA-2", "IA-3", "IA-4", "IA-5", "IA-6", "IA-7", "IA-8", "IA-11" }
            },
            ["CM"] = new ControlFamilyDetails
            {
                Family = "CM",
                Name = "Configuration Management",
                Description = "Controls for establishing and maintaining secure configuration baselines.",
                TotalControls = 14,
                ControlIds = new List<string> { "CM-1", "CM-2", "CM-3", "CM-4", "CM-5", "CM-6", "CM-7", "CM-8", "CM-9", "CM-10", "CM-11" }
            },
            ["RA"] = new ControlFamilyDetails
            {
                Family = "RA",
                Name = "Risk Assessment",
                Description = "Controls for identifying and assessing risks to organizational operations and assets.",
                TotalControls = 7,
                ControlIds = new List<string> { "RA-1", "RA-2", "RA-3", "RA-5", "RA-7" }
            },
            ["SI"] = new ControlFamilyDetails
            {
                Family = "SI",
                Name = "System and Information Integrity",
                Description = "Controls for detecting, reporting, and responding to security incidents and flaws.",
                TotalControls = 20,
                ControlIds = new List<string> { "SI-1", "SI-2", "SI-3", "SI-4", "SI-5", "SI-6", "SI-7", "SI-8", "SI-10", "SI-11", "SI-12", "SI-16" }
            },
            ["CA"] = new ControlFamilyDetails
            {
                Family = "CA",
                Name = "Assessment, Authorization, and Monitoring",
                Description = "Controls for security assessments, authorizations, and continuous monitoring.",
                TotalControls = 9,
                ControlIds = new List<string> { "CA-1", "CA-2", "CA-3", "CA-5", "CA-6", "CA-7", "CA-8", "CA-9" }
            },
            ["CP"] = new ControlFamilyDetails
            {
                Family = "CP",
                Name = "Contingency Planning",
                Description = "Controls for business continuity and disaster recovery planning.",
                TotalControls = 13,
                ControlIds = new List<string> { "CP-1", "CP-2", "CP-3", "CP-4", "CP-6", "CP-7", "CP-8", "CP-9", "CP-10" }
            },
            ["MP"] = new ControlFamilyDetails
            {
                Family = "MP",
                Name = "Media Protection",
                Description = "Controls for protecting system media during storage and transport.",
                TotalControls = 8,
                ControlIds = new List<string> { "MP-1", "MP-2", "MP-3", "MP-4", "MP-5", "MP-6", "MP-7" }
            },
            ["IR"] = new ControlFamilyDetails
            {
                Family = "IR",
                Name = "Incident Response",
                Description = "Controls for establishing incident response capabilities, including preparation, detection, analysis, containment, eradication, and recovery.",
                TotalControls = 10,
                ControlIds = new List<string> { "IR-1", "IR-2", "IR-3", "IR-4", "IR-5", "IR-6", "IR-7", "IR-8", "IR-9", "IR-10" }
            }
        };

        return families.GetValueOrDefault(family);
    }

    private object? GetControlDetails(string controlId, bool includeEnhancements)
    {
        var controls = new Dictionary<string, object>
        {
            ["AC-2"] = new
            {
                controlId = "AC-2",
                family = "AC",
                title = "Account Management",
                priority = "P1",
                baseline = new[] { "Low", "Moderate", "High" },
                description = "Manage system accounts, including establishing, activating, modifying, disabling, and removing accounts.",
                azureImplementation = new[]
                {
                    "Use Azure AD for centralized identity management",
                    "Implement PIM for privileged access",
                    "Configure automated account provisioning/deprovisioning",
                    "Enable access reviews for periodic account validation"
                },
                enhancements = includeEnhancements ? new[]
                {
                    "AC-2(1): Automated System Account Management",
                    "AC-2(2): Automated Temporary and Emergency Account Management",
                    "AC-2(3): Disable Accounts",
                    "AC-2(4): Automated Audit Actions"
                } : null
            },
            ["SC-7"] = new
            {
                controlId = "SC-7",
                family = "SC",
                title = "Boundary Protection",
                priority = "P1",
                baseline = new[] { "Low", "Moderate", "High" },
                description = "Monitor and control communications at external and key internal boundaries.",
                azureImplementation = new[]
                {
                    "Deploy Azure Firewall or NVAs at network boundaries",
                    "Implement NSGs for subnet-level segmentation",
                    "Use Private Link for PaaS service access",
                    "Enable DDoS Protection Standard"
                }
            },
            ["AU-2"] = new
            {
                controlId = "AU-2",
                family = "AU",
                title = "Event Logging",
                priority = "P1",
                baseline = new[] { "Low", "Moderate", "High" },
                description = "Identify events that need to be logged and the frequency of logging.",
                azureImplementation = new[]
                {
                    "Enable Azure Activity Log for management plane events",
                    "Configure Diagnostic Settings for resource-level logging",
                    "Use Log Analytics for centralized log collection",
                    "Enable Microsoft Sentinel for security event correlation"
                }
            }
        };

        return controls.GetValueOrDefault(controlId);
    }
}
