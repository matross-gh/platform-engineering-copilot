/* ********************************************************************************
 * cicd.deploy.pe.infrastructure.bicep
 *
 * Deploys the shared Platform Engineering Copilot infrastructure using the
 * Enterprise Bicep Registry (br/enterprisebicepregistry, see ../bicepconfig.json).
 *
 * Scope: resourceGroup (deploy this file INTO the infrastructure resource group).
 * NSGs are deployed cross-scope into the pre-existing security resource group by
 * the virtualnetworks module itself (matches the registry's own convention).
 *
 * Includes: Log Analytics, Application Insights, Virtual Network (+ subnets/NSGs),
 * Key Vault, Storage Account, Azure Cosmos DB, Container Registry, AKS, Azure Cache for
 * Redis, and Microsoft Foundry (AIServices account + project + GPT-5.1 deployment).
 *
 * ASSUMPTIONS (adjust in parameters file as needed):
 * - ACR customer-managed-key encryption is DISABLED (avoids KV-key/ACR identity
 *   bootstrap ordering problem). Flip pAcrEnableEncryption + supply KV key info
 *   if CMK is required.
 * - One shared "private endpoints" subnet is used for KeyVault/Storage/Cosmos/ACR/
 *   Redis/Foundry. AKS gets its own system-pool subnet. A third subnet is
 *   reserved for the workload's ACI container groups (delegated to
 *   Microsoft.ContainerInstance/containerGroups) and exposed as an output so
 *   cicd.deploy.pe.workload.bicep can reference it without redeploying network.
 * ********************************************************************************/

/* ********************************************************************************
 * PARAMETERS
 * ********************************************************************************/
@description('''
Global naming/region parameters shared across every module in this deployment.
Properties: landingZoneName, commandAcronym, location, azureRegionName,
standardLandingZoneName, environment.
''')
param pGlobalParameters object

@description('Tags applied to all resources.')
param pTags object

@minLength(3)
@maxLength(80)
@description('Name of the resource group this file is deployed into (the "infrastructure" RG).')
param pInfrastructureResourceGroupName string = resourceGroup().name

@minLength(3)
@maxLength(63)
@description('Name of the PRE-EXISTING security resource group that will host the NSGs.')
param pSecurityResourceGroupName string

@minLength(8)
@maxLength(15)
@allowed([
  'usdodcentral'
  'usdodeast'
  'usgovarizona'
  'usgoviowa'
  'usgovtexas'
  'usgovvirginia'
  'usnateast'
  'usnatwest'
  'usseceast'
  'ussecwest'
])
@description('Azure Government region name used for all resources in this deployment.')
param pAzureRegionName string

@description('The Microsoft Entra tenant id.')
param pTenantId string

@description('''
The static subscription Id of the "hub" (MCCOG) used to establish private DNS
zone groups for every private endpoint in this deployment.
''')
param pHubSubscriptionId string

@description('The static resource group name in the "hub" (MCCOG).')
param pHubResourceGroupName string

/* --- Networking --- */
@description('Name of the virtual network.')
param pVnetName string

@description('VNet address space, e.g. [ \'10.1.0.0/22\' ].')
param pVnetAddressPrefixes array

@description('DNS servers for the vnet (EHM on-prem/Azure resolvers).')
param pDnsServers array

@description('''
Subnets to create. Each item requires: name, addressPrefix, securityRules (array,
can be empty). This deployment expects three entries named exactly:
"snet-aks-system", "snet-private-endpoints", "snet-aci" (aci subnet is delegated
by the caller via the `delegations` property to Microsoft.ContainerInstance/containerGroups).
''')
param pSubnets array

@description('Enable a DDoS protection plan for the vnet.')
param pEnableDDoSProtection bool = false

/* --- Log Analytics / App Insights --- */
@description('Name of the Log Analytics workspace.')
param pLogAnalyticsName string

