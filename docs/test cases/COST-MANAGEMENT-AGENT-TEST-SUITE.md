# Cost Management Agent Test Suite

**Last Updated:** November 13, 2025  
**Agent:** CostManagement  
**Plugin Functions:** 4 total  
**Purpose:** Comprehensive testing of all Cost Management Agent capabilities

## 📋 Overview

The Cost Management Agent handles Azure cost analysis, optimization recommendations, budget monitoring, forecasting, and cost reporting with Azure Advisor integration.

**Key Capabilities:**
- **Cost Analysis**: Spending breakdowns by service, resource group, location
- **Optimization**: AI-powered cost savings recommendations
- **Budget Management**: Budget creation, monitoring, alert configuration
- **Forecasting**: Predictive cost forecasting
- **Reporting**: Cost dashboards, trend analysis, exports

## 🎯 Quick Test Commands

```bash
# Test cost analysis
curl -X POST http://localhost:5100/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Analyze costs for subscription 00000000-0000-0000-0000-000000000000"}' | jq .
```

## 🧪 Test Cases by Function Category

### 1️⃣ Cost Analysis & Dashboard

#### Test COST-1.1: General Cost Query
```
What did I spend on Azure last month in subscription 00000000-0000-0000-0000-000000000000?
```
**Expected Function:** `process_cost_management_query`  
**Expected Output:**
- ✅ Total spend amount
- ✅ Cost breakdown by service (AKS: $X, SQL: $Y, Storage: $Z)
- ✅ Month-over-month comparison
- ✅ Top 5 most expensive resources
- ✅ Cost trends (increasing/decreasing/stable)

**Validation:**
- ✅ Agent: CostManagement ONLY
- ✅ Intent: "cost"
- ⏱️ Time: 10-20 seconds
- ✅ Accurate spending data

---

#### Test COST-1.2: Cost Breakdown by Service
```
Show me Azure spending broken down by service for the last 90 days
```
**Expected Function:** `process_cost_management_query`  
**Expected Output:**
- ✅ Service-by-service breakdown
- ✅ Percentage of total for each
- ✅ Trend analysis per service
- ✅ Cost anomalies flagged

**Validation:**
- ✅ 90-day time range
- ✅ All services included
- ✅ Visual breakdown (if supported)

---

#### Test COST-1.3: Cost Breakdown by Resource Group
```
Analyze costs by resource group for subscription 00000000-0000-0000-0000-000000000000
```
**Expected Function:** `process_cost_management_query`  
**Expected Output:**
- ✅ Resource group breakdown
- ✅ Top spending groups
- ✅ Idle resource groups identified
- ✅ Optimization opportunities

**Validation:**
- ✅ All resource groups shown
- ✅ Cost attribution accurate

---

#### Test COST-1.4: Cost Breakdown by Location
```
Show me spending by Azure region
```
**Expected Function:** `process_cost_management_query`  
**Expected Output:**
- ✅ Region-by-region breakdown
- ✅ Most expensive regions
- ✅ Regional pricing differences
- ✅ Consolidation opportunities

**Validation:**
- ✅ All regions included
- ✅ Geographic distribution shown

---

### 2️⃣ Cost Optimization

#### Test COST-2.1: Find Savings Opportunities
```
Find cost savings opportunities in subscription 00000000-0000-0000-0000-000000000000
```
**Expected Function:** `get_cost_optimization_recommendations`  
**Expected Output:**
- ✅ Top 10 recommendations ranked by savings
- ✅ Estimated monthly savings: $X | Annual: $Y
- ✅ Azure Advisor recommendations
- ✅ Quick wins (low effort, high impact)
- ✅ Medium-term optimizations
- ✅ Long-term strategies
- ✅ Implementation guidance

**Validation:**
- ✅ Realistic savings estimates
- ✅ Actionable recommendations
- ✅ Prioritized by impact/effort
- ⏱️ Time: 20-40 seconds

---

#### Test COST-2.2: Optimize Compute Costs
```
How can I reduce my compute costs?
```
**Expected Function:** `get_cost_optimization_recommendations`  
**Expected Output:**
- ✅ VM rightsizing recommendations
- ✅ Reserved instance opportunities
- ✅ Spot instance suggestions
- ✅ Idle VM identification
- ✅ Auto-shutdown recommendations

**Validation:**
- ✅ Compute-specific focus
- ✅ Multiple optimization strategies

---

#### Test COST-2.3: Optimize Storage Costs
```
Find storage cost savings opportunities
```
**Expected Function:** `get_cost_optimization_recommendations`  
**Expected Output:**
- ✅ Tiering recommendations (Hot → Cool → Archive)
- ✅ Unused disk identification
- ✅ Snapshot cleanup opportunities
- ✅ Data lifecycle management
- ✅ Redundancy optimization

**Validation:**
- ✅ Storage-specific recommendations
- ✅ Lifecycle policy suggestions

---

#### Test COST-2.4: Optimize Database Costs
```
How can I reduce my database spending?
```
**Expected Function:** `get_cost_optimization_recommendations`  
**Expected Output:**
- ✅ Database SKU rightsizing
- ✅ Reserved capacity options
- ✅ Serverless recommendations
- ✅ Geo-replication optimization
- ✅ Backup retention tuning

**Validation:**
- ✅ Database-specific optimization
- ✅ Performance impact considered

---

### 3️⃣ Budget Management

