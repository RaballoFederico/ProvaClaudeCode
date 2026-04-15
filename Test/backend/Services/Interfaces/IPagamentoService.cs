using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IPagamentoService
{
    Task<CalcoloImportoDTO> CalcolaImportoAsync(int utenteId, CalcoloImportoRequestDTO dto);
    Task<PagamentoResultDTO> ProcessaPagamentoAsync(int utenteId, PagamentoRequestDTO dto);
    Task<RimborsoResultDTO> RimborsaAcquistoAsync(int acquistoId);
    Task<StripePaymentIntentDTO> CreaPaymentIntentAsync(decimal importo, int utenteId);
    Task<bool> VerificaPaymentIntentAsync(string paymentIntentId, decimal importoAtteso);
}
