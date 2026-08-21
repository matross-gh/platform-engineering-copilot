using 'cicd.deploy.pe.infrastructure.bicep'

param pGlobalParameters = {
  landingZoneName: 'TODO-landing-zone-name'
  commandAcronym: 'TODO-command-acronym'
  location: 'va'
  azureRegionName: 'usgovvirginia'
  standardLandingZoneName: 'pec'
  environment: 'dev'
}

param pTags = {
  Application: 'PlatformEngineeringCopilot'
  Environment: 'dev'
}

param pInfrastructureResourceGroupName = 'rg-pec-infrastructure-dev'

param pSecurityResourceGroupName = 'TODO-existing-security-rg-name'

param pAzureRegionName = 'usgovvirginia'

param pTenantId = 'TODO-tenant-id-guid'

param pHubSubscriptionId = 'TODO-hub-mccog-subscription-id-guid'

param pHubResourceGroupName = 'TODO-hub-mccog-resource-group-name'

param pVnetName = 'vnet-pec-dev'

param pVnetAddressPrefixes = [
  '10.1.0.0/22'
]

param pDnsServers = [
  'TODO-ehm-dns-server-ip-1'
  'TODO-ehm-dns-server-ip-2'
]

param pSubnets = [
  {
    name: 'snet-aks-system'
    addressPrefix: '10.1.0.0/24'
    securityRules: []
  }
  {
    name: 'snet-private-endpoints'
    addressPrefix: '10.1.1.0/24'
    securityRules: []
  }
  {
    name: 'snet-aci'
    addressPrefix: '10.1.2.0/24'
    securityRules: []
    delegations: [
      {
        name: 'aciDelegation'
        properties: {
          serviceName: 'Microsoft.ContainerInstance/containerGroups'
        }
      }
    ]
  }
]

param pEnableDDoSProtection = false

param pLogAnalyticsName = 'law-pec-dev'

param pAppInsightsName = 'appi-pec-dev'

param pKeyVaultName = 'kv-pec-dev'

param pKeyVaultAdminObjectId = 'TODO-object-id-of-kv-admin-user-or-group'

param pKeyVaultAdminPrincipalType = 'User'

param pKeyVaultPrivateIpAddress = 'TODO-ppsm-static-ip-keyvault'

param pStorageAccountName = 'stpecdev'

param pStorageAccountBlobPrivateIpAddress = 'TODO-ppsm-static-ip-storage-blob'

param pStorageAccountDfsPrivateIpAddress = 'TODO-ppsm-static-ip-storage-dfs'

param pSqlServerName = 'sql-pec-dev'

param pSqlAdminLogin = 'TODO-sql-admin-login'

param pSqlAdminLoginPassword = 'TODO-sql-admin-password-use-keyvault-reference'

param pSqlAzureADOnlyAuthentication = true

param pSqlAzureADAdminObjectId = 'TODO-object-id-of-sql-aad-admin'

param pSqlAzureADAdminLogin = 'TODO-login-name-of-sql-aad-admin'

param pSqlAzureADAdminPrincipalType = 'User'

param pSqlPrivateIpAddress = 'TODO-ppsm-static-ip-sql'

param pAcrName = 'acrpecdev'

param pAcrSku = 'Premium'

param pAcrPrivateIpAddress = 'TODO-ppsm-static-ip-acr-registry'

param pAcrDataPrivateIpAddress = 'TODO-ppsm-static-ip-acr-data'

param pAcrEnableEncryption = 'disabled'

param pAcrKeyVaultIdentityClientId = ''

param pAcrKeyVaultKeyIdentifierUri = ''

param pAksName = 'aks-pec-dev'

param pAksKubernetesVersion = 'TODO-supported-k8s-version-in-region'

param pAksUserAssignedIdentityResourceId = 'TODO-resource-id-of-user-assigned-identity-for-aks'

param pAksHostGroupId = ''

param pAksPrivateDnsZoneId = 'TODO-resource-id-of-private-dns-zone-for-aks-api'

param pAksAdminGroupObjectIds = [
  'TODO-object-id-of-aks-admin-group'
]

param pAksLinuxAdminUsername = 'azureuser'

param pAksSshPublicKey = 'TODO-ssh-public-key'

param pAksPodCidr = '10.244.0.0/16'

param pAksServiceCidr = 'TODO-ehm-assigned-service-cidr-slash24'

param pAksDnsServiceIp = 'TODO-service-cidr-dot10-address'

param pAksDnsPrefix = 'aks-pec-dev'

param pAksNodeResourceGroupName = 'rg-pec-aks-dev-nodes'

param pDeployRedis = true

param pRedisName = 'redis-pec-dev'

param pRedisSkuName = 'Premium'

param pRedisSkuCapacity = 1

param pRedisPrivateIpAddress = 'TODO-ppsm-static-ip-redis'

param pDeployFoundry = true

param pFoundryName = 'foundry-pec-dev'

param pFoundryProjectName = 'proj-pec-dev'

param pFoundryModelCapacity = 10

param pFoundryAzureRegionName = 'usgovvirginia'

param pFoundryPrivateIpAddress = 'TODO-ppsm-static-ip-foundry'
