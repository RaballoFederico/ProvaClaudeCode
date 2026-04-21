using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin");
        var utentiGroup = app.MapGroup("/admin/utenti").RequireAuthorization("AdminOnly");

        // GET /admin/users - Lista tutti gli utenti (solo Admin)
        group.MapGet("/users", [Authorize(Roles = "Admin")] async (FilmDbContext db) =>
        {
            var utenti = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Nome,
                    u.Cognome,
                    u.Telefono,
                    u.DataRegistrazione,
                    u.DataUltimoAccesso,
                    u.Attivo,
                    Ruoli = u.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList()
                })
                .ToListAsync();

            return Results.Ok(utenti);
        });

        // GET /admin/users/{id} - Dettagli utente specifico
        group.MapGet("/users/{id}", [Authorize(Roles = "Admin")] async (int id, FilmDbContext db) =>
        {
            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .Include(u => u.ProiezioniSalvate)
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Nome,
                    u.Cognome,
                    u.Telefono,
                    u.DataRegistrazione,
                    u.DataUltimoAccesso,
                    u.Attivo,
                    Ruoli = u.UtentiRuoli.Select(ur => new { ur.Ruolo.Id, ur.Ruolo.Nome }).ToList(),
                    ProiezioniSalvateCount = u.ProiezioniSalvate.Count
                })
                .FirstOrDefaultAsync();

            if (utente == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(utente);
        });

        // PUT /admin/users/{id}/roles - Aggiorna ruoli utente
        group.MapPut("/users/{id}/roles", [Authorize(Roles = "Admin")] async (int id, UpdateRuoliRequestDTO request, FilmDbContext db) =>
        {
            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utente == null)
            {
                return Results.NotFound();
            }

            // Verifica che tutti i ruoli esistano
            var ruoliEsistenti = await db.Ruoli
                .Where(r => request.RuoloIds.Contains(r.Id))
                .ToListAsync();

            if (ruoliEsistenti.Count != request.RuoloIds.Count)
            {
                return Results.BadRequest(new { message = "Uno o più ruoli non esistono" });
            }

            var adminRuolo = await db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "Admin");
            if (adminRuolo != null)
            {
                var adminRichiesto = request.RuoloIds.Contains(adminRuolo.Id);
                var adminCorrente = utente.UtentiRuoli.Any(ur => ur.RuoloId == adminRuolo.Id);

                if (adminCorrente && !adminRichiesto)
                {
                    var altriAdmin = await db.UtentiRuoli.CountAsync(ur => ur.RuoloId == adminRuolo.Id && ur.UtenteId != id);
                    if (altriAdmin == 0)
                    {
                        return Results.BadRequest(new { message = "Impossibile rimuovere il ruolo dall'ultimo admin" });
                    }
                }
            }

            // Rimuovi ruoli esistenti
            utente.UtentiRuoli.Clear();

            // Aggiungi nuovi ruoli
            foreach (var ruoloId in request.RuoloIds)
            {
                utente.UtentiRuoli.Add(new UtenteRuolo { RuoloId = ruoloId });
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Ruoli aggiornati con successo" });
        });

        // DELETE /admin/users/{id} - Disattiva utente (soft delete)
        group.MapDelete("/users/{id}", [Authorize(Roles = "Admin")] async (int id, HttpContext context, FilmDbContext db) =>
        {
            var currentUserId = GetUserId(context);
            if (currentUserId == id)
            {
                return Results.BadRequest(new { message = "Non puoi disattivare il tuo stesso account" });
            }

            var utente = await db.Utenti.FindAsync(id);
            if (utente == null)
            {
                return Results.NotFound();
            }

            utente.Attivo = false;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Utente disattivato con successo" });
        });

        // POST /admin/users/{id}/activate - Riattiva utente
        group.MapPost("/users/{id}/activate", [Authorize(Roles = "Admin")] async (int id, FilmDbContext db) =>
        {
            var utente = await db.Utenti.FindAsync(id);
            if (utente == null)
            {
                return Results.NotFound();
            }

            utente.Attivo = true;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Utente riattivato con successo" });
        });

        utentiGroup.MapGet("/", [Authorize(Roles = "Admin")] async (FilmDbContext db) =>
        {
            var utenti = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Nome,
                    u.Cognome,
                    u.Telefono,
                    u.DataRegistrazione,
                    u.DataUltimoAccesso,
                    u.Attivo,
                    Ruoli = u.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList()
                })
                .ToListAsync();

            return Results.Ok(utenti);
        });

        utentiGroup.MapPut("/{id}/ruoli", [Authorize(Roles = "Admin")] async (int id, UpdateRuoliRequestDTO request, FilmDbContext db) =>
        {
            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utente == null)
            {
                return Results.NotFound();
            }

            var ruoliEsistenti = await db.Ruoli.Where(r => request.RuoloIds.Contains(r.Id)).ToListAsync();
            if (ruoliEsistenti.Count != request.RuoloIds.Count)
            {
                return Results.BadRequest(new { message = "Uno o piu' ruoli non esistono" });
            }

            var adminRuolo = await db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "Admin");
            if (adminRuolo != null)
            {
                var adminRichiesto = request.RuoloIds.Contains(adminRuolo.Id);
                var adminCorrente = utente.UtentiRuoli.Any(ur => ur.RuoloId == adminRuolo.Id);
                if (adminCorrente && !adminRichiesto)
                {
                    var altriAdmin = await db.UtentiRuoli.CountAsync(ur => ur.RuoloId == adminRuolo.Id && ur.UtenteId != id);
                    if (altriAdmin == 0)
                    {
                        return Results.BadRequest(new { message = "Impossibile rimuovere il ruolo dall'ultimo admin" });
                    }
                }
            }

            utente.UtentiRuoli.Clear();
            foreach (var ruoloId in request.RuoloIds)
            {
                utente.UtentiRuoli.Add(new UtenteRuolo { RuoloId = ruoloId });
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Ruoli aggiornati con successo" });
        });

        return app;
    }

    private static int GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }
}
