using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IPagamentoService
{
    Task<CalcoloImportoDTO> CalcolaImportoAsync(int utenteId, CalcoloImportoRequestDTO dto);
    Task<PagamentoResultDTO> ProcessaPagamentoAsync(int utenteId, PagamentoRequestDTO dto);
    Task<RimborsoResultDTO> RimborsaAcquistoAsync(int acquistoId);
    Task<StripeCheckoutSessionDTO> CreaCheckoutSessionAsync(decimal importo, int utenteId, string successUrl, string cancelUrl);
    Task<StripeCheckoutVerificationDTO> VerificaCheckoutSessionAsync(string sessionId, decimal importoAtteso);
}
