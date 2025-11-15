@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

param migrations_containerimage string

@secure()
param admin_password_value string

@secure()
param sql_password_value string

param env_outputs_azure_container_registry_endpoint string

param env_outputs_azure_container_registry_managed_identity_id string

resource migrations 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'migrations'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'authentication--adminpassword'
          value: admin_password_value
        }
        {
          name: 'connectionstrings--db'
          value: 'Server=sql,1433;User ID=sa;Password=${sql_password_value};TrustServerCertificate=true;Initial Catalog=db'
        }
        {
          name: 'db-password'
          value: sql_password_value
        }
      ]
      activeRevisionsMode: 'Single'
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
          image: migrations_containerimage
          name: 'migrations'
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
              name: 'Authentication__AdminPassword'
              secretRef: 'authentication--adminpassword'
            }
            {
              name: 'ConnectionStrings__db'
              secretRef: 'connectionstrings--db'
            }
            {
              name: 'DB_HOST'
              value: 'sql'
            }
            {
              name: 'DB_PORT'
              value: '1433'
            }
            {
              name: 'DB_USERNAME'
              value: 'sa'
            }
            {
              name: 'DB_PASSWORD'
              secretRef: 'db-password'
            }
            {
              name: 'DB_URI'
              value: 'mssql://sql:1433/db'
            }
            {
              name: 'DB_JDBCCONNECTIONSTRING'
              value: 'jdbc:sqlserver://sql:1433;trustServerCertificate=true;databaseName=db'
            }
            {
              name: 'DB_DATABASE'
              value: 'db'
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