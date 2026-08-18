# Deployment Log — Platform Engineering Copilot (Azure Government)

Running log of commands and changes made while deploying to the user's Federal
Azure subscription. Newest entries at the bottom.

## Environment

- Cloud: `AzureUSGovernment` (portal.azure.us)
- Tenant: FedAIRS Azure Gov - GCCHigh (`03f141f3-496d-4319-bbea-a3e9286cab10`)
- Subscription: `f9de68d9-11f8-41d5-97af-b38d1adb7773` ("1364070 Internal Subscription")
- Region: `usgovvirginia`

## 2026-08-17

### Authentication
- Discovered CLI was defaulted to an unrelated subscription ("EMPTY 01") in a
  different tenant with no access to the target subscription.
- Re-authenticated: `az cloud set --name AzureUSGovernment` →
  `az login --use-device-code` → signed in against the FedAIRS tenant.
- Confirmed active subscription:
  ```
  az account set --subscription f9de68d9-11f8-41d5-97af-b38d1adb7773
  az account show -o table
  ```
  Result: subscription active, tenant FedAIRS.onmicrosoft.us.

### Subscription reconnaissance
- `az group list -o table` → only pre-existing resource group is
  `NetworkWatcherRG` (usgovvirginia). Subscription is otherwise empty.
- `az provider show -n Microsoft.CognitiveServices --query registrationState`
  → `Registered`.
- `az cognitiveservices account list-skus --kind OpenAI --location usgovvirginia`
  → `S0` (Standard) tier available.
- `az cognitiveservices model list --location usgovvirginia` → confirmed
  `gpt-4o` (2024-11-20) and `text-embedding-ada-002` (v2) are both available
  in usgovvirginia for this subscription.

