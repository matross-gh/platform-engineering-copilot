# Knowledge Base Agent

> DoD/NIST compliance knowledge retrieval and question answering specialist

## Overview

The Knowledge Base Agent is a specialized AI agent that provides expert knowledge about NIST 800-53, NIST 800-171, FedRAMP, DoD Cloud Computing SRG, RMF, STIG, and other compliance frameworks through natural language queries.

**Agent Type**: `KnowledgeBase`  
**Icon**: 📚  
**Temperature**: 0.2 (high precision for factual information)

## Capabilities

### 1. Knowledge Domains

#### NIST 800-53 Rev 5
Comprehensive coverage of all security and privacy controls:

**18 Control Families:**
- **AC**: Access Control
- **AU**: Audit and Accountability  
- **AT**: Awareness and Training
- **CM**: Configuration Management
- **CP**: Contingency Planning
- **IA**: Identification and Authentication
- **IR**: Incident Response
- **MA**: Maintenance
- **MP**: Media Protection
- **PE**: Physical and Environmental Protection
- **PL**: Planning
- **PS**: Personnel Security
- **RA**: Risk Assessment
- **CA**: Security Assessment and Authorization
- **SC**: System and Communications Protection
- **SI**: System and Information Integrity
- **SA**: System and Services Acquisition
- **PM**: Program Management

**1000+ Controls** with full descriptions, implementation guidance, and assessment procedures.

#### NIST 800-171
Protecting Controlled Unclassified Information (CUI):

- **110 Security Requirements** across 14 families
- **CUI Protection**: Safeguarding sensitive government information
- **DoD Contractor Requirements**: Compliance for defense contractors
- **Assessment Procedures**: Self-assessment guidance
- **CMMC Alignment**: Cybersecurity Maturity Model Certification mapping

#### FedRAMP (Federal Risk and Authorization Management Program)

**Authorization Process:**
- FedRAMP High, Moderate, Low baselines
- Tailored for LI-SaaS (Low Impact Software-as-a-Service)
- 3PAO assessment procedures
- ConMon (Continuous Monitoring) requirements
- SSP (System Security Plan) templates
- SAR (Security Assessment Report) formats

**Baseline Controls:**
- **FedRAMP High**: 421 controls
- **FedRAMP Moderate**: 325 controls  
- **FedRAMP Low**: 125 controls
- **FedRAMP LI-SaaS**: 133 controls

#### DoD Cloud Computing SRG

**Impact Levels (IL):**
- **IL2**: Public information, low confidentiality
- **IL4**: CUI, moderate confidentiality
- **IL5**: CUI + some classified, high confidentiality
- **IL6**: Classified information (Secret)

**Requirements per Impact Level:**
- IL2: 129 controls
- IL4: 183 controls
- IL5: 209 controls
- IL6: 330 controls

#### Risk Management Framework (RMF)

**6-Step Process:**
1. **Categorize**: System categorization based on impact
2. **Select**: Choose security controls from baselines
3. **Implement**: Deploy controls in the system
4. **Assess**: Evaluate control effectiveness
5. **Authorize**: Senior official authorization decision
6. **Monitor**: Continuous monitoring and assessment

#### STIG/SCAP (Security Technical Implementation Guides)

**Coverage:**
- **Operating Systems**: Windows Server, RHEL, Ubuntu
- **Databases**: SQL Server, Oracle, PostgreSQL
- **Applications**: IIS, Apache, Kubernetes
- **Network Devices**: Firewalls, switches, routers
- **Cloud Services**: Azure, AWS, GCP

#### eMASS (Enterprise Mission Assurance Support Service)

**Functionality:**
- ATO package submission portal
- POA&M (Plan of Action & Milestones) tracking
- Test result management
- Control assessment documentation
- Artifact uploads (SSP, SAR, SAP, etc.)

### 2. Query Types

#### Control Lookup by ID

