# Defender for Cloud Integration Guide

**Last Updated:** November 14, 2025  
**Purpose:** Integrate Microsoft Defender for Cloud as a data source for Compliance Agent

## 📋 Overview

This guide shows how to integrate Microsoft Defender for Cloud (DFC) findings into your Compliance Agent's NIST 800-53 assessments.

**Key Benefits:**
- Leverage existing DFC security assessments
- Map security findings to NIST controls automatically
- **Intelligent deduplication** - merge DFC findings with assessment findings
- Avoid duplicate scanning and duplicate findings to users
- Enrich compliance reports with DFC secure scores
- Combine DFC recommendations with compliance-specific checks

## 🔄 Integration Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Compliance Agent                         │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │   CompliancePlugin.RunComplianceAssessment()         │  │
│  └─────────────────────┬────────────────────────────────┘  │
│                        │                                    │
│                        ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │      ComplianceEngine                                │  │
│  │                                                      │  │
│  │  1. Get DFC findings ──────┐                        │  │
│  │  2. Map to NIST controls    │                       │  │
│  │  3. Add compliance checks   │                       │  │
│  │  4. Generate artifacts      │                       │  │
│  └───────────────┬─────────────┼─────────────────────  ──┘  │
│                  │             │                            │
└──────────────────┼─────────────┼────────────────────────────┘
                   │             │
                   │             ▼
                   │    ┌──────────────────────────┐
                   │    │ DefenderForCloudService  │
                   │    │                          │
                   │    │ - GetAssessments()       │
                   │    │ - MapToNistControls()    │
                   │    │ - GetSecureScore()       │
                   │    └────────┬─────────────────┘
                   │             │
                   │             ▼
                   │    ┌──────────────────────────┐
                   │    │  Azure Security Center   │
                   │    │  REST API / SDK          │
                   │    └──────────────────────────┘
                   │
                   ▼
          ┌──────────────────────────┐
          │  Azure Policy Insights   │
          │  Compliance-specific     │
          │  checks                  │
          └──────────────────────────┘
```

## 🛠️ Implementation Steps

### Step 1: Add NuGet Package

Add the Azure Security Center SDK to your Compliance Agent project:

```xml
<!-- Platform.Engineering.Copilot.Compliance.Agent.csproj -->
<PackageReference Include="Azure.ResourceManager.SecurityCenter" Version="1.2.0" />
```

### Step 2: Register DFC Service

Register the DefenderForCloudService in your DI container:

```csharp
// Extensions/ComplianceAgentCollectionExtensions.cs
public static IServiceCollection AddComplianceAgent(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // ... existing services ...
    
    // Add Defender for Cloud integration
    services.AddSingleton<IDefenderForCloudService, DefenderForCloudService>();
    
    return services;
}
```

### Step 3: Update ComplianceEngine to Use DFC

Modify your `ComplianceEngine` to fetch and map DFC findings:

```csharp
// Services/Compliance/ComplianceEngine.cs
public class ComplianceEngine : IComplianceEngine
{
    private readonly IDefenderForCloudService _defenderService;
    private readonly IAzureResourceService _azureResourceService;
    private readonly ILogger<ComplianceEngine> _logger;

    public async Task<ComplianceAssessment> RunComprehensiveAssessmentAsync(
        string subscriptionId,
        string? resourceGroupName = null,
        IProgress<AssessmentProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<AtoFinding>();
        
        // STEP 1: Get Defender for Cloud findings
        _logger.LogInformation("Fetching Defender for Cloud findings...");
        var defenderFindings = await _defenderService.GetSecurityAssessmentsAsync(
            subscriptionId, 
            resourceGroupName, 
            cancellationToken);
        
        // STEP 2: Map DFC findings to NIST controls
        _logger.LogInformation("Mapping {Count} DFC findings to NIST controls", 
            defenderFindings.Count);
        var nistMappedFindings = _defenderService.MapDefenderFindingsToNistControls(
            defenderFindings, 
            subscriptionId);
        
        // STEP 3: Add compliance-specific checks (DFC doesn't cover)
        _logger.LogInformation("Running compliance-specific checks...");
        var complianceOnlyFindings = await RunComplianceSpecificChecks(
            subscriptionId, 
            resourceGroupName, 
            cancellationToken);
        
        // STEP 4: Merge and deduplicate findings
        _logger.LogInformation("Deduplicating findings...");
        var mergedFindings = MergeAndDeduplicateFindings(
            nistMappedFindings, 
            complianceOnlyFindings);
        
        // STEP 5: Get DFC secure score for context
        var secureScore = await _defenderService.GetSecureScoreAsync(
            subscriptionId, 
            cancellationToken);
        
        // STEP 6: Generate assessment with deduplicated findings
        return new ComplianceAssessment
        {
            SubscriptionId = subscriptionId,
            ResourceGroupName = resourceGroupName,
            Findings = mergedFindings,
            DefenderSecureScore = secureScore.Percentage,
            AssessmentDate = DateTime.UtcNow,
            // ... rest of assessment
        };
    }
    
