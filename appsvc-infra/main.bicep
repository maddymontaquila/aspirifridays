targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module env 'env/env.bicep' = {
  name: 'env'
  scope: rg
  params: {
    location: location
    userPrincipalId: principalId
  }
}

module cache 'cache/cache.bicep' = {
  name: 'cache'
  scope: rg
  params: {
    location: location
    cache_kv_outputs_name: cache_kv.outputs.name
  }
}

module cache_kv 'cache-kv/cache-kv.bicep' = {
  name: 'cache-kv'
  scope: rg
  params: {
    location: location
  }
}

module boardadmin_identity 'boardadmin-identity/boardadmin-identity.bicep' = {
  name: 'boardadmin-identity'
  scope: rg
  params: {
    location: location
  }
}

module boardadmin_roles_cache_kv 'boardadmin-roles-cache-kv/boardadmin-roles-cache-kv.bicep' = {
  name: 'boardadmin-roles-cache-kv'
  scope: rg
  params: {
    location: location
    cache_kv_outputs_name: cache_kv.outputs.name
    principalId: boardadmin_identity.outputs.principalId
  }
}

output env_AZURE_CONTAINER_REGISTRY_NAME string = env.outputs.AZURE_CONTAINER_REGISTRY_NAME

output env_AZURE_CONTAINER_REGISTRY_ENDPOINT string = env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT

output env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID

output env_planId string = env.outputs.planId

output env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID string = env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID

output cache_kv_name string = cache_kv.outputs.name

output cache_kv_vaultUri string = cache_kv.outputs.vaultUri

output boardadmin_identity_id string = boardadmin_identity.outputs.id

output boardadmin_identity_clientId string = boardadmin_identity.outputs.clientId