// DOC: NotificheEndpoints - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Endpoint 'NotificheEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class NotificheEndpoints
{
    // DOC-METHOD: 'MapNotificheEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static IEndpointRouteBuilder MapNotificheEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifiche").RequireAuthorization("Authenticated");

        group.MapGet("/mine", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var list = await db.NotificheUtente
                .Where(n => n.UtenteId == userId.Value)
                .OrderByDescending(n => n.DataCreazione)
                .Take(80)
                .Select(n => new NotificaDTO
                {
                    Id = n.Id,
                    Tipo = n.Tipo,
                    Titolo = n.Titolo,
                    Messaggio = n.Messaggio,
                    Url = n.Url,
                    DedupeKey = n.DedupeKey,
                    Letta = n.Letta,
                    DataCreazione = n.DataCreazione
                })
                .ToListAsync();

            return Results.Ok(list);
        });

        group.MapPost("/mine", async (HttpContext context, CreaNotificaRequestDTO request, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var titolo = Normalize(request.Titolo, 140);
            if (string.IsNullOrWhiteSpace(titolo)) return Results.BadRequest(new { message = "Titolo notifica obbligatorio" });

            var dedupeKey = Normalize(request.DedupeKey, 120);
            if (!string.IsNullOrWhiteSpace(dedupeKey))
            {
                var exists = await db.NotificheUtente.AnyAsync(n => n.UtenteId == userId.Value && n.DedupeKey == dedupeKey);
                if (exists)
                {
                    var existing = await db.NotificheUtente
                        .Where(n => n.UtenteId == userId.Value && n.DedupeKey == dedupeKey)
                        .OrderByDescending(n => n.DataCreazione)
                        .FirstAsync();
                    return Results.Ok(new { id = existing.Id, deduped = true });
                }
            }

            var entity = new NotificaUtente
            {
                UtenteId = userId.Value,
                Tipo = Normalize(request.Tipo, 50) ?? "info",
                Titolo = titolo,
                Messaggio = Normalize(request.Messaggio, 500),
                Url = Normalize(request.Url, 500),
                DedupeKey = dedupeKey,
                Letta = false,
                DataCreazione = DateTime.UtcNow
            };

            db.NotificheUtente.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/notifiche/mine/{entity.Id}", new { id = entity.Id, deduped = false });
        });

        group.MapPut("/mine/{id:int}/read", async (int id, HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var entity = await db.NotificheUtente.FirstOrDefaultAsync(n => n.Id == id && n.UtenteId == userId.Value);
            if (entity is null) return Results.NotFound();
            entity.Letta = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPut("/mine/read-all", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            await db.NotificheUtente
                .Where(n => n.UtenteId == userId.Value && !n.Letta)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Letta, true));
            return Results.NoContent();
        });

        group.MapDelete("/mine", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            await db.NotificheUtente
                .Where(n => n.UtenteId == userId.Value)
                .ExecuteDeleteAsync();
            return Results.NoContent();
        });

        return app;
    }

    // DOC-METHOD: 'GetUserId' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static int? GetUserId(HttpContext context)
    {
        var claim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    // DOC-METHOD: 'Normalize' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}


