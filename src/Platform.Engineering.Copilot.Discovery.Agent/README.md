# Discovery Agent

> Azure resource discovery, inventory management, health monitoring, and dependency mapping specialist

## Overview

The Discovery Agent is a specialized AI agent that provides comprehensive Azure resource discovery, detailed inventory management, health monitoring, performance tracking, and dependency mapping across subscriptions and resource groups.

**Agent Type**: `Discovery`  
**Icon**: 🔍  
**Temperature**: 0.3 (precise for data queries)

## Capabilities

### 1. Resource Discovery

#### Subscription-wide Discovery
Discover all resources across Azure subscriptions:

```
Subscription Discovery: sub-production-001

Total Resources: 487

By Type:
  Virtual Machines:         89 (18%)
  Storage Accounts:         123 (25%)
  Virtual Networks:         45 (9%)
  AKS Clusters:            12 (2%)
  SQL Databases:           34 (7%)
  App Services:            56 (11%)
  Key Vaults:              23 (5%)
  Other:                   105 (22%)

By Location:
  East US:                 234 (48%)
  West US:                 156 (32%)
  Central US:              78 (16%)
  UK South:                19 (4%)

By Resource Group:
  rg-production:           187 (38%)
  rg-staging:              89 (18%)
  rg-development:          124 (25%)
  rg-shared:               87 (18%)
```

#### Resource Group Scoping
Targeted discovery within specific resource groups:

```
Resource Group: rg-production

Resources: 187

Compute:
  - VMs: 23
  - VM Scale Sets: 5
  - AKS Clusters: 3
  - App Services: 12

Storage:
  - Storage Accounts: 18
  - Managed Disks: 67

Networking:
  - VNets: 8
  - Load Balancers: 6
  - Application Gateways: 2
  - NSGs: 15

Data:
  - SQL Databases: 14
  - Cosmos DB: 5
  - Redis Cache: 3

Security:
  - Key Vaults: 7
  - Managed Identities: 23
```

#### Type-based Filtering
Find specific resource types:

```
Discovery Query: All AKS Clusters

Found: 12 AKS Clusters

1. aks-prod-cluster-001
   Location: East US
   Kubernetes Version: 1.28.3
   Node Count: 15 nodes
   Resource Group: rg-production

2. aks-staging-cluster
   Location: East US
   Kubernetes Version: 1.27.7
   Node Count: 6 nodes
   Resource Group: rg-staging

3. aks-dev-cluster
   Location: West US
   Kubernetes Version: 1.28.3
   Node Count: 3 nodes
   Resource Group: rg-development

[... 9 more clusters]

Summary:
  Total Nodes: 78
  Average Cluster Size: 6.5 nodes
  Kubernetes Versions: 1.28.3 (8), 1.27.7 (4)
```

#### Tag-based Search
Discover resources by tags:

```
Tag Search: environment=production

Found: 187 resources

By Type:
  VMs: 23
  Storage: 18
  AKS: 3
  SQL: 14
  App Service: 12
  [... more types]

Tag Compliance:
  ✅ cost-center: 187/187 (100%)
  ✅ owner: 187/187 (100%)
  ⚠️  project: 145/187 (78%)
  ❌ expires-on: 23/187 (12%)

Missing Tags:
  project: 42 resources
  expires-on: 164 resources
```

#### Location-based Discovery
Find resources in specific regions:

```
Location Search: East US

Found: 234 resources

Breakdown:
  Compute: 67 resources ($5,234/month)
  Storage: 89 resources ($1,890/month)
  Networking: 34 resources ($1,234/month)
  Databases: 23 resources ($3,456/month)
  Other: 21 resources ($567/month)

Total Monthly Cost: $12,381/month

Regional Distribution:
  East US: 234 (48%)
  West US: 156 (32%)
  Central US: 78 (16%)
  Other: 19 (4%)
```

### 2. Inventory Management

#### Comprehensive Inventory
Detailed resource properties and metadata:

