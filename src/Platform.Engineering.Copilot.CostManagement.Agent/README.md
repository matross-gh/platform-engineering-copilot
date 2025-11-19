# Cost Management Agent

> Azure cost analysis, budget management, and cost optimization specialist

## Overview

The Cost Management Agent is a specialized AI agent that provides real-time cost analysis, budget tracking, forecasting, and optimization recommendations for Azure resources. It helps organizations control cloud spending and maximize ROI.

**Agent Type**: `CostManagement`  
**Icon**: 💰  
**Temperature**: 0.3 (precise for financial analysis)

## Capabilities

### 1. Cost Analysis

#### Real-time Cost Tracking
- **Current Month Spend**: Total costs accumulated this month
- **Daily Trends**: Spending patterns by day
- **Hourly Breakdown**: Granular cost tracking for rapid changes
- **Cost Velocity**: Rate of spending increase/decrease

#### Multi-dimensional Breakdowns

**By Resource Type:**
```
Virtual Machines:    $5,234.12 (42%)
Storage Accounts:    $1,890.45 (15%)
Azure SQL:           $1,567.89 (13%)
Networking:          $1,234.56 (10%)
AKS:                 $982.34 (8%)
Other:               $1,456.78 (12%)
```

**By Resource Group:**
```
rg-production:       $7,890.12 (63%)
rg-staging:          $2,345.67 (19%)
rg-development:      $1,234.56 (10%)
rg-test:             $895.79 (7%)
```

**By Location/Region:**
```
East US:             $6,543.21 (52%)
West US:             $3,210.98 (26%)
Central US:          $1,876.54 (15%)
UK South:            $735.41 (6%)
```

**By Tags:**
```
CostCenter=Engineering:  $5,432.10 (43%)
CostCenter=Marketing:    $3,210.98 (26%)
CostCenter=Sales:        $2,109.87 (17%)
Environment=Prod:        $7,890.12 (63%)
Environment=Dev:         $1,876.54 (15%)
```

**By Service Tier:**
```
Premium:             $4,567.89 (36%)
Standard:            $5,678.90 (45%)
Basic:               $2,119.35 (17%)
```

### 2. Budget Management

#### Budget Creation
Create budgets with:
- **Scope**: Subscription, resource group, or tag-based
- **Amount**: Monthly, quarterly, or annual limits
- **Alerts**: Threshold-based notifications (50%, 80%, 100%, 120%)
- **Period**: Current month, calendar year, fiscal year

#### Budget Tracking
```
Budget: Production Workloads
Period: November 2025
Limit:  $10,000.00

Current Spend: $7,890.12 (79%)
Remaining:     $2,109.88 (21%)
Forecast:      $9,876.54 (99%)

Status: ⚠️ Approaching Limit
Alert: 80% threshold exceeded
```

#### Budget Alerts
- **50% Threshold**: Early warning
- **80% Threshold**: Action required
- **100% Threshold**: Budget exceeded
- **120% Threshold**: Critical overspend
- **Custom Thresholds**: User-defined percentages

### 3. Cost Forecasting

Predict future costs based on historical trends:

```
Cost Forecast (Next 30 Days)

Current Trend:   +12% month-over-month
Forecast Model:  Linear regression + seasonal adjustment

Projected Costs:
  Week 1:  $2,345.67
  Week 2:  $2,456.78
  Week 3:  $2,567.89
  Week 4:  $2,678.90
  Total:   $10,049.24

Confidence:      87%
Variance Range:  ±$512.34

Recommendation: Budget increase needed
```

**Forecasting Models:**
- **Linear Trend**: Simple extrapolation
- **Moving Average**: Smoothed predictions
- **Seasonal Adjustment**: Account for monthly patterns
- **Exponential Smoothing**: Weight recent data more heavily

### 4. Cost Optimization

#### Right-sizing Recommendations
Identify over-provisioned resources:

```
Right-sizing Opportunities

1. VM: vm-prod-web-01
   Current:  Standard_D8s_v3 ($374.40/month)
   Recommended: Standard_D4s_v3 ($187.20/month)
   Savings: $187.20/month (50%)
   Reason: CPU utilization <15% for 30 days

2. SQL Database: sqldb-analytics
   Current:  Gen5 32 vCores ($7,890/month)
   Recommended: Gen5 16 vCores ($3,945/month)
   Savings: $3,945/month (50%)
   Reason: DTU utilization <20%

Total Monthly Savings: $4,132.20
Annual Savings: $49,586.40
```

