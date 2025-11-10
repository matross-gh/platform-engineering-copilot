using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Models.ServiceCreation;

/// <summary>
/// Wizard step enumeration for 8-step service creation process
/// </summary>
public enum WizardStep
{
    NotStarted = 0,
    Step1_MissionSponsor = 1,
    Step2_ImpactLevel = 2,
    Step3_Region = 3,
    Step4_DataClassification = 4,
    Step5_Environment = 5,
    Step6_DoDAAC = 6,
    Step7_ServiceType = 7,
    Step8_TechStack = 8,
    Completed = 9
}

/// <summary>
/// State container for Service Creation Wizard
/// Tracks user progress through 8-step DoD service creation flow
/// </summary>
public class ServiceWizardState
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public WizardStep CurrentStep { get; set; } = WizardStep.NotStarted;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Step 1: Mission Sponsor
    public string? MissionSponsor { get; set; }
    
    // Step 2: Impact Level
    public ImpactLevel? ImpactLevel { get; set; }
    
    // Step 3: Azure Region
    public string? Region { get; set; }
    
    // Step 4: Data Classification
    public string? DataClassification { get; set; }
    
    // Step 5: Environment
    public string? Environment { get; set; }
    
    // Step 6: DoDAAC
    public string? DoDAAC { get; set; }
    public string? OrganizationUnit { get; set; }
    
    // Step 7: Service Type
    public string? ServiceName { get; set; }
    public string? ServiceDescription { get; set; }
    public string? ServiceType { get; set; } // "API", "Web App", "Worker Service", "Database", etc.
    
    // Step 8: Tech Stack
    public string? ProgrammingLanguage { get; set; }
    public string? Framework { get; set; }
    public string? DatabaseType { get; set; }
    public string? InfrastructureFormat { get; set; } // "Terraform", "Bicep", "Kubernetes"
    public string? ComputePlatform { get; set; } // "AKS", "App Service", "Container Apps"
    
    // Validation tracking
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    
    // Step history for "go back" functionality
    public List<WizardStep> StepHistory { get; set; } = new();
    
    /// <summary>
    /// Check if wizard is complete
    /// </summary>
    public bool IsComplete => CurrentStep == WizardStep.Completed;
    
    /// <summary>
    /// Get completion percentage (0-100)
    /// </summary>
    public int CompletionPercentage => ((int)CurrentStep * 100) / (int)WizardStep.Completed;
    
    /// <summary>
    /// Advance to next step
    /// </summary>
    public void AdvanceToNextStep()
    {
        if (CurrentStep < WizardStep.Completed)
        {
            StepHistory.Add(CurrentStep);
            CurrentStep = (WizardStep)((int)CurrentStep + 1);
            LastUpdated = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Go back to previous step
    /// </summary>
    public bool GoBackToPreviousStep()
    {
        if (StepHistory.Count > 0)
        {
            CurrentStep = StepHistory[^1];
            StepHistory.RemoveAt(StepHistory.Count - 1);
            LastUpdated = DateTime.UtcNow;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Reset wizard to beginning
    /// </summary>
    public void Reset()
    {
        CurrentStep = WizardStep.NotStarted;
        StepHistory.Clear();
        ValidationErrors.Clear();
        ValidationWarnings.Clear();
        
        MissionSponsor = null;
        ImpactLevel = null;
        Region = null;
        DataClassification = null;
        Environment = null;
        DoDAAC = null;
        OrganizationUnit = null;
        ServiceName = null;
        ServiceDescription = null;
        ServiceType = null;
        ProgrammingLanguage = null;
        Framework = null;
        DatabaseType = null;
        InfrastructureFormat = null;
        ComputePlatform = null;
        
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Wizard prompt with validation rules
/// </summary>
public class WizardPrompt
{
    public WizardStep Step { get; set; }
    public string Question { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
    public List<string> SuggestedAnswers { get; set; } = new();
    public bool IsRequired { get; set; } = true;
    public string? ValidationPattern { get; set; }
    public string? ValidationErrorMessage { get; set; }
}

/// <summary>
/// Result of wizard step validation
/// </summary>
public class WizardStepValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string? NextPrompt { get; set; }
    
    public static WizardStepValidationResult Success(string? nextPrompt = null)
    {
        return new WizardStepValidationResult
        {
            IsValid = true,
            NextPrompt = nextPrompt
        };
    }
    
    public static WizardStepValidationResult Failure(params string[] errors)
    {
        return new WizardStepValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}
