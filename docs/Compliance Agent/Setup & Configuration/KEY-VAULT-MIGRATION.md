# Azure Key Vault Secrets Migration Guide

## Overview

This guide documents the migration of hardcoded secrets from `appsettings.json` to Azure Key Vault for enhanced security and compliance. This is **Tier 1, Task 2** of the enhancement roadmap.

## Current State (Security Risk ⚠️)

### Hardcoded Secrets in appsettings.json

```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=sqlserver;Database=PlatformEngineeringCopilot;User Id=sa;Password=YourStrong@Passw0rd;..."
  },
  "AzureAd": {
    "ClientSecret": ""
  },
  "Gateway": {
    "AzureOpenAI": {
      "ApiKey": "your-azure-openai-api-key-here"
    },
    "GitHub": {
      "AccessToken": "ghp_your_github_personal_access_token_here",
      "PersonalAccessToken": "ghp_your_github_personal_access_token_here",
      "WebhookSecret": "your-webhook-secret-here"
    }
  }
}
```

**Risks:**
- Secrets stored in plain text
- Committed to source control
- No rotation capability
- No audit trail for access
- Non-compliance with FedRAMP/NIST 800-53 requirements (SC-12, SC-13, IA-5)

---

## Target State (Secure ✅)

### Azure Key Vault References

```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/SqlServerConnectionString/)"
  },
  "AzureAd": {
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/AzureAD-ClientSecret/)"
  },
  "Gateway": {
    "AzureOpenAI": {
      "ApiKey": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/AzureOpenAI-ApiKey/)"
    },
    "GitHub": {
      "AccessToken": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/GitHub-AccessToken/)",
      "PersonalAccessToken": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/GitHub-AccessToken/)",
      "WebhookSecret": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/GitHub-WebhookSecret/)"
    }
  }
}
```

**Benefits:**
- Centralized secret management
- Audit logs for all access (NIST 800-53 AU-2, AU-3)
- Rotation without code changes
- Role-based access control (AC-3, AC-6)
- Encryption at rest (SC-28)
- Managed Identity support (IA-2)

---

## Implementation Steps

### 1. Create Azure Key Vault (Azure Government Cloud)

```bash
# Variables
RESOURCE_GROUP="rg-platform-engineering-copilot"
KEY_VAULT_NAME="pec-compliance-kv"  # Must be globally unique
LOCATION="usgovvirginia"

# Create Key Vault (Azure Government Cloud)
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Premium \
  --enable-purge-protection true \
  --enable-soft-delete true \
  --retention-days 90 \
  --cloud AzureUSGovernment
```

**Key Vault Configuration:**
- **SKU**: Premium (HSM-backed keys for FedRAMP compliance)
- **Purge Protection**: Enabled (prevents accidental deletion)
- **Soft Delete**: Enabled (90-day retention)
- **Cloud**: AzureUSGovernment

### 2. Add Secrets to Key Vault

```bash
# Azure OpenAI API Key
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "AzureOpenAI-ApiKey" \
  --value "YOUR_ACTUAL_AZURE_OPENAI_API_KEY_HERE" \
  --cloud AzureUSGovernment

# Azure AD Client Secret
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "AzureAD-ClientSecret" \
  --value "YOUR_ACTUAL_CLIENT_SECRET_HERE" \
  --cloud AzureUSGovernment

# GitHub Access Token
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "GitHub-AccessToken" \
  --value "YOUR_ACTUAL_GITHUB_TOKEN_HERE" \
  --cloud AzureUSGovernment

# GitHub Webhook Secret
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "GitHub-WebhookSecret" \
  --value "YOUR_ACTUAL_WEBHOOK_SECRET_HERE" \
  --cloud AzureUSGovernment

# SQL Server Connection String (complete connection string)
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "SqlServerConnectionString" \
  --value "Server=sqlserver;Database=PlatformEngineeringCopilot;User Id=sa;Password=YOUR_ACTUAL_SQL_PASSWORD;TrustServerCertificate=True;" \
  --cloud AzureUSGovernment
```

