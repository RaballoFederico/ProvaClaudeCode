using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using FilmAPI.Services;
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

        group.MapPost("/tmdb-sync", [Authorize(Roles = "Admin")] async (TMDBFilmSyncService syncService) =>
        {
            await syncService.SyncPopularMoviesAsync();
            return Results.Ok(new { message = "Sincronizzazione TMDB avviata" });
        });

        group.MapGet("/ruoli", [Authorize(Roles = "Admin")] async (FilmDbContext db) =>
        {
            var ruoli = await db.Ruoli
                .OrderBy(r => r.Id)
                .Select(r => new { r.Id, r.Nome, r.Descrizione })
                .ToListAsync();
            return Results.Ok(ruoli);
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

        utentiGroup.MapPut("/{id}/ruoli", [Authorize(Roles = "Admin")] async (int id, UpdateRuoliRequestDTO request, HttpContext context, FilmDbContext db, IUserSecurityAuditService auditService) =>
        {
            var actorUserId = GetUserId(context);
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

            var powerUserRuolo = await db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "PowerUser");
            if (powerUserRuolo != null)
            {
                var powerUserRichiesto = request.RuoloIds.Contains(powerUserRuolo.Id);
                var powerUserCorrente = utente.UtentiRuoli.Any(ur => ur.RuoloId == powerUserRuolo.Id);
                if (powerUserCorrente && !powerUserRichiesto)
                {
                    var altriPowerUser = await db.UtentiRuoli.CountAsync(ur => ur.RuoloId == powerUserRuolo.Id && ur.UtenteId != id);
                    if (altriPowerUser == 0)
                    {
                        return Results.BadRequest(new { message = "Impossibile rimuovere il ruolo dall'ultimo PowerUser" });
                    }
                }
            }

            utente.UtentiRuoli.Clear();
            foreach (var ruoloId in request.RuoloIds)
            {
                utente.UtentiRuoli.Add(new UtenteRuolo { RuoloId = ruoloId });
            }

            await db.SaveChangesAsync();
            await auditService.LogAsync("admin_role_change", "success", actorUserId, id, utente.Email, context.Connection.RemoteIpAddress?.ToString(), $"roleIds={string.Join(',', request.RuoloIds)}");
            return Results.Ok(new { message = "Ruoli aggiornati con successo" });
        });

        utentiGroup.MapPost("/invito", [Authorize(Roles = "Admin")] async (
            CreateInviteRequestDTO request,
            HttpContext context,
            IAccountActionTokenService tokenService,
            IEmailService emailService,
            IUserSecurityAuditService auditService) =>
        {
            var actorUserId = GetUserId(context);
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var ruolo = (request.Ruolo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return Results.BadRequest(new { message = "Email non valida" });
            }

            if (!string.Equals(ruolo, "PowerUser", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ruolo, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Ruolo invito consentito: PowerUser o Admin" });
            }

            var token = await tokenService.CreateAsync(email, AccountActionTokenPurpose.AccountInvite, TimeSpan.FromHours(24), null, ruolo);
            var frontendBase = ResolveFrontendBaseUrl(request.ReturnUrl);
            var inviteUrl = $"{frontendBase.TrimEnd('/')}/reimposta-password.html?inviteToken={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

            var html = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.45;color:#1f2937'>
                    <h2>Invito ruolo {System.Net.WebUtility.HtmlEncode(ruolo)}</h2>
                    <p>Hai ricevuto un invito per completare il tuo account.</p>
                    <p><a href='{inviteUrl}'>Completa invito</a></p>
                    <p>Link valido 24 ore, monouso.</p>
                </div>";

            await emailService.InviaConfermaAcquistoAsync(email, $"FilmHub - Invito {ruolo}", html);
            await auditService.LogAsync("admin_invite_created", "success", actorUserId, null, email, context.Connection.RemoteIpAddress?.ToString(), $"role={ruolo}");

            return Results.Ok(new { message = "Invito creato", email, ruolo });
        });

        utentiGroup.MapPut("/{id:int}/profilo", [Authorize(Roles = "Admin")] async (int id, UpdateProfiloRequestDTO request, HttpContext context, FilmDbContext db) =>
        {
            var actorUserId = GetUserId(context);
            var utente = await db.Utenti.FirstOrDefaultAsync(u => u.Id == id);
            if (utente == null)
            {
                return Results.NotFound(new { message = "Utente non trovato" });
            }

            var email = (request.Email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return Results.BadRequest(new { message = "Email non valida" });
            }

            var emailExists = await db.Utenti.AnyAsync(u => u.Email == email && u.Id != id);
            if (emailExists)
            {
                return Results.Conflict(new { message = "Email gia in uso" });
            }

            utente.Email = email;
            utente.Nome = request.Nome;
            utente.Cognome = request.Cognome;
            utente.Telefono = request.Telefono;

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Profilo utente aggiornato", actorUserId, userId = id });
        });

        utentiGroup.MapDelete("/{id:int}", [Authorize(Roles = "Admin")] async (int id, HttpContext context, FilmDbContext db) =>
        {
            var currentUserId = GetUserId(context);
            if (currentUserId == id)
            {
                return Results.BadRequest(new { message = "Non puoi eliminare il tuo stesso account" });
            }

            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (utente == null)
            {
                return Results.NotFound(new { message = "Utente non trovato" });
            }

            var adminRuolo = await db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "Admin");
            if (adminRuolo != null)
            {
                var isAdmin = utente.UtentiRuoli.Any(ur => ur.RuoloId == adminRuolo.Id);
                if (isAdmin)
                {
                    var altriAdmin = await db.UtentiRuoli.CountAsync(ur => ur.RuoloId == adminRuolo.Id && ur.UtenteId != id);
                    if (altriAdmin == 0)
                    {
                        return Results.BadRequest(new { message = "Impossibile eliminare l'ultimo admin" });
                    }
                }
            }

            var powerUserRuolo = await db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "PowerUser");
            if (powerUserRuolo != null)
            {
                var isPowerUser = utente.UtentiRuoli.Any(ur => ur.RuoloId == powerUserRuolo.Id);
                if (isPowerUser)
                {
                    var altriPowerUser = await db.UtentiRuoli.CountAsync(ur => ur.RuoloId == powerUserRuolo.Id && ur.UtenteId != id);
                    if (altriPowerUser == 0)
                    {
                        return Results.BadRequest(new { message = "Impossibile eliminare l'ultimo PowerUser" });
                    }
                }
            }

            db.Utenti.Remove(utente);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Account eliminato definitivamente" });
        });

        utentiGroup.MapGet("/{id:int}/transazioni", [Authorize(Roles = "Admin")] async (int id, FilmDbContext db) =>
        {
            var utente = await db.Utenti
                .Where(u => u.Id == id)
                .Select(u => new { u.Id, u.Username, u.Email, u.Nome, u.Cognome })
                .FirstOrDefaultAsync();

            if (utente == null)
            {
                return Results.NotFound(new { message = "Utente non trovato" });
            }

            var transazioniCredito = await db.TransazioniCredito
                .Where(t => t.UtenteId == id)
                .OrderByDescending(t => t.DataTransazione)
                .Select(t => new
                {
                    t.Id,
                    Tipo = t.Tipo.ToString(),
                    t.Importo,
                    t.SaldoPrecedente,
                    t.SaldoSuccessivo,
                    t.DataTransazione,
                    t.Descrizione,
                    t.CinemaId,
                    t.OperatoreId,
                    t.AcquistoId
                })
                .Take(100)
                .ToListAsync();

            var acquisti = await db.Acquisti
                .Where(a => a.UtenteId == id)
                .OrderByDescending(a => a.DataAcquisto)
                .Select(a => new
                {
                    a.Id,
                    a.DataAcquisto,
                    a.ImportoTotale,
                    a.CreditoUsato,
                    Stato = a.Stato.ToString(),
                    a.MetodoPagamento,
                    a.MetodoPagamentoEtichetta,
                    a.CodiceConferma,
                    a.ShowId
                })
                .Take(100)
                .ToListAsync();

            return Results.Ok(new
            {
                utente,
                storicoCredito = transazioniCredito,
                storicoAcquisti = acquisti
            });
        });

        return app;
    }

    private static int GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    private static string ResolveFrontendBaseUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Absolute, out var parsed))
        {
            return $"{parsed.Scheme}://{parsed.Authority}";
        }

        return "https://filmhub-frontend.delightfuldune-f7916078.francecentral.azurecontainerapps.io";
    }
}
