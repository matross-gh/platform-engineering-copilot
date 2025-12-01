# Compliance Agent Configuration Implementation

## Overview

The Compliance Agent now fully implements all configuration settings from `appsettings.json`, including the nested DefenderForCloud, CodeScanning, Evidence, and NistControls sections.

## What Was Implemented

### 1. Root-Level Configuration Properties

Added to `ComplianceAgentOptions` class:

```csharp
public class ComplianceAgentOptions
{
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 6000;
    public bool EnableAutomatedRemediation { get; set; } = true;
    public string DefaultFramework { get; set; } = "NIST80053";
    public string DefaultBaseline { get; set; } = "FedRAMPHigh";
    
    // Nested sections
    public DefenderForCloudOptions DefenderForCloud { get; set; } = new();
    public CodeScanningOptions CodeScanning { get; set; } = new();
    public EvidenceOptions Evidence { get; set; } = new();
    public NistControlsOptions NistControls { get; set; } = new();
}
```

**Key Changes:**
- Moved `Temperature` and `MaxTokens` from nested `AzureOpenAI` to root level
- Added `EnableAutomatedRemediation` flag
- Added `DefaultFramework` and `DefaultBaseline` properties

### 2. DefenderForCloudOptions Enhancement

Added missing properties:

```csharp
public class DefenderForCloudOptions
{
    public bool Enabled { get; set; } = false;
    public bool IncludeSecureScore { get; set; } = true;
    public bool MapToNistControls { get; set; } = true;
    public int CacheDurationMinutes { get; set; } = 60;
    public bool EnableDeduplication { get; set; } = true;
    public string? SubscriptionId { get; set; }        // NEW
    public string? WorkspaceId { get; set; }           // NEW
}
```

### 3. CodeScanningOptions (New Class)

Created complete code scanning configuration:

```csharp
public class CodeScanningOptions
{
    public bool EnableSecretsDetection { get; set; } = true;
    public bool EnableDependencyScanning { get; set; } = true;
    public bool EnableStigChecks { get; set; } = true;
    public List<string> SecretPatterns { get; set; } = new()
    {
        "API_KEY",
        "PASSWORD",
        "SECRET",
        "TOKEN"
    };
}
```

### 4. EvidenceOptions (New Class)

Created evidence storage configuration:

```csharp
public class EvidenceOptions
{
    public string StorageAccount { get; set; } = "complianceevidence";
    public string Container { get; set; } = "evidence";
    public int RetentionDays { get; set; } = 2555;  // ~7 years
    public bool EnableVersioning { get; set; } = true;
    public bool EnableImmutability { get; set; } = true;
}
```

### 5. NistControlsOptions Enhancement

Updated default timeout to match appsettings.json:

```csharp
public class NistControlsOptions
{
    public string BaseUrl { get; set; } = "...";
    public int TimeoutSeconds { get; set; } = 60;      // Changed from 30 to 60
    public int CacheDurationHours { get; set; } = 24;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public bool EnableOfflineFallback { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;
}
```

### 6. Agent Implementation Updates

Modified `ComplianceAgent.cs`:

```csharp
public ComplianceAgent(
    ISemanticKernelService semanticKernelService,
    ILogger<ComplianceAgent> logger,
    IOptions<ComplianceAgentOptions> options,  // ADDED
    CompliancePlugin compliancePlugin,
    ConfigurationPlugin configurationPlugin)
{
    _options = options.Value;
    
    // Uses configuration values
    var executionSettings = new OpenAIPromptExecutionSettings
    {
        Temperature = _options.Temperature,     // From config
        MaxTokens = _options.MaxTokens,         // From config
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };
}
```

### 7. Dependency Injection Registration

Updated `ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddComplianceAgent(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // Register ComplianceAgent configuration
    services.Configure<ComplianceAgentOptions>(
        configuration.GetSection(ComplianceAgentOptions.SectionName));
    
    // ... rest of registrations
}
```

## Complete Configuration Example

```json
{
  "NistControls": {
    "BaseUrl": "https://raw.githubusercontent.com/usnistgov/oscal-content/main/nist.gov/SP800-53/rev5/json",
    "TimeoutSeconds": 60,
    "CacheDurationHours": 24,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 2,
    "EnableOfflineFallback": true,
    "EnableDetailedLogging": false
  },
  "ComplianceAgent": {
    "Temperature": 0.2,
    "MaxTokens": 6000,
    "EnableAutomatedRemediation": true,
    "DefaultFramework": "NIST80053",
    "DefaultBaseline": "FedRAMPHigh",
    "DefenderForCloud": {
      "Enabled": false,
      "IncludeSecureScore": true,
      "MapToNistControls": true,
      "CacheDurationMinutes": 60,
      "EnableDeduplication": true,
      "SubscriptionId": "your-subscription-id",
      "WorkspaceId": "your-log-analytics-workspace-id"
    },
    "CodeScanning": {
      "EnableSecretsDetection": true,
      "EnableDependencyScanning": true,
      "EnableStigChecks": true,
      "SecretPatterns": [
        "API_KEY",
        "PASSWORD",
        "SECRET",
        "TOKEN"
      ]
    },
    "Evidence": {
      "StorageAccount": "complianceevidence",
      "Container": "evidence",
      "RetentionDays": 2555,
      "EnableVersioning": true,
      "EnableImmutability": true
    }
  }
}
```

