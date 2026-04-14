using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class CreditoEndpoints
{
    public static IEndpointRouteBuilder MapCreditoEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/admin/credito").RequireAuthorization("PowerUserOrAdmin");

        adminGroup.MapPost("/ricarica", async (HttpContext ctx, RicaricaCreditoDTO dto, ICreditoService creditoService) =>
        {
            var operatoreId = GetUserId(ctx);
            if (operatoreId is null) return Results.Unauthorized();
            var tx = await creditoService.RicaricaAsync(operatoreId.Value, dto);
            return Results.Ok(tx);
        });

        adminGroup.MapGet("/storico/{utenteId:int}", async (int utenteId, ICreditoService creditoService) =>
            Results.Ok(await creditoService.GetStoricoAsync(utenteId)));

        adminGroup.MapGet("/transazioni", async (
            int? utenteId,
            int? tipo,
            DateTime? dal,
            DateTime? al,
            int? cinemaId,
            ICreditoService creditoService) =>
        {
            var list = await creditoService.GetAllTransazioniAsync(new TransazioneFilterDTO
            {
                UtenteId = utenteId,
                Tipo = tipo,
                Dal = dal,
                Al = al,
                CinemaId = cinemaId
            });
            return Results.Ok(list);
        });

        adminGroup.MapGet("/ricerca-utente", async (string query, FilmDbContext db) =>
        {
            var users = await db.Utenti
                .Where(u => u.Email.Contains(query) || u.Username.Contains(query) || u.Id.ToString() == query)
                .Select(u => new { u.Id, u.Username, u.Email, u.Nome, u.Cognome })
                .Take(20)
                .ToListAsync();
            return Results.Ok(users);
        });

        return app;
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }
}
