# Knowledge Base Integration - Step-by-Step Checklist

## Phase 1: Service Registration (Estimated: 15 minutes)

### Step 1.1: Update DI Container Registration

**File:** `src/Platform.Engineering.Copilot.Compliance.Agent/Extensions/ComplianceAgentCollectionExtensions.cs`

**Action:** Add knowledge base services to DI container

```csharp
public static IServiceCollection AddComplianceAgent(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing code ...
    
    // ✅ ADD: Register knowledge base services (NEW)
    services.AddSingleton<IRmfKnowledgeService, RmfKnowledgeService>();
    services.AddSingleton<IStigKnowledgeService, StigKnowledgeService>();
    services.AddSingleton<IDoDInstructionService, DoDInstructionService>();
    services.AddSingleton<IDoDWorkflowService, DoDWorkflowService>();
    services.AddSingleton<IImpactLevelService, ImpactLevelService>();
    
    // ✅ ADD: Register knowledge base plugin (NEW)
    services.AddSingleton<KnowledgeBasePlugin>();
    
    // ... existing plugin registrations ...
    services.AddSingleton<CompliancePlugin>();
    services.AddSingleton<ISpecializedAgent, ComplianceAgent>();
    
    return services;
}
```

**Validation:**
```bash
# Build project to ensure DI registration compiles
dotnet build src/Platform.Engineering.Copilot.Compliance.Agent/
```

---

## Phase 2: Update AtoComplianceEngine (Estimated: 30 minutes)

### Step 2.1: Update Constructor

**File:** `src/Platform.Engineering.Copilot.Compliance.Agent/Services/Compliance/AtoComplianceEngine.cs`

**Action:** Add knowledge base service dependencies

```csharp
public class AtoComplianceEngine : IAtoComplianceEngine
{
    private readonly ILogger<AtoComplianceEngine> _logger;
    private readonly INistControlsService _nistControlsService;
    private readonly IAzureResourceService _azureResourceService;
    private readonly IMemoryCache _cache;
    private readonly ComplianceMetricsService _metricsService;
    private readonly ComplianceAgentOptions _options;
    
    // ✅ ADD: Knowledge base service fields (NEW)
    private readonly IRmfKnowledgeService _rmfKnowledgeService;
    private readonly IStigKnowledgeService _stigKnowledgeService;
    private readonly IDoDInstructionService _doDInstructionService;
    private readonly IDoDWorkflowService _doDWorkflowService;
    
    public AtoComplianceEngine(
        ILogger<AtoComplianceEngine> logger,
        INistControlsService nistControlsService,
        IAzureResourceService azureResourceService,
        IMemoryCache cache,
        ComplianceMetricsService metricsService,
        IOptions<ComplianceAgentOptions> options,
        // ✅ ADD: Inject knowledge base services (NEW)
        IRmfKnowledgeService rmfKnowledgeService,
        IStigKnowledgeService stigKnowledgeService,
        IDoDInstructionService doDInstructionService,
        IDoDWorkflowService doDWorkflowService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _nistControlsService = nistControlsService ?? throw new ArgumentNullException(nameof(nistControlsService));
        _azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // ✅ ADD: Store knowledge base services (NEW)
        _rmfKnowledgeService = rmfKnowledgeService ?? throw new ArgumentNullException(nameof(rmfKnowledgeService));
        _stigKnowledgeService = stigKnowledgeService ?? throw new ArgumentNullException(nameof(stigKnowledgeService));
        _doDInstructionService = doDInstructionService ?? throw new ArgumentNullException(nameof(doDInstructionService));
        _doDWorkflowService = doDWorkflowService ?? throw new ArgumentNullException(nameof(doDWorkflowService));
        
        _scanners = InitializeScanners();
        _evidenceCollectors = InitializeEvidenceCollectors();
    }
}
```

### Step 2.2: Add STIG Validation Methods

**Action:** Copy methods from `KNOWLEDGE-BASE-CODE-EXAMPLES.md` section 1.1