@description('Name of the Application Insights component.')
param pAppInsightsName string

/* --- Key Vault --- */
@description('Name of the Key Vault.')
param pKeyVaultName string

@description('Object id (user/group/service principal) granted the Key Vault RBAC role below.')
param pKeyVaultAdminObjectId string

@allowed([ 'Device', 'ForeignGroup', 'Group', 'ServicePrincipal', 'User' ])
param pKeyVaultAdminPrincipalType string = 'User'

@description('Static/PPSM private IP address for the Key Vault private endpoint.')
param pKeyVaultPrivateIpAddress string

/* --- Storage Account --- */
@description('Name of the storage account (lowercase, 3-24 chars).')
param pStorageAccountName string

@description('Static/PPSM private IP address for the storage account Blob private endpoint.')
param pStorageAccountBlobPrivateIpAddress string

@description('Static/PPSM private IP address for the storage account DFS private endpoint.')
param pStorageAccountDfsPrivateIpAddress string

/* --- Azure Cosmos DB (br/enterprisebicepregistry:microsoft.documentdb/databaseaccounts:1.0.2) --- */
@description('Name of the Azure Cosmos DB account.')
param pCosmosAccountName string

@description('Static/PPSM private IP address for the Cosmos DB private endpoint.')
param pCosmosPrivateIpAddress string

@description('Name of the Cosmos SQL database used for environment/template/compliance management data.')
param pCosmosPlatformDatabaseName string = 'PlatformEngineeringCopilot'

@description('Name of the Cosmos SQL database used for chat conversation/message data.')
param pCosmosChatDatabaseName string = 'PlatformEngineeringCopilotChat'

@allowed([ 'None', 'Manual', 'Autoscale' ])
@description('''
Shared throughput mode for both Cosmos databases. Defaults to Autoscale since the
app creates containers at runtime via EF Core EnsureCreatedAsync() without
specifying their own per-container throughput, so they need to inherit shared
database-level throughput.
''')
param pCosmosDatabaseThroughputMode string = 'Autoscale'

@minValue(1000)
@description('Maximum shared autoscale throughput (RU/s) per Cosmos database. Used only when pCosmosDatabaseThroughputMode is Autoscale.')
param pCosmosDatabaseAutoscaleMaxThroughput int = 1000

@minValue(400)
@description('Shared manual throughput (RU/s) per Cosmos database. Used only when pCosmosDatabaseThroughputMode is Manual.')
param pCosmosDatabaseThroughput int = 400

@description('Principal (object) id of the workload user-assigned managed identity granted Cosmos DB Built-in Data Contributor on the account.')
param pCosmosDataAccessIdentityPrincipalId string

/* --- ACR --- */
@description('Name of the Azure Container Registry.')
param pAcrName string

@allowed([ 'Basic', 'Standard', 'Premium' ])
param pAcrSku string = 'Premium'

@description('Static/PPSM private IP address for the ACR registry private endpoint.')
param pAcrPrivateIpAddress string

@description('Static/PPSM private IP address for the ACR data private endpoint.')
param pAcrDataPrivateIpAddress string

@description('Enable customer-managed-key encryption for ACR. See ASSUMPTIONS above.')
param pAcrEnableEncryption string = 'disabled'

@description('Managed identity client id used to access the KV key, required only if pAcrEnableEncryption is enabled.')
param pAcrKeyVaultIdentityClientId string = ''

@description('Key Vault key identifier URI, required only if pAcrEnableEncryption is enabled.')
param pAcrKeyVaultKeyIdentifierUri string = ''

/* --- AKS --- */
@description('Name of the AKS cluster.')
param pAksName string

@description('Kubernetes version for the AKS cluster.')
param pAksKubernetesVersion string

@description('Resource id of the user-assigned managed identity for the AKS control plane.')
param pAksUserAssignedIdentityResourceId string

