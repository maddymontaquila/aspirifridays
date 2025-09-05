@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource boardadmin_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('boardadmin_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = boardadmin_identity.id

output clientId string = boardadmin_identity.properties.clientId

output principalId string = boardadmin_identity.properties.principalId

output principalName string = boardadmin_identity.name

output name string = boardadmin_identity.name