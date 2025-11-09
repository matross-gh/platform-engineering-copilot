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
/// Service for RMF process and documentation guidance
/// </summary>
public class RmfKnowledgeService : IRmfKnowledgeService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RmfKnowledgeService> _logger;
    private const string CACHE_KEY = "rmf_process_data";
    private const string RMF_FILE = "KnowledgeBase/rmf-process.json";

    public RmfKnowledgeService(IMemoryCache cache, ILogger<RmfKnowledgeService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RmfProcess?> GetRmfStepAsync(string step, CancellationToken cancellationToken = default)
    {
        var data = await LoadRmfDataAsync(cancellationToken);
        return data?.RmfSteps?.FirstOrDefault(s => s.Step == step);
    }

    public async Task<IReadOnlyList<RmfProcess>> GetAllRmfStepsAsync(CancellationToken cancellationToken = default)
    {
        var data = await LoadRmfDataAsync(cancellationToken);
        return data?.RmfSteps?.AsReadOnly() ?? Array.Empty<RmfProcess>().ToList().AsReadOnly();
    }

    public async Task<string> ExplainRmfProcessAsync(string? specificStep = null, CancellationToken cancellationToken = default)
    {
        var data = await LoadRmfDataAsync(cancellationToken);
        
        if (data == null)
            return "RMF process data not available.";

        if (string.IsNullOrWhiteSpace(specificStep))
        {
            // Explain entire RMF process
            return $@"# Risk Management Framework (RMF)

{data.RmfOverview.Description}

**Purpose:** {data.RmfOverview.Purpose}

**Applicability:** {data.RmfOverview.Applicability}

## RMF Steps

{string.Join("\n\n", data.RmfSteps.Select(s => $@"### Step {s.Step}: {s.Title}
{s.Description}

**Key Activities:**
{string.Join("\n", s.Activities.Select(a => $"- {a}"))}

**Deliverables:**
{string.Join("\n", s.Outputs.Select(o => $"- {o}"))}

**Responsible Roles:** {string.Join(", ", s.Roles)}
**Reference:** {s.DodInstruction}"))}

## Key Principles
{string.Join("\n", data.RmfOverview.KeyPrinciples.Select(p => $"- {p}"))}
";
        }
        else
        {
            // Explain specific step
            var step = await GetRmfStepAsync(specificStep, cancellationToken);
            if (step == null)
                return $"RMF Step {specificStep} not found. Valid steps are 1-6.";

            return $@"# RMF Step {step.Step}: {step.Title}

{step.Description}

## Activities

{string.Join("\n", step.Activities.Select((a, i) => $"{i + 1}. {a}"))}

## Key Deliverables

{string.Join("\n", step.Outputs.Select(o => $"- {o}"))}

## Responsible Roles

{string.Join("\n", step.Roles.Select(r => $"- {r}"))}

## DoD Guidance

{step.DodInstruction}

## Next Steps

{(int.Parse(step.Step) < 6 
    ? $"After completing Step {step.Step}, proceed to Step {int.Parse(step.Step) + 1}." 
    : "Step 6 (Monitor) is an ongoing activity throughout the system lifecycle.")}
";
        }
    }

    public async Task<List<string>> GetRmfOutputsForStepAsync(string step, CancellationToken cancellationToken = default)
    {
        var rmfStep = await GetRmfStepAsync(step, cancellationToken);
        return rmfStep?.Outputs ?? new List<string>();
    }

    private async Task<RmfData?> LoadRmfDataAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CACHE_KEY, out RmfData? cachedData))
            return cachedData;

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, RMF_FILE);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("RMF process file not found: {FilePath}", filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var data = JsonSerializer.Deserialize<RmfData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _cache.Set(CACHE_KEY, data, TimeSpan.FromHours(24));
            _logger.LogInformation("Loaded RMF process data with {StepCount} steps", data?.RmfSteps?.Count ?? 0);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RMF process data");
            return null;
        }
    }

    private class RmfData
    {
        public List<RmfProcess> RmfSteps { get; set; } = new();
        public RmfOverview RmfOverview { get; set; } = new();
    }

    private class RmfOverview
    {
        public string Description { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Applicability { get; set; } = string.Empty;
        public List<string> KeyPrinciples { get; set; } = new();
    }
}
