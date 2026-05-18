// DOC: Interfaccia service 'ICategoriaService': contratto della logica applicativa usata dagli endpoint.
using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface ICategoriaService
{
    Task<List<CategoriaDTO>> GetAllAsync();
    Task<CategoriaDTO?> GetByIdAsync(int id);
    Task<List<FilmDTO>> GetFilmsByCategoriaAsync(int categoriaId);
    Task<(CategoriaDTO? categoria, string? error)> CreateAsync(CategoriaCreateDTO dto);
    Task<(CategoriaDTO? categoria, string? error)> UpdateAsync(int id, CategoriaCreateDTO dto);
    Task<bool> DeleteAsync(int id);
}

