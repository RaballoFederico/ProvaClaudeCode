using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class BigliettoService(
    FilmDbContext context,
    ICreditoService creditoService,
    IPagamentoService pagamentoService,
    IEmailService emailService,
    IPdfService pdfService,
    IConfiguration configuration) : IBigliettoService
{
    public async Task<IEnumerable<PostoStatoDTO>> GetPiantinaStatoAsync(int showId)
    {
        var show = await context.Shows.Include(s => s.Sala).FirstOrDefaultAsync(s => s.Id == showId);
        if (show?.Sala is null) return Enumerable.Empty<PostoStatoDTO>();

        var occupied = await context.Biglietti.Where(b => b.ShowId == showId).Select(b => b.Posto).ToListAsync();
        var locked = await context.PrenotazioniTemporanee
            .Where(p => p.ShowId == showId && p.Stato == StatoPrenotazioneTemp.ATTIVA && p.DataScadenza > DateTime.UtcNow)
            .Select(p => p.Posto)
            .ToListAsync();

        var rows = BuildRows(show.Sala.NumeroFile, show.Sala.PostiPerFila, show.Sala.ConfigurazionePosti);
        var result = new List<PostoStatoDTO>();
        foreach (var row in rows)
        {
            for (var seat = 1; seat <= row.posti; seat++)
            {
                var posto = $"Fila {row.fila}, Posto {seat}";
                var stato = occupied.Contains(posto) ? "occupato" : (locked.Contains(posto) ? "prenotato" : "disponibile");
                result.Add(new PostoStatoDTO { Posto = posto, Stato = stato });
            }
        }

        return result;
    }

    public async Task<PrenotazioneTempDTO> LockPostiAsync(int utenteId, int showId, List<PostoDTO> posti, string sessionId)
    {
        if (posti.Count == 0) throw new InvalidOperationException("Nessun posto selezionato");
        if (posti.Count > 10) throw new InvalidOperationException("Massimo 10 posti");

        var scadenza = DateTime.UtcNow.AddMinutes(10);
        var codiceTemporaneo = Guid.NewGuid().ToString();

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            foreach (var posto in posti)
            {
                var postoText = posto.ToString();

                var occupato = await context.Biglietti.AnyAsync(b => b.ShowId == showId && b.Posto == postoText);
                if (occupato) throw new InvalidOperationException($"Posto occupato: {postoText}");

                var prenotato = await context.PrenotazioniTemporanee.AnyAsync(p => p.ShowId == showId && p.Posto == postoText && p.Stato == StatoPrenotazioneTemp.ATTIVA && p.DataScadenza > DateTime.UtcNow);
                if (prenotato) throw new InvalidOperationException($"Posto gia prenotato: {postoText}");

                context.PrenotazioniTemporanee.Add(new PrenotazioneTemporanea
                {
                    CodiceTemporaneo = codiceTemporaneo,
                    ShowId = showId,
                    Posto = postoText,
                    UtenteId = utenteId,
                    DataCreazione = DateTime.UtcNow,
                    DataScadenza = scadenza,
                    Stato = StatoPrenotazioneTemp.ATTIVA,
                    SessionId = sessionId
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new PrenotazioneTempDTO { CodiceTemporaneo = codiceTemporaneo, ShowId = showId, Posti = posti, DataScadenza = scadenza };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RinnovaLockAsync(string codiceTemporaneo)
    {
        var locks = await context.PrenotazioniTemporanee
            .Where(p => p.CodiceTemporaneo == codiceTemporaneo && p.Stato == StatoPrenotazioneTemp.ATTIVA)
            .ToListAsync();
        if (locks.Count == 0) return false;

        var nuova = DateTime.UtcNow.AddMinutes(10);
        foreach (var l in locks) l.DataScadenza = nuova;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<LockDettaglioDTO?> GetLockDettaglioAsync(string codiceTemporaneo, int utenteId)
    {
        var locks = await context.PrenotazioniTemporanee
            .Where(p => p.CodiceTemporaneo == codiceTemporaneo && p.UtenteId == utenteId && p.Stato == StatoPrenotazioneTemp.ATTIVA && p.DataScadenza > DateTime.UtcNow)
            .OrderBy(p => p.Id)
            .ToListAsync();

        if (locks.Count == 0) return null;

        var posti = locks
            .Select(p => ParsePosto(p.Posto))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        return new LockDettaglioDTO
        {
            CodiceTemporaneo = codiceTemporaneo,
            ShowId = locks[0].ShowId,
            DataScadenza = locks[0].DataScadenza,
            Posti = posti
        };
    }

    public async Task<bool> RilasciaLockAsync(string codiceTemporaneo)
    {
        var locks = await context.PrenotazioniTemporanee
            .Where(p => p.CodiceTemporaneo == codiceTemporaneo && p.Stato == StatoPrenotazioneTemp.ATTIVA)
            .ToListAsync();
        if (locks.Count == 0) return false;

        foreach (var l in locks) l.Stato = StatoPrenotazioneTemp.CANCELLATA;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<AcquistoResultDTO> ConfermaAcquistoAsync(int utenteId, ConfermaAcquistoDTO dto)
    {
        var locks = await context.PrenotazioniTemporanee
            .Include(p => p.Show)
            .ThenInclude(s => s!.Sala)
            .Where(p => p.CodiceTemporaneo == dto.CodiceTemporaneo && p.Stato == StatoPrenotazioneTemp.ATTIVA && p.DataScadenza > DateTime.UtcNow)
            .OrderBy(p => p.Id)
            .ToListAsync();

        if (locks.Count == 0) throw new InvalidOperationException("Lock non valido o scaduto");
        var show = locks.First().Show;
        if (show?.Sala is null) throw new InvalidOperationException("Show non valido");

        var importoTotale = show.PrezzoBase * locks.Count;
        var saldo = await creditoService.GetSaldoAsync(utenteId);
        var creditoUsato = dto.UsaCredito ? Math.Min(saldo, importoTotale) : 0m;
        var rimanenteCarta = importoTotale - creditoUsato;
        var externalPaymentId = string.Empty;
        if (rimanenteCarta > 0m)
        {
            if (string.IsNullOrWhiteSpace(dto.CheckoutSessionId))
                throw new InvalidOperationException("CheckoutSessionId richiesto per pagamento carta");

            var checkoutVerification = await pagamentoService.VerificaCheckoutSessionAsync(dto.CheckoutSessionId, rimanenteCarta);
            if (!checkoutVerification.Success)
                throw new InvalidOperationException("Pagamento checkout non verificato");

            externalPaymentId = checkoutVerification.PaymentIntentId;
        }

        var acquisto = new Acquisto
        {
            UtenteId = utenteId,
            ShowId = show.Id,
            DataAcquisto = DateTime.UtcNow,
            ImportoTotale = importoTotale,
            CreditoUsato = creditoUsato,
            StripeChargeId = externalPaymentId,
            MetodoPagamento = NormalizeValue(dto.PaymentMethodType, 50),
            MetodoPagamentoEtichetta = NormalizeValue(dto.PaymentMethodLabel, 120),
            MetodoPagamentoSalvato = dto.SavePaymentMethodForFuture,
            Stato = StatoAcquisto.PAGATO,
            CodiceConferma = Guid.NewGuid().ToString()
        };

        context.Acquisti.Add(acquisto);
        await context.SaveChangesAsync();

        if (creditoUsato > 0)
        {
            var ok = await creditoService.ScalaCreditoAsync(utenteId, creditoUsato, acquisto.Id);
            if (!ok) throw new InvalidOperationException("Credito insufficiente");
        }

        var biglietti = new List<Biglietto>();
        foreach (var l in locks)
        {
            l.Stato = StatoPrenotazioneTemp.CONFERMATA;

            var codiceUnivoco = Guid.NewGuid().ToString("N")[..20];
            var biglietto = new Biglietto
            {
                AcquistoId = acquisto.Id,
                ShowId = show.Id,
                Posto = l.Posto,
                SalaNumero = show.Sala.NumeroSala,
                TipologiaSala = show.Sala.Tipologia.ToString(),
                Prezzo = show.PrezzoBase,
                CodiceUnivoco = codiceUnivoco,
                CodiceHash = Guid.NewGuid().ToString("N"),
                CinemaId = show.Sala.CinemaId,
                QRCodeUrl = string.Empty
            };
            context.Biglietti.Add(biglietto);
            biglietti.Add(biglietto);
        }

        await context.SaveChangesAsync();

        foreach (var b in biglietti)
        {
            b.CodiceHash = GeneraCodiceHash(b.Id, acquisto.Id, b.Posto);
            b.QRCodeUrl = GeneraQRCodeUrl(b.CodiceHash);
        }

        await context.SaveChangesAsync();

        var acquistoConUtente = await context.Acquisti
            .Include(a => a.Utente)
            .FirstAsync(a => a.Id == acquisto.Id);

        var ticketPdfData = await context.Biglietti
            .Where(b => b.AcquistoId == acquisto.Id)
            .Join(context.Shows.Include(s => s.Film).Include(s => s.Sala),
                b => b.ShowId,
                s => s.Id,
                (b, s) => new { b, s })
            .Join(context.Cinemas,
                x => x.b.CinemaId,
                c => c.Id,
                (x, c) => new BigliettoPdfDTO
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
                })
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(acquistoConUtente.Utente.Email) && ticketPdfData.Count > 0)
        {
            var pdf = pdfService.GeneraBigliettiPdf(ticketPdfData, acquisto.CodiceConferma);
            var nominativo = $"{acquistoConUtente.Utente.Nome} {acquistoConUtente.Utente.Cognome}".Trim();
            if (string.IsNullOrWhiteSpace(nominativo)) nominativo = acquistoConUtente.Utente.Username;

            var html = EmailComposer.BuildConfermaAcquistoHtml(
                nominativo,
                acquisto.CodiceConferma,
                acquisto.ImportoTotale,
                acquisto.CreditoUsato,
                ticketPdfData,
                configuration["Branding:Name"] ?? "FilmAPI",
                configuration["Branding:PrimaryColor"] ?? "#0f172a",
                configuration["Branding:AccentColor"] ?? "#bfdbfe",
                configuration["Branding:EmailLogoUrl"],
                Environment.GetEnvironmentVariable("SMTP_FROM") ?? configuration["SMTP:From"]);

            var safeFilmTitle = string.Concat(ticketPdfData[0].FilmTitolo.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
            if (string.IsNullOrWhiteSpace(safeFilmTitle)) safeFilmTitle = "film";
            var attachmentName = $"FilmAPI-Biglietti-{DateTime.Now:yyyyMMdd}-{safeFilmTitle}-{acquisto.CodiceConferma}.pdf";
            var subject = $"FilmAPI | Conferma ordine {acquisto.CodiceConferma} - {ticketPdfData[0].FilmTitolo}";

            await emailService.InviaConfermaAcquistoAsync(
                acquistoConUtente.Utente.Email,
                subject,
                html,
                pdf,
                attachmentName);
        }

        return new AcquistoResultDTO
        {
            AcquistoId = acquisto.Id,
            CodiceConferma = acquisto.CodiceConferma,
            ImportoTotale = importoTotale,
            CreditoUsato = creditoUsato,
            Biglietti = biglietti.Select(ToDto).ToList()
        };
    }

    public async Task<BigliettoDTO?> GetBigliettoAsync(int id)
    {
        var b = await context.Biglietti.FirstOrDefaultAsync(x => x.Id == id);
        return b is null ? null : ToDto(b);
    }

    public async Task<IEnumerable<BigliettoDTO>> GetBigliettiUtenteAsync(int utenteId)
    {
        return await context.Biglietti
            .Include(b => b.Acquisto)
            .Where(b => b.Acquisto.UtenteId == utenteId)
            .OrderByDescending(b => b.Id)
            .Select(b => new BigliettoDTO
            {
                Id = b.Id,
                AcquistoId = b.AcquistoId,
                ShowId = b.ShowId,
                Posto = b.Posto,
                SalaNumero = b.SalaNumero,
                TipologiaSala = b.TipologiaSala,
                Prezzo = b.Prezzo,
                CodiceUnivoco = b.CodiceUnivoco,
                CodiceHash = b.CodiceHash,
                Validato = b.Validato,
                DataValidazione = b.DataValidazione,
                CinemaId = b.CinemaId,
                QRCodeUrl = b.QRCodeUrl
            })
            .ToListAsync();
    }

    public async Task<BigliettoValidazioneDTO?> GetBigliettoPerValidazioneAsync(string codiceHash)
    {
        var b = await context.Biglietti
            .Include(x => x.Show)
            .ThenInclude(s => s!.Film)
            .Include(x => x.Cinema)
            .FirstOrDefaultAsync(x => x.CodiceHash == codiceHash);

        if (b?.Show is null) return null;

        return new BigliettoValidazioneDTO
        {
            BigliettoId = b.Id,
            FilmTitolo = b.Show.Film?.Titolo ?? string.Empty,
            CinemaNome = b.Cinema?.Nome ?? string.Empty,
            SalaNumero = b.SalaNumero,
            TipologiaSala = b.TipologiaSala,
            Data = b.Show.Data,
            OraInizio = b.Show.OraInizio,
            Posto = b.Posto,
            CodiceHash = b.CodiceHash,
            GiaValidato = b.Validato
        };
    }

    public async Task<bool> ValidaBigliettoAsync(string codiceHash, int operatoreId, int cinemaId)
    {
        var b = await context.Biglietti.FirstOrDefaultAsync(x => x.CodiceHash == codiceHash);
        if (b is null || b.Validato || b.CinemaId != cinemaId) return false;
        b.Validato = true;
        b.DataValidazione = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public string GeneraCodiceHash(int bigliettoId, int acquistoId, string posto)
    {
        var raw = $"{bigliettoId}|{acquistoId}|{posto}|filmapi";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string GeneraQRCodeUrl(string codiceHash)
    {
        return $"https://filmapi.com/validazione/qr/{codiceHash}";
    }

    private static BigliettoDTO ToDto(Biglietto b) => new()
    {
        Id = b.Id,
        AcquistoId = b.AcquistoId,
        ShowId = b.ShowId,
        Posto = b.Posto,
        SalaNumero = b.SalaNumero,
        TipologiaSala = b.TipologiaSala,
        Prezzo = b.Prezzo,
        CodiceUnivoco = b.CodiceUnivoco,
        CodiceHash = b.CodiceHash,
        Validato = b.Validato,
        DataValidazione = b.DataValidazione,
        CinemaId = b.CinemaId,
        QRCodeUrl = b.QRCodeUrl
    };

    private static List<(int fila, int posti)> BuildRows(int numeroFile, int? postiPerFila, string? configurazione)
    {
        if (!string.IsNullOrWhiteSpace(configurazione))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(configurazione);
                if (doc.RootElement.TryGetProperty("file", out var rows) && rows.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var result = new List<(int fila, int posti)>();
                    foreach (var row in rows.EnumerateArray())
                    {
                        result.Add((row.GetProperty("fila").GetInt32(), row.GetProperty("posti").GetInt32()));
                    }
                    if (result.Count > 0) return result;
                }
            }
            catch
            {
            }
        }

        var per = postiPerFila ?? 10;
        return Enumerable.Range(1, numeroFile).Select(i => (i, per)).ToList();
    }

    private static PostoDTO? ParsePosto(string postoText)
    {
        var parts = postoText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return null;
        if (!parts[0].StartsWith("Fila ", StringComparison.OrdinalIgnoreCase)) return null;
        if (!parts[1].StartsWith("Posto ", StringComparison.OrdinalIgnoreCase)) return null;

        if (!int.TryParse(parts[0][5..], out var fila)) return null;
        if (!int.TryParse(parts[1][6..], out var numero)) return null;
        return new PostoDTO { Fila = fila, Numero = numero };
    }

    private static string? NormalizeValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength) return trimmed;
        return trimmed[..maxLength];
    }
}
