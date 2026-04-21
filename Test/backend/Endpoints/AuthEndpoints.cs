using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login", [AllowAnonymous] async (LoginRequestDTO request, HttpContext context, IAuthService authService) =>
        {
            var (ipAddress, userAgent) = GetRequestContext(context);
            var (response, error) = await authService.LoginAsync(request, ipAddress, userAgent);
            if (error != null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        });

        group.MapPost("/refresh", [AllowAnonymous] async (RefreshTokenRequestDTO request, HttpContext context, IAuthService authService) =>
        {
            var (ipAddress, userAgent) = GetRequestContext(context);
            var (response, error) = await authService.RefreshAsync(request, ipAddress, userAgent);
            if (error != null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        });

        group.MapPost("/logout", [Authorize] async (HttpContext context, IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out var userIdValue))
            {
                return Results.Unauthorized();
            }

            var (ipAddress, userAgent) = GetRequestContext(context);
            await authService.LogoutAsync(userIdValue, ipAddress, userAgent);
            return Results.Ok(new { message = "Logout effettuato con successo" });
        });

        group.MapPost("/logout/all", [Authorize] async (HttpContext context, IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out var userIdValue))
            {
                return Results.Unauthorized();
            }

            var (ipAddress, userAgent) = GetRequestContext(context);
            await authService.LogoutAllAsync(userIdValue, ipAddress, userAgent);
            return Results.Ok(new { message = "Logout globale effettuato con successo" });
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

        group.MapPost("/change-password", [Authorize] async (HttpContext context, ChangePasswordRequestDTO request, IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out var userIdValue))
            {
                return Results.Unauthorized();
            }

            var (ok, error) = await authService.ChangePasswordAsync(userIdValue, request);
            if (!ok)
            {
                return error switch
                {
                    "NOT_FOUND" => Results.NotFound(),
                    "PASSWORD_NOT_SET" => Results.BadRequest(new { message = "Password non impostata per questo account" }),
                    "INVALID_CREDENTIALS" => Results.Unauthorized(),
                    "PASSWORD_TOO_SHORT" => Results.BadRequest(new { message = "Password deve essere di almeno 8 caratteri" }),
                    _ => Results.BadRequest(new { message = "Richiesta non valida" })
                };
            }

            return Results.NoContent();
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

        group.MapPost("/external/complete", [AllowAnonymous] async (ExternalAuthCompleteRequestDTO request, IExternalAuthService externalAuthService) =>
        {
            var (response, error) = await externalAuthService.CompleteAsync(request);
            if (response == null)
            {
                return Results.BadRequest(new { message = error ?? "Completamento login esterno non riuscito" });
            }

            return Results.Ok(response);
        });

        group.MapGet("/roles", [Authorize(Roles = "Admin")] async (FilmDbContext db) =>
        {
            var roles = await db.Ruoli
                .OrderBy(r => r.Nome)
                .Select(r => new { r.Id, r.Nome, r.Descrizione })
                .ToListAsync();
            return Results.Ok(roles);
        });

        return app;
    }

    private static (string? ipAddress, string? userAgent) GetRequestContext(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        return (ipAddress, string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
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
}
