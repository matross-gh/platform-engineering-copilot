# ChatBuilder Usage Guide

## Overview

**ChatBuilder** is a token-aware conversation history management service that builds formatted chat history within token limits. It works alongside the existing token management system to ensure conversation context fits within model constraints.

## Key Features

✅ **Token-Aware**: Builds history within specified token limits using accurate BPE tokenization  
✅ **Formatted Output**: Returns conversation as formatted string ready for LLM context  
✅ **Metadata Rich**: Provides token count, utilization percentage, truncation info  
✅ **Vector Search Ready**: Creates optimized context snippets for RAG search  
✅ **Flexible Configuration**: Multiple factory methods for different use cases  
✅ **Smart Truncation**: Keeps newest messages, ensures minimum message count  

---

## Core Components

### 1. ChatHistoryResult

Return value containing formatted history and metadata:

```csharp
public class ChatHistoryResult
{
    public string FormattedHistory { get; set; }      // Ready-to-use formatted string
    public int TokenCount { get; set; }               // Actual token count
    public int MessageCount { get; set; }             // Number of messages included
    public int TruncatedMessageCount { get; set; }    // Number of messages removed
    public double UtilizationPercentage { get; set; } // Token usage percentage
    public List<MessageSnapshot> Messages { get; set; } // Included messages
    public int MaxTokens { get; set; }                // Maximum allowed tokens
    public string ModelName { get; set; }             // Target model name
    public bool WasTruncated { get; }                 // Whether truncation occurred
    
    // Utility methods
    public string GetDebugSummary();
    public string[] GetLines();
    public bool IsEmpty { get; }
}
```

### 2. ChatBuilderOptions

Configuration for history building:

```csharp
public class ChatBuilderOptions
{
    public int MaxTokens { get; set; } = 5000;
    public string ModelName { get; set; } = "gpt-4o";
    public int ReservedTokens { get; set; } = 0;
    public bool IncludeSystemMessages { get; set; } = false;
    public int MinimumMessages { get; set; } = 2;
    public string MessageSeparator { get; set; } = "\n";
    public string FormatTemplate { get; set; } = "{role}: {content}";
    public bool IncludeTimestamps { get; set; } = false;
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public bool OldestFirst { get; set; } = false;
    
    // Factory methods
    public static ChatBuilderOptions CreateDefault(string? modelName = null);
    public static ChatBuilderOptions CreateForVectorSearch(int maxMessages = 5, string? modelName = null);
    public static ChatBuilderOptions CreateForDebugging(string? modelName = null);
}
```

### 3. ChatBuilder Service

Main service for building conversation history:

```csharp
public class ChatBuilder
{
    // Build history from ConversationContext
    public ChatHistoryResult BuildHistory(
        ConversationContext context,
        ChatBuilderOptions? options = null);
    
    // Build history from explicit message list
    public ChatHistoryResult BuildHistory(
        List<MessageSnapshot> messages,
        ChatBuilderOptions? options = null);
    
    // Get search context for vector search (optimized, limited)
    public string GetSearchContext(
        ConversationContext context,
        int maxMessages = 5);
    
    // Append new message and rebuild
    public ChatHistoryResult AppendMessage(
        ChatHistoryResult existingHistory,
        MessageSnapshot newMessage,
        ChatBuilderOptions? options = null);
    
    // Get conversation summary (last N exchanges)
    public string GetConversationSummary(
        ConversationContext context,
        int exchangeCount = 3);
}
```

---

## Usage Examples

### Example 1: Basic History Building

```csharp
// Inject ChatBuilder or use TokenManagementHelper
private readonly TokenManagementHelper _tokenManagement;

public async Task ProcessChatAsync(ConversationContext context)
{
    // Build history with default settings
    var historyResult = _tokenManagement.BuildChatHistory(context);
    
    Console.WriteLine($"History: {historyResult.FormattedHistory}");
    Console.WriteLine($"Tokens: {historyResult.TokenCount}");
    Console.WriteLine($"Utilization: {historyResult.UtilizationPercentage:F1}%");
    
    if (historyResult.WasTruncated)
    {
        Console.WriteLine($"Truncated {historyResult.TruncatedMessageCount} messages");
    }
}
```

### Example 2: Custom Token Limits

