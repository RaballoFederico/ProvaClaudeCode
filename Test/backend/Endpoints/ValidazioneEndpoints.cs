// DOC: ValidazioneEndpoints - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Endpoint 'ValidazioneEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class ValidazioneEndpoints
{
    // DOC-METHOD: 'MapValidazioneEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static IEndpointRouteBuilder MapValidazioneEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/validazione");

        group.MapGet("/", [Authorize(Roles = "Admin,PowerUser")] () => Results.Ok(new { message = "Pagina validazione disponibile" }));

        group.MapPost("/verifica", [Authorize(Roles = "Admin,PowerUser")] async (string codiceHash, IBigliettoService bigliettoService) =>
        {
            var info = await bigliettoService.GetBigliettoPerValidazioneAsync(codiceHash);
            return info is null ? Results.NotFound() : Results.Ok(info);
        });

        group.MapGet("/qr/{codiceHash}", [Authorize(Roles = "Admin,PowerUser")] async (string codiceHash, IBigliettoService bigliettoService) =>
        {
            var info = await bigliettoService.GetBigliettoPerValidazioneAsync(codiceHash);
            return info is null ? Results.NotFound() : Results.Ok(info);
        });

        group.MapPost("/conferma", [Authorize(Roles = "Admin,PowerUser")] async (HttpContext ctx, string codiceHash, int cinemaId, IBigliettoService bigliettoService) =>
        {
            var operatoreId = GetUserId(ctx);
            if (operatoreId is null) return Results.Unauthorized();
            var ok = await bigliettoService.ValidaBigliettoAsync(codiceHash, operatoreId.Value, cinemaId);
            return ok ? Results.Ok(new { validato = true }) : Results.BadRequest(new { validato = false });
        });

        group.MapGet("/{codice}/info", [Authorize(Roles = "Admin,PowerUser")] async (string codice, IBigliettoService bigliettoService) =>
        {
            var info = await bigliettoService.GetBigliettoPerValidazioneAsync(codice);
            return info is null ? Results.NotFound() : Results.Ok(info);
        });

        return app;
    }

    // DOC-METHOD: 'GetUserId' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }
}