Add these methods to `AtoComplianceEngine.cs`:
- `ValidateFamilyStigsAsync()` - Validates STIGs for a control family
- `ValidateStigComplianceAsync()` - Validates specific STIG
- `ValidateMfaStigAsync()` - V-219153 validator
- `ValidatePublicIpStigAsync()` - V-219187 validator
- `ValidateStorageEncryptionStigAsync()` - V-219165 validator
- `MapStigSeverityToAtoSeverity()` - Severity mapper
- `StigComplianceResult` class

### Step 2.3: Integrate STIG Validation into Assessment

**Action:** Modify `AssessControlFamilyAsync()` method

**Find this code:**
```csharp
foreach (var scanner in scanners)
{
    var scanResults = await scanner.ScanAsync(resources, cancellationToken);
    familyAssessment.Findings.AddRange(scanResults);
}
```

**Add after it:**
```csharp
// ✅ ADD: STIG validation (NEW)
_logger.LogDebug("Running STIG validation for family {Family}", family);
var stigFindings = await ValidateFamilyStigsAsync(
    family, 
    subscriptionId, 
    cancellationToken);

familyAssessment.Findings.AddRange(stigFindings);

_logger.LogInformation(
    "Family {Family} assessment complete: {NistFindings} NIST findings, {StigFindings} STIG findings",
    family,
    familyAssessment.Findings.Count - stigFindings.Count,
    stigFindings.Count);
```

**Validation:**
```bash
# Build to ensure compilation
dotnet build src/Platform.Engineering.Copilot.Compliance.Agent/
```

---

## Phase 3: Update NistControlsService (Estimated: 30 minutes)

### Step 3.1: Update Constructor

**File:** `src/Platform.Engineering.Copilot.Compliance.Agent/Services/Compliance/NistControlsService.cs`

**Action:** Add STIG and DoD service dependencies

```csharp
public class NistControlsService : INistControlsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NistControlsService> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly NistControlsOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly ComplianceMetricsService _metricsService;
    
    // ✅ ADD: Knowledge base service fields (NEW)
    private readonly IStigKnowledgeService _stigKnowledgeService;
    private readonly IDoDInstructionService _doDInstructionService;
    
    public NistControlsService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<NistControlsService> logger,
        IHostEnvironment hostEnvironment,
        IOptions<NistControlsOptions> options,
        ComplianceMetricsService metricsService,
        // ✅ ADD: Inject knowledge base services (NEW)
        IStigKnowledgeService stigKnowledgeService,
        IDoDInstructionService doDInstructionService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        
        // ✅ ADD: Store knowledge base services (NEW)
        _stigKnowledgeService = stigKnowledgeService ?? throw new ArgumentNullException(nameof(stigKnowledgeService));
        _doDInstructionService = doDInstructionService ?? throw new ArgumentNullException(nameof(doDInstructionService));
        
        _retryPolicy = CreateRetryPolicy();
    }
}
```

### Step 3.2: Add Enhanced Methods

**Action:** Copy methods from `KNOWLEDGE-BASE-CODE-EXAMPLES.md` section 2.1

Add these methods to `NistControlsService.cs`:
- `GetControlWithStigMappingAsync()` - Returns NIST + STIG + DoD
- `GetStigsForNistControlAsync()` - Returns STIGs for NIST control
- `GetCompleteControlMappingAsync()` - Returns NIST ↔ STIG ↔ CCI mapping
- `GetDoDInstructionsForControlAsync()` - Returns DoD instructions
- `GetAzureImplementationAsync()` - Returns Azure implementation guidance

### Step 3.3: Update Interface

**File:** `src/Platform.Engineering.Copilot.Core/Interfaces/Compliance/INistControlsService.cs`

**Action:** Add new method signatures

