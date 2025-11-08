using Platform.Engineering.Copilot.Core.Models.Compliance;
using Platform.Engineering.Copilot.Core.Models.Mcp;

namespace Platform.Engineering.Copilot.Core.Interfaces.Compliance;

public interface IAzurePolicyService
{
    Task<PreFlightGovernanceResult> EvaluatePreFlightPoliciesAsync(McpToolCall toolCall, CancellationToken cancellationToken = default);
    Task<PostFlightGovernanceResult> EvaluatePostFlightComplianceAsync(McpToolCall toolCall, McpToolResult result, CancellationToken cancellationToken = default);
    Task<List<AzurePolicyEvaluation>> GetPolicyEvaluationsForResourceAsync(string resourceId, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflow> CreateApprovalWorkflowAsync(McpToolCall toolCall, List<PolicyViolation> violations, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflow?> GetApprovalWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflow> UpdateApprovalWorkflowAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken = default);
}