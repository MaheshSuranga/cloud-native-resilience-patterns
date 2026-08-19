@description('The name of the Azure Managed Redis instance.')
param redisCacheName string

@description('Location for the Redis resource.')
param location string = resourceGroup().location

@description('The SKU of the Azure Managed Redis instance.')
@allowed([
  'Balanced_B0'
  'Balanced_B1'
  'Balanced_B3'
  'MemoryOptimized_M10'
  'ComputeOptimized_X10'
])
param skuName string = 'Balanced_B0'

resource redisEnterprise 'Microsoft.Cache/redisEnterprise@2024-09-01-preview' = {
  name: redisCacheName
  location: location
  sku: {
    name: skuName
  }
}

resource redisDatabase 'Microsoft.Cache/redisEnterprise/databases@2024-09-01-preview' = {
  parent: redisEnterprise
  name: 'default'
  properties: {
    clusteringPolicy: 'OSSCluster'
    evictionPolicy: 'VolatileLRU'
    port: 10000
  }
}

@description('The hostname of the Redis cache.')
output redisHost string = redisEnterprise.properties.hostName

@description('The SSL port of the Redis cache.')
output redisSslPort int = 10000

@description('The primary connection string for Redis cache.')
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = '${redisEnterprise.properties.hostName}:10000,password=${redisDatabase.listKeys().primaryKey},ssl=True,abortConnect=False'
