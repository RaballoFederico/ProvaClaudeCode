using System.Security.Claims;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class SupportEndpoints
{
    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/support/tickets").RequireAuthorization("Authenticated");

        group.MapGet("/mine", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var tickets = await db.SupportTickets
                .AsNoTracking()
                .Include(t => t.Utente)
                .Include(t => t.AssegnatoA)
                .Where(t => t.UtenteId == userId.Value)
                .OrderByDescending(t => t.AggiornatoIl)
                .Take(80)
                .ToListAsync();

            return Results.Ok(tickets.Select(TicketListDto));
        });

        group.MapGet("/", [Authorize(Roles = "Admin,PowerUser")] async (
            FilmDbContext db,
            string? status,
            string? priority,
            string? category) =>
        {
            var query = db.SupportTickets.AsNoTracking();

            if (Enum.TryParse<SupportTicketStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(t => t.Stato == parsedStatus);
            }

            if (Enum.TryParse<SupportTicketPriority>(priority, true, out var parsedPriority))
            {
                query = query.Where(t => t.Priorita == parsedPriority);
            }

            if (Enum.TryParse<SupportTicketCategory>(category, true, out var parsedCategory))
            {
                query = query.Where(t => t.Categoria == parsedCategory);
            }

            var tickets = await query
                .Include(t => t.Utente)
                .Include(t => t.AssegnatoA)
                .OrderBy(t => t.Stato == SupportTicketStatus.Chiuso)
                .ThenByDescending(t => t.Priorita)
                .ThenByDescending(t => t.AggiornatoIl)
                .Take(120)
                .ToListAsync();

            return Results.Ok(tickets.Select(TicketListDto));
        });

        group.MapPost("/", async (CreateSupportTicketRequest request, HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var oggetto = Normalize(request.Oggetto ?? request.Subject ?? request.Title, 160);
            var messaggio = Normalize(request.Messaggio ?? request.Message ?? request.Body, 4000);
            var categoriaRaw = request.Categoria ?? request.Category;
            var prioritaRaw = request.Priorita ?? request.Priority;

            if (string.IsNullOrWhiteSpace(oggetto) && !string.IsNullOrWhiteSpace(messaggio))
            {
                oggetto = $"Richiesta supporto - {Normalize(categoriaRaw, 40) ?? "Altro"}";
            }
            if (string.IsNullOrWhiteSpace(oggetto) || string.IsNullOrWhiteSpace(messaggio))
            {
                return Results.BadRequest(new { message = "Oggetto e messaggio sono obbligatori." });
            }

            var now = DateTime.UtcNow;
            var ticket = new SupportTicket
            {
                UtenteId = userId.Value,
                Oggetto = oggetto,
                Categoria = ParseEnum(categoriaRaw, SupportTicketCategory.Altro),
                Priorita = ParseEnum(prioritaRaw, SupportTicketPriority.Media),
                Stato = SupportTicketStatus.Aperto,
                CreatoIl = now,
                AggiornatoIl = now,
                Messaggi = new List<SupportTicketMessage>
                {
                    new()
                    {
                        AutoreId = userId.Value,
                        Staff = false,
                        Messaggio = messaggio,
                        CreatoIl = now
                    }
                }
            };

            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync();
            await NotifyManagersAsync(db, ticket.Id, "Nuovo ticket supporto", ticket.Oggetto);
            await db.SaveChangesAsync();

            return Results.Created($"/support/tickets/{ticket.Id}", new { ticket.Id });
        });

        group.MapGet("/{id:int}", async (int id, HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var isManager = IsManager(context);
            var ticket = await db.SupportTickets
                .AsNoTracking()
                .Include(t => t.Utente)
                .Include(t => t.AssegnatoA)
                .Include(t => t.Messaggi)
                .ThenInclude(m => m.Autore)
                .FirstOrDefaultAsync(t => t.Id == id && (isManager || t.UtenteId == userId.Value));

            if (ticket is null) return Results.NotFound();
            return Results.Ok(TicketDetailDto(ticket));
        });

        group.MapPost("/{id:int}/messages", async (int id, AddSupportMessageRequest request, HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var isManager = IsManager(context);
            var message = Normalize(request.Messaggio, 4000);
            if (string.IsNullOrWhiteSpace(message))
            {
                return Results.BadRequest(new { message = "Messaggio obbligatorio." });
            }

            var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && (isManager || t.UtenteId == userId.Value));
            if (ticket is null) return Results.NotFound();
            if (ticket.Stato == SupportTicketStatus.Chiuso && !isManager)
            {
                return Results.BadRequest(new { message = "Il ticket e chiuso. Chiedi allo staff di riaprirlo." });
            }

            var now = DateTime.UtcNow;
            db.SupportTicketMessages.Add(new SupportTicketMessage
            {
                SupportTicketId = id,
                AutoreId = userId.Value,
                Staff = isManager,
                Messaggio = message,
                CreatoIl = now
            });

            ticket.AggiornatoIl = now;
            if (isManager)
            {
                ticket.Stato = SupportTicketStatus.InAttesaUtente;
                await NotifyUserAsync(db, ticket.UtenteId, id, "Risposta al tuo ticket", ticket.Oggetto);
            }
            else
            {
                ticket.Stato = SupportTicketStatus.Aperto;
                await NotifyManagersAsync(db, id, "Nuova risposta utente", ticket.Oggetto);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Messaggio inviato" });
        });

        group.MapPut("/{id:int}/assign-me", [Authorize(Roles = "Admin,PowerUser")] async (int id, HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var ticket = await db.SupportTickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();

            ticket.AssegnatoAId = userId.Value;
            ticket.Stato = SupportTicketStatus.InLavorazione;
            ticket.AggiornatoIl = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Ticket preso in carico" });
        });

        group.MapPut("/{id:int}/status", [Authorize(Roles = "Admin,PowerUser")] async (int id, UpdateSupportTicketStatusRequest request, FilmDbContext db) =>
        {
            var ticket = await db.SupportTickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();

            var status = ParseEnum(request.Stato, ticket.Stato);
            ticket.Stato = status;
            ticket.AggiornatoIl = DateTime.UtcNow;
            ticket.ChiusoIl = status == SupportTicketStatus.Chiuso ? DateTime.UtcNow : null;
            await NotifyUserAsync(db, ticket.UtenteId, id, "Stato ticket aggiornato", $"{ticket.Oggetto}: {status}");
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Stato aggiornato" });
        });

        return app;
    }

    private static int? GetUserId(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    private static bool IsManager(HttpContext context)
    {
        return context.User.IsInRole("Admin") || context.User.IsInRole("PowerUser");
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : normalized[..max];
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var result) ? result : fallback;
    }

    private static async Task NotifyManagersAsync(FilmDbContext db, int ticketId, string title, string message)
    {
        var managerIds = await db.UtentiRuoli
            .Where(ur => ur.Ruolo.Nome == "Admin" || ur.Ruolo.Nome == "PowerUser")
            .Select(ur => ur.UtenteId)
            .Distinct()
            .ToListAsync();

        foreach (var managerId in managerIds)
        {
            db.NotificheUtente.Add(BuildNotification(managerId, ticketId, title, message));
        }
    }

    private static Task NotifyUserAsync(FilmDbContext db, int userId, int ticketId, string title, string message)
    {
        db.NotificheUtente.Add(BuildNotification(userId, ticketId, title, message));
        return Task.CompletedTask;
    }

    private static NotificaUtente BuildNotification(int userId, int ticketId, string title, string message)
    {
        return new NotificaUtente
        {
            UtenteId = userId,
            Tipo = "support",
            Titolo = Normalize(title, 140) ?? "Supporto",
            Messaggio = Normalize(message, 500),
            Url = $"/supporto.html?ticketId={ticketId}",
            DedupeKey = Normalize($"support:{ticketId}:{Guid.NewGuid():N}", 120),
            Letta = false,
            DataCreazione = DateTime.UtcNow
        };
    }

    private static object TicketListDto(SupportTicket ticket)
    {
        return new
        {
            ticket.Id,
            ticket.Oggetto,
            Categoria = ticket.Categoria.ToString(),
            Priorita = ticket.Priorita.ToString(),
            Stato = ticket.Stato.ToString(),
            ticket.CreatoIl,
            ticket.AggiornatoIl,
            Utente = ticket.Utente == null ? null : new { ticket.Utente.Id, ticket.Utente.Username, ticket.Utente.Email },
            AssegnatoA = ticket.AssegnatoA == null ? null : new { ticket.AssegnatoA.Id, ticket.AssegnatoA.Username }
        };
    }

    private static object TicketDetailDto(SupportTicket ticket)
    {
        return new
        {
            ticket.Id,
            ticket.Oggetto,
            Categoria = ticket.Categoria.ToString(),
            Priorita = ticket.Priorita.ToString(),
            Stato = ticket.Stato.ToString(),
            ticket.CreatoIl,
            ticket.AggiornatoIl,
            ticket.ChiusoIl,
            Utente = ticket.Utente == null ? null : new { ticket.Utente.Id, ticket.Utente.Username, ticket.Utente.Email },
            AssegnatoA = ticket.AssegnatoA == null ? null : new { ticket.AssegnatoA.Id, ticket.AssegnatoA.Username },
            Messaggi = ticket.Messaggi
                .OrderBy(m => m.CreatoIl)
                .Select(m => new
                {
                    m.Id,
                    m.Staff,
                    m.Messaggio,
                    m.CreatoIl,
                    Autore = m.Autore == null ? null : new { m.Autore.Id, m.Autore.Username }
                })
                .ToList()
        };
    }

    private sealed class CreateSupportTicketRequest
    {
        public string? Oggetto { get; set; }
        public string? Subject { get; set; }
        public string? Title { get; set; }
        public string? Categoria { get; set; }
        public string? Category { get; set; }
        public string? Priorita { get; set; }
        public string? Priority { get; set; }
        public string? Messaggio { get; set; }
        public string? Message { get; set; }
        public string? Body { get; set; }
    }

    private sealed class AddSupportMessageRequest
    {
        public string? Messaggio { get; set; }
    }

    private sealed class UpdateSupportTicketStatusRequest
    {
        public string? Stato { get; set; }
    }
}