    /// <summary>
    /// Merge DFC findings with compliance findings and remove duplicates
    /// Prioritizes keeping the finding with the most detailed information
    /// </summary>
    private List<AtoFinding> MergeAndDeduplicateFindings(
        List<AtoFinding> dfcFindings,
        List<AtoFinding> complianceFindings)
    {
        var mergedFindings = new List<AtoFinding>();
        var processedKeys = new HashSet<string>();
        
        // Create lookup for all findings by dedup key
        var allFindings = dfcFindings.Concat(complianceFindings).ToList();
        
        foreach (var finding in allFindings)
        {
            // Generate deduplication key based on resource + control + issue type
            var dedupKey = GenerateFindingKey(finding);
            
            if (!processedKeys.Contains(dedupKey))
            {
                // Find all findings with the same key
                var duplicates = allFindings
                    .Where(f => GenerateFindingKey(f) == dedupKey)
                    .ToList();
                
                if (duplicates.Count > 1)
                {
                    // Merge duplicate findings, prioritizing DFC for security details
                    var mergedFinding = MergeDuplicateFindings(duplicates);
                    mergedFindings.Add(mergedFinding);
                    _logger.LogDebug("Merged {Count} duplicate findings for key: {Key}", 
                        duplicates.Count, dedupKey);
                }
                else
                {
                    mergedFindings.Add(finding);
                }
                
                processedKeys.Add(dedupKey);
            }
        }
        
        _logger.LogInformation(
            "Deduplication complete: {Original} findings → {Deduplicated} unique findings ({Removed} duplicates removed)", 
            allFindings.Count, 
            mergedFindings.Count,
            allFindings.Count - mergedFindings.Count);
        
        return mergedFindings;
    }
    
    /// <summary>
    /// Generate a deduplication key for a finding based on:
    /// - Resource ID (same resource)
    /// - NIST Controls (same control violation)
    /// - Issue type (similar problem)
    /// </summary>
    private string GenerateFindingKey(AtoFinding finding)
    {
        // Normalize resource ID (remove subscription/resource group variations)
        var normalizedResourceId = finding.ResourceId
            ?.Split('/')
            .LastOrDefault() ?? "unknown";
        
        // Use first affected NIST control as primary control
        var primaryControl = finding.AffectedNistControls
            ?.FirstOrDefault() ?? "unknown";
        
        // Extract key words from title to identify similar issues
        var issueType = ExtractIssueType(finding.Title, finding.Description);
        
        return $"{normalizedResourceId}|{primaryControl}|{issueType}".ToLowerInvariant();
    }
    
    /// <summary>
    /// Extract issue type from finding title/description
    /// Examples: "encryption", "mfa", "logging", "network"
    /// </summary>
    private string ExtractIssueType(string title, string description)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        
        // Common issue type patterns
        if (text.Contains("encryption") || text.Contains("encrypt")) return "encryption";
        if (text.Contains("mfa") || text.Contains("multi-factor")) return "mfa";
        if (text.Contains("logging") || text.Contains("audit") || text.Contains("log")) return "logging";
        if (text.Contains("network") || text.Contains("nsg") || text.Contains("firewall")) return "network";
        if (text.Contains("backup") || text.Contains("recovery")) return "backup";
        if (text.Contains("vulnerability") || text.Contains("patch")) return "vulnerability";
        if (text.Contains("access") || text.Contains("rbac") || text.Contains("permission")) return "access";
        if (text.Contains("endpoint") || text.Contains("private link")) return "endpoint";
        if (text.Contains("tls") || text.Contains("ssl") || text.Contains("https")) return "tls";
        
