// DOC: Service 'CategoriaService': implementa logica di business e integrazioni esterne (DB/TMDB/Stripe).
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class CategoriaService : ICategoriaService
{
    private readonly FilmDbContext _db;

    public CategoriaService(FilmDbContext db)
    {
        _db = db;
    }

    // DOC-METHOD: 'GetAllAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<List<CategoriaDTO>> GetAllAsync()
    {
        return await _db.Categorie
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Descrizione = c.Descrizione
            })
            .ToListAsync();
    }

    // DOC-METHOD: 'GetByIdAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<CategoriaDTO?> GetByIdAsync(int id)
    {
        return await _db.Categorie
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoriaDTO
            {
                Id = c.Id,
                Nome = c.Nome,
                Descrizione = c.Descrizione
            })
            .FirstOrDefaultAsync();
    }

    // DOC-METHOD: 'GetFilmsByCategoriaAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<List<FilmDTO>> GetFilmsByCategoriaAsync(int categoriaId)
    {
        return await _db.FilmsCategorie
            .AsNoTracking()
            .Where(fc => fc.CategoriaId == categoriaId)
            .Include(fc => fc.Film)
            .ThenInclude(f => f.FilmsCategorie)
            .ThenInclude(link => link.Categoria)
            .Select(fc => new FilmDTO
            {
                Id = fc.Film.Id,
                Titolo = fc.Film.Titolo,
                DataProduzione = fc.Film.DataProduzione,
                RegistaId = fc.Film.RegistaId,
                Durata = fc.Film.Durata,
                CopertinaPath = fc.Film.CopertinaPath,
                FilmatoPath = fc.Film.FilmatoPath,
                CategorieIds = fc.Film.FilmsCategorie.Select(link => link.CategoriaId).ToList(),
                Categorie = fc.Film.FilmsCategorie
                    .Select(link => new CategoriaDTO
                    {
                        Id = link.Categoria.Id,
                        Nome = link.Categoria.Nome,
                        Descrizione = link.Categoria.Descrizione
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<(CategoriaDTO? categoria, string? error)> CreateAsync(CategoriaCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            return (null, "Il nome della categoria e' obbligatorio");
        }

        var nome = dto.Nome.Trim();
        var alreadyExists = await _db.Categorie.AnyAsync(c => c.Nome.ToLower() == nome.ToLower());
        if (alreadyExists)
        {
            return (null, "Categoria con questo nome gia' esistente");
        }

        var categoria = new Categoria
        {
            Nome = nome,
            Descrizione = dto.Descrizione
        };

        _db.Categorie.Add(categoria);
        await _db.SaveChangesAsync();

        return (new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Descrizione = categoria.Descrizione
        }, null);
    }

    public async Task<(CategoriaDTO? categoria, string? error)> UpdateAsync(int id, CategoriaCreateDTO dto)
    {
        var categoria = await _db.Categorie.FindAsync(id);
        if (categoria == null)
        {
            return (null, "NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            return (null, "Il nome della categoria e' obbligatorio");
        }

        var nome = dto.Nome.Trim();
        var alreadyExists = await _db.Categorie.AnyAsync(c => c.Id != id && c.Nome.ToLower() == nome.ToLower());
        if (alreadyExists)
        {
            return (null, "Categoria con questo nome gia' esistente");
        }

        categoria.Nome = nome;
        categoria.Descrizione = dto.Descrizione;

        await _db.SaveChangesAsync();

        return (new CategoriaDTO
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Descrizione = categoria.Descrizione
        }, null);
    }

    // DOC-METHOD: 'DeleteAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<bool> DeleteAsync(int id)
    {
        var categoria = await _db.Categorie.FindAsync(id);
        if (categoria == null)
        {
            return false;
        }

        _db.Categorie.Remove(categoria);
        await _db.SaveChangesAsync();
        return true;
    }
}

