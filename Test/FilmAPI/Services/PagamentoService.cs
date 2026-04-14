using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;

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

        if (calc.DaPagareCarta > 0m && string.IsNullOrWhiteSpace(dto.PaymentIntentId))
        {
            throw new InvalidOperationException("PaymentIntentId richiesto per pagamento carta");
        }

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

    public async Task<StripePaymentIntentDTO> CreaPaymentIntentAsync(decimal importo, int utenteId)
    {
        if (importo <= 0)
        {
            throw new InvalidOperationException("Importo non valido per PaymentIntent");
        }

        if (!StripeConfigurato)
        {
            var fakeId = $"pi_mock_{Guid.NewGuid():N}";
            return new StripePaymentIntentDTO
            {
                ClientSecret = $"{fakeId}_secret_mock",
                PaymentIntentId = fakeId
            };
        }

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(importo * 100m, 0, MidpointRounding.AwayFromZero),
            Currency = "eur",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                { "integration", "filmapi" },
                { "utenteId", utenteId.ToString() }
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);

        return new StripePaymentIntentDTO
        {
            ClientSecret = intent.ClientSecret,
            PaymentIntentId = intent.Id
        };
    }

    public async Task<bool> VerificaPaymentIntentAsync(string paymentIntentId, decimal importoAtteso)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId)) return false;

        if (!StripeConfigurato)
        {
            return paymentIntentId.StartsWith("pi_mock_", StringComparison.OrdinalIgnoreCase);
        }

        var service = new PaymentIntentService();
        var intent = await service.GetAsync(paymentIntentId);
        var importoCents = (long)Math.Round(importoAtteso * 100m, 0, MidpointRounding.AwayFromZero);

        return intent is not null &&
               intent.Status == "succeeded" &&
               intent.AmountReceived >= importoCents;
    }
}