## Configuration Options Explained

### Root-Level Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Temperature` | double | 0.2 | AI response temperature (0.0=deterministic, 2.0=creative) |
| `MaxTokens` | int | 6000 | Maximum response length for compliance assessments |
| `EnableAutomatedRemediation` | bool | true | Allow agent to automatically fix non-compliant resources |
| `DefaultFramework` | string | "NIST80053" | Default compliance framework (NIST80053, FedRAMPHigh, DoD IL5, SOC2, GDPR) |
| `DefaultBaseline` | string | "FedRAMPHigh" | Default compliance baseline (FedRAMPHigh, FedRAMPModerate, DoD IL5, DoD IL4) |

### DefenderForCloud Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | false | Enable Defender for Cloud integration |
| `IncludeSecureScore` | bool | true | Include secure score in reports |
| `MapToNistControls` | bool | true | Map findings to NIST 800-53 controls |
| `CacheDurationMinutes` | int | 60 | Cache duration for DFC findings |
| `EnableDeduplication` | bool | true | Merge duplicate findings |
| `SubscriptionId` | string | null | Azure subscription ID for DFC |
| `WorkspaceId` | string | null | Log Analytics workspace ID |

### CodeScanning Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableSecretsDetection` | bool | true | Detect secrets in code repositories |
| `EnableDependencyScanning` | bool | true | Scan for vulnerable dependencies |
| `EnableStigChecks` | bool | true | Perform STIG compliance checks |
| `SecretPatterns` | array | ["API_KEY", "PASSWORD", "SECRET", "TOKEN"] | Patterns to detect as secrets |

### Evidence Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `StorageAccount` | string | "complianceevidence" | Azure Storage Account for evidence |
| `Container` | string | "evidence" | Container name for evidence |
| `RetentionDays` | int | 2555 | Evidence retention period (~7 years) |
| `EnableVersioning` | bool | true | Enable blob versioning |
| `EnableImmutability` | bool | true | Enable WORM (Write Once, Read Many) |

### NistControls Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `BaseUrl` | string | NIST OSCAL URL | Base URL for NIST control repository |
| `TimeoutSeconds` | int | 60 | HTTP client timeout |
| `CacheDurationHours` | int | 24 | Cache duration for controls |
| `MaxRetryAttempts` | int | 3 | Retry attempts for failed requests |
| `RetryDelaySeconds` | int | 2 | Delay between retries |
| `EnableOfflineFallback` | bool | true | Use offline fallback if API unavailable |
| `EnableDetailedLogging` | bool | false | Log detailed control lookups |

## Benefits

1. **Flexibility**: Configure compliance behavior without code changes
2. **Feature Toggles**: Enable/disable automated remediation, code scanning, etc.
3. **Integration Control**: Toggle Defender for Cloud integration
4. **Evidence Management**: Configure evidence storage and retention
5. **Performance**: Adjust AI parameters for accuracy vs. speed

## Migration Notes

The configuration structure has changed:

**Before (nested in AzureOpenAI):**
```json
{
  "ComplianceAgent": {
    "AzureOpenAI": {
      "Temperature": 0.7,
      "MaxTokens": 4000
    }
  }
}
```

**After (root level):**
```json
{
  "ComplianceAgent": {
    "Temperature": 0.2,
    "MaxTokens": 6000,
    "EnableAutomatedRemediation": true,
    "DefaultFramework": "NIST80053",
    "DefaultBaseline": "FedRAMPHigh"
  }
}
```

All new properties have sensible defaults, so existing deployments will continue to work.

## Files Modified

- ✅ `Configuration/ComplianceAgentOptions.cs` - Added root properties and new option classes
- ✅ `Services/Agents/ComplianceAgent.cs` - Inject and use configuration
- ✅ `Extensions/ServiceCollectionExtensions.cs` - Register configuration
- ✅ `appsettings.example.json` - Updated with complete configuration

## Testing

To verify the configuration:

1. **Check Temperature**: Monitor logs to see configured temperature value
2. **Test Remediation Toggle**: Set `EnableAutomatedRemediation: false` and verify remediation is disabled
3. **Verify Framework**: Request compliance scan without specifying framework, verify default is used
4. **Test Code Scanning**: Add/remove secret patterns and verify detection
5. **Check Evidence**: Verify evidence is stored in configured storage account

## References

- Configuration class: `src/Platform.Engineering.Copilot.Compliance.Agent/Configuration/ComplianceAgentOptions.cs`
- Agent implementation: `src/Platform.Engineering.Copilot.Compliance.Agent/Services/Agents/ComplianceAgent.cs`
- DI registration: `src/Platform.Engineering.Copilot.Compliance.Agent/Extensions/ServiceCollectionExtensions.cs`
- Example config: `appsettings.example.json`