```json
{
  "resourceId": "/subscriptions/.../resourceGroups/rg-prod/providers/Microsoft.Compute/virtualMachines/vm-web-01",
  "name": "vm-web-01",
  "type": "Microsoft.Compute/virtualMachines",
  "location": "eastus",
  "resourceGroup": "rg-production",
  "properties": {
    "vmSize": "Standard_D4s_v3",
    "osType": "Linux",
    "osDisk": {
      "diskSizeGB": 128,
      "managedDisk": {
        "storageAccountType": "Premium_LRS"
      }
    },
    "networkProfile": {
      "networkInterfaces": [
        "nic-vm-web-01"
      ]
    },
    "provisioningState": "Succeeded",
    "vmId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
  },
  "tags": {
    "environment": "production",
    "cost-center": "engineering",
    "owner": "platform-team",
    "application": "web-frontend"
  },
  "createdTime": "2025-01-15T10:30:00Z",
  "changedTime": "2025-11-10T14:22:00Z",
  "createdBy": "admin@company.com",
  "changedBy": "devops@company.com"
}
```

#### Tagging Analysis
Identify untagged and improperly tagged resources:

```
Tagging Compliance Report

Required Tags: environment, cost-center, owner

Total Resources: 487

Compliance Status:
  ✅ Fully Compliant: 345 (71%)
  ⚠️  Partially Compliant: 89 (18%)
  ❌ Non-Compliant: 53 (11%)

Missing Tags by Resource:
  environment: 53 resources
  cost-center: 89 resources
  owner: 67 resources

Most Common Issues:
1. Storage accounts missing cost-center (45 resources)
2. VMs missing owner (23 resources)
3. Disks missing environment (34 resources)

Remediation Script Generated:
  📄 tag_remediation.ps1 (53 resources to tag)
```

#### Lifecycle Tracking
Resource creation and modification history:

```
Resource Lifecycle Report

Created in Last 7 Days: 23 resources
  VMs: 5
  Storage: 8
  AKS: 1
  SQL: 2
  Other: 7

Modified in Last 7 Days: 67 resources
  Configuration Changes: 45
  Tag Updates: 12
  Scale Operations: 10

Oldest Resources:
1. vnet-prod-core (created 834 days ago)
2. stor-logs-001 (created 712 days ago)
3. sql-prod-db (created 598 days ago)

Newest Resources:
1. aks-test-cluster (created 2 hours ago)
2. vm-temp-build (created 1 day ago)
3. stor-data-new (created 3 days ago)
```

### 3. Health Monitoring

#### Resource Health Status
Azure Resource Health API integration:

```
Health Status: rg-production

Overall Health: ✅ Healthy (95%)

Resource Health:
✅ Healthy: 178 resources (95%)
⚠️  Degraded: 6 resources (3%)
❌ Unhealthy: 3 resources (2%)

Critical Issues:
❌ vm-app-03: Platform issue detected
   Issue: Host hardware failure
   Impact: VM unavailable
   Started: 2025-11-19 12:30 UTC
   Resolution: Auto-healing in progress

⚠️  sql-analytics-db: Degraded performance
   Issue: High CPU utilization (95%)
   Impact: Slow query responses
   Recommendation: Scale up tier

⚠️  aks-prod-cluster-001: Node pressure
   Issue: Memory pressure on 2/15 nodes
   Impact: Pod evictions detected
   Recommendation: Add nodes or scale pods
```

#### Performance Metrics
CPU, memory, network, and disk utilization:

```
Performance Metrics: vm-web-01 (Last 24 Hours)

CPU:
  Average: 45%
  Peak: 87% (at 14:30 UTC)
  Trend: +12% vs. last 7 days

Memory:
  Average: 62%
  Peak: 78%
  Available: 6.2 GB

Network:
  Inbound: 234 Mbps avg, 890 Mbps peak
  Outbound: 123 Mbps avg, 456 Mbps peak
  Total Transfer: 2.3 TB

Disk:
  IOPS: 1,234 avg, 3,456 peak
  Throughput: 45 MB/s avg, 123 MB/s peak
  Latency: 12ms p50, 34ms p95

Status: ✅ Healthy
Recommendations:
  - Consider scaling if CPU trend continues
  - Monitor memory for potential leaks
```

