using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class ProgrammazioneEndpoints
{
    public static IEndpointRouteBuilder MapProgrammazioneEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/programmazione", async (int? cinemaId, string? search, string? genere, FilmDbContext db) =>
        {
            var query = db.Films.Include(f => f.Shows).ThenInclude(s => s.Sala).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(f => f.Titolo.Contains(search));
            if (!string.IsNullOrWhiteSpace(genere)) query = query.Where(f => f.Genere != null && f.Genere == genere);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var next7 = today.AddDays(7);
            query = query.Where(f => f.Shows.Any(s => s.Data >= today));
            if (cinemaId.HasValue) query = query.Where(f => f.Shows.Any(s => s.Sala != null && s.Sala.CinemaId == cinemaId.Value));

            var items = await query
                .Select(f => new
                {
                    f.Id,
                    f.Titolo,
                    f.Durata,
                    f.CopertinaPath,
                    f.Genere,
                    f.Featured,
                    f.DataRilascio,
                    ShowsNext7Days = f.Shows.Count(s => s.Data >= today && s.Data <= next7),
                    FeaturedTag = f.Featured || f.Shows.Count(s => s.Data >= today && s.Data <= next7) >= 5,
                    InSelectedCinema = cinemaId.HasValue && f.Shows.Any(s => s.Sala != null && s.Sala.CinemaId == cinemaId.Value)
                })
                .OrderByDescending(f => f.FeaturedTag)
                .ThenByDescending(f => f.ShowsNext7Days)
                .ThenBy(f => f.Titolo)
                .ToListAsync();

            return Results.Ok(items);
        });

        app.MapGet("/programmazione/featured", async (FilmDbContext db) =>
        {
            var limitDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
            var items = await db.Films
                .Include(f => f.Shows)
                .Where(f => f.Featured || f.Shows.Count(s => s.Data <= limitDate) >= 5)
                .Select(f => new { f.Id, f.Titolo, f.CopertinaPath, f.Durata, f.Genere })
                .ToListAsync();
            return Results.Ok(items);
        });

        app.MapGet("/programmazione/coming-soon", async (FilmDbContext db) =>
        {
            var now = DateTime.UtcNow.Date;
            var max = now.AddDays(14);
            var items = await db.Films
                .Where(f => f.DataRilascio.HasValue && f.DataRilascio.Value.Date >= now && f.DataRilascio.Value.Date <= max)
                .Select(f => new { f.Id, f.Titolo, f.CopertinaPath, f.DataRilascio, f.Genere })
                .ToListAsync();
            return Results.Ok(items);
        });

        app.MapGet("/film/{id:int}/shows", async (int id, int? cinemaId, DateOnly? data, FilmDbContext db) =>
        {
            var query = db.Shows.Include(s => s.Sala).ThenInclude(s => s!.Cinema).Where(s => s.FilmId == id);
            if (cinemaId.HasValue) query = query.Where(s => s.Sala != null && s.Sala.CinemaId == cinemaId.Value);
            if (data.HasValue) query = query.Where(s => s.Data == data.Value);

            var items = await query.OrderBy(s => s.OraInizio).Select(s => new
            {
                s.Id,
                s.FilmId,
                s.SalaId,
                CinemaId = s.Sala != null ? s.Sala.CinemaId : 0,
                CinemaNome = s.Sala != null && s.Sala.Cinema != null ? s.Sala.Cinema.Nome : string.Empty,
                TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
                s.Data,
                s.OraInizio,
                s.OraFine,
                s.PrezzoBase
            }).ToListAsync();

            return Results.Ok(items);
        });

        app.MapGet("/shows/{id:int}", async (int id, FilmDbContext db) =>
        {
            var show = await db.Shows
                .Include(s => s.Sala)
                .ThenInclude(s => s!.Cinema)
                .Include(s => s.Film)
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.SalaId,
                    CinemaId = s.Sala != null ? s.Sala.CinemaId : 0,
                    s.FilmId,
                    FilmTitolo = s.Film != null ? s.Film.Titolo : string.Empty,
                    SalaNome = s.Sala != null ? (s.Sala.Nome ?? $"Sala {s.Sala.NumeroSala}") : string.Empty,
                    TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
                    s.Data,
                    s.OraInizio,
                    s.OraFine,
                    s.PrezzoBase,
                    CinemaNome = s.Sala != null && s.Sala.Cinema != null ? s.Sala.Cinema.Nome : string.Empty,
                    CinemaIndirizzo = s.Sala != null && s.Sala.Cinema != null ? s.Sala.Cinema.Indirizzo : string.Empty,
                    CinemaCitta = s.Sala != null && s.Sala.Cinema != null ? s.Sala.Cinema.Citta : string.Empty
                })
                .FirstOrDefaultAsync();

            return show is null ? Results.NotFound() : Results.Ok(show);
        });

        app.MapGet("/cinema/{id:int}/programmazione", async (int id, DateOnly? data, FilmDbContext db) =>
        {
            var day = data ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var items = await db.Shows
                .Include(s => s.Film)
                .Include(s => s.Sala)
                .Where(s => s.Sala != null && s.Sala.CinemaId == id && s.Data == day)
                .OrderBy(s => s.OraInizio)
                .Select(s => new
                {
                    s.Id,
                    s.FilmId,
                    FilmTitolo = s.Film != null ? s.Film.Titolo : string.Empty,
                    s.SalaId,
                    TipologiaSala = s.Sala != null ? s.Sala.Tipologia.ToString() : string.Empty,
                    s.Data,
                    s.OraInizio,
                    s.PrezzoBase
                })
                .ToListAsync();
            return Results.Ok(items);
        });

        app.MapGet("/cinemas/nearby", async (decimal lat, decimal lng, FilmDbContext db) =>
        {
            var cinemas = await db.Cinemas
                .Where(c => c.Latitudine.HasValue && c.Longitudine.HasValue)
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.Citta,
                    c.Indirizzo,
                    c.Latitudine,
                    c.Longitudine,
                    Distance = Math.Sqrt(
                        Math.Pow((double)(c.Latitudine!.Value - lat), 2) +
                        Math.Pow((double)(c.Longitudine!.Value - lng), 2))
                })
                .OrderBy(c => c.Distance)
                .ToListAsync();
            return Results.Ok(cinemas);
        });

        return app;
    }
}
