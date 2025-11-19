# DefaultFramework and DefaultBaseline Configuration Implementation

## Overview
This document describes the implementation of the `DefaultFramework` and `DefaultBaseline` configuration settings for the Compliance Agent. These settings allow administrators to configure which compliance framework and baseline the agent uses by default when users don't explicitly specify one.

## Configuration Settings

### Location
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "DefaultFramework": "NIST80053",
      "DefaultBaseline": "FedRAMPHigh"
    }
  }
}
```

### Purpose
- **DefaultFramework**: Sets the default compliance framework to use for assessments, validations, and reports
- **DefaultBaseline**: Sets the default compliance baseline to apply for control mappings and security requirements

### Supported Values

#### DefaultFramework Options
- `"NIST80053"` - NIST 800-53 Revision 5 controls (Default)
- `"FedRAMPHigh"` - FedRAMP High baseline
- `"FedRAMPModerate"` - FedRAMP Moderate baseline
- `"DoDIL5"` - DoD Impact Level 5
- `"DoDIL4"` - DoD Impact Level 4  
- `"SOC2"` - SOC 2 compliance framework
- `"GDPR"` - GDPR compliance requirements
- `"CMMC"` - Cybersecurity Maturity Model Certification
- `"HIPAA"` - HIPAA Security Rule

#### DefaultBaseline Options
- `"FedRAMPHigh"` - FedRAMP High baseline (Default)
- `"FedRAMPModerate"` - FedRAMP Moderate baseline
- `"FedRAMPLow"` - FedRAMP Low baseline
- `"DoDIL5"` - DoD Impact Level 5 baseline
- `"DoDIL4"` - DoD Impact Level 4 baseline
- `"DoDIL2"` - DoD Impact Level 2 baseline
- `"NIST800-53High"` - NIST 800-53 High baseline
- `"NIST800-53Moderate"` - NIST 800-53 Moderate baseline
- `"NIST800-53Low"` - NIST 800-53 Low baseline

## Implementation Details

### 1. Configuration Class
**File**: `src/Platform.Engineering.Copilot.Compliance.Agent/Configuration/ComplianceAgentOptions.cs`

```csharp
/// <summary>
/// Default compliance framework to use
/// Options: "NIST80053", "FedRAMPHigh", "DoD IL5", "SOC2", "GDPR"
/// Default: "NIST80053"
/// </summary>
public string DefaultFramework { get; set; } = "NIST80053";

/// <summary>
/// Default compliance baseline to apply
/// Options: "FedRAMPHigh", "FedRAMPModerate", "DoD IL5", "DoD IL4"
/// Default: "FedRAMPHigh"
/// </summary>
public string DefaultBaseline { get; set; } = "FedRAMPHigh";
```

### 2. Helper Methods in CompliancePlugin
**File**: `src/Platform.Engineering.Copilot.Compliance.Agent/Plugins/CompliancePlugin.cs`

```csharp
/// <summary>
/// Gets the effective compliance framework to use (parameter or configured default)
/// </summary>
private string GetEffectiveFramework(string? requestedFramework = null)
{
    if (!string.IsNullOrWhiteSpace(requestedFramework))
    {
        _logger.LogDebug("Using requested framework: {Framework}", requestedFramework);
        return requestedFramework;
    }
    
    _logger.LogDebug("Using default framework from configuration: {Framework}", _options.DefaultFramework);
    return _options.DefaultFramework;
}

