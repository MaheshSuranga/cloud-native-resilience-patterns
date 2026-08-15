@description('The name of the Azure Cache for Redis instance.')
param redisCacheName string

@description('Location for the Redis cache resource.')
param location string = resourceGroup().location

@description('The pricing tier of the Redis cache.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Basic'

@description('The SKU family of the Redis cache.')
@allowed([
  'C'
  'P'
])
param skuFamily string = 'C'

@description('The size of the Redis cache.')
@allowed([
  0
  1
  2
  3
  4
  5
  6
])
param skuCapacity int = 0

resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: skuName
      family: skuFamily
      capacity: skuCapacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'volatile-lru'
    }
  }
}

@description('The hostname of the Redis cache.')
output redisHost string = redis.properties.hostName

@description('The SSL port of the Redis cache.')
output redisSslPort int = redis.properties.sslPort

@description('The primary connection string for Redis cache.')
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = '${redis.properties.hostName}:${redis.properties.sslPort},password=${redis.listKeys().primaryKey},ssl=True,abortConnect=False'
