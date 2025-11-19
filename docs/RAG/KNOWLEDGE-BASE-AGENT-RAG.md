# Knowledge Base Agent - RAG-Powered Documentation Retrieval

## Overview

The **Knowledge Base Agent** is a specialized agent that uses **RAG (Retrieval-Augmented Generation)** to search and retrieve information from indexed documentation, technical guides, and knowledge repositories. It leverages Azure AI Search for vector-based semantic search and provides accurate, citation-backed answers.

## Key Features

✅ **RAG-Powered Search** - Uses vector embeddings and semantic search for accurate document retrieval  
✅ **Detailed Token Tracking** - Leverages SemanticKernelRAGService for comprehensive token usage metrics  
✅ **Citation Support** - Always provides source references for retrieved information  
✅ **Multi-Document Synthesis** - Combines information from multiple sources  
✅ **Coverage Analysis** - Checks knowledge base coverage for specific topics  
✅ **Configurable Relevance** - Adjustable thresholds for search result quality  

## Architecture

### Components

```
KnowledgeBase.Agent/
├── Configuration/
│   └── KnowledgeBaseAgentOptions.cs      # Agent configuration
├── Services/
│   └── Agents/
│       └── KnowledgeBaseAgent.cs         # Main agent implementation
├── Plugins/
│   └── KnowledgeBasePlugin.cs            # Semantic Kernel functions
└── Extensions/
    └── KnowledgeBaseAgentCollectionExtensions.cs  # DI registration
```

### Key Classes

#### 1. KnowledgeBaseAgent
Main agent class that orchestrates RAG-powered knowledge base lookups.

**Responsibilities:**
- Perform vector search on knowledge base index
- Execute RAG completions with token tracking
- Manage conversation context and history
- Provide structured responses with citations

**Dependencies:**
- `ISemanticKernelService` - For kernel creation
- `TokenManagementHelper` - For RAG completions
- `IVectorSearchService` - For document retrieval
- `KnowledgeBasePlugin` - For specialized functions

#### 2. KnowledgeBasePlugin
Semantic Kernel plugin providing specialized knowledge base functions.

**Available Functions:**

| Function | Purpose | Use Case |
|----------|---------|----------|
| `search_knowledge_base` | Search for relevant documents | "Find information about deployment best practices" |
| `get_document` | Retrieve specific document by name/ID | "Get the Azure Security Guide document" |
| `find_related_documents` | Discover related content | "Find documents related to container security" |
| `summarize_documents` | Create multi-document summary | "Summarize all documents about Kubernetes" |
| `check_knowledge_coverage` | Verify documentation coverage | "Check if we have docs on Azure Policy" |

#### 3. KnowledgeBaseAgentOptions
Configuration class for agent behavior.

**Key Settings:**
```json
{
  "KnowledgeBaseAgent": {
    "EnableRag": true,
    "MinimumRelevanceScore": 0.75,
    "MaxRagResults": 5,
    "MaxCompletionTokens": 4000,
    "Temperature": 0.3,
    "ModelName": "gpt-4o",
    "IncludeConversationHistory": true,
    "KnowledgeBaseIndexName": "knowledge-base-index",
    "EnableSemanticSearch": true,
    "CacheDurationMinutes": 60
  }
}
```

## RAG Integration

### How It Works

1. **User Query** → Agent receives question
2. **Vector Search** → Searches indexed documents using semantic similarity
3. **Result Filtering** → Filters by relevance score threshold
4. **Context Building** → Combines search results with conversation history
5. **RAG Completion** → Generates answer using SemanticKernelRAGService
6. **Token Tracking** → Records detailed token metrics
7. **Response** → Returns answer with citations

### RAG Completion Flow

```csharp
// 1. Perform vector search
var searchResults = await _vectorSearch.SearchAsync(
    indexName: "knowledge-base-index",
    query: userQuestion,
    topK: 5,
    minRelevanceScore: 0.75,
    useSemanticSearch: true);

// 2. Build RAG request
var ragRequest = new ChatCompletionRequest
{
    SystemPrompt = "You are a Knowledge Base Expert...",
    UserPrompt = userQuestion,
    RagResults = searchResults,
    ConversationContext = conversationContext,
    ModelName = "gpt-4o",
    Temperature = 0.3,
    MaxTokens = 4000
};

// 3. Execute RAG completion
var response = await _tokenHelper.GetRagCompletionAsync(ragRequest);

// Response includes:
// - response.Response (AI-generated answer)
// - response.TokenMetrics.TotalTokens
// - response.TokenMetrics.RagContextTokens
// - response.TokenMetrics.EstimatedCost
```

### Token Tracking

The Knowledge Base Agent provides detailed token breakdown:

```csharp
{
  "totalTokens": 2847,
  "systemPromptTokens": 245,
  "ragContextTokens": 1523,  // Tokens from retrieved documents
  "conversationHistoryTokens": 412,
  "userPromptTokens": 89,
  "completionTokens": 578,
  "estimatedCost": 0.0142,
  "ragResultCount": 5
}
```

