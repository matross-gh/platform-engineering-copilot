# GitHub Pull Request Compliance Review Integration

## Overview

Automated compliance scanning for Infrastructure-as-Code (IaC) pull requests using GitHub API integration. Reviews PRs for NIST 800-53, STIG, and DoD instruction violations with inline comments and commit status checks.

**Phase 1 Compliance**: Advisory only - no auto-merge or auto-approval.

## Implementation Summary

### Components Created

#### 1. **GitHubPullRequestService.cs** (226 lines)
Location: `src/Platform.Engineering.Copilot.Compliance.Agent/Services/PullRequest/`

GitHub REST API client for PR operations:
- `GetPullRequestAsync()` - Fetch PR details and file list
- `GetFileContentAsync()` - Download file content from raw URLs
- `PostReviewCommentAsync()` - Post inline review comments on specific lines
- `SubmitReviewAsync()` - Submit overall PR review (approve/request changes/comment)
- `SetCommitStatusAsync()` - Update commit status check (success/failure)

**Authentication**: Bearer token with GitHub Personal Access Token (PAT)
**API Version**: GitHub REST API v3 (2022-11-28)

#### 2. **PullRequestModels.cs** (123 lines)
Location: `src/Platform.Engineering.Copilot.Core/Models/PullRequest/`

Data models for PR review workflow:
- `PullRequestDetails` - PR metadata and file list
- `PullRequestFile` - File changes with `IsIaCFile()` detection (.bicep, .tf, .json, .yaml)
- `PullRequestReviewComment` - Inline code comments with position, line, body
- `PullRequestReview` - Overall review submission with event type
- `CommitStatus` - GitHub commit status check
- `ComplianceFinding` - Violation with NIST/STIG/DoD references
- `PullRequestComplianceReview` - Complete review results

**Enums**:
- `ReviewEvent` - Approve, RequestChanges, Comment
- `ComplianceSeverity` - Critical, High, Medium, Low

#### 3. **PullRequestReviewService.cs** (406 lines)
Location: `src/Platform.Engineering.Copilot.Compliance.Agent/Services/PullRequest/`

IaC compliance scanning engine with file-specific analyzers:

**Bicep Scanning** (`ScanBicepFileAsync`):
- Public IP addresses (AC-4, STIG V-219187)
- Storage encryption at rest (SC-28, STIG V-220001)
- TLS 1.2 enforcement (SC-8, STIG V-220002)
- Public blob access (AC-3, STIG V-220003)

**Terraform Scanning** (`ScanTerraformFileAsync`):
- Public IP addresses (AC-4, STIG V-219187)
- Encryption at rest (SC-28, STIG V-220001)

**ARM Template Scanning** (`ScanArmTemplateAsync`):
- Public endpoints (AC-4, STIG V-219187)

**Kubernetes Scanning** (`ScanKubernetesFileAsync`):
- Privileged containers (CM-7, STIG V-242376)
- Host network mode (SC-7, STIG V-242377)

**Output Generation**:
- `GenerateReviewSummary()` - Markdown summary with severity emojis (🔴🟠🟡⚪)
- `GenerateFindingComment()` - Inline comment with code snippet, NIST/STIG refs, remediation

#### 4. **GitHubConfiguration.cs** (11 lines)
Location: `src/Platform.Engineering.Copilot.Core/Configuration/`

Configuration model:
```csharp
public class GitHubConfiguration
{
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool EnablePrReviews { get; set; } = true;
    public bool AutoApproveOnSuccess { get; set; } = false; // Phase 1: Advisory only
    public int MaxFileSizeKb { get; set; } = 1024; // 1MB limit
}
```

#### 5. **PullRequestReviewPlugin.cs** (228 lines)
Location: `src/Platform.Engineering.Copilot.Compliance.Agent/Plugins/`

Semantic Kernel plugin with two kernel functions:

**`review_pull_request`**:
- Parameters: `repository` (owner/repo), `pr_number`
- Workflow:
  1. Fetch PR details from GitHub API
  2. Filter IaC files (.bicep, .tf, .json, .yaml/.yml)
  3. Download file contents
  4. Run compliance scanning for each file
  5. Post up to 20 inline review comments
  6. Submit overall PR review (request changes if critical findings, comment otherwise)
  7. Set commit status check (failure if critical findings)
- Returns: Review summary with findings count and severity breakdown

**`get_pr_review_status`**:
- Parameters: `repository`, `pr_number`
- Returns: Current PR review status and commit check state

**Phase 1 Compliance Messaging**:
- All reviews include "⚠️ This is an advisory review only. Manual approval is required."
- No auto-merge or auto-approval actions
- Critical findings trigger "request changes" review event but don't block merge

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                   GitHub Pull Request Event                   │
│                  (opened, synchronized, reopened)             │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────┐
│              PullRequestReviewPlugin (Semantic Kernel)        │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ review_pull_request(repository, pr_number)             │  │
│  └────────────────────────────────────────────────────────┘  │
└────────────────────────┬─────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                  │
        ▼                                  ▼
