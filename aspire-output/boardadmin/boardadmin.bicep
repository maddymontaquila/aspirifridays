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

param admin_cert_name string

param admin_domain string

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
          name: 'cache-password'
          value: cache_password_value
        }
        {
          name: 'cache-uri'
          value: 'redis://:${uriComponent(cache_password_value)}@cache:6379'
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
        customDomains: [
          {
            name: admin_domain
            bindingType: (admin_cert_name != '') ? 'SniEnabled' : 'Disabled'
            certificateId: (admin_cert_name != '') ? '${env_outputs_azure_container_apps_environment_id}/managedCertificates/${admin_cert_name}' : null
          }
        ]
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
              name: 'CACHE_HOST'
              value: 'cache'
            }
            {
              name: 'CACHE_PORT'
              value: '6379'
            }
            {
              name: 'CACHE_PASSWORD'
              secretRef: 'cache-password'
            }
            {
              name: 'CACHE_URI'
              secretRef: 'cache-uri'
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
        maxReplicas: 1
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