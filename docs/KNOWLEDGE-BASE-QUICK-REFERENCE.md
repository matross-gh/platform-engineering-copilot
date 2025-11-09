# RMF/STIG Knowledge Base - Quick Reference

## 🎯 Quick Start

### Available Knowledge Base Functions

| Function | Purpose | Example Query |
|----------|---------|---------------|
| `explain_rmf_process` | RMF overview or specific step | "Explain RMF Step 4" |
| `get_rmf_deliverables` | Required documents per RMF step | "What docs for RMF Step 2?" |
| `explain_stig` | Detailed STIG control info | "Explain STIG V-219153" |
| `search_stigs` | Search STIGs by keyword | "STIGs for encryption" |
| `get_stigs_for_nist_control` | STIGs implementing NIST control | "STIGs for AC-2" |
| `get_control_mapping` | NIST↔STIG↔CCI↔DoD mapping | "Map IA-2(1)" |
| `explain_dod_instruction` | DoD instruction details | "Explain DoDI 8500.01" |
| `search_dod_instructions` | Search instructions by topic | "DoD cybersecurity policy" |
| `get_navy_ato_process` | Navy ATO workflow (8 steps) | "How to get ATO?" |
| `get_pmw_deployment_process` | PMW cloud deployment (6 steps) | "PMW deployment process" |
| `get_emass_registration_process` | eMASS registration (5 steps) | "Register in eMASS" |
| `explain_impact_level` | IL requirements (IL2-IL6) | "What is IL5?" |

## 📚 Knowledge Base Content

### RMF Process (6 Steps)
1. **Categorize** - FIPS 199 analysis, impact level
2. **Select** - Control baseline selection
3. **Implement** - Control implementation, STIG compliance
4. **Assess** - SAP/SAR, vulnerability scanning
5. **Authorize** - ATO decision by AO
6. **Monitor** - Continuous monitoring, POA&M

### Azure STIGs (5 Controls)
- **V-219153** - Azure AD MFA (High)
- **V-219187** - No public IPs on VMs (High)
- **V-219165** - Storage encryption at rest (High)
- **V-219201** - SQL TLS 1.2 minimum (Medium)
- **V-219178** - Key Vault purge protection (Medium)

### DoD Instructions (5)
- **DoDI 8500.01** - Cybersecurity
- **DoDI 8510.01** - RMF for DoD IT
- **DoDI 8520.02** - PKI and CAC/PIV
- **DoDI 8140.01** - Cyberspace Workforce
- **CNSSI 1253** - Security for NSS

### Navy Workflows (3)
- **WF-NAV-ATO-001** - Navy RMF/ATO (8 steps, 20-60 weeks)
- **WF-PMW-CLOUD-001** - PMW cloud deployment (6 steps)
- **WF-NAV-EMASS-001** - eMASS registration (5 steps)

### Impact Levels (4)
- **IL2** - DoD CUI Low (NIST 800-171)
- **IL4** - DoD CUI Moderate (FedRAMP Moderate)
- **IL5** - DoD CUI High / Secret (Complete isolation)
- **IL6** - Secret / Top Secret (Air-gapped)

## 💡 Common Use Cases

### "I need to get an ATO"
```
@platform How do I get an ATO in the Navy?
→ Returns 8-step Navy ATO process with timelines
```

### "What's this STIG about?"
```
@platform Explain STIG V-219153
→ Returns MFA requirement with Azure implementation
```

### "How do I implement this NIST control?"
```
@platform Map IA-2(1) to STIGs
→ Returns STIG V-219153, implementation guidance
```

### "What are IL5 requirements?"
```
@platform Explain Impact Level 5
→ Returns IL5 networking, encryption, logging requirements
```

### "What docs do I need for RMF?"
```
@platform What documents do I need for RMF Step 4?
→ Returns SAP, SAR, POA&M, scan results
```

## 🔍 Search Examples

### STIG Search
```
@platform What STIGs apply to encryption?
→ V-219165 (Storage encryption)
→ V-219201 (TLS encryption)
```

### DoD Instruction Search
```
@platform DoD cybersecurity policy
→ DoDI 8500.01 - Cybersecurity
```

## 📖 Example Responses

