namespace Platform.Engineering.Copilot.Core.Models.Agents;

/// <summary>
/// Defines the types of specialized agents in the multi-agent system
/// </summary>
public enum AgentType
{
    /// <summary>
    /// Orchestrator agent that coordinates and plans execution across specialized agents
    /// </summary>
    Orchestrator,
    
    /// <summary>
    /// Infrastructure provisioning and Azure resource management
    /// </summary>
    Infrastructure,
    
    /// <summary>
    /// Compliance scanning, NIST 800-53, security assessments
    /// </summary>
    Compliance,
    
    /// <summary>
    /// Cost estimation, optimization, and budget management
    /// </summary>
    CostManagement,
    
    /// <summary>
    /// Environment lifecycle, cloning, scaling operations
    /// </summary>
    Environment,
    
    /// <summary>
    /// Resource discovery, inventory, health monitoring
    /// </summary>
    Discovery,
    
    /// <summary>
    /// Service creation, mission ServiceCreation and requirement gathering
    /// </summary>
    ServiceCreation,
    
    /// <summary>
    /// Knowledge base search, document retrieval, and RAG-powered information synthesis
    /// </summary>
    KnowledgeBase
}
