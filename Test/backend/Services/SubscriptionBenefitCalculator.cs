using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public sealed record SubscriptionBenefitQuote(
    string Piano,
    int IngressiSettimanali,
    int UtilizziSettimana,
    int IngressiDisponibiliPrima,
    int IngressiApplicati,
    bool Include3D,
    bool IncludeScontoSnack,
    bool ProiezioneCopertaDalPiano,
    decimal ImportoLordo,
    decimal ScontoAbbonamento,
    decimal ImportoFinale,
    string Messaggio);

public static class SubscriptionBenefitCalculator
{
    public static async Task<SubscriptionBenefitQuote> QuoteAsync(
        FilmDbContext context,
        int utenteId,
        Show show,
        int numeroBiglietti)
    {
        numeroBiglietti = Math.Max(0, numeroBiglietti);
        var importoLordo = show.PrezzoBase * numeroBiglietti;
        var empty = new SubscriptionBenefitQuote(
            "Free",
            0,
            0,
            0,
            0,
            false,
            false,
            false,
            importoLordo,
            0m,
            importoLordo,
            "Nessun abbonamento attivo");

        if (numeroBiglietti <= 0)
        {
            return empty;
        }

        var abbonamento = await context.AbbonamentiUtente
            .Include(a => a.Utilizzi)
            .Where(a => a.UtenteId == utenteId && a.Stato == StatoAbbonamento.Attivo)
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        if (abbonamento is null)
        {
            return empty;
        }

        var plan = ResolvePlan(abbonamento.Piano);
        var is3D = show.Sala?.Tipologia == TipologiaSala.TRE_D;
        var projectionCovered = !is3D || plan.Include3D;
        var startOfWeek = GetStartOfWeek(DateTime.UtcNow);
        var usedThisWeek = abbonamento.Utilizzi.Count(u => u.DataUtilizzo >= startOfWeek);
        var availableBefore = Math.Max(0, plan.IngressiSettimanali - usedThisWeek);
        var includedTickets = projectionCovered ? Math.Min(numeroBiglietti, availableBefore) : 0;
        var discount = decimal.Round(show.PrezzoBase * includedTickets, 2, MidpointRounding.AwayFromZero);
        var final = Math.Max(0m, importoLordo - discount);

        var message = includedTickets > 0
            ? $"{includedTickets} ingresso/i inclusi dal piano {plan.Codice}"
            : is3D && !plan.Include3D
                ? $"Il piano {plan.Codice} non include proiezioni 3D"
                : $"Nessun ingresso incluso disponibile questa settimana per il piano {plan.Codice}";

        return new SubscriptionBenefitQuote(
            plan.Codice,
            plan.IngressiSettimanali,
            usedThisWeek,
            availableBefore,
            includedTickets,
            plan.Include3D,
            plan.IncludeScontoSnack,
            projectionCovered,
            importoLordo,
            discount,
            final,
            message);
    }

    public static void AddUsageRecords(FilmDbContext context, int abbonamentoId, int? showId, int count, string? note = null)
    {
        for (var i = 0; i < count; i++)
        {
            context.UtilizziAbbonamento.Add(new UtilizzoAbbonamento
            {
                AbbonamentoUtenteId = abbonamentoId,
                ShowId = showId,
                Note = note
            });
        }
    }

    public static async Task<int?> GetActiveSubscriptionIdAsync(FilmDbContext context, int utenteId)
    {
        return await context.AbbonamentiUtente
            .Where(a => a.UtenteId == utenteId && a.Stato == StatoAbbonamento.Attivo)
            .OrderByDescending(a => a.Id)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
    }

    public static DateTime GetStartOfWeek(DateTime value)
    {
        var date = value.Date;
        var diffToMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diffToMonday);
    }

    public static PlanBenefits ResolvePlan(TipoPianoAbbonamento piano)
    {
        return piano switch
        {
            TipoPianoAbbonamento.Base => new PlanBenefits("Base", 1, false, false),
            TipoPianoAbbonamento.Plus => new PlanBenefits("Plus", 3, true, false),
            TipoPianoAbbonamento.Premium => new PlanBenefits("Premium", 7, true, true),
            _ => new PlanBenefits("Free", 0, false, false)
        };
    }
}

public sealed record PlanBenefits(string Codice, int IngressiSettimanali, bool Include3D, bool IncludeScontoSnack);
