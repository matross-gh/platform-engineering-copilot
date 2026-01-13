using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.Compliance.Configuration;
using Platform.Engineering.Copilot.Agents.Compliance.State;
using Platform.Engineering.Copilot.Core.Interfaces.Compliance;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Agents.Compliance.Tools;

/// <summary>
/// Tool for collecting and packaging compliance evidence artifacts for ATO/eMASS documentation.
/// This is for evidence collection ONLY - NOT for scanning or finding vulnerabilities.
/// </summary>
public class EvidenceCollectionTool : BaseTool
{
    private readonly ComplianceStateAccessors _stateAccessors;
    private readonly ComplianceAgentOptions _options;
    private readonly IEvidenceStorageService _evidenceStorage;
    private readonly IEnumerable<IEvidenceCollector> _evidenceCollectors;

    public override string Name => "collect_evidence";

    public override string Description =>
        "📦 EVIDENCE COLLECTION ONLY - Collect and package compliance evidence artifacts for ATO/eMASS documentation (NOT for scanning). " +
        "THIS FUNCTION GATHERS DOCUMENTATION - it does NOT scan for security findings. " +
        "Use ONLY when user says: 'collect evidence', 'generate evidence', 'evidence package', 'gather evidence', 'ATO package', 'documentation'. " +
        "DO NOT use for: 'run assessment', 'scan', 'check compliance', 'find vulnerabilities'. " +
        "Output: Evidence package with configuration data, logs, metrics, policies - suitable for ATO attestation and audits. " +
        "Requires NIST control family (AC, AU, CM, etc.). Can scope to specific resource group. " +
        "Accepts subscription GUID or friendly name (e.g., 'production', 'dev', 'staging'). " +
        "RBAC: Requires Compliance.Administrator, Compliance.Auditor, or Compliance.Analyst role.";

    public EvidenceCollectionTool(
        ILogger<EvidenceCollectionTool> logger,
        ComplianceStateAccessors stateAccessors,
        IOptions<ComplianceAgentOptions> options,
        IEvidenceStorageService evidenceStorage,
        IEnumerable<IEvidenceCollector> evidenceCollectors) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _options = options?.Value ?? new ComplianceAgentOptions();
        _evidenceStorage = evidenceStorage ?? throw new ArgumentNullException(nameof(evidenceStorage));
        _evidenceCollectors = evidenceCollectors ?? throw new ArgumentNullException(nameof(evidenceCollectors));

        Parameters.Add(new ToolParameter("control_family", 
            "NIST control family code: AC (Access Control), AU (Audit), CM (Configuration Management), " +
            "CP (Contingency Planning), IA (Identification/Authentication), IR (Incident Response), " +
            "RA (Risk Assessment), SC (System Communications), SI (System Integrity), CA (Security Assessment)", true));
        Parameters.Add(new ToolParameter("subscription_id", 
            "Azure subscription ID (GUID) or friendly name (e.g., 'production', 'dev', 'staging'). " +
            "OPTIONAL - if not provided, uses the default subscription from configuration.", false));
        Parameters.Add(new ToolParameter("resource_group_name", 
            "Optional resource group name to limit scope", false));
        Parameters.Add(new ToolParameter("evidence_types", 
            "Comma-separated list of evidence types to collect: configuration, logs, metrics, policies, all. Default: all", false));
        Parameters.Add(new ToolParameter("include_raw_data", 
            "Include raw configuration exports in package. Default: false", false));
        Parameters.Add(new ToolParameter("conversation_id", "Conversation ID for state tracking", false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.EnableEvidenceCollection)
            {
                return ToJson(new { success = false, error = "Evidence collection is disabled in configuration" });
            }

            var controlFamily = GetOptionalString(arguments, "control_family")?.ToUpper()
                ?? throw new ArgumentException("control_family is required");
            var subscriptionIdOrName = GetOptionalString(arguments, "subscription_id");
            var resourceGroupName = GetOptionalString(arguments, "resource_group_name");
            var evidenceTypesStr = GetOptionalString(arguments, "evidence_types") ?? "all";
            var includeRawData = GetOptionalBool(arguments, "include_raw_data", false);
            var conversationId = GetOptionalString(arguments, "conversation_id") ?? Guid.NewGuid().ToString();

            // Resolve subscription
            var subscriptionId = await ResolveSubscriptionIdAsync(subscriptionIdOrName);
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return ToJson(new
                {
                    success = false,
                    error = "Subscription ID is required. Either provide subscription_id parameter or set default using configure_subscription tool."
                });
            }