## Usage Examples

### Example 1: Basic Knowledge Base Search

```csharp
// In your agent or service
public async Task<string> SearchKnowledgeBase(string question)
{
    var task = new AgentTask
    {
        TaskId = Guid.NewGuid().ToString(),
        Prompt = question,
        ConversationId = "session-123"
    };

    var memory = new SharedMemory();
    var response = await knowledgeBaseAgent.ProcessAsync(task, memory);

    return response.Content; // Includes answer with citations
}
```

### Example 2: Using Plugin Functions

```csharp
// Search knowledge base
var searchResult = await knowledgeBasePlugin.SearchKnowledgeBaseAsync(
    query: "How do I configure Azure Key Vault for production?",
    maxResults: 5);

// Get specific document
var document = await knowledgeBasePlugin.GetDocumentAsync(
    documentId: "azure-key-vault-guide");

// Find related content
var related = await knowledgeBasePlugin.FindRelatedDocumentsAsync(
    topic: "secrets management",
    maxResults: 5);

// Summarize multiple documents
var summary = await knowledgeBasePlugin.SummarizeDocumentsAsync(
    topic: "container security",
    focusArea: "vulnerability scanning");

// Check coverage
var coverage = await knowledgeBasePlugin.CheckKnowledgeCoverageAsync(
    topic: "Kubernetes best practices");
```

### Example 3: Configuration in Program.cs

```csharp
// Chat/Program.cs
using Platform.Engineering.Copilot.KnowledgeBase.Agent.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Knowledge Base Agent
builder.Services.AddKnowledgeBaseAgent(builder.Configuration);

// Or with custom options
builder.Services.AddKnowledgeBaseAgent(options =>
{
    options.EnableRag = true;
    options.MinimumRelevanceScore = 0.80; // Higher threshold
    options.MaxRagResults = 3; // Fewer results
    options.Temperature = 0.2; // More deterministic
    options.KnowledgeBaseIndexName = "custom-kb-index";
});
```

### Example 4: Mcp Integration (Conditional Loading)

```csharp
// Mcp/Program.cs
if (agentConfig.IsAgentEnabled("KnowledgeBase"))
{
    builder.Services.AddKnowledgeBaseAgent(builder.Configuration);
    logger.LogInformation("✅ Knowledge Base agent enabled");
}
```

## Configuration

### Required Configuration

```json
{
  "KnowledgeBaseAgent": {
    "EnableRag": true,
    "KnowledgeBaseIndexName": "knowledge-base-index",
    "MinimumRelevanceScore": 0.75,
    "MaxRagResults": 5
  },
  "AzureAISearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "ApiKey": "your-api-key",
    "IndexName": "knowledge-base-index"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-openai.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o"
  }
}
```

### Optional Configuration

```json
{
  "KnowledgeBaseAgent": {
    "MaxCompletionTokens": 4000,
    "Temperature": 0.3,
    "ModelName": "gpt-4o",
    "IncludeConversationHistory": true,
    "MaxConversationHistoryMessages": 10,
    "EnableSemanticSearch": true,
    "CacheDurationMinutes": 60
  }
}
```

## Plugin Functions Reference

### 1. search_knowledge_base
Search for relevant documents and information.

**Parameters:**
- `query` (string, required): Search query or question
- `maxResults` (int, optional): Maximum results to return (default: 5)

**Returns:** Formatted search results with citations and relevance scores

**Example:**
```
Query: "How do I implement zero-trust security?"

Results:
- Zero Trust Security Architecture Guide (Relevance: 92%)
- Azure AD Zero Trust Implementation (Relevance: 87%)
- Network Segmentation Best Practices (Relevance: 81%)
```

### 2. get_document
Retrieve a specific document by name or ID.

**Parameters:**
- `documentId` (string, required): Document name or identifier

**Returns:** Full document content organized by sections

**Example:**
```
Document: "azure-security-guide"

Sections:
- Overview
- Identity and Access Management
- Network Security
- Data Protection
```

### 3. find_related_documents
Discover documents related to a topic.

**Parameters:**
- `topic` (string, required): Topic or document to find relations for
- `maxResults` (int, optional): Maximum documents to return (default: 5)

**Returns:** List of related documents with relevance scores

### 4. summarize_documents
Create a summary across multiple documents.

**Parameters:**
- `topic` (string, required): Topic to summarize
- `focusArea` (string, optional): Specific aspect to emphasize

**Returns:** Structured summary with source citations

### 5. check_knowledge_coverage
Check documentation coverage for a topic.

**Parameters:**
- `topic` (string, required): Topic to check coverage for

**Returns:** Coverage analysis with recommendations

## Token Management

### Cost Optimization

The Knowledge Base Agent uses RAG efficiently to minimize costs:

