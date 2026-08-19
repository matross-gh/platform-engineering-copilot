// Microsoft Foundry module — AIServices account + project + chat model deployment
@description('Name of the Foundry (Cognitive Services AIServices) account')
param accountName string

@description('Name of the Foundry project')
param projectName string = 'default-project'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Foundry account SKU name')
param skuName string = 'S0'

@description('Public network access')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'

@description('Chat model deployment name')
param chatDeploymentName string = 'gpt-4o'

@description('Chat model name')
param chatModelName string = 'gpt-4o'

@description('Chat model version')
param chatModelVersion string = '2024-11-20'

@description('Chat model deployment capacity (in units of 1,000 TPM)')
param chatCapacity int = 10

@description('Resource tags')
param tags object = {
  Environment: 'Development'
  ManagedBy: 'Bicep'
}

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-10-01-preview' = {
  name: accountName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: publicNetworkAccess
    networkAcls: {
      defaultAction: environment == 'prod' ? 'Deny' : 'Allow'
    }
    disableLocalAuth: false
    allowProjectManagement: true
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-10-01-preview' = {
  parent: foundryAccount
  name: chatDeploymentName
  sku: {
    name: 'Standard'
    capacity: chatCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
  }
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-10-01-preview' = {
  parent: foundryAccount
  name: projectName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: projectName
  }
  dependsOn: [
    chatDeployment
  ]
}

@description('Foundry account name')
output accountName string = foundryAccount.name

@description('Foundry account endpoint')
output accountEndpoint string = foundryAccount.properties.endpoint

@description('Foundry project name')
output projectName string = foundryProject.name

@description('Foundry project endpoint (used by SDK/REST callers)')
output projectEndpoint string = '${foundryAccount.properties.endpoint}api/projects/${foundryProject.name}'

@description('Chat model deployment name')
output chatDeploymentName string = chatDeployment.name

@description('Foundry account principal ID (system-assigned identity)')
output accountPrincipalId string = foundryAccount.identity.principalId

@description('Foundry account resource ID (for RBAC role assignments)')
output accountResourceId string = foundryAccount.id
