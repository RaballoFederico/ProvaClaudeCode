// DOC: AbbonamentiEndpoints - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Endpoint 'AbbonamentiEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class AbbonamentiEndpoints
{
    private static readonly List<PianoAbbonamentoDTO> Piani = new()
    {
        new PianoAbbonamentoDTO { Codice = "Base", Nome = "Base", PrezzoMensile = 14.90m, IngressiSettimanali = 1, Include3D = false, IncludeScontoSnack = false },
        new PianoAbbonamentoDTO { Codice = "Plus", Nome = "Plus", PrezzoMensile = 29.90m, IngressiSettimanali = 3, Include3D = true, IncludeScontoSnack = false },
        new PianoAbbonamentoDTO { Codice = "Premium", Nome = "Premium", PrezzoMensile = 49.90m, IngressiSettimanali = 7, Include3D = true, IncludeScontoSnack = true }
    };

    // DOC-METHOD: 'MapAbbonamentiEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static IEndpointRouteBuilder MapAbbonamentiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/abbonamenti").RequireAuthorization("Authenticated");

        group.MapGet("/piani", () => Results.Ok(Piani));

        group.MapPost("/checkout-session", async (HttpContext context, AbbonamentoCheckoutSessionRequestDTO request, IPagamentoService pagamentoService) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            if (!TryResolvePiano(request.Piano, out var pianoEnum, out var pianoInfo))
            {
                return Results.BadRequest(new { message = "Piano non valido" });
            }

            if (string.IsNullOrWhiteSpace(request.SuccessUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
            {
                return Results.BadRequest(new { message = "URL di redirect non validi" });
            }

            try
            {
                var session = await pagamentoService.CreaCheckoutSessionAsync(
                    pianoInfo.PrezzoMensile,
                    userId.Value,
                    request.SuccessUrl,
                    request.CancelUrl,
                    productName: $"Abbonamento {pianoInfo.Nome}",
                    integration: "filmapi_subscription_checkout",
                    extraMetadata: new Dictionary<string, string>
                    {
                        { "piano", pianoEnum.ToString() }
                    },
                    preferredPaymentMethodType: request.PaymentMethodType);

                return Results.Ok(session);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"Checkout abbonamento non disponibile: {ex.Message}" });
            }
        });

        group.MapPost("/conferma-checkout", async (HttpContext context, ConfermaAbbonamentoCheckoutRequestDTO request, FilmDbContext db, IPagamentoService pagamentoService) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            if (!TryResolvePiano(request.Piano, out var pianoEnum, out var pianoInfo))
            {
                return Results.BadRequest(new { message = "Piano non valido" });
            }

            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return Results.BadRequest(new { message = "Sessione checkout non valida" });
            }

            var verifica = await pagamentoService.VerificaCheckoutSessionAsync(request.SessionId, pianoInfo.PrezzoMensile);
            if (!verifica.Success)
            {
                return Results.BadRequest(new { message = "Pagamento non confermato" });
            }

            var activeSamePlan = await db.AbbonamentiUtente
                .Where(a => a.UtenteId == userId.Value && a.Stato == StatoAbbonamento.Attivo && a.Piano == pianoEnum)
                .OrderByDescending(a => a.DataInizio)
                .FirstOrDefaultAsync();

            if (activeSamePlan != null && activeSamePlan.DataInizio >= DateTime.UtcNow.AddMinutes(-10))
            {
                return Results.Ok(new { message = $"Abbonamento {pianoEnum} gia attivo", alreadyActive = true });
            }

            await AttivaPianoAsync(db, userId.Value, pianoEnum);
            return Results.Ok(new { message = $"Abbonamento {pianoEnum} attivato con successo" });
        });

        group.MapGet("/mio", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var abbonamento = await db.AbbonamentiUtente
                    .Include(a => a.Utilizzi)
                    .Where(a => a.UtenteId == userId.Value)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefaultAsync();

                if (abbonamento == null)
                {
                    return Results.Ok(new { hasSubscription = false });
                }

                var now = DateTime.UtcNow.Date;
                var diffToMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var inizioSettimana = now.AddDays(-diffToMonday);

                var utilizziSettimana = (abbonamento.Utilizzi ?? new List<UtilizzoAbbonamento>())
                    .Count(u => u.DataUtilizzo >= inizioSettimana);

                var piano = Piani.FirstOrDefault(p => string.Equals(p.Codice, abbonamento.Piano.ToString(), StringComparison.OrdinalIgnoreCase));

                return Results.Ok(new
                {
                    hasSubscription = true,
                    abbonamento.Id,
                    piano = abbonamento.Piano.ToString(),
                    stato = abbonamento.Stato.ToString(),
                    abbonamento.DataInizio,
                    abbonamento.ProssimoRinnovo,
                    abbonamento.DataDisdetta,
                    ingressiSettimanali = piano?.IngressiSettimanali ?? 0,
                    utilizziSettimana
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    hasSubscription = false,
                    fallback = true,
                    message = $"Dettaglio abbonamento non disponibile: {ex.Message}"
                });
            }
        });

        group.MapPost("/attiva", async (HttpContext context, AttivaAbbonamentoRequestDTO request, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            if (!Enum.TryParse<TipoPianoAbbonamento>(request.Piano, true, out var piano))
            {
                return Results.BadRequest(new { message = "Piano non valido" });
            }

            await AttivaPianoAsync(db, userId.Value, piano);
            return Results.Ok(new { message = $"Abbonamento {piano} attivato con successo" });
        });

        group.MapPost("/disdici", async (HttpContext context, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var active = await db.AbbonamentiUtente
                .Where(a => a.UtenteId == userId.Value && a.Stato == StatoAbbonamento.Attivo)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            if (active == null)
            {
                return Results.BadRequest(new { message = "Nessun abbonamento attivo da disdire" });
            }

            active.Stato = StatoAbbonamento.Disdetto;
            active.DataDisdetta = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Abbonamento disdetto" });
        });

        group.MapPost("/usa", async (HttpContext context, RegistraUtilizzoAbbonamentoRequestDTO request, FilmDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId == null) return Results.Unauthorized();

            var active = await db.AbbonamentiUtente
                .Include(a => a.Utilizzi)
                .Where(a => a.UtenteId == userId.Value && a.Stato == StatoAbbonamento.Attivo)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            if (active == null)
            {
                return Results.BadRequest(new { message = "Nessun abbonamento attivo" });
            }

            var piano = Piani.FirstOrDefault(p => string.Equals(p.Codice, active.Piano.ToString(), StringComparison.OrdinalIgnoreCase));
            var weeklyLimit = piano?.IngressiSettimanali ?? 0;
            var inizioSettimana = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
            var usatiSettimana = active.Utilizzi.Count(u => u.DataUtilizzo >= inizioSettimana);

            if (usatiSettimana >= weeklyLimit)
            {
                return Results.BadRequest(new { message = "Hai raggiunto il limite settimanale del tuo piano" });
            }

            db.UtilizziAbbonamento.Add(new UtilizzoAbbonamento
            {
                AbbonamentoUtenteId = active.Id,
                ShowId = request.ShowId,
                Note = request.Note
            });
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Ingresso registrato", usatiSettimana = usatiSettimana + 1, limite = weeklyLimit });
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

    // DOC-METHOD: 'TryResolvePiano' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static bool TryResolvePiano(string? pianoRaw, out TipoPianoAbbonamento pianoEnum, out PianoAbbonamentoDTO pianoInfo)
    {
        if (Enum.TryParse<TipoPianoAbbonamento>(pianoRaw, true, out pianoEnum))
        {
            var pianoName = pianoEnum.ToString();
            pianoInfo = Piani.FirstOrDefault(p => string.Equals(p.Codice, pianoName, StringComparison.OrdinalIgnoreCase))
                ?? new PianoAbbonamentoDTO { Codice = pianoName, Nome = pianoName, PrezzoMensile = 0m };
            return true;
        }

        pianoInfo = new PianoAbbonamentoDTO();
        return false;
    }

    // DOC-METHOD: 'AttivaPianoAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static async Task AttivaPianoAsync(FilmDbContext db, int userId, TipoPianoAbbonamento piano)
    {
        var active = await db.AbbonamentiUtente
            .Where(a => a.UtenteId == userId && a.Stato == StatoAbbonamento.Attivo)
            .ToListAsync();

        foreach (var a in active)
        {
            a.Stato = StatoAbbonamento.Disdetto;
            a.DataDisdetta = DateTime.UtcNow;
        }

        db.AbbonamentiUtente.Add(new AbbonamentoUtente
        {
            UtenteId = userId,
            Piano = piano,
            Stato = StatoAbbonamento.Attivo,
            DataInizio = DateTime.UtcNow,
            ProssimoRinnovo = DateTime.UtcNow.AddMonths(1)
        });

        await db.SaveChangesAsync();
    }
}


