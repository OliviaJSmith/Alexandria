var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var alexandriaDb = postgres.AddDatabase("alexandria");

// Add the API project
var api = builder.AddProject<Projects.Alexandria_API>("alexandria-api")
    .WithReference(alexandriaDb);

builder.Build().Run();

