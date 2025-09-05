@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource cache_kv 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: take('cachekv-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'cache-kv'
  }
}

output vaultUri string = cache_kv.properties.vaultUri

output name string = cache_kv.name