#### Reserved Instance Analysis
```
Reserved Instance Opportunities

1. Virtual Machines (D-Series)
   Current On-Demand: $2,345.67/month
   1-Year RI Cost:    $1,523.45/month
   3-Year RI Cost:    $1,234.56/month
   
   Savings:
   - 1-Year: $822.22/month (35%)
   - 3-Year: $1,111.11/month (47%)

2. SQL Database (vCore)
   Current On-Demand: $7,890.12/month
   1-Year RI Cost:    $5,134.58/month
   3-Year RI Cost:    $4,151.67/month
   
   Savings:
   - 1-Year: $2,755.54/month (35%)
   - 3-Year: $3,738.45/month (47%)

Recommendation: Purchase 3-year RIs
Total Annual Savings: $58,190.72
```

#### Idle Resource Detection
```
Idle Resources Detected

1. VM: vm-old-test-server
   Status: Stopped (30 days)
   Monthly Cost: $187.20 (storage + IP)
   Recommendation: Delete or deallocate

2. Storage Account: olddataarchive
   Last Access: 90 days ago
   Monthly Cost: $234.56
   Recommendation: Move to Cool/Archive tier

3. SQL Database: db-abandoned-project
   Connections: 0 (14 days)
   Monthly Cost: $456.78
   Recommendation: Pause or delete

Total Wasted Spend: $878.54/month
Potential Annual Savings: $10,542.48
```

#### Auto-shutdown Schedules
```
Auto-shutdown Recommendations

Dev/Test Resources:
- vm-dev-server-01: Shutdown 7PM-7AM + weekends (60% savings)
- vm-test-app-01: Shutdown 6PM-8AM + weekends (65% savings)
- sql-dev-db: Auto-pause after 1hr idle (40% savings)

Estimated Savings: $1,234.56/month
```

#### Disk Optimization
```
Disk Optimization Opportunities

1. Disk: disk-web-01
   Current: Premium SSD 512GB ($73.82/month)
   Recommended: Standard SSD 512GB ($38.40/month)
   Savings: $35.42/month
   Reason: IOPS usage <500, no burst required

2. Disk: disk-archive-data
   Current: Standard SSD 1TB ($76.80/month)
   Recommended: Standard HDD 1TB ($21.33/month)
   Savings: $55.47/month
   Reason: Infrequent access pattern

Total Monthly Savings: $90.89
Annual Savings: $1,090.68
```

#### Savings Plans
```
Azure Savings Plan Analysis

Current Hourly Compute Spend: $5.43
Recommended Commitment: $4.00/hour

1-Year Savings Plan:
  Commitment: $4.00/hour
  Savings: 20%
  Monthly Cost: $2,920.00
  Current Cost: $3,909.60
  Monthly Savings: $989.60

3-Year Savings Plan:
  Commitment: $4.00/hour
  Savings: 31%
  Monthly Cost: $2,697.60
  Current Cost: $3,909.60
  Monthly Savings: $1,212.00

Recommendation: 3-Year Savings Plan
Annual Savings: $14,544.00
```

### 5. Cost Anomaly Detection

Identify unusual cost spikes:

```
Cost Anomaly Detected! 🚨

Date: November 19, 2025
Resource Group: rg-production
Anomaly: +347% vs. baseline

Details:
  Baseline (30-day avg): $234.56/day
  Today's Cost: $1,048.32/day
  Increase: +$813.76/day

Root Cause Analysis:
  1. VM Scale Set scaled to 50 instances (normal: 5)
  2. Auto-scaling triggered by load spike
  3. No scale-down policy configured

Recommendation:
  - Review auto-scaling configuration
  - Set max instance limit
  - Configure scale-down rules
```

### 6. Reporting

#### Export Formats
- **JSON**: Machine-readable data
- **CSV**: Excel-compatible spreadsheet
- **Excel**: Formatted workbook with charts
- **PDF**: Executive summary report

#### Report Types
- **Cost Allocation**: Chargeback by department/project
- **Trend Analysis**: Historical cost patterns
- **Optimization Summary**: Savings opportunities
- **Budget Status**: Budget vs. actual
- **Forecast Report**: Future cost projections

## Plugins