#### Availability Tracking
Uptime percentages and SLA compliance:

```
Availability Report: November 2025

Resource: app-prod-web
SLA Target: 99.95% (21m 36s downtime allowed/month)

Actual Uptime: 99.98%
  Total Minutes: 43,200
  Uptime: 43,191 minutes
  Downtime: 9 minutes

SLA Status: ✅ Met (12m 36s margin)

Incidents:
1. Nov 10, 02:15-02:19 (4 min)
   Cause: Planned maintenance
   Impact: None (zero-downtime deployment)

2. Nov 15, 14:30-14:35 (5 min)
   Cause: Platform issue
   Impact: Service unavailable
   RCA: Azure infrastructure issue

Monthly Trend:
  Oct 2025: 99.99%
  Sep 2025: 99.97%
  Aug 2025: 99.95%
  Jul 2025: 99.98%
```

### 4. Dependency Mapping

#### Resource Relationships
Map dependencies between Azure resources:

```
Dependency Map: app-prod-web

Direct Dependencies (app-prod-web depends on):
  ├─ App Service Plan: asp-prod-001
  ├─ VNet Integration: vnet-prod-001/snet-apps
  ├─ Key Vault: kv-prod-secrets
  │   └─ Secrets: 12
  ├─ Application Insights: ai-prod-monitoring
  │   └─ Log Analytics: law-prod
  ├─ SQL Database: sql-prod-db
  │   ├─ Server: sqlsrv-prod-001
  │   └─ Private Endpoint: pe-sql-prod
  └─ Storage Account: storprod001
      └─ Blob Container: uploads

Indirect Dependencies:
  ├─ NSG: nsg-apps (via VNet)
  ├─ Route Table: rt-apps (via VNet)
  ├─ DNS Zone: privatelink.database.windows.net (via SQL PE)
  └─ Managed Identity: id-app-prod (for Key Vault access)

Dependents (resources depending on app-prod-web):
  ├─ Front Door: fd-prod-cdn
  │   └─ Backend Pool: pool-apps
  ├─ API Management: apim-prod
  │   └─ API: web-api-v1
  └─ Application Gateway: ag-prod
      └─ Backend Pool: pool-web

Impact Analysis:
  Deleting app-prod-web affects: 3 resources
  Full dependency chain: 18 resources
  Recommended deletion order: available
```

#### Orphaned Resources
Detect resources without parent dependencies:

```
Orphaned Resources Report

Found: 34 orphaned resources

High-Value Orphans:
1. Managed Disks (12 disks)
   Total Size: 3.2 TB
   Monthly Cost: $234.56
   Reason: Parent VMs deleted
   Recommendation: Review and delete unused

2. Network Interfaces (8 NICs)
   Monthly Cost: $32.00
   Reason: VMs deallocated/deleted
   Recommendation: Safe to delete

3. Public IP Addresses (6 IPs)
   Monthly Cost: $21.60
   Reason: Load balancers removed
   Recommendation: Delete if unused

4. Storage Accounts (4 accounts)
   Total Size: 234 GB
   Monthly Cost: $189.23
   Reason: Associated apps deleted
   Recommendation: Archive or delete

5. Application Gateways (2 gateways)
   Monthly Cost: $456.78
   Reason: Backend pools empty
   Recommendation: Review configuration

Low-Value Orphans:
  - NSG rules (2): No associated subnets
  - Route tables (1): No associated subnets

Total Wasted Cost: $934.17/month
Potential Annual Savings: $11,210.04

Remediation script: cleanup_orphans.ps1
```

#### Network Topology
Visualize network connectivity:

