using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Platform.Engineering.Copilot.Core.Services;

/// <summary>
/// Service for managing persistent configuration in ~/.platform-copilot/config.json
/// Provides subscription context persistence across GitHub Copilot sessions
/// </summary>
public class ConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;
        
        // Use ~/.platform-copilot/ for config storage (cross-platform)
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDirectory = Path.Combine(homeDirectory, ".platform-copilot");
        _configFilePath = Path.Combine(_configDirectory, "config.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        EnsureConfigDirectoryExists();
    }

    /// <summary>
    /// Gets the default Azure subscription ID from config file
    /// </summary>
    public string? GetDefaultSubscription()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogDebug("Config file does not exist: {ConfigPath}", _configFilePath);
                return null;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<PlatformConfig>(json, _jsonOptions);
            
            if (!string.IsNullOrWhiteSpace(config?.DefaultSubscription))
            {
                _logger.LogInformation("Loaded default subscription from config: {SubscriptionId}", 
                    config.DefaultSubscription);
                return config.DefaultSubscription;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read default subscription from config file");
            return null;
        }
    }

    /// <summary>
    /// Sets the default Azure subscription ID in config file
    /// </summary>
    public void SetDefaultSubscription(string subscriptionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentException("Subscription ID cannot be null or empty", nameof(subscriptionId));
            }

            // Load existing config or create new
            var config = LoadConfig() ?? new PlatformConfig();
            config.DefaultSubscription = subscriptionId;
            config.LastUpdated = DateTime.UtcNow;

            // Write to file
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configFilePath, json);

            _logger.LogInformation("Saved default subscription to config: {SubscriptionId}", subscriptionId);
            _logger.LogDebug("Config file updated: {ConfigPath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save default subscription to config file");
            throw;
        }
    }

    /// <summary>
    /// Sets the Azure tenant ID in config file
    /// </summary>
    public void SetTenantId(string tenantId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
            }

            // Load existing config or create new
            var config = LoadConfig() ?? new PlatformConfig();
            config.TenantId = tenantId;
            config.LastUpdated = DateTime.UtcNow;

            // Write to file
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configFilePath, json);

            _logger.LogInformation("Saved tenant ID to config: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save tenant ID to config file");
            throw;
        }
    }

    /// <summary>
    /// Sets the authentication method in config file
    /// </summary>
    public void SetAuthenticationMethod(string authenticationMethod)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(authenticationMethod))
            {
                throw new ArgumentException("Authentication method cannot be null or empty", nameof(authenticationMethod));
            }

            // Load existing config or create new
            var config = LoadConfig() ?? new PlatformConfig();
            config.AuthenticationMethod = authenticationMethod.ToLower();
            config.LastUpdated = DateTime.UtcNow;

            // Write to file
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configFilePath, json);

            _logger.LogInformation("Saved authentication method to config: {Method}", authenticationMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save authentication method to config file");
            throw;
        }
    }

    /// <summary>
    /// Clears the default subscription from config
    /// </summary>
    public void ClearDefaultSubscription()
    {
        try
        {
            var config = LoadConfig();
            if (config != null)
            {
                config.DefaultSubscription = null;
                config.LastUpdated = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(_configFilePath, json);

                _logger.LogInformation("Cleared default subscription from config");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear default subscription from config file");
        }
    }

    /// <summary>
    /// Gets the full config object
    /// </summary>
    public PlatformConfig? GetConfig()
    {
        return LoadConfig();
    }

    /// <summary>
    /// Gets the config file path for user reference
    /// </summary>
    public string GetConfigFilePath() => _configFilePath;

    private PlatformConfig? LoadConfig()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(_configFilePath);
            return JsonSerializer.Deserialize<PlatformConfig>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load config file");
            return null;
        }
    }

    private void EnsureConfigDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
                _logger.LogInformation("Created config directory: {ConfigDirectory}", _configDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create config directory: {ConfigDirectory}", _configDirectory);
        }
    }
}

/// <summary>
/// Platform Engineering Copilot configuration model
/// </summary>
public class PlatformConfig
{
    /// <summary>
    /// Default Azure subscription ID to use for operations
    /// </summary>
    public string? DefaultSubscription { get; set; }

    /// <summary>
    /// Last time config was updated
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Optional: User-friendly name for the subscription
    /// </summary>
    public string? SubscriptionName { get; set; }

    /// <summary>
    /// Optional: Azure environment (AzureCloud, AzureUSGovernment, etc.)
    /// </summary>
    public string? AzureEnvironment { get; set; }

    /// <summary>
    /// Optional: Azure tenant ID for authentication
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Optional: Authentication method (credential, key, connectionString)
    /// </summary>
    public string? AuthenticationMethod { get; set; }
}
