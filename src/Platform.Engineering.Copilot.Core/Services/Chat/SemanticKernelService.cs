using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Platform.Engineering.Copilot.Core.Interfaces.Chat;
using Platform.Engineering.Copilot.Core.Models.Agents;

namespace Platform.Engineering.Copilot.Core.Services.Chat;

/// <summary>
/// Service that creates Semantic Kernel instances for the multi-agent system.
/// Each agent receives its own isolated kernel with specialized configuration.
/// This service handles Azure OpenAI/OpenAI configuration and kernel creation.
/// </summary>
public class SemanticKernelService : ISemanticKernelService
{
    private readonly ILogger<SemanticKernelService> _logger;
    private readonly IConfiguration _configuration;

    public SemanticKernelService(ILogger<SemanticKernelService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        _logger.LogInformation("SemanticKernelService initialized for multi-agent kernel creation");
    }

    /// <summary>
    /// Create a specialized kernel for a specific agent type.
    /// This is the PRIMARY method used by the multi-agent system.
    /// Each agent (Orchestrator, Infrastructure, Compliance, Cost, Environment, Discovery, ServiceCreation)
    /// calls this to get its own isolated kernel instance.
    /// </summary>
    /// <param name="agentType">The type of agent to create a kernel for</param>
    /// <returns>A specialized kernel instance configured with Azure OpenAI or OpenAI</returns>
    public Kernel CreateSpecializedKernel(AgentType agentType)
    {
        _logger.LogInformation("🤖 Creating specialized kernel for agent: {AgentType}", agentType);
        
        var builder = Kernel.CreateBuilder();
        
        // Get configuration for AI service
        var azureOpenAIEndpoint = _configuration.GetValue<string>("Gateway:AzureOpenAI:Endpoint");
        var azureOpenAIApiKey = _configuration.GetValue<string>("Gateway:AzureOpenAI:ApiKey");
        var azureOpenAIDeployment = _configuration.GetValue<string>("Gateway:AzureOpenAI:DeploymentName");
        var useManagedIdentity = _configuration.GetValue<bool>("Gateway:AzureOpenAI:UseManagedIdentity");
        var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        // Try Azure OpenAI first (recommended for production)
        if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && 
            !string.IsNullOrEmpty(azureOpenAIDeployment) &&
            (!string.IsNullOrEmpty(azureOpenAIApiKey) || useManagedIdentity))
        {
            try
            {
                if (useManagedIdentity)
                {
                    _logger.LogInformation("Using Azure OpenAI with Managed Identity for {AgentType}", agentType);
                    builder.AddAzureOpenAIChatCompletion(
                        azureOpenAIDeployment, 
                        azureOpenAIEndpoint, 
                        new DefaultAzureCredential());
                }
                else
                {
                    _logger.LogInformation("Using Azure OpenAI with API Key for {AgentType}", agentType);
                    builder.AddAzureOpenAIChatCompletion(
                        azureOpenAIDeployment, 
                        azureOpenAIEndpoint, 
                        azureOpenAIApiKey!);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure Azure OpenAI for {AgentType}, trying OpenAI", agentType);
            }
        }
        
        // Fallback to OpenAI (for development)
        if (!string.IsNullOrEmpty(openAIApiKey) && openAIApiKey != "demo-key")
        {
            try
            {
                _logger.LogInformation("Using OpenAI for {AgentType}", agentType);
                builder.AddOpenAIChatCompletion("gpt-4", openAIApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure OpenAI for {AgentType}", agentType);
            }
        }
        
        var kernel = builder.Build();
        
        // Verify that chat completion service is available
        try
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            _logger.LogInformation("✅ Created specialized kernel for {AgentType} with chat completion service", agentType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "⚠️ Created kernel for {AgentType} but no chat completion service available. " +
                "Configure Azure OpenAI in appsettings.json or set OPENAI_API_KEY environment variable.", 
                agentType);
        }
        
        return kernel;
    }
}
