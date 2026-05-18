// DOC: File backend 'Program': parte del runtime API .NET dell'app.
using System.Text;
using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using FilmAPI.Data;
using FilmAPI.Endpoints;
using FilmAPI.Services;
using FilmAPI.Services.Interfaces;
using Stripe;

var aspnetEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.Equals(aspnetEnvironment, "Development", StringComparison.OrdinalIgnoreCase)
    || string.Equals(aspnetEnvironment, "Staging", StringComparison.OrdinalIgnoreCase)
    || string.IsNullOrWhiteSpace(aspnetEnvironment))
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing") ||
    Environment.GetEnvironmentVariable("ASPNETCORE_TESTING") == "true";

if (!isTesting)
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "filmhub-db.internal.delightfuldune-f7916078.francecentral.azurecontainerapps.io";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "filmapi_db";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
    var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";

    builder.Services.AddDbContext<FilmDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.Parse("8.0.29-mysql")));
}

var startupValidation = ValidateStartupConfiguration(builder.Configuration, builder.Environment, isTesting);

// Configurazione JWT
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ??
    builder.Configuration["Jwt:SecretKey"] ??
    string.Empty;

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
builder.Services.AddScoped<IAccountActionTokenService, AccountActionTokenService>();
builder.Services.AddScoped<IUserSecurityAuditService, UserSecurityAuditService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ITMDBService, TMDBService>();
builder.Services.AddSingleton<TMDBFilmSyncService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TMDBFilmSyncService>());
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
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 4,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("webhook", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var configuredOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'))
            .Where(o => Uri.TryCreate(o, UriKind.Absolute, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var isProduction = builder.Environment.IsProduction();
        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (configuredOrigins.Length > 0)
            {
                var normalizedOrigin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                return configuredOrigins.Any(o => string.Equals(o, normalizedOrigin, StringComparison.OrdinalIgnoreCase));
            }

            if (isProduction)
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttps
                && uri.Host.Contains("azurecontainerapps.io", StringComparison.OrdinalIgnoreCase);
        })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddSwaggerDocument();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    var trustedProxies = (Environment.GetEnvironmentVariable("TRUSTED_PROXIES") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var proxy in trustedProxies)
    {
        if (IPAddress.TryParse(proxy, out var ip))
        {
            options.KnownProxies.Add(ip);
        }
    }
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseResponseCompression();

if (startupValidation.Errors.Count > 0)
{
    foreach (var error in startupValidation.Errors)
    {
        app.Logger.LogError("Configurazione startup non valida: {Message}", error);
    }

    throw new InvalidOperationException("Configurazione applicativa non valida. Correggi i valori richiesti e riavvia.");
}

foreach (var warning in startupValidation.Warnings)
{
    app.Logger.LogWarning("Configurazione startup: {Message}", warning);
}

app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    var cspConnectSources = BuildConnectSrcDirective(builder.Environment);

    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] =
        $"default-src 'self'; img-src 'self' data: https:; script-src 'self' https://cdn.tailwindcss.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; connect-src {cspConnectSources}; frame-ancestors 'none'; base-uri 'self'; form-action 'self';";

    await next();
});

app.Use(async (context, next) =>
{
    var startedAt = DateTime.UtcNow;
    await next();

    var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
    var statusCode = context.Response.StatusCode;
    var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";

    if (statusCode >= 500)
    {
        app.Logger.LogError("{Method} {Path} => {StatusCode} in {ElapsedMs} ms (trace: {TraceId})",
            context.Request.Method,
            path,
            statusCode,
            Math.Round(elapsedMs, 2),
            context.TraceIdentifier);
    }
    else if (statusCode >= 400)
    {
        app.Logger.LogWarning("{Method} {Path} => {StatusCode} in {ElapsedMs} ms (trace: {TraceId})",
            context.Request.Method,
            path,
            statusCode,
            Math.Round(elapsedMs, 2),
            context.TraceIdentifier);
    }
});

app.UseRateLimiter();