### CostManagementPlugin

Main plugin for all cost operations.

**Functions:**
- `get_cost_overview` - Current month cost summary
- `analyze_costs` - Multi-dimensional cost breakdown
- `forecast_costs` - Predict future spending
- `get_budget_status` - Budget tracking
- `create_budget` - Create new budget
- `get_optimization_recommendations` - Savings opportunities
- `analyze_reserved_instances` - RI coverage and recommendations
- `detect_idle_resources` - Find unused resources
- `analyze_cost_anomalies` - Detect unusual spending
- `export_cost_report` - Generate cost reports

### ConfigurationPlugin

Azure subscription management.

**Functions:**
- `set_azure_subscription` - Set active subscription
- `get_azure_subscription` - Get current subscription

## Example Prompts

### Cost Analysis

```
"Show cost breakdown for the last 30 days"
"What's the total spend on resource group rg-prod?"
"Cost by service type in East US region"
"Show me costs by tag cost-center"
"Daily cost trend for November 2025"
"Compare costs between production and staging"
```

### Budget Management

```
"Create a $10,000 monthly budget for rg-dev"
"Check budget status for subscription xyz"
"Set alert when we reach 80% of budget"
"Show all budgets and their status"
"Update budget for rg-prod to $15,000"
```

### Forecasting

```
"Forecast next month's cost"
"Estimate annual cost at current run rate"
"Project costs for next quarter"
"What will my VM costs be next week?"
```

### Optimization

```
"Find cost optimization opportunities"
"Show me idle resources I can shut down"
"Calculate savings from Reserved Instances"
"Right-size recommendations for VMs in rg-prod"
"Analyze disk optimization opportunities"
"Should I use Savings Plans or Reserved Instances?"
"Find resources wasting money"
```

### Anomaly Detection

```
"Check for cost anomalies in the last 7 days"
"Why did my costs spike yesterday?"
"Alert me if costs increase by more than 20%"
```

### Reporting

```
"Export cost report as CSV"
"Generate executive cost summary for November"
"Create chargeback report by cost center"
"Show me a cost allocation report"
```

## Key Services

| Service | Purpose |
|---------|---------|
| `CostOptimizationEngine` | Analysis and recommendations |
| `AzureCostManagementService` | Azure Cost Management API integration |
| `BudgetManagementService` | Budget tracking and alerts |
| `ForecastingService` | Cost prediction models |
| `AnomalyDetectionService` | Unusual cost spike detection |
| `ReservedInstanceAnalyzer` | RI coverage and savings analysis |

## Configuration

### appsettings.json

```json
{
  "CostManagementAgent": {
    "Temperature": 0.3,
    "MaxTokens": 4000,
    "DefaultCurrency": "USD",
    "DefaultTimeframe": "MonthToDate",
    "EnableAnomalyDetection": true,
    "EnableOptimizationRecommendations": true
  },
  "CostManagement": {
    "RefreshIntervalMinutes": 60,
    "AnomalyThresholdPercentage": 50,
    "MinimumSavingsThreshold": 100.00,
    "ForecastDays": 30
  },
  "Budgets": {
    "DefaultAlertThresholds": [50, 80, 100, 120],
    "EmailNotifications": true,
    "NotificationEmails": ["finance@company.com"]
  }
}
```

## Usage Patterns

### Pattern 1: Cost Overview

```
User: "Show cost breakdown for last 30 days"

Agent Response:
💰 Cost Overview (Last 30 Days)

Total Spend: $12,456.78
Daily Average: $415.23
Trend: +8% vs. previous 30 days

Top 5 Services:
1. Virtual Machines:   $5,234.12 (42%)
2. Storage:            $1,890.45 (15%)
3. SQL Database:       $1,567.89 (13%)
4. Networking:         $1,234.56 (10%)
5. AKS:                $982.34 (8%)

Top 5 Resource Groups:
1. rg-production:      $7,890.12 (63%)
2. rg-staging:         $2,345.67 (19%)
3. rg-development:     $1,234.56 (10%)
4. rg-test:            $895.79 (7%)

📊 Full breakdown: cost_analysis_20251119.json
```

### Pattern 2: Optimization Recommendations