        // Default: use first significant word from title
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 2 ? words[2].ToLowerInvariant() : "general";
    }
    
    /// <summary>
    /// Merge multiple duplicate findings into a single comprehensive finding
    /// Combines information from all sources, prioritizing DFC for security details
    /// </summary>
    private AtoFinding MergeDuplicateFindings(List<AtoFinding> duplicates)
    {
        // Prioritize findings: DFC > Compliance scans > others
        var dfcFinding = duplicates.FirstOrDefault(f => 
            f.Metadata.ContainsKey("Source") && 
            f.Metadata["Source"].ToString() == "Defender for Cloud");
        
        var baseFinding = dfcFinding ?? duplicates.First();
        
        // Merge affected controls from all duplicates
        var allControls = duplicates
            .SelectMany(f => f.AffectedNistControls ?? new List<string>())
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        
        // Combine remediation guidance
        var allGuidance = duplicates
            .Where(f => !string.IsNullOrEmpty(f.RemediationGuidance))
            .Select(f => f.RemediationGuidance)
            .Distinct()
            .ToList();
        
        var mergedGuidance = allGuidance.Count > 1
            ? string.Join("\n\nAlternative approach:\n", allGuidance)
            : allGuidance.FirstOrDefault() ?? baseFinding.RemediationGuidance;
        
        // Take the highest severity
        var highestSeverity = duplicates
            .Select(f => f.Severity)
            .OrderBy(s => GetSeverityPriority(s))
            .First();
        
        // Merge metadata
        var mergedMetadata = new Dictionary<string, object>(baseFinding.Metadata);
        mergedMetadata["MergedFrom"] = duplicates.Count;
        mergedMetadata["Sources"] = duplicates
            .Select(f => f.Metadata.ContainsKey("Source") ? f.Metadata["Source"].ToString() : "Compliance Scan")
            .Distinct()
            .ToList();
        
        return new AtoFinding
        {
            Id = baseFinding.Id,
            ResourceId = baseFinding.ResourceId,
            ResourceName = baseFinding.ResourceName,
            ResourceType = baseFinding.ResourceType,
            SubscriptionId = baseFinding.SubscriptionId,
            ResourceGroupName = baseFinding.ResourceGroupName,
            FindingType = baseFinding.FindingType,
            Severity = highestSeverity,
            ComplianceStatus = baseFinding.ComplianceStatus,
            Title = baseFinding.Title,
            Description = baseFinding.Description,
            Recommendation = baseFinding.Recommendation,
            RuleId = baseFinding.RuleId,
            RemediationGuidance = mergedGuidance,
            IsAutoRemediable = duplicates.Any(f => f.IsAutoRemediable),
            AffectedControls = allControls,
            AffectedNistControls = allControls,
            ComplianceFrameworks = baseFinding.ComplianceFrameworks,
            DetectedAt = baseFinding.DetectedAt,
            Evidence = baseFinding.Evidence,
            Metadata = mergedMetadata,
            IsRemediable = duplicates.Any(f => f.IsRemediable),
            RemediationActions = baseFinding.RemediationActions,
            RemediationStatus = baseFinding.RemediationStatus
        };
    }
    
    private int GetSeverityPriority(AtoFindingSeverity severity)
    {
        return severity switch
        {
            AtoFindingSeverity.Critical => 0,
            AtoFindingSeverity.High => 1,
            AtoFindingSeverity.Medium => 2,
            AtoFindingSeverity.Low => 3,
            _ => 4
        };
    }
    
    private async Task<List<AtoFinding>> RunComplianceSpecificChecks(
        string subscriptionId,
        string? resourceGroupName,
        CancellationToken cancellationToken)
    {
        // These are checks DFC doesn't cover:
        // - RMF-specific documentation requirements
        // - eMASS artifact validation
        // - Control family-specific configurations
        // - POA&M tracking
        // - Evidence collection completeness
        
        var findings = new List<AtoFinding>();
        
        // Example: Check if SSP exists
        var sspExists = await CheckSystemSecurityPlanExists(subscriptionId);
        if (!sspExists)
        {
            findings.Add(new AtoFinding
            {
                Title = "PL-2 - System Security Plan Missing",
                Description = "No System Security Plan found for this system",
                Severity = AtoFindingSeverity.High,
                AffectedNistControls = new List<string> { "PL-2" },
                IsAutoRemediable = false,
                RemediationGuidance = "Generate SSP using generate_emass_package function"
            });
        }
        
        // Example: Check control family tagging
        var resources = await _azureResourceService.ListAllResourcesAsync(
            subscriptionId, 
            resourceGroupName, 
            cancellationToken);
            
        foreach (var resource in resources)
        {
            if (!resource.Tags.ContainsKey("ControlFamily"))
            {
                findings.Add(new AtoFinding
                {
                    ResourceId = resource.Id,
                    ResourceName = resource.Name,
                    ResourceType = resource.Type,
                    Title = "CM-8 - Resource not tagged with Control Family",
                    Description = $"Resource {resource.Name} missing ControlFamily tag for inventory management",
                    Severity = AtoFindingSeverity.Low,
                    AffectedNistControls = new List<string> { "CM-8" },
                    IsAutoRemediable = true
                });
            }
        }
        
        return findings;
    }
}
```

## 📊 DFC to NIST Control Mapping

The `DefenderForCloudService` uses this mapping logic:

| DFC Assessment | NIST Controls | Rationale |
|----------------|---------------|-----------|
| MFA Required | AC-2, IA-2, IA-5 | Account management, identification/authentication |
| NSG Missing | SC-7, AC-4 | Boundary protection, information flow enforcement |
| Encryption at Rest | SC-28, SC-13 | Protection of data at rest, cryptography |
| Diagnostic Logs | AU-2, AU-3, AU-12 | Audit events, content, generation |
| Vulnerability Assessment | RA-5, SI-2 | Vulnerability monitoring, flaw remediation |
| Secure Configuration | CM-6, CM-7 | Configuration settings, least functionality |

See `DefenderForCloudService.cs` for the complete mapping table.

## 🧪 Testing the Integration

### Test 1: Verify DFC Findings Are Retrieved

```bash
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Check compliance for subscription 00000000-0000-0000-0000-000000000000"
  }' | jq .
