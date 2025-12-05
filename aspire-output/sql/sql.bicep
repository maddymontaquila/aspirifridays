@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

@secure()
param sql_password_value string

resource sql 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'sql'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'mssql-sa-password'
          value: sql_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 1433
        transport: 'tcp'
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'mcr.microsoft.com/mssql/server:2022-latest'
          name: 'sql'
          env: [
            {
              name: 'ACCEPT_EULA'
              value: 'Y'
            }
            {
              name: 'MSSQL_SA_PASSWORD'
              secretRef: 'mssql-sa-password'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
}