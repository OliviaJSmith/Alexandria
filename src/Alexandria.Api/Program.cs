using Alexandria.Api.Data;
using Alexandria.Api.Options;
using Alexandria.Api.Repositories;
using System.Text.Json.Serialization;
using Alexandria.Api.Services;
using Alexandria.Api.Services.BookCatalog;
using Alexandria.Api.Services.Notifications;
using Alexandria.Api.Services.Scanning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => o.HasValidSigningKey(), $"Jwt:SigningKey must be base64 and at least {JwtOptions.MinimumSigningKeyBytes} bytes.")
    .ValidateOnStart();

builder.Services.AddOptions<GoogleOptions>()
    .Bind(builder.Configuration.GetSection(GoogleOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ---------- Data ----------

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
builder.Services.AddDbContext<AlexandriaDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

// ---------- Services ----------

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(Random.Shared);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUsernameGenerator, UsernameGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddHttpClient<IGoogleTokenValidator, GoogleTokenValidator>(client => client.Timeout = TimeSpan.FromSeconds(10));

// External book catalogs, queried in registration order: Open Library first, Google Books as fallback.
builder.Services.Configure<GoogleBooksOptions>(builder.Configuration.GetSection(GoogleBooksOptions.SectionName));
builder.Services.AddHttpClient<OpenLibraryCatalogSource>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Alexandria/1.0");
});
builder.Services.AddHttpClient<GoogleBooksCatalogSource>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddTransient<IBookCatalogSource>(sp => sp.GetRequiredService<OpenLibraryCatalogSource>());
builder.Services.AddTransient<IBookCatalogSource>(sp => sp.GetRequiredService<GoogleBooksCatalogSource>());
builder.Services.AddScoped<IBookLookupService, BookLookupService>();

// Book scanning: Azure Vision reads the photo, Azure OpenAI names the books. Off when not configured.
builder.Services.Configure<AzureVisionOptions>(builder.Configuration.GetSection(AzureVisionOptions.SectionName));
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName));
builder.Services.AddHttpClient<ITextRecognizer, AzureVisionTextRecognizer>(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient<IBookTextInterpreter, AzureOpenAIBookTextInterpreter>(client => client.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddScoped<IBookScanService, BookScanService>();

// Web Push notifications (e.g. borrow requests). Off when the VAPID keys aren't configured.
builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection(WebPushOptions.SectionName));
builder.Services.AddHttpClient<IWebPushSender, WebPushSender>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<INotificationService, NotificationService>();

// ---------- Authentication ----------

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwt.GetSigningKeyBytes()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// ---------- Web ----------

// In Azure, App Service's platform CORS handles the Static Web App origin; this covers local development.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AlexandriaDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