@description('Resource id of the dedicated host group for the AKS system pool, or empty string if not used.')
param pAksHostGroupId string = ''

@description('Resource id of the private DNS zone used for the AKS private cluster API server.')
param pAksPrivateDnsZoneId string

@description('Object ids of the Microsoft Entra group(s) granted cluster-admin via Azure RBAC.')
param pAksAdminGroupObjectIds array

@description('Linux admin username for AKS nodes.')
param pAksLinuxAdminUsername string

@secure()
@description('SSH public key for AKS Linux nodes.')
param pAksSshPublicKey string

@description('Pod CIDR for AKS (Azure CNI Overlay).')
param pAksPodCidr string

@description('Service CIDR for AKS.')
param pAksServiceCidr string

@description('DNS service IP for AKS (must be inside pAksServiceCidr).')
param pAksDnsServiceIp string

@description('DNS prefix for the AKS cluster.')
param pAksDnsPrefix string

@description('Name of the AKS node resource group (MC_ resource group).')
param pAksNodeResourceGroupName string

/* --- Redis (br/enterprisebicepregistry:microsoft.cache/redis:1.0.2) --- */
@description('Deploy an Azure Cache for Redis instance.')
param pDeployRedis bool = true

@description('Name of the Redis cache.')
param pRedisName string = ''

@allowed([ 'Basic', 'Standard', 'Premium' ])
param pRedisSkuName string = 'Premium'

@minValue(0)
@maxValue(6)
param pRedisSkuCapacity int = 1

@description('Static/PPSM private IP address for the Redis private endpoint.')
param pRedisPrivateIpAddress string = ''

/* --- Microsoft Foundry (br/enterprisebicepregistry:microsoft.cognitiveservices/accounts:1.0.2) --- */
@description('Deploy a Microsoft Foundry account/project with a GPT-5.1 model deployment.')
param pDeployFoundry bool = true

@description('Globally unique name of the Microsoft Foundry account.')
param pFoundryName string = ''

@description('Name of the Microsoft Foundry project.')
param pFoundryProjectName string = ''

@description('The GPT-5.1 deployment capacity in thousands of tokens per minute (K TPM). Requires sufficient regional quota.')
param pFoundryModelCapacity int = 10

@allowed([ 'usgovarizona', 'usgovvirginia' ])
@description('Azure Government region for the Foundry account (GPT-5.1 is only available in these regions).')
param pFoundryAzureRegionName string = 'usgovvirginia'

@description('Static/PPSM private IP address for the Foundry private endpoint.')
param pFoundryPrivateIpAddress string = ''

/* ********************************************************************************
 * VARIABLES
 * ********************************************************************************/
var vAksSystemPoolSubnetId = resourceId('Microsoft.Network/virtualNetworks/subnets', pVnetName, 'snet-aks-system')
var vPrivateEndpointsSubnetId = resourceId('Microsoft.Network/virtualNetworks/subnets', pVnetName, 'snet-private-endpoints')
var vAciSubnetId = resourceId('Microsoft.Network/virtualNetworks/subnets', pVnetName, 'snet-aci')

/* ********************************************************************************
 * MONITORING
 * ********************************************************************************/
module law 'br/enterprisebicepregistry:microsoft.operationalinsights/workspaces:1.0.2' = {
  name: 'deploy-${pLogAnalyticsName}'
  params: {
    pWorkspacesName: pLogAnalyticsName
    pWorkspacesAzureRegionName: pAzureRegionName
    pWorkspacesTags: pTags
  }
}

module appInsights 'br/enterprisebicepregistry:microsoft.insights/components:1.0.2' = {
  name: 'deploy-${pAppInsightsName}'
  params: {
    pComponentsName: pAppInsightsName
    pComponentsAzureRegionName: pAzureRegionName
    pComponentsTags: pTags
    pComponentsWorkspaceResourceId: law.outputs.law_id
  }
}

/* ********************************************************************************
 * NETWORK
 * ********************************************************************************/
