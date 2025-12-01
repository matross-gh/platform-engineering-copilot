using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services.Compliance;

/// <summary>
/// Service for loading and providing NIST control remediation steps for Azure resources.
/// Loads remediation data from JSON configuration file.
/// </summary>
public class NistRemediationStepsService : INistRemediationStepsService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<NistRemediationStepsService> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    private const string REMEDIATION_CACHE_KEY = "nist_remediation_steps";
    private const string REMEDIATION_FILENAME = "nist-azure-remediation-steps.json";
    private const int CACHE_DURATION_HOURS = 24;

    private Dictionary<string, RemediationStepsDefinition>? _remediationSteps;

    public NistRemediationStepsService(
        IMemoryCache cache,
        ILogger<NistRemediationStepsService> logger,
        IHostEnvironment hostEnvironment)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    }

    public async Task<RemediationStepsDefinition?> GetRemediationStepsAsync(string controlId)
    {
        if (string.IsNullOrWhiteSpace(controlId))
        {
            throw new ArgumentException("Control ID cannot be null or empty", nameof(controlId));
        }

        await EnsureRemediationDataLoadedAsync();

        var normalizedId = controlId.ToUpperInvariant();
        return _remediationSteps?.GetValueOrDefault(normalizedId);
    }

    public async Task<IReadOnlyList<RemediationAction>> GetAvailableActionsAsync(string controlId, string resourceType = "")
    {
        var steps = await GetRemediationStepsAsync(controlId);
        if (steps == null)
        {
            return Array.Empty<RemediationAction>();
        }

        // Filter by resource type if specified
        if (!string.IsNullOrWhiteSpace(resourceType) && steps.ApplicableResourceTypes.Any())
        {
            var isApplicable = steps.ApplicableResourceTypes.Contains("*") ||
                             steps.ApplicableResourceTypes.Contains(resourceType, StringComparer.OrdinalIgnoreCase);

            if (!isApplicable)
            {
                return Array.Empty<RemediationAction>();
            }
        }

        return steps.AvailableActions;
    }

    public async Task<bool> SupportsAutomationAsync(string controlId)
    {
        var steps = await GetRemediationStepsAsync(controlId);
        return steps?.IsAutomated ?? false;
    }

    public async Task<IReadOnlyList<string>> GetAutomatedControlsAsync()
    {
        await EnsureRemediationDataLoadedAsync();

        if (_remediationSteps == null)
        {
            return Array.Empty<string>();
        }

        return _remediationSteps
            .Where(kvp => kvp.Value.IsAutomated)
            .Select(kvp => kvp.Key)
            .ToList()
            .AsReadOnly();
    }

    private async Task EnsureRemediationDataLoadedAsync()
    {
        // Check memory cache first
        if (_cache.TryGetValue(REMEDIATION_CACHE_KEY, out Dictionary<string, RemediationStepsDefinition>? cached))
        {
            _remediationSteps = cached;
            return;
        }

        // Check if already loaded in instance
        if (_remediationSteps != null)
        {
            return;
        }

        // Load from file
        await LoadRemediationDataAsync();
    }

    private async Task LoadRemediationDataAsync()
    {
        try
        {
            var remediationPath = Path.Combine(_hostEnvironment.ContentRootPath, "Data", REMEDIATION_FILENAME);

            if (!File.Exists(remediationPath))
            {
                _logger.LogWarning("Remediation data file not found: {Path}. No remediation steps will be available.", remediationPath);
                _remediationSteps = new Dictionary<string, RemediationStepsDefinition>();
                return;
            }

            _logger.LogInformation("Loading NIST remediation steps from {Path}", remediationPath);

            var jsonContent = await File.ReadAllTextAsync(remediationPath);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var root = JsonSerializer.Deserialize<RemediationDataRoot>(jsonContent, options);

            if (root?.Controls == null)
            {
                _logger.LogWarning("Remediation data file contains no controls");
                _remediationSteps = new Dictionary<string, RemediationStepsDefinition>();
                return;
            }

            // Convert to dictionary with uppercase keys for case-insensitive lookup
            _remediationSteps = root.Controls.ToDictionary(
                kvp => kvp.Key.ToUpperInvariant(),
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

            // Cache for 24 hours
            _cache.Set(REMEDIATION_CACHE_KEY, _remediationSteps,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CACHE_DURATION_HOURS),
                    Priority = CacheItemPriority.High
                });

            _logger.LogInformation("Loaded {Count} NIST control remediation definitions (Version: {Version}, Platform: {Platform})",
                _remediationSteps.Count, root.Version ?? "Unknown", root.Platform ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load NIST remediation data");
            _remediationSteps = new Dictionary<string, RemediationStepsDefinition>();
        }
    }

    /// <summary>
    /// Root object for JSON deserialization
    /// </summary>
    private class RemediationDataRoot
    {
        public string? Version { get; set; }
        public string? Framework { get; set; }
        public string? Platform { get; set; }
        public string? LastUpdated { get; set; }
        public Dictionary<string, RemediationStepsDefinition> Controls { get; set; } = new();
    }
}
