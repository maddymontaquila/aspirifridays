@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

param env_outputs_azure_container_registry_endpoint string

param env_outputs_azure_container_registry_managed_identity_id string

param boardadmin_containerimage string

param boardadmin_containerport string

@secure()
param cache_password_value string

@secure()
param admin_password_value string

resource boardadmin 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'boardadmin'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--cache'
          value: 'cache:6379,password=${cache_password_value}'
        }
        {
          name: 'authentication--adminpassword'
          value: admin_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: int(boardadmin_containerport)
        transport: 'http'
        allowInsecure: true
        stickySessions: {
          affinity: 'sticky'
        }
      }
      registries: [
        {
          server: env_outputs_azure_container_registry_endpoint
          identity: env_outputs_azure_container_registry_managed_identity_id
        }
      ]
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: boardadmin_containerimage
          name: 'boardadmin'
          env: [
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
              secretRef: 'connectionstrings--cache'
            }
            {
              name: 'Authentication__AdminPassword'
              secretRef: 'authentication--adminpassword'
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