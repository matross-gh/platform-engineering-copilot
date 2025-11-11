# Azure Authentication Guide

> **Quick Start:** [Jump to 5-minute setup](#quick-start-5-minutes) | **Comprehensive Guide:** [Full documentation](#comprehensive-guide)

---

## Table of Contents

1. [Quick Start (5 Minutes)](#quick-start-5-minutes)
2. [Comprehensive Guide](#comprehensive-guide)
   - [Overview](#overview)
   - [Authentication Architecture](#authentication-architecture)
   - [Setup Instructions](#setup-instructions)
   - [User Authentication Flow](#user-authentication-flow)
   - [Subscription ID Handling](#subscription-id-handling)
   - [Troubleshooting](#troubleshooting)
   - [Security Best Practices](#security-best-practices)
3. [Configuration Reference](#configuration-reference)
4. [Testing Your Setup](#testing-your-setup)

---

## Quick Start (5 Minutes)

### For Developers (Local Development)

**Get up and running in 3 commands:**

```bash
# 1. Login to Azure Government
az cloud set --name AzureUSGovernment
az login

# 2. Set tenant ID (required for Azure Government)
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)

# 3. Run the application
cd src/Platform.Engineering.Copilot.API
dotnet run
```

**For Azure Commercial instead:**
```bash
az cloud set --name AzureCloud
az login
```

✅ **Done!** The application automatically uses your Azure CLI credentials.

### For End Users (Chat Interface)

1. **Login once:** Run `az login` on your machine
2. **Use the chat:** All Azure operations run with YOUR permissions
3. **Example queries:**
   ```
   "Show me all my Azure subscriptions"
   "List all resource groups"
   "What AKS clusters are deployed?"
   ```

### For Production Deployment

```bash
# 1. Enable Managed Identity
az webapp identity assign \
  --name your-app-name \
  --resource-group your-resource-group

# 2. Grant permissions
PRINCIPAL_ID=$(az webapp identity show \
  --name your-app-name \
  --resource-group your-resource-group \
  --query principalId -o tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Reader" \
  --scope "/subscriptions/your-subscription-id"

# 3. Set UseManagedIdentity: true in appsettings.json
```

### Quick Troubleshooting

```bash
# Not seeing subscriptions?
az login

# Authorization failed?
# Request RBAC permissions from Azure admin

# Azure CLI not found?
brew install azure-cli  # macOS
```

**Need more details?** Continue reading the [Comprehensive Guide](#comprehensive-guide) below.

---

## Comprehensive Guide

## Overview

The Platform Engineering Copilot uses **Azure DefaultAzureCredential** for secure, credential-free authentication to Azure resources. This approach eliminates the need for hardcoded credentials in configuration files and supports multiple authentication methods automatically.

### Key Benefits

- ✅ **No Hardcoded Credentials** - Never store credentials in configuration files
- ✅ **User-Level Security** - Each user uses their own Azure identity
- ✅ **Automatic Token Management** - Azure SDK handles token refresh
- ✅ **Production Ready** - Managed Identity for Azure deployments
- ✅ **Audit Trail** - All actions logged under user's identity in Azure Activity Log
- ✅ **Multi-Cloud Support** - Works with Azure Government and Azure Commercial

---

## Authentication Architecture

### Current Implementation

The system uses Azure's `DefaultAzureCredential` class, which automatically tries multiple authentication methods in the following order:

1. **Environment Variables** - Service principal credentials from environment
2. **Managed Identity** - System-assigned or user-assigned managed identity (Azure deployments)
3. **Azure CLI** - Credentials from `az login` (local development)
4. **Visual Studio** - Credentials from Visual Studio
5. **Azure PowerShell** - Credentials from Azure PowerShell

### Code Implementation

Located in `src/Platform.Engineering.Copilot.Core/Services/Azure/AzureResourceService.cs`:

```csharp
TokenCredential credential = _options.UseManagedIdentity 
    ? new DefaultAzureCredential()
    : new ChainedTokenCredential(
        new AzureCliCredential(),
        new DefaultAzureCredential()
    );

// Configure for Azure Government environment
var armClientOptions = new ArmClientOptions();
armClientOptions.Environment = ArmEnvironment.AzureGovernment;

_armClient = new ArmClient(credential, defaultSubscriptionId: null, armClientOptions);
```

### Configuration

Located in `appsettings.json`:

```json
{
  "Gateway": {
    "Azure": {
      "UseManagedIdentity": true,
      "CloudEnvironment": "AzureGovernment",
      "Enabled": true
    }
  }
}
```

**Key Configuration Options:**
- `UseManagedIdentity`: Set to `true` for production (uses DefaultAzureCredential), `false` for development (prioritizes Azure CLI)
- `CloudEnvironment`: Set to `AzureGovernment` for Azure Government or `AzurePublicCloud` for commercial Azure
- `Enabled`: Set to `true` to enable Azure resource operations

---

## Setup Instructions

### For Local Development (Azure CLI Authentication)

This is the **recommended approach** for developers working on their local machines.

#### Prerequisites
- Azure CLI installed
- Access to Azure subscription(s)
- Appropriate Azure RBAC permissions

#### Step-by-Step Setup

**1. Install Azure CLI** (if not already installed)

```bash
# macOS
brew install azure-cli

# Windows
winget install -e --id Microsoft.AzureCLI

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

**2. Login to Azure**

For **Azure Government** (default):
```bash
az cloud set --name AzureUSGovernment
az login
```

For **Azure Commercial**:
```bash
az cloud set --name AzureCloud
az login
```

**3. Set Your Tenant ID** (Required for Azure Government)

```bash
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)
```

**Make it persistent across sessions:**
```bash
# For zsh (macOS default)
echo 'export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv 2>/dev/null)' >> ~/.zshrc

# For bash
echo 'export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv 2>/dev/null)' >> ~/.bashrc
```

**4. Verify Your Login**

```bash
# List your subscriptions
az account list --output table

# Set default subscription (optional)
az account set --subscription "your-subscription-id"

# Verify current subscription
az account show
```

**5. Configure the Application**

Update `appsettings.Development.json`:
```json
{
  "Gateway": {
    "Azure": {
      "UseManagedIdentity": false,
      "CloudEnvironment": "AzureGovernment",
      "Enabled": true
    }
  }
}
```

Setting `UseManagedIdentity: false` prioritizes Azure CLI credentials during development.

**6. Run the Application**

```bash
cd src/Platform.Engineering.Copilot.API
dotnet run
```

The application will automatically use your Azure CLI credentials!

#### How It Works

When you run `az login`:
- Azure CLI stores your credentials locally (typically in `~/.azure/`)
- `AzureCliCredential` reads these credentials automatically
- Your personal Azure permissions are used for all Azure operations
- **No credentials are stored in the application or configuration files**

---

### For Production Deployment (Managed Identity)

This is the **recommended approach** for production deployments in Azure.

#### Prerequisites
- Application hosted in Azure (App Service, Container Apps, AKS, VM, etc.)
- Managed Identity enabled on the hosting resource
- Appropriate Azure RBAC role assignments

#### Step-by-Step Setup

**1. Enable Managed Identity** on your Azure resource

For **Azure App Service**:
```bash
az webapp identity assign \
  --name your-app-name \
  --resource-group your-resource-group
```

For **Azure Container Apps**:
```bash
az containerapp identity assign \
  --name your-app-name \
  --resource-group your-resource-group
```

For **Azure Kubernetes Service (AKS)**:
```bash
az aks update \
  --name your-aks-cluster \
  --resource-group your-resource-group \
  --enable-managed-identity
```

**2. Grant RBAC Permissions**

Assign the necessary roles to the managed identity:

```bash
# Get the managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name your-app-name \
  --resource-group your-resource-group \
  --query principalId -o tsv)

# Grant Reader access to subscription
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Reader" \
  --scope "/subscriptions/your-subscription-id"

# Grant Contributor access to specific resource group (if needed)
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Contributor" \
  --scope "/subscriptions/your-subscription-id/resourceGroups/your-rg"
```

**Common Roles Needed:**
- `Reader` - Read access to Azure resources
- `Contributor` - Create/modify Azure resources
- `Cost Management Reader` - Access to cost data
- `Security Reader` - Access to security center data
- `Monitoring Reader` - Access to monitoring data

**3. Configure the Application**

Update `appsettings.json` for production:
```json
{
  "Gateway": {
    "Azure": {
      "UseManagedIdentity": true,
      "CloudEnvironment": "AzureGovernment",
      "Enabled": true
    }
  }
}
```

**4. Deploy the Application**

The application will automatically use the managed identity - no additional configuration needed!

#### How It Works

When deployed to Azure:
- Azure automatically provides a managed identity token to your application
- `DefaultAzureCredential` detects it's running in Azure and uses the managed identity
- The identity's RBAC permissions determine what Azure resources can be accessed
- **No credentials are ever stored or managed by the application**

---

### For CI/CD Pipelines (Service Principal)

Use environment variables for service principal authentication in automated pipelines.

#### Setup

**1. Create a Service Principal**

```bash
az ad sp create-for-rbac \
  --name "platform-copilot-cicd" \
  --role contributor \
  --scopes /subscriptions/your-subscription-id
```

This outputs:
```json
{
  "appId": "your-app-id",
  "displayName": "platform-copilot-cicd",
  "password": "your-password",
  "tenant": "your-tenant-id"
}
```

**2. Set Environment Variables**

In your CI/CD pipeline, set:
```bash
export AZURE_CLIENT_ID="your-app-id"
export AZURE_CLIENT_SECRET="your-password"
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
```

**3. Run the Application**

`DefaultAzureCredential` will automatically detect and use these environment variables.

---

## User Authentication Flow

### How Users Authenticate

When users interact with the Platform Engineering Copilot via the chat interface:

1. **User Logs Into Their Machine**
   - Developer runs `az login` on their local machine
   - Azure CLI stores their credentials securely

2. **User Sends Query**
   - User: "Deploy an AKS cluster in subscription 00000000-0000-0000-0000-000000000000"
   - Chat interface sends query to API

3. **Multi-Agent System Processes Request**
   - OrchestratorAgent parses the query
   - Extracts subscription ID from user's message
   - Routes to appropriate agents (Infrastructure, Discovery, etc.)

4. **Azure Operations Use User's Credentials**
   - Agents call AzureResourceService
   - AzureResourceService uses `DefaultAzureCredential`
   - `DefaultAzureCredential` uses Azure CLI credentials (user's identity)
   - Azure operations run with **user's permissions**

5. **RBAC Enforces Security**
   - Azure checks if the user has permissions for the requested operation
   - If user lacks permissions, Azure returns authorization error
   - Application handles error gracefully and informs user

### Security Model

**Key Security Principles:**

1. **No Shared Credentials**
   - Each user uses their own Azure identity
   - No hardcoded credentials in configuration files
   - No credential sharing between users

2. **Principle of Least Privilege**
   - Users can only perform actions they're authorized for in Azure
   - Application doesn't elevate or reduce user permissions
   - Azure RBAC controls all access

3. **Audit Trail**
   - All Azure operations are logged under the user's identity
   - Azure Activity Log shows who did what
   - Compliance and audit requirements satisfied

4. **Token Security**
   - Credentials never leave the user's machine
   - Azure CLI manages token refresh automatically
   - Tokens stored securely by Azure SDK

---

## Subscription ID Handling

### How Subscription IDs are Determined

The system uses a flexible approach to determine which Azure subscription to use:

1. **Explicit in Query** (Highest Priority)
   - User specifies subscription ID in their message
   - Example: "in subscription 00000000-0000-0000-0000-000000000000"
   - Agents extract and use this subscription ID

2. **User's Default Subscription** (Fallback)
   - Uses the subscription set via `az account set`
   - Query: `az account show --query id -o tsv`

3. **Multi-Subscription Operations**
   - User can list all subscriptions: "Show me all my Azure subscriptions"
   - User can switch context: "Use subscription xyz for the next operations"

### Setting Your Default Subscription

```bash
# List all accessible subscriptions
az account list --output table

# Set default subscription
az account set --subscription "subscription-id-or-name"

# Verify
az account show --query name
```

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: "No subscriptions found"

**Cause:** Not logged into Azure CLI or no RBAC permissions

**Solution:**
```bash
# Login to Azure
az login

# Verify subscriptions
az account list --output table

# If no subscriptions appear, request access from your Azure admin
```

#### Issue: "The GUID for subscription is invalid"

**Cause:** Invalid or placeholder subscription ID being used

**Solution:**
- Ensure you specify a real subscription ID in your query
- Or set your default subscription: `az account set --subscription "your-id"`
- Verify with: `az account show`

#### Issue: "Authorization failed"

**Cause:** User lacks RBAC permissions for the requested operation

**Solution:**
- Request appropriate RBAC role assignment from Azure admin
- Common roles needed:
  - `Reader` - View resources
  - `Contributor` - Create/modify resources
  - `Owner` - Full control (rarely needed)

#### Issue: "Azure CLI not found" or "AzureCliCredential failed"

**Cause:** Azure CLI not installed or not in PATH

**Solution:**
```bash
# Install Azure CLI
# macOS:
brew install azure-cli

# Windows:
winget install -e --id Microsoft.AzureCLI

# Verify installation
az --version

# Login
az login
```

#### Issue: "Tenant ID required for Azure Government"

**Cause:** Azure Government requires explicit tenant ID

**Solution:**
```bash
# Set tenant ID environment variable
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)

# Make it persistent
echo 'export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv 2>/dev/null)' >> ~/.zshrc
```

#### Issue: "DefaultAzureCredential failed to retrieve a token"

**Cause:** No authentication method succeeded

**Solution:**
1. Verify Azure CLI login: `az account show`
2. Check environment variables: `echo $AZURE_TENANT_ID`
3. Ensure managed identity is enabled (if in Azure)
4. Check application logs for specific credential errors
5. Try explicit login: `az logout && az login`

### Enabling Detailed Logging

To diagnose authentication issues, enable detailed logging:

Update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Azure.Core": "Debug",
      "Azure.Identity": "Debug",
      "Platform.Engineering.Copilot.Core.Services.Azure": "Debug"
    }
  }
}
```

This will show detailed authentication attempts in the application logs.

---

## Security Best Practices

### ✅ DO

- **Use Azure CLI for local development**
  - Keeps credentials on your machine
  - Easy to refresh and rotate
  - Automatic token management

- **Use Managed Identity for production**
  - No credentials to manage
  - Automatic token rotation
  - Azure handles all security

- **Grant minimal RBAC permissions**
  - Only assign roles users actually need
  - Use resource group scopes instead of subscription
  - Regular access reviews

- **Use Azure Government for DoD workloads**
  - Compliant with FedRAMP High
  - IL4/IL5 certified regions
  - Required for DoD data

- **Monitor authentication logs**
  - Review Azure Activity Log regularly
  - Set up alerts for suspicious activity
  - Track failed authentication attempts

### ❌ DON'T

- **Don't hardcode credentials**
  - Never put credentials in appsettings.json
  - Never commit credentials to source control
  - Never share credentials between users

- **Don't use service principals for local development**
  - Use Azure CLI instead
  - Service principals should be for automation only
  - Harder to rotate and manage

- **Don't grant excessive permissions**
  - Avoid Owner role unless absolutely necessary
  - Don't grant subscription-wide access if resource group is sufficient
  - Follow principle of least privilege

- **Don't disable authentication**
  - Always require authentication
  - Never bypass RBAC checks
  - Validate all Azure operations

---

## Configuration Reference

### Environment Variables

#### Required for Azure Government (Development)
```bash
export AZURE_TENANT_ID="your-tenant-id-here"
```

#### For Service Principal (CI/CD)
```bash
export AZURE_CLIENT_ID="service-principal-app-id"
export AZURE_CLIENT_SECRET="service-principal-password"
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_SUBSCRIPTION_ID="default-subscription-id"  # Optional
```

#### Optional Environment Variables
```bash
# Override cloud environment
export AZURE_CLOUD_ENVIRONMENT="AzureGovernment"  # or "AzurePublicCloud"

# Set specific subscription
export AZURE_SUBSCRIPTION_ID="your-subscription-id"

# Configure credential timeout
export AZURE_CREDENTIAL_TIMEOUT="30"  # seconds
```

### Configuration Files

#### Development: `appsettings.Development.json`
```json
{
  "Gateway": {
    "Azure": {
      "UseManagedIdentity": false,
      "CloudEnvironment": "AzureGovernment",
      "Enabled": true
    },
    "AzureOpenAI": {
      "Endpoint": "https://your-openai.openai.azure.us/",
      "DeploymentName": "gpt-4o",
      "UseManagedIdentity": false,
      "ChatDeploymentName": "gpt-4o",
      "EmbeddingDeploymentName": "text-embedding-ada-002"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Azure.Core": "Information",
      "Azure.Identity": "Debug"
    }
  }
}
```

#### Production: `appsettings.json`
```json
{
  "Gateway": {
    "Azure": {
      "UseManagedIdentity": true,
      "CloudEnvironment": "AzureGovernment",
      "Enabled": true
    },
    "AzureOpenAI": {
      "Endpoint": "https://your-openai.openai.azure.us/",
      "DeploymentName": "gpt-4o",
      "UseManagedIdentity": true,
      "ChatDeploymentName": "gpt-4o",
      "EmbeddingDeploymentName": "text-embedding-ada-002"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Azure.Core": "Warning",
      "Azure.Identity": "Warning"
    }
  }
}
```

#### Azure Commercial (Non-Government)
```json
{
  "Gateway": {
    "Azure": {
      "UseManagedIdentity": true,
      "CloudEnvironment": "AzurePublicCloud",
      "Enabled": true
    },
    "AzureOpenAI": {
      "Endpoint": "https://your-openai.openai.azure.com/",
      "DeploymentName": "gpt-4o",
      "UseManagedIdentity": true
    }
  }
}
```

**Note:** Use `.azure.com` endpoints for commercial Azure, `.azure.us` for Azure Government.

---

## Testing Your Setup

### Quick Verification

**1. Test Azure CLI Authentication**
```bash
az account show
```
Should display your current subscription.

**2. Test Application Authentication**
```bash
cd src/Platform.Engineering.Copilot.API
dotnet run
```

Look for in the logs:
```
Azure ARM client initialized successfully for AzureGovernment
```

**3. Query via Chat**
```
"Show me all resource groups in my subscription"
```

Expected behavior:
- Multi-agent system processes query
- Discovery agent calls Azure APIs
- Returns list of resource groups
- Operations run with your Azure permissions

### Integration Testing

Use these natural language test cases:

```bash
# Test resource discovery
"What resources are deployed in my subscription?"

# Test with specific subscription
"List all AKS clusters in subscription 00000000-0000-0000-0000-000000000000"

# Test multi-subscription query
"Show me all my Azure subscriptions"

# Test deployment (if you have contributor permissions)
"Deploy a basic storage account in resource group test-rg"
```

### Success Criteria

✅ You've successfully configured authentication when:

- You can run `az account show` and see your subscription
- You can start the API without errors
- Logs show "Azure ARM client initialized successfully"
- You can send a query: "Show me all my Azure subscriptions"
- The system returns actual Azure resources
- Operations run with your Azure permissions

---

## Summary

### Key Takeaways

1. **No Credentials in Configuration** ✅
   - System uses DefaultAzureCredential
   - No hardcoded credentials needed
   - Secure by default

2. **Developer Experience** ✅
   - Just run `az login`
   - Application automatically uses your credentials
   - Works exactly like Azure CLI

3. **Production Ready** ✅
   - Uses Managed Identity in Azure
   - No credential management required
   - Automatic token rotation

4. **Security** ✅
   - User-level permissions enforced by Azure RBAC
   - Audit trail in Azure Activity Log
   - Follows cloud security best practices

5. **Multi-User Support** ✅
   - Each user authenticates separately
   - No shared credentials
   - Individual accountability

---

## Additional Resources

- [Azure Identity Documentation](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme)
- [DefaultAzureCredential Class](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [Azure CLI Authentication](https://learn.microsoft.com/en-us/cli/azure/authenticate-azure-cli)
- [Managed Identity Overview](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
- [Azure RBAC Documentation](https://learn.microsoft.com/en-us/azure/role-based-access-control/overview)
- [Azure Government Documentation](https://learn.microsoft.com/en-us/azure/azure-government/)

---

**Document Version:** 2.0  
**Last Updated:** November 2025  
**Status:** ✅ Consolidated and Production Ready

**Supersedes:** AUTHENTICATION-DOCS.md, AZURE-AUTHENTICATION.md, QUICKSTART-AUTHENTICATION.md
