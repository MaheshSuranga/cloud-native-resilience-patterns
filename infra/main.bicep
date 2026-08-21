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

// =============================================================================
// APP SERVICE PLAN SKU CONFIGURATION
// =============================================================================
// Current Setting: 'F1' (Free Tier)
// Reason: Azure trial/student accounts have 0-quota limits for paid VMs (Standard/Premium)
//         in some regions unless a quota increase is approved in the Azure Portal.
//
// PRODUCTION SETTING (Enables Deployment Slots & Canary Traffic Routing):
//   - Use 'S1' (Standard) or 'P1v3' (Premium V3)
//   - When set to 'S1' or 'P1v3', Bicep automatically deploys the 'staging' slot
//     and enables slot-sticky configuration (slotConfigNames) in appservice.bicep.
//
// To switch to Production:
//   param appServiceSku string = 'S1'   // Standard Tier ($73/mo, supports slots)
//   param appServiceSku string = 'P1v3' // Premium V3 ($130/mo, dedicated vCPU & slots)
// =============================================================================
@description('The SKU for the App Service Plan. F1 for trial subscriptions, S1/P1v3 for production with staging slots.')
param appServiceSku string = 'F1'

// 2. Provision App Service Plan, EntitlementsService & RecommendationsService (with Staging Slot)
module appServiceModule 'modules/appservice.bicep' = {
  name: 'appServiceDeployment'
  params: {
    appServicePlanName: planName
    entitlementsAppName: entitlementsName
    recommendationsAppName: recommendationsName
    location: location
    appServiceSku: appServiceSku
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