            // Validate control family
            var validFamilies = new[] { "AC", "AU", "CM", "CP", "IA", "IR", "RA", "SC", "SI", "CA", "MP" };
            if (!validFamilies.Contains(controlFamily))
            {
                return ToJson(new
                {
                    success = false,
                    error = $"Invalid control family '{controlFamily}'. Valid options: {string.Join(", ", validFamilies)}"
                });
            }

            var scope = string.IsNullOrWhiteSpace(resourceGroupName)
                ? $"subscription {subscriptionId}"
                : $"resource group '{resourceGroupName}' in subscription {subscriptionId}";

            Logger.LogInformation("📦 Collecting evidence for {ControlFamily} control family in {Scope}", 
                controlFamily, scope);

            // Check for cached evidence package
            var cached = _stateAccessors.GetCachedEvidence(conversationId, controlFamily);
            if (cached != null)
            {
                return ToJson(new
                {
                    success = true,
                    fromCache = true,
                    evidence = cached,
                    message = $"Retrieved cached evidence package from {cached.CollectedAt:g}. Use fresh=true to collect new evidence."
                });
            }

            // Parse evidence types
            var evidenceTypes = ParseEvidenceTypes(evidenceTypesStr);

            // Collect evidence using real collectors
            var collectedEvidence = new List<ComplianceEvidence>();
            var collectionErrors = new List<string>();
            var collectedBy = "EvidenceCollectionTool";