module vnet 'br/enterprisebicepregistry:microsoft.network/virtualnetworks:1.0.4' = {
  name: 'deploy-${pVnetName}'
  params: {
    pGlobalParameters: pGlobalParameters
    pInfrastructureResourceGroupName: pInfrastructureResourceGroupName
    pSecurityResourceGroupName: pSecurityResourceGroupName
    pVirtualNetworksName: pVnetName
    pVirtualNetworksAzureRegionName: pAzureRegionName
    pVirtualNetworksTags: pTags
    pVirtualNetworksVnetAddressPrefixes: pVnetAddressPrefixes
    pVirtualNetworksSubnets: pSubnets
    pDnsServers: pDnsServers
    pVirtualNetworksEnableDDoSProtection: pEnableDDoSProtection
  }
}

/* ********************************************************************************
 * KEY VAULT
 * ********************************************************************************/
module keyVault 'br/enterprisebicepregistry:microsoft.keyvault/vaults:1.0.2' = {
  name: 'deploy-${pKeyVaultName}'
  params: {
    pVaultsName: pKeyVaultName
    pVaultsAzureRegionName: pAzureRegionName
    pVaultsTags: pTags
    pVaultsTenantId: pTenantId
    pVaultsObjectId: pKeyVaultAdminObjectId
    pVaultsPrincipalType: pKeyVaultAdminPrincipalType
    pVaultsWorkspaceResourceId: law.outputs.law_id
    pVaultsPrivateIpAddress: pKeyVaultPrivateIpAddress
    pVaultsSubnetResourceId: vPrivateEndpointsSubnetId
    pVaultsHubSubscriptionId: pHubSubscriptionId
    pVaultsHubResourceGroupName: pHubResourceGroupName
    pVaultsPublicNetworkAccess: 'Disabled'
    pVaultsPrivateEndPointName: 'pep-${pGlobalParameters.standardLandingZoneName}-kv'
    pVaultsDiagnosticsName: 'diag-${pKeyVaultName}'
  }
  dependsOn: [
    vnet
  ]
}

/* ********************************************************************************
 * STORAGE ACCOUNT
 * ********************************************************************************/
module storageAccount 'br/enterprisebicepregistry:microsoft.storage/storageaccounts:1.0.2' = {
  name: 'deploy-${pStorageAccountName}'
  params: {
    pGlobalParameters: pGlobalParameters
    pStorageAccountsName: pStorageAccountName
    pStorageAccountsAzureRegionName: pAzureRegionName
    pStorageAccountsTags: pTags
    pStorageAccountsLogAnalyticsName: pLogAnalyticsName
    pStorageAccountsLogAnalyticsResourceGroupName: pInfrastructureResourceGroupName
    pStorageAccountsPublicNetworkAccess: 'Disabled'
    pStorageAccountsEnabledServices: [ 'Blob' ]
    pStorageAccountsBlobPrivateIpAddress: pStorageAccountBlobPrivateIpAddress
    pStorageAccountsDfsPrivateIpAddress: pStorageAccountDfsPrivateIpAddress
    pStorageAccountsSubnetResourceId: vPrivateEndpointsSubnetId
    pStorageAccountsHubSubscriptionId: pHubSubscriptionId
    pStorageAccountsHubResourceGroupName: pHubResourceGroupName
    pStorageAccountsUsedByResourceName: pGlobalParameters.standardLandingZoneName
    pDiagnosticsName: 'diag-${pStorageAccountName}'
  }
  dependsOn: [
    vnet
    law
  ]
}

/* ********************************************************************************
 * AZURE COSMOS DB
 * ********************************************************************************/
