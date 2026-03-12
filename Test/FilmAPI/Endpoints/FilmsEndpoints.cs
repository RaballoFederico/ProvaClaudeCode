using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class FilmsEndpoints
{
    private static readonly string DefaultCoverImagePath = 
        Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH") ?? "/media/defaults/cover-default.jpg";

    public static RouteGroupBuilder MapFilmsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (FilmDbContext db) =>
            await db.Films.Select(f => new FilmDTO
            {
                Id = f.Id,
                Titolo = f.Titolo,
                DataProduzione = f.DataProduzione,
                RegistaId = f.RegistaId,
                Durata = f.Durata,
                CopertinaPath = f.CopertinaPath,
                FilmatoPath = f.FilmatoPath
            }).ToListAsync());

        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var film = await db.Films.FindAsync(id);
            return film is null ? Results.NotFound() : Results.Ok(new FilmDTO
            {
                Id = film.Id,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = film.CopertinaPath,
                FilmatoPath = film.FilmatoPath
            });
        });

        group.MapPost("/", async (FilmCreateDTO dto, FilmDbContext db) =>
        {
            var registaExists = await db.Registi.AnyAsync(r => r.Id == dto.RegistaId);
            if (!registaExists)
                return Results.BadRequest("Regista not found");

            var copertinaPath = string.IsNullOrWhiteSpace(dto.CopertinaPath) 
                ? DefaultCoverImagePath 
                : dto.CopertinaPath;

            var film = new Film
            {
                Titolo = dto.Titolo,
                DataProduzione = dto.DataProduzione,
                RegistaId = dto.RegistaId,
                Durata = dto.Durata,
                CopertinaPath = copertinaPath,
                FilmatoPath = dto.FilmatoPath
            };
            db.Films.Add(film);
            await db.SaveChangesAsync();
            return Results.Created($"/films/{film.Id}", new FilmDTO
            {
                Id = film.Id,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = film.CopertinaPath,
                FilmatoPath = film.FilmatoPath
            });
        });

        group.MapPut("/{id}", async (int id, FilmUpdateDTO dto, FilmDbContext db) =>
        {
            var film = await db.Films.FindAsync(id);
            if (film is null) return Results.NotFound();

            var registaExists = await db.Registi.AnyAsync(r => r.Id == dto.RegistaId);
            if (!registaExists)
                return Results.BadRequest("Regista not found");

            film.Titolo = dto.Titolo;
            film.DataProduzione = dto.DataProduzione;
            film.RegistaId = dto.RegistaId;
            film.Durata = dto.Durata;
            film.CopertinaPath = string.IsNullOrWhiteSpace(dto.CopertinaPath) 
                ? DefaultCoverImagePath 
                : dto.CopertinaPath;
            film.FilmatoPath = dto.FilmatoPath;
            
            await db.SaveChangesAsync();
            return Results.Ok(new FilmDTO
            {
                Id = film.Id,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = film.CopertinaPath,
                FilmatoPath = film.FilmatoPath
            });
        });

        group.MapDelete("/{id}", async (int id, FilmDbContext db) =>
        {
            var film = await db.Films.FindAsync(id);
            if (film is null) return Results.NotFound();

            db.Films.Remove(film);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