### 3. Grant Access to Managed Identity

**Option A: System-Assigned Managed Identity (Recommended for Azure-hosted)**

```bash
# Enable Managed Identity on App Service / Container App
APP_NAME="platform-engineering-copilot-chat"
az webapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --cloud AzureUSGovernment

# Get the Managed Identity Principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv \
  --cloud AzureUSGovernment)

# Grant Key Vault access
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list \
  --cloud AzureUSGovernment
```

**Option B: User-Assigned Managed Identity (Multi-service)**

```bash
# Create User-Assigned Managed Identity
IDENTITY_NAME="pec-key-vault-identity"
az identity create \
  --name $IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --cloud AzureUSGovernment

# Get Principal ID
PRINCIPAL_ID=$(az identity show \
  --name $IDENTITY_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv \
  --cloud AzureUSGovernment)

# Grant Key Vault access
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list \
  --cloud AzureUSGovernment
```

**Option C: Service Principal (Development/Local)**

```bash
# For local development, grant access to your Azure AD user
YOUR_USER_OBJECT_ID=$(az ad signed-in-user show --query id --output tsv --cloud AzureUSGovernment)

az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $YOUR_USER_OBJECT_ID \
  --secret-permissions get list \
  --cloud AzureUSGovernment
```

### 4. Update appsettings.json

Replace hardcoded secrets with Key Vault references:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./platform_engineering_copilot_management.db",
    "SqlServerConnection": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/SqlServerConnectionString/)"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.us/",
    "TenantId": "your-gov-tenant-id",
    "ClientId": "your-mcp-app-registration-id",
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/AzureAD-ClientSecret/)",
    "Audience": "api://platform-engineering-copilot"
  },
  "Gateway": {
    "AzureOpenAI": {
      "ApiKey": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/AzureOpenAI-ApiKey/)",
      "Endpoint": "https://your-resource-name.openai.azure.us/",
      "DeploymentName": "gpt-4o",
      "UseManagedIdentity": false
    },
    "GitHub": {
      "AccessToken": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/GitHub-AccessToken/)",
      "PersonalAccessToken": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/GitHub-AccessToken/)",
      "WebhookSecret": "@Microsoft.KeyVault(SecretUri=https://pec-compliance-kv.vault.azure.us/secrets/GitHub-WebhookSecret/)"
    }
  }
}
```

### 5. Configure Key Vault in Program.cs

**For Platform.Engineering.Copilot.Chat/Program.cs:**

Add the following **before** `builder.Build()`:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

// Add Azure Key Vault configuration
var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"] ?? "https://pec-compliance-kv.vault.azure.us/";

if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    // Use DefaultAzureCredential for automatic authentication
    // Local: Uses Azure CLI credentials
    // Azure: Uses Managed Identity
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Prioritize Managed Identity for Azure deployments
            ManagedIdentityClientId = builder.Configuration["KeyVault:ManagedIdentityClientId"],
            // Exclude Visual Studio and VS Code credentials for production
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true
        }));
    
    Console.WriteLine($"✅ Azure Key Vault configured: {keyVaultEndpoint}");
}
else
{
    Console.WriteLine("⚠️  Key Vault not configured. Using local secrets only.");
}
```

**Update appsettings.json with Key Vault endpoint:**

```json
{
  "KeyVault": {
    "Endpoint": "https://pec-compliance-kv.vault.azure.us/"
  }
}
```

### 6. Add Required NuGet Packages

```bash
# Navigate to the Chat project
cd src/Platform.Engineering.Copilot.Chat

# Add Azure Key Vault packages
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

**Repeat for all projects that need Key Vault access:**
- `Platform.Engineering.Copilot.Compliance.Agent`
- `Platform.Engineering.Copilot.Admin.API`
- `Platform.Engineering.Copilot.Mcp`

---

## Testing & Validation

### 1. Test Local Development

```bash
# Login to Azure CLI (uses your user account for Key Vault access)
az cloud set --name AzureUSGovernment
az login