// NOTE: pDatabaseAccountsAzureRegionName's @allowed list (usdodcentral/usdodeast/usgovarizona/
// usgovtexas/usgovvirginia) is narrower than pAzureRegionName's, so an `any()` cast is used
// to bypass the compile-time literal-union mismatch. Deploying to a region outside that
// narrower set (usgoviowa, usnateast/west, usseceast/west) will fail at deployment time.
module cosmosAccount 'br/enterprisebicepregistry:microsoft.documentdb/databaseaccounts:1.0.2' = {
  name: 'deploy-${pCosmosAccountName}'
  params: {
    pDatabaseAccountsName: pCosmosAccountName
    pDatabaseAccountsAzureRegionName: any(pAzureRegionName)
    pDatabaseAccountsTags: pTags
    pDatabaseAccountsPrivateIpAddress: pCosmosPrivateIpAddress
    pDatabaseAccountsPrivateEndpointsSubnetResourceId: vPrivateEndpointsSubnetId
    pDatabaseAccountsHubSubscriptionId: pHubSubscriptionId
    pDatabaseAccountsHubResourceGroupName: pHubResourceGroupName
    pDatabaseAccountsPrivateEndPointName: 'pep-${pGlobalParameters.standardLandingZoneName}-cosmos'
    pDatabaseAccountsLogAnalyticsWorkspaceId: law.outputs.law_id
    pDatabaseAccountsDiagnosticsName: 'diag-${pCosmosAccountName}'
  }
  dependsOn: [
    vnet
  ]
}

// NOTE: pSqlDatabasesAzureRegionName's @allowed list (usdodcentral/usdodeast/usgovarizona/
// usgovtexas/usgovvirginia) is narrower than pAzureRegionName's, so an `any()` cast is used
// to bypass the compile-time literal-union mismatch. Deploying to a region outside that
// narrower set (usgoviowa, usnateast/west, usseceast/west) will fail at deployment time.
module cosmosPlatformDatabase 'br/enterprisebicepregistry:microsoft.documentdb/databaseaccounts/sqldatabases:1.0.0' = {
  name: 'deploy-${pCosmosPlatformDatabaseName}'
  params: {
    pSqlDatabasesName: pCosmosPlatformDatabaseName
    pSqlDatabasesDatabaseAccountName: pCosmosAccountName
    pSqlDatabasesAzureRegionName: any(pAzureRegionName)
    pSqlDatabasesTags: pTags
    pSqlDatabasesThroughputMode: pCosmosDatabaseThroughputMode
    pSqlDatabasesThroughput: pCosmosDatabaseThroughput
    pSqlDatabasesAutoscaleMaxThroughput: pCosmosDatabaseAutoscaleMaxThroughput
  }
  dependsOn: [
    cosmosAccount
  ]
}

module cosmosChatDatabase 'br/enterprisebicepregistry:microsoft.documentdb/databaseaccounts/sqldatabases:1.0.0' = {
  name: 'deploy-${pCosmosChatDatabaseName}'
  params: {
    pSqlDatabasesName: pCosmosChatDatabaseName
    pSqlDatabasesDatabaseAccountName: pCosmosAccountName
    pSqlDatabasesAzureRegionName: any(pAzureRegionName)
    pSqlDatabasesTags: pTags
    pSqlDatabasesThroughputMode: pCosmosDatabaseThroughputMode
    pSqlDatabasesThroughput: pCosmosDatabaseThroughput
    pSqlDatabasesAutoscaleMaxThroughput: pCosmosDatabaseAutoscaleMaxThroughput
  }
  dependsOn: [
    cosmosAccount
  ]
}

// Local (key-based) auth defaults to disabled on the account; grant the workload
// identity Cosmos DB Built-in Data Contributor for SQL API data-plane access instead.
resource cosmosAccountRef 'Microsoft.DocumentDB/databaseAccounts@2025-04-15' existing = {
  name: pCosmosAccountName
  dependsOn: [
    cosmosAccount
  ]
}

