// DOC: Endpoint 'NewsletterEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FilmAPI.Endpoints;

public static class NewsletterEndpoints
{
    private static readonly Regex EmailRegex = new(
        "^[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // DOC-METHOD: 'MapNewsletterEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static IEndpointRouteBuilder MapNewsletterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/newsletter/subscribe", async (NewsletterPublicSubscribeRequestDTO request, FilmDbContext db) =>
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
            {
                return Results.BadRequest(new { message = "Email non valida" });
            }

            var user = await db.Utenti.FirstOrDefaultAsync(u => u.Attivo && u.Email.ToLower() == email);
            if (user == null)
            {
                return Results.NotFound(new { message = "Nessun account trovato con questa email" });
            }

            user.ConsensoNewsletter = true;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Iscrizione newsletter attivata" });
        });

        var userGroup = app.MapGroup("/newsletter").RequireAuthorization("Authenticated");
        var adminGroup = app.MapGroup("/admin/newsletter").RequireAuthorization("AdminOnly");

        userGroup.MapGet("/preference", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var user = await db.Utenti.FindAsync(userId.Value);
            if (user == null) return Results.NotFound();
            return Results.Ok(new { consenso = user.ConsensoNewsletter });
        });

        userGroup.MapPut("/preference", async (HttpContext context, NewsletterPreferenceRequestDTO request, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var user = await db.Utenti.FindAsync(userId.Value);
            if (user == null) return Results.NotFound();
            user.ConsensoNewsletter = request.Consenso;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = request.Consenso ? "Iscrizione newsletter attivata" : "Iscrizione newsletter disattivata" });
        });

        adminGroup.MapGet("/campagne", async (FilmDbContext db, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("NewsletterEndpoints");
            try
            {
                var list = await db.NewsletterCampagne
                    .OrderByDescending(c => c.DataInvio)
                    .Take(50)
                    .Select(c => new
                    {
                        c.Id,
                        c.Oggetto,
                        c.DataInvio,
                        c.DestinatariCount,
                        c.CreatoDaUtenteId
                    })
                    .ToListAsync();
                return Results.Ok(list);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore caricamento storico campagne newsletter");
                return Results.Problem("Storico campagne temporaneamente non disponibile", statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        adminGroup.MapPost("/campagne/invia", async (HttpContext context, NewsletterCampagnaRequestDTO request, FilmDbContext db, IEmailService emailService) =>
        {
            var adminUserId = GetUserId(context);
            if (adminUserId == null) return Results.Unauthorized();

            var subject = (request.Oggetto ?? string.Empty).Trim();
            var html = (request.HtmlBody ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(html))
            {
                return Results.BadRequest(new { message = "Oggetto e contenuto sono obbligatori" });
            }

            var recipients = await db.Utenti
                .Where(u => u.Attivo && u.ConsensoNewsletter && !string.IsNullOrWhiteSpace(u.Email))
                .Select(u => u.Email)
                .ToListAsync();

            if (recipients.Count == 0)
            {
                return Results.BadRequest(new { message = "Nessun destinatario iscritto alla newsletter" });
            }

            var sent = 0;
            foreach (var email in recipients)
            {
                await emailService.InviaEmailStrictAsync(email, subject, html);
                sent++;
            }

            db.NewsletterCampagne.Add(new NewsletterCampagna
            {
                Oggetto = subject,
                HtmlBody = html,
                CreatoDaUtenteId = adminUserId.Value,
                DataInvio = DateTime.UtcNow,
                DestinatariCount = sent
            });
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Campagna inviata", destinatari = sent });
        });

        return app;
    }

    // DOC-METHOD: 'GetUserId' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }
}

