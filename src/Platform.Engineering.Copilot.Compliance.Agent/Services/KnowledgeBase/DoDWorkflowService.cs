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
/// Service for Navy/DoD workflow documentation
/// </summary>
public class DoDWorkflowService : IDoDWorkflowService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DoDWorkflowService> _logger;
    private const string CACHE_KEY = "dod_workflows_data";
    private const string WORKFLOW_FILE = "KnowledgeBase/navy-workflows.json";

    public DoDWorkflowService(IMemoryCache cache, ILogger<DoDWorkflowService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DoDWorkflow?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var data = await LoadWorkflowDataAsync(cancellationToken);
        return data?.Workflows?.FirstOrDefault(w => 
            w.WorkflowId.Equals(workflowId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<DoDWorkflow>> GetWorkflowsByOrganizationAsync(DoDOrganization organization, CancellationToken cancellationToken = default)
    {
        var data = await LoadWorkflowDataAsync(cancellationToken);
        return data?.Workflows?
            .Where(w => w.Organization == organization)
            .ToList()
            .AsReadOnly() ?? Array.Empty<DoDWorkflow>().ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<DoDWorkflow>> GetWorkflowsByImpactLevelAsync(string impactLevel, CancellationToken cancellationToken = default)
    {
        var data = await LoadWorkflowDataAsync(cancellationToken);
        return data?.Workflows?
            .Where(w => w.ImpactLevel.Contains(impactLevel, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly() ?? Array.Empty<DoDWorkflow>().ToList().AsReadOnly();
    }

    public async Task<string> ExplainWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowAsync(workflowId, cancellationToken);
        
        if (workflow == null)
            return $"Workflow {workflowId} not found in knowledge base.";

        return $@"# {workflow.Name}

**Organization:** {workflow.Organization}
**Impact Level:** {workflow.ImpactLevel}

## Description

{workflow.Description}

## Workflow Steps

{string.Join("\n\n", workflow.Steps.Select(s => $@"### Step {s.StepNumber}: {s.Title}

{s.Description}

**Responsibilities:**
{string.Join("\n", s.Responsibilities.Select(r => $"- {r}"))}

**Deliverables:**
{string.Join("\n", s.Deliverables.Select(d => $"- {d}"))}

**Estimated Duration:** {s.EstimatedDuration}

{(s.Prerequisites.Any() ? $"**Prerequisites:** {string.Join(", ", s.Prerequisites)}" : "")}"))}

## Required Documents

{string.Join("\n", workflow.RequiredDocuments.Select(d => $"- {d}"))}

## Approval Authorities

{string.Join("\n", workflow.ApprovalAuthorities.Select(a => $"- {a}"))}

## Total Timeline

Estimated total duration: {CalculateTotalDuration(workflow.Steps)}
";
    }

    private string CalculateTotalDuration(List<WorkflowStep> steps)
    {
        // Simple aggregation - in reality would parse duration strings
        var hasOngoing = steps.Any(s => s.EstimatedDuration.Contains("Ongoing", StringComparison.OrdinalIgnoreCase));
        
        if (hasOngoing)
            return "Variable (includes ongoing monitoring phase)";
        
        return "20-60 weeks for initial ATO (typical)";
    }

    private async Task<WorkflowData?> LoadWorkflowDataAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CACHE_KEY, out WorkflowData? cachedData))
            return cachedData;

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, WORKFLOW_FILE);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Navy workflows file not found: {FilePath}", filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var data = JsonSerializer.Deserialize<WorkflowData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            _cache.Set(CACHE_KEY, data, TimeSpan.FromHours(24));
            _logger.LogInformation("Loaded workflow data with {WorkflowCount} workflows", data?.Workflows?.Count ?? 0);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflow data");
            return null;
        }
    }

    private class WorkflowData
    {
        public List<DoDWorkflow> Workflows { get; set; } = new();
        public Dictionary<string, OrganizationInfo> Organizations { get; set; } = new();
    }

    private class OrganizationInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Examples { get; set; } = new();
        public string Contact { get; set; } = string.Empty;
        public List<string> Responsibilities { get; set; } = new();
        public string? CurrentName { get; set; }
    }
}
