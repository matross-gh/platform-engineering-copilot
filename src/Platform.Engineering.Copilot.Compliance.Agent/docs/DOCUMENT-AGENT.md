# Document Agent

> ATO documentation generation and compliance artifact management specialist

## Overview

The Document Agent is a specialized AI agent for generating Authority to Operate (ATO) documentation including System Security Plans (SSP), Security Assessment Reports (SAR), Plan of Action & Milestones (POA&M), and other compliance artifacts.

**Agent Type**: `Compliance`  
**Icon**: 📄  
**Temperature**: 0.3 (balanced for technical writing)  
**Location**: `Platform.Engineering.Copilot.Compliance.Agent`

## Capabilities

### 1. Document Types

#### System Security Plan (SSP)
Complete system security documentation required for ATO:

**SSP Sections (per NIST SP 800-18):**
1. **System Identification**: System name, categorization, authorization boundary
2. **System Overview**: Purpose, functions, data sensitivity
3. **System Environment**: Hardware, software, network topology
4. **System Interconnections**: External systems, data flows
5. **Applicable Laws and Regulations**: FISMA, Privacy Act, etc.
6. **Minimum Security Controls**: NIST 800-53 control implementation
7. **Control Implementation**: Detailed narratives for each control
8. **Attachments**: Network diagrams, data flow diagrams, POA&M

**Auto-generated Content:**
- Control narratives based on actual Azure deployment
- Evidence references from compliance scans
- Customer vs. inherited responsibility matrices
- Configuration screenshots and logs
- Network topology diagrams

#### Security Assessment Report (SAR)
Assessment findings from security testing:

**SAR Components:**
- **Executive Summary**: High-level findings and recommendations
- **Assessment Methodology**: Testing approach (interviews, document review, testing)
- **Control Assessment Results**: Pass/fail status for each control
- **Findings and Recommendations**: Vulnerabilities discovered
- **Risk Ratings**: High/Medium/Low severity
- **Remediation Timeline**: Expected fix dates

**Auto-generation:**
```
Control: AC-2 (Account Management)
Status: ✅ Satisfied
Assessment Method: Interview, Examine, Test

Findings:
- Azure AD implements account lifecycle management
- RBAC enforces least privilege
- Access reviews conducted quarterly
- Account creation requires manager approval

Evidence:
- Azure AD user list (evidence_AC-2_001.xlsx)
- RBAC assignments (evidence_AC-2_002.json)
- Access review logs (evidence_AC-2_003.csv)

Result: Control is implemented correctly
```

#### Security Assessment Plan (SAP)
Test plan for security assessment:

**SAP Sections:**
- **Scope**: Systems and controls to assess
- **Methodology**: Assessment procedures
- **Schedule**: Assessment timeline
- **Personnel**: Assessors and system owners
- **Tools**: Automated scanning tools
- **Deliverables**: Expected outputs (SAR, POA&M)

#### Plan of Action & Milestones (POA&M)
Remediation tracking for compliance gaps:

```
POA&M Entry #001

Control: SC-7 (Boundary Protection)
Weakness: 3 storage accounts with public blob access
Severity: High
Status: Open

Resources Affected:
- storageaccount1
- storageaccount2
- storageaccount3

Remediation Plan:
1. Disable public blob access
2. Configure private endpoints
3. Update network ACLs
4. Test connectivity

Milestones:
- 2025-11-25: Remediation plan approved
- 2025-11-30: Changes implemented
- 2025-12-05: Testing complete
- 2025-12-10: Validation scan

Cost: $0 (configuration change only)
Point of Contact: security-team@company.com
```

#### STIG Checklists
Security Technical Implementation Guide compliance checklists:

**Supported STIGs:**
- Windows Server 2019/2022
- RHEL 7/8/9
- Ubuntu 20.04/22.04
- SQL Server 2019/2022
- IIS 10.0
- Kubernetes 1.27+

