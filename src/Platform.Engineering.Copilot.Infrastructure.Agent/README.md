# Infrastructure Agent

> Azure infrastructure provisioning and Infrastructure-as-Code (IaC) generation specialist

## Overview

The Infrastructure Agent is a specialized AI agent that handles Azure resource provisioning, Infrastructure-as-Code template generation (Bicep, Terraform, Kubernetes), network topology design, and compliance-aware infrastructure deployment.

**Agent Type**: `Infrastructure`  
**Icon**: 🏗️  
**Temperature**: 0.4 (balanced creativity for infrastructure design)

## Capabilities

### 1. Resource Provisioning

Create and manage Azure resources via ARM API with natural language:

- **Compute**: Virtual Machines, VM Scale Sets, AKS clusters, Container Instances
- **Storage**: Storage Accounts, Blob containers, File shares, Disks
- **Networking**: VNets, Subnets, NSGs, Load Balancers, Application Gateways
- **Databases**: SQL Database, Cosmos DB, PostgreSQL, MySQL
- **Security**: Key Vault, Managed Identities, RBAC assignments
- **Web & Apps**: App Service, Function Apps, API Management
- **Multi-cloud**: Azure (primary), AWS, GCP (secondary support)

### 2. Infrastructure-as-Code Generation

#### Bicep Templates
- Complete Bicep modules with parameters and outputs
- Azure MCP Server integration for schema validation
- Best practices enforcement (naming conventions, tagging)
- Modular template structure

#### Terraform (HCL)
- Provider configuration (Azure, AWS, GCP)
- Resource definitions with variables
- State management guidance
- Module-based architecture

#### Kubernetes YAML
- Deployments, Services, ConfigMaps, Secrets
- Ingress controllers and networking
- StatefulSets and persistent storage
- RBAC and service accounts

#### ARM Templates
- JSON-based resource definitions
- Linked templates for complex deployments
- Template validation

### 3. Network Topology Design

Advanced network architecture design and visualization:

- **VNet Design**: Hub-spoke topology, peering configurations
- **Subnetting**: CIDR calculation, subnet allocation
- **Security**: NSG rules, route tables, Azure Firewall
- **Hybrid Connectivity**: VPN Gateway, ExpressRoute
- **Visual Diagrams**: Auto-generated network topology diagrams

### 4. Predictive Scaling

AI-powered resource scaling forecasts:

- **Forecast Horizon**: Hours, days, or weeks ahead
- **Resource Types**: VM Scale Sets, App Service Plans, AKS clusters
- **Metrics Analyzed**: CPU, memory, network, custom metrics
- **Optimization**: Auto-scaling configuration tuning
- **Cost Impact**: Predict scaling costs and savings

**Example Use Cases:**
- "Forecast scaling needs for next week"
- "Optimize auto-scaling for AKS cluster"
- "Analyze historical scaling patterns"

### 5. Compliance-Aware Templates

Automatically inject security controls into IaC templates:

#### FedRAMP High (10 Controls)
- **AC-2**: Account Management (RBAC, least privilege)
- **AC-3**: Access Enforcement (NSG rules, private endpoints)
- **AU-2**: Audit Events (diagnostic settings, Log Analytics)
- **AU-12**: Audit Generation (Azure Monitor integration)
- **SC-7**: Boundary Protection (network segmentation)
- **SC-8**: Transmission Confidentiality (TLS, HTTPS)
- **SC-13**: Cryptographic Protection (encryption at rest/transit)
- **IA-2**: Identification & Authentication (managed identities)
- **IA-5**: Authenticator Management (key rotation)
- **IA-7**: Cryptographic Module Authentication (FIPS 140-2)

#### DoD IL5 (15 Controls)
- All FedRAMP High controls plus:
- **PE-3**: Physical Access Control (Azure Government regions)
- **RA-5**: Vulnerability Scanning (Defender integration)
- **CA-2**: Security Assessments (Policy assignments)
- **SI-4**: Information System Monitoring (Security Center)
- **CM-2**: Baseline Configuration (blueprints, policies)

#### PCI-DSS (8 Controls)
- **Requirement 1**: Firewall configuration
- **Requirement 2**: Default credentials removal
- **Requirement 3**: Data encryption
- **Requirement 4**: Transmission encryption
- **Requirement 8**: Access control
- **Requirement 10**: Logging and monitoring

## Plugins

### InfrastructurePlugin

Main plugin for provisioning and template generation.

**Functions:**
- `provision_infrastructure` - Natural language resource provisioning
- `generate_bicep_template` - Create Bicep IaC templates
- `generate_terraform_template` - Create Terraform HCL templates
- `generate_kubernetes_manifest` - Create Kubernetes YAML
- `design_network_topology` - Network architecture design
- `analyze_predictive_scaling` - Scaling forecasts
- `enhance_template_compliance` - Inject compliance controls
- `validate_template` - Template validation
- `deploy_template` - Deploy IaC to Azure

### ServiceWizardPlugin

Interactive DoD Service Creation Wizard with 8-step workflow.

