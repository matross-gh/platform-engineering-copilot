# Workspace Creation Guide - VS Code Extension

> **Save infrastructure templates directly to your VS Code workspace with one click!**

This guide explains how to use the new workspace creation feature in the Platform Engineering Copilot extension for GitHub Copilot.

---

## 🎯 Overview

When you ask Platform Copilot to generate infrastructure templates (Bicep, Terraform, Kubernetes), the extension automatically:
1. Detects templates in the response
2. Displays "Save to Workspace" buttons
3. Creates properly organized project structures
4. Generates README files with deployment instructions
5. Opens files in your editor

**No more copy-pasting templates!** 🎉

---

## 🚀 Quick Start

### Basic Workflow

1. **Open GitHub Copilot Chat** in VS Code (`Cmd+Shift+I` or `Ctrl+Shift+I`)

2. **Ask for a template:**
   ```
   @platform Create a Bicep template for an Azure Storage Account
   ```

3. **Review the response** - Template appears with syntax highlighting

4. **Click the button:**
   - 📁 **"Create Project in Workspace"** - Creates full project structure
   - 💾 **"Save Single File"** - Saves individual file

5. **Enter project name** when prompted (e.g., `storage-infrastructure`)

6. **Files are created** with proper organization and README

7. **Main file opens** automatically in editor

---

## 📁 Supported Template Types

### Bicep Templates

**Detected by:** ` ```bicep ` code blocks

**Auto-generated structure:**
```
my-bicep-project/
├── main.bicep                    # Main template
├── main.parameters.json          # Parameter file
├── modules/                      # Modules subfolder
│   ├── network.bicep
│   └── storage.bicep
└── README.md                     # Deployment guide
```

**README includes:**
- `az deployment group create` commands
- Parameter descriptions
- Prerequisites (Azure CLI, permissions)
- Resource naming conventions

**Example request:**
```
@platform Create a Bicep template for:
- Virtual Network with 3 subnets
- Storage Account with private endpoint
- Network Security Groups
```

---

### Terraform Templates

**Detected by:** ` ```terraform ` or ` ```hcl ` code blocks

**Auto-generated structure:**
```
my-terraform-project/
├── main.tf                       # Main configuration
├── variables.tf                  # Input variables
├── outputs.tf                    # Output values
├── providers.tf                  # Provider config
├── versions.tf                   # Version constraints
├── .gitignore                    # Ignores state files
├── modules/                      # Modules subfolder
│   └── network/
│       ├── main.tf
│       ├── variables.tf
│       └── outputs.tf
└── README.md                     # Terraform guide
```

**README includes:**
- `terraform init`, `plan`, `apply` workflow
- Variable descriptions
- Output values
- State management (backend configuration)
- Provider authentication

**Example request:**
```
@platform Generate Terraform for:
- AKS cluster with 3 node pools
- Azure Container Registry
- Log Analytics workspace
- All using modules
```

---

### Kubernetes Manifests

**Detected by:** ` ```yaml ` code blocks with `apiVersion:` and `kind:`

**Auto-generated structure:**
```
my-k8s-project/
├── manifests/                    # Organized by type
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── configmap.yaml
│   ├── secret.yaml
│   └── ingress.yaml
└── README.md                     # kubectl guide
```

**README includes:**
- `kubectl apply -f` commands
- Resource descriptions
- Namespace setup
- Service access instructions

**Example request:**
```
@platform Create Kubernetes manifests for a 3-tier web app:
- Frontend deployment and service
- Backend API deployment and service
- PostgreSQL StatefulSet
- Ingress with TLS
```

---

### ARM Templates (JSON)

**Detected by:** ` ```json ` code blocks with `"type": "Microsoft.*"`

**Auto-generated structure:**
```
my-arm-project/
├── azuredeploy.json              # Main template
├── azuredeploy.parameters.json   # Parameters
└── README.md                     # Deployment guide
```

**README includes:**
- Azure Portal deployment steps
- PowerShell deployment commands
- Parameter explanations

---

## 🎨 Template Detection Logic

The extension scans chat responses for code blocks and identifies templates:

| Template Type | Detection Pattern | File Extension |
|--------------|------------------|----------------|
| Bicep | ` ```bicep ` | `.bicep` |
| Terraform | ` ```terraform ` or ` ```hcl ` | `.tf` |
| Kubernetes | ` ```yaml ` with `apiVersion:` | `.yaml` |
| ARM | ` ```json ` with `"type": "Microsoft.` | `.json` |

### Multiple Files Detection

**Bicep:**
- Main files: `main.bicep`, `main.parameters.json`
- Modules: Any `.bicep` file with "module" in name → `modules/` folder

**Terraform:**
- Detected by content:
  - `variable "` → `variables.tf`
  - `output "` → `outputs.tf`
  - `provider "` → `providers.tf`
  - `terraform {` → `versions.tf`
  - Default: `main.tf`

