using System.ComponentModel.DataAnnotations;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Configuration;

/// <summary>
/// Configuration options for Knowledge Base Agent
/// </summary>
public class KnowledgeBaseAgentOptions
{
    public const string SectionName = "AgentConfiguration:KnowledgeBaseAgent";

    /// <summary>
    /// Whether this agent is enabled. When false, the agent will not be registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable RAG (Retrieval-Augmented Generation) for knowledge base lookups
    /// </summary>
    public bool EnableRag { get; set; } = true;

    /// <summary>
    /// Minimum relevance score threshold for RAG results (0.0 - 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double MinimumRelevanceScore { get; set; } = 0.75;

    /// <summary>
    /// Maximum number of RAG results to include in context
    /// </summary>
    [Range(1, 20)]
    public int MaxRagResults { get; set; } = 5;

    /// <summary>
    /// Maximum tokens for completion response
    /// </summary>
    [Range(1, 128000)]
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Temperature for LLM responses (0.0 = deterministic, 1.0 = creative)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Model name for chat completions
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o";

    /// <summary>
    /// Enable inclusion of conversation history in RAG completions
    /// </summary>
    public bool IncludeConversationHistory { get; set; } = true;

    /// <summary>
    /// Maximum number of previous messages to include in conversation history
    /// </summary>
    [Range(1, 50)]
    public int MaxConversationHistoryMessages { get; set; } = 10;

    /// <summary>
    /// Azure AI Search index name for knowledge base documents
    /// </summary>
    public string KnowledgeBaseIndexName { get; set; } = "knowledge-base-index";

    /// <summary>
    /// Enable semantic search for improved relevance
    /// </summary>
    public bool EnableSemanticSearch { get; set; } = true;

    /// <summary>
    /// Cache duration for knowledge base lookups (in minutes)
    /// </summary>
    [Range(1, 1440)]
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Default subscription ID for Azure operations
    /// </summary>
    public string? DefaultSubscriptionId { get; set; }

    /// <summary>
    /// Path to the knowledge base data folder
    /// </summary>
    public string KnowledgeBasePath { get; set; } = "KnowledgeBase";
}