```csharp
public interface INistControlsService
{
    // Existing methods...
    Task<NistCatalog?> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<NistControl?> GetControlAsync(string controlId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NistControl>> GetControlsByFamilyAsync(string family, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NistControl>> SearchControlsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
    Task<ControlEnhancement?> GetControlEnhancementAsync(string controlId, CancellationToken cancellationToken = default);
    Task<bool> ValidateControlIdAsync(string controlId, CancellationToken cancellationToken = default);
    
    // ✅ ADD: Knowledge base integration methods (NEW)
    Task<NistControlWithStigMapping?> GetControlWithStigMappingAsync(string controlId, CancellationToken cancellationToken = default);
    Task<List<StigControl>> GetStigsForNistControlAsync(string controlId, CancellationToken cancellationToken = default);
    Task<ControlMapping?> GetCompleteControlMappingAsync(string controlId, CancellationToken cancellationToken = default);
    Task<List<DoDInstruction>> GetDoDInstructionsForControlAsync(string controlId, CancellationToken cancellationToken = default);
    Task<List<AzureStigImplementation>> GetAzureImplementationAsync(string controlId, CancellationToken cancellationToken = default);
}
```

### Step 3.4: Add Supporting Model

**File:** `src/Platform.Engineering.Copilot.Core/Models/Compliance/NistControlWithStigMapping.cs` (NEW)

**Action:** Create new file with model from `KNOWLEDGE-BASE-CODE-EXAMPLES.md` section 2.2

**Validation:**
```bash
# Build to ensure compilation
dotnet build src/Platform.Engineering.Copilot.Compliance.Agent/
dotnet build src/Platform.Engineering.Copilot.Core/
```

---

## Phase 4: Update ComplianceAgent (Estimated: 15 minutes)

### Step 4.1: Update Constructor

**File:** `src/Platform.Engineering.Copilot.Compliance.Agent/Services/Agents/ComplianceAgent.cs`

**Action:** Add KnowledgeBasePlugin to constructor

**Find this code:**
```csharp
public ComplianceAgent(
    ISemanticKernelService semanticKernelService,
    ILogger<ComplianceAgent> logger,
    CompliancePlugin compliancePlugin)
```

**Replace with:**
```csharp
public ComplianceAgent(
    ISemanticKernelService semanticKernelService,
    ILogger<ComplianceAgent> logger,
    CompliancePlugin compliancePlugin,
    KnowledgeBasePlugin knowledgeBasePlugin)  // ✅ ADD: NEW parameter
```

### Step 4.2: Register Plugin

**Find this code:**
```csharp
// Register compliance plugin
_kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(compliancePlugin, "CompliancePlugin"));

_logger.LogInformation("✅ Compliance Agent initialized with specialized kernel");
```

**Replace with:**
```csharp
// Register plugins
_kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(compliancePlugin, "CompliancePlugin"));
_kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(knowledgeBasePlugin, "KnowledgeBasePlugin")); // ✅ ADD: NEW

_logger.LogInformation("✅ Compliance Agent initialized with CompliancePlugin and KnowledgeBasePlugin");
```

### Step 4.3: Update System Prompt (Optional but Recommended)

**Find the `BuildSystemPrompt()` method**

**Add this section to the prompt:**
```csharp
private string BuildSystemPrompt()
{
    return @"You are a specialized DoD/Navy compliance expert with comprehensive knowledge...

**Available Tools:**

**Compliance Scanning (CompliancePlugin):**
- run_compliance_scan: Run Azure resource compliance assessment
- get_nist_control: Get NIST 800-53 control details
- validate_control: Validate control implementation
...

**✅ NEW: Knowledge Base (KnowledgeBasePlugin):**
- RMF Process: explain_rmf_process, get_rmf_deliverables
- STIG Controls: explain_stig, search_stigs, get_stigs_for_nist_control, get_control_mapping
- DoD Guidance: explain_dod_instruction, search_dod_instructions
- Navy Workflows: explain_navy_workflow, get_navy_ato_process, get_pmw_deployment_process, get_emass_registration_process
- Impact Levels: explain_impact_level

**When to Use Knowledge Base Functions:**
- User asks about RMF steps or deliverables → use explain_rmf_process or get_rmf_deliverables
- User asks about STIGs → use explain_stig or search_stigs
- User asks 'what STIGs implement [NIST control]?' → use get_stigs_for_nist_control
- User asks about Navy ATO process → use get_navy_ato_process
- User asks about Impact Levels → use explain_impact_level
- User asks about DoD instructions/policy → use explain_dod_instruction or search_dod_instructions

...";
}
```

