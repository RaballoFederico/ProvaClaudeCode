using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class CinemasEndpoints
{
    public static RouteGroupBuilder MapCinemasEndpoints(this RouteGroupBuilder group)
    {
        // GET /cinemas - Visibile a tutti
        group.MapGet("/", async (FilmDbContext db) =>
        await db.Cinemas.Select(c => new CinemaDTO
        {
            Id = c.Id,
            Nome = c.Nome,
            Indirizzo = c.Indirizzo,
            Citta = c.Citta
        }).ToListAsync());

        // GET /cinemas/{id} - Visibile a tutti
        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var cinema = await db.Cinemas.FindAsync(id);
            return cinema is null ? Results.NotFound() : Results.Ok(new CinemaDTO
            {
                Id = cinema.Id,
                Nome = cinema.Nome,
                Indirizzo = cinema.Indirizzo,
                Citta = cinema.Citta
            });
        });

        // POST /cinemas - Solo Admin (PowerUser può solo leggere)
        group.MapPost("/", [Authorize(Roles = "Admin")] async (CinemaCreateDTO dto, FilmDbContext db) =>
        {
            var cinema = new Cinema
            {
                Nome = dto.Nome,
                Indirizzo = dto.Indirizzo,
                Citta = dto.Citta
            };
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();
            return Results.Created($"/cinemas/{cinema.Id}", new CinemaDTO
            {
                Id = cinema.Id,
                Nome = cinema.Nome,
                Indirizzo = cinema.Indirizzo,
                Citta = cinema.Citta
            });
        });

        // PUT /cinemas/{id} - Solo Admin
        group.MapPut("/{id}", [Authorize(Roles = "Admin")] async (int id, CinemaUpdateDTO dto, FilmDbContext db) =>
        {
            var cinema = await db.Cinemas.FindAsync(id);
            if (cinema is null) return Results.NotFound();

            cinema.Nome = dto.Nome;
            cinema.Indirizzo = dto.Indirizzo;
            cinema.Citta = dto.Citta;

            await db.SaveChangesAsync();
            return Results.Ok(new CinemaDTO
            {
                Id = cinema.Id,
                Nome = cinema.Nome,
                Indirizzo = cinema.Indirizzo,
                Citta = cinema.Citta
            });
        });

        // DELETE /cinemas/{id} - Solo Admin
        group.MapDelete("/{id}", [Authorize(Roles = "Admin")] async (int id, FilmDbContext db) =>
        {
            var cinema = await db.Cinemas.FindAsync(id);
            if (cinema is null) return Results.NotFound();

            db.Cinemas.Remove(cinema);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
