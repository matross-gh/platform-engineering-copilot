# Environment Agent

> Environment lifecycle management, cloning, scaling, and deployment strategy specialist

## Overview

The Environment Agent is a specialized AI agent that manages the complete lifecycle of Azure environments including creation, cloning, scaling, blue-green deployments, canary releases, and configuration drift detection.

**Agent Type**: `Environment`  
**Icon**: 🌍  
**Temperature**: 0.3 (precise for operational tasks)

## Capabilities

### 1. Environment Lifecycle

#### Create Environments
Provision complete environments with all resources:

- **Development**: Low-cost, minimal redundancy
- **Testing**: Moderate scale, production-like configuration
- **Staging**: Production mirror for pre-deployment validation
- **Production**: High availability, disaster recovery enabled
- **Custom**: User-defined environment specifications

**Environment Components:**
- Resource groups with naming conventions
- Networking (VNets, subnets, NSGs)
- Compute resources (VMs, AKS, App Service)
- Storage and databases
- Monitoring and logging
- Tags for environment classification

#### Clone Environments
Replicate existing environments with all dependencies:

```
Clone Operation: Production → Staging

Source: rg-production (23 resources)
Target: rg-staging-new

Cloning Resources:
✅ VNet: vnet-prod-001 → vnet-staging-001
✅ AKS: aks-prod-cluster → aks-staging-cluster
✅ SQL: sql-prod-db → sql-staging-db
✅ Storage: storprod001 → storstaging001
✅ App Service: app-prod-web → app-staging-web

Configuration Updates:
- Scaled down to 50% capacity
- Changed environment tags
- Updated connection strings
- Applied staging-specific configs

Clone Status: ✅ Complete (23/23 resources)
Duration: 4m 32s
```

**Clone Options:**
- **Full Clone**: Exact replica including data
- **Configuration Clone**: Resources only (no data)
- **Scaled Clone**: Smaller/larger resource sizes
- **Selective Clone**: Choose specific resource types

#### Scale Environments
Adjust environment capacity up or down:

```
Scale Operation: Development → 2x Capacity

Resource Scaling:
✅ AKS nodes: 3 → 6 nodes
✅ App Service: B1 → S1 tier
✅ SQL Database: Basic → Standard S2
✅ VM Scale Set: 2 → 4 instances

Estimated Cost Impact:
  Before: $234.56/month
  After:  $468.12/month
  Increase: +100% ($233.56/month)

Scale Status: ✅ Complete
```

**Scaling Types:**
- **Horizontal**: Add/remove instances
- **Vertical**: Increase/decrease resource sizes
- **Proportional**: Scale all resources by percentage
- **Selective**: Scale specific resource types only

#### Destroy Environments
Clean up environments with dependency-aware deletion:

```
Destroy Operation: rg-old-dev

Pre-deletion Checks:
✅ No production resources detected
✅ No active connections
✅ Backups completed
⚠️  Found 2 locked resources (will skip)

Deletion Order (dependency-aware):
1. Application Gateway → OK
2. AKS Cluster → OK
3. SQL Databases → OK
4. VMs → OK
5. Disks → OK
6. NICs → OK
7. NSGs → OK
8. VNets → OK
9. Storage Accounts → OK
10. Resource Group → OK

Destroy Status: ✅ Complete (21/23 resources)
Skipped: 2 locked resources
Duration: 3m 18s
```

### 2. Deployment Patterns

#### Blue-Green Deployment
Zero-downtime environment swaps:

```
Blue-Green Deployment

Blue Environment (Current):
  - app-prod-blue (Active, 100% traffic)
  - Version: 1.2.3
  - Health: ✅ Healthy

Green Environment (New):
  - app-prod-green (Staging)
  - Version: 1.3.0
  - Health: ✅ Healthy

Deployment Steps:
1. Deploy v1.3.0 to Green ✅
2. Run smoke tests on Green ✅
3. Switch 100% traffic to Green ✅
4. Monitor for 10 minutes ✅
5. Keep Blue for rollback (24h)

Status: ✅ Deployment Complete
Rollback: Available (blue environment preserved)
```

**Benefits:**
- Instant rollback capability
- Zero downtime
- Full smoke testing before traffic switch
- Production validation

#### Canary Deployment
Gradual traffic shifting for risk mitigation:

```
Canary Deployment

Baseline (v1.2.3): 90% traffic
Canary (v1.3.0):   10% traffic

Phase 1: 10% Traffic (Current)
  Requests: 1,234 (10%)
  Errors: 2 (0.16%)
  Latency: 124ms (p95)
  Status: ✅ Healthy

Next Phase: 50% Traffic (in 30 min)
Final Phase: 100% Traffic (in 90 min)

Monitoring:
  Error Rate: 0.16% (threshold: 1%)
  Latency p95: 124ms (threshold: 500ms)
  CPU: 45% (threshold: 80%)

Status: ✅ Proceeding to next phase
```

