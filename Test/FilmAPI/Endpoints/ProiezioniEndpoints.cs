using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class ProiezioniEndpoints
{
    public static RouteGroupBuilder MapProiezioniEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (FilmDbContext db) =>
            await db.Proiezioni.Select(p => new ProiezioneDTO
            {
                Id = p.Id,
                CinemaId = p.CinemaId,
                FilmId = p.FilmId,
                Data = p.Data,
                Ora = p.Ora
            }).ToListAsync());

        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var proiezione = await db.Proiezioni.FindAsync(id);
            return proiezione is null ? Results.NotFound() : Results.Ok(new ProiezioneDTO
            {
                Id = proiezione.Id,
                CinemaId = proiezione.CinemaId,
                FilmId = proiezione.FilmId,
                Data = proiezione.Data,
                Ora = proiezione.Ora
            });
        });

        group.MapPost("/", async (ProiezioneCreateDTO dto, FilmDbContext db) =>
        {
            var cinemaExists = await db.Cinemas.AnyAsync(c => c.Id == dto.CinemaId);
            if (!cinemaExists)
                return Results.BadRequest("Cinema not found");

            var filmExists = await db.Films.AnyAsync(f => f.Id == dto.FilmId);
            if (!filmExists)
                return Results.BadRequest("Film not found");

            var exists = await db.Proiezioni.AnyAsync(p =>
                p.CinemaId == dto.CinemaId &&
                p.FilmId == dto.FilmId &&
                p.Data == dto.Data &&
                p.Ora == dto.Ora);
            
            if (exists)
                return Results.Conflict("Proiezione already exists with same CinemaId, FilmId, Data and Ora");

            var proiezione = new Proiezione
            {
                CinemaId = dto.CinemaId,
                FilmId = dto.FilmId,
                Data = dto.Data,
                Ora = dto.Ora
            };
            db.Proiezioni.Add(proiezione);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                if (inner.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) || inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) || inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                    return Results.Conflict("Proiezione already exists with same CinemaId, FilmId, Data and Ora");
                throw;
            }
            return Results.Created($"/proiezioni/{proiezione.Id}", new ProiezioneDTO
            {
                Id = proiezione.Id,
                CinemaId = proiezione.CinemaId,
                FilmId = proiezione.FilmId,
                Data = proiezione.Data,
                Ora = proiezione.Ora
            });
        });

        group.MapPut("/{id}", async (int id, ProiezioneUpdateDTO dto, FilmDbContext db) =>
        {
            var proiezione = await db.Proiezioni.FindAsync(id);
            if (proiezione is null) return Results.NotFound();

            var cinemaExists = await db.Cinemas.AnyAsync(c => c.Id == dto.CinemaId);
            if (!cinemaExists)
                return Results.BadRequest("Cinema not found");

            var filmExists = await db.Films.AnyAsync(f => f.Id == dto.FilmId);
            if (!filmExists)
                return Results.BadRequest("Film not found");

            var exists = await db.Proiezioni.AnyAsync(p =>
                p.Id != id &&
                p.CinemaId == dto.CinemaId &&
                p.FilmId == dto.FilmId &&
                p.Data == dto.Data &&
                p.Ora == dto.Ora);
            
            if (exists)
                return Results.Conflict("Proiezione already exists with same CinemaId, FilmId, Data and Ora");

            proiezione.CinemaId = dto.CinemaId;
            proiezione.FilmId = dto.FilmId;
            proiezione.Data = dto.Data;
            proiezione.Ora = dto.Ora;
            
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                if (inner.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) || inner.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) || inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                    return Results.Conflict("Proiezione already exists with same CinemaId, FilmId, Data and Ora");
                throw;
            }
            return Results.Ok(new ProiezioneDTO
            {
                Id = proiezione.Id,
                CinemaId = proiezione.CinemaId,
                FilmId = proiezione.FilmId,
                Data = proiezione.Data,
                Ora = proiezione.Ora
            });
        });

        group.MapDelete("/{id}", async (int id, FilmDbContext db) =>
        {
            var proiezione = await db.Proiezioni.FindAsync(id);
            if (proiezione is null) return Results.NotFound();

            db.Proiezioni.Remove(proiezione);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