```
Query: "What is NIST 800-53 control AC-2?"

Response:
📚 NIST 800-53 Rev 5: AC-2

Control ID: AC-2
Title: Account Management
Family: Access Control (AC)
Baseline: Low, Moderate, High

Description:
The organization manages information system accounts by:
a. Identifying account types (individual, group, system, etc.)
b. Establishing conditions for group and role membership
c. Specifying authorized users, group/role membership, access authorizations
d. Requiring approvals for requests to create accounts
e. Creating, enabling, modifying, disabling, and removing accounts
f. Monitoring information system account use
g. Notifying account managers when accounts are no longer required
h. Authorizing access based on intended use
i. Reviewing accounts for compliance with account management requirements
j. Establishing processes for reissuing accounts

Related Controls: AC-3, AC-5, AC-6, IA-2, IA-4, IA-5, IA-8, AU-9

Implementation Guidance:
Organizations should implement automated mechanisms for account management
where feasible. This includes automated notifications to account managers
when temporary accounts expire or when users are terminated.

Assessment Procedures:
Examine:
- Account management policies and procedures
- List of information system accounts
- Account creation/modification/deletion records
- Access authorization documentation

Interview:
- System administrators
- Account managers
- Security personnel

Test:
- Account creation process
- Account modification workflow
- Account deletion procedures
- Account review processes
```

#### Implementation Guidance

```
Query: "How do I implement AU-2 (Audit Events)?"

Response:
📚 Implementation Guide: AU-2 (Audit Events)

Azure Implementation:

1. Enable Diagnostic Settings
   - All Azure resources should send logs to Log Analytics
   - Configure retention period (minimum 90 days for FedRAMP)
   - Enable all log categories

2. Log Analytics Workspace
   - Centralized logging repository
   - Query language (KQL) for analysis
   - Alert rules for security events

3. Azure Monitor
   - Activity logs (control plane operations)
   - Resource logs (data plane operations)
   - Metrics for performance tracking

4. Events to Audit:
   Required Events:
   - User account creation, modification, deletion
   - Privilege escalations (RBAC changes)
   - Authentication events (successful/failed logins)
   - Resource access (read, write, delete)
   - Configuration changes
   - Security policy modifications
   - Audit log access

   Optional Events:
   - Application-specific events
   - Performance metrics
   - Network traffic logs

5. Bicep Template Example:
```bicep
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'audit-logs'
  scope: storageAccount
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'StorageRead'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
      {
        category: 'StorageWrite'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
    ]
  }
}
```

6. Compliance Evidence:
   - Screenshot of Diagnostic Settings configuration
   - Log Analytics workspace query showing retention
   - Sample audit log entries
   - Alert rules for security events

7. Assessment Questions:
   ✓ Are all required events being audited?
   ✓ Is the retention period compliant (90+ days)?
   ✓ Are logs stored in a centralized location?
   ✓ Are logs protected from unauthorized access?
   ✓ Are alerts configured for critical events?
```

#### Baseline Queries

```
Query: "Show all FedRAMP High baseline controls"

Response:
📚 FedRAMP High Baseline

Total Controls: 421

By Control Family:
AC (Access Control): 25 controls
AU (Audit and Accountability): 16 controls
AT (Awareness and Training): 5 controls
CM (Configuration Management): 11 controls
CP (Contingency Planning): 13 controls
IA (Identification and Authentication): 11 controls
IR (Incident Response): 10 controls
MA (Maintenance): 6 controls
MP (Media Protection): 8 controls
PE (Physical and Environmental): 20 controls
PL (Planning): 11 controls
PS (Personnel Security): 8 controls
RA (Risk Assessment): 10 controls
CA (Security Assessment): 9 controls
SC (System and Communications): 46 controls
SI (System and Information Integrity): 17 controls
SA (System and Services Acquisition): 22 controls
PM (Program Management): 16 controls

High-Priority Controls:
- AC-2: Account Management
- AC-3: Access Enforcement
- AU-2: Audit Events
- AU-12: Audit Generation
- SC-7: Boundary Protection
- SC-8: Transmission Confidentiality
- SC-13: Cryptographic Protection
- IA-2: Identification and Authentication
- IA-5: Authenticator Management

Full Baseline: fedramp_high_baseline.json
```

#### Control Family Information