// Middleware di autenticazione e autorizzazione
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", async (IServiceProvider services) =>
{
    var checks = new List<object>();
    var status = "Healthy";

    if (!isTesting)
    {
        var started = DateTime.UtcNow;
        var dbOk = false;
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
            dbOk = await db.Database.CanConnectAsync();
        }
        catch
        {
            dbOk = false;
        }

        var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
        checks.Add(new
        {
            name = "database",
            status = dbOk ? "Healthy" : "Unhealthy",
            duration = Math.Round(elapsed, 2)
        });

        if (!dbOk)
        {
            status = "Unhealthy";
        }
    }

    return status == "Healthy"
        ? Results.Ok(new { status, checks })
        : Results.Json(new { status, checks }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Seed dati iniziali (solo se non in testing)
if (!isTesting)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await db.Database.MigrateAsync();
        // Hotfix compatibilita schema:
        // una migration storica vuota puo lasciare assente la colonna ConsensoNewsletter,
        // causando HTTP 500 sul login quando EF prova a selezionarla.
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE `utenti` ADD COLUMN IF NOT EXISTS `ConsensoNewsletter` TINYINT(1) NOT NULL DEFAULT 0;");
        // Hotfix compatibilita schema newsletter:
        // la migration AddSubscriptionsAndNewsletter risulta vuota in alcuni ambienti
        // e puo lasciare assente la tabella newsletter_campagne (HTTP 500 su storico campagne).
        await db.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE IF NOT EXISTS `newsletter_campagne` (
                `Id` INT NOT NULL AUTO_INCREMENT,
                `Oggetto` VARCHAR(160) NOT NULL,
                `HtmlBody` LONGTEXT NOT NULL,
                `CreatoDaUtenteId` INT NOT NULL,
                `DataInvio` DATETIME(6) NOT NULL,
                `DestinatariCount` INT NOT NULL,
                CONSTRAINT `PK_newsletter_campagne` PRIMARY KEY (`Id`),
                INDEX `IX_newsletter_campagne_DataInvio` (`DataInvio`),
                INDEX `IX_newsletter_campagne_CreatoDaUtenteId` (`CreatoDaUtenteId`),
                CONSTRAINT `FK_newsletter_campagne_utenti_CreatoDaUtenteId`
                    FOREIGN KEY (`CreatoDaUtenteId`) REFERENCES `utenti` (`Id`) ON DELETE RESTRICT
            );");
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
app.MapDashboardEndpoints();
app.MapAcquistoEndpoints();
app.MapCreditoEndpoints();
app.MapValidazioneEndpoints();
app.MapNotificheEndpoints();
app.MapStripeWebhookEndpoints();
app.MapAbbonamentiEndpoints();
app.MapNewsletterEndpoints();

app.MapGroup("/registi").MapRegistiEndpoints();
app.MapGroup("/films").MapFilmsEndpoints();
app.MapGroup("/cinemas").MapCinemasEndpoints();
app.MapGroup("/proiezioni").MapProiezioniEndpoints();

app.Run();

static (List<string> Errors, List<string> Warnings) ValidateStartupConfiguration(IConfiguration configuration, IWebHostEnvironment environment, bool isTesting)
{
    var errors = new List<string>();
    var warnings = new List<string>();

    if (isTesting)
    {
        return (errors, warnings);
    }

    var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? configuration["Jwt:SecretKey"]
        ?? string.Empty;

    if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 || IsPlaceholder(jwtSecret))
    {
        errors.Add("JWT_SECRET_KEY non configurata correttamente (minimo 32 caratteri, no placeholder).");
    }

    var stripeSecret = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
        ?? configuration["Stripe:SecretKey"]
        ?? string.Empty;

    if (!string.IsNullOrWhiteSpace(stripeSecret) && IsPlaceholder(stripeSecret))
    {
        var message = "STRIPE_SECRET_KEY contiene un placeholder non valido.";
        if (environment.IsProduction()) errors.Add(message); else warnings.Add(message);
    }
    else if (string.IsNullOrWhiteSpace(stripeSecret))
    {
        warnings.Add("STRIPE_SECRET_KEY non impostata: i pagamenti carta saranno disabilitati.");
    }

    var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
        ?? configuration["Stripe:WebhookSecret"]
        ?? string.Empty;

    if (string.IsNullOrWhiteSpace(webhookSecret))
    {
        warnings.Add("STRIPE_WEBHOOK_SECRET non impostata: webhook Stripe non verificabile.");
    }
    else if (IsPlaceholder(webhookSecret))
    {
        var message = "STRIPE_WEBHOOK_SECRET contiene un placeholder non valido.";
        if (environment.IsProduction()) errors.Add(message); else warnings.Add(message);
    }

    var backendBaseUrl = Environment.GetEnvironmentVariable("EXTERNAL_AUTH_BACKEND_BASE_URL")
        ?? configuration["ExternalAuth:BackendBaseUrl"];
    if (!string.IsNullOrWhiteSpace(backendBaseUrl)
        && !Uri.TryCreate(backendBaseUrl, UriKind.Absolute, out _))
    {
        errors.Add("ExternalAuth:BackendBaseUrl non e un URL assoluto valido.");
    }

    if (environment.IsProduction())
    {
        var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (corsOrigins.Length == 0)
        {
            errors.Add("CORS_ALLOWED_ORIGINS non impostata in produzione.");
        }

        var allowedReturnUrls = configuration.GetSection("ExternalAuth:AllowedReturnUrls").Get<string[]>() ?? Array.Empty<string>();
        if (allowedReturnUrls.Any(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("In produzione sono presenti AllowedReturnUrls non HTTPS per ExternalAuth.");
        }
    }

    return (errors, warnings);
}

static bool IsPlaceholder(string value)
{
    var trimmed = value.Trim();
    if (string.IsNullOrWhiteSpace(trimmed)) return true;

    return trimmed.Contains("your-", StringComparison.OrdinalIgnoreCase)
           || trimmed.Contains("example", StringComparison.OrdinalIgnoreCase)
           || trimmed.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
           || trimmed.Contains("...", StringComparison.OrdinalIgnoreCase);
}

static string BuildConnectSrcDirective(IWebHostEnvironment environment)
{
    var configured = Environment.GetEnvironmentVariable("CSP_CONNECT_SRC");
    if (!string.IsNullOrWhiteSpace(configured))
    {
        return configured.Trim();
    }

    var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "'self'",
        "https://api.stripe.com"
    };

    var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var origin in corsOrigins)
    {
        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            origins.Add(uri.GetLeftPart(UriPartial.Authority));
        }
    }

    if (!environment.IsProduction())
    {
        origins.Add("https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io");
        origins.Add("https://filmhub-frontend.delightfuldune-f7916078.francecentral.azurecontainerapps.io");
    }

    return string.Join(' ', origins);
}

public partial class Program { }