/// <summary>
/// Gets the effective compliance baseline to use (parameter or configured default)
/// </summary>
private string GetEffectiveBaseline(string? requestedBaseline = null)
{
    if (!string.IsNullOrWhiteSpace(requestedBaseline))
    {
        _logger.LogDebug("Using requested baseline: {Baseline}", requestedBaseline);
        return requestedBaseline;
    }
    
    _logger.LogDebug("Using default baseline from configuration: {Baseline}", _options.DefaultBaseline);
    return _options.DefaultBaseline;
}
```

## Usage in Plugin Functions

### Where These Settings Should Be Used

1. **Compliance Assessments**
   - When running assessments without specifying a framework
   - When validating resources against compliance requirements
   - When generating compliance reports

2. **Template Generation**
   - When enhancing templates with compliance controls
   - When validating generated templates for compliance
   - When applying security baselines to infrastructure

3. **Remediation Planning**
   - When generating remediation plans based on compliance gaps
   - When prioritizing remediation actions by framework requirements
   - When validating fixes meet baseline requirements

4. **Documentation Generation**
   - When creating compliance documentation
   - When generating ATO checklists
   - When producing compliance gap analysis reports

### Example Usage Patterns

#### Pattern 1: Assessment Function
```csharp
[KernelFunction("run_compliance_assessment")]
public async Task<string> RunComplianceAssessmentAsync(
    string? subscriptionIdOrName = null,
    string? resourceGroupName = null,
    string? framework = null,  // Optional - uses DefaultFramework if not provided
    CancellationToken cancellationToken = default)
{
    // Get effective framework
    var effectiveFramework = GetEffectiveFramework(framework);
    _logger.LogInformation("Running compliance assessment with framework: {Framework}", effectiveFramework);
    
    // Use effectiveFramework for assessment...
}
```

#### Pattern 2: Template Enhancement
```csharp
[KernelFunction("enhance_template_with_compliance")]
public async Task<string> EnhanceTemplateAsync(
    string templateContent,
    string? framework = null,  // Optional - uses DefaultFramework if not provided
    string? baseline = null,   // Optional - uses DefaultBaseline if not provided
    CancellationToken cancellationToken = default)
{
    var effectiveFramework = GetEffectiveFramework(framework);
    var effectiveBaseline = GetEffectiveBaseline(baseline);
    
    _logger.LogInformation("Enhancing template with framework: {Framework}, baseline: {Baseline}",
        effectiveFramework, effectiveBaseline);
    
    // Apply compliance controls based on framework and baseline...
}
```

#### Pattern 3: Remediation Planning
```csharp
[KernelFunction("generate_remediation_plan")]
public async Task<string> GenerateRemediationPlanAsync(
    string subscriptionIdOrName,
    string? framework = null,  // Optional - uses DefaultFramework if not provided
    string? baseline = null,   // Optional - uses DefaultBaseline if not provided
    CancellationToken cancellationToken = default)
{
    var effectiveFramework = GetEffectiveFramework(framework);
    var effectiveBaseline = GetEffectiveBaseline(baseline);
    
    // Generate plan based on framework and baseline requirements...
}
```

## Behavior

### When Framework/Baseline Not Specified by User

**Scenario 1**: User runs assessment without specifying framework
```
User: "Run compliance assessment"
Agent: Uses DefaultFramework from configuration (NIST80053)
Result: Assessment runs against NIST 800-53 controls
```

**Scenario 2**: User generates template without specifying baseline
```
User: "Generate secure Azure SQL template"
Agent: Uses DefaultBaseline from configuration (FedRAMPHigh)
Result: Template includes FedRAMP High security controls
```

### When Framework/Baseline Explicitly Specified

**Scenario 3**: User specifies different framework
```
User: "Run compliance assessment with SOC2 framework"
Agent: Uses SOC2 (overrides DefaultFramework)
Result: Assessment runs against SOC 2 controls
```

**Scenario 4**: User specifies different baseline
```
User: "Generate template with DoD IL4 baseline"
Agent: Uses DoD IL4 (overrides DefaultBaseline)
Result: Template includes DoD IL4 security controls
```

## Configuration Examples

### Government/Defense Environment
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "DefaultFramework": "NIST80053",
      "DefaultBaseline": "FedRAMPHigh",
      "Temperature": 0.2,
      "MaxTokens": 6000
    }
  }
}
```

### Commercial/Enterprise Environment
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "DefaultFramework": "SOC2",
      "DefaultBaseline": "FedRAMPModerate",
      "Temperature": 0.2,
      "MaxTokens": 6000
    }
  }
}
```

### Healthcare Environment
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "DefaultFramework": "HIPAA",
      "DefaultBaseline": "NIST800-53Moderate",
      "Temperature": 0.2,
      "MaxTokens": 6000
    }
  }
}
```

