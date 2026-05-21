// DOC: CategorieEndpoints - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Endpoint 'CategorieEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace FilmAPI.Endpoints;

public static class CategorieEndpoints
{
    // DOC-METHOD: 'MapCategorieEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static IEndpointRouteBuilder MapCategorieEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categorie");

        group.MapGet("/", async (ICategoriaService categoriaService) => Results.Ok(await categoriaService.GetAllAsync()));

        group.MapGet("/{id}", async (int id, ICategoriaService categoriaService) =>
        {
            var categoria = await categoriaService.GetByIdAsync(id);
            return categoria == null ? Results.NotFound() : Results.Ok(categoria);
        });

        group.MapGet("/{id}/films", async (int id, ICategoriaService categoriaService) =>
        {
            var categoria = await categoriaService.GetByIdAsync(id);
            if (categoria == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await categoriaService.GetFilmsByCategoriaAsync(id));
        });

        group.MapPost("/", [Authorize(Roles = "Admin")] async (CategoriaCreateDTO request, ICategoriaService categoriaService) =>
        {
            var (categoria, error) = await categoriaService.CreateAsync(request);
            if (error != null)
            {
                if (error.Contains("gia' esistente", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Conflict(new { message = error });
                }

                return Results.BadRequest(new { message = error });
            }

            return Results.Created($"/categorie/{categoria!.Id}", categoria);
        });

        group.MapPut("/{id}", [Authorize(Roles = "Admin")] async (int id, CategoriaCreateDTO request, ICategoriaService categoriaService) =>
        {
            var (categoria, error) = await categoriaService.UpdateAsync(id, request);
            if (error != null)
            {
                if (error == "NOT_FOUND")
                {
                    return Results.NotFound();
                }

                if (error.Contains("gia' esistente", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Conflict(new { message = error });
                }

                return Results.BadRequest(new { message = error });
            }

            return Results.Ok(categoria);
        });

        group.MapDelete("/{id}", [Authorize(Roles = "Admin")] async (int id, ICategoriaService categoriaService) =>
        {
            var deleted = await categoriaService.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}