1. **Relevance Filtering** - Only includes high-quality results
2. **Token Limits** - Respects MaxRagResults to control context size
3. **Conversation Truncation** - Uses ChatBuilder for optimal history
4. **Caching** - Caches search results for repeated queries

### Cost Calculation

```csharp
// Automatic cost estimation based on model pricing
{
  "model": "gpt-4o",
  "totalTokens": 2847,
  "inputTokens": 2269,
  "outputTokens": 578,
  "inputCost": 0.0114,   // $0.005 per 1K input tokens
  "outputCost": 0.0289,  // $0.015 per 1K output tokens
  "estimatedCost": 0.0403
}
```

## Best Practices

### 1. Index Structure
Ensure your Azure AI Search index has these fields:
- `id` - Unique document identifier
- `content` - Main document text
- `sourceDocument` - Document name/path
- `section` - Section or chapter name
- `metadata` - Additional context (author, date, version)
- `contentVector` - Embedding vector for semantic search

### 2. Relevance Tuning
- Start with `MinimumRelevanceScore: 0.75`
- Increase to 0.80+ for high-precision needs
- Decrease to 0.60-0.70 for broader coverage

### 3. Result Count
- `MaxRagResults: 3-5` - Standard use cases
- `MaxRagResults: 1-2` - Quick answers, low latency
- `MaxRagResults: 7-10` - Comprehensive research

### 4. Temperature Settings
- `Temperature: 0.2-0.3` - Factual, citation-heavy responses
- `Temperature: 0.5-0.7` - Balanced answers with some creativity
- `Temperature: 0.8+` - Not recommended (hallucination risk)

### 5. Conversation History
- Enable for follow-up questions
- Disable for one-shot queries
- Limit to 5-10 messages for cost control

## Troubleshooting

### Common Issues

#### 1. No Search Results
**Problem:** Vector search returns no results

**Solutions:**
- Lower `MinimumRelevanceScore` threshold
- Verify index name matches configuration
- Check if documents are indexed
- Ensure embeddings are generated

#### 2. Irrelevant Results
**Problem:** Search results don't match query

**Solutions:**
- Increase `MinimumRelevanceScore`
- Enable semantic search
- Improve document chunking strategy
- Update embedding model

#### 3. High Token Costs
**Problem:** Token usage exceeds budget

**Solutions:**
- Reduce `MaxRagResults`
- Disable `IncludeConversationHistory`
- Lower `MaxCompletionTokens`
- Implement result caching

#### 4. RAG Service Unavailable
**Problem:** Agent fails with RAG disabled error

**Solutions:**
- Verify `IVectorSearchService` is registered
- Check Azure AI Search configuration
- Ensure `EnableRag: true` in settings
- Validate API keys and endpoints

## Integration with Other Agents

### Compliance Agent
Knowledge Base Agent can provide compliance documentation to Compliance Agent:

```csharp
// Compliance Agent requests NIST documentation
var nistDocs = await knowledgeBaseAgent.ProcessAsync(new AgentTask
{
    Prompt = "Find all NIST 800-53 control documentation"
});
```

### Infrastructure Agent
Infrastructure Agent uses Knowledge Base for Azure best practices:

```csharp
// Infrastructure Agent needs deployment guidance
var guidance = await knowledgeBaseAgent.ProcessAsync(new AgentTask
{
    Prompt = "Best practices for Azure Kubernetes Service production deployment"
});
```

### Security Agent
Security Agent queries for threat intelligence and security guides:

```csharp
// Security Agent needs vulnerability information
var vulnInfo = await knowledgeBaseAgent.ProcessAsync(new AgentTask
{
    Prompt = "Latest CVE information for container runtimes"
});
```

## Performance Metrics

### Typical Response Times
- Vector Search: 100-300ms
- RAG Completion: 1-3 seconds
- Total Response: 1.5-3.5 seconds

### Token Usage Patterns
- Average Query: 2,000-3,000 tokens
- Simple Question: 500-1,500 tokens
- Complex Research: 4,000-6,000 tokens

## Future Enhancements

Planned improvements for the Knowledge Base Agent:

1. **Multi-Index Search** - Search across multiple knowledge bases
2. **Document Upload** - Add new documents via API
3. **Auto-Indexing** - Automatic document processing pipeline
4. **Feedback Loop** - Learn from user feedback on results
5. **Query Expansion** - Automatic query reformulation
6. **Result Ranking** - Custom ranking algorithms
7. **Hybrid Search** - Combine keyword and semantic search

## Summary

The Knowledge Base Agent provides:

✅ RAG-powered document retrieval with Azure AI Search  
✅ Detailed token tracking and cost management  
✅ Citation-backed answers from indexed documentation  
✅ Flexible configuration for different use cases  
✅ Integration with other specialized agents  
✅ Comprehensive plugin functions for knowledge operations  

For questions or issues, see the main [AGENT-RAG-INTEGRATION-EXAMPLES.md](AGENT-RAG-INTEGRATION-EXAMPLES.md) documentation.
