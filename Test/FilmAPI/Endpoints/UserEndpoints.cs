using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user").RequireAuthorization();
        var profiloGroup = app.MapGroup("/profilo").RequireAuthorization("Authenticated");
        var prenotazioniGroup = app.MapGroup("/prenotazioni").RequireAuthorization("Authenticated");

        // GET /user/profile - Profilo utente corrente
        group.MapGet("/profile", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .Include(u => u.ProiezioniSalvate)
                .ThenInclude(ps => ps.Proiezione)
                .ThenInclude(p => p.Film)
                .Include(u => u.ProiezioniSalvate)
                .ThenInclude(ps => ps.Proiezione)
                .ThenInclude(p => p.Cinema)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Attivo);

            if (utente == null) return Results.NotFound();

            var profilo = new ProfiloUtenteDTO
            {
                Id = utente.Id,
                Username = utente.Username,
                Email = utente.Email,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Telefono = utente.Telefono,
                DataRegistrazione = utente.DataRegistrazione,
                Ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList(),
                ProiezioniSalvate = utente.ProiezioniSalvate.Select(ps => new ProiezioneSalvataDTO
                {
                    Id = ps.Id,
                    ProiezioneId = ps.ProiezioneId,
                    FilmTitolo = ps.Proiezione.Film?.Titolo ?? "N/A",
                    CinemaNome = ps.Proiezione.Cinema?.Nome ?? "N/A",
                    DataProiezione = ps.Proiezione.Data,
                    OraProiezione = ps.Proiezione.Ora,
                    DataSalvataggio = ps.DataSalvataggio,
                    Prenotato = ps.Prenotato,
                    NumeroPosti = ps.NumeroPosti
                }).OrderByDescending(ps => ps.DataSalvataggio).ToList()
            };

            return Results.Ok(profilo);
        });

        // PUT /user/profile - Aggiorna profilo
        group.MapPut("/profile", async (HttpContext context, UpdateProfiloRequestDTO request, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var utente = await db.Utenti.FindAsync(userId);
            if (utente == null) return Results.NotFound();

            // Verifica email unica se modificata
            if (request.Email != utente.Email)
            {
                if (await db.Utenti.AnyAsync(u => u.Email == request.Email && u.Id != userId))
                {
                    return Results.Conflict(new { message = "Email già in uso" });
                }
                utente.Email = request.Email;
            }

            utente.Nome = request.Nome;
            utente.Cognome = request.Cognome;
            utente.Telefono = request.Telefono;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Profilo aggiornato con successo" });
        });

        // GET /user/proiezioni-salvate
        group.MapGet("/proiezioni-salvate", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var proiezioni = await db.ProiezioniSalvate
                .Include(ps => ps.Proiezione)
                .ThenInclude(p => p.Film)
                .Include(ps => ps.Proiezione)
                .ThenInclude(p => p.Cinema)
                .Where(ps => ps.UtenteId == userId)
                .OrderByDescending(ps => ps.DataSalvataggio)
                .Select(ps => new ProiezioneSalvataDTO
                {
                    Id = ps.Id,
                    ProiezioneId = ps.ProiezioneId,
                    FilmTitolo = ps.Proiezione.Film != null ? ps.Proiezione.Film.Titolo : "N/A",
                    CinemaNome = ps.Proiezione.Cinema != null ? ps.Proiezione.Cinema.Nome : "N/A",
                    DataProiezione = ps.Proiezione.Data,
                    OraProiezione = ps.Proiezione.Ora,
                    DataSalvataggio = ps.DataSalvataggio,
                    Prenotato = ps.Prenotato,
                    NumeroPosti = ps.NumeroPosti
                })
                .ToListAsync();

            return Results.Ok(proiezioni);
        });

        // POST /user/proiezioni-salvate
        group.MapPost("/proiezioni-salvate", async (HttpContext context, SalvaProiezioneRequestDTO request, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            // Verifica che la proiezione esista
            var proiezione = await db.Proiezioni.FindAsync(request.ProiezioneId);
            if (proiezione == null)
            {
                return Results.NotFound(new { message = "Proiezione non trovata" });
            }

            // Verifica che non sia già salvata
            var esistente = await db.ProiezioniSalvate
                .FirstOrDefaultAsync(ps => ps.UtenteId == userId && ps.ProiezioneId == request.ProiezioneId);

            if (esistente != null)
            {
                return Results.Conflict(new { message = "Proiezione già salvata" });
            }

            var salvata = new ProiezioneSalvata
            {
                UtenteId = userId.Value,
                ProiezioneId = request.ProiezioneId,
                DataSalvataggio = DateTime.UtcNow,
                Prenotato = false,
                NumeroPosti = 0
            };

            await db.ProiezioniSalvate.AddAsync(salvata);
            await db.SaveChangesAsync();

            return Results.Created($"/user/proiezioni-salvate/{salvata.Id}", new { message = "Proiezione salvata con successo", id = salvata.Id });
        });

        // DELETE /user/proiezioni-salvate/{id}
        group.MapDelete("/proiezioni-salvate/{id}", async (int id, HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var salvata = await db.ProiezioniSalvate
                .FirstOrDefaultAsync(ps => ps.Id == id && ps.UtenteId == userId);

            if (salvata == null)
            {
                return Results.NotFound();
            }

            db.ProiezioniSalvate.Remove(salvata);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // POST /user/prenota
        group.MapPost("/prenota", async (HttpContext context, PrenotazioneRequestDTO request, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var salvata = await db.ProiezioniSalvate
                .Include(ps => ps.Proiezione)
                .FirstOrDefaultAsync(ps => ps.Id == request.ProiezioneSalvataId && ps.UtenteId == userId);

            if (salvata == null)
            {
                return Results.NotFound(new { message = "Proiezione salvata non trovata" });
            }

            if (salvata.Prenotato)
            {
                return Results.Conflict(new { message = "Proiezione già prenotata" });
            }

            if (request.NumeroPosti <= 0)
            {
                return Results.BadRequest(new { message = "Numero posti deve essere maggiore di 0" });
            }

            // Verifica che la proiezione sia futura
            var dataOraProiezione = salvata.Proiezione.Data.Add(salvata.Proiezione.Ora);
            if (dataOraProiezione < DateTime.Now)
            {
                return Results.BadRequest(new { message = "Non è possibile prenotare una proiezione passata" });
            }

            salvata.Prenotato = true;
            salvata.DataPrenotazione = DateTime.UtcNow;
            salvata.NumeroPosti = request.NumeroPosti;

            var prenotazione = new Prenotazione
            {
                UtenteId = userId.Value,
                ProiezioneId = salvata.ProiezioneId,
                DataPrenotazione = salvata.DataPrenotazione.Value,
                NumeroPosti = request.NumeroPosti
            };

            await db.Prenotazioni.AddAsync(prenotazione);

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Prenotazione effettuata con successo" });
        });

        // DELETE /user/prenota/{id} - Annulla prenotazione
        group.MapDelete("/prenota/{id}", async (int id, HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var salvata = await db.ProiezioniSalvate
                .Include(ps => ps.Proiezione)
                .FirstOrDefaultAsync(ps => ps.Id == id && ps.UtenteId == userId);

            if (salvata == null)
            {
                return Results.NotFound();
            }

            if (!salvata.Prenotato)
            {
                return Results.BadRequest(new { message = "Nessuna prenotazione da annullare" });
            }

            salvata.Prenotato = false;
            salvata.DataPrenotazione = null;
            salvata.NumeroPosti = 0;

            var ultimaPrenotazione = await db.Prenotazioni
                .Where(p => p.UtenteId == userId && p.ProiezioneId == salvata.ProiezioneId && p.DataAnnullamento == null)
                .OrderByDescending(p => p.DataPrenotazione)
                .FirstOrDefaultAsync();

            if (ultimaPrenotazione != null)
            {
                ultimaPrenotazione.DataAnnullamento = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Prenotazione annullata con successo" });
        });

        profiloGroup.MapGet("/", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Attivo);

            if (utente == null) return Results.NotFound();

            return Results.Ok(new UtenteDTO
            {
                Id = utente.Id,
                Username = utente.Username,
                Email = utente.Email,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList()
            });
        });

        prenotazioniGroup.MapGet("/", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var prenotazioni = await db.Prenotazioni
                .Include(p => p.Proiezione)
                .ThenInclude(pr => pr.Film)
                .Include(p => p.Proiezione)
                .ThenInclude(pr => pr.Cinema)
                .Where(p => p.UtenteId == userId && p.DataAnnullamento == null)
                .OrderByDescending(p => p.DataPrenotazione)
                .Select(p => new
                {
                    p.Id,
                    p.ProiezioneId,
                    Film = p.Proiezione.Film != null ? p.Proiezione.Film.Titolo : "N/A",
                    Cinema = p.Proiezione.Cinema != null ? p.Proiezione.Cinema.Nome : "N/A",
                    p.DataPrenotazione,
                    p.NumeroPosti
                })
                .ToListAsync();

            return Results.Ok(prenotazioni);
        });

        return app;
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }
}
