namespace Platform.Engineering.Copilot.Core.Models.Compliance;

/// <summary>
/// Result of script validation (syntax check)
/// </summary>
public class ScriptValidationResult
{
    public bool IsValid { get; set; }
    public string ScriptType { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> BlockedCommands { get; set; } = new();
    public string Summary => IsValid 
        ? $"✅ Valid ({Warnings.Count} warnings)" 
        : $"❌ Invalid ({Errors.Count} errors, {Warnings.Count} warnings)";
}
