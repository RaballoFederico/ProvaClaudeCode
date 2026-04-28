using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IPagamentoService
{
    Task<CalcoloImportoDTO> CalcolaImportoAsync(int utenteId, CalcoloImportoRequestDTO dto);
    Task<PagamentoResultDTO> ProcessaPagamentoAsync(int utenteId, PagamentoRequestDTO dto);
    Task<RimborsoResultDTO> RimborsaAcquistoAsync(int acquistoId);
    Task<StripeCheckoutSessionDTO> CreaCheckoutSessionAsync(decimal importo, int utenteId, string successUrl, string cancelUrl, string? productName = null, string integration = "filmapi_checkout", Dictionary<string, string>? extraMetadata = null);
    Task<StripeCheckoutVerificationDTO> VerificaCheckoutSessionAsync(string sessionId, decimal importoAtteso);
    Task<StripeWebhookResultDTO> GestisciWebhookAsync(string payload, string? stripeSignature, string? expectedWebhookSecret);
}