```
Network Topology: rg-production

VNets: 3
  vnet-prod-hub (10.0.0.0/16)
  ├─ Subnets:
  │  ├─ snet-gateway (10.0.1.0/24) - VPN Gateway
  │  ├─ snet-firewall (10.0.2.0/24) - Azure Firewall
  │  └─ snet-shared (10.0.3.0/24) - 12 resources
  └─ Peerings:
     ├─ vnet-prod-spoke1 (peered)
     └─ vnet-prod-spoke2 (peered)

  vnet-prod-spoke1 (10.1.0.0/16)
  ├─ Subnets:
  │  ├─ snet-web (10.1.1.0/24) - 23 VMs
  │  ├─ snet-app (10.1.2.0/24) - 15 VMs
  │  └─ snet-data (10.1.3.0/24) - 8 SQL Servers
  └─ Peerings:
     └─ vnet-prod-hub (peered)

  vnet-prod-spoke2 (10.2.0.0/16)
  ├─ Subnets:
  │  ├─ snet-aks (10.2.1.0/24) - AKS cluster
  │  └─ snet-services (10.2.2.0/24) - 12 App Services
  └─ Peerings:
     └─ vnet-prod-hub (peered)

Connectivity:
  Internet → VPN Gateway → Hub → Spokes
  Azure Firewall: All spoke-to-spoke traffic
  Private Endpoints: 23 (SQL, Storage, Key Vault)

Security:
  NSGs: 15 (covering all subnets)
  UDRs: 8 (force-tunnel to firewall)
```

### 5. Reporting

#### Export Formats
- **JSON**: Machine-readable structured data
- **CSV**: Excel-compatible spreadsheet
- **Excel**: Formatted workbook with multiple sheets
- **Markdown**: Human-readable reports

#### Report Types

**Inventory Report:**
```csv
ResourceName,Type,Location,ResourceGroup,CreatedDate,Cost,Tags
vm-web-01,VirtualMachine,eastus,rg-prod,2025-01-15,$234.56,env=prod;app=web
stor001,StorageAccount,eastus,rg-prod,2024-06-20,$89.12,env=prod;app=data
[... more resources]
```

**Compliance Report:**
```
Tagging Compliance Report

Organization: Contoso Corp
Generated: 2025-11-19 14:30 UTC

Total Resources: 487
Compliant: 345 (71%)
Non-Compliant: 142 (29%)

Required Tags:
  - environment: 434/487 (89%)
  - cost-center: 398/487 (82%)
  - owner: 420/487 (86%)
  - project: 356/487 (73%)

Non-Compliant Resources: [see attached CSV]
```

**Architecture Diagram:**
```
┌─────────────────────────────────────────────┐
│          vnet-prod-hub (10.0.0.0/16)        │
│  ┌───────────┐  ┌──────────────┐           │
│  │ VPN       │  │ Azure        │           │
│  │ Gateway   │  │ Firewall     │           │
│  └───────────┘  └──────────────┘           │
└─────────────────────────────────────────────┘
           │                    │
           ├────────────────────┤
           │                    │
┌──────────▼───────┐  ┌────────▼──────────┐
│  vnet-spoke1     │  │  vnet-spoke2      │
│  (10.1.0.0/16)   │  │  (10.2.0.0/16)    │
│  ┌────────────┐  │  │  ┌─────────────┐  │
│  │ Web Tier   │  │  │  │ AKS Cluster │  │
│  │ (23 VMs)   │  │  │  │ (15 nodes)  │  │
│  └────────────┘  │  │  └─────────────┘  │
│  ┌────────────┐  │  │                   │
│  │ Data Tier  │  │  │                   │
│  │ (8 DBs)    │  │  │                   │
│  └────────────┘  │  │                   │
└──────────────────┘  └───────────────────┘
```

## Plugins

### AzureResourceDiscoveryPlugin

Main plugin for resource discovery and inventory.

**Functions:**
- `discover_resources` - Find resources by criteria
- `get_resource_inventory` - Detailed inventory with properties
- `analyze_resource_tags` - Tag compliance analysis
- `map_dependencies` - Resource relationship mapping
- `find_orphaned_resources` - Detect orphaned resources
- `get_resource_health` - Health status check
- `get_performance_metrics` - Performance data
- `visualize_network_topology` - Network diagram generation
- `export_inventory_report` - Generate reports

