// Azure AI Search module — indexes Cosmos DB content for agent knowledge retrieval (RAG)
@description('Name of the Azure AI Search service')
param searchServiceName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Search service SKU')
@allowed([
  'free'
  'basic'
  'standard'
  'standard2'
  'standard3'
])
param skuName string = environment == 'prod' ? 'standard' : 'basic'

@description('Number of replicas')
param replicaCount int = 1

@description('Number of partitions')
param partitionCount int = 1

@description('Public network access')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'

@description('Resource tags')
param tags object = {
  Environment: 'Development'
  ManagedBy: 'Bicep'
}

resource searchService 'Microsoft.Search/searchServices@2025-05-01' = {
  name: searchServiceName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: 'Default'
    publicNetworkAccess: publicNetworkAccess
    disableLocalAuth: false
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
  }
}

@description('Azure AI Search service name')
output searchServiceName string = searchService.name

@description('Azure AI Search endpoint')
output endpoint string = searchService.properties.endpoint

@description('Azure AI Search resource ID (for RBAC role assignments)')
output searchServiceResourceId string = searchService.id

@description('Azure AI Search principal ID (system-assigned identity, used for the Cosmos DB indexer)')
output searchServicePrincipalId string = searchService.identity.principalId
