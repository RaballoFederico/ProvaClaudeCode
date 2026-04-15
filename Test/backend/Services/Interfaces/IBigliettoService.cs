using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IBigliettoService
{
    Task<IEnumerable<PostoStatoDTO>> GetPiantinaStatoAsync(int showId);
    Task<PrenotazioneTempDTO> LockPostiAsync(int utenteId, int showId, List<PostoDTO> posti, string sessionId);
    Task<LockDettaglioDTO?> GetLockDettaglioAsync(string codiceTemporaneo, int utenteId);
    Task<bool> RinnovaLockAsync(string codiceTemporaneo);
    Task<bool> RilasciaLockAsync(string codiceTemporaneo);
    Task<AcquistoResultDTO> ConfermaAcquistoAsync(int utenteId, ConfermaAcquistoDTO dto);
    Task<BigliettoDTO?> GetBigliettoAsync(int id);
    Task<IEnumerable<BigliettoDTO>> GetBigliettiUtenteAsync(int utenteId);
    Task<BigliettoValidazioneDTO?> GetBigliettoPerValidazioneAsync(string codiceHash);
    Task<bool> ValidaBigliettoAsync(string codiceHash, int operatoreId, int cinemaId);
    string GeneraCodiceHash(int bigliettoId, int acquistoId, string posto);
    string GeneraQRCodeUrl(string codiceHash);
}