### ConfigurationPlugin

Azure subscription management.

**Functions:**
- `set_azure_subscription` - Set active subscription
- `get_azure_subscription` - Get current subscription

## Example Prompts

### Resource Discovery

```
"List all resources in my subscription"
"Find all VMs in East US region"
"Show storage accounts in resource group rg-data"
"Discover AKS clusters with tag environment=production"
"List all resources created in last 7 days"
```

### Inventory & Tagging

```
"Generate inventory report for rg-prod"
"Find resources missing 'cost-center' tag"
"Show all untagged resources"
"Check tagging compliance across subscription"
"List resources without owner tag"
```

### Health & Performance

```
"Check health status of all VMs"
"Show performance metrics for vm-web-01"
"Find resources with high CPU utilization"
"Get availability report for app-service-001"
"Monitor health of production environment"
```

### Dependencies & Topology

```
"Map dependencies for app-service-001"
"Find orphaned disks and NICs"
"Show network topology for rg-network"
"Visualize resource relationships in rg-prod"
"Identify resources with no dependencies"
```

### Reporting

```
"Export inventory as CSV"
"Generate compliance report for tagging"
"Create architecture diagram for network"
"Show resource lifecycle report"
"Export orphaned resources list"
```

## Key Services

| Service | Purpose |
|---------|---------|
| `AzureResourceDiscoveryService` | Resource enumeration via Azure SDK |
| `ResourceHealthMonitoringService` | Health and performance tracking |
| `DependencyMappingService` | Resource relationship analysis |
| `TaggingAnalysisService` | Tag compliance checking |
| `InventoryReportingService` | Report generation and export |
| `NetworkTopologyService` | Network visualization |
| `AzureMcpClient` | Azure MCP Server integration |

## Configuration

### appsettings.json

```json
{
  "DiscoveryAgent": {
    "Temperature": 0.3,
    "MaxTokens": 4000,
    "EnableHealthMonitoring": true,
    "EnablePerformanceMetrics": true,
    "EnableDependencyMapping": true
  },
  "AzureMcpServer": {
    "Endpoint": "http://localhost:3000",
    "Timeout": 30
  },
  "Discovery": {
    "CacheDurationMinutes": 15,
    "MaxResourcesPerQuery": 1000,
    "IncludeDeletedResources": false,
    "RequiredTags": ["environment", "cost-center", "owner"]
  },
  "HealthMonitoring": {
    "RefreshIntervalMinutes": 5,
    "MetricsRetentionDays": 90,
    "PerformanceMetrics": ["CPU", "Memory", "Network", "Disk"]
  }
}
```

## Usage Patterns

### Pattern 1: Complete Inventory

```
User: "Generate inventory for my subscription"

Agent Response:
🔍 Resource Inventory

Subscription: sub-production-001
Total Resources: 487

Resource Breakdown:
  Virtual Machines: 89 (18%)
  Storage Accounts: 123 (25%)
  Virtual Networks: 45 (9%)
  AKS Clusters: 12 (2%)
  SQL Databases: 34 (7%)
  App Services: 56 (11%)
  Key Vaults: 23 (5%)
  Other: 105 (22%)

Locations:
  East US: 234 (48%)
  West US: 156 (32%)
  Central US: 78 (16%)
  UK South: 19 (4%)

Tagging Status:
  Compliant: 345 (71%)
  Partial: 89 (18%)
  Missing: 53 (11%)

Monthly Cost: $45,678.90

📊 Full inventory: inventory_20251119.xlsx
📄 CSV export: inventory_20251119.csv
```

### Pattern 2: Orphaned Resource Cleanup

