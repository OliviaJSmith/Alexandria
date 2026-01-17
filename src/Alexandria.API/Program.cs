using Alexandria.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Check if running as migration runner
if (args.Length > 0 && args[0] == "MigrationRunner")
{
    return await RunMigrations(args);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure PostgreSQL database
var connectionString = builder.Configuration.GetConnectionString("alexandria") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=alexandria;Username=postgres;Password=postgres";

// Ensure SSL is disabled for local development (Docker container doesn't have SSL configured)
if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) && 
    !connectionString.Contains("sslmode", StringComparison.OrdinalIgnoreCase))
{
    connectionString += ";SSL Mode=Disable";
}

builder.Services.AddDbContext<AlexandriaDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSecretKeyForAuthenticationOfAlexandria2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Alexandria";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AlexandriaUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
});

builder.Services.AddAuthorization();

// Add CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:8081", "http://localhost:19006" }; // Default to Expo dev ports

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

return 0; // Normal exit code

// Migration runner method
static async Task<int> RunMigrations(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure PostgreSQL database
    var connectionString = builder.Configuration.GetConnectionString("alexandria") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=alexandria;Username=postgres;Password=postgres";

    // Ensure SSL is disabled for local development
    if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) && 
        !connectionString.Contains("sslmode", StringComparison.OrdinalIgnoreCase))
    {
        connectionString += ";SSL Mode=Disable";
    }

    builder.Services.AddDbContext<AlexandriaDbContext>(options =>
        options.UseNpgsql(connectionString));

    var app = builder.Build();

    // Apply migrations
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AlexandriaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        var retryCount = 0;
        const int maxRetries = 10;
        
        while (retryCount < maxRetries)
        {
            try
            {
                logger.LogInformation("🔄 Applying database migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("✅ Database migrations applied successfully.");
                return 0; // Success
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    logger.LogError(ex, "❌ Failed to apply migrations after {RetryCount} attempts.", retryCount);
                    return 1; // Failure
                }
                
                logger.LogWarning("⚠️ Failed to apply migrations (attempt {RetryCount}/{MaxRetries}). Retrying in 2 seconds...", 
                    retryCount, maxRetries);
                await Task.Delay(2000);
            }
        }
    }

    return 0;
}