**Validation:**
```bash
# Build to ensure compilation
dotnet build src/Platform.Engineering.Copilot.Compliance.Agent/
```

---

## Phase 5: Configure JSON File Deployment (Estimated: 5 minutes)

### Step 5.1: Update .csproj File

**File:** `src/Platform.Engineering.Copilot.Core/Platform.Engineering.Copilot.Core.csproj`

**Action:** Add ItemGroup to copy JSON files to output directory

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <!-- ... existing properties ... -->
  </PropertyGroup>

  <!-- ✅ ADD: Copy knowledge base JSON files to output (NEW) -->
  <ItemGroup>
    <None Update="KnowledgeBase\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <ItemGroup>
    <!-- ... existing package references ... -->
  </ItemGroup>

</Project>
```

**Validation:**
```bash
# Build and verify JSON files are copied
dotnet build src/Platform.Engineering.Copilot.Core/

# Check output directory
ls -la src/Platform.Engineering.Copilot.Core/bin/Debug/net8.0/KnowledgeBase/
# Expected output:
# rmf-process.json
# stig-controls.json
# dod-instructions.json
# navy-workflows.json
```

---

## Phase 6: Testing (Estimated: 30 minutes)

### Step 6.1: Build All Projects

```bash
# Build entire solution
dotnet build Platform.Engineering.Copilot.sln

# Expected: No compilation errors
```

### Step 6.2: Run Unit Tests

```bash
# Run existing tests to ensure no regressions
dotnet test tests/Platform.Engineering.Copilot.Tests.Unit/

# Expected: All tests pass
```

### Step 6.3: Manual Integration Test

**Create test script:** `test-knowledge-base-integration.sh`

```bash
#!/bin/bash

echo "Testing Knowledge Base Integration..."

# Test 1: RMF query
echo -e "\n🔍 Test 1: RMF Process Query"
curl -X POST http://localhost:5000/api/compliance/query \
  -H "Content-Type: application/json" \
  -d '{"message": "What is RMF Step 4?"}' | jq .

# Test 2: STIG query
echo -e "\n🔍 Test 2: STIG Explanation Query"
curl -X POST http://localhost:5000/api/compliance/query \
  -H "Content-Type: application/json" \
  -d '{"message": "Explain STIG V-219153"}' | jq .

# Test 3: NIST ↔ STIG mapping
echo -e "\n🔍 Test 3: NIST to STIG Mapping Query"
curl -X POST http://localhost:5000/api/compliance/query \
  -H "Content-Type: application/json" \
  -d '{"message": "What STIGs implement IA-2(1)?"}' | jq .

# Test 4: Navy ATO workflow
echo -e "\n🔍 Test 4: Navy ATO Workflow Query"
curl -X POST http://localhost:5000/api/compliance/query \
  -H "Content-Type: application/json" \
  -d '{"message": "How do I get an ATO in the Navy?"}' | jq .

# Test 5: Impact Level query
echo -e "\n🔍 Test 5: Impact Level Query"
curl -X POST http://localhost:5000/api/compliance/query \
  -H "Content-Type: application/json" \
  -d '{"message": "What are IL5 requirements?"}' | jq .

echo -e "\n✅ Integration tests complete!"
```

**Run tests:**
```bash
chmod +x test-knowledge-base-integration.sh
./test-knowledge-base-integration.sh
```

### Step 6.4: Verify Knowledge Base Data Loading

**Create verification script:** `verify-knowledge-base.sh`

```bash
#!/bin/bash

