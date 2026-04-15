using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login", [AllowAnonymous] async (LoginRequestDTO request, IAuthService authService) =>
        {
            var (response, error) = await authService.LoginAsync(request);
            if (error != null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        });

        group.MapPost("/refresh", [AllowAnonymous] async (RefreshTokenRequestDTO request, IAuthService authService) =>
        {
            var (response, error) = await authService.RefreshAsync(request);
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

            await authService.LogoutAsync(userIdValue);
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

        return app;
    }
}
