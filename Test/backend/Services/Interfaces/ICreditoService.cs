// DOC: Interfaccia service 'ICreditoService': contratto della logica applicativa usata dagli endpoint.
using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface ICreditoService
{
    Task<decimal> GetSaldoAsync(int utenteId);
    Task<TransazioneCreditoDTO> RicaricaAsync(int operatoreId, RicaricaCreditoDTO dto);
    Task<IEnumerable<TransazioneCreditoDTO>> GetStoricoAsync(int utenteId);
    Task<IEnumerable<TransazioneCreditoDTO>> GetAllTransazioniAsync(TransazioneFilterDTO? filter = null);
    Task<bool> ScalaCreditoAsync(int utenteId, decimal importo, int acquistoId);
}