echo "Verifying Knowledge Base Data Loading..."

# Check if JSON files exist
echo -e "\n📄 Checking JSON files..."
FILES=(
  "src/Platform.Engineering.Copilot.Core/KnowledgeBase/rmf-process.json"
  "src/Platform.Engineering.Copilot.Core/KnowledgeBase/stig-controls.json"
  "src/Platform.Engineering.Copilot.Core/KnowledgeBase/dod-instructions.json"
  "src/Platform.Engineering.Copilot.Core/KnowledgeBase/navy-workflows.json"
)

for file in "${FILES[@]}"; do
  if [ -f "$file" ]; then
    SIZE=$(wc -l < "$file")
    echo "  ✅ $file ($SIZE lines)"
  else
    echo "  ❌ MISSING: $file"
  fi
done

# Validate JSON syntax
echo -e "\n🔍 Validating JSON syntax..."
for file in "${FILES[@]}"; do
  if jq empty "$file" 2>/dev/null; then
    echo "  ✅ Valid JSON: $file"
  else
    echo "  ❌ INVALID JSON: $file"
  fi
done

echo -e "\n✅ Knowledge base verification complete!"
```

**Run verification:**
```bash
chmod +x verify-knowledge-base.sh
./verify-knowledge-base.sh
```

---

## Phase 7: Validation Checklist

### ✅ Pre-Deployment Validation

- [ ] **DI Registration**
  - [ ] All 5 knowledge base services registered
  - [ ] KnowledgeBasePlugin registered
  - [ ] No DI resolution errors on startup

- [ ] **AtoComplianceEngine**
  - [ ] Constructor accepts 4 new knowledge base service parameters
  - [ ] STIG validation methods added
  - [ ] `AssessControlFamilyAsync()` calls `ValidateFamilyStigsAsync()`
  - [ ] Project compiles without errors

- [ ] **NistControlsService**
  - [ ] Constructor accepts 2 new knowledge base service parameters
  - [ ] 5 new methods added (GetControlWithStigMapping, etc.)
  - [ ] Interface updated with new method signatures
  - [ ] `NistControlWithStigMapping` model created
  - [ ] Project compiles without errors

- [ ] **ComplianceAgent**
  - [ ] Constructor accepts `KnowledgeBasePlugin` parameter
  - [ ] Plugin registered with kernel
  - [ ] System prompt updated (optional)
  - [ ] Project compiles without errors

- [ ] **JSON Files**
  - [ ] All 4 JSON files exist in `Core/KnowledgeBase/`
  - [ ] All JSON files have valid syntax
  - [ ] .csproj configured to copy files to output
  - [ ] Files present in bin/Debug/net8.0/KnowledgeBase/

- [ ] **Build & Tests**
  - [ ] Solution builds without errors: `dotnet build`
  - [ ] No compilation warnings
  - [ ] Existing unit tests pass: `dotnet test`
  - [ ] No test regressions

### ✅ Post-Deployment Validation

- [ ] **Service Startup**
  - [ ] Application starts without DI errors
  - [ ] Knowledge base services instantiated
  - [ ] JSON files loaded successfully (check logs)
  - [ ] Cache populated (check memory usage)

- [ ] **Knowledge Base Queries**
  - [ ] RMF queries return data: "What is RMF Step 4?"
  - [ ] STIG queries return data: "Explain STIG V-219153"
  - [ ] NIST ↔ STIG mapping works: "What STIGs implement IA-2(1)?"
  - [ ] Navy workflow queries work: "How do I get an ATO?"
  - [ ] Impact Level queries work: "What are IL5 requirements?"

- [ ] **Compliance Assessment**
  - [ ] Assessment runs without errors
  - [ ] STIG findings appear in results
  - [ ] STIG metadata populated (AzureService, AutomationCommand, etc.)
  - [ ] Assessment includes RMF status (if implemented)

- [ ] **Enhanced NIST Queries**
  - [ ] `GetControlWithStigMappingAsync()` returns enriched data
  - [ ] STIG controls included in response
  - [ ] DoD instructions included in response
  - [ ] Azure implementation guidance present

- [ ] **Performance**
  - [ ] Cache hit rate >95% (check logs)
  - [ ] Query response time <200ms (cached)
  - [ ] Assessment completion time <15 seconds
  - [ ] Memory usage <2.1MB (no leaks)

- [ ] **Logging**
  - [ ] Knowledge base services log cache hits/misses
  - [ ] STIG validation logged during assessments
  - [ ] No ERROR level logs during normal operations
  - [ ] WARN logs only for expected scenarios (cache miss on first load)

---

## Phase 8: Documentation Updates

### Step 8.1: Update README

**File:** `README.md`

**Action:** Add knowledge base features to feature list

```markdown
## Features

