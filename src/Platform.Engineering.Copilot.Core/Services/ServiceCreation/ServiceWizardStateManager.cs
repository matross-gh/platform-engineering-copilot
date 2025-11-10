using Microsoft.SemanticKernel.Memory;
using Platform.Engineering.Copilot.Core.Models.ServiceCreation;
using System.Text.Json;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

namespace Platform.Engineering.Copilot.Core.Services.ServiceCreation;

/// <summary>
/// Manages Service Wizard state in Semantic Kernel Memory (SharedMemory)
/// Enables persistence across multiple agent interactions
/// </summary>
public class ServiceWizardStateManager
{
    private const string MemoryCollectionName = "service-wizard-sessions";
    private readonly ISemanticTextMemory _memory;
    
    public ServiceWizardStateManager(ISemanticTextMemory memory)
    {
        _memory = memory;
    }
    
    /// <summary>
    /// Create a new wizard session
    /// </summary>
    public async Task<ServiceWizardState> CreateSessionAsync()
    {
        var state = new ServiceWizardState
        {
            SessionId = Guid.NewGuid().ToString(),
            CurrentStep = WizardStep.NotStarted,
            StartedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await SaveStateAsync(state);
        return state;
    }
    
    /// <summary>
    /// Get wizard state by session ID
    /// </summary>
    public async Task<ServiceWizardState?> GetStateAsync(string sessionId)
    {
        try
        {
            var memoryRecord = await _memory.GetAsync(MemoryCollectionName, sessionId);
            
            if (memoryRecord == null)
                return null;
            
            var state = JsonSerializer.Deserialize<ServiceWizardState>(memoryRecord.Metadata.Text);
            return state;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Get the most recent active session for the user
    /// </summary>
    public async Task<ServiceWizardState?> GetActiveSessionAsync()
    {
        try
        {
            var results = new List<MemoryQueryResult>();
            await foreach (var result in _memory.SearchAsync(
                MemoryCollectionName,
                "active wizard session",
                limit: 1,
                minRelevanceScore: 0.0
            ))
            {
                results.Add(result);
            }
            
            if (results.Count == 0)
                return null;
            
            var state = JsonSerializer.Deserialize<ServiceWizardState>(results[0].Metadata.Text);
            return state;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Save wizard state to memory
    /// </summary>
    public async Task SaveStateAsync(ServiceWizardState state)
    {
        state.LastUpdated = DateTime.UtcNow;
        
        var stateJson = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await _memory.SaveInformationAsync(
            collection: MemoryCollectionName,
            id: state.SessionId,
            text: stateJson,
            description: $"Service Wizard Session - Step {state.CurrentStep} - {state.ServiceName ?? "Unnamed"}",
            additionalMetadata: $"Started: {state.StartedAt:yyyy-MM-dd HH:mm:ss} | Progress: {state.CompletionPercentage}%"
        );
    }
    
    /// <summary>
    /// Update specific field in wizard state
    /// </summary>
    public async Task UpdateStateFieldAsync(string sessionId, Action<ServiceWizardState> updateAction)
    {
        var state = await GetStateAsync(sessionId);
        
        if (state == null)
            throw new InvalidOperationException($"Wizard session {sessionId} not found");
        
        updateAction(state);
        await SaveStateAsync(state);
    }
    
    /// <summary>
    /// Delete wizard session
    /// </summary>
    public async Task DeleteSessionAsync(string sessionId)
    {
        try
        {
            await _memory.RemoveAsync(MemoryCollectionName, sessionId);
        }
        catch
        {
            // Ignore errors on delete
        }
    }
    
    /// <summary>
    /// List all wizard sessions
    /// </summary>
    public async Task<List<ServiceWizardState>> ListSessionsAsync(int limit = 10)
    {
        var sessions = new List<ServiceWizardState>();
        
        try
        {
            var results = new List<MemoryQueryResult>();
            await foreach (var result in _memory.SearchAsync(
                MemoryCollectionName,
                "wizard session",
                limit: limit,
                minRelevanceScore: 0.0
            ))
            {
                results.Add(result);
            }
            
            foreach (var result in results)
            {
                try
                {
                    var state = JsonSerializer.Deserialize<ServiceWizardState>(result.Metadata.Text);
                    if (state != null)
                    {
                        sessions.Add(state);
                    }
                }
                catch
                {
                    // Skip invalid sessions
                    continue;
                }
            }
        }
        catch
        {
            // Return empty list on error
        }
        
        return sessions.OrderByDescending(s => s.LastUpdated).ToList();
    }
    
    /// <summary>
    /// Clean up old completed sessions (older than 7 days)
    /// </summary>
    public async Task CleanupOldSessionsAsync(int daysToKeep = 7)
    {
        var sessions = await ListSessionsAsync(limit: 100);
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        foreach (var session in sessions)
        {
            if (session.IsComplete && session.LastUpdated < cutoffDate)
            {
                await DeleteSessionAsync(session.SessionId);
            }
        }
    }
}
