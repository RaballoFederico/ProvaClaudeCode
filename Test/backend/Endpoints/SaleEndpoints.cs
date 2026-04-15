using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class SaleEndpoints
{
    public static IEndpointRouteBuilder MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/cinemas/{cinemaId:int}/sale", [Authorize(Roles = "Admin,PowerUser")] async (int cinemaId, ISalaService salaService) =>
        {
            var sale = await salaService.GetSaleByCinemaAsync(cinemaId);
            return Results.Ok(sale);
        });

        app.MapGet("/admin/sale/{id:int}", [Authorize(Roles = "Admin,PowerUser")] async (int id, ISalaService salaService) =>
        {
            var sala = await salaService.GetSalaAsync(id);
            return sala is null ? Results.NotFound() : Results.Ok(sala);
        });

        app.MapPost("/admin/cinemas/{cinemaId:int}/sale", [Authorize(Roles = "Admin")] async (int cinemaId, SalaCreateDTO dto, FilmDbContext db, ISalaService salaService) =>
        {
            var cinemaExists = await db.Cinemas.AnyAsync(c => c.Id == cinemaId);
            if (!cinemaExists) return Results.BadRequest("Cinema non trovato");
            var created = await salaService.CreateSalaAsync(cinemaId, dto);
            return Results.Created($"/admin/sale/{created.Id}", created);
        });

        app.MapPut("/admin/sale/{id:int}", [Authorize(Roles = "Admin")] async (int id, SalaUpdateDTO dto, ISalaService salaService) =>
        {
            var updated = await salaService.UpdateSalaAsync(id, dto);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        app.MapDelete("/admin/sale/{id:int}", [Authorize(Roles = "Admin")] async (int id, ISalaService salaService) =>
        {
            var deleted = await salaService.DeleteSalaAsync(id);
            return deleted ? Results.NoContent() : Results.BadRequest("Impossibile eliminare sala con show futuri o sala inesistente");
        });

        app.MapGet("/admin/sale/{id:int}/piantina", [Authorize(Roles = "Admin,PowerUser")] async (int id, ISalaService salaService) =>
        {
            var piantina = await salaService.GetPiantinaAsync(id);
            return piantina is null ? Results.NotFound() : Results.Ok(piantina);
        });

        app.MapPut("/admin/sale/{id:int}/piantina", [Authorize(Roles = "Admin")] async (int id, PiantinaUpdateDTO dto, ISalaService salaService) =>
        {
            var ok = await salaService.UpdatePiantinaAsync(id, dto);
            return ok ? Results.Ok() : Results.NotFound();
        });

        return app;
    }
}
