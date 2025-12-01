# Compliance Agent - Quick Start Guide

> **Get started with the Compliance Agent in 5 minutes - compliance assessments, remediation, and ATO documentation**

---

## 🎯 What the Compliance Agent Does

The Compliance Agent automates:
- ✅ **NIST 800-53 compliance assessments** - Scan Azure resources against federal controls
- ✅ **Automated remediation** - Fix compliance violations with PowerShell/Terraform
- ✅ **ATO document generation** - Create SSP, SAR, POA&M for federal authorization
- ✅ **Continuous monitoring** - Track compliance posture and findings
- ✅ **AI-enhanced documentation** - Professional control narratives with GPT-4

---

## 📋 Choose Your Quick Start Path

### Path 1: Run Your First Compliance Assessment (2 minutes)
**Goal:** Scan your Azure environment for NIST 800-53 violations  
**Jump to:** [Quick Assessment](#quick-assessment)

### Path 2: Generate ATO Documents (5 minutes)
**Goal:** Create SSP, SAR, POA&M for federal compliance  
**Jump to:** [ATO Package](#ato-package-generation)

### Path 3: Fix Compliance Issues (10 minutes)
**Goal:** Automatically remediate violations  
**Jump to:** [Automated Remediation](#automated-remediation)

---

## Quick Assessment

### Run Compliance Scan (2 minutes)

**Via Chat:**
```
Run compliance assessment for subscription abc-123-def-456
```

**With NIST baseline:**
```
Run NIST 800-53 High baseline assessment
```

⏱️ **Wait time:** 2-10 minutes (depends on resource count)

**What it scans:**
- Virtual machines (OS hardening, patching, encryption)
- Storage accounts (encryption at rest, access controls)
- Databases (TDE, firewall rules, audit logging)
- Networking (NSGs, DDoS, private endpoints)
- Key Vaults (RBAC, soft delete, logging)
- App Services (TLS versions, authentication)
- Kubernetes clusters (pod security, RBAC)
- Azure Policy compliance
- Defender for Cloud findings

**Output:**
```markdown
✅ Compliance Assessment Complete

📊 Overall Score: 78.5%
🔍 Resources Scanned: 147
❌ Findings: 23 violations

Top Violations by Control Family:
- AC (Access Control): 5 violations
- AU (Audit & Accountability): 3 violations
- SC (System Communications): 8 violations
- CM (Configuration Management): 7 violations

Critical Issues:
1. MFA not enforced for admin accounts (AC-2)
2. Encryption at rest disabled on 3 storage accounts (SC-28)
3. Network security groups allow unrestricted inbound (SC-7)
```

---

## ATO Package Generation

### Prerequisites
- ✅ Azure subscription access (Reader role minimum)
- ✅ Platform Engineering Copilot running
- ✅ Compliance assessment completed (required first!)

### 1️⃣ Set Your Subscription (One-time)

**Via Chat:**
```
Set subscription to production
```

**Or use subscription ID:**
```
Set subscription to abc-123-def-456
```

---

### 2️⃣ Generate System Security Plan (SSP)

**Basic:**
```
Generate SSP with system name "Azure Government Platform"
```

**Full parameters:**
```
Generate SSP with system name "My Platform", impact level IL4, 
classification UNCLASSIFIED, framework NIST80053R5
```

**Parameters:**
- `systemName`: Your system's official name
- `impactLevel`: IL2, IL4, IL5, IL6 (default: IL4)
- `classification`: UNCLASSIFIED, CUI, SECRET (default: UNCLASSIFIED)
- `framework`: NIST80053R5

📄 **Output:** SSP with NIST 800-53 control narratives, implementation status, responsibilities

**AI Enhancement:** If Azure OpenAI is configured, you'll get professional executive summaries and evidence-based control narratives.

---

### 3️⃣ Generate Security Assessment Report (SAR)
```
Generate SAR
```

**With parameters:**
```
Generate SAR for subscription abc-123 with classification UNCLASSIFIED
```

📄 **Output:** `SAR/{subscription-id}/sar_YYYY-MM-DD.pdf` in blob storage

**SAR Contents:**
- Executive summary
- Assessment methodology
- Findings by severity (Critical/High/Medium/Low)
- Control family analysis
- Risk ratings
- Remediation recommendations

---

### 4️⃣ Create Plan of Action & Milestones (POA&M)

**Via Chat:**
```
Create POA&M
```

**With AI enhancements:**
```
Create POA&M with AI risk narratives
```

📊 **Output:** Excel workbook with color-coded findings and remediation plans

**Color Coding:**
- 🔴 **Critical** - 15-day deadline
- 🟡 **High** - 30-day deadline
- 🟨 **Medium** - 90-day deadline
- ⚪ **Low** - 180-day deadline

**AI Enhancement:** AI-generated risk narratives and actionable milestones

---

### 5️⃣ Export Complete eMASS Package

**Via Chat:**
```
Generate eMASS package
```

📦 **Output:** Complete package ready for eMASS submission (SSP, SAR, POA&M, evidence)

---

## Automated Remediation

### Fix Compliance Violations

**View recommendations:**
```
Show me remediation options for critical findings
```

**Execute automated fixes:**
```
Execute automated remediation for AC-2 violations
```

**PowerShell example:**
```
Execute this PowerShell script:
Set-AzContext -SubscriptionId "abc-123"
Enable-AzSecurityAutoProvisioning -Name "default"
```

**Terraform example:**
```
Execute Terraform to deploy Azure Policy baseline
```

**What gets fixed:**
- ✅ Enable MFA for admin accounts (AC-2)
- ✅ Configure encryption at rest (SC-28)
- ✅ Harden NSGs (SC-7)
- ✅ Enable audit logging (AU-2)
- ✅ Apply Azure Policy baselines (CM-6)

**Safety features:**
- Script sanitization (15+ blocked commands)
- Resource scope validation
- Timeout protection (5 min)
- Retry logic (3 attempts)

**See:** [Script Execution Quick Start](QUICKSTART-SCRIPT-EXECUTION.md)

---

## Advanced Features

### AI-Enhanced Documents

Enable professional-quality documentation with Azure OpenAI:

**Configure:**
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

**What AI adds:**
- Professional executive summaries (800+ chars)
- Evidence-based control narratives
- Compliance gap analysis
- Risk narratives with business impact
- Actionable remediation milestones

**See:** [AI Documents Quick Start](QUICKSTART-AI-DOCUMENTS.md)

---

### Defender for Cloud Integration

**Integrate existing security findings:**
```
Integrate Defender for Cloud findings into compliance assessment
```

**Benefits:**
- Maps Defender recommendations to NIST controls
- Consolidated security posture
- Automated evidence collection
- Continuous compliance monitoring

**See:** [Defender Integration](../Integrations/DEFENDER-INTEGRATION.md)

---

### Repository Scanning

**Scan code repositories:**
```
Scan GitHub repository myorg/myrepo for compliance
```

**Checks:**
- Hardcoded secrets
- Configuration drift
- Security misconfigurations
- Policy violations in IaC

**See:** [Repository Scanning](../Integrations/REPOSITORY-SCANNING-GUIDE.md)

---

## Common Tasks

### Check Compliance Status

```
What's my current compliance status?
```

**Output:**
```
📊 Compliance Status

Overall Score: 78.5%
Resources Scanned: 147
Findings: 23

By Control Family:
- AC (Access Control): 5 violations
- AU (Audit): 3 violations
- SC (Communications): 8 violations
- CM (Configuration): 7 violations
```

---

### Collect Evidence

**For specific controls:**
```
Collect evidence for AC-2 and AU-3
```

**For resource group:**
```
Collect evidence for resource group rg-prod
```

**Evidence collected:**
- Configuration snapshots
- Audit logs
- Policy assignments
- Security findings
- Resource inventory

---

### Export Documents

```
Export SSP as PDF
```

```
Export POA&M as DOCX
```

```
Export all compliance documents
```

---

## Troubleshooting

### "No assessment results found"
**Solution:** Run `Run compliance assessment` first

### "Subscription not found"
**Solution:** `Set subscription to abc-123-def-456`

### "AI enhancements not working"
**Solution:** Check Azure OpenAI config - automatic template fallback

### "Remediation script blocked"
**Solution:** Script sanitization detected dangerous commands

---

## Next Steps

### For ATO/RMF Process
1. ✅ Run compliance assessment
2. ✅ Generate eMASS package  
3. ✅ Review and remediate findings
4. ✅ Export for submission
5. 📚 [ATO Preparation Guide](../Complete%20Guides/ATO-DOCUMENT-PREPARATION-GUIDE.md)

### For AI Documentation
1. ✅ Configure Azure OpenAI
2. ✅ Generate AI-enhanced documents
3. 📚 [AI Documents Quick Start](QUICKSTART-AI-DOCUMENTS.md)

### For Automated Remediation
1. ✅ Review remediation options
2. ✅ Execute automated fixes
3. 📚 [Script Execution Quick Start](QUICKSTART-SCRIPT-EXECUTION.md)

### For Production Deployment
1. ✅ Set up RBAC
2. ✅ Configure Key Vault
3. ✅ Enable continuous monitoring
4. 📚 [Setup & Configuration](../Setup%20%26%20Configuration/SETUP-CONFIGURATION.md)

---

## Quick Reference

| Task | Command | Time |
|------|---------|------|
| **Compliance Assessment** | `Run compliance assessment` | 2-10 min |
| **Generate SSP** | `Generate SSP with system name "..."` | 1-3 min |
| **Generate SAR** | `Generate SAR` | 1-2 min |
| **Generate POA&M** | `Create POA&M` | 1-2 min |
| **Complete Package** | `Generate eMASS package` | 3-5 min |
| **Remediate** | `Execute automated remediation` | 5-30 min |
| **Check Status** | `What's my compliance status?` | <1 min |
| **Collect Evidence** | `Collect evidence for AC-2` | 1-2 min |
| **Export** | `Export all documents as PDF` | 1-2 min |

---

**Need Help?** See [Quick Reference Guide](../QUICK-REFERENCE.md) for comprehensive overview.

**Status:** Production-ready | **Last Updated:** November 26, 2025