**Checklist Format:**
```
STIG ID: V-254800
Rule Title: Windows must enforce password complexity
Severity: CAT II (Medium)
Status: Not a Finding

Check:
gpresult /r | findstr "Password must meet complexity"

Fix:
Configure GPO: Computer Configuration → Windows Settings → 
Security Settings → Account Policies → Password Policy → 
Password must meet complexity requirements = Enabled

Evidence: gpo_password_policy_20251119.png
Comments: Configured via Azure AD Domain Services
```

#### Runbooks
Operational procedures and playbooks:

**Runbook Types:**
- Deployment procedures
- Incident response playbooks
- Disaster recovery plans
- Backup and restore procedures
- Patch management processes
- Security monitoring procedures

#### Architecture Diagrams
Technical documentation and visualizations:

**Diagram Types:**
- Network topology diagrams
- Data flow diagrams
- Authorization boundary diagrams
- System interconnection diagrams
- Logical architecture diagrams
- Physical deployment diagrams

### 2. Document Operations

#### Generate from Templates
Pre-built FedRAMP/DoD templates:

```
Template: FedRAMP SSP Template (Moderate Baseline)

Available Sections:
1. Front Matter (title page, signature page)
2. Executive Summary
3. System Identification
4. System Overview
5. System Environment
6. Applicable Controls (325 controls)
7. Control Implementation Narratives
8. System Interconnections
9. Attachments

Auto-fill Data:
✅ System name: Production Environment
✅ Authorization boundary: rg-production
✅ Impact level: Moderate
✅ FIPS 199 categorization: (M, M, M)
✅ Network diagrams: Generated
✅ Control narratives: 287/325 complete
⚠️  Manual input needed: 38 controls

Generate: fedramp_ssp_production_v1.0.docx
```

#### Control Narratives
AI-generated implementation descriptions:

```
Generate Narrative: AC-2 (Account Management)

Input:
- Azure AD tenant: contoso.onmicrosoft.com
- RBAC assignments: 234 users, 15 groups
- Privileged roles: 12 users with elevated access
- Access review frequency: Quarterly

Generated Narrative:
The Production Environment implements account management through
Azure Active Directory (Azure AD), which serves as the centralized
identity and access management system. All user accounts are created,
modified, and deleted through Azure AD according to the organization's
account management procedures documented in POL-IAM-001.

Account Types:
- Individual User Accounts: 234 accounts for named individuals
- Service Accounts: 12 managed identities for application access
- Administrative Accounts: Separate accounts for privileged access

Account Lifecycle:
Account creation requires approval from the user's manager submitted
through the ServiceNow ticketing system. New accounts are provisioned
with the principle of least privilege, granting only permissions
necessary for job functions. Azure AD Privileged Identity Management
(PIM) provides just-in-time access for administrative roles.

Access Reviews:
Quarterly access reviews are conducted using Azure AD Access Reviews
to ensure continued appropriateness of access rights. Reviews are
completed by resource owners and documented in evidence_AC-2_reviews.xlsx.

Monitoring:
All account management activities are logged to the centralized
Log Analytics workspace (law-prod-001) with 90-day retention.
Alerts are configured for suspicious account activities including
privilege escalations and unusual access patterns.

Evidence:
- Azure AD user list (evidence_AC-2_001.xlsx)
- RBAC role assignments (evidence_AC-2_002.json)
- Access review reports (evidence_AC-2_003.pdf)
- Account management procedure (POL-IAM-001.pdf)
```

#### Section Updates
Modify specific document sections:

```
Update Request: SSP Section 5 (System Environment)

Changes:
- Add new AKS cluster: aks-prod-002
- Update node count: 15 → 18 nodes
- Add private endpoint for SQL
- Update network diagram

Before:
"The system utilizes one AKS cluster (aks-prod-001) with 15 nodes..."

After:
"The system utilizes two AKS clusters:
- aks-prod-001: 18 nodes (primary workloads)
- aks-prod-002: 6 nodes (analytics workloads)

All database connections use private endpoints to ensure traffic
remains within the Azure backbone network and does not traverse
the public internet."

✅ Section 5 updated
✅ Network diagram regenerated
✅ Version incremented: v1.0 → v1.1
```

