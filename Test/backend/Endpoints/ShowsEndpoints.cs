using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class ShowsEndpoints
{
    public static IEndpointRouteBuilder MapShowsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/shows", [Authorize(Roles = "Admin,PowerUser")] async (
            int? cinemaId,
            int? salaId,
            int? filmId,
            DateOnly? data,
            int? stato,
            IShowService showService) =>
        {
            var items = await showService.GetShowsAsync(new ShowFilterDTO
            {
                CinemaId = cinemaId,
                SalaId = salaId,
                FilmId = filmId,
                Data = data,
                Stato = stato
            });
            return Results.Ok(items);
        });

        app.MapGet("/admin/shows/{id:int}", [Authorize(Roles = "Admin,PowerUser")] async (int id, IShowService showService) =>
        {
            var item = await showService.GetShowAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        app.MapPost("/admin/shows", [Authorize(Roles = "Admin,PowerUser")] async (ShowCreateDTO dto, IShowService showService) =>
        {
            var created = await showService.CreateShowAsync(dto);
            return Results.Created($"/admin/shows/{created.Id}", created);
        });

        app.MapPut("/admin/shows/{id:int}", [Authorize(Roles = "Admin,PowerUser")] async (int id, ShowUpdateDTO dto, IShowService showService) =>
        {
            var updated = await showService.UpdateShowAsync(id, dto);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        app.MapDelete("/admin/shows/{id:int}", [Authorize(Roles = "Admin,PowerUser")] async (int id, IShowService showService) =>
        {
            var ok = await showService.DeleteShowAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        app.MapPost("/admin/shows/bulk", [Authorize(Roles = "Admin,PowerUser")] async (ShowBulkCreateDTO dto, IShowService showService, FilmDbContext db) =>
        {
            var created = new List<ShowDTO>();
            for (var day = dto.Dal; day <= dto.Al; day = day.AddDays(1))
            {
                var weekday = (int)day.DayOfWeek;
                foreach (var slot in dto.Slots)
                {
                    if (slot.GiorniSettimana.Count > 0 && !slot.GiorniSettimana.Contains(weekday)) continue;
                    foreach (var orario in slot.Orari)
                    {
                        try
                        {
                            var show = await showService.CreateShowAsync(new ShowCreateDTO
                            {
                                SalaId = slot.SalaId,
                                FilmId = dto.FilmId,
                                Data = day,
                                OraInizio = orario
                            });
                            created.Add(show);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return Results.Ok(new { count = created.Count, shows = created });
        });

        app.MapGet("/admin/shows/{id:int}/disponibilita", [Authorize(Roles = "Admin,PowerUser")] async (int id, IShowService showService) =>
        {
            var disp = await showService.GetDisponibilitaPostiAsync(id);
            return disp is null ? Results.NotFound() : Results.Ok(disp);
        });

        return app;
    }
}
