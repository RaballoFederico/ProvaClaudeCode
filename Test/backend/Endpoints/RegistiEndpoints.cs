// DOC: Endpoint 'RegistiEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class RegistiEndpoints
{
    // DOC-METHOD: 'MapRegistiEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static RouteGroupBuilder MapRegistiEndpoints(this RouteGroupBuilder group)
    {
        // GET /registi - Visibile a tutti
        group.MapGet("/", async (FilmDbContext db) =>
        await db.Registi
            .AsNoTracking()
            .Select(r => new RegistaDTO
        {
            Id = r.Id,
            Nome = r.Nome,
            Cognome = r.Cognome,
            Nazionalita = r.Nazionalita
        }).ToListAsync());

        // GET /registi/{id} - Visibile a tutti
        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var regista = await db.Registi.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            return regista is null ? Results.NotFound() : Results.Ok(new RegistaDTO
            {
                Id = regista.Id,
                Nome = regista.Nome,
                Cognome = regista.Cognome,
                Nazionalita = regista.Nazionalita
            });
        });

        // POST /registi - Admin e PowerUser
        group.MapPost("/", [Authorize(Roles = "Admin,PowerUser")] async (RegistaCreateDTO dto, FilmDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
                return Results.BadRequest("Nome and Cognome are required");

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

        // PUT /registi/{id} - Admin e PowerUser
        group.MapPut("/{id}", [Authorize(Roles = "Admin,PowerUser")] async (int id, RegistaUpdateDTO dto, FilmDbContext db) =>
        {
            var regista = await db.Registi.FindAsync(id);
            if (regista is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
                return Results.BadRequest("Nome and Cognome are required");

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

        // DELETE /registi/{id} - Admin e PowerUser
        group.MapDelete("/{id}", [Authorize(Roles = "Admin,PowerUser")] async (int id, FilmDbContext db) =>
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

