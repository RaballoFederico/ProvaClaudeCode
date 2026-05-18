// DOC: Interfaccia service 'IBigliettoService': contratto della logica applicativa usata dagli endpoint.
using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IBigliettoService
{
    Task<IEnumerable<PostoStatoDTO>> GetPiantinaStatoAsync(int showId);
    Task<PrenotazioneTempDTO> LockPostiAsync(int utenteId, int showId, List<PostoDTO> posti, string sessionId);
    Task<LockDettaglioDTO?> GetLockDettaglioAsync(string codiceTemporaneo, int utenteId);
    Task<bool> RinnovaLockAsync(int utenteId, string codiceTemporaneo);
    Task<bool> RilasciaLockAsync(int utenteId, string codiceTemporaneo);
    Task<AcquistoResultDTO> ConfermaAcquistoAsync(int utenteId, ConfermaAcquistoDTO dto);
    Task<(bool success, string message)> RichiediRimborsoAsync(int utenteId, int acquistoId);
    Task<(bool success, string message)> RichiediRimborsoBigliettoAsync(int utenteId, int bigliettoId);
    Task<BigliettoDTO?> GetBigliettoAsync(int id);
    Task<IEnumerable<BigliettoDTO>> GetBigliettiUtenteAsync(int utenteId);
    Task<BigliettoValidazioneDTO?> GetBigliettoPerValidazioneAsync(string codiceHash);
    Task<bool> ValidaBigliettoAsync(string codiceHash, int operatoreId, int cinemaId);
    string GeneraCodiceHash(int bigliettoId, int acquistoId, string posto);
    string GeneraQRCodeUrl(string codiceHash);
}