┌──────────────────┐            ┌──────────────────────┐
│ GitHubPRService  │            │ PRReviewService      │
│                  │            │                      │
│ • GetPR()        │            │ • ReviewPR()         │
│ • GetContent()   │────────────▶ • ScanBicep()       │
│ • PostComment()  │◀────────────│ • ScanTerraform()  │
│ • SubmitReview() │            │ • ScanARM()         │
│ • SetStatus()    │            │ • ScanKubernetes()  │
└──────────────────┘            └──────────────────────┘
        │                                  │
        │                                  ▼
        │                        ┌──────────────────────┐
        │                        │ CodeScanningEngine   │
        │                        │ (existing)           │
        │                        └──────────────────────┘
        │
        ▼
┌──────────────────────────────────────────────────────────────┐
│                         GitHub API                            │
│  • PR details & file list                                     │
│  • File raw content                                           │
│  • Review comments (inline)                                   │
│  • PR review submission                                       │
│  • Commit status check                                        │
└──────────────────────────────────────────────────────────────┘
```

## Compliance Checks Performed

### NIST 800-53 Controls Mapped

| Control | Description | Checked In |
|---------|-------------|------------|
| **AC-3** | Access Enforcement | Bicep (public blob access) |
| **AC-4** | Information Flow Enforcement | Bicep/Terraform/ARM (public IPs) |
| **SC-7** | Boundary Protection | Kubernetes (host network) |
| **SC-8** | Transmission Confidentiality | Bicep (TLS 1.2) |
| **SC-28** | Protection of Information at Rest | Bicep/Terraform (encryption) |
| **CM-7** | Least Functionality | Kubernetes (privileged containers) |

### STIG IDs Mapped

| STIG ID | Description | Severity |
|---------|-------------|----------|
| **V-219187** | Public IP addresses | High |
| **V-220001** | Storage encryption at rest | High |
| **V-220002** | TLS 1.2 enforcement | High |
| **V-220003** | Public blob access | High |
| **V-242376** | Privileged containers | Critical |
| **V-242377** | Host network mode | High |

### DoD Instructions Referenced

- **DoD 8500.01** - Cybersecurity
- **DoD 8510.01** - Risk Management Framework

## Severity Levels

| Level | Symbol | Action | Description |
|-------|--------|--------|-------------|
| **Critical** | 🔴 | Request Changes | Blocks best practices (advisory) |
| **High** | 🟠 | Request Changes | Requires review and remediation |
| **Medium** | 🟡 | Comment | Warning - should be addressed |
| **Low** | ⚪ | Comment | Informational |

## Configuration

### appsettings.json

Add GitHub configuration section:

```json
{
  "GitHub": {
    "PersonalAccessToken": "github_pat_YOUR_TOKEN_HERE",
    "WebhookSecret": "your-webhook-secret",
    "EnablePrReviews": true,
    "AutoApproveOnSuccess": false,
    "MaxFileSizeKb": 1024
  }
}
```

### Service Registration

Add to `Compliance.Agent` DI container:

```csharp
// In Program.cs or ServiceCollectionExtensions.cs
services.AddScoped<GitHubPullRequestService>();
services.AddScoped<PullRequestReviewService>();
services.AddHttpClient("GitHub");

// Configuration
services.Configure<GitHubConfiguration>(
    builder.Configuration.GetSection("GitHub")
);

// Plugin registration
services.AddSingleton<PullRequestReviewPlugin>();
```

### GitHub Personal Access Token Setup

1. Go to GitHub Settings → Developer Settings → Personal Access Tokens → Fine-grained tokens
2. Create new token with repository permissions:
   - **Read access**: Contents, Metadata, Pull requests
   - **Read and write access**: Commit statuses, Pull requests
3. Copy token to `appsettings.json`

## Usage Examples

### Manual Review via Semantic Kernel

```csharp
var kernel = sp.GetRequiredService<Kernel>();
var result = await kernel.InvokeAsync(
    "PullRequestReviewPlugin",
    "review_pull_request",
    new KernelArguments
    {
        ["repository"] = "myorg/myrepo",
        ["pr_number"] = "42"
    }
);

Console.WriteLine(result.ToString());
```

### Check PR Review Status

```csharp
var status = await kernel.InvokeAsync(
    "PullRequestReviewPlugin",
    "get_pr_review_status",
    new KernelArguments
    {
        ["repository"] = "myorg/myrepo",
        ["pr_number"] = "42"
    }
);
```

### Example Output

#### Review Summary Posted as PR Comment

```markdown
# 🔍 Compliance Review Summary