```
User: "Find cost optimization opportunities"

Agent Response:
🎯 Cost Optimization Opportunities

Total Potential Savings: $6,234.56/month ($74,814.72/year)

1. Right-sizing (3 resources)
   - vm-prod-web-01: $187.20/month
   - sql-analytics: $3,945.00/month
   Savings: $4,132.20/month

2. Reserved Instances (2 services)
   - VMs (3-year): $1,111.11/month
   - SQL (3-year): $3,738.45/month
   Savings: $4,849.56/month

3. Idle Resources (3 resources)
   - vm-old-test: $187.20/month
   - storage-archive: $234.56/month
   - sql-abandoned: $456.78/month
   Savings: $878.54/month

4. Disk Optimization (2 disks)
   - disk-web-01: $35.42/month
   - disk-archive: $55.47/month
   Savings: $90.89/month

Priority Actions:
1. Purchase SQL RI (3-year) → $3,738/month savings
2. Right-size sql-analytics → $3,945/month savings
3. Delete idle resources → $879/month savings
```

### Pattern 3: Budget Tracking

```
User: "Check budget status"

Agent Response:
📊 Budget Status Summary

Active Budgets: 4

1. Production Workloads
   Limit: $10,000.00/month
   Spent: $7,890.12 (79%)
   Remaining: $2,109.88
   Forecast: $9,876.54 (99%)
   Status: ⚠️ Approaching Limit

2. Development
   Limit: $2,000.00/month
   Spent: $1,234.56 (62%)
   Remaining: $765.44
   Forecast: $1,890.12 (95%)
   Status: ✅ On Track

3. Test Environment
   Limit: $1,000.00/month
   Spent: $1,234.56 (123%)
   Remaining: -$234.56
   Forecast: $1,456.78 (146%)
   Status: 🚨 EXCEEDED

4. Q4 Total
   Limit: $50,000.00/quarter
   Spent: $32,145.67 (64%)
   Remaining: $17,854.33
   Forecast: $48,765.43 (98%)
   Status: ✅ On Track
```

## Integration with Other Agents

### → Infrastructure Agent
Infrastructure Agent provisions resources → Cost Management Agent estimates costs

### → Environment Agent
Environment Agent scales environments → Cost Management Agent tracks cost impact

### → Discovery Agent
Discovery Agent finds resources → Cost Management Agent analyzes their costs

## Troubleshooting

### Issue: Cost Data Not Available

**Symptom**: "No cost data found"

**Solutions:**
```bash
# Verify Cost Management API access
az provider show --namespace Microsoft.CostManagement

# Register if needed
az provider register --namespace Microsoft.CostManagement

# Check RBAC permissions (need Cost Management Reader)
az role assignment list --assignee <user-id> \
  | grep "Cost Management"

# Grant permission if missing
az role assignment create \
  --role "Cost Management Reader" \
  --assignee <user-id> \
  --scope "/subscriptions/{sub-id}"
```

### Issue: Budget Alerts Not Working

**Symptom**: Not receiving budget alert emails

**Solutions:**
```bash
# Verify budget alert configuration
az consumption budget list --subscription {sub-id}

# Check notification email
az consumption budget show \
  --budget-name "Production" \
  --subscription {sub-id} \
  | grep "contactEmails"

# Update notification settings
az consumption budget update \
  --budget-name "Production" \
  --notifications '{...}'
```

## Performance

| Operation | Typical Duration |
|-----------|-----------------|
| Cost overview | 2-5 seconds |
| Multi-dimensional analysis | 5-10 seconds |
| Budget status check | 1-3 seconds |
| Optimization recommendations | 10-20 seconds |
| Forecast generation | 5-15 seconds |

## Limitations

- **Data Latency**: Cost data typically 8-24 hours delayed
- **Granularity**: Hourly data available for last 3 days only
- **Historical Data**: 13 months of cost history
- **Export Size**: Large exports may timeout
- **API Throttling**: Azure Cost Management API rate limits

## References

- [Azure Cost Management](https://docs.microsoft.com/en-us/azure/cost-management-billing/)
- [Cost Management API](https://docs.microsoft.com/en-us/rest/api/cost-management/)
- [Reserved Instances](https://azure.microsoft.com/en-us/pricing/reserved-vm-instances/)
- [Azure Savings Plans](https://azure.microsoft.com/en-us/pricing/offers/savings-plan-compute/)

---

**Last Updated**: November 2025  
**Version**: 0.6.35  
**Agent Type**: `CostManagement`
