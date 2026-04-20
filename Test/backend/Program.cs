using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FilmAPI.Data;
using FilmAPI.Endpoints;
using FilmAPI.Services;
using FilmAPI.Services.Interfaces;
using Stripe;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing") ||
    Environment.GetEnvironmentVariable("ASPNETCORE_TESTING") == "true";

if (!isTesting)
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "filmapi_db";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
    var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";

    builder.Services.AddDbContext<FilmDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.Parse("8.0.29-mysql")));
}

// Configurazione JWT
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ??
    builder.Configuration["Jwt:SecretKey"] ??
    "your-super-secret-key-min-32-characters-for-jwt";

builder.Services.AddSingleton(new JwtService(builder.Configuration));
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();
builder.Services.AddScoped<ISalaService, SalaService>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<IBigliettoService, BigliettoService>();
builder.Services.AddScoped<ICreditoService, CreditoService>();
builder.Services.AddScoped<IPagamentoService, PagamentoService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddHostedService<PrenotazioneTempCleanupService>();

StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
    ?? builder.Configuration["Stripe:SecretKey"]
    ?? string.Empty;

if (StripeConfiguration.ApiKey.StartsWith("sk_test_", StringComparison.OrdinalIgnoreCase) && StripeConfiguration.ApiKey.Contains("..."))
{
    StripeConfiguration.ApiKey = string.Empty;
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? "FilmAPI",
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? "FilmFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("PowerUserOrAdmin", policy => policy.RequireRole("Admin", "PowerUser"));
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5001",
                "http://localhost:5285",
                "https://localhost:7217")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Middleware di autenticazione e autorizzazione
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Seed dati iniziali (solo se non in testing)
if (!isTesting)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await DbInitializer.InitializeAsync(db);
    }
}

// Registrazione endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapAdminEndpoints();
app.MapCategorieEndpoints();
app.MapSaleEndpoints();
app.MapShowsEndpoints();
app.MapProgrammazioneEndpoints();
app.MapAcquistoEndpoints();
app.MapCreditoEndpoints();
app.MapValidazioneEndpoints();

app.MapGroup("/registi").MapRegistiEndpoints();
app.MapGroup("/films").MapFilmsEndpoints();
app.MapGroup("/cinemas").MapCinemasEndpoints();
app.MapGroup("/proiezioni").MapProiezioniEndpoints();

app.Run();

public partial class Program { }
