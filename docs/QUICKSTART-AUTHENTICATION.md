# Quick Start: User Authentication Guide

## For Developers (Local Development)

### 1. Install Azure CLI
```bash
# macOS
brew install azure-cli

# Windows
winget install -e --id Microsoft.AzureCLI

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### 2. Login to Azure Government
```bash
az cloud set --name AzureUSGovernment
az login
```

**For Azure Commercial instead:**
```bash
az cloud set --name AzureCloud
az login
```

### 3. Set Your Tenant ID
```bash
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)
```

**Add to your shell profile for persistence:**
```bash
# For zsh (macOS default)
echo 'export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv 2>/dev/null)' >> ~/.zshrc

# For bash
echo 'export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv 2>/dev/null)' >> ~/.bashrc
```

### 4. Run the Application
```bash
cd src/Platform.Engineering.Copilot.API
dotnet run
```

**That's it!** The application will automatically use your Azure CLI credentials.

---

## For End Users (Using the Chat Interface)

### Prerequisites
- Azure CLI installed on your machine
- Logged into Azure (`az login`)
- Access to Azure subscription(s) you want to manage

### How It Works

1. **You authenticate once:** Run `az login` on your machine
2. **Start the chat application:** The app uses YOUR Azure credentials automatically
3. **Send queries:** All Azure operations run with YOUR permissions
4. **RBAC enforces security:** You can only do what your Azure role allows

### Example Queries

```
"Show me all my Azure subscriptions"

"List all resource groups in subscription 453c2549-4cc5-464f-ba66-acad920823e8"

"What AKS clusters are deployed in my subscription?"

"Deploy a storage account in resource group test-rg"

"Show me the cost breakdown for last month"
```

### Setting Your Default Subscription

```bash
# List all your subscriptions
az account list --output table

# Set default subscription
az account set --subscription "subscription-id-or-name"

# Verify
az account show --query name
```

Now queries without explicit subscription IDs will use this default.

---

## For Production Deployments

### Azure App Service / Container Apps

1. **Enable Managed Identity:**
   ```bash
   az webapp identity assign \
     --name your-app-name \
     --resource-group your-resource-group
   ```

2. **Grant Permissions:**
   ```bash
   # Get the principal ID
   PRINCIPAL_ID=$(az webapp identity show \
     --name your-app-name \
     --resource-group your-resource-group \
     --query principalId -o tsv)
   
   # Grant Reader role to subscription
   az role assignment create \
     --assignee $PRINCIPAL_ID \
     --role "Reader" \
     --scope "/subscriptions/your-subscription-id"
   ```

3. **Update Configuration:**
   Ensure `appsettings.json` has:
   ```json
   {
     "Gateway": {
       "Azure": {
         "UseManagedIdentity": true
       }
     }
   }
   ```

4. **Deploy:** The application will automatically use the managed identity!

---

## Troubleshooting

### "No subscriptions found"
```bash
# Re-login to Azure
az logout
az login

# Verify subscriptions
az account list --output table
```

### "Authorization failed"
You lack permissions in Azure. Contact your Azure admin to grant appropriate roles:
- `Reader` - View resources
- `Contributor` - Create/modify resources
- `Cost Management Reader` - View costs

### "Azure CLI not found"
Install Azure CLI:
```bash
# macOS
brew install azure-cli

# Verify
az --version
```

### "Tenant ID required"
```bash
# For Azure Government
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)
```

---

## Configuration Quick Reference

### Development (`appsettings.Development.json`)
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

### Production (`appsettings.json`)
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

### Commercial Azure
Change `"CloudEnvironment": "AzurePublicCloud"`

---

## Security Notes

✅ **What This Approach Provides:**
- No hardcoded credentials in configuration files
- Each user uses their own Azure identity
- Azure RBAC enforces all permissions
- Full audit trail in Azure Activity Log
- Automatic credential rotation

❌ **What NOT to Do:**
- Don't hardcode credentials in appsettings.json
- Don't share credentials between users
- Don't commit credentials to source control
- Don't disable Azure authentication

---

## Need More Details?

See the comprehensive [AZURE-AUTHENTICATION.md](./AZURE-AUTHENTICATION.md) guide for:
- Detailed architecture explanation
- Advanced scenarios
- Production deployment guides
- CI/CD pipeline setup
- Security best practices
- Complete troubleshooting guide

---

**Quick Test:**
```bash
# 1. Verify you're logged in
az account show

# 2. Start the API
cd src/Platform.Engineering.Copilot.API
dotnet run

# 3. Send a test query
"Show me all my Azure subscriptions"
```

✅ **You're ready to use the Platform Engineering Copilot!**
