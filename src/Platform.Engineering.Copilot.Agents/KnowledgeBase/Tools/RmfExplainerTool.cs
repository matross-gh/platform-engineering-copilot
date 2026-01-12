using Microsoft.Extensions.Logging;
using Platform.Engineering.Copilot.Agents.Common;
using Platform.Engineering.Copilot.Agents.KnowledgeBase.State;
using Platform.Engineering.Copilot.Core.Interfaces.KnowledgeBase;

namespace Platform.Engineering.Copilot.Agents.KnowledgeBase.Tools;

/// <summary>
/// Tool for explaining RMF (Risk Management Framework) process and steps.
/// Provides educational content about RMF procedures and DoD guidance.
/// </summary>
public class RmfExplainerTool : BaseTool
{
    private readonly KnowledgeBaseStateAccessors _stateAccessors;
    private readonly IRmfKnowledgeService _rmfService;

    public override string Name => "explain_rmf";

    public override string Description =>
        "Explain the Risk Management Framework (RMF) process, specific steps, or DoD implementation guidance. " +
        "Use for questions about RMF Steps 1-6, required deliverables, or service-specific guidance. " +
        "Example: 'Explain RMF Step 3', 'What are RMF deliverables?', 'Navy RMF guidance'.";

    public RmfExplainerTool(
        ILogger<RmfExplainerTool> logger,
        KnowledgeBaseStateAccessors stateAccessors,
        IRmfKnowledgeService rmfService) : base(logger)
    {
        _stateAccessors = stateAccessors ?? throw new ArgumentNullException(nameof(stateAccessors));
        _rmfService = rmfService ?? throw new ArgumentNullException(nameof(rmfService));

        Parameters.Add(new ToolParameter(
            name: "topic",
            description: "RMF topic to explain: 'overview', 'step1'-'step6', 'deliverables', or service name (e.g., 'navy', 'army')",
            required: false));

        Parameters.Add(new ToolParameter(
            name: "step",
            description: "Specific RMF step number (1-6) to explain in detail",
            required: false));
    }

    public override async Task<string> ExecuteAsync(
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var topic = GetOptionalString(arguments, "topic")?.ToLowerInvariant();
        var stepStr = GetOptionalString(arguments, "step");

        Logger.LogInformation("Explaining RMF topic: {Topic}, step: {Step}", topic ?? "overview", stepStr ?? "all");

        try
        {
            // Track the query
            await _stateAccessors.SetLastQueryAsync("system", topic ?? "rmf_overview", "rmf", cancellationToken);

            // Handle step-specific requests
            if (!string.IsNullOrEmpty(stepStr))
            {
                return await _rmfService.ExplainRmfProcessAsync(stepStr, cancellationToken);
            }

            // Handle topic-based requests
            if (!string.IsNullOrEmpty(topic))
            {
                // Check if it's a step reference
                if (topic.StartsWith("step") && topic.Length > 4)
                {
                    var step = topic.Substring(4);
                    return await _rmfService.ExplainRmfProcessAsync(step, cancellationToken);
                }

                // Check if it's a service-specific request
                if (topic is "navy" or "army" or "airforce" or "disa")
                {
                    return await _rmfService.GetServiceSpecificGuidanceAsync(topic, cancellationToken);
                }

                // Check if it's deliverables
                if (topic == "deliverables")
                {
                    return await GetAllDeliverablesAsync(cancellationToken);
                }
            }

            // Default to overview
            return await _rmfService.ExplainRmfProcessAsync(null, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error explaining RMF topic: {Topic}", topic);
            return ToJson(new
            {
                success = false,
                error = $"Error retrieving RMF information: {ex.Message}"
            });
        }
    }

    private async Task<string> GetAllDeliverablesAsync(CancellationToken cancellationToken)
    {
        var allSteps = await _rmfService.GetAllRmfStepsAsync(cancellationToken);

        var result = "# RMF Deliverables by Step\n\n";

        foreach (var step in allSteps.OrderBy(s => s.Step))
        {
            result += $"## Step {step.Step}: {step.Title}\n\n";
            foreach (var output in step.Outputs)
            {
                result += $"- {output}\n";
            }
            result += "\n";
        }

        return result;
    }
}
