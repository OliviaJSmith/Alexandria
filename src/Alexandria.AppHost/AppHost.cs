var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Key Vault for configuration management
var keyVault = builder.AddAzureKeyVault("keyvault");

// Add PostgreSQL database with data persistence
var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume("alexandria-postgres-data") // Persist data between restarts
    .WithPgAdmin();

var alexandriaDb = postgres.AddDatabase("alexandria");

// Add the API project
var api = builder
    .AddProject<Projects.Alexandria_API>("alexandria-api")
    .WithReference(alexandriaDb)
    .WithReference(keyVault)
    .WithExternalHttpEndpoints();

// Add the Mobile app (Expo/React Native web)
// Use Dockerfile for production deployment, npm for local development
if (builder.ExecutionContext.IsPublishMode)
{
    builder
        .AddDockerfile("alexandria-mobile", "../Alexandria.Mobile")
        .WithHttpEndpoint(targetPort: 80)
        .WithExternalHttpEndpoints();
}
else
{
    builder
        .AddNpmApp("alexandria-mobile", "../Alexandria.Mobile", "web")
        .WithHttpEndpoint(targetPort: 8081)
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