### Compliance & Governance
- ✅ NIST 800-53 Control Catalog with caching
- ✅ **NEW: RMF Process Guidance (6 steps with deliverables)**
- ✅ **NEW: STIG Control Validation (5 Azure STIGs)**
- ✅ **NEW: DoD Instruction Reference (5 key policies)**
- ✅ **NEW: Navy ATO Workflow (8-step process, 20-60 weeks)**
- ✅ **NEW: Impact Level Requirements (IL2, IL4, IL5, IL6)**
- ✅ Azure Resource Compliance Scanning
- ✅ Automated Evidence Collection
- ✅ **NEW: NIST ↔ STIG ↔ CCI ↔ DoD Control Mappings**
```

### Step 8.2: Update CHANGELOG

**File:** `CHANGELOG.md`

**Action:** Add entry for knowledge base integration

```markdown
## [Unreleased]

### Added
- **Knowledge Base Integration** - Comprehensive DoD/Navy compliance knowledge base
  - RMF Process Service with 6-step guidance and deliverables
  - STIG Knowledge Service with 5 Azure STIGs and Azure implementation guidance
  - DoD Instruction Service with 5 key DoD policies
  - Navy Workflow Service with ATO, PMW deployment, and eMASS workflows
  - Impact Level Service with IL2, IL4, IL5, IL6 requirements
  - 15 new AI-callable functions via KnowledgeBasePlugin
  - NIST ↔ STIG ↔ CCI ↔ DoD control mappings
  - Azure-specific implementation guidance for STIG controls
  
### Enhanced
- **AtoComplianceEngine** - Now validates STIG compliance during assessments
- **NistControlsService** - Enriched with STIG mappings and DoD instruction references
- **ComplianceAgent** - Can answer RMF, STIG, and Navy workflow questions

### Documentation
- Added `KNOWLEDGE-BASE-IMPLEMENTATION.md` - Technical implementation guide
- Added `KNOWLEDGE-BASE-INTEGRATION-GUIDE.md` - Integration architecture and patterns
- Added `KNOWLEDGE-BASE-CODE-EXAMPLES.md` - Concrete code examples
- Added `KNOWLEDGE-BASE-ARCHITECTURE-DIAGRAM.md` - Visual architecture and data flows
- Updated `PHASE1-COMPLIANCE.md` - Phase 1 compliance now 92% (up from 58%)
```

---

## Phase 9: Rollback Plan (If Needed)

### Rollback Procedure

If integration causes issues, follow these steps to rollback:

```bash
# 1. Revert DI registration
git checkout HEAD -- src/Platform.Engineering.Copilot.Compliance.Agent/Extensions/ComplianceAgentCollectionExtensions.cs

# 2. Revert AtoComplianceEngine
git checkout HEAD -- src/Platform.Engineering.Copilot.Compliance.Agent/Services/Compliance/AtoComplianceEngine.cs