**Canary Phases:**
1. **10% Traffic**: Initial validation (30 min)
2. **25% Traffic**: Expanded testing (30 min)
3. **50% Traffic**: Half rollout (30 min)
4. **100% Traffic**: Full rollout

**Automatic Rollback Triggers:**
- Error rate > threshold
- Latency > threshold
- CPU/Memory > threshold
- Manual abort

#### Rolling Update
Progressive resource updates:

```
Rolling Update: AKS Node Pool

Strategy: RollingUpdate
Max Surge: 1 node
Max Unavailable: 0 nodes

Update Progress:
✅ Node 1: Drained → Updated → Ready (v1.28.3)
✅ Node 2: Drained → Updated → Ready (v1.28.3)
🔄 Node 3: Draining... (pods migrating)
⏸️  Node 4: Pending
⏸️  Node 5: Pending
⏸️  Node 6: Pending

Status: 2/6 nodes updated (33%)
Estimated Completion: 18 minutes
```

#### A/B Testing
Run multiple environment variants:

```
A/B Test Configuration

Variant A (Control): 50% traffic
  - Feature flags: legacy-ui=true
  - Version: 1.2.3

Variant B (Test): 50% traffic
  - Feature flags: new-ui=true
  - Version: 1.3.0-beta

Metrics Collected:
  - Conversion rate
  - Page load time
  - User engagement
  - Error rate

Duration: 7 days
Sample Size: ~50,000 users per variant
```

### 3. Configuration Management

#### Drift Detection
Compare actual vs. desired state:

```
Configuration Drift Report

Environment: rg-production
Baseline: production-baseline.json
Scan Date: 2025-11-19

Drift Detected:
⚠️  VM: vm-prod-web-01
    Property: vmSize
    Expected: Standard_D4s_v3
    Actual: Standard_D8s_v3
    Drift: Manual resize detected

⚠️  Storage: storprod001
    Property: allowBlobPublicAccess
    Expected: false
    Actual: true
    Drift: Security policy violation

✅ AKS: aks-prod-cluster (No drift)
✅ SQL: sql-prod-db (No drift)

Summary:
  Total Resources: 23
  In Sync: 21 (91%)
  Drifted: 2 (9%)

Recommendations:
1. Revert vm-prod-web-01 to Standard_D4s_v3
2. Disable public blob access on storprod001
```

#### Environment Promotion
Move configurations through lifecycle stages:

```
Promotion: Development → Test

Source: rg-dev
Target: rg-test

Configuration Updates:
✅ App Service: Basic → Standard tier
✅ SQL Database: Basic → Standard S1
✅ Monitoring: Enabled diagnostics
✅ Tags: environment=dev → environment=test
✅ Connection strings updated
✅ Secrets rotated

Validation:
✅ All tests passed
✅ Health checks OK
✅ Performance baseline met

Promotion Status: ✅ Complete
Ready for Test: Yes
```

**Promotion Workflow:**
```
Dev → Test → Staging → Production
```

Each promotion includes:
- Configuration transformation
- Secret rotation
- Performance validation
- Automated testing
- Approval gates (for Prod)

#### Configuration Sync
Keep environments consistent:

```
Configuration Sync

Source: rg-production (baseline)
Targets: rg-staging, rg-test, rg-dev

Syncing:
✅ NSG rules: prod → staging, test
✅ App settings: prod → staging
✅ Tags: standardized across all
✅ Monitoring alerts: prod → all
⏭️  Skipped: Database configs (env-specific)

Sync Status: ✅ Complete
Environments in sync: 4/4
```

### 4. Resource Management

#### Dependency Mapping
Understand resource relationships:

```
Resource Dependencies: app-prod-web

Direct Dependencies:
  → App Service Plan: asp-prod-001
  → VNet Integration: vnet-prod-001/snet-apps
  → Key Vault: kv-prod-secrets
  → Application Insights: ai-prod-monitoring
  → SQL Database: sql-prod-db

Indirect Dependencies:
  → Storage Account: storprod001 (via Key Vault)
  → Log Analytics: law-prod (via App Insights)
  → Private Endpoint: pe-sql (via SQL)

Dependents (resources that depend on this):
  ← Front Door: fd-prod-cdn
  ← API Management: apim-prod-gateway

Impact Analysis:
  Deleting this resource will affect 2 dependents
  Recommend: Remove dependencies first
```

