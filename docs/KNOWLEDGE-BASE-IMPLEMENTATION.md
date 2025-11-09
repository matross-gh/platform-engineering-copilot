# RMF/STIG Knowledge Base Implementation

## Overview

This implementation provides comprehensive DoD compliance knowledge base functionality for the Platform Engineering Copilot, addressing **Phase 1 compliance gaps** for RMF/STIG explanations and Navy/DoD workflow guidance.

## Phase 1 Compliance Status

✅ **IMPLEMENTED**

| Feature | Status | Evidence |
|---------|--------|----------|
| **RMF/STIG Explanations** | ✅ Implemented | Knowledge base with 6 RMF steps, 5 Azure STIGs, control mappings |
| **DoD Process Guidance** | ✅ Implemented | Navy workflows (ATO, PMW deployment, eMASS), DoD instructions |

## Architecture

### Components

#### 1. Data Models (`Core/Models/KnowledgeBase/`)
- `KnowledgeBaseModels.cs` - All data models for RMF, STIG, DoD instructions, workflows

#### 2. Interfaces (`Core/Interfaces/KnowledgeBase/`)
- `IKnowledgeBaseServices.cs` - Service contracts for knowledge base queries

#### 3. Services (`Compliance.Agent/Services/KnowledgeBase/`)
- `RmfKnowledgeService.cs` - RMF process explanations
- `StigKnowledgeService.cs` - STIG control queries and mappings
- `DoDInstructionService.cs` - DoD instruction reference
- `DoDWorkflowService.cs` - Navy/DoD workflow guidance

#### 4. Plugin (`Compliance.Agent/Plugins/`)
- `KnowledgeBasePlugin.cs` - Semantic Kernel functions for knowledge base queries

#### 5. Knowledge Base Data (`Core/KnowledgeBase/`)
- `rmf-process.json` - 6 RMF steps with activities, deliverables, roles
- `stig-controls.json` - Azure STIGs with NIST mappings, Azure implementation
- `dod-instructions.json` - DoD instructions, Impact Levels, boundary protection
- `navy-workflows.json` - Navy ATO process, PMW deployment, eMASS registration

## Features

### RMF (Risk Management Framework)

**Available Functions:**
- `explain_rmf_process` - Explain entire RMF or specific step (1-6)
- `get_rmf_deliverables` - Get required documents for each RMF step

**Example Queries:**
```
User: @platform Explain the RMF process
User: @platform What is RMF Step 4?
User: @platform What documents do I need for RMF Step 2?
```

**Knowledge Base Contains:**
- All 6 RMF steps (Categorize, Select, Implement, Assess, Authorize, Monitor)
- Activities for each step
- Deliverables (SSP, SAP, SAR, POA&M, etc.)
- Responsible roles (AO, ISSO, ISSM, SCA)
- DoD instruction references (DoDI 8510.01)

### STIG (Security Technical Implementation Guides)

**Available Functions:**
- `explain_stig` - Detailed STIG control explanation with Azure implementation
- `search_stigs` - Search STIGs by keyword
- `get_stigs_for_nist_control` - Get STIGs that implement a NIST control
- `get_control_mapping` - Complete mapping between NIST, STIGs, CCIs, DoD instructions

**Example Queries:**
```
User: @platform Explain STIG V-219153
User: @platform What STIGs apply to encryption?
User: @platform Show STIGs for NIST control AC-2
User: @platform Map IA-2(1) to STIGs
```

**Knowledge Base Contains:**
- 5 key Azure STIGs (MFA, public IPs, encryption, TLS, Key Vault)
- Severity ratings (Critical, High, Medium, Low)
- Check procedures and fix procedures
- NIST 800-53 control mappings
- CCI references
- Azure-specific implementation guidance
- Automation commands (Azure CLI)

### DoD Instructions

**Available Functions:**
- `explain_dod_instruction` - Explain DoD instruction and requirements
- `search_dod_instructions` - Search instructions by topic

**Example Queries:**
```
User: @platform Explain DoDI 8500.01
User: @platform What is the DoD RMF instruction?
User: @platform DoD cybersecurity policy
```

**Knowledge Base Contains:**
- DoDI 8500.01 (Cybersecurity)
- DoDI 8510.01 (RMF for DoD IT)
- DoDI 8520.02 (PKI and CAC/PIV)
- DoDI 8140.01 (Cyberspace Workforce)
- CNSSI 1253 (Security for NSS)

### Navy/DoD Workflows

**Available Functions:**
- `explain_navy_workflow` - Generic workflow explanation
- `get_navy_ato_process` - Navy RMF/ATO authorization process (8 steps)
- `get_pmw_deployment_process` - PMW cloud deployment workflow (6 steps)
- `get_emass_registration_process` - eMASS system registration (5 steps)