```
Query: "Tell me about the AC (Access Control) family"

Response:
📚 Access Control (AC) Family

Overview:
The Access Control family includes controls that limit information system
access to authorized users, processes acting on behalf of authorized users,
or devices (including other systems). Access controls provide protection
against unauthorized access and are fundamental to information security.

NIST 800-53 Rev 5: 25 Controls

Key Controls:
- AC-1: Access Control Policy and Procedures
- AC-2: Account Management ⭐ Critical
- AC-3: Access Enforcement ⭐ Critical
- AC-4: Information Flow Enforcement
- AC-5: Separation of Duties
- AC-6: Least Privilege ⭐ Critical
- AC-7: Unsuccessful Logon Attempts
- AC-8: System Use Notification
- AC-17: Remote Access
- AC-18: Wireless Access
- AC-19: Access Control for Mobile Devices
- AC-20: Use of External Systems

Azure Implementation:
- **AC-2, AC-3, AC-6**: Azure RBAC (Role-Based Access Control)
- **AC-4**: Network Security Groups, Application Security Groups
- **AC-7**: Azure AD Conditional Access, Smart Lockout
- **AC-17**: VPN Gateway, Azure Bastion
- **AC-18**: Network Security Groups for wireless isolation

Common Challenges:
1. Over-privileged accounts (AC-6 violations)
2. Shared accounts (AC-2 violations)
3. No periodic access reviews (AC-2 requirement)
4. Missing separation of duties (AC-5)

Best Practices:
✓ Implement least privilege by default
✓ Use Azure AD PIM for just-in-time access
✓ Regular access reviews (quarterly minimum)
✓ Automated account lifecycle management
✓ Strong authentication (MFA) for all users
```

#### Compliance Mappings

```
Query: "Map NIST 800-171 to 800-53"

Response:
📚 NIST 800-171 to 800-53 Rev 5 Mapping

NIST 800-171 is a subset of NIST 800-53, focused on protecting
Controlled Unclassified Information (CUI) in non-federal systems.

Family Mapping:
3.1 Access Control (AC)
  → Maps to AC family in 800-53
  → 22 requirements vs. 25 controls

3.2 Awareness and Training (AT)
  → Maps to AT family in 800-53
  → 3 requirements vs. 5 controls

3.3 Audit and Accountability (AU)
  → Maps to AU family in 800-53
  → 9 requirements vs. 16 controls

3.4 Configuration Management (CM)
  → Maps to CM family in 800-53
  → 9 requirements vs. 11 controls

[... continues for all 14 families]

Example Detailed Mapping:
800-171 Requirement 3.1.1:
  "Limit information system access to authorized users"
  
  Maps to 800-53 Controls:
  - AC-2: Account Management
  - AC-3: Access Enforcement
  - AC-14: Permitted Actions Without Identification
  - IA-2: Identification and Authentication

Key Differences:
- 800-171: 110 requirements
- 800-53: 1000+ controls
- 800-171 is less prescriptive
- 800-53 provides more implementation detail
- 800-171 targets CUI, 800-53 targets all federal systems

Full Mapping: nist_800-171_to_800-53_mapping.xlsx
```

### 3. Response Format

Every knowledge base response includes:

- **Control ID**: Unique identifier (e.g., AC-2, AU-12)
- **Title**: Control name
- **Description**: What the control requires
- **Implementation Guidance**: How to implement (Azure-specific when applicable)
- **Assessment Procedures**: How to assess compliance
- **Related Controls**: Cross-references
- **References**: Links to official documentation

## Plugins

### KnowledgeBasePlugin

Main plugin for knowledge retrieval.

**Functions:**
- `lookup_control` - Get control details by ID
- `search_controls` - Search controls by keyword
- `get_baseline` - Retrieve baseline control lists
- `get_family_info` - Control family information
- `map_frameworks` - Cross-framework mappings
- `get_implementation_guidance` - How-to guides
- `get_assessment_procedures` - Testing guidance

### ConfigurationPlugin

Azure subscription management.

**Functions:**
- `set_azure_subscription` - Set active subscription
- `get_azure_subscription` - Get current subscription

## Example Prompts

### Control Lookup

```
"What is NIST 800-53 control AC-2?"
"Explain AU-2 (Audit Events)"
"Tell me about SC-7 (Boundary Protection)"
"Show me control IA-2 details"
"What does SC-13 require?"
```