#### Evidence Attachment
Link evidence to controls:

```
Attach Evidence: AU-2 (Audit Events)

Evidence Files:
1. diagnostic_settings_screenshot.png
   Description: Azure Monitor diagnostic settings
   Timestamp: 2025-11-19 14:30 UTC

2. log_analytics_retention.json
   Description: Log Analytics retention configuration
   Retention: 90 days
   Timestamp: 2025-11-19 14:31 UTC

3. sample_audit_logs.csv
   Description: Sample audit log entries
   Records: 1,000 samples
   Date Range: 2025-11-12 to 2025-11-19

4. alert_rules.json
   Description: Azure Monitor alert rules for critical events
   Alert Count: 15 configured

Evidence Storage:
Account: complianceevidence
Container: evidence/AU-2
Access: Restricted to compliance team
Versioning: Enabled
Immutability: 7-year retention

✅ Evidence attached to control AU-2
✅ References updated in SSP Section 6.2
```

#### Version Control
Track document revisions:

```
Document Version History

Document: SSP_Production_Environment.docx

v1.2 (2025-11-19) - Current
  Changes:
  - Added aks-prod-002 cluster
  - Updated control narratives (AC-2, AU-2, SC-7)
  - Attached new evidence for 8 controls
  - Updated network diagram
  Modified by: security-team@company.com

v1.1 (2025-11-10)
  Changes:
  - Updated FIPS 199 categorization
  - Added POA&M reference
  - Fixed typos in Section 3
  Modified by: compliance@company.com

v1.0 (2025-11-01) - Initial Release
  Changes:
  - Initial SSP creation
  - All 325 controls documented
  - Network diagrams generated
  Created by: platform-team@company.com

Change Control:
- All changes require approval
- Version increments: major.minor
- Archive previous versions
```

#### Export Formats
Generate documents in multiple formats:

**Supported Formats:**
- **DOCX**: Microsoft Word (editable)
- **PDF**: Adobe PDF (final submission)
- **Markdown**: Human-readable text
- **HTML**: Web-based viewing
- **JSON**: Machine-readable data

```
Export Request: SSP_Production

Formats:
✅ DOCX: SSP_Production_v1.2.docx (2.3 MB)
✅ PDF: SSP_Production_v1.2.pdf (1.8 MB)
✅ MD: SSP_Production_v1.2.md (456 KB)
✅ JSON: SSP_Production_v1.2.json (234 KB)

Validation:
✅ All controls documented
✅ All evidence attached
✅ All diagrams included
✅ No broken references
✅ Spell check passed

Export Status: ✅ Complete
Storage: Azure Blob Storage (container: ato-documents)
```

### 3. Content Generation

#### AI-powered Writing
Generate control narratives from deployment data:

**Input Sources:**
- Azure resource configuration
- Compliance scan results
- Policy assignments
- RBAC roles
- Network topology
- Evidence artifacts

**Output:**
- Clear "what" is implemented
- Detailed "how" control is met
- Evidence references
- Customer vs. inherited responsibilities

#### Template-based Generation
Use pre-approved templates:

**Template Library:**
- FedRAMP SSP templates (High, Moderate, Low)
- DoD SSP templates (IL2, IL4, IL5, IL6)
- NIST 800-171 SSP templates
- ISO 27001 ISMS documentation
- SOC 2 Type II documentation

#### Evidence Integration
Auto-link evidence to control narratives:

```
Control Narrative with Evidence:

AC-2: Account Management

The system implements account management through Azure Active Directory...

Evidence:
[1] Azure AD user list (evidence_AC-2_001.xlsx)
[2] RBAC role assignments (evidence_AC-2_002.json)
[3] Access review reports Q3 2025 (evidence_AC-2_003.pdf)
[4] Account management procedure (POL-IAM-001.pdf)

See Attachment 7 for complete evidence package.
```

## Plugins