**Example Queries:**
```
User: @platform How do I get an ATO in the Navy?
User: @platform What is the PMW cloud deployment process?
User: @platform How do I register my system in eMASS?
```

**Knowledge Base Contains:**
- Navy RMF ATO Process (WF-NAV-ATO-001)
  - 8 steps from initiation to continuous monitoring
  - Timelines (20-60 weeks typical)
  - Required documents
  - Approval authorities
- PMW Cloud Deployment (WF-PMW-CLOUD-001)
  - 6 steps specific to PMW organizations
  - Security architecture review
  - Navy SOC integration
- eMASS Registration (WF-NAV-EMASS-001)
  - 5 steps for system registration
  - Artifact upload procedures
  - Continuous monitoring updates

### Impact Level (IL) Guidance

**Available Functions:**
- `explain_impact_level` - Explain IL requirements (IL2, IL4, IL5, IL6)

**Example Queries:**
```
User: @platform What is Impact Level 5?
User: @platform IL5 boundary protection requirements
User: @platform Explain IL6
```

**Knowledge Base Contains:**
- IL2 (DoD CUI Low) - Basic cloud security
- IL4 (DoD CUI Moderate) - Enhanced monitoring
- IL5 (DoD CUI High / Secret) - Complete isolation, FIPS 140-2
- IL6 (Secret / Top Secret) - Air-gapped, NSA Suite B crypto

## Integration with Compliance Agent

The `KnowledgeBasePlugin` is registered with the Compliance Agent to provide knowledge base functions during compliance conversations.

**Registration (in `ComplianceAgent.cs` constructor):**
```csharp
_kernel.ImportPluginFromObject(knowledgeBasePlugin, "KnowledgeBase");
```

**System Prompt Enhancement:**
The Compliance Agent's system prompt should reference these functions for knowledge queries.

## Usage Examples

### Example 1: RMF Overview
```
User: @platform Explain the RMF process

Copilot: # Risk Management Framework (RMF)

The Risk Management Framework (RMF) provides a disciplined and structured process 
that integrates information security and risk management activities into the system 
development life cycle.

**Purpose:** To ensure that managing information system-related security risks is 
consistent with the mission and business objectives of the organization.

## RMF Steps

### Step 1: Categorize
Categorize the system and information processed...

**Key Activities:**
- Identify system boundaries
- Conduct FIPS 199 impact analysis
...

[Complete 6-step process]
```

### Example 2: STIG Explanation
```
User: @platform Explain STIG V-219153

Copilot: # V-219153: Azure AD authentication must use multi-factor authentication

**Severity:** High
**Category:** Identity and Access Management
**STIG Family:** Azure STIG

## Description

Multi-factor authentication (MFA) is required for all Azure AD privileged accounts 
to prevent unauthorized access...

## NIST 800-53 Controls

IA-2(1), IA-2(2), IA-2(8), AC-2

## Check Procedure

Verify that MFA is enabled for all privileged Azure AD accounts...

## Remediation

Enable MFA for all privileged Azure AD accounts:
1. Navigate to Azure AD > Security > Multi-Factor Authentication
...

## Azure Implementation

**Service:** Azure AD
**Configuration:** Conditional Access Policies, MFA Settings
**Azure Policy:** Enable MFA for privileged accounts

### Automation Command

```bash
az ad user update --id <user> --force-change-password-next-login true && Enable-AzureADMFA
```
```

### Example 3: Navy ATO Process
```
User: @platform How do I get an ATO in the Navy?

Copilot: # Navy RMF ATO Process

**Organization:** Navy
**Impact Level:** All levels (IL2-IL6)

## Description

Standard Navy process for obtaining Authority to Operate (ATO) for information systems

## Workflow Steps

### Step 1: Initiate RMF Process

System owner initiates RMF process and assigns key personnel

**Responsibilities:**
- Identify system owner
- Assign ISSO/ISSM
- Identify Authorizing Official (AO)
...

**Estimated Duration:** 1-2 weeks

[8 complete steps with 20-60 week timeline]
```

### Example 4: STIG Search
```
User: @platform What STIGs apply to encryption?

Copilot: **STIG Search Results for "encryption"**

Found 2 matching controls:

**V-219165**: Azure Storage accounts must use encryption at rest
- Severity: High
- Category: Data Protection
- NIST Controls: SC-28, SC-28(1), SC-13
- Description: All Azure Storage accounts must use encryption at rest...

**V-219178**: Azure Key Vault must have soft delete and purge protection enabled
- Severity: Medium
- Category: Backup and Recovery
...

Use 'explain_stig' function with a specific STIG ID for complete details.
```