#### Health Monitoring
Track environment health:

```
Environment Health: rg-production

Overall Status: ✅ Healthy (94/100)

Component Health:
✅ Compute: 98/100 (23/23 resources healthy)
✅ Networking: 95/100 (1 minor issue)
⚠️  Storage: 88/100 (1 warning)
✅ Databases: 100/100 (all healthy)

Issues Detected:
⚠️  NSG: nsg-web has 0 applied rules (check config)
⚠️  Storage: storprod001 high latency (p95: 245ms)

Recommendations:
1. Review NSG rule application
2. Consider upgrading storage tier
```

## Plugins

### EnvironmentManagementPlugin

Main plugin for all environment operations.

**Functions:**
- `create_environment` - Provision new environment
- `clone_environment` - Replicate existing environment
- `scale_environment` - Adjust environment capacity
- `destroy_environment` - Clean up environment
- `deploy_blue_green` - Blue-green deployment
- `deploy_canary` - Canary deployment with traffic shifting
- `rolling_update` - Progressive resource updates
- `detect_drift` - Configuration drift detection
- `promote_environment` - Move config through lifecycle
- `sync_configuration` - Synchronize environment configs
- `get_environment_health` - Health status check

### ConfigurationPlugin

Azure subscription management.

**Functions:**
- `set_azure_subscription` - Set active subscription
- `get_azure_subscription` - Get current subscription

## Example Prompts

### Lifecycle Operations

```
"Create a new dev environment"
"Clone production to staging environment"
"Scale up the test environment by 50%"
"Destroy the old-dev environment"
"Provision QA environment with 3 VMs and AKS"
```

### Deployment Patterns

```
"Deploy with blue-green strategy"
"Start canary deployment with 10% traffic"
"Perform rolling update of production"
"Setup A/B test between v1.2 and v1.3"
"Switch all traffic to green environment"
"Rollback canary deployment"
```

### Configuration Management

```
"Check for configuration drift in staging"
"Promote dev environment to test"
"Sync configuration between environments"
"Compare production and staging configs"
"Show configuration differences"
```

### Resource Operations

```
"Map dependencies for app-service-001"
"Check health of production environment"
"Show environment resource inventory"
"Validate environment before deployment"
```

## Key Services

| Service | Purpose |
|---------|---------|
| `EnvironmentLifecycleService` | Environment CRUD operations |
| `EnvironmentCloningService` | Resource replication |
| `DeploymentStrategyService` | Blue-green, canary patterns |
| `DriftDetectionService` | Configuration drift analysis |
| `PromotionService` | Environment promotion workflow |
| `HealthMonitoringService` | Environment health tracking |
| `DependencyAnalyzer` | Resource relationship mapping |

## Configuration

### appsettings.json

```json
{
  "EnvironmentAgent": {
    "Temperature": 0.3,
    "MaxTokens": 4000,
    "DefaultScalingFactor": 1.0,
    "EnableDriftDetection": true,
    "EnableHealthMonitoring": true
  },
  "EnvironmentManagement": {
    "NamingConvention": "{env}-{type}-{name}-{seq}",
    "DefaultTags": {
      "ManagedBy": "PlatformEngineeringCopilot",
      "CreatedBy": "EnvironmentAgent"
    },
    "CloneDataByDefault": false,
    "PreserveBlueEnvironmentHours": 24
  },
  "DeploymentStrategies": {
    "Canary": {
      "Phases": [10, 25, 50, 100],
      "PhaseDurationMinutes": 30,
      "ErrorRateThreshold": 1.0,
      "LatencyThresholdMs": 500
    },
    "BlueGreen": {
      "SmokeTestDurationMinutes": 10,
      "PreserveOldEnvironmentHours": 24
    }
  }
}
```

## Usage Patterns

### Pattern 1: Environment Clone

```
User: "Clone production to staging"

Agent Response:
🌍 Cloning Environment

Source: rg-production
Target: rg-staging-new

Analysis:
  Resources to clone: 23
  Estimated duration: 4-6 minutes
  Estimated cost: $234.56/month (50% of prod)

Cloning resources...
✅ VNet: vnet-prod-001 → vnet-staging-001
✅ AKS: aks-prod-cluster → aks-staging-cluster (3 nodes)
✅ SQL: sql-prod-db → sql-staging-db (schema only)
✅ Storage: storprod001 → storstaging001
✅ App Service: app-prod-web → app-staging-web

Configuration updates:
✅ Scaled to 50% capacity
✅ Updated tags: environment=staging
✅ Rotated secrets
✅ Connected to staging KeyVault

Clone Status: ✅ Complete
Resources cloned: 23/23
Duration: 4m 32s
```

