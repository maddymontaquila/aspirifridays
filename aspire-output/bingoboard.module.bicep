@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

param env_outputs_azure_container_registry_endpoint string

param env_outputs_azure_container_registry_managed_identity_id string

param bingoboard_containerimage string

resource bingoboard 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'bingoboard'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5000
        transport: 'http'
      }
      registries: [
        {
          server: env_outputs_azure_container_registry_endpoint
          identity: env_outputs_azure_container_registry_managed_identity_id
        }
      ]
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: bingoboard_containerimage
          name: 'bingoboard'
          command: [
            'dotnet'
          ]
          args: [
            '/app/yarp.dll'
          ]
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'YARP_ENABLE_STATIC_FILES'
              value: 'true'
            }
            {
              name: 'REVERSEPROXY__ROUTES__route0__MATCH__PATH'
              value: '/bingohub/{**catch-all}'
            }
            {
              name: 'REVERSEPROXY__ROUTES__route0__CLUSTERID'
              value: 'cluster_boardadmin'
            }
            {
              name: 'REVERSEPROXY__CLUSTERS__cluster_boardadmin__DESTINATIONS__destination1__ADDRESS'
              value: 'https+http://boardadmin'
            }
            {
              name: 'services__boardadmin__http__0'
              value: 'http://boardadmin.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__boardadmin__https__0'
              value: 'https://boardadmin.${env_outputs_azure_container_apps_environment_default_domain}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}