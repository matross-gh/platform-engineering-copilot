namespace Platform.Engineering.Copilot.KnowledgeBase.Agent.Configuration;

/// <summary>
/// Configuration options for Knowledge Base Agent
/// </summary>
public class KnowledgeBaseAgentOptions
{
    public const string SectionName = "KnowledgeBaseAgent";

    /// <summary>
    /// Enable RAG (Retrieval-Augmented Generation) for knowledge base lookups
    /// </summary>
    public bool EnableRag { get; set; } = true;

    /// <summary>
    /// Minimum relevance score threshold for RAG results (0.0 - 1.0)
    /// </summary>
    public double MinimumRelevanceScore { get; set; } = 0.75;

    /// <summary>
    /// Maximum number of RAG results to include in context
    /// </summary>
    public int MaxRagResults { get; set; } = 5;

    /// <summary>
    /// Maximum tokens for completion response
    /// </summary>
    public int MaxCompletionTokens { get; set; } = 4000;

    /// <summary>
    /// Temperature for LLM responses (0.0 = deterministic, 1.0 = creative)
    /// </summary>
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
    public int CacheDurationMinutes { get; set; } = 60;
}
