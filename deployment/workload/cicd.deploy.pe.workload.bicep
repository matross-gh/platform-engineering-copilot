/* ********************************************************************************
 * cicd.deploy.pe.workload.bicep
 *
 * Deploys the Platform Engineering Copilot workload containers as Azure Container
 * Instances (ACI): mcp, chat, admin-api, admin-client, using the Enterprise Bicep
 * Registry module br/enterprisebicepregistry:microsoft.containerinstance/containergroups:1.0.2.
 *
 * All four container groups share one VNet-integrated subnet (snet-aci) produced
 * by cicd.deploy.pe.infrastructure.bicep (see its `aciSubnetId` output), and log
 * to the same Log Analytics workspace as the rest of the infrastructure.
 *
 * Scope: resourceGroup (deploy this file INTO the workload resource group; can be
 * the same resource group as infrastructure, or a separate one).
 * ********************************************************************************/

/* ********************************************************************************
 * PARAMETERS
 * ********************************************************************************/
@description('Tags applied to all resources.')
param pTags object

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

@description('Resource id of the "snet-aci" subnet output by cicd.deploy.pe.infrastructure.bicep (aciSubnetId output).')
param pAciSubnetId string

@description('Login server (FQDN) of the ACR output by cicd.deploy.pe.infrastructure.bicep (acrLoginServer output).')
param pAcrLoginServer string

@description('Resource id of a user-assigned managed identity with AcrPull on the registry, used by every container group to pull images.')
param pAcrPullIdentityResourceId string

@description('Name of the pre-existing Log Analytics workspace (from infrastructure deployment) used for ACI diagnostics.')
param pLogAnalyticsWorkspaceName string

@description('Resource group name of the Log Analytics workspace above.')
param pLogAnalyticsResourceGroupName string

@description('''
Array of container group definitions to deploy. Each item requires:
name, image (full ACR image ref e.g. "mcp:latest"), cpu (number), memoryInGb (number),
port (int), environmentVariables (array of { name, value } or { name, secureValue }).
Defaults to mcp / chat / admin-api / admin-client per the confirmed workload scope.
''')
param pContainerGroups array = [
  {
    name: 'pec-mcp'
    image: 'mcp:latest'
    cpu: 1
    memoryInGb: 2
    port: 8080
    environmentVariables: []
  }
  {
    name: 'pec-chat'
    image: 'chat:latest'
    cpu: 1
    memoryInGb: 2
    port: 8081
    environmentVariables: []
  }
  {
    name: 'pec-admin-api'
    image: 'admin-api:latest'
    cpu: 1
    memoryInGb: 2
    port: 8082
    environmentVariables: []
  }
  {
    name: 'pec-admin-client'
    image: 'admin-client:latest'
    cpu: 1
    memoryInGb: 1
    port: 8083
    environmentVariables: []
  }
]

/* ********************************************************************************
 * DEPLOYMENT
 * ********************************************************************************/
module containerGroups 'br/enterprisebicepregistry:microsoft.containerinstance/containergroups:1.0.2' = [for cg in pContainerGroups: {
  name: 'deploy-${cg.name}'
  params: {
    pContainerGroupsName: cg.name
    pContainerGroupsAzureRegionName: pAzureRegionName
    pContainerGroupsTags: pTags
    pContainerGroupsEnableSystemIdentity: false
    pContainerGroupsImageRegistryIdentityResourceId: pAcrPullIdentityResourceId
    pContainerGroupsImageRegistryServer: pAcrLoginServer
    pContainerGroupsSubnetResourceId: pAciSubnetId
    pContainerGroupsIpAddressPorts: [
      {
        port: cg.port
        protocol: 'TCP'
      }
    ]
    pContainerGroupsContainers: [
      {
        name: cg.name
        properties: {
          image: '${pAcrLoginServer}/${cg.image}'
          ports: [
            {
              port: cg.port
              protocol: 'TCP'
            }
          ]
          environmentVariables: cg.environmentVariables
          resources: {
            requests: {
              cpu: cg.cpu
              memoryInGB: cg.memoryInGb
            }
          }
        }
      }
    ]
    pContainerGroupsLogAnalyticsName: pLogAnalyticsWorkspaceName
    pContainerGroupsLogAnalyticsResourceGroupName: pLogAnalyticsResourceGroupName
  }
}]

/* ********************************************************************************
 * OUTPUT
 * ********************************************************************************/
output containerGroupIds array = [for i in range(0, length(pContainerGroups)): containerGroups[i].outputs.cg_id]
output containerGroupPrivateIps array = [for i in range(0, length(pContainerGroups)): containerGroups[i].outputs.cg_ipaddress]
