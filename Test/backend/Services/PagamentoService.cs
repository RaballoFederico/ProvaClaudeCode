using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace FilmAPI.Services;

public class PagamentoService(FilmDbContext context, ICreditoService creditoService) : IPagamentoService
{
    private static bool StripeConfigurato => !string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey);

    public async Task<CalcoloImportoDTO> CalcolaImportoAsync(int utenteId, CalcoloImportoRequestDTO dto)
    {
        var show = await context.Shows.FindAsync(dto.ShowId) ?? throw new InvalidOperationException("Show non trovato");
        var subtotale = show.PrezzoBase * dto.NumeroBiglietti;
        var saldo = await creditoService.GetSaldoAsync(utenteId);
        var creditoUsato = dto.UsaCredito ? Math.Min(saldo, subtotale) : 0m;

        return new CalcoloImportoDTO
        {
            PrezzoUnitario = show.PrezzoBase,
            Subtotale = subtotale,
            CreditoDisponibile = saldo,
            CreditoUsato = creditoUsato,
            DaPagareCarta = Math.Max(0m, subtotale - creditoUsato)
        };
    }

    public async Task<PagamentoResultDTO> ProcessaPagamentoAsync(int utenteId, PagamentoRequestDTO dto)
    {
        var calc = await CalcolaImportoAsync(utenteId, new CalcoloImportoRequestDTO
        {
            ShowId = dto.ShowId,
            NumeroBiglietti = dto.NumeroBiglietti,
            UsaCredito = dto.UsaCredito
        });

        return new PagamentoResultDTO
        {
            Success = true,
            Message = "Pagamento processato",
            ImportoTotale = calc.Subtotale,
            CreditoUsato = calc.CreditoUsato,
            CartaAddebitata = calc.DaPagareCarta
        };
    }

    public async Task<RimborsoResultDTO> RimborsaAcquistoAsync(int acquistoId)
    {
        var acquisto = await context.Acquisti.FirstOrDefaultAsync(a => a.Id == acquistoId);
        if (acquisto is null)
        {
            return new RimborsoResultDTO { Success = false, Message = "Acquisto non trovato" };
        }

        acquisto.Stato = Model.StatoAcquisto.REFUNDED;
        await context.SaveChangesAsync();
        return new RimborsoResultDTO { Success = true, Message = "Rimborso registrato" };
    }

    public async Task<RimborsoResultDTO> RimborsaPagamentoStripeAsync(string paymentIntentId, decimal importo, string? motivo = null)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            return new RimborsoResultDTO { Success = false, Message = "PaymentIntent non valido" };
        }

        if (importo <= 0m)
        {
            return new RimborsoResultDTO { Success = false, Message = "Importo rimborso non valido" };
        }

        if (!StripeConfigurato)
        {
            if (paymentIntentId.StartsWith("pi_mock_", StringComparison.OrdinalIgnoreCase))
            {
                return new RimborsoResultDTO { Success = true, Message = "Rimborso Stripe simulato completato" };
            }

            return new RimborsoResultDTO { Success = false, Message = "Stripe non configurato" };
        }

        try
        {
            var refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)Math.Round(importo * 100m, 0, MidpointRounding.AwayFromZero),
                Reason = "requested_by_customer",
                Metadata = new Dictionary<string, string>
                {
                    { "source", "filmapi_refund" },
                    { "motivo", string.IsNullOrWhiteSpace(motivo) ? "rimborso utente" : motivo.Trim() }
                }
            };

            await refundService.CreateAsync(options);
            return new RimborsoResultDTO { Success = true, Message = "Rimborso Stripe completato" };
        }
        catch (Exception ex)
        {
            return new RimborsoResultDTO { Success = false, Message = $"Errore rimborso Stripe: {ex.Message}" };
        }
    }

    public async Task<StripeCheckoutSessionDTO> CreaCheckoutSessionAsync(decimal importo, int utenteId, string successUrl, string cancelUrl, string? productName = null, string integration = "filmapi_checkout", Dictionary<string, string>? extraMetadata = null, string? preferredPaymentMethodType = null)
    {
        if (importo <= 0)
        {
            throw new InvalidOperationException("Importo non valido per Checkout");
        }

        if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
        {
            throw new InvalidOperationException("URL di redirect non validi");
        }

        if (!StripeConfigurato)
        {
            var fakeSession = $"cs_mock_{Guid.NewGuid():N}";
            return new StripeCheckoutSessionDTO
            {
                SessionId = fakeSession,
                Url = successUrl.Replace("{CHECKOUT_SESSION_ID}", fakeSession, StringComparison.Ordinal)
            };
        }

        var normalizedProductName = string.IsNullOrWhiteSpace(productName)
            ? "Acquisto biglietti FilmAPI"
            : productName.Trim();

        var metadata = new Dictionary<string, string>
        {
            { "integration", string.IsNullOrWhiteSpace(integration) ? "filmapi_checkout" : integration.Trim() },
            { "utenteId", utenteId.ToString() }
        };

        var normalizedMethod = (preferredPaymentMethodType ?? "card").Trim().ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "card",
            "revolut_pay"
        };
        if (!allowed.Contains(normalizedMethod))
        {
            normalizedMethod = "card";
        }
        metadata["payment_method_type"] = normalizedMethod;

        if (extraMetadata is not null)
        {
            foreach (var item in extraMetadata)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                metadata[item.Key.Trim()] = item.Value;
            }
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            PaymentMethodTypes = new List<string> { normalizedMethod },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = (long)Math.Round(importo * 100m, 0, MidpointRounding.AwayFromZero),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = normalizedProductName
                        }
                    }
                }
            },
            Metadata = metadata
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return new StripeCheckoutSessionDTO
        {
            SessionId = session.Id,
            Url = session.Url ?? string.Empty
        };
    }

    public async Task<StripeCheckoutVerificationDTO> VerificaCheckoutSessionAsync(string sessionId, decimal importoAtteso)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new StripeCheckoutVerificationDTO { Success = false };
        }

        if (!StripeConfigurato)
        {
            return new StripeCheckoutVerificationDTO
            {
                Success = sessionId.StartsWith("cs_mock_", StringComparison.OrdinalIgnoreCase),
                PaymentIntentId = sessionId.StartsWith("cs_mock_", StringComparison.OrdinalIgnoreCase)
                    ? $"pi_mock_{Guid.NewGuid():N}"
                    : string.Empty
            };
        }

        var service = new SessionService();
        var session = await service.GetAsync(sessionId);
        var importoCents = (long)Math.Round(importoAtteso * 100m, 0, MidpointRounding.AwayFromZero);

        var paid = session is not null
                   && string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
                   && (session.AmountTotal ?? 0) >= importoCents;

        return new StripeCheckoutVerificationDTO
        {
            Success = paid,
            PaymentIntentId = paid ? (session?.PaymentIntentId ?? string.Empty) : string.Empty
        };
    }

    public async Task<StripeWebhookResultDTO> GestisciWebhookAsync(string payload, string? stripeSignature, string? expectedWebhookSecret)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new StripeWebhookResultDTO
            {
                Success = false,
                Message = "Payload webhook vuoto"
            };
        }

        Stripe.Event stripeEvent;
        try
        {
            if (!string.IsNullOrWhiteSpace(expectedWebhookSecret))
            {
                if (string.IsNullOrWhiteSpace(stripeSignature))
                {
                    return new StripeWebhookResultDTO
                    {
                        Success = false,
                        Message = "Header Stripe-Signature mancante"
                    };
                }

                stripeEvent = EventUtility.ConstructEvent(payload, stripeSignature, expectedWebhookSecret);
            }
            else
            {
                stripeEvent = EventUtility.ParseEvent(payload);
            }
        }
        catch (Exception ex)
        {
            return new StripeWebhookResultDTO
            {
                Success = false,
                Message = $"Webhook Stripe non valido: {ex.Message}"
            };
        }

        if (!string.Equals(stripeEvent.Type, "checkout.session.completed", StringComparison.OrdinalIgnoreCase))
        {
            return new StripeWebhookResultDTO
            {
                Success = true,
                Message = $"Evento ignorato: {stripeEvent.Type}",
                EventType = stripeEvent.Type,
                EventId = stripeEvent.Id
            };
        }

        var checkoutSession = stripeEvent.Data.Object as Session;
        if (checkoutSession is null || string.IsNullOrWhiteSpace(checkoutSession.Id))
        {
            return new StripeWebhookResultDTO
            {
                Success = false,
                Message = "Evento checkout.session.completed senza Session valida",
                EventType = stripeEvent.Type,
                EventId = stripeEvent.Id
            };
        }

        var metadata = checkoutSession.Metadata ?? new Dictionary<string, string>();

        var integration = metadata.TryGetValue("integration", out var integrationValue)
            ? integrationValue
            : "";

        if (!string.Equals(integration, "filmapi_credit_topup", StringComparison.OrdinalIgnoreCase))
        {
            return new StripeWebhookResultDTO
            {
                Success = true,
                Message = "Sessione checkout non destinata alla ricarica credito",
                EventType = stripeEvent.Type,
                EventId = stripeEvent.Id,
                CheckoutSessionId = checkoutSession.Id
            };
        }

        var userIdRaw = metadata.TryGetValue("utenteId", out var userIdString) ? userIdString : null;
        if (string.IsNullOrWhiteSpace(userIdRaw) || !int.TryParse(userIdRaw, out var userId))
        {
            return new StripeWebhookResultDTO
            {
                Success = false,
                Message = "Metadata utenteId mancante o non valida",
                EventType = stripeEvent.Type,
                EventId = stripeEvent.Id,
                CheckoutSessionId = checkoutSession.Id
            };
        }

        var amountTotal = checkoutSession.AmountTotal ?? 0;
        if (amountTotal <= 0)
        {
            return new StripeWebhookResultDTO
            {
                Success = false,
                Message = "Importo checkout non valido",
                EventType = stripeEvent.Type,
                EventId = stripeEvent.Id,
                CheckoutSessionId = checkoutSession.Id
            };
        }

        var importo = decimal.Round(amountTotal / 100m, 2, MidpointRounding.AwayFromZero);
        var sessionTag = $"[stripe_session:{checkoutSession.Id}]";

        var alreadyProcessed = await context.TransazioniCredito.AnyAsync(t =>
            t.UtenteId == userId
            && t.Tipo == Model.TipoTransazione.RICARICA
            && t.Descrizione != null
            && t.Descrizione.Contains(sessionTag));

        if (alreadyProcessed)
        {
            return new StripeWebhookResultDTO
            {
                Success = true,
                Message = "Webhook gia elaborato in precedenza",
                EventType = stripeEvent.Type,
                EventId = stripeEvent.Id,
                CheckoutSessionId = checkoutSession.Id,
                Amount = importo,
                UserId = userId,
                AlreadyProcessed = true
            };
        }

        await creditoService.RicaricaAsync(userId, new RicaricaCreditoDTO
        {
            UtenteId = userId,
            Importo = importo,
            Descrizione = $"Ricarica credito da webhook Stripe {sessionTag}",
            CinemaId = null
        });

        return new StripeWebhookResultDTO
        {
            Success = true,
            Message = "Webhook elaborato correttamente",
            EventType = stripeEvent.Type,
            EventId = stripeEvent.Id,
            CheckoutSessionId = checkoutSession.Id,
            Amount = importo,
            UserId = userId,
            AlreadyProcessed = false
        };
    }
}
