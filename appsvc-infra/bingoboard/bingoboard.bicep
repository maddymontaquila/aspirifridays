@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_registry_endpoint string

param env_outputs_planid string

param env_outputs_azure_container_registry_managed_identity_id string

param env_outputs_azure_container_registry_managed_identity_client_id string

param bingoboard_containerimage string

resource mainContainer 'Microsoft.Web/sites/sitecontainers@2024-11-01' = {
  name: 'main'
  properties: {
    authType: 'UserAssigned'
    image: bingoboard_containerimage
    isMain: true
    userManagedIdentityClientId: env_outputs_azure_container_registry_managed_identity_client_id
  }
  parent: webapp
}

resource webapp 'Microsoft.Web/sites@2024-11-01' = {
  name: take('${toLower('bingoboard')}-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    serverFarmId: env_outputs_planid
    siteConfig: {
      linuxFxVersion: 'SITECONTAINERS'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: env_outputs_azure_container_registry_managed_identity_client_id
      appSettings: [
        {
          name: 'NODE_ENV'
          value: 'production'
        }
        {
          name: 'PORT'
          value: '80'
        }
        {
          name: 'services__boardadmin__http__0'
          value: 'http://${take('${toLower('boardadmin')}-${uniqueString(resourceGroup().id)}', 60)}.azurewebsites.net'
        }
        {
          name: 'services__boardadmin__https__0'
          value: 'https://${take('${toLower('boardadmin')}-${uniqueString(resourceGroup().id)}', 60)}.azurewebsites.net'
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}