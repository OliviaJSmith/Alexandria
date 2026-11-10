targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('azd environment name, used to derive unique resource names.')
param environmentName string

@minLength(1)
@description('Primary Azure region for all resources.')
param location string

@description('Region for the Static Web App, which supports fewer regions than the other resources.')
param staticWebAppLocation string = 'westus2'

@description('Region for the PostgreSQL server. Defaults to the main region; set when the subscription cannot create PostgreSQL there.')
param postgresLocation string = ''

@description('Name of the resource group to create.')
param resourceGroupName string = 'alexandria-rg'

@description('Object ID of the developer running azd; granted data-plane roles so local development can reach Azure OpenAI and Vision.')
param principalId string = ''

@description('PostgreSQL administrator login.')
param postgresAdminLogin string = 'alexandriaadmin'

@secure()
@description('PostgreSQL administrator password. Supplied from the azd environment; never committed.')
param postgresAdminPassword string

@secure()
@description('Symmetric key used to sign the API\'s JWTs. Supplied from the azd environment; never committed.')
param jwtSigningKey string

@description('Google OAuth web client ID used to validate Google sign-ins.')
param googleClientId string

@description('Web Push (VAPID) public key, base64url. Not secret: browsers need it to subscribe.')
param webPushPublicKey string

@secure()
@description('Web Push (VAPID) private key, base64url. Supplied from the azd environment; never committed.')
param webPushPrivateKey string

@description('Azure OpenAI model to deploy for turning scanned text into book data.')
param openAiModelName string

@description('Version of the Azure OpenAI model to deploy.')
param openAiModelVersion string

@description('App Service plan size for the API. F1 (Free) for development; B1 or larger for always-on hosting.')
param appServiceSku string = 'F1'

var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    staticWebAppLocation: staticWebAppLocation
    postgresLocation: empty(postgresLocation) ? location : postgresLocation
    tags: tags
    principalId: principalId
    postgresAdminLogin: postgresAdminLogin
    postgresAdminPassword: postgresAdminPassword
    jwtSigningKey: jwtSigningKey
    googleClientId: googleClientId
    webPushPublicKey: webPushPublicKey
    webPushPrivateKey: webPushPrivateKey
    openAiModelName: openAiModelName
    openAiModelVersion: openAiModelVersion
    appServiceSku: appServiceSku
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_KEY_VAULT_NAME string = resources.outputs.keyVaultName
output AZURE_OPENAI_ENDPOINT string = resources.outputs.openAiEndpoint
output AZURE_OPENAI_DEPLOYMENT string = resources.outputs.openAiDeploymentName
output AZURE_VISION_ENDPOINT string = resources.outputs.visionEndpoint
output API_URL string = resources.outputs.apiUrl
output WEB_URL string = resources.outputs.webUrl
output POSTGRES_HOST string = resources.outputs.postgresHost
