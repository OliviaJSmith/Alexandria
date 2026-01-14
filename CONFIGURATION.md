# Alexandria Configuration Guide

## Environment Variables

### Backend API (.NET)

Create `appsettings.Development.json` or set environment variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=alexandria;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "YourSecretKeyForAuthenticationOfAlexandria2026MinimumLength32Chars",
    "Issuer": "Alexandria",
    "Audience": "AlexandriaUsers",
    "ExpirationMinutes": 1440
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    }
  }
}
```

### Mobile App (React Native)

Create `AlexandriaMobile/.env`:

```env
API_BASE_URL=http://localhost:5000/api
GOOGLE_CLIENT_ID=your-google-client-id
```

Update `src/services/api.ts` with your API URL.

## Google OAuth Setup

### 1. Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project: "Alexandria"
3. Enable APIs:
   - Google+ API
   - Google Identity Services

### 2. Configure OAuth Consent Screen

1. Go to APIs & Services > OAuth consent screen
2. Choose "External" for user type
3. Fill in application details:
   - App name: Alexandria
   - User support email: your-email@example.com
   - Developer contact: your-email@example.com
4. Add scopes:
   - `userinfo.email`
   - `userinfo.profile`
   - `openid`

### 3. Create OAuth Credentials

1. Go to APIs & Services > Credentials
2. Click "Create Credentials" > "OAuth client ID"
3. Application type: Web application
4. Name: Alexandria API
5. Authorized redirect URIs:
   - `http://localhost:5000/signin-google` (development)
   - `https://your-domain.azurewebsites.net/signin-google` (production)
6. Save Client ID and Client Secret

### 4. Mobile OAuth Setup

1. Create another OAuth client ID
2. Application type: 
   - iOS: iOS
   - Android: Android
   - Web: Web application (for Expo web)
3. Configure bundle IDs and package names as needed

## Database Setup

### PostgreSQL Local Development

#### Using Docker

```bash
docker run --name alexandria-postgres \
  -e POSTGRES_DB=alexandria \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:15
```

#### Manual Installation

1. Install PostgreSQL 15+
2. Create database:
   ```sql
   CREATE DATABASE alexandria;
   ```
3. Update connection string in appsettings.json

### Run Migrations

```bash
cd src/Alexandria.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Development Setup

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL 15+
- Docker (optional, for Aspire)
- Expo CLI: `npm install -g expo-cli`

### Backend

```bash
# Restore packages
dotnet restore

# Run migrations
cd src/Alexandria.API
dotnet ef database update

# Run API
dotnet run
```

API will be available at `https://localhost:5001`

### Mobile App

```bash
cd AlexandriaMobile

# Install dependencies
npm install

# Start development server
npm start

# Run on specific platform
npm run ios      # iOS (Mac only)
npm run android  # Android
npm run web      # Web browser
```

### Aspire (Recommended)

```bash
# Run entire stack with Aspire
cd src/Alexandria.AppHost
dotnet run
```

This starts:
- PostgreSQL in Docker
- Alexandria API
- Aspire Dashboard at http://localhost:15888

## Security Configuration

### JWT Token Configuration

**Important**: Change the JWT secret key in production!

```json
{
  "Jwt": {
    "Key": "CHANGE_THIS_TO_A_SECURE_RANDOM_STRING_AT_LEAST_32_CHARACTERS_LONG",
    "Issuer": "Alexandria",
    "Audience": "AlexandriaUsers",
    "ExpirationMinutes": 1440
  }
}
```

Generate a secure key:

```bash
# PowerShell
$bytes = New-Object byte[] 32; (New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes); [Convert]::ToBase64String($bytes)

# Linux/Mac
openssl rand -base64 32
```

### CORS Configuration

Update `Program.cs` for production:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://your-app-domain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### HTTPS Configuration

For production, use HTTPS only:

```csharp
app.UseHttpsRedirection();
app.UseHsts(); // Add this for production
```

## Feature Flags

### Image Search (OCR)

The image search feature requires integration with an OCR service:

**Options:**
1. Azure Computer Vision API
2. Google Cloud Vision API
3. AWS Textract
4. Tesseract (self-hosted)

**Implementation:**

```csharp
// Add to appsettings.json
{
  "OCR": {
    "Provider": "Azure",
    "ApiKey": "your-api-key",
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/"
  }
}
```

Update `BooksController.SearchBooksByImage` to use the OCR service.

## Performance Tuning

### Database Indexing

Already configured in `AlexandriaDbContext`:
- User email and GoogleId
- Book ISBN
- Library and LibraryBook relationships

### API Response Caching

Add response caching for book searches:

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Connection Pooling

PostgreSQL connection pooling is handled by Npgsql by default.

Optimize pool size:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=alexandria;Username=postgres;Password=postgres;Maximum Pool Size=50;Minimum Pool Size=5"
  }
}
```

## Monitoring and Logging

### Application Insights (Azure)

```bash
# Add package
dotnet add package Microsoft.ApplicationInsights.AspNetCore

# Configure in Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

### Serilog (Alternative)

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

## Testing

### Backend Tests

Create test project:

```bash
dotnet new xunit -n Alexandria.API.Tests
dotnet sln add Alexandria.API.Tests/Alexandria.API.Tests.csproj
```

### Mobile Tests

```bash
cd AlexandriaMobile
npm install --save-dev @testing-library/react-native jest
```

## Troubleshooting

### Database Connection Issues

Check PostgreSQL is running:
```bash
docker ps  # If using Docker
pg_isready  # If installed locally
```

### CORS Issues

Ensure CORS is configured to allow your mobile app origin.

### Authentication Issues

Verify:
1. JWT secret key is consistent
2. Google OAuth credentials are correct
3. Redirect URIs match configuration

### Migration Issues

Reset database:
```bash
dotnet ef database drop
dotnet ef database update
```

## Next Steps

1. Configure Google OAuth credentials
2. Set up PostgreSQL database
3. Update API URL in mobile app
4. Run database migrations
5. Test the application
6. Deploy to Azure (see DEPLOYMENT.md)

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [React Native Documentation](https://reactnative.dev)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