**Workflow Steps:**
1. **Mission & Classification**: DoDAAC, Impact Level (IL2-IL6), mission sponsor
2. **Service Type**: Web app, API, database, microservice, batch processing
3. **Compute Platform**: AKS, App Service, VMs, Container Instances
4. **Data Requirements**: Storage, databases, message queues
5. **Networking**: VNet design, connectivity, DNS
6. **Security & Compliance**: STIG, FIPS 140-2, encryption
7. **Monitoring & Operations**: Logging, alerting, backup
8. **Review & Generate**: Template generation with all requirements

**Functions:**
- `wizard_start` - Begin DoD service creation
- `wizard_set_context` - Set mission parameters
- `wizard_help` - Get help on DoD terms (DoDAAC, IL, CAC, ATO, eMASS)
- `wizard_status` - Check wizard progress
- `wizard_generate` - Generate compliant templates

### ConfigurationPlugin

Azure subscription and configuration management.

**Functions:**
- `set_azure_subscription` - Set active subscription
- `get_azure_subscription` - Get current subscription
- `list_azure_subscriptions` - List available subscriptions

## Example Prompts

### Resource Provisioning

```
"Create a storage account named mydata001 in eastus with Standard_LRS"
"Deploy an AKS cluster with 3 nodes in resource group rg-prod"
"Provision a PostgreSQL database with 100GB storage"
"Create a Key Vault in West US with RBAC enabled"
```

### IaC Template Generation

```
"Generate Bicep template for a 3-tier web application"
"Create Terraform for a hub-spoke network topology"
"Generate Kubernetes manifests for a microservices app"
"Create ARM template for a complete dev environment"
```

### Network Design

```
"Design a hub-spoke network with 3 spokes"
"Create VNet topology with DMZ and private subnets"
"Design network for DoD IL5 compliance"
"Generate NSG rules for web tier security"
```

### Compliance & Security

```
"Create FedRAMP High compliant storage account template"
"Generate DoD IL5 compliant AKS deployment"
"Design PCI-DSS compliant payment processing infrastructure"
"Add NIST 800-53 controls to my Bicep template"
```

### Predictive Scaling

```
"Forecast scaling needs for next 7 days"
"Optimize auto-scaling for my VM scale set"
"Analyze scaling patterns for the last month"
"Predict cost impact of scaling to 10 nodes"
```

### DoD Service Wizard

```
"Start DoD service creation wizard"
"Create IL5 web service with FIPS 140-2"
"Help me with DoDAAC selection"
"What is an Impact Level?"
"Generate STIG-compliant AKS service"
```

## Key Services

| Service | Purpose |
|---------|---------|
| `InfrastructureProvisioningService` | Azure resource API interactions |
| `TemplateGenerationService` | IaC template creation (Bicep/Terraform) |
| `NetworkTopologyDesignService` | Network design and visualization |
| `PredictiveScalingEngine` | AI-powered scaling forecasts |
| `ComplianceAwareTemplateEnhancer` | Inject compliance controls into templates |
| `PolicyEnforcementService` | Azure Policy integration |
| `DeploymentOrchestrationService` | Multi-stage deployment orchestration |
| `DynamicTemplateGenerator` | Dynamic IaC generation |
| `AzureMcpClient` | Azure MCP Server integration |

## Configuration

### appsettings.json

The Infrastructure Agent supports the following configuration options:

```json
{
  "InfrastructureAgent": {
    "Temperature": 0.4,              // AI response temperature (0.0-2.0, default: 0.4)
    "MaxTokens": 8000,                // Maximum tokens per response (default: 8000)
    "DefaultRegion": "eastus",        // Default Azure region (default: "eastus")
    "EnableComplianceEnhancement": true,  // Enable compliance-aware templates (default: true)
    "DefaultComplianceFramework": "FedRAMPHigh",  // Default framework (default: "FedRAMPHigh")
    "EnablePredictiveScaling": true,  // Enable AI-powered scaling predictions (default: true)
    "EnableNetworkDesign": true       // Enable network topology design (default: true)
  },
  "AzureGateway": {
    "SubscriptionId": "your-subscription-id",
    "TenantId": "your-tenant-id",
    "Cloud": "AzureUSGovernment"
  },
  "AzureMcpServer": {
    "Endpoint": "http://localhost:3000",
    "Timeout": 30
  }
}
```

### Configuration Options Explained

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Temperature` | double | 0.4 | Controls AI creativity (0.0=deterministic, 2.0=creative) |
| `MaxTokens` | int | 8000 | Maximum response length for complex templates |
| `DefaultRegion` | string | "eastus" | Default Azure region for resource provisioning |
| `EnableComplianceEnhancement` | bool | true | Automatically inject compliance controls into templates |
| `DefaultComplianceFramework` | string | "FedRAMPHigh" | Default compliance framework (FedRAMPHigh, DoD IL5, NIST80053, SOC2, GDPR) |
| `EnablePredictiveScaling` | bool | true | Enable AI-powered scaling forecasts and optimization |
| `EnableNetworkDesign` | bool | true | Enable network topology design and subnet calculations |

**Note**: Setting any `Enable*` option to `false` will disable that feature and its associated functions will return an error message indicating the feature is disabled.
```

