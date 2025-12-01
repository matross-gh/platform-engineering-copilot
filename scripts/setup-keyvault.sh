#!/bin/bash
# Azure Key Vault Setup Script for Platform Engineering Copilot
# This script creates and configures Azure Key Vault for secure secret management

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to read secret from user input
read_secret() {
    local prompt="$1"
    local secret_var="$2"
    
    echo -n "$prompt: "
    read -s secret_value
    echo
    
    eval "$secret_var='$secret_value'"
}

# Check prerequisites
print_info "Checking prerequisites..."

if ! command_exists az; then
    print_error "Azure CLI not found. Please install: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

print_success "Azure CLI found"

# Set Azure Cloud to Government
print_info "Setting Azure Cloud to AzureUSGovernment..."
az cloud set --name AzureUSGovernment

# Check if logged in
if ! az account show &>/dev/null; then
    print_warning "Not logged in to Azure. Initiating login..."
    az login --cloud AzureUSGovernment
fi

# Get current subscription
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
SUBSCRIPTION_NAME=$(az account show --query name --output tsv)
print_success "Logged in to subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"

# Get configuration from user
print_info "\n📝 Configuration"
echo -n "Resource Group Name [rg-platform-engineering-copilot]: "
read RESOURCE_GROUP
RESOURCE_GROUP=${RESOURCE_GROUP:-rg-platform-engineering-copilot}

echo -n "Key Vault Name [pec-compliance-kv]: "
read KEY_VAULT_NAME
KEY_VAULT_NAME=${KEY_VAULT_NAME:-pec-compliance-kv}

echo -n "Location [usgovvirginia]: "
read LOCATION
LOCATION=${LOCATION:-usgovvirginia}

echo -n "App Service Name (leave empty to skip Managed Identity setup): "
read APP_NAME

# Create Resource Group (if it doesn't exist)
print_info "\n🏗️  Creating Resource Group..."
if az group show --name "$RESOURCE_GROUP" &>/dev/null; then
    print_warning "Resource Group '$RESOURCE_GROUP' already exists. Skipping creation."
else
    az group create \
      --name "$RESOURCE_GROUP" \
      --location "$LOCATION" \
      --cloud AzureUSGovernment
    print_success "Resource Group '$RESOURCE_GROUP' created"
fi

# Create Key Vault
print_info "\n🔐 Creating Azure Key Vault..."
if az keyvault show --name "$KEY_VAULT_NAME" &>/dev/null; then
    print_warning "Key Vault '$KEY_VAULT_NAME' already exists. Skipping creation."
else
    az keyvault create \
      --name "$KEY_VAULT_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --location "$LOCATION" \
      --sku Premium \
      --enable-purge-protection true \
      --enable-soft-delete true \
      --retention-days 90 \
      --cloud AzureUSGovernment
    print_success "Key Vault '$KEY_VAULT_NAME' created"
fi

# Grant current user access to Key Vault
print_info "\n👤 Granting current user access to Key Vault..."
CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show --query id --output tsv --cloud AzureUSGovernment)
az keyvault set-policy \
  --name "$KEY_VAULT_NAME" \
  --object-id "$CURRENT_USER_OBJECT_ID" \
  --secret-permissions get list set delete \
  --cloud AzureUSGovernment
print_success "Current user granted access to Key Vault"

# Add secrets
print_info "\n🔑 Adding secrets to Key Vault..."
print_warning "Leave any secret blank to skip"

# Azure OpenAI API Key
read_secret "Azure OpenAI API Key" AZURE_OPENAI_API_KEY
if [ -n "$AZURE_OPENAI_API_KEY" ]; then
    az keyvault secret set \
      --vault-name "$KEY_VAULT_NAME" \
      --name "AzureOpenAI-ApiKey" \
      --value "$AZURE_OPENAI_API_KEY" \
      --cloud AzureUSGovernment \
      >/dev/null
    print_success "Azure OpenAI API Key added"
fi

# Azure AD Client Secret
read_secret "Azure AD Client Secret" AZURE_AD_CLIENT_SECRET
if [ -n "$AZURE_AD_CLIENT_SECRET" ]; then
    az keyvault secret set \
      --vault-name "$KEY_VAULT_NAME" \
      --name "AzureAD-ClientSecret" \
      --value "$AZURE_AD_CLIENT_SECRET" \
      --cloud AzureUSGovernment \
      >/dev/null
    print_success "Azure AD Client Secret added"
fi

# GitHub Access Token
read_secret "GitHub Personal Access Token" GITHUB_ACCESS_TOKEN
if [ -n "$GITHUB_ACCESS_TOKEN" ]; then
    az keyvault secret set \
      --vault-name "$KEY_VAULT_NAME" \
      --name "GitHub-AccessToken" \
      --value "$GITHUB_ACCESS_TOKEN" \
      --cloud AzureUSGovernment \
      >/dev/null
    print_success "GitHub Access Token added"