```
User: "Find orphaned resources"

Agent Response:
🔍 Orphaned Resources Detected

Found: 34 orphaned resources
Wasted Cost: $934.17/month ($11,210/year)

High Priority (expensive):
1. ⚠️  Managed Disks (12)
   Size: 3.2 TB
   Cost: $234.56/month
   Recommendation: Review and delete

2. ⚠️  Application Gateways (2)
   Cost: $456.78/month
   Reason: Empty backend pools
   Recommendation: Check configuration

3. ⚠️  Storage Accounts (4)
   Size: 234 GB
   Cost: $189.23/month
   Recommendation: Archive or delete

Low Priority:
  - NICs (8): $32.00/month
  - Public IPs (6): $21.60/month

🔧 Remediation Options:
  1. Review each resource manually
  2. Auto-delete safe resources (NICs, IPs)
  3. Generate cleanup script

Would you like me to create a cleanup script?
```

### Pattern 3: Dependency Analysis

```
User: "Map dependencies for app-service-001"

Agent Response:
🔍 Dependency Analysis: app-service-001

Direct Dependencies (7):
✅ App Service Plan: asp-prod-001
✅ VNet Integration: vnet-prod/snet-apps
✅ Key Vault: kv-prod-secrets
✅ Application Insights: ai-prod-monitoring
✅ SQL Database: sql-prod-db
✅ Storage Account: storprod001
✅ Managed Identity: id-app-prod

Indirect Dependencies (6):
  → Log Analytics: law-prod (via App Insights)
  → SQL Server: sqlsrv-prod (via SQL Database)
  → Private Endpoint: pe-sql (via SQL Database)
  → NSG: nsg-apps (via VNet)
  → DNS Zone: privatelink.database.windows.net
  → Subnet: snet-apps (via VNet Integration)

Dependents (3 resources depend on this):
  ← Front Door: fd-prod-cdn
  ← API Management: apim-prod
  ← Application Gateway: ag-prod

Impact Analysis:
  Total dependency chain: 16 resources
  Deleting this affects: 3 dependent resources
  Deletion order available: Yes

⚠️  Warning: Cannot delete without first removing 3 dependents
```

## Integration with Other Agents

### → Compliance Agent
Discovery Agent inventories resources → Compliance Agent scans for compliance

### → Cost Management Agent
Discovery Agent finds resources → Cost Management analyzes their costs

### → Environment Agent
Discovery Agent maps dependencies → Environment Agent uses for cloning

## Troubleshooting

### Issue: No Resources Found

**Symptom**: "No resources discovered"

**Solutions:**
```bash
# Verify subscription access
az account show

# Check RBAC permissions (need Reader role minimum)
az role assignment list --assignee <user-id>

# List resource providers
az provider list --query "[?registrationState=='Registered']"

# Try Azure CLI directly
az resource list --subscription {sub-id}
```

### Issue: Incomplete Inventory

**Symptom**: Some resources missing from inventory

**Solutions:**
```bash
# Check resource provider registration
az provider show --namespace Microsoft.Compute
az provider show --namespace Microsoft.Network

# Register missing providers
az provider register --namespace Microsoft.Compute

# Verify resource group access
az group list
```

## Performance

| Operation | Typical Duration | Resources |
|-----------|-----------------|-----------|
| Subscription discovery | 30-60 seconds | 100-500 resources |
| Resource group discovery | 5-15 seconds | 10-100 resources |
| Type-based search | 2-5 seconds | Results vary |
| Dependency mapping | 5-10 seconds | Per resource |
| Health check | 3-8 seconds | Per resource |
| Inventory export | 10-30 seconds | 100-500 resources |

## Limitations

- **API Rate Limits**: Azure Resource Manager API throttling
- **Large Subscriptions**: 1000+ resources may require pagination
- **Cross-Subscription**: One subscription at a time
- **Real-time Data**: Resource cache refresh interval (15 min default)

## References

- [Azure Resource Manager](https://docs.microsoft.com/en-us/azure/azure-resource-manager/)
- [Azure Resource Graph](https://docs.microsoft.com/en-us/azure/governance/resource-graph/)
- [Azure Resource Health](https://docs.microsoft.com/en-us/azure/service-health/resource-health-overview)
- [Azure Monitor](https://docs.microsoft.com/en-us/azure/azure-monitor/)

---

**Last Updated**: November 2025  
**Version**: 0.6.35  
**Agent Type**: `Discovery`