            foreach (var collector in _evidenceCollectors)
            {
                try
                {
                    if (evidenceTypes.Contains("configuration") || evidenceTypes.Contains("all"))
                    {
                        var configEvidence = await collector.CollectConfigurationEvidenceAsync(
                            subscriptionId, controlFamily, collectedBy, cancellationToken);
                        collectedEvidence.AddRange(configEvidence);
                    }

                    if (evidenceTypes.Contains("logs") || evidenceTypes.Contains("all"))
                    {
                        var logEvidence = await collector.CollectLogEvidenceAsync(
                            subscriptionId, controlFamily, collectedBy, cancellationToken);
                        collectedEvidence.AddRange(logEvidence);
                    }

                    if (evidenceTypes.Contains("metrics") || evidenceTypes.Contains("all"))
                    {
                        var metricEvidence = await collector.CollectMetricEvidenceAsync(
                            subscriptionId, controlFamily, collectedBy, cancellationToken);
                        collectedEvidence.AddRange(metricEvidence);
                    }

                    if (evidenceTypes.Contains("policies") || evidenceTypes.Contains("all"))
                    {
                        var policyEvidence = await collector.CollectPolicyEvidenceAsync(
                            subscriptionId, controlFamily, collectedBy, cancellationToken);
                        collectedEvidence.AddRange(policyEvidence);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Evidence collector {Collector} encountered an error", collector.GetType().Name);
                    collectionErrors.Add($"{collector.GetType().Name}: {ex.Message}");
                }
            }

            // Create evidence package
            var packageId = Guid.NewGuid().ToString();
            var collectionStart = DateTime.UtcNow;
            var evidencePackage = new EvidencePackage
            {
                PackageId = packageId,
                SubscriptionId = subscriptionId,
                ControlFamily = controlFamily,
                CollectionDate = DateTimeOffset.UtcNow,
                CollectionStartTime = collectionStart,
                CollectionEndTime = DateTimeOffset.UtcNow,
                CollectedBy = collectedBy,
                Evidence = collectedEvidence,
                TotalItems = collectedEvidence.Count,
                Summary = $"Evidence package for {controlFamily} control family containing {collectedEvidence.Count} items",
                CompletenessScore = collectedEvidence.Count > 0 ? 0.75 : 0.0
            };

            // Store evidence package
            string? storageUri = null;
            try
            {
                storageUri = await _evidenceStorage.StoreComplianceEvidencePackageAsync(evidencePackage, cancellationToken);
                Logger.LogInformation("📦 Evidence package stored at: {StorageUri}", storageUri);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to store evidence package to blob storage");
                collectionErrors.Add($"Storage: {ex.Message}");
            }

            // Cache the evidence
            var cacheItem = new EvidenceItem
            {
                EvidenceId = packageId,
                ControlId = controlFamily,
                Type = "package",
                Title = $"{controlFamily} Evidence Package",
                Description = $"Evidence package for {controlFamily} control family",
                CollectedAt = DateTime.UtcNow,
                StorageUri = storageUri,
                Metadata = new Dictionary<string, object>
                {
                    ["controlFamily"] = controlFamily,
                    ["subscriptionId"] = subscriptionId,
                    ["itemCount"] = collectedEvidence.Count,
                    ["evidenceTypes"] = evidenceTypes
                }
            };
            _stateAccessors.CacheEvidence(conversationId, controlFamily, cacheItem);

            // Group evidence by type for summary
            var byType = collectedEvidence.GroupBy(e => e.EvidenceType)
                .ToDictionary(g => g.Key, g => g.Count());

            return ToJson(new
            {
                success = true,
                packageId,
                controlFamily,
                subscriptionId,
                scope,
                collectedAt = DateTime.UtcNow,
                summary = new
                {
                    totalItems = collectedEvidence.Count,
                    byType,
                    evidenceTypesRequested = evidenceTypes
                },
                evidence = collectedEvidence.Select(e => new
                {
                    id = e.EvidenceId,
                    type = e.EvidenceType,
                    controlId = e.ControlId,
                    resourceId = e.ResourceId,
                    collectedAt = e.CollectedAt,
                    hasData = e.Data.Count > 0 || !string.IsNullOrEmpty(e.ConfigSnapshot)
                }).Take(50).ToList(), // Limit to first 50 items in response
                storageUri,
                errors = collectionErrors.Count > 0 ? collectionErrors : null,
                message = $"📦 Successfully collected {collectedEvidence.Count} evidence items for {controlFamily} control family. " +
                    (storageUri != null ? $"Package stored at: {storageUri}" : "Package not stored (storage unavailable).")
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error collecting evidence");
            return ToJson(new { success = false, error = ex.Message });
        }
    }

    private async Task<string?> ResolveSubscriptionIdAsync(string? subscriptionIdOrName)
    {
        if (string.IsNullOrWhiteSpace(subscriptionIdOrName))
        {
            // Try to get from persistent configuration
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".platform-copilot", "config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    var configJson = await File.ReadAllTextAsync(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(configJson);
                    if (config?.TryGetValue("subscription_id", out var savedId) == true)
                    {
                        Logger.LogInformation("Using subscription from persistent config: {SubscriptionId}", savedId);
                        return savedId;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to read config file");
                }
            }
            return null;
        }

        // Check if it's already a GUID
        if (Guid.TryParse(subscriptionIdOrName, out _))
        {
            return subscriptionIdOrName;
        }

        // Return as-is for friendly names (would be resolved from config in production)
        return subscriptionIdOrName;
    }

    private static List<string> ParseEvidenceTypes(string evidenceTypesStr)
    {
        var types = evidenceTypesStr.ToLower()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var validTypes = new[] { "configuration", "logs", "metrics", "policies", "all" };
        return types.Where(t => validTypes.Contains(t)).ToList();
    }
}
