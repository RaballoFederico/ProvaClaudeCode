using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class FilmsEndpoints
{
    private static readonly string DefaultCoverImagePath =
        Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH") ?? "/media/defaults/cover-default.jpg";

    public static RouteGroupBuilder MapFilmsEndpoints(this RouteGroupBuilder group)
    {
        // GET /films - Visibile a tutti (include categorie)
        group.MapGet("/", async (FilmDbContext db) =>
        await db.Films
            .Include(f => f.FilmsCategorie)
            .ThenInclude(fc => fc.Categoria)
            .Select(f => new FilmDTO
            {
                Id = f.Id,
                Titolo = f.Titolo,
                DataProduzione = f.DataProduzione,
                RegistaId = f.RegistaId,
                Durata = f.Durata,
                CopertinaPath = f.CopertinaPath,
                FilmatoPath = f.FilmatoPath,
                Categorie = f.FilmsCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.Categoria.Id,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
            }).ToListAsync());

        // GET /films/{id} - Visibile a tutti (include categorie)
        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var film = await db.Films
                .Include(f => f.FilmsCategorie)
                .ThenInclude(fc => fc.Categoria)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (film is null) return Results.NotFound();

            return Results.Ok(new FilmDTO
            {
                Id = film.Id,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = film.CopertinaPath,
                FilmatoPath = film.FilmatoPath,
                Categorie = film.FilmsCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.Categoria.Id,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
            });
        });

        // POST /films - Solo Admin (con categorie)
        group.MapPost("/", [Authorize(Roles = "Admin")] async (FilmCreateDTO dto, FilmDbContext db) =>
        {
            var registaExists = await db.Registi.AnyAsync(r => r.Id == dto.RegistaId);
            if (!registaExists)
                return Results.BadRequest("Regista not found");

            // Verifica che le categorie esistano
            if (dto.CategoriaIds.Any())
            {
                var categorieEsistenti = await db.Categorie
                    .Where(c => dto.CategoriaIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (categorieEsistenti.Count != dto.CategoriaIds.Count)
                {
                    return Results.BadRequest("Una o più categorie non esistono");
                }
            }

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
                FilmatoPath = dto.FilmatoPath,
                FilmsCategorie = dto.CategoriaIds.Select(id => new FilmCategoria { CategoriaId = id }).ToList()
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
                FilmatoPath = film.FilmatoPath,
                Categorie = film.FilmsCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.CategoriaId,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
            });
        });

        // PUT /films/{id} - Solo Admin (con categorie)
        group.MapPut("/{id}", [Authorize(Roles = "Admin")] async (int id, FilmUpdateDTO dto, FilmDbContext db) =>
        {
            var film = await db.Films
                .Include(f => f.FilmsCategorie)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (film is null) return Results.NotFound();

            var registaExists = await db.Registi.AnyAsync(r => r.Id == dto.RegistaId);
            if (!registaExists)
                return Results.BadRequest("Regista not found");

            // Verifica che le categorie esistano
            if (dto.CategoriaIds.Any())
            {
                var categorieEsistenti = await db.Categorie
                    .Where(c => dto.CategoriaIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (categorieEsistenti.Count != dto.CategoriaIds.Count)
                {
                    return Results.BadRequest("Una o più categorie non esistono");
                }
            }

            film.Titolo = dto.Titolo;
            film.DataProduzione = dto.DataProduzione;
            film.RegistaId = dto.RegistaId;
            film.Durata = dto.Durata;
            film.CopertinaPath = string.IsNullOrWhiteSpace(dto.CopertinaPath)
                ? DefaultCoverImagePath
                : dto.CopertinaPath;
            film.FilmatoPath = dto.FilmatoPath;

            // Aggiorna categorie
            film.FilmsCategorie.Clear();
            foreach (var categoriaId in dto.CategoriaIds)
            {
                film.FilmsCategorie.Add(new FilmCategoria { CategoriaId = categoriaId });
            }

            await db.SaveChangesAsync();

            // Ricarica le categorie per il response
            await db.Entry(film).Collection(f => f.FilmsCategorie).LoadAsync();

            return Results.Ok(new FilmDTO
            {
                Id = film.Id,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = film.CopertinaPath,
                FilmatoPath = film.FilmatoPath,
                Categorie = film.FilmsCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.CategoriaId,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
            });
        });

        // DELETE /films/{id} - Solo Admin
        group.MapDelete("/{id}", [Authorize(Roles = "Admin")] async (int id, FilmDbContext db) =>
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
