using System.Text;
using Platform.Engineering.Copilot.Core.Models;
using Platform.Engineering.Copilot.Core.Models.Compliance;

namespace Platform.Engineering.Copilot.Core.Services.Generators.Documentation;

/// <summary>
/// Generates DoD-specific documentation for IL2-IL6 environments
/// Includes: COMPLIANCE.md, ATO-CHECKLIST.md, SECURITY.md, DoD-specific README sections
/// </summary>
public class DoDDocumentationGenerator
{
    /// <summary>
    /// Generate all DoD-specific documentation files
    /// </summary>
    public Dictionary<string, string> GenerateDoDDocumentation(TemplateGenerationRequest request)
    {
        var files = new Dictionary<string, string>();
        var dodSpec = request.DoDCompliance;
        
        if (dodSpec == null)
            return files;
        
        // Always include COMPLIANCE.md for IL2+
        files["docs/COMPLIANCE.md"] = GenerateComplianceDoc(request);
        
        // IL5+ requires ATO checklist
        if (dodSpec.RequiresATO)
        {
            files["docs/ATO-CHECKLIST.md"] = GenerateATOChecklist(request);
        }
        
        // Security documentation with DoD context
        files["docs/SECURITY.md"] = GenerateSecurityDoc(request);
        
        // Generate DoD-specific README section
        files["docs/README_DOD_SECTION.md"] = GenerateDoDReadmeSection(request);
        
        return files;
    }
    