### Environment Variables

```bash
# Azure authentication
export AZURE_SUBSCRIPTION_ID="..."
export AZURE_TENANT_ID="..."
export AZURE_CLIENT_ID="..."
export AZURE_CLIENT_SECRET="..."

# For Azure Government
az cloud set --name AzureUSGovernment
az login
```

## Usage Patterns

### Pattern 1: Simple Resource Creation

```
User: "Create storage account data001 in rg-prod"

Agent Response:
✅ Created storage account 'data001'
   - Resource Group: rg-prod
   - Location: eastus
   - SKU: Standard_LRS
   - Encryption: Enabled (Microsoft-managed keys)
   - HTTPS only: Enabled
```

### Pattern 2: IaC Generation with Compliance

```
User: "Generate FedRAMP High Bicep template for web app"

Agent Response:
✅ Generated Bicep template with FedRAMP High controls:
   - AC-2: RBAC with least privilege
   - AU-2: Diagnostic settings to Log Analytics
   - SC-7: Private endpoints, no public access
   - SC-13: TLS 1.2, encryption at rest
   - IA-2: Managed identity authentication

📄 Template saved to shared memory: webapp-fedramp.bicep
```

### Pattern 3: Multi-Agent Workflow

```
User: "Deploy web app and scan for compliance"

Orchestrator Plan:
1. InfrastructureAgent: Generate and deploy template
2. ComplianceAgent: Scan deployed resources
3. DocumentAgent: Generate SSP documentation
```

## Integration with Other Agents

### → Compliance Agent
Infrastructure Agent generates templates → Compliance Agent validates and scans deployed resources

### → Environment Agent
Infrastructure Agent creates baseline template → Environment Agent clones and scales environments

### → Cost Management Agent
Infrastructure Agent provisions resources → Cost Management Agent analyzes costs and optimizes

### → Document Agent
Infrastructure Agent deploys infrastructure → Document Agent generates architecture documentation

## Troubleshooting

### Issue: Template Generation Fails

**Symptom**: "Failed to generate Bicep template"

**Solutions:**
```bash
# Check Azure MCP Server connectivity
curl http://localhost:3000/health

# Verify Azure credentials
az account show

# Check logs
docker logs platform-engineering-copilot-mcp | grep "InfrastructureAgent"
```

### Issue: Deployment Fails

**Symptom**: "Deployment to Azure failed"

**Solutions:**
```bash
# Verify subscription access
az account set --subscription "your-sub-id"
az provider list --query "[?registrationState=='Registered']"

# Check resource provider registration
az provider register --namespace Microsoft.Compute
az provider register --namespace Microsoft.Storage

# Validate template syntax
az deployment group validate \
  --resource-group rg-test \
  --template-file template.bicep
```

### Issue: Compliance Enhancement Not Working

**Symptom**: Templates don't include compliance controls

**Solutions:**
```json
// Verify configuration
{
  "InfrastructureAgent": {
    "EnableComplianceEnhancement": true,  // Must be true
    "DefaultComplianceFramework": "FedRAMPHigh"
  }
}
```

## Development

### Adding New Resource Types

```csharp
// In InfrastructurePlugin.cs
[KernelFunction("provision_custom_resource")]
[Description("Provision a custom Azure resource")]
public async Task<string> ProvisionCustomResource(
    [Description("Resource configuration")] string config)
{
    // Implementation
}
```

### Adding Compliance Controls

```csharp
// In ComplianceAwareTemplateEnhancer.cs
public Dictionary<string, ComplianceControl> CustomControls = new()
{
    ["CUSTOM-1"] = new ComplianceControl
    {
        ControlId = "CUSTOM-1",
        Title = "Custom Control",
        Implementation = "// Bicep code"
    }
};
```

## Performance

| Operation | Typical Duration |
|-----------|-----------------|
| Simple resource creation | 2-5 seconds |
| Bicep template generation | 3-8 seconds |
| Complex network design | 10-20 seconds |
| Predictive scaling analysis | 15-30 seconds |
| Compliance enhancement | 5-10 seconds |

## Limitations

- **Resource Quotas**: Subject to Azure subscription quotas
- **Region Availability**: Some resources not available in all regions
- **Multi-cloud**: AWS/GCP support is basic compared to Azure
- **Template Complexity**: Very large templates (>10,000 lines) may timeout

## References

- [Azure Resource Manager API](https://docs.microsoft.com/en-us/rest/api/resources/)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [FedRAMP Control Baselines](https://www.fedramp.gov/baselines/)
- [DoD Cloud Computing SRG](https://dl.dod.cyber.mil/wp-content/uploads/cloud/pdf/Cloud_Computing_SRG_v1r3.pdf)

---

**Last Updated**: November 2025  
**Version**: 0.6.35  
**Agent Type**: `Infrastructure`
