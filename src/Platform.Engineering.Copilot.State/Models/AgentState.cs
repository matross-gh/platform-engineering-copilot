namespace Platform.Engineering.Copilot.State.Models;

/// <summary>
/// State for a specific agent within a conversation.
/// </summary>
public class AgentState
{
    /// <summary>
    /// Agent type identifier.
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Conversation this state belongs to.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Agent-specific data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Results from tool executions.
    /// </summary>
    public Dictionary<string, ToolExecutionResult> ToolResults { get; set; } = new();

    /// <summary>
    /// Current workflow state if agent is in a multi-step workflow.
    /// </summary>
    public WorkflowState? CurrentWorkflow { get; set; }

    /// <summary>
    /// Pending actions that need confirmation.
    /// </summary>
    public List<PendingAction> PendingActions { get; set; } = new();

    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Agent-specific metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Set a data value.
    /// </summary>
    public void SetData<T>(string key, T value) where T : notnull
    {
        Data[key] = value;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get a data value.
    /// </summary>
    public T? GetData<T>(string key)
    {
        if (Data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}

/// <summary>
/// Result from a tool execution.
/// </summary>
public class ToolExecutionResult
{
    public string ToolName { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Workflow state for multi-step operations.
/// </summary>
public class WorkflowState
{
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
    public string WorkflowType { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public int StepIndex { get; set; }
    public int TotalSteps { get; set; }
    public Dictionary<string, object> StepData { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.InProgress;
}

/// <summary>
/// Workflow status.
/// </summary>
public enum WorkflowStatus
{
    NotStarted,
    InProgress,
    AwaitingConfirmation,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// A pending action that needs user confirmation.
/// </summary>
public class PendingAction
{
    public string ActionId { get; set; } = Guid.NewGuid().ToString();
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? ActionData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
    public PendingActionStatus Status { get; set; } = PendingActionStatus.Pending;

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// Status of a pending action.
/// </summary>
public enum PendingActionStatus
{
    Pending,
    Confirmed,
    Rejected,
    Expired,
    Executed
}
