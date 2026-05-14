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
            foreach (var seat in row.posti)
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

        var show = await context.Shows.Include(s => s.Sala).FirstOrDefaultAsync(s => s.Id == showId);
        if (show?.Sala is null) throw new InvalidOperationException("Show non valido");
        var validSeats = BuildRows(show.Sala.NumeroFile, show.Sala.PostiPerFila, show.Sala.ConfigurazionePosti)
            .SelectMany(r => r.posti.Select(p => $"Fila {r.fila}, Posto {p}"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scadenza = DateTime.UtcNow.AddMinutes(10);
        var codiceTemporaneo = Guid.NewGuid().ToString();

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            foreach (var posto in posti)
            {
                var postoText = posto.ToString();
                if (!validSeats.Contains(postoText))
                    throw new InvalidOperationException($"Posto non valido per questa sala: {postoText}");

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

    public async Task<bool> RinnovaLockAsync(int utenteId, string codiceTemporaneo)
    {
        var locks = await context.PrenotazioniTemporanee
            .Where(p => p.CodiceTemporaneo == codiceTemporaneo
                        && p.UtenteId == utenteId
                        && p.Stato == StatoPrenotazioneTemp.ATTIVA)
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

    public async Task<bool> RilasciaLockAsync(int utenteId, string codiceTemporaneo)
    {
        var locks = await context.PrenotazioniTemporanee
            .Where(p => p.CodiceTemporaneo == codiceTemporaneo
                        && p.UtenteId == utenteId
                        && p.Stato == StatoPrenotazioneTemp.ATTIVA)
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

    public async Task<(bool success, string message)> RichiediRimborsoAsync(int utenteId, int acquistoId)
    {
        var acquisto = await context.Acquisti
            .Include(a => a.Show)
            .Include(a => a.Biglietti)
            .FirstOrDefaultAsync(a => a.Id == acquistoId && a.UtenteId == utenteId);

        if (acquisto is null)
        {
            return (false, "Acquisto non trovato");
        }

        if (acquisto.Stato != StatoAcquisto.PAGATO)
        {
            return (false, "Rimborso non disponibile per questo acquisto");
        }

        if (acquisto.Biglietti.Any(b => b.Validato))
        {
            return (false, "Rimborso non disponibile: uno o piu biglietti risultano gia validati");
        }

        if (acquisto.Show is null)
        {
            return (false, "Show associato non valido");
        }

        var showStart = acquisto.Show.Data.ToDateTime(acquisto.Show.OraInizio);
        if (showStart <= DateTime.Now)
        {
            return (false, "Rimborso disponibile solo prima dell'inizio proiezione");
        }

        var (alreadyCredited, alreadyCardRefunded) = await GetRefundedAmountsAsync(acquisto.Id, utenteId);
        var creditoDaRimborsare = Math.Max(0m, acquisto.CreditoUsato - alreadyCredited);
        var cartaTotale = Math.Max(0m, acquisto.ImportoTotale - acquisto.CreditoUsato);
        var cartaDaRimborsare = Math.Max(0m, cartaTotale - alreadyCardRefunded);

        if (cartaDaRimborsare > 0m)
        {
            if (string.IsNullOrWhiteSpace(acquisto.StripeChargeId))
            {
                return (false, "Impossibile rimborsare la quota carta: PaymentIntent mancante");
            }

            var stripeRefund = await pagamentoService.RimborsaPagamentoStripeAsync(
                acquisto.StripeChargeId,
                cartaDaRimborsare,
                $"Rimborso acquisto #{acquisto.Id}");

            if (!stripeRefund.Success)
            {
                return (false, stripeRefund.Message);
            }
        }

        if (creditoDaRimborsare > 0m)
        {
            var descrizioneCredito = $"Rimborso acquisto #{acquisto.Id} [rimborso_credito:{creditoDaRimborsare:F2}]";
            await creditoService.RicaricaAsync(utenteId, new RicaricaCreditoDTO
            {
                UtenteId = utenteId,
                Importo = creditoDaRimborsare,
                Descrizione = descrizioneCredito,
                CinemaId = null
            });
        }

        if (cartaDaRimborsare > 0m)
        {
            var credito = await context.CreditiUtente.FirstOrDefaultAsync(c => c.UtenteId == utenteId);
            if (credito is null)
            {
                credito = new CreditoUtente
                {
                    UtenteId = utenteId,
                    Saldo = 0m,
                    DataUltimoAggiornamento = DateTime.UtcNow
                };
                context.CreditiUtente.Add(credito);
                await context.SaveChangesAsync();
            }

            context.TransazioniCredito.Add(new TransazioneCredito
            {
                UtenteId = utenteId,
                Tipo = TipoTransazione.RIMBORSO,
                Importo = 0m,
                SaldoPrecedente = credito.Saldo,
                SaldoSuccessivo = credito.Saldo,
                DataTransazione = DateTime.UtcNow,
                OperatoreId = utenteId,
                CinemaId = null,
                AcquistoId = acquisto.Id,
                Descrizione = $"Rimborso acquisto #{acquisto.Id} [rimborso_carta:{cartaDaRimborsare:F2}]"
            });
        }

        acquisto.Stato = StatoAcquisto.REFUNDED;
        await context.SaveChangesAsync();
        return (true, "Rimborso completato: quota carta su Stripe e quota credito sul saldo sito");
    }

    public async Task<(bool success, string message)> RichiediRimborsoBigliettoAsync(int utenteId, int bigliettoId)
    {
        var biglietto = await context.Biglietti
            .Include(b => b.Acquisto)
            .ThenInclude(a => a.Show)
            .Include(b => b.Acquisto)
            .ThenInclude(a => a.Biglietti)
            .FirstOrDefaultAsync(b => b.Id == bigliettoId && b.Acquisto.UtenteId == utenteId);

        if (biglietto is null)
        {
            return (false, "Biglietto non trovato");
        }

        if (biglietto.Acquisto.Stato != StatoAcquisto.PAGATO)
        {
            return (false, "Rimborso non disponibile per questo acquisto");
        }

        if (biglietto.Validato)
        {
            return (false, "Rimborso non disponibile: biglietto gia validato");
        }

        if (biglietto.Acquisto.Show is null)
        {
            return (false, "Show associato non valido");
        }

        var showStart = biglietto.Acquisto.Show.Data.ToDateTime(biglietto.Acquisto.Show.OraInizio);
        if (showStart <= DateTime.Now)
        {
            return (false, "Rimborso disponibile solo prima dell'inizio proiezione");
        }

        var marker = $"[rimborso_biglietto:{biglietto.Id}]";
        var alreadyRefunded = await context.TransazioniCredito.AnyAsync(t =>
            t.UtenteId == utenteId &&
            t.AcquistoId == biglietto.AcquistoId &&
            t.Tipo == TipoTransazione.RIMBORSO &&
            t.Descrizione != null &&
            t.Descrizione.Contains(marker));

        if (alreadyRefunded)
        {
            return (false, "Biglietto gia rimborsato");
        }

        var acquisto = biglietto.Acquisto;
        var (alreadyCredited, alreadyCardRefunded) = await GetRefundedAmountsAsync(acquisto.Id, utenteId);
        var cartaTotale = Math.Max(0m, acquisto.ImportoTotale - acquisto.CreditoUsato);
        var creditoTotale = acquisto.CreditoUsato;
        var cartaResidua = Math.Max(0m, cartaTotale - alreadyCardRefunded);
        var creditoResiduo = Math.Max(0m, creditoTotale - alreadyCredited);

        var rimborsoCarta = Math.Min(biglietto.Prezzo, cartaResidua);
        var rimborsoCredito = Math.Min(biglietto.Prezzo - rimborsoCarta, creditoResiduo);

        if (rimborsoCarta > 0m)
        {
            if (string.IsNullOrWhiteSpace(acquisto.StripeChargeId))
            {
                return (false, "Impossibile rimborsare la quota carta: PaymentIntent mancante");
            }

            var stripeRefund = await pagamentoService.RimborsaPagamentoStripeAsync(
                acquisto.StripeChargeId,
                rimborsoCarta,
                $"Rimborso biglietto #{biglietto.Id} acquisto #{acquisto.Id}");

            if (!stripeRefund.Success)
            {
                return (false, stripeRefund.Message);
            }
        }

        if (rimborsoCredito > 0m)
        {
            await creditoService.RicaricaAsync(utenteId, new RicaricaCreditoDTO
            {
                UtenteId = utenteId,
                Importo = rimborsoCredito,
                Descrizione = $"Rimborso credito biglietto #{biglietto.Id} acquisto #{acquisto.Id} [rimborso_credito:{rimborsoCredito:F2}] {marker}",
                CinemaId = biglietto.CinemaId
            });
        }

        if (rimborsoCarta > 0m)
        {
            var credito = await context.CreditiUtente.FirstOrDefaultAsync(c => c.UtenteId == utenteId);
            if (credito is null)
            {
                credito = new CreditoUtente
                {
                    UtenteId = utenteId,
                    Saldo = 0m,
                    DataUltimoAggiornamento = DateTime.UtcNow
                };
                context.CreditiUtente.Add(credito);
                await context.SaveChangesAsync();
            }

            context.TransazioniCredito.Add(new TransazioneCredito
            {
                UtenteId = utenteId,
                Tipo = TipoTransazione.RIMBORSO,
                Importo = 0m,
                SaldoPrecedente = credito.Saldo,
                SaldoSuccessivo = credito.Saldo,
                DataTransazione = DateTime.UtcNow,
                OperatoreId = utenteId,
                CinemaId = biglietto.CinemaId,
                AcquistoId = biglietto.AcquistoId,
                Descrizione = $"Rimborso carta biglietto #{biglietto.Id} acquisto #{biglietto.AcquistoId} [rimborso_carta:{rimborsoCarta:F2}] {marker}"
            });
        }

        var rimborsoDescrizioni = await context.TransazioniCredito
            .Where(t => t.AcquistoId == biglietto.AcquistoId && t.Tipo == TipoTransazione.RIMBORSO && t.Descrizione != null)
            .Select(t => t.Descrizione!)
            .ToListAsync();
        rimborsoDescrizioni.Add($"new {marker}");

        var allRefunded = biglietto.Acquisto.Biglietti.All(b =>
            rimborsoDescrizioni.Any(d => d.Contains($"[rimborso_biglietto:{b.Id}]")));

        if (allRefunded)
        {
            biglietto.Acquisto.Stato = StatoAcquisto.REFUNDED;
        }

        await context.SaveChangesAsync();
        return (true, "Rimborso ticket completato: quota carta su Stripe e quota credito sul saldo sito");
    }

    public async Task<BigliettoDTO?> GetBigliettoAsync(int id)
    {
        var b = await context.Biglietti.FirstOrDefaultAsync(x => x.Id == id);
        return b is null ? null : ToDto(b);
    }

    public async Task<IEnumerable<BigliettoDTO>> GetBigliettiUtenteAsync(int utenteId)
    {
        var list = await context.Biglietti
            .Include(b => b.Acquisto)
            .Include(b => b.Show)
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
                QRCodeUrl = b.QRCodeUrl,
                StatoAcquisto = b.Acquisto.Stato.ToString()
            })
            .ToListAsync();

        var acquistoIds = list.Select(x => x.AcquistoId).Distinct().ToList();
        var txRimborsi = await context.TransazioniCredito
            .Where(t => t.UtenteId == utenteId &&
                        t.Tipo == TipoTransazione.RIMBORSO &&
                        t.AcquistoId != null &&
                        acquistoIds.Contains(t.AcquistoId.Value) &&
                        t.Descrizione != null)
            .Select(t => new { t.AcquistoId, t.Descrizione })
            .ToListAsync();

        var now = DateTime.Now;
        var ticketIds = list.Select(x => x.Id).ToList();
        var showTimes = await context.Biglietti
            .Where(b => ticketIds.Contains(b.Id))
            .Select(b => new
            {
                b.Id,
                Data = b.Show.Data,
                Ora = b.Show.OraInizio
            })
            .ToListAsync();
        var showMap = showTimes.ToDictionary(x => x.Id, x => x);

        foreach (var ticket in list)
        {
            var marker = $"[rimborso_biglietto:{ticket.Id}]";
            var refunded = txRimborsi.Any(t =>
                t.AcquistoId == ticket.AcquistoId &&
                t.Descrizione != null &&
                t.Descrizione.Contains(marker));

            ticket.Rimborsato = refunded;

            var canRefundByTime = showMap.TryGetValue(ticket.Id, out var st)
                && st.Data.ToDateTime(st.Ora) > now;

            ticket.RimborsoDisponibile = !ticket.Validato
                                         && !ticket.Rimborsato
                                         && string.Equals(ticket.StatoAcquisto, StatoAcquisto.PAGATO.ToString(), StringComparison.OrdinalIgnoreCase)
                                         && canRefundByTime;
        }

        return list;
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

    private static List<(int fila, List<int> posti)> BuildRows(int numeroFile, int? postiPerFila, string? configurazione)
    {
        if (!string.IsNullOrWhiteSpace(configurazione))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(configurazione);
                if (doc.RootElement.TryGetProperty("file", out var rows) && rows.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var result = new List<(int fila, List<int> posti)>();
                    foreach (var row in rows.EnumerateArray())
                    {
                        var fila = row.GetProperty("fila").GetInt32();
                        var postiProp = row.GetProperty("posti");
                        List<int> seats;
                        if (postiProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            seats = postiProp.EnumerateArray()
                                .Where(x => x.ValueKind == System.Text.Json.JsonValueKind.Number)
                                .Select(x => x.GetInt32())
                                .Where(x => x > 0)
                                .Distinct()
                                .OrderBy(x => x)
                                .ToList();
                        }
                        else
                        {
                            var count = postiProp.GetInt32();
                            seats = Enumerable.Range(1, Math.Max(0, count)).ToList();
                        }

                        if (seats.Count > 0) result.Add((fila, seats));
                    }
                    if (result.Count > 0) return result;
                }
            }
            catch
            {
            }
        }

        var per = postiPerFila ?? 10;
        return Enumerable.Range(1, numeroFile)
            .Select(i => (i, Enumerable.Range(1, per).ToList()))
            .ToList();
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

    private async Task<(decimal credito, decimal carta)> GetRefundedAmountsAsync(int acquistoId, int utenteId)
    {
        var descrizioni = await context.TransazioniCredito
            .Where(t => t.UtenteId == utenteId
                        && t.AcquistoId == acquistoId
                        && t.Tipo == TipoTransazione.RIMBORSO
                        && t.Descrizione != null)
            .Select(t => t.Descrizione!)
            .ToListAsync();

        decimal credito = 0m;
        decimal carta = 0m;
        foreach (var descrizione in descrizioni)
        {
            credito += ExtractTaggedAmount(descrizione, "[rimborso_credito:", "]");
            carta += ExtractTaggedAmount(descrizione, "[rimborso_carta:", "]");
        }

        return (credito, carta);
    }

    private static decimal ExtractTaggedAmount(string text, string prefix, string suffix)
    {
        var start = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return 0m;
        start += prefix.Length;

        var end = text.IndexOf(suffix, start, StringComparison.OrdinalIgnoreCase);
        if (end <= start) return 0m;

        var amountRaw = text[start..end].Trim();
        return decimal.TryParse(amountRaw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(0m, parsed)
            : 0m;
    }
}
