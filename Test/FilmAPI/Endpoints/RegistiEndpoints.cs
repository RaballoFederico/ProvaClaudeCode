using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class RegistiEndpoints
{
    public static RouteGroupBuilder MapRegistiEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (FilmDbContext db) =>
            await db.Registi.Select(r => new RegistaDTO
            {
                Id = r.Id,
                Nome = r.Nome,
                Cognome = r.Cognome,
                Nazionalita = r.Nazionalita
            }).ToListAsync());

        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var regista = await db.Registi.FindAsync(id);
            return regista is null ? Results.NotFound() : Results.Ok(new RegistaDTO
            {
                Id = regista.Id,
                Nome = regista.Nome,
                Cognome = regista.Cognome,
                Nazionalita = regista.Nazionalita
            });
        });

        group.MapPost("/", async (RegistaCreateDTO dto, FilmDbContext db) =>
        {
            var regista = new Regista
            {
                Nome = dto.Nome,
                Cognome = dto.Cognome,
                Nazionalita = dto.Nazionalita
            };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();
            return Results.Created($"/registi/{regista.Id}", new RegistaDTO
            {
                Id = regista.Id,
                Nome = regista.Nome,
                Cognome = regista.Cognome,
                Nazionalita = regista.Nazionalita
            });
        });

        group.MapPut("/{id}", async (int id, RegistaUpdateDTO dto, FilmDbContext db) =>
        {
            var regista = await db.Registi.FindAsync(id);
            if (regista is null) return Results.NotFound();

            regista.Nome = dto.Nome;
            regista.Cognome = dto.Cognome;
            regista.Nazionalita = dto.Nazionalita;
            
            await db.SaveChangesAsync();
            return Results.Ok(new RegistaDTO
            {
                Id = regista.Id,
                Nome = regista.Nome,
                Cognome = regista.Cognome,
                Nazionalita = regista.Nazionalita
            });
        });

        group.MapDelete("/{id}", async (int id, FilmDbContext db) =>
        {
            var regista = await db.Registi.FindAsync(id);
            if (regista is null) return Results.NotFound();

            db.Registi.Remove(regista);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
