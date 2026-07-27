using Alexandria.API.Data;
using Alexandria.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load local settings override (gitignored)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Load secrets from Azure Key Vault when a vault is referenced (i.e. when deployed
// via Aspire/azd). Locally no "keyvault" connection string exists, so this is skipped
// and secrets come from appsettings.Local.json instead.
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("keyvault")))
{
    builder.Configuration.AddAzureKeyVaultSecrets("keyvault");
}

// Add services to the container.
builder.Services.AddControllers();

// Register application services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBookLookupService, BookLookupService>();
builder.Services.AddScoped<IOcrService, AzureOcrService>();

// Configure Azure Computer Vision options with validation.
// Validated on first use (not at startup) so the app can run without OCR configured;
// scan endpoints will fail with a clear validation error until secrets are provided.
builder
    .Services.AddOptions<AzureOpenAiOptions>()
    .Bind(builder.Configuration.GetSection(AzureOpenAiOptions.SectionName))
    .ValidateDataAnnotations();

// Configure HTTP clients for external APIs
builder.Services.AddHttpClient(
    "OpenLibrary",
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["OpenLibrary:BaseUrl"] ?? "https://openlibrary.org"
        );
        client.DefaultRequestHeaders.Add(
            "User-Agent",
            "Alexandria-Library-App/1.0 (https://github.com/OliviaJSmith/Alexandria)"
        );
    }
);

builder.Services.AddHttpClient(
    "GoogleBooks",
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["GoogleBooks:BaseUrl"] ?? "https://www.googleapis.com/books/v1"
        );
    }
);

// Add default HttpClient for general use (e.g., Google OAuth verification)
builder.Services.AddHttpClient();

// Configure PostgreSQL database with Aspire service discovery
// When running in Aspire, this will use service discovery to connect to the "alexandria" database
// When running standalone, it will fall back to the connection string
builder.AddNpgsqlDbContext<AlexandriaDbContext>("alexandria");

// Configure Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSecretKeyForAuthenticationOfAlexandria2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Alexandria";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AlexandriaUsers";

var authBuilder = builder
    .Services.AddAuthentication(options =>
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

// Only add Google authentication if credentials are configured
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

builder.Services.AddAuthorization();

// Add CORS
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[]
    {
        "http://localhost:8081",
        "http://localhost:19006",
        "http://localhost:61480",
        "http://localhost:50484",
        // Origin headers never include a trailing slash
        "https://alexandria-mobile.delightfulocean-4bd7ffeb.eastus2.azurecontainerapps.io",
        "https://alexandria-api.delightfulocean-4bd7ffeb.eastus2.azurecontainerapps.io"
    }; // Default to Expo dev ports

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }
    );
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

// Add Cross-Origin headers for browser compatibility
// app.Use(async (context, next) =>
// {
//     context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
//     context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "credentialless");
//     await next();
// });

// CORS must be before other middleware to handle preflight requests
app.UseCors("AllowAll");

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AlexandriaDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();