```csharp
// Create custom options for smaller context
var options = new ChatBuilderOptions
{
    MaxTokens = 2000,           // Smaller limit
    ModelName = "gpt-4o",
    MinimumMessages = 3,        // Keep at least 3 messages
    IncludeTimestamps = true    // Add timestamps
};

var historyResult = _tokenManagement.BuildChatHistory(context, options);
```

### Example 3: Vector Search Context

```csharp
// Get optimized context for RAG search (limited to last 5 messages)
var searchContext = _tokenManagement.GetSearchContext(context, maxMessages: 5);

// Use in vector search
var searchResults = await _vectorStore.SearchAsync(searchContext, userQuery);
```

### Example 4: Factory Methods

```csharp
// Default configuration
var defaultOptions = ChatBuilderOptions.CreateDefault("gpt-4o");

// Optimized for vector search (compact, no system messages)
var searchOptions = ChatBuilderOptions.CreateForVectorSearch(maxMessages: 5);

// Debugging (includes timestamps, all messages)
var debugOptions = ChatBuilderOptions.CreateForDebugging("gpt-4o");

var result = chatBuilder.BuildHistory(context, debugOptions);
Console.WriteLine(result.GetDebugSummary());
```

### Example 5: Integration with IntelligentChatService

The ChatBuilder is automatically integrated into `IntelligentChatService`:

```csharp
// In IntelligentChatService (already implemented)
private async Task AddMessageToHistoryAsync(
    ConversationContext context,
    MessageSnapshot message,
    CancellationToken cancellationToken = default)
{
    lock (_conversationLock)
    {
        context.MessageHistory.Add(message);
        
        // ChatBuilder automatically manages token limits
        if (_tokenManagement.IsEnabled)
        {
            var options = ChatBuilderOptions.CreateDefault(_tokenManagement.Options.DefaultModelName);
            var historyResult = _tokenManagement.BuildChatHistory(context, options);
            
            if (historyResult.WasTruncated)
            {
                _logger.LogInformation(
                    "💾 Chat history optimized: {Messages} messages, {Tokens} tokens",
                    historyResult.MessageCount,
                    historyResult.TokenCount);
                
                // Update context with optimized messages
                context.MessageHistory = historyResult.Messages;
            }
        }
    }
}
```

### Example 6: Building System Prompts

```csharp
// Build complete prompt with history
var historyResult = _tokenManagement.BuildChatHistory(context);

var systemPrompt = $@"
You are a platform engineering assistant.

CONVERSATION HISTORY:
{historyResult.FormattedHistory}

Current tokens used: {historyResult.TokenCount} / {historyResult.MaxTokens}
";

// Use in LLM call
var response = await llm.GenerateAsync(systemPrompt, userMessage);
```

### Example 7: Debug Information

```csharp
var historyResult = _tokenManagement.BuildChatHistory(
    context,
    ChatBuilderOptions.CreateForDebugging());

// Get comprehensive debug output
Console.WriteLine(historyResult.GetDebugSummary());

// Output:
// ChatHistory Summary
// ------------------
// Messages: 15 included, 5 truncated (20 total)
// Tokens: 3,842 / 5,000 (76.8% utilized)
// Model: gpt-4o
// Status: Truncated
```

---

## Configuration

### appsettings.tokenmanagement.json

```json
{
  "TokenManagement": {
    "Enabled": true,
    "DefaultModelName": "gpt-4o",
    "ConversationHistory": {
      "MaxTokens": 5000,
      "ChatBuilder": {
        "DefaultMaxTokens": 5000,
        "ReservedTokens": 0,
        "IncludeSystemMessages": false,
        "MinimumMessages": 2,
        "MessageSeparator": "\n",
        "FormatTemplate": "{role}: {content}",
        "IncludeTimestamps": false,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss",
        "OldestFirst": false
      }
    }
  }
}
```

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `DefaultMaxTokens` | 5000 | Maximum tokens for conversation history |
| `ReservedTokens` | 0 | Tokens to reserve (reduces available tokens) |
| `IncludeSystemMessages` | false | Include system messages in history |
| `MinimumMessages` | 2 | Minimum messages to keep even if exceeds limit |
| `MessageSeparator` | "\n" | Separator between messages in output |
| `FormatTemplate` | "{role}: {content}" | Template for formatting messages |
| `IncludeTimestamps` | false | Add timestamps to messages |
| `TimestampFormat` | "yyyy-MM-dd HH:mm:ss" | Format for timestamps |
| `OldestFirst` | false | Process oldest messages first (vs newest) |