fi

# GitHub Webhook Secret
read_secret "GitHub Webhook Secret" GITHUB_WEBHOOK_SECRET
if [ -n "$GITHUB_WEBHOOK_SECRET" ]; then
    az keyvault secret set \
      --vault-name "$KEY_VAULT_NAME" \
      --name "GitHub-WebhookSecret" \
      --value "$GITHUB_WEBHOOK_SECRET" \
      --cloud AzureUSGovernment \
      >/dev/null
    print_success "GitHub Webhook Secret added"
fi

# SQL Server Password
read_secret "SQL Server Password" SQL_PASSWORD
if [ -n "$SQL_PASSWORD" ]; then
    SQL_CONNECTION_STRING="Server=sqlserver;Database=PlatformEngineeringCopilot;User Id=sa;Password=${SQL_PASSWORD};TrustServerCertificate=True;"
    az keyvault secret set \
      --vault-name "$KEY_VAULT_NAME" \
      --name "SqlServerConnectionString" \
      --value "$SQL_CONNECTION_STRING" \
      --cloud AzureUSGovernment \
      >/dev/null
    print_success "SQL Server Connection String added"
fi

# Setup Managed Identity (if App Service name provided)
if [ -n "$APP_NAME" ]; then
    print_info "\n🔐 Setting up Managed Identity..."
    
    # Enable System-Assigned Managed Identity
    PRINCIPAL_ID=$(az webapp identity assign \
      --name "$APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --query principalId \
      --output tsv \
      --cloud AzureUSGovernment)
    print_success "Managed Identity enabled for App Service '$APP_NAME'"
    
    # Grant Key Vault access
    az keyvault set-policy \
      --name "$KEY_VAULT_NAME" \
      --object-id "$PRINCIPAL_ID" \
      --secret-permissions get list \
      --cloud AzureUSGovernment
    print_success "Managed Identity granted access to Key Vault"
fi

# Enable diagnostic logging
print_info "\n📊 Enabling diagnostic logging..."
LOG_ANALYTICS_WORKSPACE="pec-log-analytics"

# Check if Log Analytics workspace exists
if ! az monitor log-analytics workspace show \
  --resource-group "$RESOURCE_GROUP" \
  --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
  &>/dev/null; then
    print_warning "Log Analytics workspace '$LOG_ANALYTICS_WORKSPACE' not found. Creating..."
    az monitor log-analytics workspace create \
      --resource-group "$RESOURCE_GROUP" \
      --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
      --location "$LOCATION" \
      --cloud AzureUSGovernment
    print_success "Log Analytics workspace created"
fi

WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group "$RESOURCE_GROUP" \
  --workspace-name "$LOG_ANALYTICS_WORKSPACE" \
  --query id \
  --output tsv \
  --cloud AzureUSGovernment)

# Enable Key Vault audit logging
az monitor diagnostic-settings create \
  --name pec-kv-audit-logs \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME" \
  --logs '[{"category":"AuditEvent","enabled":true}]' \
  --workspace "$WORKSPACE_ID" \
  --cloud AzureUSGovernment \
  >/dev/null 2>&1 || print_warning "Diagnostic settings already exist or failed to create"
print_success "Diagnostic logging enabled"

# Display summary
print_info "\n📋 Setup Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Key Vault created: $KEY_VAULT_NAME"
echo "🌐 Endpoint: https://${KEY_VAULT_NAME}.vault.azure.us/"
echo "📍 Location: $LOCATION"
echo "🔒 SKU: Premium (HSM-backed)"
echo "🗑️  Purge Protection: Enabled"
echo "♻️  Soft Delete: Enabled (90 days)"
echo "📊 Audit Logging: Enabled (Log Analytics)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

print_info "\n📝 Next Steps"
echo "1. Update appsettings.json with Key Vault references:"
echo "   {\"KeyVault\": {\"Endpoint\": \"https://${KEY_VAULT_NAME}.vault.azure.us/\"}}"
echo ""
echo "2. Replace secrets with Key Vault references:"
echo "   \"ApiKey\": \"@Microsoft.KeyVault(SecretUri=https://${KEY_VAULT_NAME}.vault.azure.us/secrets/AzureOpenAI-ApiKey/)\""
echo ""
echo "3. Add NuGet packages to your project:"
echo "   dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets"
echo "   dotnet add package Azure.Identity"
echo ""
echo "4. Add Key Vault configuration to Program.cs (see KEY-VAULT-MIGRATION.md)"
echo ""
echo "5. Test locally:"
echo "   az login --cloud AzureUSGovernment"
echo "   dotnet run"

print_success "\n🎉 Key Vault setup complete!"