### Implementation Guidance

```
"How do I implement AC-2 in Azure?"
"Implementation guide for AU-2"
"What Azure services satisfy SC-7?"
"How to configure IA-2 with Azure AD?"
"Best practices for SC-13 implementation"
```

### Baseline Queries

```
"Show all FedRAMP High baseline controls"
"List DoD IL5 required controls"
"What controls are in FedRAMP Moderate?"
"NIST 800-171 control list"
"FedRAMP Low baseline requirements"
```

### Family Information

```
"Tell me about the AC family"
"What controls are in the AU family?"
"Explain the SC family controls"
"Show all IA family controls"
"Overview of the CM family"
```

### Compliance Questions

```
"What's the difference between 800-53 and 800-171?"
"How do I get a FedRAMP ATO?"
"What is the RMF process?"
"Explain DoD Impact Levels"
"What is eMASS used for?"
"How does CMMC relate to NIST 800-171?"
```

### Framework Mappings

```
"Map NIST 800-171 to 800-53"
"How does FedRAMP relate to NIST 800-53?"
"What's the difference between IL4 and IL5?"
"Map FedRAMP High to DoD IL5"
```

## Key Services

| Service | Purpose |
|---------|---------|
| `KnowledgeBaseSearchService` | Semantic search over compliance documents |
| `ControlMappingService` | Cross-framework control mappings |
| `BaselineService` | Baseline control retrieval |
| `ImplementationGuidanceService` | Azure-specific implementation guides |

## Configuration

### appsettings.json

```json
{
  "KnowledgeBaseAgent": {
    "Temperature": 0.2,
    "MaxTokens": 2000,
    "EnableSemanticSearch": true,
    "EnableControlCaching": true
  },
  "KnowledgeBase": {
    "Endpoint": "https://knowledge-base-api.azurewebsites.net",
    "ApiKey": "your-api-key",
    "CacheDurationMinutes": 60,
    "SupportedFrameworks": [
      "NIST-800-53-Rev5",
      "NIST-800-171",
      "FedRAMP",
      "DoD-Cloud-SRG",
      "ISO-27001",
      "PCI-DSS",
      "HIPAA"
    ]
  }
}
```

### Knowledge Base Structure

```
knowledge-base/
├── nist-800-53-rev5/
│   ├── controls/
│   │   ├── AC-001.json
│   │   ├── AC-002.json
│   │   └── ... (1000+ controls)
│   ├── baselines/
│   │   ├── low.json
│   │   ├── moderate.json
│   │   └── high.json
│   └── families/
│       ├── AC.json
│       ├── AU.json
│       └── ... (18 families)
├── nist-800-171/
│   ├── requirements/
│   ├── mappings/
│   └── baselines/
├── fedramp/
│   ├── high-baseline.json
│   ├── moderate-baseline.json
│   ├── low-baseline.json
│   └── templates/
└── dod-cloud-srg/
    ├── IL2.json
    ├── IL4.json
    ├── IL5.json
    └── IL6.json
```

## Usage Patterns

### Pattern 1: Control Lookup

```
User: "What is AC-2?"

Agent Response:
📚 NIST 800-53 Rev 5: AC-2

Control ID: AC-2
Title: Account Management
Family: Access Control
Baseline: Low, Moderate, High

Description:
Organizations manage information system accounts by identifying account
types, establishing conditions for group membership, specifying authorized
users, requiring approvals, creating/disabling accounts, monitoring use,
and reviewing accounts periodically.

Azure Implementation:
✓ Azure AD for user accounts
✓ Azure RBAC for access control
✓ Azure AD PIM for privileged access
✓ Conditional Access policies
✓ Access reviews (quarterly recommended)

Related Controls: AC-3, AC-5, AC-6, IA-2, IA-4

📄 Full details: AC-2_implementation_guide.md
```

### Pattern 2: Implementation Guidance

