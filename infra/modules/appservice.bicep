@description('The name of the App Service Plan.')
param appServicePlanName string

@description('The name of the Entitlements App Service.')
param entitlementsAppName string

@description('The name of the Recommendations App Service.')
param recommendationsAppName string

@description('Location for all App Service resources.')
param location string = resourceGroup().location

@description('The pricing tier for the App Service Plan. F1/B1 for dev/trial, S1+/P1v3+ for production with deployment slots.')
@allowed([
  'F1'
  'B1'
  'S1'
  'S2'
  'S3'
  'P1v3'
  'P2v3'
])
param appServiceSku string = 'S1'

@description('The Redis connection string to inject into Recommendations App Service.')
@secure()
param redisConnectionString string

var supportsAlwaysOn = (appServiceSku != 'F1')

// 1. Linux App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSku
  }
  properties: {
    reserved: true
  }
}

// 2. Entitlements App Service
resource entitlementsApp 'Microsoft.Web/sites@2023-12-01' = {
  name: entitlementsAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: supportsAlwaysOn
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

// 3. Recommendations App Service (Production Slot)
resource recommendationsApp 'Microsoft.Web/sites@2023-12-01' = {
  name: recommendationsAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: supportsAlwaysOn
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__Redis'
          value: redisConnectionString
        }
        {
          name: 'Services__EntitlementsUrl'
          value: 'https://${entitlementsApp.properties.defaultHostName}'
        }
      ]
    }
  }
}

// =============================================================================
// DEPLOYMENT SLOTS & CANARY SUPPORT
// =============================================================================
// Deployment slots require Standard (S1/S2/S3) or Premium (P1v3/P2v3) tiers.
// - F1 (Free) and B1 (Basic) do NOT support deployment slots.
// - supportsSlots evaluates to true when S1 or P1v3 is selected, provisioning:
//     1. 'staging' slot for RecommendationsService
//     2. 'slotConfigNames' sticky settings to maintain environment isolation
// =============================================================================
var supportsSlots = (appServiceSku != 'B1' && appServiceSku != 'F1')

// 4. Recommendations App Service - Staging Deployment Slot (Enabled for Standard & Premium SKUs)
// When enabled, new application builds are deployed here first for warmup and health probing
// before progressive canary traffic shifting (10% -> 50% -> 100% atomic swap).
resource recommendationsStagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = if (supportsSlots) {
  parent: recommendationsApp
  name: 'staging'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: supportsAlwaysOn
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
        {
          name: 'ConnectionStrings__Redis'
          value: redisConnectionString
        }
        {
          name: 'Services__EntitlementsUrl'
          value: 'https://${entitlementsApp.properties.defaultHostName}'
        }
      ]
    }
  }
}

// 5. Slot-Sticky Settings Configuration (Prevents Staging settings from swapping to Prod)
resource slotConfig 'Microsoft.Web/sites/config@2023-12-01' = if (supportsSlots) {
  parent: recommendationsApp
  name: 'slotConfigNames'
  properties: {
    appSettingNames: [
      'ASPNETCORE_ENVIRONMENT'
    ]
    connectionStringNames: [
      'Redis'
    ]
  }
}

@description('URL of the Entitlements Service.')
output entitlementsUrl string = 'https://${entitlementsApp.properties.defaultHostName}'

@description('Production URL of the Recommendations Service.')
output recommendationsProdUrl string = 'https://${recommendationsApp.properties.defaultHostName}'

@description('Staging slot URL of the Recommendations Service.')
output recommendationsStagingUrl string = supportsSlots ? 'https://${recommendationsApp.name}-staging.azurewebsites.net' : 'https://${recommendationsApp.properties.defaultHostName}'