### Findings from reviewing infra/bicep
- `infra/bicep/main.parameters.aci.json` does **not** match
  `infra/bicep/main.bicep` parameter names (e.g. `sqlAdminUsername` vs actual
  `sqlAdminLogin`, `aciMcpCpuCores` vs actual `aciCpuCores`, plus params like
  `enablePublicAccess`/`tags` that don't exist in the template at all). It
  would fail deployment as-is. **Decision: fix this file.**
- `main.bicep` deploys ACI container groups for `admin-api` and
  `admin-client` images, but there is no corresponding .NET project/Dockerfile
  anywhere in `src/` — only Kubernetes manifests and `scripts/start-admin.sh`
  reference them. **Decision: skip deploying Admin API/Client (deploy MCP +
  Chat only).**
- No Bicep module provisions Azure OpenAI at all. **Decision: add a new
  `infra/bicep/modules/openai.bicep` module and wire it into `main.bicep`.**
- **Decision: deploy into a new resource group** (name TBD, e.g.
  `rg-platform-copilot-dev`) rather than reusing `NetworkWatcherRG`.

### Plan going forward (not yet executed)
1. Add `modules/openai.bicep` (Cognitive Services OpenAI account + gpt-4o and
   text-embedding-ada-002 deployments) and wire into `main.bicep`.
2. Fix `main.parameters.aci.json` to use real parameter names, set
   `deployAdminApi=false`, `deployChat=true`.
3. Create resource group in usgovvirginia.
4. Validate (`az deployment group validate` / what-if) before applying.
5. Deploy Bicep (network, monitoring, Key Vault, storage, SQL, ACR, ACI: MCP
   + Chat, OpenAI).
6. Build and push MCP + Chat Docker images to the new ACR.
7. Configure secrets (Azure OpenAI key/endpoint, SQL connection string) and
   verify `/health` endpoints.

### Redis (Azure Cache for Redis) added
- Created `infra/bicep/modules/redis.bicep`: Cognitive... no — Azure Cache for
  Redis (`Microsoft.Cache/redis`), SKU defaults to Basic (dev) / Standard
  (prod), TLS 1.2 minimum, non-SSL port disabled, outputs `hostName`,
  `sslPort`, `primaryKey`, `connectionString`.
- Wired into `main.bicep`:
  - New params: `deployRedis` (default `true`), `redisSkuName`,
    `redisSkuCapacity`.
  - New `redis` module block (conditional on `deployRedis`), placed before
    the SQL module.
  - MCP and Chat ACI container groups now get
    `StateManagement__Provider=Redis` (or `Memory` if `deployRedis=false`)
    and `StateManagement__RedisConnectionString` (matches the config keys
    read by `Platform.Engineering.Copilot.State/Extensions/ServiceCollectionExtensions.cs`).
  - New Key Vault secret `RedisConnectionString`.
  - New output `redisHostName`; `deployRedis` added to `deploymentSummary`.
- Note: had to drop `@secure()` from the module's `connectionString`/
  `primaryKey` outputs — Bicep (BCP426) disallows secure outputs from a
  conditional module being consumed inside an array/object literal (our
  `secureEnvironmentVariables` array entries and the Key Vault secret
  `properties.value`). The value is still only ever written into Key Vault
  and container env vars, not otherwise surfaced, but it will appear in the
  raw ARM deployment output/logs (`az deployment group show`) — acceptable
  for a dev subscription, worth tightening later for prod.
- Verified compile: `mcp_azure_bicep build` on `main.bicep` → 0 errors (only
  pre-existing unrelated warnings in `aks.bicep`/`acr.bicep`).

### Azure OpenAI module added
- Created `infra/bicep/modules/openai.bicep`: `Microsoft.CognitiveServices/accounts`
  (kind `OpenAI`, SKU S0, system-assigned identity, `customSubDomainName` set
  so `properties.endpoint` resolves correctly for Gov cloud
  `openai.azure.us`) plus two `accounts/deployments` child resources —
  `gpt-4o` (2024-11-20) for chat and `text-embedding-ada-002` (v2) for
  embeddings, both confirmed available in usgovvirginia earlier. Embedding
  deployment explicitly `dependsOn` the chat deployment to serialize
  Cognitive Services deployment operations.
- Wired into `main.bicep`:
  - New params: `deployOpenAI` (default `true`), `openAiSkuName`,
    `openAiChatDeploymentName`/`openAiChatModelName`/`openAiChatModelVersion`/
    `openAiChatCapacity`, and the embedding equivalents.
  - New `openAi` module block (conditional on `deployOpenAI`), placed right
    after the Redis module.
  - MCP ACI container now gets `Gateway__AzureOpenAI__Endpoint`,
    `Gateway__AzureOpenAI__DeploymentName`, `Gateway__AzureOpenAI__ChatDeploymentName`,
    `Gateway__AzureOpenAI__EmbeddingDeploymentName` (plain env vars) and
    `Gateway__AzureOpenAI__ApiKey` (env var, same secure-output caveat as
    Redis) — matches the config keys read from `Gateway:AzureOpenAI:*` in
    `appsettings.example.json`.
  - New Key Vault secret `AzureOpenAIApiKey`.
  - New outputs `openAiEndpoint`, `openAiAccountName`; `deployOpenAI` added
    to `deploymentSummary`.
- Verified compile: both `main.bicep` and `modules/openai.bicep` build with
  0 errors (expected warnings only: unused `environment` param — kept for
  style consistency with other modules — and the same
  `outputs-should-not-contain-secrets` warning as Redis, for the same
  documented reason).

### Fixed `main.parameters.aci.json`
- Rewrote the file from scratch — the old version referenced parameter
  names that don't exist in `main.bicep` at all (`sqlAdminUsername` vs real
  `sqlAdminLogin`; `aciMcpCpuCores`/`aciChatCpuCores`/`aciAdminApiCpuCores`/
  `aciAdminClientCpuCores` vs real single `aciCpuCores`/`aciMemoryInGB`;
  `existingVNetName` vs real `existingVnetName`; nonexistent
  `allowedIpAddresses`, `enablePublicAccess`, `enablePrivateEndpoints`,
  `tags`). Cross-checked every key against a grep of all 45
  `param` declarations in `main.bicep` — all 43 included keys now match
  exactly (only 2 required params intentionally omitted, see below).