### DoD IL5 Environment
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "DefaultFramework": "DoDIL5",
      "DefaultBaseline": "DoDIL5",
      "Temperature": 0.2,
      "MaxTokens": 6000
    }
  }
}
```

## Logging

When defaults are used, logs will show:
```
[DEBUG] Using default framework from configuration: NIST80053
[DEBUG] Using default baseline from configuration: FedRAMPHigh
[INFO] Running compliance assessment with framework: NIST80053
```

When user overrides defaults:
```
[DEBUG] Using requested framework: SOC2
[DEBUG] Using requested baseline: FedRAMPModerate
[INFO] Running compliance assessment with framework: SOC2
```

## Integration Points

### 1. ComplianceAwareTemplateEnhancer
**Usage**: Apply framework and baseline when enhancing templates
```csharp
public async Task<TemplateGenerationResult> EnhanceWithComplianceAsync(
    TemplateGenerationRequest request,
    string? complianceFramework = null)  // Uses DefaultFramework if null
{
    complianceFramework ??= _options.DefaultFramework;
    // ... rest of implementation
}
```

### 2. AtoComplianceEngine
**Usage**: Use framework for control selection and assessment
```csharp
public async Task<ComprehensiveAssessment> RunComprehensiveAssessmentAsync(
    string subscriptionId,
    string? resourceGroupName,
    string? framework = null)  // Uses DefaultFramework if null
{
    framework ??= _options.DefaultFramework;
    // ... rest of implementation
}
```

### 3. RemediationPlanGenerator
**Usage**: Prioritize remediation based on baseline requirements
```csharp
public async Task<RemediationPlan> GeneratePlanAsync(
    List<AtoFinding> findings,
    string? baseline = null)  // Uses DefaultBaseline if null
{
    baseline ??= _options.DefaultBaseline;
    // ... rest of implementation
}
```

## User Experience

### Conversational Interaction

**Without Configuration (Old Behavior)**:
```
User: "Run compliance assessment"
Agent: "Which compliance framework would you like to use? (NIST80053, FedRAMP, SOC2, etc.)"
User: "Use NIST80053"
Agent: "Running assessment with NIST80053..."
```

**With Configuration (New Behavior)**:
```
User: "Run compliance assessment"
Agent: "Running assessment with NIST80053 (default framework)..."
```

**Explicit Override**:
```
User: "Run compliance assessment with SOC2"
Agent: "Running assessment with SOC2 (overriding default NIST80053)..."
```

## Benefits

1. **Reduced User Friction**: Users don't need to specify framework every time
2. **Organizational Consistency**: All teams use the same default framework
3. **Flexibility**: Users can still override when needed
4. **Environment-Specific**: Different environments can have different defaults
5. **Compliance Alignment**: Ensures assessments align with org's primary compliance requirements

## Implementation Status

### ✅ Completed
- Configuration properties defined in `ComplianceAgentOptions`
- Helper methods implemented in `CompliancePlugin`
- Configuration binding in DI container

### 🔄 Recommended Next Steps
1. **Update ComplianceAwareTemplateEnhancer** to use DefaultFramework
2. **Update AtoComplianceEngine** to accept and use framework parameter
3. **Update kernel function descriptions** to mention defaults
4. **Add framework info to assessment results** showing which was used
5. **Update documentation examples** to show default behavior

## Testing

### Test Scenarios

1. **Default Framework Used**
   - Configure `DefaultFramework = "NIST80053"`
   - Run assessment without framework parameter
   - Verify NIST 800-53 controls are used

2. **Default Baseline Used**
   - Configure `DefaultBaseline = "FedRAMPHigh"`
   - Generate template without baseline parameter
   - Verify FedRAMP High controls are applied

3. **Explicit Override**
   - Configure `DefaultFramework = "NIST80053"`
   - Run assessment with `framework = "SOC2"`
   - Verify SOC2 controls are used (not NIST)

4. **Logging Verification**
   - Configure defaults
   - Run operations with and without overrides
   - Verify appropriate debug logs are emitted

## Future Enhancements

1. **Framework Compatibility Validation**: Warn if framework/baseline combination is unusual
2. **Multiple Default Frameworks**: Support different defaults for different operations
3. **Environment-Based Defaults**: Use different defaults based on Azure environment (Commercial vs Government)
4. **Framework Recommendations**: Suggest appropriate framework based on resource types
5. **Baseline Auto-Selection**: Automatically select baseline based on framework choice

## Summary

The `DefaultFramework` and `DefaultBaseline` configuration settings provide:

- **Simplified User Experience**: Users don't need to specify framework every time
- **Organizational Standards**: Enforce org-wide compliance framework preferences
- **Flexibility**: Users can override when needed for specific use cases
- **Consistency**: All assessments, templates, and reports use consistent compliance criteria by default

The implementation uses a simple pattern:
1. Add optional `framework` and `baseline` parameters to functions
2. Call `GetEffectiveFramework()` or `GetEffectiveBaseline()` to resolve actual value
3. Use the resolved value throughout the function
4. Log which value was used (default or override)

This approach maintains backward compatibility while providing better defaults based on organizational requirements.
