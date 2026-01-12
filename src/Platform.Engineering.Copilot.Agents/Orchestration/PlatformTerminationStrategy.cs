using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;

namespace Platform.Engineering.Copilot.Agents.Orchestration;

/// <summary>
/// Strategy for determining when to stop multi-agent orchestration
/// </summary>
public class PlatformTerminationStrategy
{
    private readonly ILogger<PlatformTerminationStrategy> _logger;

    /// <summary>
    /// Maximum number of consecutive agent responses allowed
    /// </summary>
    public int MaxConsecutiveResponses { get; set; } = 5;

    /// <summary>
    /// Maximum total responses in a conversation turn
    /// </summary>
    public int MaxTotalResponses { get; set; } = 10;

    public PlatformTerminationStrategy(ILogger<PlatformTerminationStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Determine if orchestration should terminate
    /// </summary>
    public Task<bool> ShouldTerminateAsync(
        IReadOnlyList<AgentResponse> responses,
        AgentConversationContext context,
        CancellationToken cancellationToken = default)
    {
        // No responses yet - don't terminate
        if (responses.Count == 0)
        {
            return Task.FromResult(false);
        }

        // Check max total responses
        if (responses.Count >= MaxTotalResponses)
        {
            _logger.LogInformation("Terminating: Max total responses ({Max}) reached", MaxTotalResponses);
            return Task.FromResult(true);
        }

        // Check last response for explicit termination signals
        var lastResponse = responses.Last();

        // If the last response was successful and doesn't require handoff, terminate
        if (lastResponse.Success && !lastResponse.RequiresHandoff)
        {
            _logger.LogDebug("Terminating: Successful response with no handoff");
            return Task.FromResult(true);
        }

        // If all responses failed, terminate
        if (responses.All(r => !r.Success))
        {
            _logger.LogWarning("Terminating: All {Count} responses failed", responses.Count);
            return Task.FromResult(true);
        }

        // Check for repeated agent loop (same agent responding multiple times in a row)
        if (responses.Count >= MaxConsecutiveResponses)
        {
            var lastAgents = responses.TakeLast(MaxConsecutiveResponses).Select(r => r.AgentName).Distinct().Count();
            if (lastAgents == 1)
            {
                _logger.LogWarning("Terminating: Same agent ({Agent}) responded {Max} times consecutively",
                    lastResponse.AgentName, MaxConsecutiveResponses);
                return Task.FromResult(true);
            }
        }

        // Check for completion keywords in content
        var lowerContent = lastResponse.Content.ToLowerInvariant();
        if (lowerContent.Contains("task completed") ||
            lowerContent.Contains("operation complete") ||
            lowerContent.Contains("successfully completed"))
        {
            _logger.LogDebug("Terminating: Completion keyword detected");
            return Task.FromResult(true);
        }

        // Default: continue orchestration
        return Task.FromResult(false);
    }
}