---

## Message Format Templates

The `FormatTemplate` supports these placeholders:

- `{role}` - Message role (user, assistant, system)
- `{content}` - Message content
- `{timestamp}` - Message timestamp (if enabled)

### Format Examples

```csharp
// Simple format
"{role}: {content}"
// Output: "user: What is Docker?"

// With timestamps
"[{timestamp}] {role}: {content}"
// Output: "[2024-01-15 10:30:45] user: What is Docker?"

// Structured format
"{role}\n{content}\n---"
// Output:
// user
// What is Docker?
// ---
```

---

## Algorithm Details

### History Building Process

1. **Filter Messages**: Remove system messages if `IncludeSystemMessages = false`
2. **Calculate Available Tokens**: `MaxTokens - ReservedTokens`
3. **Iterate Messages**: Process from newest to oldest (or vice versa based on `OldestFirst`)
4. **Count Tokens**: Use `ITokenCounter` to count each message
5. **Check Limits**: Stop when token limit reached (but keep `MinimumMessages`)
6. **Format Output**: Apply `FormatTemplate` and join with `MessageSeparator`
7. **Return Result**: Include formatted string + metadata

### Token Counting

ChatBuilder uses **SharpToken** for accurate BPE tokenization:

```csharp
// Each message is formatted first, then counted
var formatted = FormatMessage(message, options);
var tokens = _tokenCounter.CountTokens(formatted, modelName);
```

This ensures the token count matches what the LLM will see.

---

## Best Practices

### 1. Use Factory Methods

```csharp
// ✅ Good - Use appropriate factory
var options = ChatBuilderOptions.CreateForVectorSearch(5);

// ❌ Avoid - Manual configuration when factory exists
var options = new ChatBuilderOptions
{
    MaxTokens = 1000,
    IncludeSystemMessages = false,
    MinimumMessages = 2,
    // ... lots of config
};
```

### 2. Reserve Tokens for Completion

```csharp
// ✅ Good - Reserve tokens for LLM response
var options = new ChatBuilderOptions
{
    MaxTokens = 10000,
    ReservedTokens = 4000  // Reserve 4k for response
};
// Available for history: 6,000 tokens
```

### 3. Check Truncation Status

```csharp
var result = chatBuilder.BuildHistory(context);

if (result.WasTruncated)
{
    // Inform user about lost context
    await NotifyUserAsync($"Conversation truncated ({result.TruncatedMessageCount} messages removed)");
}
```

### 4. Monitor Token Utilization

```csharp
if (result.UtilizationPercentage > 90)
{
    _logger.LogWarning("⚠️ High token usage: {Pct:F1}%", result.UtilizationPercentage);
}
```

### 5. Use Search Context for RAG

```csharp
// ✅ Good - Optimized context for search
var searchContext = chatBuilder.GetSearchContext(context, maxMessages: 5);

// ❌ Avoid - Full history too large for search
var searchContext = chatBuilder.BuildHistory(context).FormattedHistory;
```

---

## Performance Considerations

### Token Counting Performance

- **TokenCounter is a singleton** - Encoders are cached
- **Counting is fast** - ~1-2ms per message
- **History building** - O(n) where n = message count

### Memory Usage

- **ChatHistoryResult** - Stores references to messages (not copies)
- **Formatted string** - Only created once at the end
- **No caching** - Each call rebuilds (stateless)

### Concurrency

- **ChatBuilder is scoped** - Safe for concurrent requests (different instances)
- **ITokenCounter is thread-safe** - Singleton can be used concurrently

---

## Integration Points

### 1. IntelligentChatService

Automatically uses ChatBuilder for conversation history management.

### 2. TokenManagementHelper

Provides convenience methods:

```csharp
_tokenManagement.BuildChatHistory(context);
_tokenManagement.GetSearchContext(context);
```

### 3. RAG Search

Use search context for vector queries:

```csharp
var searchContext = chatBuilder.GetSearchContext(context);
var results = await vectorStore.SearchAsync(searchContext, query);
```

### 4. Prompt Construction

Build complete prompts with token-aware history:

```csharp
var history = chatBuilder.BuildHistory(context);
var prompt = $"HISTORY:\n{history.FormattedHistory}\n\nUSER: {userMessage}";
```

