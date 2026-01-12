using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for explaining STIG (Security Technical Implementation Guide) controls.
/// Provides educational content about STIG requirements and Azure implementation.
/// </summary>
public class StigExplainerTool : BaseTool
{
    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly IStigKnowledgeService _stigService;
    private readonly KnowledgeBaseAgentOptions _options;

    public override string Name => "explain_stig";

    public override string Description =>
        "Explain what a STIG (Security Technical Implementation Guide) control is and how to implement it. " +
        "Provides the control description, check procedure, remediation steps, and Azure implementation guidance. " +
        "Use for questions like: 'What is STIG V-12345?', 'Explain Windows Server STIG requirements'.";

    public StigExplainerTool(
        ILogger<StigExplainerTool> logger,
        KnowledgeBaseStateAccessors stateAccessors,
        IStigKnowledgeService stigService,
        IOptions<KnowledgeBaseAgentOptions> options) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _stigService = stigService ?? throw new ArgumentNullException(nameof(stigService));
        _options = options?.Value ?? new KnowledgeBaseAgentOptions();

        Parameters.Add(new ToolParameter(
            name: "stig_id",
            description: "STIG control ID (e.g., 'V-12345') or vulnerability ID",
            required: true));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var stigId = GetRequiredString(arguments, "stig_id");
        var normalizedId = stigId.Trim().ToUpperInvariant();

        Logger.LogInformation("Explaining STIG control {StigId}", normalizedId);

        try
        {
            // Check cache first
            var cached = _stateAccessors.GetCachedStig(normalizedId);
            if (cached != null)
            {
                Logger.LogDebug("Returning cached explanation for STIG {StigId}", normalizedId);
                return cached.Explanation;
            }

            // Get the STIG control
            var stig = await _stigService.GetStigControlAsync(normalizedId, cancellationToken);

            if (stig == null)
            {
                return ToJson(new
                {
                    success = false,
                    error = $"STIG {stigId} not found in knowledge base.",
                    suggestions = new[]
                    {
                        "Check the STIG ID format (e.g., V-12345)",
                        "Use 'search_stigs' to find related STIG controls",
                        "Try searching by the vulnerability number or rule ID"
                    }
                });
            }

            var response = $@"# {stig.StigId}: {stig.Title}

**Severity:** {stig.Severity}
**Category:** {stig.Category}
**STIG Family:** {stig.StigFamily}

## Description

{stig.Description}

## NIST 800-53 Controls

{string.Join(", ", stig.NistControls)}

## CCI References

{string.Join(", ", stig.CciRefs)}

## Check Procedure

{stig.CheckText}

## Remediation

{stig.FixText}

## Azure Implementation

**Service:** {stig.AzureImplementation.GetValueOrDefault("service", "N/A")}
**Configuration:** {stig.AzureImplementation.GetValueOrDefault("configuration", "N/A")}
**Azure Policy:** {stig.AzureImplementation.GetValueOrDefault("azurePolicy", "N/A")}

### Automation Command

```bash
{stig.AzureImplementation.GetValueOrDefault("automation", "Manual configuration required")}
```

## Compliance Mapping

- **Rule ID:** {stig.RuleId}
- **Vulnerability ID:** {stig.VulnId}

---
*This is informational only. To check your compliance status, ask: 'Run a compliance assessment'*";

            // Cache the result
            _stateAccessors.CacheStig(normalizedId, response,
                TimeSpan.FromMinutes(_options.CacheDurationMinutes));

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error explaining STIG {StigId}", stigId);
            return ToJson(new
            {
                success = false,
                error = $"Error retrieving information for STIG {stigId}: {ex.Message}"
            });
        }
    }
}
