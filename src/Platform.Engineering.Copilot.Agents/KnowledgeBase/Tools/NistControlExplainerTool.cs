using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for explaining NIST 800-53 controls without running assessments.
/// Provides educational/informational content about control requirements.
/// </summary>
public class NistControlExplainerTool : BaseTool
{
    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly INistControlsService _nistControlsService;
    private readonly KnowledgeBaseAgentOptions _options;

    public override string Name => "explain_nist_control";

    public override string Description =>
        "📚 KNOWLEDGE/DEFINITION ONLY - Explain what a NIST 800-53 control IS or MEANS. " +
        "Returns the control definition, requirements, and Azure implementation guidance WITHOUT scanning. " +
        "Use for questions like: 'What is AC-2?', 'Explain SC-28', 'Define IA-2', 'What does CM-6 require?'";

    public NistControlExplainerTool(
        ILogger<NistControlExplainerTool> logger,
        KnowledgeBaseStateAccessors stateAccessors,
        INistControlsService nistControlsService,
        IOptions<KnowledgeBaseAgentOptions> options) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _nistControlsService = nistControlsService ?? throw new ArgumentNullException(nameof(nistControlsService));
        _options = options?.Value ?? new KnowledgeBaseAgentOptions();

        Parameters.Add(new ToolParameter(
            name: "control_id",
            description: "NIST 800-53 control ID (e.g., 'AC-2', 'IA-2', 'SC-28', 'CM-6'). Include enhancement number if asking about a specific enhancement (e.g., 'IA-2(1)', 'AC-2(4)').",
            required: true));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var controlId = GetRequiredString(arguments, "control_id");
        var normalizedId = controlId.Trim().ToUpperInvariant();

        Logger.LogInformation("Explaining NIST 800-53 control {ControlId}", normalizedId);

        try
        {
            // Check cache first
            var cached = _stateAccessors.GetCachedControl(normalizedId);
            if (cached != null)
            {
                Logger.LogDebug("Returning cached explanation for {ControlId}", normalizedId);
                return cached.Explanation;
            }

            // Get the control from the NIST catalog
            var control = await _nistControlsService.GetControlAsync(normalizedId, cancellationToken);

            if (control == null)
            {
                // Try without the enhancement part if it's an enhancement
                if (normalizedId.Contains('('))
                {
                    var baseControlId = normalizedId.Split('(')[0];
                    control = await _nistControlsService.GetControlAsync(baseControlId, cancellationToken);

                    if (control != null)
                    {
                        var enhancementResult = $@"# 📚 NIST 800-53 Control: {normalizedId}

**Note:** This is an enhancement of base control {baseControlId}.

## Base Control: {control.Title}

{GetControlStatement(control)}

{GetControlGuidance(control)}

{GetAzureImplementationGuidance(normalizedId)}

---
*This is informational only. To check your compliance status, ask: 'Run a compliance assessment'*";

                        _stateAccessors.CacheControl(normalizedId, enhancementResult,
                            TimeSpan.FromMinutes(_options.CacheDurationMinutes));

                        return enhancementResult;
                    }
                }

                return ToJson(new
                {
                    success = false,
                    error = $"Control '{controlId}' was not found in the NIST 800-53 catalog.",
                    suggestions = new[]
                    {
                        "Check the control ID format (e.g., AC-2, IA-2(1), SC-28)",
                        "Use 'search_nist_controls' to find related controls",
                        "Common control families: AC (Access Control), AU (Audit), IA (Identification), SC (System Protection), CM (Configuration)"
                    }
                });
            }

            // Build the explanation response
            var response = $@"# 📚 NIST 800-53 Control: {control.Id}

## {control.Title}

{GetControlStatement(control)}

{GetControlGuidance(control)}

{GetAzureImplementationGuidance(control.Id ?? "")}

{GetRelatedControls(control)}

---
*This is informational only. To check your compliance status, ask: 'Run a compliance assessment'*";

            // Cache the result
            _stateAccessors.CacheControl(normalizedId, response,
                TimeSpan.FromMinutes(_options.CacheDurationMinutes));

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error explaining NIST control {ControlId}", controlId);
            return ToJson(new
            {
                success = false,
                error = $"Error retrieving information for control {controlId}. Please try again or check the control ID format."
            });
        }
    }

    private static string GetControlStatement(Core.Models.Compliance.NistControl control)
    {
        var statement = control.Parts?
            .FirstOrDefault(p => p.Name?.Equals("statement", StringComparison.OrdinalIgnoreCase) == true);

        if (statement?.Prose != null)
        {
            return $"### Control Statement\n\n{statement.Prose}";
        }

        if (statement?.Parts != null && statement.Parts.Any())
        {
            var parts = statement.Parts.Select(p => $"- {p.Prose}").ToList();
            return $"### Control Statement\n\n{string.Join("\n", parts)}";
        }

        return "### Control Statement\n\nNo detailed statement available.";
    }

    private static string GetControlGuidance(Core.Models.Compliance.NistControl control)
    {
        var guidance = control.Parts?
            .FirstOrDefault(p => p.Name?.Equals("guidance", StringComparison.OrdinalIgnoreCase) == true);

        if (guidance?.Prose != null)
        {
            return $"### Supplemental Guidance\n\n{guidance.Prose}";
        }

        return "";
    }

    private static string GetAzureImplementationGuidance(string controlId)
    {
        var family = controlId.Split('-')[0].ToUpperInvariant();

        var guidance = family switch
        {
            "AC" => "Use Azure RBAC, Entra ID Conditional Access, and PIM for access control.",
            "AU" => "Configure Azure Monitor, Log Analytics, and audit logging policies.",
            "IA" => "Implement Entra ID with MFA, managed identities, and certificate-based auth.",
            "SC" => "Use Azure Firewall, NSGs, Private Link, and Key Vault for encryption.",
            "CM" => "Use Azure Policy, Guest Configuration, and Azure Automation for configuration.",
            "SI" => "Enable Microsoft Defender for Cloud, Azure Update Manager, and threat detection.",
            "CA" => "Implement security assessments, penetration testing, and continuous monitoring.",
            "RA" => "Use Microsoft Defender for Cloud recommendations and vulnerability assessments.",
            "PE" => "Azure datacenters provide physical protection; document in SSP as inherited.",
            "PL" => "Document security architecture and system boundaries in your SSP.",
            "PS" => "Implement personnel security through Entra ID and access reviews.",
            "SA" => "Use secure development practices and supply chain security controls.",
            _ => "Implement using appropriate Azure services and document in your SSP."
        };

        return $"### Azure Implementation Guidance\n\n{guidance}";
    }

    private static string GetRelatedControls(Core.Models.Compliance.NistControl control)
    {
        // Related controls are tracked through control family and enhancements
        // The control ID format AC-2, AC-2(1), etc. indicates related controls
        var controlId = control.Id ?? "";
        if (string.IsNullOrEmpty(controlId))
        {
            return "";
        }

        // Get the base control family
        var parts = controlId.Split('-');
        if (parts.Length < 2)
        {
            return "";
        }

        var family = parts[0];
        var baseNumber = parts[1].Split('(')[0];

        return $"### Related Controls\n\nOther controls in the {family} (Access Control) family may be related. " +
               $"Use 'search_nist_controls' with family='{family}' to explore related controls.";
    }
}
