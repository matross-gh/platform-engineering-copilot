
using Platform.Engineering.Copilot.Core.Models;
namespace Platform.Engineering.Copilot.Core.Services
{
    public interface IDynamicTemplateGenerator
    {
        Task<TemplateGenerationResult> GenerateTemplateAsync(TemplateGenerationRequest request, CancellationToken cancellationToken = default);
    }
}