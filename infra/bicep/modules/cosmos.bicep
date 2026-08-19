// Azure Cosmos DB (NoSQL API) module — single account/database consolidating platform persistence
@description('Name of the Cosmos DB account')
param accountName string

@description('Name of the Cosmos DB SQL database')
param databaseName string = 'platform-engineering-copilot'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Enable serverless capacity mode (recommended for dev; use provisioned throughput for prod)')
param serverless bool = environment != 'prod'

@description('Public network access')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'

@description('Containers to create in the database, each with its own partition key')
param containers array = [
  { name: 'environmentTemplates', partitionKeyPath: '/id' }
  { name: 'environmentDeployments', partitionKeyPath: '/id' }
  { name: 'scalingPolicies', partitionKeyPath: '/id' }
  { name: 'environmentMetrics', partitionKeyPath: '/environmentId' }
  { name: 'agentConfigurations', partitionKeyPath: '/id' }
  { name: 'semanticIntents', partitionKeyPath: '/id' }
  { name: 'environmentLifecycles', partitionKeyPath: '/environmentId' }
  { name: 'approvalWorkflows', partitionKeyPath: '/id' }
  { name: 'complianceAssessments', partitionKeyPath: '/id' }
  { name: 'auditLogs', partitionKeyPath: '/id' }
  { name: 'agentState', partitionKeyPath: '/sessionId' }
]

@description('Resource tags')
param tags object = {
  Environment: 'Development'
  ManagedBy: 'Bicep'
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: accountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    capabilities: serverless ? [
      { name: 'EnableServerless' }
    ] : []
    publicNetworkAccess: publicNetworkAccess
    disableLocalAuth: false
  }
}

resource sqlDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource sqlContainers 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = [for container in containers: {
  parent: sqlDatabase
  name: container.name
  properties: {
    resource: {
      id: container.name
      partitionKey: {
        paths: [
          container.partitionKeyPath
        ]
        kind: 'Hash'
      }
    }
  }
}]

@description('Cosmos DB account name')
output accountName string = cosmosAccount.name

@description('Cosmos DB endpoint')
output endpoint string = cosmosAccount.properties.documentEndpoint

@description('Cosmos DB database name')
output databaseName string = sqlDatabase.name

@description('Cosmos DB account resource ID (for RBAC role assignments)')
output accountResourceId string = cosmosAccount.id

@description('Cosmos DB account principal ID (system-assigned identity)')
output accountPrincipalId string = cosmosAccount.identity.principalId