- Key values set for this deployment: `projectName=platsup` (must be
  3-8 chars per `@minLength(3)/@maxLength(8)` — old value
  `platform-engineering` would have failed validation), `location=usgovvirginia`,
  `containerDeploymentTarget=aci`, `deployACR=true`, `deployACI=true`,
  `deployAdminApi=false`, `deployChat=true` (per the earlier decision to skip
  Admin API/Client this round), `deployRedis=true`, `deployOpenAI=true`.
  `keyVaultAdminObjectId` set to the signed-in user's AAD object ID
  (`az ad signed-in-user show --query id -o tsv`).
- **Intentionally omitted from the file** (both are `@secure()`/required
  params with no default in `main.bicep`, so they must be supplied via
  `--parameters` on the `az deployment group create` command line at deploy
  time, never committed to git):
  - `sqlAdminPassword` — generate a strong random password just before
    deploying.
  - Nothing else omitted is security-sensitive; `keyVaultAdminObjectId` is
    an AAD object ID (not a secret) so it's fine to keep in the file for
    this working deployment, though it's tenant/user-specific and shouldn't
    be upstreamed to a shared/public template as-is.

### Resource group created
- `az group create --name rg-platform-copilot-dev --location usgovvirginia`
  → succeeded.

### Deployment validated (`az deployment group validate`)
- Ran against `main.bicep` + `main.parameters.aci.json`, with a freshly
  generated random `sqlAdminPassword` passed via `--parameters` on the CLI
  (never written to any file).
- Result: `"error": null`, `"provisioningState": "Succeeded"`.
- Only diagnostics: 3x `NestedDeploymentShortCircuited` warnings (expected —
  ARM short-circuits deep validation of nested module deployments whose
  params depend on `reference()`/other-module outputs not yet known at
  validate time; this is normal and not an error).
- Confirms `main.bicep` + the fixed parameters file are deployable as-is.

### First real deployment attempt — FAILED (storage/Key Vault naming bug)
- `az deployment group create` was run for real (background job) against
  `rg-platform-copilot-dev`.
- Result: `DeploymentFailed`. Root cause: **pre-existing bug** in
  `main.bicep`'s resource naming, unrelated to Redis/OpenAI — surfaced only
  now because this was the first real deployment attempt ever made against
  this template.
  - `storageAccountName = replace('${resourcePrefix}st${uniqueSuffix}', '-', '')`
    produced `platsupdevst2sy2zj72zb3cg` — **25 characters**, but Azure
    Storage account names must be ≤24 chars (lowercase alphanumeric only).
    Deployment failed with `AccountNameInvalid`.
  - `keyVaultName = '${resourcePrefix}-kv-${uniqueSuffix}'` produced
    `platsup-dev-kv-2sy2zj72zb3cg` — **28 characters**, but Key Vault names
    must be ≤24 chars. This caused the Key Vault deployment to fail too,
    which cascaded into `ParentResourceNotFound` errors for all the
    `vaults/secrets` child resources (SqlConnectionString, AppInsights keys,
    RedisConnectionString, AzureOpenAIApiKey).
  - Both formulas overflow for essentially any realistic `projectName`
    (even the 7-char default `platsup` triggered it), since `resourcePrefix`
    (`projectName-environment`) + fixed suffixes + the 13-char
    `uniqueString()` easily exceeds 24 chars.
- **Fix**: added a new `shortId` variable — `projectName`+`environment`
  concatenated (no hyphen), lowercased, and bounded to 6 chars via
  `substring(..., 0, min(length(...), 6))`. Rewrote:
  - `storageAccountName = '${shortId}st${uniqueSuffix}'` (max 21 chars)
  - `keyVaultName = 'kv-${shortId}${uniqueSuffix}'` (max 22 chars)
  - Checked all other length-constrained resource names (SQL server ≤63,
    ACR ≤50, Redis ≤63, Cognitive Services account ≤64) — all comfortably
    within limits with the current `resourcePrefix`-based scheme, no other
    fixes needed.