resource cosmosDataContributorRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2025-04-15' = {
  name: guid(cosmosAccountRef.id, pCosmosDataAccessIdentityPrincipalId, 'CosmosDataContributor')
  parent: cosmosAccountRef
  properties: {
    roleDefinitionId: '${cosmosAccountRef.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: pCosmosDataAccessIdentityPrincipalId
    scope: cosmosAccountRef.id
  }
}

/* ********************************************************************************
 * CONTAINER REGISTRY
 * ********************************************************************************/
module acr 'br/enterprisebicepregistry:microsoft.containerregistry/containerregistry:1.0.2' = {
  name: 'deploy-${pAcrName}'
  params: {
    pRegistriesName: pAcrName
    pRegistriesAzureRegionName: pAzureRegionName
    pRegistriesTags: pTags
    pRegistriesSku: pAcrSku
    pRegistriesEnableSystemIdentity: true
    pRegistriesEnableAdminUser: false
    pRegistriesPublicNetworkAccess: 'Disabled'
    pRegistriesZoneRedundancy: 'Disabled'
    pRegistriesNetworkRuleBypassOptions: 'AzureServices'
    pRegistriesPolicies: {}
    pRegistriesLogAnalyticsWorkspaceId: law.outputs.law_id
    pRegistriesSubnetResourceId: vPrivateEndpointsSubnetId
    pRegistriesPrivateIpAddress: pAcrPrivateIpAddress
    pRegistriesDataPrivateIpAddress: pAcrDataPrivateIpAddress
    pRegistriesHubSubscriptionId: pHubSubscriptionId
    pRegistriesHubResourceGroupName: pHubResourceGroupName
    pRegistriesPrivateEndPointName: 'pep-${pGlobalParameters.standardLandingZoneName}-acr'
    pRegistriesDiagnosticsName: 'diag-${pAcrName}'
    pRegistriesEnableEncryption: pAcrEnableEncryption
    pRegistriesKeyVaultIdentityForCMKKey: pAcrKeyVaultIdentityClientId
    pRegistriesKeyVaultKeyIdentifierURI: pAcrKeyVaultKeyIdentifierUri
  }
  dependsOn: [
    vnet
  ]
}

/* ********************************************************************************
 * AKS
 * ********************************************************************************/
module aks 'br/enterprisebicepregistry:microsoft.containerservice/managedclusters:2.1.0' = {
  name: 'deploy-${pAksName}'
  params: {
    pGlobalParameters: pGlobalParameters
    pManagedClustersName: pAksName
    pManagedClustersTags: pTags
    pManagedClustersKubernetesVersion: pAksKubernetesVersion
    pManagedClustersLogAnalyticsWorkspaceId: law.outputs.law_id
    pManagedClustersUserAssignedIdentityResourceId: pAksUserAssignedIdentityResourceId
    pManagedClustersHostGroupId: pAksHostGroupId
    pManagedClustersPrivateDnsZoneId: pAksPrivateDnsZoneId
    pManagedClustersAdminGroupObjectIds: pAksAdminGroupObjectIds
    pManagedClustersLinuxAdminUsername: pAksLinuxAdminUsername
    pManagedClustersSshPublicKey: pAksSshPublicKey
    pManagedClustersSystemPoolSubnetId: vAksSystemPoolSubnetId
    pManagedClustersPodCidr: pAksPodCidr
    pManagedClustersServiceCidr: pAksServiceCidr
    pManagedClustersDnsServiceIp: pAksDnsServiceIp
    pManagedClustersDnsPrefix: pAksDnsPrefix
    pManagedClustersNodeResourceGroupName: pAksNodeResourceGroupName
  }
  dependsOn: [
    vnet
  ]
}

/* ********************************************************************************
 * REDIS (br/enterprisebicepregistry:microsoft.cache/redis:1.0.2)
 * ********************************************************************************/
