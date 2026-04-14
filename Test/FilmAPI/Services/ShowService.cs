using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class ShowService(FilmDbContext context) : IShowService
{
    private static readonly Dictionary<TipologiaSala, decimal> PrezziBase = new()
    {
        [TipologiaSala.ISENSE] = 12.00m,
        [TipologiaSala.XL] = 10.00m,
        [TipologiaSala.TRE_D] = 9.50m,
        [TipologiaSala.DUE_D] = 8.00m
    };

    public async Task<IEnumerable<ShowDTO>> GetShowsAsync(ShowFilterDTO? filter = null)
    {
        var query = context.Shows
            .Include(s => s.Film)
            .Include(s => s.Sala)
            .AsQueryable();

        if (filter is not null)
        {
            if (filter.SalaId.HasValue) query = query.Where(s => s.SalaId == filter.SalaId.Value);
            if (filter.CinemaId.HasValue) query = query.Where(s => s.Sala != null && s.Sala.CinemaId == filter.CinemaId.Value);
            if (filter.FilmId.HasValue) query = query.Where(s => s.FilmId == filter.FilmId.Value);
            if (filter.Data.HasValue) query = query.Where(s => s.Data == filter.Data.Value);
            if (filter.Stato.HasValue) query = query.Where(s => (int)s.Stato == filter.Stato.Value);
        }

        return await query.OrderBy(s => s.Data).ThenBy(s => s.OraInizio).Select(s => new ShowDTO
        {
            Id = s.Id,
            SalaId = s.SalaId,
            CinemaId = s.Sala != null ? s.Sala.CinemaId : 0,
            FilmId = s.FilmId,
            FilmTitolo = s.Film != null ? s.Film.Titolo : string.Empty,
            SalaNome = s.Sala != null ? (s.Sala.Nome ?? ("Sala " + s.SalaId)) : ("Sala " + s.SalaId),
            TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
            Data = s.Data,
            OraInizio = s.OraInizio,
            OraFine = s.OraFine,
            PrezzoBase = s.PrezzoBase,
            Stato = s.Stato.ToString()
        }).ToListAsync();
    }

    public async Task<ShowDTO?> GetShowAsync(int id)
    {
        return await context.Shows.Include(s => s.Film).Include(s => s.Sala).Where(s => s.Id == id).Select(s => new ShowDTO
        {
            Id = s.Id,
            SalaId = s.SalaId,
            CinemaId = s.Sala != null ? s.Sala.CinemaId : 0,
            FilmId = s.FilmId,
            FilmTitolo = s.Film != null ? s.Film.Titolo : string.Empty,
            SalaNome = s.Sala != null ? (s.Sala.Nome ?? ("Sala " + s.SalaId)) : ("Sala " + s.SalaId),
            TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
            Data = s.Data,
            OraInizio = s.OraInizio,
            OraFine = s.OraFine,
            PrezzoBase = s.PrezzoBase,
            Stato = s.Stato.ToString()
        }).FirstOrDefaultAsync();
    }

    public async Task<ShowDTO> CreateShowAsync(ShowCreateDTO dto)
    {
        var sala = await context.Sale.FindAsync(dto.SalaId) ?? throw new InvalidOperationException("Sala non trovata");
        var film = await context.Films.FindAsync(dto.FilmId) ?? throw new InvalidOperationException("Film non trovato");

        var valido = await ValidateOrarioAsync(dto.SalaId, dto.Data, dto.OraInizio, film.Durata);
        if (!valido) throw new InvalidOperationException("Orario show sovrapposto");

        var show = new Show
        {
            SalaId = dto.SalaId,
            FilmId = dto.FilmId,
            Data = dto.Data,
            OraInizio = dto.OraInizio,
            OraFine = dto.OraInizio.AddMinutes(film.Durata),
            PrezzoBase = dto.PrezzoBase ?? PrezziBase.GetValueOrDefault(sala.Tipologia, 8m),
            Stato = StatoShow.PROGRAMMATO
        };

        context.Shows.Add(show);
        await context.SaveChangesAsync();
        await context.Entry(show).Reference(s => s.Film).LoadAsync();
        await context.Entry(show).Reference(s => s.Sala).LoadAsync();
        return ToDtoCompiled(show);
    }

    public async Task<ShowDTO?> UpdateShowAsync(int id, ShowUpdateDTO dto)
    {
        var show = await context.Shows.FindAsync(id);
        if (show is null) return null;

        var film = await context.Films.FindAsync(dto.FilmId) ?? throw new InvalidOperationException("Film non trovato");
        var valido = await ValidateOrarioAsync(dto.SalaId, dto.Data, dto.OraInizio, film.Durata, id);
        if (!valido) throw new InvalidOperationException("Orario show sovrapposto");

        show.SalaId = dto.SalaId;
        show.FilmId = dto.FilmId;
        show.Data = dto.Data;
        show.OraInizio = dto.OraInizio;
        show.OraFine = dto.OraInizio.AddMinutes(film.Durata);
        show.PrezzoBase = dto.PrezzoBase;
        show.Stato = (StatoShow)dto.Stato;

        await context.SaveChangesAsync();
        await context.Entry(show).Reference(s => s.Film).LoadAsync();
        await context.Entry(show).Reference(s => s.Sala).LoadAsync();
        return ToDtoCompiled(show);
    }

    public async Task<bool> DeleteShowAsync(int id)
    {
        var show = await context.Shows.FindAsync(id);
        if (show is null) return false;
        context.Shows.Remove(show);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ValidateOrarioAsync(int salaId, DateOnly data, TimeOnly oraInizio, int durataFilm, int? excludeShowId = null)
    {
        var oraFine = oraInizio.AddMinutes(durataFilm);

        var showPrecedente = await context.Shows
            .Where(s => s.SalaId == salaId && s.Data == data && s.OraInizio < oraInizio)
            .Where(s => excludeShowId == null || s.Id != excludeShowId)
            .OrderByDescending(s => s.OraInizio)
            .FirstOrDefaultAsync();

        if (showPrecedente != null && oraInizio < showPrecedente.OraFine)
            return false;

        var showSuccessivo = await context.Shows
            .Where(s => s.SalaId == salaId && s.Data == data && s.OraInizio > oraInizio)
            .Where(s => excludeShowId == null || s.Id != excludeShowId)
            .OrderBy(s => s.OraInizio)
            .FirstOrDefaultAsync();

        if (showSuccessivo != null && oraFine > showSuccessivo.OraInizio)
            return false;

        return true;
    }

    public async Task<IEnumerable<ShowDTO>> GetShowsByFilmAsync(int filmId, int? cinemaId = null, DateOnly? data = null)
    {
        var query = context.Shows.Include(s => s.Film).Include(s => s.Sala).Where(s => s.FilmId == filmId);
        if (cinemaId.HasValue) query = query.Where(s => s.Sala != null && s.Sala.CinemaId == cinemaId.Value);
        if (data.HasValue) query = query.Where(s => s.Data == data.Value);

        return await query.OrderBy(s => s.Data).ThenBy(s => s.OraInizio).Select(s => new ShowDTO
        {
            Id = s.Id,
            SalaId = s.SalaId,
            CinemaId = s.Sala != null ? s.Sala.CinemaId : 0,
            FilmId = s.FilmId,
            FilmTitolo = s.Film != null ? s.Film.Titolo : string.Empty,
            SalaNome = s.Sala != null ? (s.Sala.Nome ?? ("Sala " + s.SalaId)) : ("Sala " + s.SalaId),
            TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
            Data = s.Data,
            OraInizio = s.OraInizio,
            OraFine = s.OraFine,
            PrezzoBase = s.PrezzoBase,
            Stato = s.Stato.ToString()
        }).ToListAsync();
    }

    public async Task<IEnumerable<ShowDTO>> GetShowsByCinemaAsync(int cinemaId, DateOnly data)
    {
        return await context.Shows.Include(s => s.Film).Include(s => s.Sala)
            .Where(s => s.Sala != null && s.Sala.CinemaId == cinemaId && s.Data == data)
            .OrderBy(s => s.OraInizio)
            .Select(s => new ShowDTO
            {
                Id = s.Id,
                SalaId = s.SalaId,
                CinemaId = s.Sala != null ? s.Sala.CinemaId : 0,
                FilmId = s.FilmId,
                FilmTitolo = s.Film != null ? s.Film.Titolo : string.Empty,
                SalaNome = s.Sala != null ? (s.Sala.Nome ?? ("Sala " + s.SalaId)) : ("Sala " + s.SalaId),
                TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
                Data = s.Data,
                OraInizio = s.OraInizio,
                OraFine = s.OraFine,
                PrezzoBase = s.PrezzoBase,
                Stato = s.Stato.ToString()
            })
            .ToListAsync();
    }

    public async Task<int> GetPostiDisponibiliAsync(int showId)
    {
        var disp = await GetDisponibilitaPostiAsync(showId);
        return disp?.Disponibili ?? 0;
    }

    public async Task<DisponibilitaPostiDTO?> GetDisponibilitaPostiAsync(int showId)
    {
        var show = await context.Shows.Include(s => s.Sala).FirstOrDefaultAsync(s => s.Id == showId);
        if (show?.Sala is null) return null;

        var occupati = await context.Biglietti.CountAsync(b => b.ShowId == showId);
        var prenotati = await context.PrenotazioniTemporanee.CountAsync(p => p.ShowId == showId && p.Stato == StatoPrenotazioneTemp.ATTIVA && p.DataScadenza > DateTime.UtcNow);
        var tot = show.Sala.PostiTotali;

        return new DisponibilitaPostiDTO
        {
            ShowId = showId,
            Totali = tot,
            Occupati = occupati,
            PrenotatiTemporanei = prenotati,
            Disponibili = Math.Max(0, tot - occupati - prenotati)
        };
    }

    private static readonly Func<Show, ShowDTO> ToDto = s => new ShowDTO
    {
        Id = s.Id,
        SalaId = s.SalaId,
        CinemaId = s.Sala != null ? s.Sala.CinemaId : 0,
        FilmId = s.FilmId,
        FilmTitolo = s.Film != null ? s.Film.Titolo : string.Empty,
        SalaNome = s.Sala?.Nome ?? $"Sala {s.SalaId}",
        TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
        Data = s.Data,
        OraInizio = s.OraInizio,
        OraFine = s.OraFine,
        PrezzoBase = s.PrezzoBase,
        Stato = s.Stato.ToString()
    };

    private static ShowDTO ToDtoCompiled(Show s) => ToDto(s);
}
