// Azure Cache for Redis module for session/state caching
@description('Name of the Redis Cache')
param redisCacheName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Redis SKU name')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = environment == 'prod' ? 'Standard' : 'Basic'

@description('Redis SKU family (C for Basic/Standard, P for Premium)')
@allowed([
  'C'
  'P'
])
param skuFamily string = 'C'

@description('Redis SKU capacity (0-6 for C family, 1-5 for P family)')
param skuCapacity int = 0

@description('Minimum TLS version')
@allowed([
  '1.0'
  '1.1'
  '1.2'
])
param minimumTlsVersion string = '1.2'

@description('Enable the non-SSL (6379) port')
param enableNonSslPort bool = false

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

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  tags: tags
  properties: {
    sku: {
      name: skuName
      family: skuFamily
      capacity: skuCapacity
    }
    minimumTlsVersion: minimumTlsVersion
    enableNonSslPort: enableNonSslPort
    publicNetworkAccess: publicNetworkAccess
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

@description('Redis Cache resource name')
output redisCacheName string = redisCache.name

@description('Redis Cache host name')
output hostName string = redisCache.properties.hostName

@description('Redis Cache SSL port')
output sslPort int = redisCache.properties.sslPort

@description('Redis Cache primary access key')
output primaryKey string = redisCache.listKeys().primaryKey

@description('Redis Cache connection string (StackExchange.Redis format)')
output connectionString string = '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
