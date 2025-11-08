using Platform.Engineering.Copilot.Core.Models.Compliance;
using Platform.Engineering.Copilot.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Engineering.Copilot.Core.Services
{

    public interface IComplianceAwareTemplateEnhancer
    {
        /// <summary>
        /// Generate templates with compliance controls automatically injected
        /// </summary>
        Task<TemplateGenerationResult> EnhanceWithComplianceAsync(
            TemplateGenerationRequest request,
            string complianceFramework = "FedRAMP-High",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate if generated template meets compliance requirements
        /// </summary>
        Task<ComplianceValidationResult> ValidateComplianceAsync(
            string templateContent,
            string complianceFramework,
            CancellationToken cancellationToken = default);
    }
}