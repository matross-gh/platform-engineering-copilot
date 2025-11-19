using Platform.Engineering.Copilot.Core.Interfaces.TokenManagement;
using Platform.Engineering.Copilot.Core.Models.TokenManagement;
using SharpToken;

namespace Platform.Engineering.Copilot.Core.Services.TokenManagement;

/// <summary>
/// Service for counting tokens using SharpToken library
/// </summary>
public class TokenCounter : ITokenCounter
{
    private readonly Dictionary<string, GptEncoding> _encoderCache = new();
    private readonly object _cacheLock = new();

    // Model configurations
    private static readonly Dictionary<string, ModelConfig> ModelConfigs = new()
    {
        ["gpt-4o"] = new ModelConfig { Encoding = "o200k_base", MaxContext = 128000, MaxCompletion = 16384 },
        ["gpt-4o-mini"] = new ModelConfig { Encoding = "o200k_base", MaxContext = 128000, MaxCompletion = 16384 },
        ["gpt-4-turbo"] = new ModelConfig { Encoding = "cl100k_base", MaxContext = 128000, MaxCompletion = 4096 },
        ["gpt-4-turbo-preview"] = new ModelConfig { Encoding = "cl100k_base", MaxContext = 128000, MaxCompletion = 4096 },
        ["gpt-4"] = new ModelConfig { Encoding = "cl100k_base", MaxContext = 8192, MaxCompletion = 4096 },
        ["gpt-4-32k"] = new ModelConfig { Encoding = "cl100k_base", MaxContext = 32768, MaxCompletion = 4096 },
        ["gpt-3.5-turbo"] = new ModelConfig { Encoding = "cl100k_base", MaxContext = 16385, MaxCompletion = 4096 },
        ["gpt-3.5-turbo-16k"] = new ModelConfig { Encoding = "cl100k_base", MaxContext = 16385, MaxCompletion = 4096 }
    };

    /// <inheritdoc/>
    public int CountTokens(string text, string modelName = "gpt-4o")
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var encoder = GetEncoder(modelName);
        return encoder.CountTokens(text);
    }

    /// <inheritdoc/>
    public List<int> EncodeText(string text, string modelName = "gpt-4o")
    {
        if (string.IsNullOrEmpty(text))
            return new List<int>();

        var encoder = GetEncoder(modelName);
        return encoder.Encode(text);
    }

    /// <inheritdoc/>
    public string DecodeTokens(List<int> tokens, string modelName = "gpt-4o")
    {
        if (tokens == null || tokens.Count == 0)
            return string.Empty;

        var encoder = GetEncoder(modelName);
        return encoder.Decode(tokens);
    }

    /// <inheritdoc/>
    public TokenEstimate EstimateTokens(
        string systemPrompt,
        string userMessage,
        List<string> ragContext,
        List<string> conversationHistory,
        string modelName = "gpt-4o")
    {
        var estimate = new TokenEstimate
        {
            ModelName = modelName,
            MaxContextWindow = GetMaxContextWindow(modelName),
            ReservedCompletionTokens = 4000 // Default reservation
        };

        // Count system prompt tokens
        estimate.SystemPromptTokens = CountTokens(systemPrompt ?? string.Empty, modelName);

        // Count user message tokens
        estimate.UserMessageTokens = CountTokens(userMessage ?? string.Empty, modelName);

        // Count RAG context tokens
        if (ragContext != null && ragContext.Any())
        {
            foreach (var item in ragContext)
            {
                var tokens = CountTokens(item, modelName);
                estimate.RagContextItemTokens.Add(tokens);
                estimate.RagContextTokens += tokens;
            }
        }

        // Count conversation history tokens
        if (conversationHistory != null && conversationHistory.Any())
        {
            foreach (var message in conversationHistory)
            {
                var tokens = CountTokens(message, modelName);
                estimate.ConversationHistoryItemTokens.Add(tokens);
                estimate.ConversationHistoryTokens += tokens;
            }
        }

        return estimate;
    }

    /// <inheritdoc/>
    public int GetMaxContextWindow(string modelName)
    {
        // Normalize model name (remove deployment-specific suffixes)
        var normalizedName = NormalizeModelName(modelName);
        
        if (ModelConfigs.TryGetValue(normalizedName, out var config))
        {
            return config.MaxContext;
        }

        // Default to GPT-4o if unknown
        return ModelConfigs["gpt-4o"].MaxContext;
    }

    /// <inheritdoc/>
    public int GetMaxCompletionTokens(string modelName)
    {
        // Normalize model name
        var normalizedName = NormalizeModelName(modelName);
        
        if (ModelConfigs.TryGetValue(normalizedName, out var config))
        {
            return config.MaxCompletion;
        }

        // Default to GPT-4o if unknown
        return ModelConfigs["gpt-4o"].MaxCompletion;
    }

    /// <summary>
    /// Get or create encoder for a specific model
    /// </summary>
    private GptEncoding GetEncoder(string modelName)
    {
        var normalizedName = NormalizeModelName(modelName);

        lock (_cacheLock)
        {
            if (_encoderCache.TryGetValue(normalizedName, out var encoder))
            {
                return encoder;
            }

            // Get encoding name from model config
            if (!ModelConfigs.TryGetValue(normalizedName, out var config))
            {
                // Default to GPT-4o encoding
                config = ModelConfigs["gpt-4o"];
            }

            // Create encoder using SharpToken
            encoder = GptEncoding.GetEncoding(config.Encoding);
            _encoderCache[normalizedName] = encoder;

            return encoder;
        }
    }

    /// <summary>
    /// Normalize model name to handle deployment-specific names
    /// </summary>
    private string NormalizeModelName(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            return "gpt-4o";

        // Convert to lowercase
        var normalized = modelName.ToLowerInvariant();

        // Handle common variations
        if (normalized.Contains("gpt-4o-mini"))
            return "gpt-4o-mini";
        if (normalized.Contains("gpt-4o"))
            return "gpt-4o";
        if (normalized.Contains("gpt-4-turbo"))
            return "gpt-4-turbo";
        if (normalized.Contains("gpt-4-32k"))
            return "gpt-4-32k";
        if (normalized.Contains("gpt-4"))
            return "gpt-4";
        if (normalized.Contains("gpt-3.5-turbo-16k"))
            return "gpt-3.5-turbo-16k";
        if (normalized.Contains("gpt-3.5") || normalized.Contains("gpt-35"))
            return "gpt-3.5-turbo";

        // Default to GPT-4o for unknown models
        return "gpt-4o";
    }

    /// <summary>
    /// Model configuration
    /// </summary>
    private class ModelConfig
    {
        public string Encoding { get; set; } = string.Empty;
        public int MaxContext { get; set; }
        public int MaxCompletion { get; set; }
    }
}