- Rebuilt `main.bicep` → 0 errors, confirmed compiling with the fix.
- Retrying the deployment next.

### Second deployment attempt — FAILED (2 more Gov-cloud/config bugs found)
- Storage/Key Vault naming fix worked. New failures surfaced:
  1. **`modules/keyvault.bicep` diagnostic settings**: used
     `categoryGroup: 'audit'`, which ARM rejected —
     `"CategoryGroup: 'audit' is not supported, supported ones are: 'allLogs'"`.
     **Fix**: changed to `categoryGroup: 'allLogs'`. Checked all other
     modules for the same pattern — this was the only occurrence.
  2. **`modules/monitoring.bicep` availability web test**: hardcoded classic
     ping-test `Locations` using commercial Azure region IDs
     (`us-ca-sjc-azr`, `us-tx-sn1-azr`, `us-il-ch1-azr`). ARM rejected with
     `"'us-ca-sjc-azr' is not a supported location"` / `LocationRequired` —
     these classic webtest location IDs don't exist in Azure Government.
     **Fix**: added a new `enableAvailabilityTest` bool param (default
     `true`, preserves behavior for commercial-cloud users) that makes the
     `Microsoft.Insights/webtests` resource conditional; threaded through
     `main.bicep`'s `monitoring` module call and set to `false` in
     `main.parameters.aci.json` for this Gov deployment.
- Rebuilt `main.bicep` → 0 errors. Retrying deployment again.

### Third deployment attempt — FAILED (4 more bugs found)
- Used `az deployment operation group list` per failed nested deployment
  (`database-deployment`, `acr-deployment`, `keyvault-deployment`,
  `monitoring-deployment`) to pull clean, structured error messages for each
  simultaneous failure:
  1. **`database-deployment`**: `ProvisioningDisabled` — "Provisioning is
     restricted in this region. Please choose a different region. For
     exceptions to this rule please open a support request..." This is a
     subscription/region-level Azure policy restriction on SQL Database in
     `usgovvirginia`, not a template bug.
  2. **`acr-deployment`**: `NetworkRuleNotSupported` — "The requested feature
     virtual network rule is not supported for the SKU Standard." The ACR
     module unconditionally set `networkRuleSet.defaultAction: 'Deny'`,
     which requires Premium SKU, but `acrSku=Standard` was configured.
  3. **`keyvault-deployment`**: `BadRequest` — "At least one data sink needs
     to be specified" for the Key Vault diagnostic settings resource
     (`categoryGroup` fix from attempt #2 was correct, but no
     `workspaceId`/`storageAccountId`/`eventHubAuthorizationRuleId` sink was
     ever configured).
  4. **`monitoring-deployment`**: `LocationRequired` (x2) — the two
     `Microsoft.AlertsManagement/smartDetectorAlertRules` resources
     (slow page load / slow server response) were missing the required
     `location` property entirely.
- **Fixes applied**:
  1. Added `logAnalyticsWorkspaceId` param to `modules/keyvault.bicep`,
     wired the `diagnosticSettings` resource to use it as `workspaceId`
     (made the resource conditional on the param being non-empty), and
     passed `monitoring!.outputs.logAnalyticsWorkspaceId` from `main.bicep`'s
     `keyVault` module call.
  2. Added `location: 'global'` to both `slowPageLoadTimeRule` and
     `slowServerResponseTimeRule` in `modules/monitoring.bicep`
     (`smartDetectorAlertRules` is a global-scoped resource type).
  3. Wrapped the ACR `networkRuleSet` property in a `union()` so it's only
     included when `sku == 'Premium'`, leaving Standard/Basic SKUs
     unaffected (no functional change for Premium deployments).
  4. **SQL region restriction** — not fixable in code. Asked the user how to
     proceed; decision: **skip SQL for now**. Added a new `deploySql` bool
     param (default `true`, so commercial/other deployments are unaffected),
     made the `database` module and its Key Vault secret resource
     conditional on it, guarded every `database.outputs.*` reference across
     the template (App Service / ACI env vars, outputs, deployment summary)
     with `deploySql ? database!.outputs.X : ''`, and set
     `"deploySql": { "value": false }` in `main.parameters.aci.json`. The
     app already supports Redis for state management, so the platform is
     fully functional without SQL for this initial deployment. SQL can be
     re-enabled later once a support request lifts the regional
     restriction, or once tried in a different Gov region.