```
User: "How do I implement AU-2 in Azure?"

Agent Response:
📚 Implementation Guide: AU-2 (Audit Events)

Azure Services:
1. Azure Monitor (Activity Logs)
2. Diagnostic Settings (Resource Logs)
3. Log Analytics Workspace
4. Azure Sentinel (Optional)

Step-by-Step:
1️⃣  Create Log Analytics Workspace
2️⃣  Configure Diagnostic Settings for all resources
3️⃣  Set retention to 90+ days (FedRAMP requirement)
4️⃣  Enable all log categories
5️⃣  Create alert rules for critical events

Required Events:
✓ Account creation/deletion
✓ Privilege changes
✓ Authentication (success/failure)
✓ Resource access
✓ Configuration changes

Bicep Example:
```bicep
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'audit-logs'
  scope: resourceToMonitor
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      { category: 'Audit', enabled: true }
      { category: 'Security', enabled: true }
    ]
  }
}
```

Evidence Needed:
- Diagnostic Settings screenshots
- Log Analytics retention configuration
- Sample audit log entries

📄 Complete guide: AU-2_Azure_Implementation.md
```

### Pattern 3: Baseline Query

```
User: "Show FedRAMP High controls"

Agent Response:
📚 FedRAMP High Baseline

Total Controls: 421

Top 20 Critical Controls:
✅ AC-2: Account Management
✅ AC-3: Access Enforcement
✅ AC-6: Least Privilege
✅ AU-2: Audit Events
✅ AU-12: Audit Generation
✅ SC-7: Boundary Protection
✅ SC-8: Transmission Confidentiality
✅ SC-13: Cryptographic Protection
✅ IA-2: Identification & Authentication
✅ IA-5: Authenticator Management
... (11 more)

By Control Family:
AC: 25 controls
AU: 16 controls
SC: 46 controls
IA: 11 controls
[... more families]

📊 Full baseline: fedramp_high_baseline.xlsx
📄 Control catalog: fedramp_high_controls.pdf
```

## Integration with Other Agents

### → Compliance Agent
Knowledge Base provides control details → Compliance Agent uses for assessments

### → Document Agent
Knowledge Base provides narratives → Document Agent uses for SSP generation

### → Infrastructure Agent
Knowledge Base provides requirements → Infrastructure Agent enhances templates

## Troubleshooting

### Issue: Control Not Found

**Symptom**: "Control XY-123 not found"

**Solutions:**
```
1. Verify control ID format:
   ✓ Correct: AC-2, AU-12, SC-7
   ✗ Wrong: AC2, AU.12, SC07

2. Check framework:
   - NIST 800-53: AC-2
   - NIST 800-171: 3.1.1
   - ISO 27001: A.9.2.1

3. Try alternate searches:
   "Account Management"
   "Audit Events"
   "Boundary Protection"
```

### Issue: Knowledge Base Unavailable

**Symptom**: "Knowledge base service unavailable"

**Solutions:**
```bash
# Check knowledge base endpoint
curl https://knowledge-base-api.azurewebsites.net/health

# Verify API key
echo $KNOWLEDGE_BASE_API_KEY

# Check network connectivity
ping knowledge-base-api.azurewebsites.net
```

## Performance

| Operation | Typical Duration |
|-----------|-----------------|
| Control lookup by ID | <1 second |
| Keyword search | 1-2 seconds |
| Baseline retrieval | 1-3 seconds |
| Framework mapping | 2-5 seconds |
| Implementation guide | 2-4 seconds |

## Limitations

- **Knowledge Base Updates**: Updated quarterly with NIST revisions
- **Framework Coverage**: Primary focus on NIST/FedRAMP/DoD frameworks
- **Azure-Specific**: Implementation guidance focused on Azure (AWS/GCP limited)
- **Language**: English only

## References

- [NIST 800-53 Rev 5](https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final)
- [NIST 800-171](https://csrc.nist.gov/publications/detail/sp/800-171/rev-2/final)
- [FedRAMP](https://www.fedramp.gov/)
- [DoD Cloud Computing SRG](https://dl.dod.cyber.mil/wp-content/uploads/cloud/pdf/Cloud_Computing_SRG_v1r3.pdf)
- [Risk Management Framework](https://csrc.nist.gov/projects/risk-management/about-rmf)

---

**Last Updated**: November 2025  
**Version**: 0.6.35  
**Agent Type**: `KnowledgeBase`
