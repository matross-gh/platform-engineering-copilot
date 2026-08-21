using 'cicd.deploy.pe.workload.bicep'

param pTags = {
  Application: 'PlatformEngineeringCopilot'
  Environment: 'dev'
}

param pAzureRegionName = 'usgovvirginia'

param pAciSubnetId = 'TODO-paste-aciSubnetId-output-from-infrastructure-deployment'

param pAcrLoginServer = 'TODO-paste-acrLoginServer-output-from-infrastructure-deployment'

param pAcrPullIdentityResourceId = 'TODO-resource-id-of-user-assigned-identity-with-acrpull-role'

param pLogAnalyticsWorkspaceName = 'law-pec-dev'

param pLogAnalyticsResourceGroupName = 'rg-pec-infrastructure-dev'

param pContainerGroups = [
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
