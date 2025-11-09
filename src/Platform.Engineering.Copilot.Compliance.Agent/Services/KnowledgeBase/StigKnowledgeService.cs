using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;
using Platform.Engineering.Copilot.Core.Models.KnowledgeBase;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services.KnowledgeBase;

/// <summary>
/// Service for STIG controls and compliance checking
/// </summary>
public class StigKnowledgeService : IStigKnowledgeService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<StigKnowledgeService> _logger;
    private const string CACHE_KEY = "stig_controls_data";
    private const string STIG_FILE = "KnowledgeBase/stig-controls.json";

    public StigKnowledgeService(IMemoryCache cache, ILogger<StigKnowledgeService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StigControl?> GetStigControlAsync(string stigId, CancellationToken cancellationToken = default)
    {
        var data = await LoadStigDataAsync(cancellationToken);
        return data?.StigControls?.FirstOrDefault(s => 
            s.StigId.Equals(stigId, StringComparison.OrdinalIgnoreCase) ||
            s.VulnId.Equals(stigId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<StigControl>> GetStigsByNistControlAsync(string nistControlId, CancellationToken cancellationToken = default)
    {
        var data = await LoadStigDataAsync(cancellationToken);
        return data?.StigControls?
            .Where(s => s.NistControls.Any(n => n.Equals(nistControlId, StringComparison.OrdinalIgnoreCase)))
            .ToList()
            .AsReadOnly() ?? Array.Empty<StigControl>().ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<StigControl>> SearchStigsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var data = await LoadStigDataAsync(cancellationToken);
        
        if (data?.StigControls == null)
            return Array.Empty<StigControl>().ToList().AsReadOnly();

        return data.StigControls
            .Where(s => 
                s.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                s.StigId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                s.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<StigControl>> GetStigsBySeverityAsync(StigSeverity severity, CancellationToken cancellationToken = default)
    {
        var data = await LoadStigDataAsync(cancellationToken);
        return data?.StigControls?
            .Where(s => s.Severity == severity)
            .ToList()
            .AsReadOnly() ?? Array.Empty<StigControl>().ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<StigControl>> GetAllStigsAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadStigDataAsync(cancellationToken);
        return data?.StigControls?.AsReadOnly() ?? Array.Empty<StigControl>().ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<StigControl>> GetStigsByServiceTypeAsync(StigServiceType serviceType, CancellationToken cancellationToken = default)
    {
        var data = await LoadStigDataAsync(cancellationToken);
        return data?.StigControls?
            .Where(s => s.ServiceType == serviceType)
            .ToList()
            .AsReadOnly() ?? Array.Empty<StigControl>().ToList().AsReadOnly();
    }

    public async Task<ControlMapping?> GetControlMappingAsync(string nistControlId, CancellationToken cancellationToken = default)
    {
        var data = await LoadStigDataAsync(cancellationToken);
        return data?.ControlMappings?.FirstOrDefault(m => 
            m.NistControlId.Equals(nistControlId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> ExplainStigAsync(string stigId, CancellationToken cancellationToken = default)
    {
        var stig = await GetStigControlAsync(stigId, cancellationToken);
        
        if (stig == null)
            return $"STIG {stigId} not found in knowledge base.";

        return $@"# {stig.StigId}: {stig.Title}

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
- **Vuln ID:** {stig.VulnId}
- **STIG ID:** {stig.StigId}
";
    }

    private async Task<StigData?> LoadStigDataAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CACHE_KEY, out StigData? cachedData))
            return cachedData;

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, STIG_FILE);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("STIG controls file not found: {FilePath}", filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var data = JsonSerializer.Deserialize<StigData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            _cache.Set(CACHE_KEY, data, TimeSpan.FromHours(24));
            _logger.LogInformation("Loaded STIG data with {ControlCount} controls", data?.StigControls?.Count ?? 0);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading STIG data");
            return null;
        }
    }

    private class StigData
    {
        public List<StigControl> StigControls { get; set; } = new();
        public List<StigFamily> StigFamilies { get; set; } = new();
        public List<ControlMapping> ControlMappings { get; set; } = new();
    }

    private class StigFamily
    {
        public string Family { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