**Kubernetes:**
- Detected by `kind:` field:
  - `kind: Deployment` → `deployment.yaml`
  - `kind: Service` → `service.yaml`
  - `kind: ConfigMap` → `configmap.yaml`
  - etc.

---

## 💡 Advanced Usage

### Multi-File Projects

When Platform Copilot returns multiple templates, all are organized automatically:

**Request:**
```
@platform Create complete Bicep infrastructure with:
1. Main template for resource group and VNet
2. Storage module
3. Database module
4. Parameter file
```

**Response includes 4 code blocks:**
- ` ```bicep ` (main.bicep)
- ` ```bicep ` (modules/storage.bicep)
- ` ```bicep ` (modules/database.bicep)
- ` ```json ` (main.parameters.json)

**Click "Create Project"** → All organized correctly:
```
infrastructure/
├── main.bicep
├── main.parameters.json
├── modules/
│   ├── storage.bicep
│   └── database.bicep
└── README.md
```

### Workspace Selection

**Scenario 1: No workspace open**
- Extension shows folder picker dialog
- Select where to create project
- Project created in selected location

**Scenario 2: Single workspace**
- Project created in workspace root
- Or in subfolder if "Create in new folder" is enabled

**Scenario 3: Multi-root workspace**
- Quick pick shows all workspace folders
- Select target workspace
- Project created there

### File Overwrite Protection

If a file already exists:
1. Extension shows warning dialog
2. Options: "Overwrite" or "Cancel"
3. If cancelled, operation stops
4. No data loss without confirmation

---

## ⚙️ Commands

Access via Command Palette (`Cmd+Shift+P` / `Ctrl+Shift+P`):

### `Platform Copilot: Create Workspace`
- **Trigger:** Clicking "Create Project in Workspace" button
- **Input:** Map of filename → content
- **Prompts for:**
  - Project name (validates: letters, numbers, hyphens, underscores only)
  - Folder location (if no workspace open)
- **Creates:**
  - All template files
  - Organized folder structure (modules/, manifests/)
  - README.md with deployment instructions
  - .gitignore (for Terraform)
- **Opens:** Main template file in editor

### `Platform Copilot: Save Template`
- **Trigger:** Clicking "Save Single File" button
- **Input:** Map of filename → content
- **Prompts for:**
  - File selection (if multiple templates)
  - Save location
- **Creates:** Single file
- **Opens:** Created file in editor

---

## 🛠️ Customization

### Project Naming Validation

Project names must match: `^[a-zA-Z0-9_-]+$`

**Valid:**
- `storage-infrastructure`
- `aks_cluster_dev`
- `my-app-2024`

**Invalid:**
- `my project` (spaces)
- `app@prod` (special chars)
- `123` (starts with number is OK)

### README Templates

READMEs are auto-generated based on template type. They include:

**All Types:**
- Title with project name
- Description of infrastructure
- Prerequisites
- Deployment instructions
- Attribution footer

**Bicep-specific:**
- `az deployment group create` commands
- `az deployment sub create` for subscriptions
- Parameter file usage
- Resource group creation

**Terraform-specific:**
- `terraform init` setup
- `terraform plan` validation
- `terraform apply` deployment
- State backend configuration
- Variable file usage

**Kubernetes-specific:**
- `kubectl apply -f` commands
- Namespace creation
- Resource verification (`kubectl get`)
- Service access instructions

---

## 🐛 Troubleshooting

### Issue: Buttons Not Appearing

**Symptoms:** Generate template but no "Create Project" button shows

**Causes & Fixes:**
1. **Code block language not recognized**
   - ✅ Use ` ```bicep `, not ` ```bicep-lang `
   - ✅ Use ` ```terraform ` or ` ```hcl `, not ` ```tf `

2. **Template content invalid**
   - ✅ Ensure Kubernetes has `apiVersion:` and `kind:`
   - ✅ Ensure ARM JSON has `"type": "Microsoft.*"`

3. **Extension not activated**
   - ✅ Check VS Code status bar for "Platform Copilot"
   - ✅ Run "Platform Copilot: Check Platform API Health"

### Issue: Project Creation Failed

**Symptoms:** Button clicked but error message appears

**Causes & Fixes:**
1. **Workspace folder not writable**
   - ✅ Check folder permissions
   - ✅ Try different location

2. **Workspace folder doesn't exist**
   - ✅ Create workspace folder first
   - ✅ Or let extension prompt for location

3. **File path too long (Windows)**
   - ✅ Use shorter project names
   - ✅ Create project closer to drive root

### Issue: Files Created But Not Visible

**Symptoms:** Success message shown but files not in Explorer

**Causes & Fixes:**
1. **Wrong workspace folder selected**
   - ✅ Check active workspace in Explorer
   - ✅ Look in all workspace folders (multi-root)

2. **Explorer needs refresh**
   - ✅ Click Refresh button in Explorer
   - ✅ Close and reopen folder

3. **Files created outside workspace**
   - ✅ Use File → Open Folder to navigate
   - ✅ Search workspace for project name

### Issue: README Not Generated

**Symptoms:** Templates created but no README.md

**Causes & Fixes:**
1. **Generic workspace creation used**
   - ✅ Ensure template type detected correctly
   - ✅ Check "intentType" in debug logs

2. **README already exists**
   - ✅ Extension doesn't overwrite existing READMEs
   - ✅ Manually rename old README first

---

## 📊 Examples

### Example 1: Simple Bicep Storage Account

**Chat:**
```
@platform Create a Bicep template for a storage account with:
- Name: mystorageacct001
- SKU: Standard_LRS
- Location: East US
```

**Generated Files:**
```
storage-infrastructure/
├── main.bicep
├── main.parameters.json
└── README.md
```

**main.bicep:**
```bicep
param storageAccountName string = 'mystorageacct001'
param location string = 'eastus'
param sku string = 'Standard_LRS'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: sku
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

