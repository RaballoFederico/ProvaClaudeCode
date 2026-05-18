// DOC: Interfaccia service 'ISalaService': contratto della logica applicativa usata dagli endpoint.
using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface ISalaService
{
    Task<IEnumerable<SalaDTO>> GetSaleByCinemaAsync(int cinemaId);
    Task<SalaDTO?> GetSalaAsync(int id);
    Task<SalaDTO> CreateSalaAsync(int cinemaId, SalaCreateDTO dto);
    Task<SalaDTO?> UpdateSalaAsync(int id, SalaUpdateDTO dto);
    Task<bool> DeleteSalaAsync(int id);
    Task<PiantinaDTO?> GetPiantinaAsync(int salaId);
    Task<bool> UpdatePiantinaAsync(int salaId, PiantinaUpdateDTO dto);
    Task<bool> ValidateConfigurazioneAsync(string configurazioneJson);
}