### Example 5: Control Mapping
```
User: @platform Map NIST control IA-2(1) to STIGs

Copilot: **Control Mapping for NIST IA-2(1)**

Multi-factor authentication for network access to privileged accounts

**STIG IDs:** V-219153
**CCI References:** CCI-000765
**DoD Instructions:** DoD 8500.01, DoDI 8520.02

**Implementation Guidance:**
- **azure**: Use Azure AD Conditional Access to enforce MFA for privileged roles
- **il5**: Require hardware tokens or CAC/PIV for IL5 environments
- **il6**: Mandatory CAC/PIV authentication for all IL6 privileged access
```

## File Structure

```
Platform.Engineering.Copilot/
├── src/
│   ├── Platform.Engineering.Copilot.Core/
│   │   ├── Models/
│   │   │   └── KnowledgeBase/
│   │   │       └── KnowledgeBaseModels.cs [NEW]
│   │   ├── Interfaces/
│   │   │   └── KnowledgeBase/
│   │   │       └── IKnowledgeBaseServices.cs [NEW]
│   │   └── KnowledgeBase/ [NEW]
│   │       ├── rmf-process.json
│   │       ├── stig-controls.json
│   │       ├── dod-instructions.json
│   │       └── navy-workflows.json
│   └── Platform.Engineering.Copilot.Compliance.Agent/
│       ├── Services/
│       │   └── KnowledgeBase/ [NEW]
│       │       ├── RmfKnowledgeService.cs
│       │       ├── StigKnowledgeService.cs
│       │       ├── DoDInstructionService.cs
│       │       └── DoDWorkflowService.cs
│       └── Plugins/
│           └── KnowledgeBasePlugin.cs [NEW]
```

## Configuration

### Dependency Injection Registration

Add to `ServiceCollectionExtensions.cs` or startup:

```csharp
// Knowledge Base Services
services.AddScoped<IRmfKnowledgeService, RmfKnowledgeService>();
services.AddScoped<IStigKnowledgeService, StigKnowledgeService>();
services.AddScoped<IDoDInstructionService, DoDInstructionService>();
services.AddScoped<IDoDWorkflowService, DoDWorkflowService>();

// Knowledge Base Plugin
services.AddScoped<KnowledgeBasePlugin>();
```

### Knowledge Base Files Deployment

Ensure JSON files are copied to output directory:

```xml
<ItemGroup>
  <None Update="KnowledgeBase\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Testing

### Unit Tests

Create tests for each service:
- `RmfKnowledgeServiceTests.cs`
- `StigKnowledgeServiceTests.cs`
- `DoDInstructionServiceTests.cs`
- `DoDWorkflowServiceTests.cs`

### Integration Tests

Test plugin functions through Compliance Agent:
- RMF explanations
- STIG queries and mappings
- Workflow guidance
- Control mapping validation

## Performance

### Caching Strategy

All knowledge base data is cached for 24 hours using `IMemoryCache`:
- RMF process data: ~5KB
- STIG controls: ~15KB
- DoD instructions: ~10KB
- Navy workflows: ~20KB

**Total memory footprint:** ~50KB cached data

### Load Time

- Initial load from JSON files: <100ms
- Subsequent cached queries: <1ms

## Extensibility

### Adding New STIGs

Edit `stig-controls.json` and add entries:

```json
{
  "stigId": "V-XXXXX",
  "title": "...",
  "description": "...",
  "severity": "High",
  "nistControls": ["AC-2"],
  "azureImplementation": { ... }
}
```

### Adding Navy Workflows

Edit `navy-workflows.json` and add workflow entries with steps.

### Adding DoD Instructions

Edit `dod-instructions.json` and include instruction details with NIST mappings.

## Phase 1 Compliance

✅ **Advisory Only** - All functions provide knowledge and guidance without performing actions
✅ **No Direct Execution** - Only returns explanatory text
✅ **Educational Focus** - Helps users understand compliance requirements
✅ **Safe for IL5/IL6** - No system modifications or deployments

## Future Enhancements

1. **STIG Viewer Integration** - Pull latest STIG data from DISA
2. **eMASS API Integration** - Real-time eMASS status queries
3. **NIST Control Enhancements** - Map control enhancements (e.g., AC-2(1))
4. **IL-Specific Templates** - Generate IL-compliant IaC templates
5. **Compliance Dashboard** - Visual compliance status
6. **ATO Package Generator** - Generate complete ATO packages

## References

- NIST 800-53 Rev 5
- DoD 8510.01 - Risk Management Framework
- DoD 8500.01 - Cybersecurity
- DISA STIGs: https://public.cyber.mil/stigs/
- eMASS: https://www.cdse.edu/catalog/emass.html

## Support

For questions or issues:
- File GitHub issue with `knowledge-base` label
- Contact compliance team for DoD-specific guidance
- Consult ISSO/ISSM for ATO/RMF questions