# Verify Key Vault access
az keyvault secret show \
  --vault-name pec-compliance-kv \
  --name AzureOpenAI-ApiKey \
  --cloud AzureUSGovernment

# Run the application
cd src/Platform.Engineering.Copilot.Chat
dotnet run
```

**Expected Output:**
```
✅ Azure Key Vault configured: https://pec-compliance-kv.vault.azure.us/
✅ Compliance Agent initialized with AI chat completion service
```

### 2. Test Azure Deployment

```bash
# Deploy to Azure App Service with Managed Identity
az webapp config appsettings set \
  --name platform-engineering-copilot-chat \
  --resource-group rg-platform-engineering-copilot \
  --settings KeyVault__Endpoint=https://pec-compliance-kv.vault.azure.us/ \
  --cloud AzureUSGovernment

# Check application logs
az webapp log tail \
  --name platform-engineering-copilot-chat \
  --resource-group rg-platform-engineering-copilot \
  --cloud AzureUSGovernment
```

### 3. Verify Secret Rotation

```bash
# Rotate Azure OpenAI API Key
NEW_API_KEY="new-azure-openai-api-key"
az keyvault secret set \
  --vault-name pec-compliance-kv \
  --name AzureOpenAI-ApiKey \
  --value $NEW_API_KEY \
  --cloud AzureUSGovernment

# Application will pick up new secret automatically (no restart required)
# Verify by checking Key Vault access logs
az monitor activity-log list \
  --resource-id "/subscriptions/{subscription-id}/resourceGroups/rg-platform-engineering-copilot/providers/Microsoft.KeyVault/vaults/pec-compliance-kv" \
  --start-time 2024-01-01 \
  --cloud AzureUSGovernment
```

---

## Security Best Practices

### 1. Least Privilege Access

Only grant `get` and `list` permissions to application identities:

```bash
az keyvault set-policy \
  --name pec-compliance-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list  # NO set, delete, purge permissions
```

### 2. Network Restrictions

Limit Key Vault access to specific virtual networks:

```bash
az keyvault network-rule add \
  --name pec-compliance-kv \
  --vnet-name vnet-platform-engineering \
  --subnet subnet-apps \
  --cloud AzureUSGovernment
```

### 3. Audit Logging

Enable diagnostic settings to log all Key Vault operations:

```bash
az monitor diagnostic-settings create \
  --name pec-kv-audit-logs \
  --resource /subscriptions/{subscription-id}/resourceGroups/rg-platform-engineering-copilot/providers/Microsoft.KeyVault/vaults/pec-compliance-kv \
  --logs '[{"category":"AuditEvent","enabled":true}]' \
  --workspace /subscriptions/{subscription-id}/resourceGroups/rg-platform-engineering-copilot/providers/Microsoft.OperationalInsights/workspaces/pec-log-analytics \
  --cloud AzureUSGovernment
```

### 4. Secret Expiration

Set expiration dates on secrets:

```bash
# Set expiration 90 days from now
EXPIRATION_DATE=$(date -u -d "+90 days" +"%Y-%m-%dT%H:%M:%SZ")

az keyvault secret set \
  --vault-name pec-compliance-kv \
  --name AzureOpenAI-ApiKey \
  --value $API_KEY \
  --expires $EXPIRATION_DATE \
  --cloud AzureUSGovernment
