// Azure OpenAI module — Cognitive Services account + chat/embedding model deployments
@description('Name of the Azure OpenAI (Cognitive Services) account')
param accountName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Azure OpenAI SKU name')
param skuName string = 'S0'

@description('Public network access')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'

@description('Chat model deployment name (referenced by app config as DeploymentName/ChatDeploymentName)')
param chatDeploymentName string = 'gpt-4o'

@description('Chat model name')
param chatModelName string = 'gpt-4o'

@description('Chat model version')
param chatModelVersion string = '2024-11-20'

@description('Chat model deployment capacity (in units of 1,000 TPM)')
param chatCapacity int = 10

@description('Embedding model deployment name')
param embeddingDeploymentName string = 'text-embedding-ada-002'

@description('Embedding model name')
param embeddingModelName string = 'text-embedding-ada-002'

@description('Embedding model version')
param embeddingModelVersion string = '2'

@description('Embedding model deployment capacity (in units of 1,000 TPM)')
param embeddingCapacity int = 10

@description('Resource tags')
param tags object = {
  Environment: 'Development'
  ManagedBy: 'Bicep'
}

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: accountName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'OpenAI'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: publicNetworkAccess
    networkAcls: {
      defaultAction: 'Allow'
    }
    disableLocalAuth: false
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAiAccount
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

resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAiAccount
  name: embeddingDeploymentName
  sku: {
    name: 'Standard'
    capacity: embeddingCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: embeddingModelName
      version: embeddingModelVersion
    }
  }
  dependsOn: [
    chatDeployment
  ]
}

@description('Azure OpenAI account name')
output accountName string = openAiAccount.name

@description('Azure OpenAI endpoint URL')
output endpoint string = openAiAccount.properties.endpoint

@description('Azure OpenAI primary API key')
output apiKey string = openAiAccount.listKeys().key1

@description('Chat model deployment name')
output chatDeploymentName string = chatDeployment.name

@description('Embedding model deployment name')
output embeddingDeploymentName string = embeddingDeployment.name
