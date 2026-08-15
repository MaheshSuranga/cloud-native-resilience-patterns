@description('The name of the App Service Plan.')
param appServicePlanName string

@description('The name of the Entitlements App Service.')
param entitlementsAppName string

@description('The name of the Recommendations App Service.')
param recommendationsAppName string

@description('Location for all App Service resources.')
param location string = resourceGroup().location

@description('The pricing tier for the App Service Plan (Standard S1 required for deployment slots).')
@allowed([
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

// 1. Linux App Service Plan (Standard tier supporting deployment slots & traffic routing)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSku
    tier: 'Standard'
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
      alwaysOn: true
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
      alwaysOn: true
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

// 4. Recommendations App Service - Staging Deployment Slot
resource recommendationsStagingSlot 'Microsoft.Web/sites/slots@2023-12-01' = {
  parent: recommendationsApp
  name: 'staging'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
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

// 5. Slot-Sticky Settings Configuration
resource slotConfig 'Microsoft.Web/sites/config@2023-12-01' = {
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
output recommendationsStagingUrl string = 'https://${recommendationsApp.name}-staging.azurewebsites.net'
