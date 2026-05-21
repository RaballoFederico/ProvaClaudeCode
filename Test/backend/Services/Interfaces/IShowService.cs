// DOC: IShowService - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Interfaccia service 'IShowService': contratto della logica applicativa usata dagli endpoint.
using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IShowService
{
    Task<IEnumerable<ShowDTO>> GetShowsAsync(ShowFilterDTO? filter = null);
    Task<ShowDTO?> GetShowAsync(int id);
    Task<ShowDTO> CreateShowAsync(ShowCreateDTO dto);
    Task<ShowDTO?> UpdateShowAsync(int id, ShowUpdateDTO dto);
    Task<bool> DeleteShowAsync(int id);
    Task<bool> ValidateOrarioAsync(int salaId, DateOnly data, TimeOnly oraInizio, int durataFilm, int? excludeShowId = null);
    Task<IEnumerable<ShowDTO>> GetShowsByFilmAsync(int filmId, int? cinemaId = null, DateOnly? data = null);
    Task<IEnumerable<ShowDTO>> GetShowsByCinemaAsync(int cinemaId, DateOnly data);
    Task<int> GetPostiDisponibiliAsync(int showId);
    Task<DisponibilitaPostiDTO?> GetDisponibilitaPostiAsync(int showId);
}


