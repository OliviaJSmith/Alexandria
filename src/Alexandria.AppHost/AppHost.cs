var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var alexandriaDb = postgres.AddDatabase("alexandria");

// Run database migrations as a visible job
var migrations = builder.AddExecutable(
    name: "migrations",
    command: "dotnet",
    workingDirectory: "../Alexandria.API",
    args: ["run", "--no-build", "--", "MigrationRunner"])
    .WithReference(alexandriaDb)
    .WaitFor(alexandriaDb);

// Add the API project - wait for migrations to complete
var api = builder.AddProject<Projects.Alexandria_API>("alexandria-api")
    .WithReference(alexandriaDb)
    .WaitFor(migrations);

builder.Build().Run();

