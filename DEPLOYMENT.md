# Alexandria Deployment Guide

## Overview
This guide covers deploying Alexandria to Azure using .NET Aspire and GitHub Actions.

## Prerequisites

- Azure subscription
- Azure CLI installed
- GitHub repository with Actions enabled
- .NET 10 SDK
- Docker (for local Aspire testing)

## Azure Resources Setup

### 1. Create Resource Group

```bash
az group create \
  --name alexandria-rg \
  --location eastus
```

### 2. Create PostgreSQL Database

```bash
az postgres flexible-server create \
  --resource-group alexandria-rg \
  --name alexandria-db \
  --location eastus \
  --admin-user alexandriaadmin \
  --admin-password <YourSecurePassword> \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --version 15 \
  --storage-size 32

# Configure firewall to allow Azure services
az postgres flexible-server firewall-rule create \
  --resource-group alexandria-rg \
  --name alexandria-db \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 3. Create App Service

```bash
# Create App Service Plan
az appservice plan create \
  --name alexandria-plan \
  --resource-group alexandria-rg \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group alexandria-rg \
  --plan alexandria-plan \
  --name alexandria-api \
  --runtime "DOTNETCORE:10.0"
```

### 4. Configure App Settings

```bash
# Get database connection string
DB_HOST=$(az postgres flexible-server show --resource-group alexandria-rg --name alexandria-db --query fullyQualifiedDomainName -o tsv)

# Set connection string
az webapp config connection-string set \
  --resource-group alexandria-rg \
  --name alexandria-api \
  --connection-string-type PostgreSQL \
  --settings DefaultConnection="Host=$DB_HOST;Database=alexandria;Username=alexandriaadmin;Password=<YourSecurePassword>;SSL Mode=Require"

# Configure other app settings
az webapp config appsettings set \
  --resource-group alexandria-rg \
  --name alexandria-api \
  --settings \
    Jwt__Key="<YourJWTSecretKey>" \
    Jwt__Issuer="Alexandria" \
    Jwt__Audience="AlexandriaUsers" \
    Authentication__Google__ClientId="<YourGoogleClientId>" \
    Authentication__Google__ClientSecret="<YourGoogleClientSecret>"
```

## GitHub Actions Setup

### 1. Get Azure Publish Profile

```bash
az webapp deployment list-publishing-profiles \
  --resource-group alexandria-rg \
  --name alexandria-api \
  --xml > publish-profile.xml
```

### 2. Configure GitHub Secrets

Add these secrets to your GitHub repository (Settings > Secrets and variables > Actions):

- `AZURE_WEBAPP_NAME`: alexandria-api
- `AZURE_WEBAPP_PUBLISH_PROFILE`: (content of publish-profile.xml)

### 3. Enable GitHub Actions

The workflow in `.github/workflows/deploy.yml` will automatically:
1. Build the .NET API
2. Run tests
3. Deploy to Azure on push to main branch

## Aspire Deployment (Alternative)

### Local Development with Aspire

```bash
cd src/Alexandria.AppHost
dotnet run
```

This starts:
- PostgreSQL container
- Alexandria API
- Aspire Dashboard (http://localhost:15888)

### Deploy Aspire to Azure Container Apps

```bash
# Install Aspire tools
dotnet workload install aspire

# Deploy to Azure
cd src/Alexandria.AppHost
azd init
azd up
```

Follow the prompts to:
- Select subscription
- Choose region
- Deploy all services

## Database Migrations

### Run migrations on Azure

```bash
# Update connection string in appsettings.json to point to Azure
cd src/Alexandria.API
dotnet ef database update
```

Or use the Azure CLI:

```bash
# SSH into the app
az webapp ssh --resource-group alexandria-rg --name alexandria-api

# Run migrations
cd site/wwwroot
dotnet Alexandria.API.dll --migrate
```

## Mobile App Deployment

### Update API URL

In `AlexandriaMobile/src/services/api.ts`, update:

```typescript
const API_BASE_URL = 'https://alexandria-api.azurewebsites.net/api';
```

### Build for Production

```bash
cd AlexandriaMobile

# For iOS (requires Mac)
eas build --platform ios

# For Android
eas build --platform android

# For Web
npm run build
```

## Google OAuth Configuration

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials:
   - **Authorized redirect URIs**: 
     - `https://alexandria-api.azurewebsites.net/signin-google`
     - `http://localhost:5000/signin-google` (for local dev)
5. Add Client ID and Secret to Azure App Settings

## Monitoring and Logs

### View Application Logs

```bash
# Stream logs
az webapp log tail \
  --resource-group alexandria-rg \
  --name alexandria-api

# Download logs
az webapp log download \
  --resource-group alexandria-rg \
  --name alexandria-api
```

### Application Insights (Optional)

```bash
# Create Application Insights
az monitor app-insights component create \
  --app alexandria-insights \
  --location eastus \
  --resource-group alexandria-rg

# Get instrumentation key
az monitor app-insights component show \
  --app alexandria-insights \
  --resource-group alexandria-rg \
  --query instrumentationKey -o tsv

# Add to app settings
az webapp config appsettings set \
  --resource-group alexandria-rg \
  --name alexandria-api \
  --settings ApplicationInsights__InstrumentationKey="<key>"
```

## Security Checklist

- [ ] Use Azure Key Vault for secrets
- [ ] Enable HTTPS only
- [ ] Configure CORS properly
- [ ] Set up Azure AD authentication
- [ ] Enable database backups
- [ ] Configure firewall rules
- [ ] Use managed identities
- [ ] Enable logging and monitoring

## Troubleshooting

### API not starting

Check logs:
```bash
az webapp log tail --resource-group alexandria-rg --name alexandria-api
```

### Database connection issues

Verify connection string and firewall rules:
```bash
az postgres flexible-server firewall-rule list \
  --resource-group alexandria-rg \
  --name alexandria-db
```

### Migration issues

Run migrations manually:
```bash
dotnet ef database update --connection "Host=...;Database=alexandria;..."
```

## Cost Optimization

- Use **B1** tier for App Service during development
- Scale to **S1** or higher for production
- Use **Burstable** tier for PostgreSQL
- Enable auto-scaling based on metrics
- Monitor costs in Azure Cost Management

## Backup and Disaster Recovery

### Database Backups

```bash
# Enable automated backups (default: 7 days)
az postgres flexible-server parameter set \
  --resource-group alexandria-rg \
  --server-name alexandria-db \
  --name backup_retention_days \
  --value 30
```

### App Service Backups

```bash
# Create storage account for backups
az storage account create \
  --name alexandriabackups \
  --resource-group alexandria-rg \
  --location eastus \
  --sku Standard_LRS

# Configure backup
az webapp config backup create \
  --resource-group alexandria-rg \
  --webapp-name alexandria-api \
  --backup-name daily-backup \
  --frequency 1d \
  --retention 30
```

## Support

For deployment issues:
- Check Azure documentation
- Review Application Insights logs
- Contact Azure support if needed
