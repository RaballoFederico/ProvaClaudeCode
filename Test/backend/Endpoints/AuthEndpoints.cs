using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "filmapi_refresh_token";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");
        group.RequireRateLimiting("auth");

        group.MapPost("/login", [AllowAnonymous] async (HttpContext context, LoginRequestDTO request, IAuthService authService) =>
        {
            var (response, error) = await authService.LoginAsync(request);
            if (error != null)
            {
                return Results.Unauthorized();
            }

            AppendRefreshTokenCookie(context, response!.RefreshToken);

            return Results.Ok(response);
        });

        group.MapPost("/refresh", [AllowAnonymous] async (HttpContext context, RefreshTokenRequestDTO request, IAuthService authService) =>
        {
            var requestToken = request.RefreshToken;
            if (string.IsNullOrWhiteSpace(requestToken))
            {
                requestToken = context.Request.Cookies[RefreshTokenCookieName] ?? string.Empty;
            }

            var (response, error) = await authService.RefreshAsync(new RefreshTokenRequestDTO
            {
                RefreshToken = requestToken
            });

            if (error != null)
            {
                DeleteRefreshTokenCookie(context);
                return Results.Unauthorized();
            }

            AppendRefreshTokenCookie(context, response!.RefreshToken);

            return Results.Ok(response);
        });

        group.MapPost("/logout", [Authorize] async (HttpContext context, IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out var userIdValue))
            {
                return Results.Unauthorized();
            }

            await authService.LogoutAsync(userIdValue);
            DeleteRefreshTokenCookie(context);
            return Results.Ok(new { message = "Logout effettuato con successo" });
        });

        group.MapPost("/register", [AllowAnonymous] async (RegistrazioneRequestDTO request, IAuthService authService) =>
        {
            var (utenteId, error) = await authService.RegisterAsync(request);
            if (error != null)
            {
                if (error.Contains("gia' in uso", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Conflict(new { message = error });
                }

                if (error.Contains("Ruolo User", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Problem(error);
                }

                return Results.BadRequest(new { message = error });
            }

            return Results.Created($"/auth/me", new { message = "Registrazione completata con successo", utenteId });
        });

        group.MapGet("/me", [Authorize] async (HttpContext context, IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out var userIdValue))
            {
                return Results.Unauthorized();
            }

            var utente = await authService.GetMeAsync(userIdValue);
            if (utente is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(utente);
        });

        group.MapGet("/external/providers", [AllowAnonymous] (IExternalAuthService externalAuthService) =>
        {
            return Results.Ok(externalAuthService.GetEnabledProviders());
        });

        group.MapGet("/external/{provider}/start", [AllowAnonymous] (string provider, HttpContext context, IConfiguration configuration, IExternalAuthService externalAuthService) =>
        {
            var backendBaseUrl = ResolveBackendBaseUrl(context, configuration);
            var returnUrl = context.Request.Query["returnUrl"].FirstOrDefault();

            var (redirectUrl, error) = externalAuthService.CreateAuthorizationUrl(provider, returnUrl, backendBaseUrl);
            if (redirectUrl == null)
            {
                return Results.BadRequest(new { message = error ?? "Provider non configurato" });
            }

            return Results.Ok(new ExternalAuthStartResponseDTO { RedirectUrl = redirectUrl });
        });

        group.MapGet("/external/{provider}/callback", [AllowAnonymous] async (string provider, HttpContext context, IConfiguration configuration, IExternalAuthService externalAuthService) =>
        {
            var backendBaseUrl = ResolveBackendBaseUrl(context, configuration);
            var code = context.Request.Query["code"].FirstOrDefault();
            var state = context.Request.Query["state"].FirstOrDefault();
            var oauthError = context.Request.Query["error"].FirstOrDefault();
            var redirectUrl = await externalAuthService.HandleCallbackAsync(provider, backendBaseUrl, code, state, oauthError);
            return Results.Redirect(redirectUrl);
        });

        group.MapPost("/external/complete", [AllowAnonymous] async (HttpContext context, ExternalAuthCompleteRequestDTO request, IExternalAuthService externalAuthService) =>
        {
            var (response, error) = await externalAuthService.CompleteAsync(request);
            if (response == null)
            {
                return Results.BadRequest(new { message = error ?? "Completamento login esterno non riuscito" });
            }

            AppendRefreshTokenCookie(context, response.RefreshToken);

            return Results.Ok(response);
        });

        group.MapPost("/forgot-password", [AllowAnonymous] async (
            ForgotPasswordRequestDTO request,
            FilmDbContext db,
            IMemoryCache cache,
            IEmailService emailService,
            IConfiguration configuration) =>
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return Results.BadRequest(new { message = "Inserisci una email valida" });
            }

            var user = await db.Utenti.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Attivo);
            if (user == null)
            {
                return Results.BadRequest(new
                {
                    message = "Nessun account trovato con questa email"
                });
            }

            var token = CreateRandomUrlSafeToken();
            cache.Set($"pwd-reset:{token}", user.Id, TimeSpan.FromMinutes(30));

            var frontendBaseUrl = ResolveFrontendBaseUrl(configuration, request.ReturnUrl);
            var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/login.html?resetToken={Uri.EscapeDataString(token)}";

            var html = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.45;color:#1f2937'>
                        <h2 style='margin-bottom:8px'>Reset password FilmHub</h2>
                        <p>Abbiamo ricevuto una richiesta di reimpostazione password.</p>
                        <p>Per impostare una nuova password clicca qui:</p>
                        <p><a href='{resetUrl}' style='display:inline-block;padding:10px 16px;background:#e50914;color:#fff;text-decoration:none;border-radius:8px'>Reimposta password</a></p>
                        <p>Il link scade tra 30 minuti.</p>
                        <p>Se non hai richiesto tu il reset, puoi ignorare questa email.</p>
                    </div>";

            await emailService.InviaConfermaAcquistoAsync(
                user.Email,
                "FilmHub - Reimpostazione password",
                html);

            return Results.Ok(new
            {
                message = "Email trovata: ti abbiamo inviato il link per il reset password."
            });
        });

        group.MapPost("/reset-password", [AllowAnonymous] async (
            ResetPasswordRequestDTO request,
            IMemoryCache cache,
            IAuthService authService) =>
        {
            var token = (request.Token ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.BadRequest(new { message = "Token non valido" });
            }

            if (!cache.TryGetValue($"pwd-reset:{token}", out int userId) || userId <= 0)
            {
                return Results.BadRequest(new { message = "Token scaduto o non valido" });
            }

            var (success, error) = await authService.SetPasswordAsync(userId, request.NewPassword);
            if (!success)
            {
                if (string.Equals(error, "WEAK_PASSWORD", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { message = "Password troppo debole: usa almeno 8 caratteri, con maiuscola, minuscola, numero e simbolo" });
                }

                return Results.BadRequest(new { message = "Impossibile aggiornare la password" });
            }

            cache.Remove($"pwd-reset:{token}");
            return Results.Ok(new { message = "Password aggiornata con successo" });
        });

        return app;
    }

    private static string ResolveBackendBaseUrl(HttpContext context, IConfiguration configuration)
    {
        var configured = Environment.GetEnvironmentVariable("EXTERNAL_AUTH_BACKEND_BASE_URL")
            ?? configuration["ExternalAuth:BackendBaseUrl"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        return $"{context.Request.Scheme}://{context.Request.Host}";
    }

    private static string ResolveFrontendBaseUrl(IConfiguration configuration, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            try
            {
                var parsed = new Uri(returnUrl);
                return $"{parsed.Scheme}://{parsed.Authority}";
            }
            catch
            {
                // fallback below
            }
        }

        var configured = Environment.GetEnvironmentVariable("EXTERNAL_AUTH_FRONTEND_BASE_URL")
            ?? configuration["ExternalAuth:DefaultReturnUrl"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var parsed = new Uri(configured);
                return $"{parsed.Scheme}://{parsed.Authority}";
            }
            catch
            {
                return configured.TrimEnd('/');
            }
        }

        return "http://localhost:5002";
    }

    private static string CreateRandomUrlSafeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static void AppendRefreshTokenCookie(HttpContext context, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var isHttps = context.Request.IsHttps;

        context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(7)
        });
    }

    private static void DeleteRefreshTokenCookie(HttpContext context)
    {
        var isHttps = context.Request.IsHttps;

        context.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