    /// <summary>
    /// Generate COMPLIANCE.md documenting all compliance requirements
    /// </summary>
    private string GenerateComplianceDoc(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var dodSpec = request.DoDCompliance!;
        var serviceName = request.ServiceName ?? "Service";
        
        sb.AppendLine($"# DoD Compliance Documentation - {serviceName}");
        sb.AppendLine();
        sb.AppendLine("## Classification and Authorization");
        sb.AppendLine();
        sb.AppendLine("| Attribute | Value |");
        sb.AppendLine("|-----------|-------|");
        sb.AppendLine($"| **Impact Level** | {dodSpec.ImpactLevel} |");
        sb.AppendLine($"| **Data Classification** | {dodSpec.DataClassification ?? "Not Specified"} |");
        sb.AppendLine($"| **Mission Sponsor** | {dodSpec.MissionSponsor ?? "Not Specified"} |");
        sb.AppendLine($"| **DoDAAC** | {dodSpec.DoDAAC ?? "Not Specified"} |");
        sb.AppendLine($"| **Organization Unit** | {dodSpec.OrganizationUnit ?? "Not Specified"} |");
        sb.AppendLine($"| **FedRAMP Level** | {dodSpec.FedRAMPLevel} |");
        sb.AppendLine();
        
        sb.AppendLine("## Applicable Compliance Frameworks");
        sb.AppendLine();
        foreach (var framework in dodSpec.ComplianceFrameworks)
        {
            sb.AppendLine($"- **{framework}**");
            
            // Add framework-specific guidance
            if (framework.Contains("NIST 800-53"))
            {
                sb.AppendLine($"  - Baseline: {GetNISTBaseline(dodSpec.ImpactLevel)}");
                sb.AppendLine("  - Control Families: AC, AU, CM, IA, IR, RA, SA, SC, SI");
            }
            else if (framework.Contains("STIG"))
            {
                sb.AppendLine("  - OS Hardening: Application STIGs required");
                sb.AppendLine("  - Database STIGs: For all database components");
                sb.AppendLine("  - Container STIGs: For containerized workloads");
            }
            else if (framework.Contains("RMF"))
            {
                sb.AppendLine("  - Step 1: Categorize System (FIPS 199)");
                sb.AppendLine("  - Step 2: Select Controls (NIST 800-53)");
                sb.AppendLine("  - Step 3: Implement Controls");
                sb.AppendLine("  - Step 4: Assess Controls");
                sb.AppendLine("  - Step 5: Authorize System (ATO)");
                sb.AppendLine("  - Step 6: Monitor Controls (Continuous)");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("## Technical Requirements");
        sb.AppendLine();
        
        // Encryption
        sb.AppendLine("### Encryption");
        sb.AppendLine();
        sb.AppendLine("| Requirement | Status | Implementation |");
        sb.AppendLine("|------------|---------|----------------|");
        sb.AppendLine($"| **FIPS 140-2 Validated Cryptography** | {(dodSpec.RequiresFIPS140_2 ? "✅ Required" : "❌ Not Required")} | {(dodSpec.RequiresFIPS140_2 ? "Use FIPS-validated encryption modules" : "Standard encryption acceptable")} |");
        sb.AppendLine($"| **Customer-Managed Keys (CMK)** | {(dodSpec.RequiresCustomerManagedKeys ? "✅ Required" : "⚠️ Recommended")} | Azure Key Vault with CMK for all data at rest |");
        sb.AppendLine($"| **Minimum TLS Version** | ✅ Required | TLS {dodSpec.GetMinimumTlsVersion()} or higher |");
        sb.AppendLine($"| **Key Vault SKU** | ✅ Required | Azure Key Vault {dodSpec.GetKeyVaultSku()} |");
        sb.AppendLine();
        
        // Networking
        sb.AppendLine("### Networking and Access Control");
        sb.AppendLine();
        sb.AppendLine("| Requirement | Status | Implementation |");
        sb.AppendLine("|------------|---------|----------------|");
        sb.AppendLine($"| **Private Endpoints** | {(dodSpec.RequiresPrivateEndpoints ? "✅ Required" : "❌ Not Required")} | {(dodSpec.RequiresPrivateEndpoints ? "All Azure PaaS services must use private endpoints" : "Public endpoints allowed with proper NSG rules")} |");
        sb.AppendLine($"| **Azure Government Cloud** | {(dodSpec.RequiresAzureGovernment ? "✅ Required" : "❌ Not Required")} | {(dodSpec.RequiresAzureGovernment ? "Must deploy to Azure Government regions only" : "Commercial Azure allowed")} |");
        sb.AppendLine($"| **CAC Authentication** | {(dodSpec.RequiresCAC ? "✅ Required" : "❌ Not Required")} | {(dodSpec.RequiresCAC ? "PIV/CAC certificate authentication mandatory" : "Standard Azure AD authentication")} |");
        sb.AppendLine();
        
        // Allowed Regions
        var allowedRegions = dodSpec.GetAllowedRegions();
        sb.AppendLine("### Approved Azure Regions");
        sb.AppendLine();
        sb.AppendLine($"This {dodSpec.ImpactLevel} system may only be deployed to the following Azure regions:");
        sb.AppendLine();
        foreach (var region in allowedRegions)
        {
            var isGov = region.StartsWith("usgov");
            sb.AppendLine($"- **{region}** {(isGov ? "(Azure Government) ✅ PREFERRED" : "(Commercial Azure) ⚠️")}");
        }
        sb.AppendLine();
        
        // Monitoring and Logging
        sb.AppendLine("### Monitoring and Audit");
        sb.AppendLine();
        sb.AppendLine("| Requirement | Implementation |");
        sb.AppendLine("|------------|----------------|");
        sb.AppendLine("| **Audit Logging** | Azure Monitor, Log Analytics (180-day retention minimum) |");
        sb.AppendLine("| **Security Monitoring** | Microsoft Defender for Cloud (formerly Azure Security Center) |");
        sb.AppendLine("| **SIEM Integration** | Azure Sentinel for security event correlation |");
        sb.AppendLine($"| **eMASS Integration** | {(dodSpec.RequireseMASS ? "✅ Required - system must be registered" : "❌ Not Required")} |");
        sb.AppendLine();
        
        // Mandatory Resource Tagging
        sb.AppendLine("## Mandatory Resource Tagging");
        sb.AppendLine();
        sb.AppendLine("All Azure resources **MUST** include the following tags:");
        sb.AppendLine();
        var mandatoryTags = dodSpec.GenerateMandatoryTags("production");
        sb.AppendLine("```json");
        sb.AppendLine("{");
        foreach (var tag in mandatoryTags)
        {
            sb.AppendLine($"  \"{tag.Key}\": \"{tag.Value}\",");
        }
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();
        
        // Compliance Validation
        sb.AppendLine("## Compliance Validation");
        sb.AppendLine();
        sb.AppendLine("### Automated Checks");
        sb.AppendLine();
        sb.AppendLine("This repository includes GitHub Actions workflows for automated compliance validation:");
        sb.AppendLine();
        
        if (dodSpec.ImpactLevel >= ImpactLevel.IL4)
        {
            sb.AppendLine("- **STIG Security Scanning** (`.github/workflows/security-scan.yml`)");
            sb.AppendLine("  - TruffleHog: Secret detection");
            sb.AppendLine("  - Checkov: Infrastructure-as-Code security");
            sb.AppendLine("  - tfsec: Terraform security scanning");
            sb.AppendLine("  - Trivy: Container and dependency vulnerability scanning");
            sb.AppendLine();
        }
        
        if (dodSpec.ImpactLevel >= ImpactLevel.IL5)
        {
            sb.AppendLine("- **DoD Compliance Validation** (`.github/workflows/compliance-check.yml`)");
            sb.AppendLine("  - FIPS 140-2 cryptography validation");
            sb.AppendLine("  - Azure region restriction verification");
            sb.AppendLine("  - Private endpoint configuration check");
            sb.AppendLine("  - Mandatory tagging validation");
            sb.AppendLine();
        }
        
        // Contact Information
        sb.AppendLine("## Points of Contact");
        sb.AppendLine();
        sb.AppendLine("| Role | Contact |");
        sb.AppendLine("|------|---------|");
        sb.AppendLine("| **ISSO (Information System Security Officer)** | TBD - Update with actual ISSO contact |");
        sb.AppendLine("| **ISSM (Information System Security Manager)** | TBD - Update with actual ISSM contact |");
        sb.AppendLine("| **AO (Authorizing Official)** | TBD - Update with actual AO contact |");
        sb.AppendLine("| **Mission Sponsor** | " + (dodSpec.MissionSponsor ?? "TBD") + " |");
        sb.AppendLine();
        
        sb.AppendLine("## References");
        sb.AppendLine();
        sb.AppendLine("- [DoD Cloud Computing SRG](https://public.cyber.mil/dccs/dccs-documents/)");
        sb.AppendLine("- [NIST SP 800-53 Rev 5](https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final)");
        sb.AppendLine("- [DISA STIGs](https://public.cyber.mil/stigs/)");
        sb.AppendLine("- [RMF Knowledge Service](https://rmf.org/)");
        sb.AppendLine("- [FedRAMP Documentation](https://www.fedramp.gov/documents/)");
        sb.AppendLine($"- [Azure {(dodSpec.RequiresAzureGovernment ? "Government" : "")} Compliance](https://docs.microsoft.com/en-us/azure/compliance/)");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generate ATO checklist for IL5+ environments
    /// </summary>
    private string GenerateATOChecklist(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var dodSpec = request.DoDCompliance!;
        var serviceName = request.ServiceName ?? "Service";
        
        sb.AppendLine($"# Authority to Operate (ATO) Checklist - {serviceName}");
        sb.AppendLine();
        sb.AppendLine($"> **Impact Level:** {dodSpec.ImpactLevel}  ");
        sb.AppendLine($"> **Classification:** {dodSpec.DataClassification}  ");
        sb.AppendLine($"> **Mission Sponsor:** {dodSpec.MissionSponsor ?? "Not Specified"}  ");
        sb.AppendLine();
        sb.AppendLine("## RMF Process Steps");
        sb.AppendLine();
        
        // Step 1: Categorize
        sb.AppendLine("### ☐ Step 1: Categorize Information System");
        sb.AppendLine();
        sb.AppendLine("- [ ] Complete FIPS 199 categorization");
        sb.AppendLine("- [ ] Document system boundaries and components");
        sb.AppendLine("- [ ] Identify information types processed");
        sb.AppendLine($"- [ ] Confirm Impact Level: **{dodSpec.ImpactLevel}**");
        sb.AppendLine("- [ ] Register system in eMASS");
        sb.AppendLine();
        
        // Step 2: Select Controls
        sb.AppendLine("### ☐ Step 2: Select Security Controls");
        sb.AppendLine();
        sb.AppendLine($"- [ ] Select NIST 800-53 {GetNISTBaseline(dodSpec.ImpactLevel)} baseline controls");
        sb.AppendLine("- [ ] Tailor controls based on mission needs");
        sb.AppendLine("- [ ] Document tailoring decisions in SSP");
        sb.AppendLine("- [ ] Identify common controls vs system-specific controls");
        sb.AppendLine();
        sb.AppendLine("**Key Control Families:**");
        sb.AppendLine("- **AC** - Access Control");
        sb.AppendLine("- **AU** - Audit and Accountability");
        sb.AppendLine("- **CM** - Configuration Management");
        sb.AppendLine("- **IA** - Identification and Authentication");
        sb.AppendLine("- **IR** - Incident Response");
        sb.AppendLine("- **RA** - Risk Assessment");
        sb.AppendLine("- **SA** - System and Services Acquisition");
        sb.AppendLine("- **SC** - System and Communications Protection");
        sb.AppendLine("- **SI** - System and Information Integrity");
        sb.AppendLine();
        
        // Step 3: Implement Controls
        sb.AppendLine("### ☐ Step 3: Implement Security Controls");
        sb.AppendLine();
        sb.AppendLine("#### Technical Controls");
        sb.AppendLine("- [ ] Enable encryption at rest with customer-managed keys");
        sb.AppendLine($"- [ ] Configure TLS {dodSpec.GetMinimumTlsVersion()}+ for all communications");
        sb.AppendLine("- [ ] Implement private endpoints for Azure PaaS services");
        sb.AppendLine($"- [ ] Deploy to approved Azure regions: {string.Join(", ", dodSpec.GetAllowedRegions())}");
        
        if (dodSpec.RequiresFIPS140_2)
        {
            sb.AppendLine("- [ ] Configure FIPS 140-2 validated cryptography");
        }
        if (dodSpec.RequiresCAC)
        {
            sb.AppendLine("- [ ] Implement PIV/CAC authentication");
        }
        
        sb.AppendLine();
        sb.AppendLine("#### Operational Controls");
        sb.AppendLine("- [ ] Apply mandatory resource tags");
        sb.AppendLine("- [ ] Configure Azure Monitor and Log Analytics (180-day retention)");
        sb.AppendLine("- [ ] Enable Microsoft Defender for Cloud");
        sb.AppendLine("- [ ] Configure Azure Sentinel SIEM");
        sb.AppendLine("- [ ] Implement backup and disaster recovery");
        sb.AppendLine("- [ ] Document incident response procedures");
        sb.AppendLine();
        
        // Step 4: Assess Controls
        sb.AppendLine("### ☐ Step 4: Assess Security Controls");
        sb.AppendLine();
        sb.AppendLine("- [ ] Conduct security control assessment");
        sb.AppendLine("- [ ] Run STIG compliance scans");
        sb.AppendLine("- [ ] Perform vulnerability scanning (Nessus/ACAS)");
        sb.AppendLine("- [ ] Execute penetration testing");
        sb.AppendLine("- [ ] Document assessment findings in SAR");
        sb.AppendLine("- [ ] Create POA&M for identified weaknesses");
        sb.AppendLine();
        
        // Step 5: Authorize
        sb.AppendLine("### ☐ Step 5: Authorize Information System");
        sb.AppendLine();
        sb.AppendLine("#### Required Documentation");
        sb.AppendLine("- [ ] **System Security Plan (SSP)** - Complete and approved");
        sb.AppendLine("- [ ] **Security Assessment Report (SAR)** - Independent assessment");
        sb.AppendLine("- [ ] **Plan of Action & Milestones (POA&M)** - Risk remediation plan");
        sb.AppendLine("- [ ] **Risk Assessment** - System-specific risk analysis");
        sb.AppendLine("- [ ] **Privacy Impact Assessment (PIA)** - If PII is processed");
        sb.AppendLine("- [ ] **Interconnection Security Agreements (ISA)** - For external connections");
        sb.AppendLine();
        sb.AppendLine("#### Authorization Package Review");
        sb.AppendLine("- [ ] ISSO review and recommendation");
        sb.AppendLine("- [ ] ISSM review and recommendation");
        sb.AppendLine("- [ ] AO risk acceptance decision");
        sb.AppendLine("- [ ] **ATO Granted** ✅ (3-year authorization)");
        sb.AppendLine();
        
        // Step 6: Monitor
        sb.AppendLine("### ☐ Step 6: Monitor Security Controls (Continuous)");
        sb.AppendLine();
        sb.AppendLine("- [ ] Continuous monitoring strategy documented");
        sb.AppendLine("- [ ] Weekly vulnerability scanning");
        sb.AppendLine("- [ ] Monthly POA&M updates");
        sb.AppendLine("- [ ] Quarterly security control assessments");
        sb.AppendLine("- [ ] Annual STIG compliance validation");
        sb.AppendLine("- [ ] Annual SSP updates");
        sb.AppendLine("- [ ] Configuration change management process");
        sb.AppendLine("- [ ] Security event monitoring via Azure Sentinel");
        sb.AppendLine();
        
        // Additional Requirements
        sb.AppendLine("## Additional IL5/IL6 Requirements");
        sb.AppendLine();
        
        if (dodSpec.RequiresCAC)
        {
            sb.AppendLine("### ☐ CAC/PIV Authentication");
            sb.AppendLine("- [ ] Implement PIV/CAC authentication infrastructure");
            sb.AppendLine("- [ ] Configure certificate-based authentication");
            sb.AppendLine("- [ ] Test CAC authentication workflows");
            sb.AppendLine("- [ ] Document CAC enrollment process");
            sb.AppendLine();
        }
        
        if (dodSpec.RequiresFIPS140_2)
        {
            sb.AppendLine("### ☐ FIPS 140-2 Validation");
            sb.AppendLine("- [ ] Identify FIPS 140-2 validated crypto modules");
            sb.AppendLine("- [ ] Configure FIPS mode on all systems");
            sb.AppendLine("- [ ] Verify FIPS compliance in Azure Key Vault");
            sb.AppendLine("- [ ] Document FIPS validation certificates");
            sb.AppendLine();
        }
        
        sb.AppendLine("## eMASS Registration");
        sb.AppendLine();
        sb.AppendLine("- [ ] Register system in eMASS");
        sb.AppendLine("- [ ] Upload SSP to eMASS");
        sb.AppendLine("- [ ] Upload SAR to eMASS");
        sb.AppendLine("- [ ] Upload POA&M to eMASS");
        sb.AppendLine("- [ ] Upload ATO memo to eMASS");
        sb.AppendLine("- [ ] Configure eMASS continuous monitoring");
        sb.AppendLine();
        
        sb.AppendLine("## Important Contacts");
        sb.AppendLine();
        sb.AppendLine("| Role | Name | Contact | Notes |");
        sb.AppendLine("|------|------|---------|-------|");
        sb.AppendLine("| **ISSO** | TBD | email@mil | Information System Security Officer |");
        sb.AppendLine("| **ISSM** | TBD | email@mil | Information System Security Manager |");
        sb.AppendLine("| **AO** | TBD | email@mil | Authorizing Official |");
        sb.AppendLine("| **System Owner** | TBD | email@mil | Mission Sponsor Representative |");
        sb.AppendLine();
        
        sb.AppendLine("## Timeline");
        sb.AppendLine();
        sb.AppendLine("| Phase | Duration | Start Date | End Date | Status |");
        sb.AppendLine("|-------|----------|------------|----------|--------|");
        sb.AppendLine("| RMF Step 1: Categorize | 2 weeks | TBD | TBD | ☐ |");
        sb.AppendLine("| RMF Step 2: Select Controls | 2 weeks | TBD | TBD | ☐ |");
        sb.AppendLine("| RMF Step 3: Implement Controls | 12 weeks | TBD | TBD | ☐ |");
        sb.AppendLine("| RMF Step 4: Assess Controls | 4 weeks | TBD | TBD | ☐ |");
        sb.AppendLine("| RMF Step 5: Authorize System | 2 weeks | TBD | TBD | ☐ |");
        sb.AppendLine("| RMF Step 6: Monitor (Continuous) | Ongoing | TBD | - | ☐ |");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generate SECURITY.md with DoD-specific security context
    /// </summary>
    private string GenerateSecurityDoc(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var dodSpec = request.DoDCompliance!;
        var serviceName = request.ServiceName ?? "Service";
        
        sb.AppendLine($"# Security Policy - {serviceName}");
        sb.AppendLine();
        sb.AppendLine($"## Classification: {dodSpec.DataClassification ?? "Unclassified"}");
        sb.AppendLine($"## Impact Level: {dodSpec.ImpactLevel}");
        sb.AppendLine();
        
        sb.AppendLine("## Reporting Security Vulnerabilities");
        sb.AppendLine();
        sb.AppendLine("**DO NOT** open public GitHub issues for security vulnerabilities.");
        sb.AppendLine();
        sb.AppendLine("### Internal DoD Reporting");
        sb.AppendLine("1. Contact your **ISSO** (Information System Security Officer)");
        sb.AppendLine("2. Report to **ISSM** (Information System Security Manager)");
        sb.AppendLine($"3. For {dodSpec.ImpactLevel} systems, coordinate with **Cybersecurity Service Provider (CSSP)**");
        sb.AppendLine();
        
        sb.AppendLine("### External Vulnerability Disclosure");
        sb.AppendLine("- Email: security@[mission-sponsor-domain].mil");
        sb.AppendLine("- Include: Detailed description, reproduction steps, potential impact");
        sb.AppendLine("- Expected response time: 48 hours");
        sb.AppendLine();
        
        sb.AppendLine("## Security Controls");
        sb.AppendLine();
        sb.AppendLine($"This system implements security controls per **NIST 800-53 Rev 5 {GetNISTBaseline(dodSpec.ImpactLevel)}** baseline.");
        sb.AppendLine();
        
        sb.AppendLine("### Authentication");
        if (dodSpec.RequiresCAC)
        {
            sb.AppendLine("- **PIV/CAC Required**: All users must authenticate with Common Access Card");
            sb.AppendLine("- Multi-factor authentication (MFA) enforced via PKI");
        }
        else
        {
            sb.AppendLine("- Azure Active Directory with MFA required");
        }
        sb.AppendLine();
        
        sb.AppendLine("### Encryption");
        sb.AppendLine($"- **Data at Rest**: AES-256 with {(dodSpec.RequiresCustomerManagedKeys ? "customer-managed keys (CMK)" : "encryption")}");
        sb.AppendLine($"- **Data in Transit**: TLS {dodSpec.GetMinimumTlsVersion()}+ mandatory");
        if (dodSpec.RequiresFIPS140_2)
        {
            sb.AppendLine("- **FIPS 140-2**: All cryptographic modules FIPS 140-2 Level 2+ validated");
        }
        sb.AppendLine();
        
        sb.AppendLine("### Network Security");
        if (dodSpec.RequiresPrivateEndpoints)
        {
            sb.AppendLine("- **Private Endpoints**: All Azure PaaS services accessible only via private endpoints");
            sb.AppendLine("- **No Public Internet**: Direct internet access prohibited");
        }
        sb.AppendLine("- **Network Segmentation**: NSGs and subnets enforce least-privilege access");
        sb.AppendLine("- **DDoS Protection**: Azure DDoS Protection Standard enabled");
        sb.AppendLine();
        
        sb.AppendLine("### Monitoring and Logging");
        sb.AppendLine("- **Audit Logs**: 180-day retention minimum (Azure Monitor)");
        sb.AppendLine("- **Security Events**: Azure Sentinel SIEM integration");
        sb.AppendLine("- **Threat Detection**: Microsoft Defender for Cloud enabled");
        sb.AppendLine("- **Continuous Monitoring**: eMASS integration for RMF compliance");
        sb.AppendLine();
        
        sb.AppendLine("## Incident Response");
        sb.AppendLine();
        sb.AppendLine("### Severity Levels");
        sb.AppendLine("- **Critical**: Data breach, unauthorized access, system compromise");
        sb.AppendLine("- **High**: Significant vulnerability, DoS attack");
        sb.AppendLine("- **Medium**: Configuration issues, minor vulnerabilities");
        sb.AppendLine("- **Low**: General security concerns");
        sb.AppendLine();
        
        sb.AppendLine("### Response Timeline");
        sb.AppendLine("| Severity | Initial Response | Resolution Target |");
        sb.AppendLine("|----------|-----------------|-------------------|");
        sb.AppendLine("| Critical | 1 hour | 4 hours |");
        sb.AppendLine("| High | 4 hours | 24 hours |");
        sb.AppendLine("| Medium | 24 hours | 1 week |");
        sb.AppendLine("| Low | 1 week | 1 month |");
        sb.AppendLine();
        
        sb.AppendLine("## Compliance");
        sb.AppendLine();
        foreach (var framework in dodSpec.ComplianceFrameworks)
        {
            sb.AppendLine($"- {framework}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generate DoD-specific README section to be included in main README
    /// </summary>
    private string GenerateDoDReadmeSection(TemplateGenerationRequest request)
    {
        var sb = new StringBuilder();
        var dodSpec = request.DoDCompliance!;
        
        sb.AppendLine("## 🛡️ DoD Compliance Information");
        sb.AppendLine();
        sb.AppendLine("| Attribute | Value |");
        sb.AppendLine("|-----------|-------|");
        sb.AppendLine($"| **Classification** | {dodSpec.DataClassification ?? "Unclassified"} |");
        sb.AppendLine($"| **Impact Level** | {dodSpec.ImpactLevel} |");
        sb.AppendLine($"| **Mission Sponsor** | {dodSpec.MissionSponsor ?? "Not Specified"} |");
        sb.AppendLine($"| **DoDAAC** | {dodSpec.DoDAAC ?? "Not Specified"} |");
        sb.AppendLine($"| **FedRAMP Level** | {dodSpec.FedRAMPLevel} |");
        sb.AppendLine();
        
        sb.AppendLine("### 🔒 Security Requirements");
        sb.AppendLine();
        var requirements = new List<string>();
        
        if (dodSpec.RequiresFIPS140_2)
            requirements.Add("✅ FIPS 140-2 Validated Cryptography");
        if (dodSpec.RequiresCAC)
            requirements.Add("✅ PIV/CAC Authentication");
        if (dodSpec.RequiresCustomerManagedKeys)
            requirements.Add("✅ Customer-Managed Encryption Keys");
        if (dodSpec.RequiresPrivateEndpoints)
            requirements.Add("✅ Private Endpoints for Azure PaaS");
        if (dodSpec.RequiresAzureGovernment)
            requirements.Add("✅ Azure Government Cloud");
        
        foreach (var req in requirements)
        {
            sb.AppendLine($"- {req}");
        }
        
        sb.AppendLine();
        sb.AppendLine("### 📋 Compliance Frameworks");
        sb.AppendLine();
        foreach (var framework in dodSpec.ComplianceFrameworks.Take(5))
        {
            sb.AppendLine($"- {framework}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"**📄 Full compliance documentation**: See [COMPLIANCE.md](docs/COMPLIANCE.md)");
        
        if (dodSpec.RequiresATO)
        {
            sb.AppendLine();
            sb.AppendLine($"**🎯 ATO Requirements**: See [ATO-CHECKLIST.md](docs/ATO-CHECKLIST.md) for Authority to Operate process");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Get NIST 800-53 baseline name for Impact Level
    /// </summary>
    private string GetNISTBaseline(ImpactLevel level)
    {
        return level switch
        {
            ImpactLevel.IL2 => "LOW",
            ImpactLevel.IL4 => "MODERATE",
            ImpactLevel.IL5 => "HIGH",
            ImpactLevel.IL6 => "HIGH",
            _ => "LOW"
        };
    }
}
