# EnableAutomatedRemediation Configuration Implementation

## Overview
This document describes the implementation of the `EnableAutomatedRemediation` configuration setting for the Compliance Agent, which allows administrators to globally enable or disable automated remediation of compliance findings.

## Configuration Setting

### Location
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "EnableAutomatedRemediation": true
    }
  }
}
```

### Purpose
- **Control automated remediation**: When set to `false`, prevents the Compliance Agent from executing any automated fixes to compliance findings
- **Safety mechanism**: Provides a safety switch to disable auto-remediation in production environments if needed
- **Audit compliance**: Some compliance frameworks may require human approval for all infrastructure changes

### Default Value
`true` - Automated remediation is enabled by default

## Implementation Details

### 1. Configuration Class
**File**: `src/Platform.Engineering.Copilot.Compliance.Agent/Configuration/ComplianceAgentOptions.cs`

```csharp
/// <summary>
/// Enable automated remediation of compliance findings
/// When true, agent can automatically apply fixes to non-compliant resources
/// Default: true
/// </summary>
public bool EnableAutomatedRemediation { get; set; } = true;
```

### 2. Remediation Engine Guard
**File**: `src/Platform.Engineering.Copilot.Compliance.Agent/Services/Compliance/AtoRemediationEngine.cs`

**Added Dependencies**:
- `IOptions<ComplianceAgentOptions>` injected via constructor
- Configuration check at the start of `ExecuteRemediationAsync()`

**Implementation**:
```csharp
public async Task<RemediationExecution> ExecuteRemediationAsync(
    string subscriptionId,
    AtoFinding finding,
    RemediationExecutionOptions options,
    CancellationToken cancellationToken = default)
{
    // Check if automated remediation is enabled
    if (!_options.EnableAutomatedRemediation)
    {
        _logger.LogWarning("⚠️ Automated remediation is disabled in configuration (EnableAutomatedRemediation=false)");
        execution.Status = RemediationExecutionStatus.Failed;
        execution.Success = false;
        execution.ErrorMessage = "Automated remediation is disabled in agent configuration";
        execution.Error = "EnableAutomatedRemediation is set to false in ComplianceAgent configuration.";
        execution.Message = "Remediation blocked by configuration - EnableAutomatedRemediation is disabled";
        return execution;
    }
    // ... rest of remediation logic
}
```

### 3. Plugin-Level Guard
**File**: `src/Platform.Engineering.Copilot.Compliance.Agent/Plugins/CompliancePlugin.cs`

**Added Dependencies**:
- `IOptions<ComplianceAgentOptions>` injected via constructor
- Configuration check in `ExecuteRemediationAsync()` function before calling remediation engine

**Implementation**:
```csharp
[KernelFunction("execute_remediation")]
public async Task<string> ExecuteRemediationAsync(...)
{
    // ... validation checks ...
    
    // Check if automated remediation is enabled in configuration
    if (!_options.EnableAutomatedRemediation)
    {
        _logger.LogWarning("⚠️ Automated remediation is disabled");
        return JsonSerializer.Serialize(new
        {
            success = false,
            error = "Automated remediation is disabled",
            configurationSetting = "ComplianceAgent.EnableAutomatedRemediation",
            currentValue = false,
            recommendation = "Set EnableAutomatedRemediation to true to enable"
        });
    }
    
    // ... execute remediation ...
}
```

## Behavior When Disabled

When `EnableAutomatedRemediation` is set to `false`:

1. **Assessment Still Works**: Compliance assessments continue to run normally and identify findings
2. **Auto-Remediable Flag**: Findings are still marked as auto-remediable (`IsAutoRemediable = true`)
3. **Remediation Plans**: Remediation plans can still be generated
4. **Execution Blocked**: Any attempt to execute remediation will be blocked with a clear error message
5. **Error Response**: Users receive a JSON response indicating:
   - Remediation is disabled
   - Which configuration setting controls it
   - How to enable it
   - Manual remediation guidance is still provided

## Use Cases

### 1. Production Safety
Disable auto-remediation in production until manual approval:
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "EnableAutomatedRemediation": false
    }
  }
}
```