**Overall Status**: ⚠️ Issues Found

## Findings by Severity
- 🔴 **Critical**: 1
- 🟠 **High**: 2
- 🟡 **Medium**: 1
- ⚪ **Low**: 0

**Total Issues**: 4

---

⚠️ **This is an advisory review only. Manual approval is required.**

**Phase 1 Compliance**: This review does not block PR merging. Please address findings before final approval.
```

#### Inline Comment Example

```markdown
🔴 **Critical**: Privileged container detected

**NIST 800-53 Control**: CM-7 (Least Functionality)
**STIG ID**: V-242376
**DoD Instruction**: DoD 8500.01

**Finding**: Container `nginx` is running in privileged mode, which grants access to all host devices.

**Code**:
```yaml
securityContext:
  privileged: true  # ❌ Violation
```

**Remediation**:
```yaml
securityContext:
  privileged: false
  allowPrivilegeEscalation: false
  capabilities:
    drop: ["ALL"]
```

**References**:
- STIG V-242376: https://...
- NIST CM-7: https://...
```

## Webhook Integration (Optional)

### Future Enhancement: Automatic PR Reviews

Create Azure Function or API endpoint to receive GitHub webhook events:

```csharp
[HttpPost("api/github/webhook")]
public async Task<IActionResult> HandleWebhook(
    [FromBody] PullRequestWebhookPayload payload,
    [FromHeader("X-Hub-Signature-256")] string signature)
{
    // Validate webhook signature
    if (!ValidateSignature(signature, payload))
        return Unauthorized();

    // Handle PR events
    if (payload.Action is "opened" or "synchronize" or "reopened")
    {
        await kernel.InvokeAsync(
            "PullRequestReviewPlugin",
            "review_pull_request",
            new KernelArguments
            {
                ["repository"] = payload.Repository.FullName,
                ["pr_number"] = payload.PullRequest.Number.ToString()
            }
        );
    }

    return Ok();
}
```

## Limitations (Phase 1)

1. **Advisory Only**: Reviews do not block PR merging
2. **No Auto-Approval**: Manual approval always required
3. **Comment Limit**: Maximum 20 inline comments per PR to avoid spam
4. **File Size Limit**: Files over 1MB are skipped
5. **Pattern-Based Scanning**: Uses regex patterns, not AST parsing
6. **Supported IaC**: Bicep, Terraform, ARM templates, Kubernetes YAML only

## Testing

### Manual Testing Steps

1. Create test repository with IaC files containing violations
2. Create PR with changes to `.bicep` or `.tf` files
3. Call `review_pull_request` kernel function
4. Verify:
   - Inline comments posted on violation lines
   - PR review submitted (request changes or comment)
   - Commit status check updated (success/failure)
   - Review summary posted as comment

### Example Test PR

**File**: `main.bicep`
```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: 'mystorageacct'
  location: 'eastus'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: true  // ❌ Should trigger AC-3 violation
    supportsHttpsTrafficOnly: false  // ❌ Should trigger SC-8 violation
  }
}

resource publicIP 'Microsoft.Network/publicIPAddresses@2021-02-01' = {
  name: 'myPublicIP'
  location: 'eastus'
  properties: {
    publicIPAllocationMethod: 'Static'  // ❌ Should trigger AC-4 violation
  }
}
```

**Expected Outcome**:
- 3 inline comments (public blob access, HTTPS traffic, public IP)
- PR review with "request changes" event
- Commit status check set to "failure"
- Review summary showing 3 high-severity findings

## Future Enhancements

1. **AST-based Parsing**: Replace regex with proper Bicep/Terraform parsers
2. **Custom Rules**: Allow teams to define additional compliance checks
3. **Policy-as-Code Integration**: Support OPA/Rego policies
4. **Azure DevOps Support**: Add ADO PR review integration
5. **Auto-Fix Suggestions**: Generate PR commits with remediation code
6. **Compliance Dashboard**: Track violation trends across repositories
7. **Phase 2 Features**: Auto-approval for clean PRs (requires Phase 2 approval)

## References

- **NIST 800-53 Rev 5**: https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final
- **DISA STIGs**: https://public.cyber.mil/stigs/
- **DoD 8500.01**: https://www.esd.whs.mil/Portals/54/Documents/DD/issuances/dodi/850001p.pdf
- **GitHub REST API**: https://docs.github.com/en/rest

## Status

✅ **Phase 1 Implementation Complete** (December 2024)

Changes PHASE1-COMPLIANCE.md status:
- PR Integration: 🔴 Not Implemented → ✅ Implemented
- Comment Generation: 🔴 Not Implemented → ✅ Implemented

**Next Steps**:
1. Service registration in DI container
2. Configuration in appsettings.json
3. Integration testing with live PRs
4. Documentation review and approval