```

**Expected Output:**
```json
{
  "findings": [
    {
      "title": "AC-2 - MFA not enabled for admin accounts",
      "source": "Defender for Cloud",
      "severity": "High",
      "affectedNistControls": ["AC-2", "IA-2"]
    }
  ],
  "defenderSecureScore": 67.5,
  "complianceScore": 72.3
}
```

### Test 2: Verify DFC + Compliance Checks Combined & Deduplicated

The agent should show:
- ✅ DFC findings mapped to controls (e.g., "AC-2 - MFA Required")
- ✅ Compliance-specific findings (e.g., "PL-2 - SSP Missing")
- ✅ **No duplicate findings** - if DFC and compliance scan find the same issue, only one merged finding appears
- ✅ Merged findings show multiple sources in metadata
- ✅ Both integrated in final report

**Example: Duplicate Detection**
```
Before Deduplication (10 findings):
- DFC: "AC-2 - MFA not enabled on VM-001"
- Compliance: "AC-2 - Multi-factor authentication missing on VM-001"
- DFC: "SC-7 - NSG missing on subnet-001"
- Compliance: "SC-7 - Network security group not configured on subnet-001"
...

After Deduplication (5 findings):
- "AC-2 - MFA not enabled on VM-001" (merged from 2 sources: DFC, Compliance)
- "SC-7 - NSG missing on subnet-001" (merged from 2 sources: DFC, Compliance)
...

