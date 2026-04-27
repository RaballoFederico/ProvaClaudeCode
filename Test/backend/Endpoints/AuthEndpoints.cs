using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "filmapi_refresh_token";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

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
