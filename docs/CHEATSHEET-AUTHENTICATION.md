# Azure Authentication - Developer Cheat Sheet

## 🚀 Quick Setup (30 seconds)

```bash
# 1. Login to Azure Government
az cloud set --name AzureUSGovernment && az login

# 2. Set tenant ID
export AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)

# 3. Run the app
cd src/Platform.Engineering.Copilot.API && dotnet run
```

**For Azure Commercial:** Use `az cloud set --name AzureCloud`

---

## 📋 Daily Commands

```bash
# Check if logged in
az account show

# Re-login if token expired
az logout && az login

# List subscriptions
az account list --output table

# Set default subscription
az account set --subscription "subscription-id"
```

---

## 🔧 Configuration

### Development (`appsettings.Development.json`)
```json
{
  "Gateway": {
    "Azure": {
      "UseManagedIdentity": false,
      "CloudEnvironment": "AzureGovernment"
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
      "CloudEnvironment": "AzureGovernment"
    }
  }
}
```

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| "No subscriptions found" | `az logout && az login` |
| "Tenant ID required" | `export AZURE_TENANT_ID=$(az account show -q tenantId -o tsv)` |
| "Azure CLI not found" | `brew install azure-cli` (macOS) |
| "Authorization failed" | Request RBAC permissions from Azure admin |
| Token expired | `az account get-access-token` to refresh |

---

## 🔐 Security Model

**How it works:**
1. You: `az login` (one time)
2. App: Uses your credentials automatically via `DefaultAzureCredential`
3. Azure: Enforces your RBAC permissions
4. Result: No credentials in config files!

**Your credentials are:**
- ✅ Stored securely by Azure CLI on your machine
- ✅ Automatically refreshed
- ✅ Never in source control
- ✅ Only accessible to you

---

## 📚 Full Documentation

- **Quick Start:** [QUICKSTART-AUTHENTICATION.md](./QUICKSTART-AUTHENTICATION.md)
- **Complete Guide:** [AZURE-AUTHENTICATION.md](./AZURE-AUTHENTICATION.md)
- **Summary:** [AUTHENTICATION-DOCS.md](./AUTHENTICATION-DOCS.md)

---

## 💡 Test Queries

After setup, try these in the chat:

```
"Show me all my Azure subscriptions"
"List all resource groups"
"What AKS clusters are deployed?"
"Deploy a storage account in resource group test-rg"
```

---

## 🏭 Production Setup

```bash
# 1. Enable Managed Identity
az webapp identity assign --name your-app --resource-group your-rg

# 2. Grant permissions
PRINCIPAL_ID=$(az webapp identity show --name your-app --resource-group your-rg --query principalId -o tsv)
az role assignment create --assignee $PRINCIPAL_ID --role "Reader" --scope "/subscriptions/your-sub-id"

# 3. Deploy (credentials automatic!)
```

---

## 🎯 Remember

- ✅ Run `az login` before using the app
- ✅ Set `AZURE_TENANT_ID` for Azure Government
- ✅ No credentials in appsettings.json
- ✅ Use `UseManagedIdentity: false` for dev
- ✅ Use `UseManagedIdentity: true` for prod

**That's it!** 🎉