Result: 50% reduction in duplicate findings shown to user
```

## 🎯 What DFC Provides vs. What You Add

### DFC Provides:
- ✅ Security posture (Secure Score)
- ✅ Vulnerability assessments
- ✅ Security recommendations
- ✅ Threat detection alerts
- ✅ Resource configuration checks

### Your Compliance Agent Adds:
- ✅ NIST 800-53 control mapping
- ✅ RMF process artifacts (SSP, SAR, POA&M)
- ✅ Evidence collection for ATO
- ✅ Control family-specific checks
- ✅ eMASS package generation
- ✅ DoD IL compliance validation
- ✅ Compliance timeline tracking

## 🚀 Advanced: Multi-Source Findings with Deduplication

Your agent can combine findings from multiple sources and intelligently deduplicate:

```csharp
public async Task<ComplianceAssessment> RunComprehensiveAssessmentAsync(...)
{
    var dfcFindings = new List<AtoFinding>();
    var policyFindings = new List<AtoFinding>();
    var advisorFindings = new List<AtoFinding>();
    var complianceFindings = new List<AtoFinding>();
    
    // Source 1: Defender for Cloud
    var dfcAssessments = await _defenderService.GetSecurityAssessmentsAsync(...);
    dfcFindings = _defenderService.MapDefenderFindingsToNistControls(dfcAssessments, subscriptionId);
    
    // Source 2: Azure Policy Insights
    var policyViolations = await _policyService.GetNonCompliantResources(...);
    policyFindings = _policyService.MapPolicyViolationsToNistControls(policyViolations);
    
    // Source 3: Azure Advisor
    var advisorRecs = await _advisorService.GetRecommendations(...);
    advisorFindings = _advisorService.MapAdvisorRecommendationsToNistControls(advisorRecs);
    
    // Source 4: Compliance-specific checks
    complianceFindings = await RunComplianceSpecificChecks(...);
    
    // Deduplicate across ALL sources
    var consolidatedFindings = MergeAndDeduplicateFindings(
        dfcFindings,
        policyFindings, 
        advisorFindings,
        complianceFindings);
    
    _logger.LogInformation(
        "Multi-source deduplication: {DFC} DFC + {Policy} Policy + {Advisor} Advisor + {Compliance} Compliance = {Total} unique findings",
        dfcFindings.Count, policyFindings.Count, advisorFindings.Count, 
        complianceFindings.Count, consolidatedFindings.Count);
    
    return new ComplianceAssessment
    {
        Findings = consolidatedFindings,
        Sources = new[] { "Defender for Cloud", "Azure Policy", "Azure Advisor", "Compliance Engine" },
        OriginalFindingCount = dfcFindings.Count + policyFindings.Count + 
                               advisorFindings.Count + complianceFindings.Count,
        DeduplicatedFindingCount = consolidatedFindings.Count,
        DuplicatesRemoved = (dfcFindings.Count + policyFindings.Count + 
                            advisorFindings.Count + complianceFindings.Count) - 
                            consolidatedFindings.Count
    };
}
```

## 🔍 Deduplication Strategy

### How Duplicates Are Detected

Findings are considered duplicates if they match on:

1. **Same Resource** - Normalized resource ID (e.g., `vm-prod-001`)
2. **Same Control** - Primary NIST control affected (e.g., `AC-2`)
3. **Same Issue Type** - Extracted from title/description (e.g., `mfa`, `encryption`, `logging`)

**Deduplication Key Format:**
```
{resourceId}|{primaryControl}|{issueType}
```

**Examples:**
- `vm-001|ac-2|mfa` - MFA issue on vm-001 for AC-2 control
- `storage-001|sc-28|encryption` - Encryption issue on storage-001 for SC-28 control
- `nsg-001|sc-7|network` - Network security issue on nsg-001 for SC-7 control

### Merging Strategy

When duplicates are found:

1. **Prioritize source**: DFC > Azure Policy > Compliance Scan
2. **Take highest severity** across all duplicates
3. **Merge NIST controls** from all sources
4. **Combine remediation guidance** from all sources
5. **Mark as auto-remediable** if ANY source supports it
6. **Track sources** in metadata for audit trail

**Example Merged Finding:**
```json
{
  "id": "finding-001",
  "title": "AC-2 - MFA not enabled for admin accounts",
  "severity": "High",
  "affectedNistControls": ["AC-2", "IA-2", "IA-5"],
  "remediationGuidance": "Enable MFA via Azure Portal...\n\nAlternative approach:\nUse Azure Policy to enforce MFA...",
  "isAutoRemediable": true,
  "metadata": {
    "MergedFrom": 3,
    "Sources": ["Defender for Cloud", "Azure Policy", "Compliance Scan"]
  }
}
```
```

## 📖 Related Files

- **Service Implementation:** `/src/Platform.Engineering.Copilot.Compliance.Agent/Services/Azure/Security/DefenderForCloudService.cs`
- **Test Suite:** `/docs/test cases/COMPLIANCE-AGENT-TEST-SUITE.md`
- **DI Registration:** `/src/Platform.Engineering.Copilot.Compliance.Agent/Extensions/ComplianceAgentCollectionExtensions.cs`

## ⚠️ Important Notes

1. **DFC is a Data Source, Not a Replacement**
   - Your agent orchestrates DFC + Policy + Advisor + compliance checks
   - DFC provides security findings; you map them to RMF requirements

2. **Avoid Duplication**
   - Don't re-scan what DFC already scanned
   - Focus your custom checks on RMF/ATO-specific requirements

3. **Authentication**
   - Ensure your `TokenCredential` has permissions to read Security Center data
   - Required RBAC: `Security Reader` or `Security Admin`

4. **Performance**
   - DFC API calls can be slow (30-60 seconds)
   - Consider caching findings for 1-4 hours
   - Run assessments asynchronously

---

**Last Updated:** November 13, 2025  
**Status:** Implementation guide - ready for development
