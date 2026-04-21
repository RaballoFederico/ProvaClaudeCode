using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class AcquistoEndpoints
{
    public static IEndpointRouteBuilder MapAcquistoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/acquisto").RequireAuthorization("Authenticated");

        group.MapGet("/{showId:int}/piantina", async (int showId, IBigliettoService bigliettoService) =>
        {
            var data = await bigliettoService.GetPiantinaStatoAsync(showId);
            return Results.Ok(data);
        });

        group.MapPost("/lock-posti", async (HttpContext ctx, LockPostiRequestDTO dto, IBigliettoService bigliettoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var result = await bigliettoService.LockPostiAsync(userId.Value, dto.ShowId, dto.Posti, dto.SessionId);
            return Results.Ok(result);
        });

        group.MapPost("/rinnova-lock", async (RinnovaLockRequestDTO dto, IBigliettoService bigliettoService) =>
        {
            var ok = await bigliettoService.RinnovaLockAsync(dto.CodiceTemporaneo);
            return ok ? Results.Ok(new { success = true }) : Results.NotFound();
        });

        group.MapDelete("/lock/{codice}", async (string codice, IBigliettoService bigliettoService) =>
        {
            var ok = await bigliettoService.RilasciaLockAsync(codice);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/calcola-importo", async (HttpContext ctx, CalcoloImportoRequestDTO dto, IPagamentoService pagamentoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var res = await pagamentoService.CalcolaImportoAsync(userId.Value, dto);
            return Results.Ok(res);
        });

        group.MapPost("/payment-intent", async (HttpContext ctx, CreatePaymentIntentRequestDTO dto, IPagamentoService pagamentoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var intent = await pagamentoService.CreaPaymentIntentAsync(dto.Importo, userId.Value);
            return Results.Ok(intent);
        });

        group.MapGet("/stripe-publishable", (IConfiguration cfg) =>
        {
            var key = Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")
                ?? cfg["Stripe:PublishableKey"]
                ?? string.Empty;

            if (key.StartsWith("pk_test_", StringComparison.OrdinalIgnoreCase) && key.Contains("..."))
                key = string.Empty;

            return Results.Ok(new { publishableKey = key });
        });

        group.MapGet("/lock/{codice}", async (string codice, HttpContext ctx, IBigliettoService bigliettoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var lockInfo = await bigliettoService.GetLockDettaglioAsync(codice, userId.Value);
            return lockInfo is null ? Results.NotFound() : Results.Ok(lockInfo);
        });

        group.MapPost("/conferma", async (HttpContext ctx, ConfermaAcquistoDTO dto, IBigliettoService bigliettoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            try
            {
                var res = await bigliettoService.ConfermaAcquistoAsync(userId.Value, dto);
                return Results.Ok(res);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPost("/pagamento", async (HttpContext ctx, PagamentoRequestDTO dto, IPagamentoService pagamentoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var res = await pagamentoService.ProcessaPagamentoAsync(userId.Value, dto);
            return Results.Ok(res);
        });

        group.MapGet("/{id:int}/biglietti", async (int id, HttpContext ctx, FilmAPI.Data.FilmDbContext db, IBigliettoService bigliettoService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();
            var acquisto = await db.Acquisti.FindAsync(id);
            if (acquisto is null || acquisto.UtenteId != userId.Value) return Results.NotFound();
            var list = (await bigliettoService.GetBigliettiUtenteAsync(userId.Value)).Where(b => b.AcquistoId == id).ToList();
            return Results.Ok(list);
        });

        group.MapGet("/{id:int}/pdf", async (int id, HttpContext ctx, FilmAPI.Data.FilmDbContext db, IPdfService pdfService) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var acquisto = await db.Acquisti.Include(a => a.Biglietti).FirstOrDefaultAsync(a => a.Id == id);
            if (acquisto is null || acquisto.UtenteId != userId.Value) return Results.NotFound();

            var data = await db.Biglietti
                .Where(b => b.AcquistoId == id)
                .Join(db.Shows.Include(s => s.Film), b => b.ShowId, s => s.Id, (b, s) => new { b, s })
                .Join(db.Cinemas, x => x.b.CinemaId, c => c.Id, (x, c) => new BigliettoPdfDTO
                {
                    BigliettoId = x.b.Id,
                    FilmTitolo = x.s.Film != null ? x.s.Film.Titolo : string.Empty,
                    Data = x.s.Data,
                    OraInizio = x.s.OraInizio,
                    NomeCinema = c.Nome,
                    CodiceLocaleCinema = c.CodiceLocale ?? string.Empty,
                    IndirizzoCinema = $"{c.Indirizzo}, {c.Citta}",
                    SalaNumero = x.b.SalaNumero,
                    TipologiaSala = x.b.TipologiaSala,
                    Posto = x.b.Posto,
                    Prezzo = x.b.Prezzo,
                    CodiceUnivoco = x.b.CodiceUnivoco,
                    CodiceHash = x.b.CodiceHash,
                    QRCodeUrl = x.b.QRCodeUrl
                }).ToListAsync();

            if (data.Count == 0) return Results.NotFound();
            var pdf = pdfService.GeneraBigliettiPdf(data, acquisto.CodiceConferma);
            return Results.File(pdf, "application/pdf", $"biglietti-{acquisto.CodiceConferma}.pdf");
        });

        return app;
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }
}