### DocumentGenerationPlugin

Main plugin for document operations.

**Functions:**
- `generate_document_from_template` - Create document from template
- `generate_control_narrative` - Write control implementation description
- `update_document_section` - Modify specific section
- `attach_evidence` - Link evidence to controls
- `export_document` - Generate final document
- `list_documents` - Show available documents
- `get_document_status` - Check document completeness
- `upload_security_document` - Upload external document

### ConfigurationPlugin

Azure subscription management.

**Functions:**
- `set_azure_subscription` - Set active subscription
- `get_azure_subscription` - Get current subscription

## Example Prompts

### Document Generation

```
"Generate SSP for my production environment"
"Create POA&M for failed controls"
"Generate SAR from latest compliance scan"
"Create FedRAMP High SSP template"
"Generate STIG checklist for Windows Server"
```

### Control Narratives

```
"Write control narrative for AC-2"
"Generate implementation description for AU-2"
"Create narrative for SC-7 (Boundary Protection)"
"Generate all control narratives for AC family"
```

### Document Management

```
"Update SSP Section 10 (Incident Response)"
"Export SSP as PDF"
"Upload security documentation"
"Show my documents"
"Check SSP completion status"
"Attach evidence to AU-2 control"
```

### Templates

```
"Show available ATO document templates"
"Create STIG checklist for RHEL 8"
"Generate runbook for deployment procedure"
"Create architecture diagram for my environment"
```

## Key Services

| Service | Purpose |
|---------|---------|
| `DocumentGenerationService` | Template-based document creation |
| `ControlNarrativeGenerator` | AI-powered narrative writing |
| `DocumentStorageService` | Azure Blob Storage integration |
| `TemplateLibraryService` | Template management |
| `EvidenceAttachmentService` | Evidence linking |
| `DocumentExportService` | Multi-format export |

## Configuration

### appsettings.json

```json
{
  "DocumentAgent": {
    "Temperature": 0.3,
    "MaxTokens": 8000,
    "EnableAIGeneration": true,
    "EnableTemplates": true
  },
  "DocumentGeneration": {
    "StorageAccount": "atodocuments",
    "Container": "documents",
    "TemplateContainer": "templates",
    "EvidenceContainer": "evidence",
    "MaxDocumentSizeMB": 50,
    "SupportedFormats": ["DOCX", "PDF", "MD", "HTML", "JSON"]
  },
  "Templates": {
    "FedRAMPHigh": "templates/fedramp/ssp_high_template.docx",
    "FedRAMPModerate": "templates/fedramp/ssp_moderate_template.docx",
    "DoDIL5": "templates/dod/ssp_il5_template.docx",
    "ISO27001": "templates/iso/isms_template.docx"
  }
}
```

## Usage Patterns

### Pattern 1: Generate SSP

```
User: "Generate SSP for production environment"

Agent Response:
📄 Generating System Security Plan

Template: FedRAMP Moderate Baseline
System: Production Environment
Authorization Boundary: rg-production
Impact Level: Moderate

Auto-filling sections:
✅ Section 1: System Identification
✅ Section 2: System Overview  
✅ Section 3: System Environment (23 resources)
✅ Section 4: System Interconnections
🔄 Section 5: Applicable Controls (325 controls)
   - Auto-generated: 287 narratives (88%)
   - Manual input needed: 38 narratives (12%)

✅ Network diagrams generated
✅ Data flow diagrams generated
✅ Evidence attached (178 artifacts)

Progress: 88% complete

Next Steps:
1. Review auto-generated narratives
2. Complete 38 remaining narratives
3. Obtain signature from Authorizing Official
4. Submit to eMASS

📄 Document: SSP_Production_v1.0.docx
📊 Completion status: 88%
```

### Pattern 2: Generate Control Narrative

