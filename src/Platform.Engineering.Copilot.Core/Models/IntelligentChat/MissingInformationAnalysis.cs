namespace Platform.Engineering.Copilot.Core.Models.IntelligentChat;

/// <summary>
/// Analysis of missing information requiring follow-up
/// </summary>
public class MissingInformationAnalysis
{
    /// <summary>
    /// Whether follow-up is required from user
    /// </summary>
    public bool RequiresFollowUp { get; set; }
    
    /// <summary>
    /// Natural language follow-up question
    /// </summary>
    public string? FollowUpPrompt { get; set; }
    
    /// <summary>
    /// Structured list of missing field names
    /// </summary>
    public List<string> MissingFields { get; set; } = new();
    
    /// <summary>
    /// Plugin context that identified missing info
    /// </summary>
    public string? PluginContext { get; set; }
    
    /// <summary>
    /// Priority level of clarification (1=critical, 2=important, 3=optional)
    /// </summary>
    public int Priority { get; set; } = 1;
}
