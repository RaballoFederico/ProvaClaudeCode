using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class CategorieEndpoints
{
    public static IEndpointRouteBuilder MapCategorieEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categorie");

        // GET /categorie - Lista tutte le categorie
        group.MapGet("/", async (FilmDbContext db) =>
        {
            var categorie = await db.Categorie
                .Select(c => new CategoriaDTO
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Descrizione = c.Descrizione
                })
                .ToListAsync();

            return Results.Ok(categorie);
        });

        // GET /categorie/{id} - Dettaglio categoria
        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var categoria = await db.Categorie
                .Include(c => c.FilmsCategorie)
                .ThenInclude(fc => fc.Film)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
            {
                return Results.NotFound();
            }

            var dto = new CategoriaDTO
            {
                Id = categoria.Id,
                Nome = categoria.Nome,
                Descrizione = categoria.Descrizione
            };

            return Results.Ok(dto);
        });

        // GET /categorie/{id}/films - Film di una categoria
        group.MapGet("/{id}/films", async (int id, FilmDbContext db) =>
        {
            var categoria = await db.Categorie.FindAsync(id);
            if (categoria == null)
            {
                return Results.NotFound();
            }

            var films = await db.FilmsCategorie
                .Where(fc => fc.CategoriaId == id)
                .Include(fc => fc.Film)
                .ThenInclude(f => f.Regista)
                .Select(fc => new FilmDTO
                {
                    Id = fc.Film.Id,
                    Titolo = fc.Film.Titolo,
                    DataProduzione = fc.Film.DataProduzione,
                    RegistaId = fc.Film.RegistaId,
                    Durata = fc.Film.Durata,
                    CopertinaPath = fc.Film.CopertinaPath,
                    FilmatoPath = fc.Film.FilmatoPath
                })
                .ToListAsync();

            return Results.Ok(films);
        });

        // POST /categorie - Crea categoria (solo Admin)
        group.MapPost("/", [Authorize(Roles = "Admin")] async (CategoriaCreateDTO request, FilmDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nome))
            {
                return Results.BadRequest(new { message = "Il nome della categoria è obbligatorio" });
            }

            // Verifica che il nome sia unico
            if (await db.Categorie.AnyAsync(c => c.Nome.ToLower() == request.Nome.ToLower()))
            {
                return Results.Conflict(new { message = "Categoria con questo nome già esistente" });
            }

            var categoria = new Categoria
            {
                Nome = request.Nome,
                Descrizione = request.Descrizione
            };

            await db.Categorie.AddAsync(categoria);
            await db.SaveChangesAsync();

            var dto = new CategoriaDTO
            {
                Id = categoria.Id,
                Nome = categoria.Nome,
                Descrizione = categoria.Descrizione
            };

            return Results.Created($"/categorie/{categoria.Id}", dto);
        });

        // PUT /categorie/{id} - Aggiorna categoria (solo Admin)
        group.MapPut("/{id}", [Authorize(Roles = "Admin")] async (int id, CategoriaCreateDTO request, FilmDbContext db) =>
        {
            var categoria = await db.Categorie.FindAsync(id);
            if (categoria == null)
            {
                return Results.NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Nome))
            {
                return Results.BadRequest(new { message = "Il nome della categoria è obbligatorio" });
            }

            // Verifica che il nome sia unico (escludendo la categoria corrente)
            if (await db.Categorie.AnyAsync(c => c.Nome.ToLower() == request.Nome.ToLower() && c.Id != id))
            {
                return Results.Conflict(new { message = "Categoria con questo nome già esistente" });
            }

            categoria.Nome = request.Nome;
            categoria.Descrizione = request.Descrizione;

            await db.SaveChangesAsync();

            return Results.Ok(new CategoriaDTO
            {
                Id = categoria.Id,
                Nome = categoria.Nome,
                Descrizione = categoria.Descrizione
            });
        });

        // DELETE /categorie/{id} - Elimina categoria (solo Admin)
        group.MapDelete("/{id}", [Authorize(Roles = "Admin")] async (int id, FilmDbContext db) =>
        {
            var categoria = await db.Categorie.FindAsync(id);
            if (categoria == null)
            {
                return Results.NotFound();
            }

            db.Categorie.Remove(categoria);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}