---

## Troubleshooting

### Issue: History is Empty

**Cause**: All messages filtered out or token limit too low

```csharp
if (result.IsEmpty)
{
    // Check configuration
    Console.WriteLine($"MaxTokens: {result.MaxTokens}");
    Console.WriteLine($"MessageCount: {result.MessageCount}");
    
    // Increase limits
    var options = new ChatBuilderOptions { MaxTokens = 10000 };
}
```

### Issue: Too Many Messages Truncated

**Cause**: Token limit too restrictive

```csharp
// Increase token limit
var options = new ChatBuilderOptions
{
    MaxTokens = 10000,  // Increased from 5000
    MinimumMessages = 5  // Keep more messages
};
```

### Issue: System Messages Missing

**Cause**: `IncludeSystemMessages = false` (default)

```csharp
// Include system messages
var options = new ChatBuilderOptions
{
    IncludeSystemMessages = true
};
```

---

## Examples by Use Case

### Use Case 1: Multi-Turn Conversation

```csharp
// Keep recent conversation within token budget
var options = ChatBuilderOptions.CreateDefault("gpt-4o");
var history = chatBuilder.BuildHistory(context, options);

var prompt = $@"
You are a helpful assistant.

Previous conversation:
{history.FormattedHistory}

User: {userMessage}
";
```

### Use Case 2: RAG with Conversation Context

```csharp
// Get compact search context
var searchContext = chatBuilder.GetSearchContext(context, maxMessages: 3);

// Search knowledge base with conversation context
var query = $"{searchContext}\n\nCurrent question: {userMessage}";
var searchResults = await knowledgeBase.SearchAsync(query);

// Build optimized RAG context
var ragContext = _tokenManagement.OptimizeRagContext(searchResults);

// Build final prompt with both
var history = chatBuilder.BuildHistory(context);
var prompt = $@"
CONVERSATION HISTORY:
{history.FormattedHistory}

KNOWLEDGE BASE CONTEXT:
{string.Join("\n\n", ragContext.Results.Select(r => r.Content))}

USER QUESTION:
{userMessage}
";
```

### Use Case 3: Debug Conversation Issues

```csharp
// Use debugging options
var debugOptions = ChatBuilderOptions.CreateForDebugging("gpt-4o");
var history = chatBuilder.BuildHistory(context, debugOptions);

// Log detailed information
_logger.LogInformation(history.GetDebugSummary());

// Examine each message
foreach (var message in history.Messages)
{
    _logger.LogDebug("{Role} ({Timestamp}): {Content}",
        message.Role,
        message.Timestamp,
        message.Content.Length > 100 ? message.Content.Substring(0, 100) + "..." : message.Content);
}
```

### Use Case 4: Multi-Agent with Context

```csharp
// Each agent gets relevant conversation history
var complianceContext = chatBuilder.GetSearchContext(context, maxMessages: 5);
var complianceResponse = await complianceAgent.ProcessAsync(userMessage, complianceContext);

var costContext = chatBuilder.GetSearchContext(context, maxMessages: 3);
var costResponse = await costAgent.ProcessAsync(userMessage, costContext);

// Orchestrator gets full history
var fullHistory = chatBuilder.BuildHistory(context);
var orchestratorResponse = await orchestrator.CombineAsync(
    fullHistory.FormattedHistory,
    complianceResponse,
    costResponse);
```

---

## Related Documentation

- [Token Management Guide](TOKEN-MANAGEMENT-COMPLETE.md) - Overall token management system
- [RAG Context Optimization](TOKEN-MANAGEMENT-INTEGRATION.md#rag-context-optimizer) - RAG optimization
- [Prompt Optimization](TOKEN-MANAGEMENT-INTEGRATION.md#prompt-optimizer) - Prompt optimization strategies

---

## Summary

ChatBuilder provides **token-aware conversation history management** for the Platform Engineering Copilot:

✅ **Automatic token counting** using SharpToken  
✅ **Smart truncation** keeping newest messages  
✅ **Flexible formatting** with templates  
✅ **Integrated with IntelligentChatService** for seamless use  
✅ **RAG-ready** with optimized search context  
✅ **Configurable** via appsettings.json  
✅ **Production-ready** with comprehensive logging  

Use ChatBuilder whenever you need to include conversation history in LLM prompts while staying within token limits!