### RMF Step Explanation
```
User: @platform What is RMF Step 4?

Response:
# RMF Step 4: Assess

Assess security controls to determine effectiveness

## Activities

1. Develop Security Assessment Plan (SAP)
2. Conduct security control assessment
3. Vulnerability scanning and penetration testing
4. Review control implementation evidence
5. Document findings in Security Assessment Report (SAR)

## Key Deliverables

- Security Assessment Plan (SAP)
- Security Assessment Report (SAR)
- Vulnerability scan results
- Penetration test results
- Plan of Action and Milestones (POA&M)

## Responsible Roles

- Security Control Assessor (SCA)
- Independent Assessor
- ISSO
- Penetration Testers

## DoD Guidance

DoD 8510.01, NIST 800-53A
```

### STIG Explanation
```
User: @platform Explain STIG V-219153

Response:
# V-219153: Azure AD authentication must use multi-factor authentication

**Severity:** High
**Category:** Identity and Access Management

## Description

Multi-factor authentication (MFA) is required for all Azure AD 
privileged accounts to prevent unauthorized access.

## NIST 800-53 Controls

IA-2(1), IA-2(2), IA-2(8), AC-2

## Check Procedure

Verify that MFA is enabled for all privileged Azure AD accounts.
Check Azure AD Security > Multi-Factor Authentication settings.

## Remediation

Enable MFA for all privileged Azure AD accounts:
1. Navigate to Azure AD > Security > Multi-Factor Authentication
2. Select users with privileged roles
3. Enable MFA and configure trusted devices
4. Test MFA functionality

## Azure Implementation

**Service:** Azure AD
**Configuration:** Conditional Access Policies, MFA Settings
**Azure Policy:** Enable MFA for privileged accounts

### Automation Command

```bash
az ad user update --id <user> --force-change-password-next-login true && Enable-AzureADMFA
```
```

### Navy ATO Process
```
User: @platform How do I get an ATO?

Response:
# Navy RMF ATO Process

**Total Duration:** 20-60 weeks
**Impact Level:** All levels (IL2-IL6)

## Workflow Steps

### Step 1: Initiate RMF Process (1-2 weeks)
- Identify system owner, ISSO, ISSM, AO
- Establish security team
...

### Step 8: Continuous Monitoring (Ongoing)
- Implement continuous monitoring
- Track POA&M progress
- Conduct annual assessments

## Required Documents

- System Security Plan (SSP)
- Security Assessment Plan (SAP)
- Security Assessment Report (SAR)
- Plan of Action & Milestones (POA&M)
- Authorization Decision Memo
- Continuous Monitoring Plan

## Approval Authorities

- Authorizing Official (AO)
- Senior Information Security Officer (SISO)
- Navy CISO (for high-impact systems)
```

## 🚀 Integration Checklist

- [ ] Register services in DI container
- [ ] Register plugin with Compliance Agent
- [ ] Copy knowledge base JSON files to output
- [ ] Update Compliance Agent system prompt
- [ ] Test RMF queries
- [ ] Test STIG queries
- [ ] Test Navy workflow queries
- [ ] Test DoD instruction queries
- [ ] Test Impact Level queries

## 📁 File Locations

**Models:** `Core/Models/KnowledgeBase/KnowledgeBaseModels.cs`  
**Interfaces:** `Core/Interfaces/KnowledgeBase/IKnowledgeBaseServices.cs`  
**Services:** `Compliance.Agent/Services/KnowledgeBase/`  
**Plugin:** `Compliance.Agent/Plugins/KnowledgeBasePlugin.cs`  
**Data:** `Core/KnowledgeBase/*.json`  
**Docs:** `docs/KNOWLEDGE-BASE-IMPLEMENTATION.md`  

## 🔧 Troubleshooting

### Knowledge base data not found
- Ensure JSON files are copied to output directory
- Check build action: `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`

### Plugin not responding
- Verify plugin is registered with kernel
- Check DI registration for all services
- Verify ComplianceAgent constructor includes KnowledgeBasePlugin

### Cache not working
- Ensure IMemoryCache is registered in DI
- Default cache duration: 24 hours
- Clear cache: Restart application

## 📞 Support

- **Documentation:** `docs/KNOWLEDGE-BASE-IMPLEMENTATION.md`
- **Phase 1 Compliance:** `docs/PHASE1-COMPLIANCE.md`
- **Implementation Summary:** `docs/IMPLEMENTATION-SUMMARY-RMF-STIG.md`
