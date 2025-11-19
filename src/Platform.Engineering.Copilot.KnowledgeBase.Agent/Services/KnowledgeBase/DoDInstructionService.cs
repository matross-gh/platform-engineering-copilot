using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;
using Platform.Engineering.Copilot.Core.Models.KnowledgeBase;

namespace Platform.Engineering.Copilot.KnowledgeBase.Agent.Services.KnowledgeBase;

/// <summary>
/// Service for DoD instructions and policy guidance
/// </summary>
public class DoDInstructionService : IDoDInstructionService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DoDInstructionService> _logger;
    private const string CACHE_KEY = "dod_instructions_data";
    private const string INSTRUCTION_FILE = "KnowledgeBase/dod-instructions.json";

    public DoDInstructionService(IMemoryCache cache, ILogger<DoDInstructionService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DoDInstruction?> GetInstructionAsync(string instructionId, CancellationToken cancellationToken = default)
    {
        var data = await LoadInstructionDataAsync(cancellationToken);
        return data?.DoDInstructions?.FirstOrDefault(i => 
            i.InstructionId.Equals(instructionId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<DoDInstruction>> GetInstructionsByControlAsync(string nistControlId, CancellationToken cancellationToken = default)
    {
        var data = await LoadInstructionDataAsync(cancellationToken);
        return data?.DoDInstructions?
            .Where(i => i.RelatedNistControls.Any(c => c.Contains(nistControlId, StringComparison.OrdinalIgnoreCase)))
            .ToList()
            .AsReadOnly() ?? Array.Empty<DoDInstruction>().ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<DoDInstruction>> SearchInstructionsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var data = await LoadInstructionDataAsync(cancellationToken);
        
        if (data?.DoDInstructions == null)
            return Array.Empty<DoDInstruction>().ToList().AsReadOnly();

        return data.DoDInstructions
            .Where(i => 
                i.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                i.InstructionId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public async Task<string> ExplainInstructionAsync(string instructionId, CancellationToken cancellationToken = default)
    {
        var instruction = await GetInstructionAsync(instructionId, cancellationToken);
        
        if (instruction == null)
            return $"DoD Instruction {instructionId} not found in knowledge base.";

        return $@"# {instruction.InstructionId}: {instruction.Title}

## Description

{instruction.Description}

**Publication Date:** {instruction.PublicationDate:yyyy-MM-dd}

**Applicability:** {instruction.Applicability}

## Related NIST 800-53 Controls

{string.Join(", ", instruction.RelatedNistControls)}

{(instruction.RelatedStigIds.Any() 
    ? $@"## Related STIGs

{string.Join(", ", instruction.RelatedStigIds)}"
    : "")}

## Reference

{instruction.Url}

## Implementation Guidance

This instruction provides authoritative policy for DoD organizations. Compliance is mandatory for all systems within scope.

For specific implementation guidance, consult your organization's security team and the full instruction document.
";
    }

    private async Task<InstructionData?> LoadInstructionDataAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CACHE_KEY, out InstructionData? cachedData))
            return cachedData;

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, INSTRUCTION_FILE);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("DoD instructions file not found: {FilePath}", filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var data = JsonSerializer.Deserialize<InstructionData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _cache.Set(CACHE_KEY, data, TimeSpan.FromHours(24));
            _logger.LogInformation("Loaded DoD instruction data with {InstructionCount} instructions", 
                data?.DoDInstructions?.Count ?? 0);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DoD instruction data");
            return null;
        }
    }

    private class InstructionData
    {
        public List<DoDInstruction> DoDInstructions { get; set; } = new();
        public List<ImpactLevel> ImpactLevels { get; set; } = new();
        public List<BoundaryProtectionRequirement> BoundaryProtection { get; set; } = new();
    }
}
