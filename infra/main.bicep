@description('Environment name (e.g., prod, staging, dev).')
param environmentName string = 'prod'

@description('Primary Azure region for all resources.')
param location string = resourceGroup().location

@description('Prefix for resource names.')
param resourcePrefix string = 'resilience-demo'

@description('Unique suffix for globally unique resources (e.g. Redis, App Service).')
param uniqueSuffix string = uniqueString(resourceGroup().id)

var redisName = '${resourcePrefix}-redis-${uniqueSuffix}'
var planName = '${resourcePrefix}-plan-${environmentName}'
var entitlementsName = '${resourcePrefix}-entitlements-${uniqueSuffix}'
var recommendationsName = '${resourcePrefix}-recommendations-${uniqueSuffix}'

// 1. Provision Azure Managed Redis
module redisModule 'modules/redis.bicep' = {
  name: 'redisDeployment'
  params: {
    redisCacheName: redisName
    location: location
    skuName: 'Balanced_B0'
  }
}

// 2. Provision App Service Plan, EntitlementsService & RecommendationsService (with Staging Slot)
module appServiceModule 'modules/appservice.bicep' = {
  name: 'appServiceDeployment'
  params: {
    appServicePlanName: planName
    entitlementsAppName: entitlementsName
    recommendationsAppName: recommendationsName
    location: location
    appServiceSku: 'S1'
    redisConnectionString: redisModule.outputs.connectionString
  }
}

@description('The Redis host endpoint.')
output redisHost string = redisModule.outputs.redisHost

@description('The Entitlements microservice URL.')
output entitlementsServiceUrl string = appServiceModule.outputs.entitlementsUrl

@description('The Recommendations microservice Production URL.')
output recommendationsProdUrl string = appServiceModule.outputs.recommendationsProdUrl

@description('The Recommendations microservice Staging slot URL.')
output recommendationsStagingUrl string = appServiceModule.outputs.recommendationsStagingUrl
