using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Compliance.Agent.Services.Compliance;

/// <summary>
/// Service for sanitizing and validating remediation scripts before execution
/// Prevents command injection, validates resource scopes, and enforces security policies
/// </summary>
public interface IScriptSanitizationService
{
    /// <summary>
    /// Validate script for security risks and policy compliance
    /// </summary>
    Task<ScriptValidationResult> ValidateScriptAsync(string script, string scriptType, AtoFinding finding);
    
    /// <summary>
    /// Sanitize script by removing dangerous commands and patterns
    /// </summary>
    string SanitizeScript(string script, string scriptType);
    
    /// <summary>
    /// Check if command is allowed for execution
    /// </summary>
    bool IsCommandAllowed(string command, string scriptType);
}

public class ScriptSanitizationService : IScriptSanitizationService
{
    private readonly ILogger<ScriptSanitizationService> _logger;
    
    // Dangerous commands that should never be executed
    private static readonly HashSet<string> BlockedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm", "rmdir", "del", "delete", "format", "fdisk",
        "shutdown", "reboot", "halt", "poweroff",
        "curl", "wget", "invoke-webrequest", "download",
        "nc", "netcat", "telnet",
        "eval", "exec", "system",
        "sudo", "su", "runas",
        "chmod", "chown", "chgrp",
        "export", "set-item env:",
        "registry", "reg add", "reg delete"
    };
    
    // Dangerous patterns (command injection, data exfiltration)
    private static readonly List<Regex> DangerousPatterns = new()
    {
        new Regex(@";\s*rm\s+-rf", RegexOptions.IgnoreCase), // ; rm -rf
        new Regex(@"\|\s*sh\b", RegexOptions.IgnoreCase), // | sh
        new Regex(@"\|\s*bash\b", RegexOptions.IgnoreCase), // | bash
        new Regex(@"\$\(.*\)", RegexOptions.IgnoreCase), // $(command)
        new Regex(@"`.*`", RegexOptions.IgnoreCase), // `command`
        new Regex(@">\s*/dev/", RegexOptions.IgnoreCase), // > /dev/...
        new Regex(@"&&\s*curl", RegexOptions.IgnoreCase), // && curl
        new Regex(@"\|\s*base64", RegexOptions.IgnoreCase), // | base64
        new Regex(@"--force\b", RegexOptions.IgnoreCase), // --force flag
        new Regex(@"-f\b.*delete", RegexOptions.IgnoreCase), // force delete
    };
    
    // Allowed Azure CLI commands for remediation
    private static readonly HashSet<string> AllowedAzCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "az account", "az group", "az resource",
        "az network", "az vm", "az disk", "az snapshot",
        "az storage", "az keyvault", "az sql",
        "az webapp", "az functionapp", "az containerapp",
        "az aks", "az acr", "az monitor",
        "az policy", "az role", "az ad",
        "az backup", "az security", "az advisor"
    };
    
    public ScriptSanitizationService(ILogger<ScriptSanitizationService> logger)
    {
        _logger = logger;
    }
    
    public async Task<ScriptValidationResult> ValidateScriptAsync(
        string script, 
        string scriptType, 
        AtoFinding finding)
    {
        var result = new ScriptValidationResult
        {
            IsValid = true,
            ScriptType = scriptType,
            Warnings = new List<string>(),
            Errors = new List<string>()
        };
        
        if (string.IsNullOrWhiteSpace(script))
        {
            result.IsValid = false;
            result.Errors.Add("Script is empty or null");
            return result;
        }
        
        try
        {
            // Check for dangerous patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (pattern.IsMatch(script))
                {
                    result.Errors.Add($"Dangerous pattern detected: {pattern}");
                    result.IsValid = false;
                }
            }
            
            // Check for blocked commands
            var lines = script.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip comments
                if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                    continue;
                
                // Check each word for blocked commands
                var words = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (BlockedCommands.Contains(word))
                    {
                        result.Errors.Add($"Blocked command detected: {word}");
                        result.IsValid = false;
                    }
                }
            }
            
            // Script-type specific validation
            switch (scriptType)
            {
                case "AzureCLI":
                    ValidateAzureCliScript(script, result);
                    break;
                    
                case "PowerShell":
                    ValidatePowerShellScript(script, result);
                    break;
                    
                case "Terraform":
                    ValidateTerraformScript(script, result);
                    break;
            }
            
            // Validate resource scope
            if (!string.IsNullOrEmpty(finding.ResourceId))
            {
                ValidateResourceScope(script, finding.ResourceId, result);
            }
            
            // Length validation
            if (script.Length > 50000)
            {
                result.Warnings.Add("Script is very large (>50KB). Consider breaking into smaller scripts.");
            }
            
            _logger.LogInformation("Script validation completed: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating script");
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }
        
        return await Task.FromResult(result);
    }
    
    public string SanitizeScript(string script, string scriptType)
    {
        if (string.IsNullOrWhiteSpace(script))
            return script;
        
        var sanitized = script;
        
        // Remove potentially dangerous redirects
        sanitized = Regex.Replace(sanitized, @">\s*/dev/\w+", "", RegexOptions.IgnoreCase);
        
        // Remove command chaining with dangerous commands
        sanitized = Regex.Replace(sanitized, @";\s*(rm|del|format)\s+", "", RegexOptions.IgnoreCase);
        
        // Remove backtick command substitution
        sanitized = Regex.Replace(sanitized, @"`[^`]*`", "", RegexOptions.IgnoreCase);
        
        // Remove excessive whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ");
        
        _logger.LogInformation("Script sanitized: OriginalLength={OriginalLength}, SanitizedLength={SanitizedLength}",
            script.Length, sanitized.Length);
        
        return sanitized.Trim();
    }
    
    public bool IsCommandAllowed(string command, string scriptType)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;
        
        var lowerCommand = command.Trim().ToLowerInvariant();
        
        // Check blocked commands first
        foreach (var blocked in BlockedCommands)
        {
            if (lowerCommand.Contains(blocked.ToLowerInvariant()))
                return false;
        }
        
        // Script-type specific checks
        switch (scriptType)
        {
            case "AzureCLI":
                return lowerCommand.StartsWith("az ") && 
                       AllowedAzCommands.Any(allowed => lowerCommand.StartsWith(allowed.ToLowerInvariant()));
                       
            case "PowerShell":
                return lowerCommand.StartsWith("get-") || 
                       lowerCommand.StartsWith("set-") ||
                       lowerCommand.StartsWith("new-") ||
                       lowerCommand.StartsWith("update-") ||
                       lowerCommand.Contains("-azresource");
                       
            case "Terraform":
                return lowerCommand.StartsWith("terraform ") &&
                       (lowerCommand.Contains("plan") || 
                        lowerCommand.Contains("apply") ||
                        lowerCommand.Contains("validate"));
                        
            default:
                return false;
        }
    }
    
    private void ValidateAzureCliScript(string script, ScriptValidationResult result)
    {
        var azCommands = script.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("az ", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (azCommands.Count == 0)
        {
            result.Warnings.Add("No Azure CLI commands found in script");
        }
        
        foreach (var cmd in azCommands)
        {
            // Check for destructive operations without confirmation
            if (cmd.Contains("delete", StringComparison.OrdinalIgnoreCase) &&
                !cmd.Contains("--yes") && !cmd.Contains("-y"))
            {
                result.Warnings.Add($"Delete command without confirmation flag: {cmd}");
            }
            
            // Ensure commands use proper resource specifications
            if (!cmd.Contains("--resource-group") && !cmd.Contains("-g") &&
                !cmd.Contains("--subscription") && !cmd.Contains("--id"))
            {
                result.Warnings.Add($"Command may not specify resource scope: {cmd}");
            }
        }
    }
    
    private void ValidatePowerShellScript(string script, ScriptValidationResult result)
    {
        // Check for dangerous PowerShell cmdlets
        var dangerousCmdlets = new[] { "Remove-Item", "Clear-", "Stop-Computer", "Restart-Computer" };
        
        foreach (var cmdlet in dangerousCmdlets)
        {
            if (script.Contains(cmdlet, StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"Potentially dangerous cmdlet: {cmdlet}");
            }
        }
        
        // Ensure Azure PowerShell modules are referenced
        if (!script.Contains("Az.", StringComparison.OrdinalIgnoreCase) &&
            !script.Contains("Connect-AzAccount", StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add("Script may not use Azure PowerShell modules");
        }
    }
    
    private void ValidateTerraformScript(string script, ScriptValidationResult result)
    {
        // Check for required Terraform blocks
        if (!script.Contains("resource \"", StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add("No Terraform resource blocks found");
        }
        
        // Check for provider configuration
        if (!script.Contains("provider \"", StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add("No Terraform provider configuration found");
        }
        
        // Ensure no hardcoded credentials
        var credentialPatterns = new[] { "client_secret", "password", "access_key", "secret_key" };
        foreach (var pattern in credentialPatterns)
        {
            if (script.Contains($"{pattern} =", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"Hardcoded credential detected: {pattern}");
                result.IsValid = false;
            }
        }
    }
    
    private void ValidateResourceScope(string script, string resourceId, ScriptValidationResult result)
    {
        // Extract resource components from resourceId
        // /subscriptions/{sub}/resourceGroups/{rg}/providers/{provider}/{type}/{name}
        var parts = resourceId.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 4)
        {
            result.Warnings.Add("Invalid resource ID format");
            return;
        }
        
        var subscriptionIdx = Array.IndexOf(parts, "subscriptions");
        var rgIdx = Array.IndexOf(parts, "resourceGroups");
        
        string? subscription = subscriptionIdx >= 0 && subscriptionIdx + 1 < parts.Length 
            ? parts[subscriptionIdx + 1] : null;
        string? resourceGroup = rgIdx >= 0 && rgIdx + 1 < parts.Length 
            ? parts[rgIdx + 1] : null;
        
        // Verify script references the correct subscription
        if (!string.IsNullOrEmpty(subscription) && 
            !script.Contains(subscription, StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add($"Script may not target the correct subscription: {subscription}");
        }
        
        // Verify script references the correct resource group
        if (!string.IsNullOrEmpty(resourceGroup) && 
            !script.Contains(resourceGroup, StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add($"Script may not target the correct resource group: {resourceGroup}");
        }
    }
}

// Removed ScriptValidationResult - now in Platform.Engineering.Copilot.Core.Models.Compliance