```

### 5. Secret Naming Convention

Use consistent naming for easy management:

- **Format**: `{Service}-{Type}` (e.g., `AzureOpenAI-ApiKey`, `GitHub-AccessToken`)
- **No underscores**: Use hyphens only (Key Vault limitation)
- **No spaces**: CamelCase or hyphens

---

## Compliance Mapping

| NIST 800-53 Control | Key Vault Feature | Implementation |
|---------------------|-------------------|----------------|
| **SC-12 (Cryptographic Key Establishment and Management)** | Premium SKU (HSM-backed) | Keys stored in FIPS 140-2 Level 2 HSM |
| **SC-13 (Cryptographic Protection)** | Encryption at rest | All secrets encrypted with AES-256 |
| **SC-28 (Protection of Information at Rest)** | Azure Key Vault encryption | Automatic encryption of stored secrets |
| **IA-2 (Identification and Authentication)** | Managed Identity | No credentials in application code |
| **IA-5 (Authenticator Management)** | Secret rotation | Secrets can be rotated without code changes |
| **AC-3 (Access Enforcement)** | RBAC policies | Fine-grained access control via Azure AD |
| **AC-6 (Least Privilege)** | Minimal permissions | `get` and `list` only for app identities |
| **AU-2 (Audit Events)** | Diagnostic logs | All access logged to Log Analytics |
| **AU-3 (Content of Audit Records)** | Audit log schema | Who, what, when, result logged |
| **AU-9 (Protection of Audit Information)** | Log Analytics retention | 90-day retention with immutability |

---

## Troubleshooting

### Issue: "Authentication failed" error

**Cause:** Managed Identity not granted access to Key Vault

**Solution:**
```bash
# Verify Managed Identity Principal ID
az webapp identity show --name platform-engineering-copilot-chat --resource-group rg-platform-engineering-copilot --query principalId

# Grant access
az keyvault set-policy --name pec-compliance-kv --object-id $PRINCIPAL_ID --secret-permissions get list
```

### Issue: "Secret not found" error

**Cause:** Secret name mismatch or deleted

**Solution:**
```bash
# List all secrets
az keyvault secret list --vault-name pec-compliance-kv --cloud AzureUSGovernment

# Check for soft-deleted secrets
az keyvault secret list-deleted --vault-name pec-compliance-kv --cloud AzureUSGovernment

# Recover deleted secret
az keyvault secret recover --vault-name pec-compliance-kv --name AzureOpenAI-ApiKey --cloud AzureUSGovernment
```

### Issue: "@Microsoft.KeyVault" not replaced with actual value

**Cause:** Key Vault configuration not added to Program.cs

**Solution:** Ensure `builder.Configuration.AddAzureKeyVault()` is called **before** `builder.Build()`

### Issue: Local development can't access Key Vault

**Cause:** Azure CLI not authenticated

**Solution:**
```bash
# Login to Azure CLI
az cloud set --name AzureUSGovernment
az login

# Grant your user account access to Key Vault
az keyvault set-policy --name pec-compliance-kv --upn your-email@domain.gov --secret-permissions get list
```

---

## Rollback Plan

If issues occur, revert to local secrets:

1. **Remove Key Vault configuration** from Program.cs
2. **Restore appsettings.json** with hardcoded values (from git history)
3. **Restart application**

```bash
# Revert appsettings.json
git checkout HEAD~1 -- appsettings.json

# Restart application
dotnet run
```

---

## Success Metrics

- ✅ **No secrets in appsettings.json** (all replaced with Key Vault references)
- ✅ **AI Chat Completion warning resolved** (Azure OpenAI API key loaded from Key Vault)
- ✅ **Audit logs enabled** (Key Vault access logged to Log Analytics)
- ✅ **Managed Identity working** (no credentials in application code)
- ✅ **Secret rotation tested** (application picks up new secrets without restart)

---

## Next Steps

After Key Vault migration is complete:

1. **Delete hardcoded secrets** from git history (BFG Repo-Cleaner or git-filter-repo)
2. **Enable secret expiration** (90-day rotation policy)
3. **Implement automated rotation** (Azure Functions or Logic Apps)
4. **Complete Tier 1, Task 3**: Create AuditLogEntity for database persistence

---

## References

- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Managed Identities for Azure Resources](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [NIST 800-53 Security Controls](https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final)
- [FedRAMP Key Vault Requirements](https://www.fedramp.gov/)
