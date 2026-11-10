param environmentName string
param location string
param staticWebAppLocation string
param postgresLocation string
param tags object
param principalId string
param postgresAdminLogin string

@secure()
param postgresAdminPassword string

@secure()
param jwtSigningKey string

param googleClientId string
param webPushPublicKey string

@secure()
param webPushPrivateKey string

param openAiModelName string
param openAiModelVersion string

@allowed([ 'F1', 'B1', 'B2', 'B3', 'S1', 'P0v3', 'P1v3' ])
param appServiceSku string

// Free (F1) has no always-on: the API sleeps when idle and cold-starts on the next request.
var isFreeTier = appServiceSku == 'F1'

var token = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName))
var databaseName = 'alexandria'

// TEMPORARY: the deploying account only has Contributor, which cannot create role
// assignments. Until it gets Role Based Access Control Administrator on this resource
// group, Key Vault uses access policies and OpenAI/Vision use API keys stored in Key Vault.
// Target state: RBAC Key Vault + managed identity roles on OpenAI/Vision, local auth disabled.

// ---------- Key Vault ----------

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${token}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: false
    accessPolicies: []
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Separate resource so the vault and the API (which references the vault) don't depend on each other.
resource keyVaultAccess 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: concat(
      [
        {
          tenantId: subscription().tenantId
          objectId: api.identity.principalId
          permissions: { secrets: [ 'get' ] }
        }
      ],
      empty(principalId) ? [] : [
        {
          tenantId: subscription().tenantId
          objectId: principalId
          permissions: { secrets: [ 'get', 'list' ] }
        }
      ]
    )
  }
}

resource postgresConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'postgres-connection'
  properties: {
    value: 'Host=${postgres.properties.fullyQualifiedDomainName};Database=${databaseName};Username=${postgresAdminLogin};Password=${postgresAdminPassword};Ssl Mode=Require'
  }
}

resource jwtSigningKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-signing-key'
  properties: {
    value: jwtSigningKey
  }
}

resource webPushPrivateKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'webpush-private-key'
  properties: {
    value: webPushPrivateKey
  }
}

resource openAiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'openai-key'
  properties: {
    value: openAi.listKeys().key1
  }
}

resource visionKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'vision-key'
  properties: {
    value: vision.listKeys().key1
  }
}

// ---------- PostgreSQL ----------

// Can differ from the main region: some subscriptions can't create PostgreSQL in every region.
// The region suffix also avoids name clashes with a failed attempt in another region.
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: postgresLocation == location ? 'psql-${token}' : 'psql-${token}-${postgresLocation}'
  location: postgresLocation
  tags: tags
  sku: { name: 'Standard_B1ms', tier: 'Burstable' }
  properties: {
    version: '17'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: databaseName
}

// Lets Azure-hosted services (the App Service) reach the server.
resource postgresAllowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

// ---------- Azure OpenAI ----------

resource openAi 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: 'oai-${token}'
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: 'oai-${token}'
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

resource openAiDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAi
  name: openAiModelName
  sku: { name: 'GlobalStandard', capacity: 10 }
  properties: {
    model: { format: 'OpenAI', name: openAiModelName, version: openAiModelVersion }
  }
}

// ---------- Azure Vision ----------

resource vision 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: 'cv-${token}'
  location: location
  tags: tags
  kind: 'ComputerVision'
  // Free tier (20 calls/min, 5,000/month) keeps beta costs at zero; only one F0 Vision resource is allowed per subscription.
  sku: { name: 'F0' }
  properties: {
    customSubDomainName: 'cv-${token}'
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

// ---------- API (App Service) ----------

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'plan-${token}'
  location: location
  tags: tags
  kind: 'linux'
  sku: { name: appServiceSku }
  properties: { reserved: true }
}

// Static Web Apps only supports a few regions (not westus); content is served from a global CDN regardless.
resource web 'Microsoft.Web/staticSites@2024-04-01' = {
  name: 'swa-${token}'
  location: staticWebAppLocation
  tags: union(tags, { 'azd-service-name': 'web' })
  sku: { name: 'Free', tier: 'Free' }
  properties: {}
}

resource api 'Microsoft.Web/sites@2024-04-01' = {
  name: 'app-${token}'
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  kind: 'app,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: !isFreeTier
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: [ 'https://${web.properties.defaultHostname}' ]
      }
    }
  }
}

// Applied as a separate resource so it runs only after the access policy and secrets exist.
// Each provision re-deploys the vault with an empty policy list before re-adding the policies;
// settings resolved inside that window would be cached as AccessToKeyVaultDenied.
resource apiSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: api
  name: 'appsettings'
  // The secrets are implicit dependencies through the references below.
  dependsOn: [ keyVaultAccess ]
  properties: {
    ConnectionStrings__Default: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${postgresConnectionSecret.name})'
    Jwt__SigningKey: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${jwtSigningKeySecret.name})'
    Google__ClientIds__0: googleClientId
    Database__MigrateOnStartup: 'true'
    AzureOpenAI__Endpoint: openAi.properties.endpoint
    AzureOpenAI__Deployment: openAiDeployment.name
    AzureOpenAI__ApiKey: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${openAiKeySecret.name})'
    AzureVision__Endpoint: vision.properties.endpoint
    AzureVision__ApiKey: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${visionKeySecret.name})'
    WebPush__Subject: 'https://${web.properties.defaultHostname}'
    WebPush__PublicKey: webPushPublicKey
    WebPush__PrivateKey: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${webPushPrivateKeySecret.name})'
  }
}

output keyVaultName string = keyVault.name
output openAiEndpoint string = openAi.properties.endpoint
output openAiDeploymentName string = openAiDeployment.name
output visionEndpoint string = vision.properties.endpoint
output apiUrl string = 'https://${api.properties.defaultHostName}'
output webUrl string = 'https://${web.properties.defaultHostname}'
output postgresHost string = postgres.properties.fullyQualifiedDomainName