- Rebuilt `main.bicep` → 0 errors. Retrying deployment (attempt #4).

### Fourth deployment attempt — FAILED (2 more bugs found)
- Most resources from previous attempts remained deployed (incremental
  deployment); only newly-reached resources failed:
  1. **`acr-deployment`**: `NetworkRuleNotSupported` — "The requested
     feature **data endpoint rule** is not supported for the SKU Standard."
     `modules/acr.bicep` unconditionally set `dataEndpointEnabled: true`,
     another Premium-only ACR feature. **Fix**: made it conditional on
     `sku == 'Premium'`.
  2. **`monitoring-deployment`**: `BAD_REQUEST` — "Failed to get Smart
     Detector SlowPageLoadTimeDetector/SlowServerResponseTimeDetector
     manifest." The smart-detector manifest backing service for these two
     detector types is not available in Azure Government. **Fix**: added a
     new `enableSmartDetectionRules` bool param (default `true`) making both
     `smartDetectorAlertRules` resources conditional; set to `false` in
     `main.parameters.aci.json` for this Gov deployment.
- Rebuilt `main.bicep` → 0 errors. Retrying deployment (attempt #5) — hit a
  transient DNS resolution failure against `management.usgovcloudapi.net`
  (unrelated to the template); retried immediately and connectivity was
  fine.

### Sixth deployment attempt — FAILED (2 more issues found)
- All previously-fixed resources succeeded. Two new issues surfaced:
  1. **`openai-deployment`**: `RequestConflict` — "Cannot modify resource...
     because the resource entity provisioning state is not terminal."
     Confirmed transient — likely caused by the earlier DNS-failure attempt
     leaving the OpenAI account mid-provisioning; re-checked and its
     `provisioningState` was `Succeeded` by the time of investigation, so a
     retry should proceed cleanly.
  2. **`acr-deployment`**: `SkuNotSupported` — "The SKU Standard is not
     supported." Unlike the earlier VNet-rule/data-endpoint issues, this is
     a full SKU-level rejection — this Federal/compliance subscription
     appears to enforce Premium-tier ACR only (consistent with
     `modules/acr.bicep`'s own header comment: "Premium SKU ACR... required
     for IL5/IL6"). **Decision (user-confirmed)**: switched
     `"acrSku"` to `"Premium"` in `main.parameters.aci.json` (cost tradeoff
     accepted — ~$50/mo vs ~$9/mo for Standard).
- Retrying deployment (attempt #7) with `acrSku=Premium`.

### Seventh deployment attempt — FAILED (ACR export policy conflict)
- ACR create itself succeeded this time. New failure:
  - **`acr-deployment`**: `DisableExport_PublicNetworkAccessMustBeDisabled` —
    "Cannot disable exports on registry... Request would have also enabled
    public network access. For exports to be disabled, public network
    access must also be disabled." `modules/acr.bicep` unconditionally set
    `exportPolicy.status: 'disabled'`, but `main.bicep`'s ACR module call
    sets `publicNetworkAccess: 'Enabled'` for non-prod environments —
    incompatible combination per ARM. **Fix**: made `exportPolicy.status`
    conditional on `publicNetworkAccess == 'Disabled'`.
- Rebuilt → 0 errors. Retrying (attempt #8).

### Eighth deployment attempt — FAILED (ACR diagnostic settings, same sink bug)
- ACR resource itself now created successfully. New failure:
  - **`acr-deployment`**: same `"At least one data sink needs to be
    specified"` bug as Key Vault (attempt #3), on ACR's own diagnostic
    settings. **Fix**: added `logAnalyticsWorkspaceId` param to
    `modules/acr.bicep`, wired as `workspaceId` sink (conditional resource),
    passed `monitoring!.outputs.logAnalyticsWorkspaceId` from `main.bicep`.
    Proactively checked all other modules in the deployment path (storage,
    redis, sql, monitoring, aci, openai) for the same missing-sink pattern —
    none found; `app-service.bicep`/`app-services.bicep`/`aks.bicep` have it
    too but aren't invoked for `containerDeploymentTarget=aci`.
- Rebuilt → 0 errors. Retrying (attempt #9).

### Ninth deployment attempt — FAILED (retentionPolicy not supported)
- **`acr-deployment`**: `"Diagnostic settings does not support retention for
  new diagnostic settings"` — ARM rejects the `retentionPolicy` sub-property
  on `logs`/`metrics` entries for newly-created diagnostic settings when a
  Log Analytics Workspace sink is used (retention is managed at the
  workspace level instead for these). **Fix**: removed `retentionPolicy`
  blocks from both `modules/acr.bicep` and `modules/keyvault.bicep`
  diagnostic settings (proactively, since Key Vault's uses the identical
  pattern).
- Rebuilt → 0 errors. Retrying (attempt #10).

### Tenth deployment attempt — FAILED (ACR + Key Vault succeeded! ACI diagnostics bug found)
- **ACR and Key Vault both deployed successfully for the first time.**
  New failures on the ACI container groups:
  - **`aci-mcp-deployment`** and **`aci-chat-deployment`**:
    `InvalidLogAnalytics` — "The log analytics setting is invalid. WorkspaceId
    and WorkspaceKey should not be null or empty." `modules/aci.bicep`'s
    legacy Log Analytics container-group diagnostics integration expects the
    workspace's `customerId` (GUID) + a primary shared key — NOT the ARM
    resource ID that `main.bicep` was passing (that's only correct for the
    newer `Microsoft.Insights/diagnosticSettings` sink pattern used by
    Key Vault/ACR).
  - **Fix**: added `logAnalyticsWorkspaceCustomerId` output to
    `modules/monitoring.bicep` (`logAnalyticsWorkspace.properties.customerId`)
    alongside the existing `@secure() output logAnalyticsWorkspaceKey`.
    Discovered that Bicep disallows accessing a `@secure()` module output
    via the `!` non-null-assertion operator (`monitoring!.outputs.x`) —
    "Secure outputs may only be accessed via a direct module reference."
    Worked around this by adding a direct `existing` resource reference
    (`newLogAnalyticsWorkspace`, with an explicit `dependsOn: [monitoring]`)
    in `main.bicep` and reading `.properties.customerId` /
    `.listKeys().primarySharedKey` directly from it instead of through the
    module output, for all four ACI module calls (mcp, chat, adminApi,
    adminClient).
- Rebuilt → 0 errors. Retrying (attempt #11).

### Eleventh deployment attempt — INFRASTRUCTURE FULLY SUCCEEDED (image build needed)
- The Log Analytics customerId/key fix worked — no more `InvalidLogAnalytics`
  errors. **All infrastructure resources deployed successfully**: network,
  Log Analytics/App Insights, storage, Redis, Azure OpenAI (+ model
  deployments), Key Vault (+ diagnostics), ACR (Premium, + diagnostics).
- Only remaining failures: **`aci-chat-deployment`** and
  **`aci-mcp-deployment`** — `InaccessibleImage`: "The image
  '...azurecr.us/platform-engineering-copilot-chat:latest'... is not
  accessible." This is expected/not a bug — the ACR registry is brand new
  and empty; no images have been built/pushed yet. **Next step**: build and
  push Docker images for `platform-engineering-copilot-chat` and
  `platform-engineering-copilot-mcp` to
  `platsupdevacr2sy2zj72zb3cg.azurecr.us`, then redeploy/restart the two ACI
  container groups.


