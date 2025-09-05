@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_registry_endpoint string

param env_outputs_planid string

param env_outputs_azure_container_registry_managed_identity_id string

param env_outputs_azure_container_registry_managed_identity_client_id string

param boardadmin_containerimage string

param boardadmin_containerport string

param cache_kv_outputs_name string

@secure()
param admin_password_value string

param boardadmin_identity_outputs_id string

param boardadmin_identity_outputs_clientid string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-11-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: boardadmin_containerimage
    isMain: true
    userManagedIdentityClientId: env_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource cache_kv 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: cache_kv_outputs_name
}

resource cache_kv_connectionstrings__cache 'Microsoft.KeyVault/vaults/secrets@2024-11-01' existing = {
  name: 'connectionstrings--cache'
  parent: cache_kv
}

resource webapp 'Microsoft.Web/sites@2024-11-01' = {
  name: take('${toLower('boardadmin')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    serverFarmId: env_outputs_planid
    keyVaultReferenceIdentity: boardadmin_identity_outputs_id
    siteConfig: {
      linuxFxVersion: 'SITECONTAINERS'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: env_outputs_azure_container_registry_managed_identity_client_id
      appSettings: [
        {
          name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
          value: 'true'
        }
        {
          name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
          value: 'true'
        }
        {
          name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
          value: 'in_memory'
        }
        {
          name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
          value: 'true'
        }
        {
          name: 'HTTP_PORTS'
          value: boardadmin_containerport
        }
        {
          name: 'ConnectionStrings__cache'
          value: '@Microsoft.KeyVault(SecretUri=${cache_kv_connectionstrings__cache.properties.secretUri})'
        }
        {
          name: 'Authentication__AdminPassword'
          value: admin_password_value
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: boardadmin_identity_outputs_clientid
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${env_outputs_azure_container_registry_managed_identity_id}': { }
      '${boardadmin_identity_outputs_id}': { }
    }
  }
}