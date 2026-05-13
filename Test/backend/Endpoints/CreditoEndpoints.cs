using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class CreditoEndpoints
{
    public static IEndpointRouteBuilder MapCreditoEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/admin/credito").RequireAuthorization("PowerUserOrAdmin");
        var userGroup = app.MapGroup("/credito").RequireAuthorization("Authenticated");

        adminGroup.MapPost("/ricarica", async (HttpContext ctx, RicaricaCreditoDTO dto, ICreditoService creditoService, FilmDbContext db, IEmailService emailService, IPdfService pdfService, IConfiguration configuration) =>
        {
            var operatoreId = GetUserId(ctx);
            if (operatoreId is null) return Results.Unauthorized();
            var tx = await creditoService.RicaricaAsync(operatoreId.Value, dto);
            await InviaRicevutaRicaricaAsync(db, emailService, pdfService, configuration, tx, operatoreId.Value, isAdminTopUp: true);
            return Results.Ok(tx);
        });

        adminGroup.MapGet("/storico/{utenteId:int}", async (int utenteId, ICreditoService creditoService) =>
            Results.Ok(await creditoService.GetStoricoAsync(utenteId)));

        adminGroup.MapGet("/transazioni", async (
            int? utenteId,
            int? tipo,
            DateTime? dal,
            DateTime? al,
            int? cinemaId,
            ICreditoService creditoService) =>
        {
            var list = await creditoService.GetAllTransazioniAsync(new TransazioneFilterDTO
            {
                UtenteId = utenteId,
                Tipo = tipo,
                Dal = dal,
                Al = al,
                CinemaId = cinemaId
            });
            return Results.Ok(list);
        });

        adminGroup.MapGet("/ricerca-utente", async (string query, FilmDbContext db) =>
        {
            var users = await db.Utenti
                .Where(u => u.Email.Contains(query) || u.Username.Contains(query) || u.Id.ToString() == query)
                .Select(u => new { u.Id, u.Username, u.Email, u.Nome, u.Cognome })
                .Take(20)
                .ToListAsync();
            return Results.Ok(users);
        });

        userGroup.MapPost("/checkout-session", async (HttpContext ctx, CreateCheckoutSessionRequestDTO dto, IPagamentoService pagamentoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            if (dto.Importo <= 0)
            {
                return Results.BadRequest(new { message = "Importo non valido" });
            }

            var session = await pagamentoService.CreaCheckoutSessionAsync(
                dto.Importo,
                userId.Value,
                dto.SuccessUrl,
                dto.CancelUrl,
                "Ricarica credito FilmAPI",
                "filmapi_credit_topup",
                preferredPaymentMethodType: dto.PaymentMethodType);

            return Results.Ok(session);
        });

        userGroup.MapPost("/conferma-checkout", async (
            HttpContext ctx,
            ConfermaRicaricaCheckoutDTO dto,
            FilmDbContext db,
            IPagamentoService pagamentoService,
            ICreditoService creditoService,
            IEmailService emailService,
            IPdfService pdfService,
            IConfiguration configuration) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            if (dto.Importo <= 0)
            {
                return Results.BadRequest(new { message = "Importo non valido" });
            }

            if (string.IsNullOrWhiteSpace(dto.CheckoutSessionId))
            {
                return Results.BadRequest(new { message = "CheckoutSessionId mancante" });
            }

            var sessionTag = $"[stripe_session:{dto.CheckoutSessionId}]";
            var alreadyProcessed = await db.TransazioniCredito
                .AnyAsync(t => t.UtenteId == userId.Value
                               && t.Tipo == Model.TipoTransazione.RICARICA
                               && t.Descrizione != null
                               && t.Descrizione.Contains(sessionTag));
            if (alreadyProcessed)
            {
                return Results.Conflict(new { message = "Questa sessione Stripe e gia stata elaborata" });
            }

            var verification = await pagamentoService.VerificaCheckoutSessionAsync(dto.CheckoutSessionId, dto.Importo);
            if (!verification.Success)
            {
                return Results.BadRequest(new { message = "Pagamento Stripe non verificato" });
            }

            var baseDescrizione = string.IsNullOrWhiteSpace(dto.Descrizione)
                ? "Ricarica credito utente"
                : dto.Descrizione.Trim();
            var finalDescrizione = $"{baseDescrizione} {sessionTag}";

            var tx = await creditoService.RicaricaAsync(userId.Value, new RicaricaCreditoDTO
            {
                UtenteId = userId.Value,
                Importo = dto.Importo,
                Descrizione = finalDescrizione,
                CinemaId = null
            });

            await InviaRicevutaRicaricaAsync(db, emailService, pdfService, configuration, tx, userId.Value, isAdminTopUp: false);

            return Results.Ok(tx);
        });

        return app;
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }

    private static async Task InviaRicevutaRicaricaAsync(
        FilmDbContext db,
        IEmailService emailService,
        IPdfService pdfService,
        IConfiguration configuration,
        TransazioneCreditoDTO tx,
        int operatoreId,
        bool isAdminTopUp)
    {
        var utente = await db.Utenti.FirstOrDefaultAsync(u => u.Id == tx.UtenteId && u.Attivo);
        if (utente == null || string.IsNullOrWhiteSpace(utente.Email))
        {
            return;
        }

        var operatore = await db.Utenti.FirstOrDefaultAsync(u => u.Id == operatoreId);
        var operatoreNome = operatore == null
            ? "Sistema"
            : $"{operatore.Nome} {operatore.Cognome}".Trim();
        if (string.IsNullOrWhiteSpace(operatoreNome))
        {
            operatoreNome = operatore?.Username ?? "Sistema";
        }

        var nominativo = $"{utente.Nome} {utente.Cognome}".Trim();
        if (string.IsNullOrWhiteSpace(nominativo))
        {
            nominativo = utente.Username;
        }

        var causale = isAdminTopUp
            ? $"Ricarica effettuata da operatore ({operatoreNome})"
            : "Ricarica credito con pagamento carta (Stripe Checkout)";

        var html = EmailComposer.BuildRicevutaRicaricaCreditoHtml(
            nominativo,
            tx.Importo,
            tx.SaldoPrecedente,
            tx.SaldoSuccessivo,
            tx.DataTransazione.ToLocalTime(),
            causale,
            configuration["Branding:Name"] ?? "FilmAPI",
            configuration["Branding:PrimaryColor"] ?? "#0f172a",
            configuration["Branding:AccentColor"] ?? "#bfdbfe",
            configuration["Branding:EmailLogoUrl"],
            Environment.GetEnvironmentVariable("SMTP_FROM") ?? configuration["SMTP:From"]);

        var subjectPrefix = isAdminTopUp ? "Ricarica amministratore" : "Ricarica credito";
        var subject = $"FilmAPI | {subjectPrefix} +{tx.Importo:0.00} EUR";
        var attachmentName = $"FilmAPI-Ricevuta-Ricarica-{tx.DataTransazione:yyyyMMdd-HHmm}-{tx.Id}.pdf";
        var pdf = pdfService.GeneraRicevutaRicaricaPdf(
            nominativo,
            tx.Importo,
            tx.SaldoPrecedente,
            tx.SaldoSuccessivo,
            tx.DataTransazione.ToLocalTime(),
            causale);

        await emailService.InviaConfermaAcquistoAsync(utente.Email, subject, html, pdf, attachmentName);
    }
}
