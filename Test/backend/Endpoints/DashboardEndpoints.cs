using FilmAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard/overview", async (FilmDbContext db) =>
        {
            var nowLocal = DateTime.Now;
            var today = nowLocal.Date;
            var nowTime = nowLocal.TimeOfDay;

            var filmsCount = await db.Films.AsNoTracking().CountAsync();
            var registiCount = await db.Registi.AsNoTracking().CountAsync();
            var cinemasCount = await db.Cinemas.AsNoTracking().CountAsync();
            var proiezioniCount = await db.Proiezioni.AsNoTracking().CountAsync();

            var featuredFilmsRaw = await db.Films
                .AsNoTracking()
                .Include(f => f.Regista)
                .Include(f => f.FilmsCategorie)
                .ThenInclude(fc => fc.Categoria)
                .OrderByDescending(f => f.Featured)
                .ThenByDescending(f => f.DataRilascio ?? f.DataProduzione)
                .ThenByDescending(f => f.Id)
                .Take(6)
                .ToListAsync();

            var featuredFilms = featuredFilmsRaw.Select(f => new
            {
                f.Id,
                f.Titolo,
                f.RegistaId,
                RegistaNome = !string.IsNullOrWhiteSpace(f.RegistaNome)
                    ? f.RegistaNome
                    : (f.Regista != null ? $"{f.Regista.Nome} {f.Regista.Cognome}" : string.Empty),
                f.Durata,
                f.CopertinaPath,
                f.Featured,
                f.DataRilascio,
                f.Genere,
                Categorie = f.FilmsCategorie
                    .OrderBy(fc => fc.Categoria.Nome)
                    .Take(2)
                    .Select(fc => new
                    {
                        fc.CategoriaId,
                        Nome = fc.Categoria.Nome
                    })
                    .ToList()
            }).ToList();

            var upcomingProjections = await db.Proiezioni
                .AsNoTracking()
                .Where(p =>
                    p.Data > today ||
                    (p.Data >= today && p.Data < today.AddDays(1) && p.Ora >= nowTime))
                .OrderBy(p => p.Data)
                .ThenBy(p => p.Ora)
                .Take(5)
                .Select(p => new
                {
                    p.Id,
                    p.ShowId,
                    p.CinemaId,
                    p.FilmId,
                    p.Data,
                    p.Ora,
                    FilmTitolo = p.Film != null ? p.Film.Titolo : string.Empty,
                    CinemaNome = p.Cinema != null ? p.Cinema.Nome : string.Empty,
                    CinemaCitta = p.Cinema != null ? p.Cinema.Citta : string.Empty
                })
                .ToListAsync();

            return Results.Ok(new
            {
                stats = new
                {
                    films = filmsCount,
                    registi = registiCount,
                    cinemas = cinemasCount,
                    proiezioni = proiezioniCount
                },
                featuredFilms,
                upcomingProjections
            });
        });

        return app;
    }
}