module redis 'br/enterprisebicepregistry:microsoft.cache/redis:1.0.2' = if (pDeployRedis) {
  name: 'deploy-${pRedisName}'
  params: {
    pRedisName: pRedisName
    pRedisAzureRegionName: pAzureRegionName
    pRedisTags: pTags
    pRedisSkuName: pRedisSkuName
    pRedisSkuCapacity: pRedisSkuCapacity
    pRedisPrivateEndPointName: 'pep-${pGlobalParameters.standardLandingZoneName}-redis'
    pRedisPrivateIpAddress: pRedisPrivateIpAddress
    pRedisPrivateEndpointsSubnetResourceId: vPrivateEndpointsSubnetId
    pRedisHubSubscriptionId: pHubSubscriptionId
    pRedisHubResourceGroupName: pHubResourceGroupName
    pRedisLogAnalyticsWorkspaceId: law.outputs.law_id
    pRedisDiagnosticsName: 'diag-${pRedisName}'
  }
  dependsOn: [
    vnet
  ]
}

/* ********************************************************************************
 * MICROSOFT FOUNDRY (br/enterprisebicepregistry:microsoft.cognitiveservices/accounts:1.0.2)
 * ********************************************************************************/
module foundry 'br/enterprisebicepregistry:microsoft.cognitiveservices/accounts:1.0.2' = if (pDeployFoundry) {
  name: 'deploy-${pFoundryName}'
  params: {
    pFoundryName: pFoundryName
    pFoundryProjectName: pFoundryProjectName
    pFoundryAzureRegionName: pFoundryAzureRegionName
    pFoundryTags: pTags
    pFoundryModelCapacity: pFoundryModelCapacity
    pFoundryPrivateEndPointName: 'pep-${pGlobalParameters.standardLandingZoneName}-foundry'
    pFoundryPrivateIpAddress: pFoundryPrivateIpAddress
    pFoundryPrivateEndpointsSubnetResourceId: vPrivateEndpointsSubnetId
    pFoundryHubSubscriptionId: pHubSubscriptionId
    pFoundryHubResourceGroupName: pHubResourceGroupName
    pFoundryLogAnalyticsWorkspaceId: law.outputs.law_id
    pFoundryDiagnosticsName: 'diag-${pFoundryName}'
  }
  dependsOn: [
    vnet
  ]
}

/* ********************************************************************************
 * OUTPUT
 * ********************************************************************************/
output vnetId string = vnet.outputs.vnet_id
output vnetName string = vnet.outputs.vnet_name
output aciSubnetId string = vAciSubnetId
output logAnalyticsWorkspaceId string = law.outputs.law_id
output logAnalyticsWorkspaceName string = law.outputs.law_name
output appInsightsConnectionString string = appInsights.outputs.components_connectionstring
output keyVaultId string = keyVault.outputs.kv_id
output keyVaultName string = keyVault.outputs.kv_name
output storageAccountId string = storageAccount.outputs.sa_id
output storageAccountName string = storageAccount.outputs.sa_name
output cosmosAccountId string = cosmosAccount.outputs.cosmos_id
output cosmosAccountName string = cosmosAccount.outputs.cosmos_name
output cosmosDocumentEndpoint string = cosmosAccount.outputs.cosmos_documentendpoint
output cosmosPlatformDatabaseName string = cosmosPlatformDatabase.outputs.database_name
output cosmosChatDatabaseName string = cosmosChatDatabase.outputs.database_name
output acrId string = acr.outputs.registry_id
output acrName string = acr.outputs.registry_name
output acrLoginServer string = acr.outputs.registry_loginserver
output aksId string = aks.outputs.managedClusterId
output aksName string = aks.outputs.managedClusterName
output redisHostName string = redis.?outputs.redis_hostname ?? ''
output foundryEndpoint string = foundry.?outputs.foundry_endpoint ?? ''
output foundryProjectEndpoints object = foundry.?outputs.project_endpoints ?? {}
output foundryModelDeploymentName string = foundry.?outputs.model_name ?? ''
