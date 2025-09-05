@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param cache_kv_outputs_name string

resource cache 'Microsoft.Cache/redisEnterprise@2025-04-01' = {
  name: take('cache-${uniqueString(resourceGroup().id)}', 60)
  location: location
  sku: {
    name: 'Balanced_B0'
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource cache_default 'Microsoft.Cache/redisEnterprise/databases@2025-04-01' = {
  name: 'default'
  properties: {
    accessKeysAuthentication: 'Enabled'
    port: 10000
  }
  parent: cache
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: cache_kv_outputs_name
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'connectionstrings--cache'
  properties: {
    value: '${cache.properties.hostName}:10000,ssl=true,password=${cache_default.listKeys().primaryKey}'
  }
  parent: keyVault
}

output name string = cache.name

output hostName string = cache.properties.hostName