```
User: "Write control narrative for AC-2"

Agent Response:
📝 Control Narrative: AC-2 (Account Management)

Generated Narrative (542 words):

The Production Environment implements account management through Azure
Active Directory (Azure AD), which serves as the centralized identity
and access management system for all 234 user accounts...

[Full narrative with sections on:]
- Account Types (Individual, Service, Administrative)
- Account Lifecycle (Creation, Modification, Deletion)
- Access Reviews (Quarterly via Azure AD Access Reviews)
- Monitoring (Azure Monitor + Log Analytics)
- Evidence References

Evidence Attached:
✅ evidence_AC-2_001.xlsx (Azure AD user list)
✅ evidence_AC-2_002.json (RBAC assignments)
✅ evidence_AC-2_003.pdf (Q3 2025 access review)

Implementation Status: ✅ Fully Implemented
Inherited: None (Customer Responsibility)

Would you like me to add this to the SSP?
```

### Pattern 3: Create POA&M

```
User: "Create POA&M for failed controls"

Agent Response:
📋 Plan of Action & Milestones (POA&M)

Compliance Scan: 2025-11-19
Failed Controls: 7
Severity: 3 High, 4 Medium

POA&M Entries Created:

#001 - SC-7 (Boundary Protection) - High
  Issue: 3 storage accounts with public access
  Remediation: Disable public access, configure private endpoints
  Milestone: 2025-12-10
  Cost: $0

#002 - IA-2 (Multi-Factor Authentication) - High
  Issue: 5 users without MFA enabled
  Remediation: Enable MFA via Azure AD Conditional Access
  Milestone: 2025-11-25
  Cost: $0

#003 - AU-2 (Audit Events) - Medium
  Issue: 2 resource groups missing diagnostic settings
  Remediation: Configure diagnostic settings to Log Analytics
  Milestone: 2025-12-01
  Cost: $50/month

[... 4 more entries]

Total Remediation Cost: $250/month
Total Entries: 7
Target Completion: 2025-12-10

📄 Document: POAM_Production_20251119.xlsx
📧 Notifications sent to: security-team@company.com
```

## Integration with Other Agents

### → Compliance Agent
Compliance Agent scans resources → Document Agent generates SSP/SAR/POA&M

### → Infrastructure Agent
Infrastructure Agent deploys → Document Agent creates architecture documentation

### → ATO Preparation Agent
ATO Preparation orchestrates → Document Agent generates all ATO documents

## Troubleshooting

### Issue: Template Not Found

**Symptom**: "Template XYZ not found"

**Solutions:**
```bash
# List available templates
az storage blob list \
  --account-name atodocuments \
  --container-name templates

# Upload missing template
az storage blob upload \
  --account-name atodocuments \
  --container-name templates \
  --name ssp_template.docx \
  --file ./ssp_template.docx
```

### Issue: Document Generation Fails

**Symptom**: "Failed to generate document"

**Solutions:**
```
1. Check resource access:
   - Verify subscription access
   - Ensure resources are discoverable

2. Check template:
   - Validate template format
   - Check for corrupted files

3. Check storage:
   - Verify storage account access
   - Check container permissions
   - Ensure sufficient storage space
```

## Performance

| Operation | Typical Duration |
|-----------|-----------------|
| Control narrative generation | 5-15 seconds |
| SSP generation (325 controls) | 3-8 minutes |
| POA&M creation | 30-60 seconds |
| Document export (PDF) | 10-30 seconds |
| Evidence attachment | 2-5 seconds |

## Limitations

- **AI Generation**: May require human review for accuracy
- **Template Compatibility**: DOCX templates only
- **Document Size**: 50 MB maximum
- **Concurrent Generation**: One document at a time per agent

## References

- [NIST SP 800-18 Rev 1](https://csrc.nist.gov/publications/detail/sp/800-18/rev-1/final) - Guide for Developing Security Plans
- [FedRAMP Templates](https://www.fedramp.gov/templates/) - Official FedRAMP document templates
- [DoD RMF Knowledge Service](https://rmf.org/) - Risk Management Framework guidance

---

**Last Updated**: November 2025  
**Version**: 0.6.35  
**Agent Type**: `Compliance` (Document sub-agent)
