using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Platform.Engineering.Copilot.Core.Plugins;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Engineering.Copilot.ServiceCreation.Core;

/// <summary>
/// Semantic Kernel plugin for service creation and mission service creation
/// TODO: Re-enable full implementation when IServiceCreationService is available
/// </summary>
public class ServiceCreationPlugin : BaseSupervisorPlugin
{
    public ServiceCreationPlugin(
        ILogger<ServiceCreationPlugin> logger,
        Kernel kernel) : base(logger, kernel)
    {
    }

    [KernelFunction("capture_service_creation_requirements")]
    [Description("FIRST STEP for new mission service creation. Captures requirements and generates review summary.")]
    public Task<string> CaptureServiceCreationRequirementsAsync(
        [Description("Mission or service name")] string missionName,
        [Description("Organization")] string organization,
        [Description("Additional requirements")] string? additionalRequirements = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ServiceCreationPlugin stub called - IServiceCreationService implementation required");
        return Task.FromResult($"TODO: IServiceCreationService implementation required. Mission: {missionName}, Organization: {organization}");
    }

    [KernelFunction("submit_for_approval")]
    [Description("Submit a service creation request for platform team approval.")]
    public Task<string> SubmitForApprovalAsync(
        [Description("Request ID")] string requestId,
        [Description("Email of submitter")] string? submittedBy = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ServiceCreationPlugin stub called - IServiceCreationService implementation required");
        return Task.FromResult($"TODO: IServiceCreationService implementation required. Request ID: {requestId}");
    }

    [KernelFunction("get_service_creation_status")]
    [Description("Check service creation request status")]
    public Task<string> GetServiceCreationStatusAsync(
        [Description("Request ID")] string requestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ServiceCreationPlugin stub called - IServiceCreationService implementation required");
        return Task.FromResult($"TODO: IServiceCreationService implementation required. Request ID: {requestId}");
    }
}