# 3. Revert NistControlsService
git checkout HEAD -- src/Platform.Engineering.Copilot.Compliance.Agent/Services/Compliance/NistControlsService.cs
git checkout HEAD -- src/Platform.Engineering.Copilot.Core/Interfaces/Compliance/INistControlsService.cs

# 4. Revert ComplianceAgent
git checkout HEAD -- src/Platform.Engineering.Copilot.Compliance.Agent/Services/Agents/ComplianceAgent.cs

# 5. Remove new model file
rm src/Platform.Engineering.Copilot.Core/Models/Compliance/NistControlWithStigMapping.cs

# 6. Rebuild
dotnet build Platform.Engineering.Copilot.sln

# 7. Restart services
docker-compose restart
```

---

## Integration Timeline

| Phase | Task | Estimated Time | Complexity |
|-------|------|----------------|------------|
| 1 | Service Registration | 15 min | Low |
| 2 | Update AtoComplianceEngine | 30 min | Medium |
| 3 | Update NistControlsService | 30 min | Medium |
| 4 | Update ComplianceAgent | 15 min | Low |
| 5 | Configure JSON Deployment | 5 min | Low |
| 6 | Testing | 30 min | Medium |
| 7 | Validation | 15 min | Low |
| 8 | Documentation | 10 min | Low |
| **Total** | | **2.5 hours** | |

## Success Criteria

✅ **Must Have (Phase 1 Complete):**
- [ ] All knowledge base services registered and functional
- [ ] ComplianceAgent answers RMF, STIG, and Navy workflow questions
- [ ] AtoComplianceEngine includes STIG findings in assessments
- [ ] NistControlsService returns STIG mappings for NIST controls
- [ ] All JSON files loaded successfully
- [ ] No errors during build, startup, or runtime
- [ ] Phase 1 compliance score: 92%

✅ **Nice to Have (Future Enhancements):**
- [ ] Automated RMF step validation logic
- [ ] Real-time STIG validation (not just canned examples)
- [ ] Integration with STIG Viewer for latest DISA STIGs
- [ ] Integration with eMASS API for real-time status
- [ ] NIST control enhancement mappings (AC-2(1), etc.)
- [ ] IL-specific template generation

## Support & Troubleshooting

### Common Issues

**Issue:** DI resolution error for knowledge base services  
**Solution:** Ensure all services registered in `ComplianceAgentCollectionExtensions.cs`

**Issue:** JSON files not found at runtime  
**Solution:** Verify .csproj has `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`

**Issue:** STIG findings not appearing in assessments  
**Solution:** Check that `ValidateFamilyStigsAsync()` is called in `AssessControlFamilyAsync()`

**Issue:** ComplianceAgent not using knowledge base functions  
**Solution:** Update system prompt to explicitly mention knowledge base functions

**Issue:** Cache not working  
**Solution:** Ensure `IMemoryCache` is registered in DI container

### Getting Help

- **Documentation:** See `docs/KNOWLEDGE-BASE-*.md` files
- **Code Examples:** See `docs/KNOWLEDGE-BASE-CODE-EXAMPLES.md`
- **Architecture:** See `docs/KNOWLEDGE-BASE-ARCHITECTURE-DIAGRAM.md`
- **Phase 1 Status:** See `docs/PHASE1-COMPLIANCE.md`

---

## Completion Checklist

Once you've completed all phases, verify:

- [ ] ✅ All services registered in DI
- [ ] ✅ AtoComplianceEngine enhanced with STIG validation
- [ ] ✅ NistControlsService enhanced with STIG mappings
- [ ] ✅ ComplianceAgent registered with KnowledgeBasePlugin
- [ ] ✅ JSON files deployed to output directory
- [ ] ✅ All tests passing
- [ ] ✅ Manual testing completed successfully
- [ ] ✅ Documentation updated
- [ ] ✅ No errors in logs
- [ ] ✅ Performance acceptable (<15s for assessment)
- [ ] ✅ Phase 1 compliance: 92%

**🎉 Knowledge Base Integration Complete! 🎉**