#### Test COST-3.1: Get Budget Recommendations
```
What budget should I set for my development environment?
```
**Expected Function:** `get_budget_recommendations`  
**Expected Output:**
- ✅ Suggested monthly budget (current avg + 10% buffer)
- ✅ Alert thresholds:
  - 50%: Informational
  - 75%: Warning
  - 90%: Critical
  - 100%: Budget exceeded
- ✅ Automation scripts (Azure CLI, PowerShell)
- ✅ Budget breakdown by service

**Validation:**
- ✅ Realistic budget amounts
- ✅ Threshold recommendations sensible
- ✅ Script examples provided
- ⏱️ Time: 10-20 seconds

---

#### Test COST-3.2: Create Budget with Alerts
```
Create a $10,000/month budget for subscription 00000000-0000-0000-0000-000000000000 with alerts at 75% and 100%
```
**Expected Function:** `process_cost_management_query` (budget intent)  
**Expected Output:**
- ✅ Budget created
- ✅ Alert thresholds configured
- ✅ Notification recipients set
- ✅ Budget summary shown

**Validation:**
- ✅ Budget exists in Azure
- ✅ Alerts trigger correctly
- ⚠️ **WARNING:** Creates actual Azure budget

---

#### Test COST-3.3: Monitor Budget Status
```
Show me my current budget status
```
**Expected Function:** `process_cost_management_query` (budget intent)  
**Expected Output:**
- ✅ Current spend vs. budget
- ✅ Percentage used
- ✅ Remaining budget
- ✅ Forecast to end of period
- ✅ Alert history

**Validation:**
- ✅ Accurate budget tracking
- ✅ Forecast realistic

---

### 4️⃣ Forecasting & Reporting

#### Test COST-4.1: Forecast Next Month Spending
```
Forecast my spending for next month
```
**Expected Function:** `process_cost_management_query` (forecast intent)  
**Expected Output:**
- ✅ Predicted spend amount
- ✅ Confidence interval
- ✅ Factors influencing forecast
- ✅ Historical trend analysis
- ✅ Recommendations for cost control

**Validation:**
- ✅ Realistic forecast
- ✅ Based on historical data
- ⏱️ Time: 15-25 seconds

---

#### Test COST-4.2: Export Cost Report
```
Export a cost summary report for last quarter
```
**Expected Function:** `process_cost_management_query` (export intent)  
**Expected Output:**
- ✅ CSV/Excel report generated
- ✅ Comprehensive cost data
- ✅ Breakdown by service, resource group
- ✅ Trend charts (if supported)
- ✅ Download link or file path

**Validation:**
- ✅ Report format suitable for stakeholders
- ✅ All requested data included

---

#### Test COST-4.3: Cost Trend Analysis
```
Show me cost trends for the last 6 months
```
**Expected Function:** `process_cost_management_query`  
**Expected Output:**
- ✅ Monthly trend chart
- ✅ Trend direction (increasing/decreasing)
- ✅ Anomaly detection
- ✅ Seasonal patterns identified
- ✅ Predictions for next 3 months

**Validation:**
- ✅ 6-month historical data
- ✅ Trends clearly visualized

---

### 5️⃣ Documentation Search

#### Test COST-5.1: Search Cost Documentation
```
How do I set up cost alerts in Azure?
```
**Expected Function:** `search_cost_docs`  
**Expected Output:**
- ✅ Relevant Azure documentation
- ✅ Step-by-step guide
- ✅ Configuration examples
- ✅ Best practices
- ✅ Links to official docs

**Validation:**
- ✅ Accurate documentation
- ✅ Azure-specific guidance

---

## 🔄 Multi-Turn Conversation Tests

### Test COST-6.1: Analysis → Optimization Workflow
```
Turn 1: "Analyze my Azure costs"
Turn 2: (Agent shows spending)
Turn 3: "How can I reduce this?"
Turn 4: (Agent shows optimization recommendations)
Turn 5: "Implement the top 3 recommendations"
```
**Expected Behavior:**
- Progressive cost reduction workflow
- Context maintained (subscription not re-asked)
- Actionable steps provided

**Validation:**
- ✅ Context preserved across turns
- ✅ Logical workflow progression

---

## 🎯 Edge Cases & Error Handling

### Test COST-7.1: No Cost Data Available
```
Analyze costs for a brand new subscription with no resources
```
**Expected:** Graceful message, setup guidance

---

### Test COST-7.2: Invalid Time Range
```
Show me costs for yesterday 3 years ago
```
**Expected:** Clarification request or reasonable default

---

## 🚨 Anti-Patterns (Should NOT Work This Way)

### ❌ Test COST-8.1: Compliance Scan to Cost Agent
```
Check NIST compliance for my subscription
```
**Expected:** Routes to **Compliance Agent**, NOT Cost Management

---

## 📊 Validation Checklist

- [ ] `agentType: "CostManagement"` in plan
- [ ] Intent: "cost"
- [ ] Accurate cost data from Azure
- [ ] Realistic optimization recommendations
- [ ] Budget operations work correctly
- ⏱️ Analysis: 10-20 seconds
- ⏱️ Optimization: 20-40 seconds

---

## 📖 Related Documentation

- **Azure Cost Management:** [Azure Cost Management Docs](https://learn.microsoft.com/en-us/azure/cost-management-billing/)
- **Azure Advisor:** [Azure Advisor Docs](https://learn.microsoft.com/en-us/azure/advisor/)

---

**Last Updated:** November 13, 2025  
**Test Coverage:** 4 functions, 20+ test cases  
**Status:** Ready for comprehensive testing