output storageAccountId string = storageAccount.id
```

### Example 2: Terraform Multi-Module

**Chat:**
```
@platform Generate Terraform with modules for:
1. Network module (VNet, subnets, NSGs)
2. Compute module (VM scale set)
3. Main config that uses both
```

**Generated Files:**
```
terraform-infrastructure/
├── main.tf
├── variables.tf
├── outputs.tf
├── providers.tf
├── .gitignore
├── modules/
│   ├── network/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── outputs.tf
│   └── compute/
│       ├── main.tf
│       ├── variables.tf
│       └── outputs.tf
└── README.md
```

### Example 3: Kubernetes Full Stack

**Chat:**
```
@platform Create Kubernetes manifests for:
- Frontend (Nginx, 3 replicas)
- Backend API (Node.js, 2 replicas)
- Redis cache
- ConfigMaps for config
- Secrets for API keys
- Ingress with TLS
```

**Generated Files:**
```
k8s-fullstack/
├── manifests/
│   ├── frontend-deployment.yaml
│   ├── frontend-service.yaml
│   ├── backend-deployment.yaml
│   ├── backend-service.yaml
│   ├── redis-statefulset.yaml
│   ├── redis-service.yaml
│   ├── configmap.yaml
│   ├── secret.yaml
│   └── ingress.yaml
└── README.md
```

---

## 🔗 Related Documentation

- [VS Code Extension README](../extensions/platform-engineering-copilot-github/README.md)
- [GitHub Copilot Integration Guide](./GITHUB-COPILOT-INTEGRATION.md)
- [Infrastructure Plugin Documentation](./ARCHITECTURE.md#infrastructure-agent)
- [Bicep Template Best Practices](https://learn.microsoft.com/azure/azure-resource-manager/bicep/best-practices)
- [Terraform Module Structure](https://developer.hashicorp.com/terraform/language/modules/develop/structure)

---

## 🎓 Best Practices

### 1. Use Descriptive Project Names
✅ **Good:** `prod-aks-cluster`, `storage-with-private-endpoint`  
❌ **Bad:** `project1`, `test`, `abc`

### 2. Review Generated Code Before Deployment
- Check parameter values
- Verify resource names comply with naming conventions
- Update tags for your organization
- Review security settings (firewall rules, access controls)

### 3. Version Control Immediately
```bash
cd my-infrastructure
git init
git add .
git commit -m "Initial infrastructure template from Platform Copilot"
```

### 4. Customize READMEs
- Add environment-specific notes
- Document any manual steps required
- Include team contact information
- Add troubleshooting tips

### 5. Use Parameter Files
- Don't hardcode sensitive values
- Create separate parameter files per environment:
  - `dev.parameters.json`
  - `test.parameters.json`
  - `prod.parameters.json`

### 6. Organize Complex Infrastructures
For large projects with many resources:
```
infrastructure/
├── bicep/
│   ├── networking/
│   ├── compute/
│   ├── storage/
│   └── security/
├── terraform/
│   └── modules/
├── scripts/
└── docs/
```

Use workspace creation multiple times for different components.

---

## 🚀 What's Next?

After creating your workspace:

1. **Review and customize** templates for your environment
2. **Test deployment** in dev/sandbox subscription first
3. **Set up CI/CD** pipelines for automated deployments
4. **Add monitoring** and alerting for deployed resources
5. **Document** any environment-specific requirements
6. **Share** templates with your team via Git

---

## 📝 Feedback & Support

Found a bug or have a suggestion for workspace creation?

- **GitHub Issues:** [platform-engineering-copilot/issues](https://github.com/azurenoops/platform-engineering-copilot/issues)
- **Feature Requests:** Tag with `enhancement` and `workspace-creation`
- **Documentation:** This guide lives in `docs/WORKSPACE-CREATION-GUIDE.md`

---

**Happy Infrastructure Coding!** 🎉