### 2. Read-Only Compliance Mode
Run assessments without any risk of changes:
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "EnableAutomatedRemediation": false
    }
  }
}
```

### 3. Development/Testing
Enable full automation for rapid iteration:
```json
{
  "AgentConfiguration": {
    "ComplianceAgent": {
      "EnableAutomatedRemediation": true
    }
  }
}
```

## Error Messages

### Plugin-Level Error (User-Facing)
```json
{
  "success": false,
  "error": "Automated remediation is disabled",
  "findingId": "finding-123",
  "configurationSetting": "ComplianceAgent.EnableAutomatedRemediation",
  "currentValue": false,
  "recommendation": "Set EnableAutomatedRemediation to true in ComplianceAgent configuration to enable automated remediation",
  "manualGuidance": "..."
}
```

### Engine-Level Error (Internal)
```json
{
  "ExecutionId": "exec-456",
  "Status": "Failed",
  "Success": false,
  "ErrorMessage": "Automated remediation is disabled in agent configuration",
  "Error": "EnableAutomatedRemediation is set to false in ComplianceAgent configuration. Set to true to enable automated remediation.",
  "Message": "Remediation blocked by configuration - EnableAutomatedRemediation is disabled"
}
```

## Testing

### Test Scenarios

1. **Configuration Disabled - Execution Attempt**
   - Set `EnableAutomatedRemediation = false`
   - Run `execute_remediation` function
   - Expected: Returns error with configuration guidance

2. **Configuration Disabled - Assessment**
   - Set `EnableAutomatedRemediation = false`
   - Run `run_compliance_assessment`
   - Expected: Assessment runs normally, findings marked as auto-remediable

3. **Configuration Enabled - Execution**
   - Set `EnableAutomatedRemediation = true`
   - Run `execute_remediation` function
   - Expected: Remediation executes normally

4. **Configuration Toggle**
   - Start with `EnableAutomatedRemediation = false`
   - Attempt remediation (should fail)
   - Change to `true` and restart
   - Attempt remediation (should succeed)

## Logging

When remediation is blocked, logs include:

```
[WARNING] ⚠️ Automated remediation is disabled in configuration (EnableAutomatedRemediation=false)
```

This ensures administrators can easily identify why remediation is not executing.

## Related Configuration

Other compliance agent settings that work in conjunction:

- `RequireApproval`: Additional approval gate even when auto-remediation is enabled
- `Governance.EnforcePolicies`: Separate policy enforcement mechanism
- `Governance.BlockViolations`: Controls whether policy violations block operations

## Migration Notes

### Existing Deployments
- No migration needed
- Default value is `true` (enabled), preserving existing behavior
- Existing configurations without this setting will automatically use the default

### New Deployments
- Consider setting to `false` initially for safety
- Enable after validation and testing
- Document the decision in deployment runbooks

## Security Considerations

1. **Principle of Least Privilege**: Consider disabling auto-remediation in production
2. **Change Control**: Use with approval workflows for production changes
3. **Audit Trail**: All remediation attempts (successful or blocked) are logged
4. **Rollback Capability**: When enabled, remediation creates backups before changes

## Future Enhancements

Potential improvements:

1. **Per-Finding Type Control**: Different settings for different finding types
2. **Severity-Based Control**: Auto-remediate low/medium, require approval for high/critical
3. **Resource-Based Control**: Different settings for different resource types
4. **Schedule-Based Control**: Enable during maintenance windows only
5. **Multi-Level Approval**: Different approval requirements based on risk

## Summary

The `EnableAutomatedRemediation` configuration provides a critical safety mechanism for controlling automated compliance remediation. It allows organizations to:

- Run compliance assessments without risk of changes
- Gradually enable automation as confidence grows
- Meet compliance requirements that mandate human approval
- Maintain control over infrastructure changes

The implementation provides multiple layers of protection:
1. Plugin-level check (user-facing)
2. Engine-level check (internal safety)
3. Clear error messages and guidance
4. Comprehensive logging

This ensures that the configuration is respected throughout the remediation workflow.