### Pattern 2: Blue-Green Deployment

```
User: "Deploy with blue-green strategy"

Agent Response:
🔄 Blue-Green Deployment

Current State:
  Blue (Active): app-prod-blue, v1.2.3, 100% traffic
  Green (Staging): None

Deployment Plan:
1. Provision Green environment
2. Deploy v1.3.0 to Green
3. Run smoke tests
4. Switch traffic to Green
5. Monitor for 10 minutes
6. Preserve Blue for 24h rollback

Executing...
✅ Step 1: Green environment created
✅ Step 2: v1.3.0 deployed to Green
✅ Step 3: Smoke tests passed (42/42)
✅ Step 4: Traffic switched to Green
🔍 Step 5: Monitoring (10 min)...

Metrics:
  Error Rate: 0.12% (threshold: 1%)
  Latency p95: 142ms (threshold: 500ms)
  Status: ✅ Healthy

✅ Deployment Complete!
Green is now active
Blue preserved for rollback (until 2025-11-20 14:30)
```

### Pattern 3: Drift Detection

```
User: "Check for configuration drift in production"

Agent Response:
🔍 Configuration Drift Analysis

Environment: rg-production
Baseline: production-baseline-20251101.json
Scan Time: 2025-11-19 14:30 UTC

Drift Detected: 2 resources (9%)

⚠️  vm-prod-web-01
    Property: vmSize
    Expected: Standard_D4s_v3
    Actual: Standard_D8s_v3
    Change Date: 2025-11-15 09:23 UTC
    Changed By: admin@company.com
    Reason: Manual resize
    Impact: +$187.20/month

⚠️  storprod001
    Property: allowBlobPublicAccess
    Expected: false
    Actual: true
    Change Date: 2025-11-18 16:45 UTC
    Changed By: developer@company.com
    Reason: Unknown
    Impact: ⚠️ Security violation

Recommendations:
1. Revert vm-prod-web-01 or update baseline
2. Disable public blob access immediately
3. Enable Azure Policy to prevent manual changes

Would you like me to remediate these issues?
```

## Integration with Other Agents

### → Infrastructure Agent
Infrastructure Agent creates baseline → Environment Agent clones and manages lifecycle

### → Compliance Agent
Environment Agent deploys → Compliance Agent validates compliance

### → Cost Management Agent
Environment Agent scales → Cost Management tracks cost changes

## Troubleshooting

### Issue: Clone Fails

**Symptom**: "Failed to clone environment"

**Solutions:**
```bash
# Check RBAC permissions
az role assignment list --assignee <user-id>

# Need Contributor or Owner
az role assignment create \
  --role "Contributor" \
  --assignee <user-id> \
  --scope "/subscriptions/{sub-id}"

# Check resource quotas
az vm list-usage --location eastus

# Verify target resource group doesn't exist
az group show --name rg-staging-new
```

### Issue: Blue-Green Traffic Switch Fails

**Symptom**: "Failed to switch traffic"

**Solutions:**
```bash
# Verify Application Gateway/Front Door configuration
az network application-gateway show \
  --name ag-prod \
  --resource-group rg-network

# Check backend pool health
az network application-gateway show-backend-health \
  --name ag-prod \
  --resource-group rg-network

# Manual traffic switch
az network application-gateway url-path-map update \
  --gateway-name ag-prod \
  --name path-map \
  --default-backend-address-pool pool-green
```

## Performance

| Operation | Typical Duration | Resources |
|-----------|-----------------|-----------|
| Create environment | 5-10 minutes | 10-20 resources |
| Clone environment | 4-8 minutes | 10-30 resources |
| Scale environment | 2-5 minutes | Per resource type |
| Blue-green switch | 1-2 minutes | Traffic routing |
| Drift detection | 30-60 seconds | Per environment |

## Limitations

- **Cross-Region Cloning**: Limited to same region
- **Resource Dependencies**: Complex dependencies may require manual handling
- **Data Cloning**: Large databases may timeout
- **Rollback Window**: Blue environment preserved for 24 hours only

## References

- [Azure Resource Groups](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/overview)
- [Blue-Green Deployments](https://martinfowler.com/bliki/BlueGreenDeployment.html)
- [Canary Releases](https://martinfowler.com/bliki/CanaryRelease.html)
- [Configuration Management](https://docs.microsoft.com/en-us/azure/automation/automation-dsc-overview)

---

**Last Updated**: November 2025  
**Version**: 0.6.35  
**Agent Type**: `Environment`
