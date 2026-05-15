using System.Text.Json;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class TMDBFilmSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TMDBFilmSyncService> _logger;
    private readonly TimeSpan _syncInterval;
    private readonly int _maxPages;

    public TMDBFilmSyncService(IServiceScopeFactory scopeFactory, ILogger<TMDBFilmSyncService> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var syncIntervalMinutesRaw = Environment.GetEnvironmentVariable("TMDB_SYNC_INTERVAL_MINUTES")
            ?? configuration["TMDB:SyncIntervalMinutes"];
        var maxPagesRaw = Environment.GetEnvironmentVariable("TMDB_SYNC_MAX_PAGES")
            ?? configuration["TMDB:SyncMaxPages"];

        var syncIntervalMinutes = int.TryParse(syncIntervalMinutesRaw, out var interval) && interval > 0
            ? interval
            : 60;
        _syncInterval = TimeSpan.FromMinutes(syncIntervalMinutes);

        _maxPages = int.TryParse(maxPagesRaw, out var pages) && pages > 0
            ? pages
            : 3;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TMDB Film Sync Service avviato (intervallo: {Minutes} minuti, pagine: {Pages})", (int)_syncInterval.TotalMinutes, _maxPages);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            await RunSyncSafelyAsync(stoppingToken);

            using var timer = new PeriodicTimer(_syncInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunSyncSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TMDB Film Sync Service arrestato");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore non gestito nel TMDB Film Sync Service");
        }
    }

    private async Task RunSyncSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            await SyncPopularMoviesAsync(stoppingToken);
            await BackfillLocalMoviesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la sincronizzazione TMDB");
        }
    }

    public async Task SyncPopularMoviesAsync(CancellationToken stoppingToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var tmdbService = scope.ServiceProvider.GetRequiredService<ITMDBService>();

        _logger.LogInformation("Inizio sincronizzazione film popolari da TMDB");

        var importedCount = 0;
        var updatedCount = 0;

        for (int page = 1; page <= _maxPages; page++)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var json = await tmdbService.GetPopularMoviesAsync(page);
            if (string.IsNullOrWhiteSpace(json)) break;

            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");

            foreach (var item in results.EnumerateArray())
            {
                if (stoppingToken.IsCancellationRequested) break;

                var tmdbId = item.GetProperty("id").GetInt32();
                var title = item.GetProperty("title").GetString() ?? "Unknown";
                var posterPath = item.TryGetProperty("poster_path", out var pp) ? pp.GetString() : null;
                var overview = item.TryGetProperty("overview", out var ov) ? ov.GetString() : null;
                var releaseDateStr = item.TryGetProperty("release_date", out var rd) ? rd.GetString() : null;
                var voteAvg = item.TryGetProperty("vote_average", out var va) ? va.GetDouble() : 0;

                if (string.IsNullOrWhiteSpace(overview)) continue;

                if (string.Equals(title, "Unknown", StringComparison.OrdinalIgnoreCase)) continue;

                DateTime dataProduzione;
                if (!string.IsNullOrWhiteSpace(releaseDateStr) && DateTime.TryParse(releaseDateStr, out var parsed))
                    dataProduzione = parsed;
                else
                    dataProduzione = DateTime.Now;

                var creditsJson = await tmdbService.GetMovieCreditsAsync(tmdbId);
                string? registaNome = null;
                string? registaNazionalita = null;
                string? cast = null;
                if (!string.IsNullOrWhiteSpace(creditsJson))
                {
                    using var docCredits = JsonDocument.Parse(creditsJson);
                    var crewArr = docCredits.RootElement.GetProperty("crew");
                    var director = crewArr.EnumerateArray().FirstOrDefault(c =>
                        c.TryGetProperty("department", out var dept) &&
                        dept.GetString() == "Directing");
                    registaNome = director.TryGetProperty("name", out var dn) ? dn.GetString() : null;

                    if (director.TryGetProperty("id", out var directorIdProp) &&
                        directorIdProp.TryGetInt32(out var directorId))
                    {
                        registaNazionalita = await ResolveDirectorNationalityAsync(tmdbService, directorId, stoppingToken);
                    }

                    var castList = new List<string>();
                    if (docCredits.RootElement.TryGetProperty("cast", out var castArr))
                    {
                        foreach (var member in castArr.EnumerateArray().Take(5))
                        {
                            var name = member.TryGetProperty("name", out var n) ? n.GetString() : null;
                            var character = member.TryGetProperty("character", out var ch) ? ch.GetString() : null;

                            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "Unknown", StringComparison.OrdinalIgnoreCase))
                                continue;

                            castList.Add(string.IsNullOrWhiteSpace(character) ? name : $"{name} ({character})");
                        }
                    }

                    cast = castList.Count > 0 ? string.Join(", ", castList) : null;
                }

                var existing = context.Films.Local.FirstOrDefault(f => f.TmdbId == tmdbId);
                if (existing == null)
                    existing = await context.Films.FirstOrDefaultAsync(f => f.TmdbId == tmdbId, stoppingToken);

                if (existing != null)
                {
                    var updated = false;

                    if (!string.IsNullOrWhiteSpace(registaNome))
                    {
                        var regista = await GetOrCreateRegistaAsync(context, registaNome, registaNazionalita, stoppingToken);
                        if (existing.RegistaId != regista.Id)
                        {
                            existing.RegistaId = regista.Id;
                            updated = true;
                        }

                        if (!string.Equals(existing.RegistaNome, registaNome, StringComparison.Ordinal))
                        {
                            existing.RegistaNome = registaNome;
                            updated = true;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(existing.Descrizione) || existing.Descrizione == existing.Titolo)
                    {
                        existing.Descrizione = overview;
                        updated = true;
                    }

                    if (!string.IsNullOrWhiteSpace(cast) &&
                        (string.IsNullOrWhiteSpace(existing.Cast) || existing.Cast.Contains("Unknown", StringComparison.OrdinalIgnoreCase)))
                    {
                        existing.Cast = cast;
                        updated = true;
                    }

                    var posterUrl = tmdbService.GetPosterUrl(posterPath);
                    if (!string.IsNullOrWhiteSpace(posterUrl) && !string.Equals(existing.CopertinaPath, posterUrl, StringComparison.Ordinal))
                    {
                        existing.CopertinaPath = posterUrl;
                        updated = true;
                    }

                    var shouldBeFeatured = voteAvg >= 7.5;
                    if (existing.Featured != shouldBeFeatured)
                    {
                        existing.Featured = shouldBeFeatured;
                        updated = true;
                    }

                    if (updated)
                    {
                        updatedCount++;
                    }
                }
                else
                {
                    var detailJson = await tmdbService.GetMovieDetailAsync(tmdbId);
                    int runtime = 0;
                    var genres = new List<string>();

                    if (!string.IsNullOrWhiteSpace(detailJson))
                    {
                        using var docDetail = JsonDocument.Parse(detailJson);
                        var root = docDetail.RootElement;
                        runtime = root.TryGetProperty("runtime", out var rt) ? rt.GetInt32() : 0;

                        if (root.TryGetProperty("genres", out var genresArr))
                        {
                            foreach (var g in genresArr.EnumerateArray())
                            {
                                if (g.TryGetProperty("id", out var gid))
                                    genres.Add(ITMDBService.MapTmdbGenreToItalian(gid.GetInt32()));
                            }
                        }
                    }

                    var regista = await GetOrCreateRegistaAsync(context, registaNome, registaNazionalita, stoppingToken);

                    var genreText = genres.Count > 0 ? string.Join(", ", genres) : null;
                    if (!string.IsNullOrWhiteSpace(genreText) && genreText.Length > 200)
                    {
                        genreText = genreText[..200];
                    }

                    var film = new Film
                    {
                        Titolo = title,
                        DataProduzione = dataProduzione,
                        RegistaId = regista.Id,
                        Durata = runtime,
                        CopertinaPath = tmdbService.GetPosterUrl(posterPath),
                        Descrizione = overview,
                        RegistaNome = string.IsNullOrWhiteSpace(registaNome) ? "Regista Sconosciuto" : registaNome,
                        Cast = cast,
                        Genere = genreText,
                        Featured = voteAvg >= 7.5,
                        DataRilascio = dataProduzione,
                        TmdbId = tmdbId
                    };

                    context.Films.Add(film);
                    importedCount++;
                }

                if ((importedCount + updatedCount) > 0 && (importedCount + updatedCount) % 20 == 0)
                    await context.SaveChangesAsync(stoppingToken);
            }
        }

        await context.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Sincronizzazione TMDB completata: {Imported} importati, {Updated} aggiornati", importedCount, updatedCount);
    }

    private async Task BackfillLocalMoviesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        var tmdbService = scope.ServiceProvider.GetRequiredService<ITMDBService>();

        var candidates = await context.Films
            .Where(f =>
                !f.TmdbId.HasValue ||
                string.IsNullOrWhiteSpace(f.CopertinaPath) ||
                f.CopertinaPath!.StartsWith("/media/") ||
                f.CopertinaPath.StartsWith("media/"))
            .ToListAsync(stoppingToken);

        var films = candidates
            .Where(f =>
                !f.TmdbId.HasValue ||
                string.IsNullOrWhiteSpace(f.CopertinaPath) ||
                f.CopertinaPath.StartsWith("/media/", StringComparison.OrdinalIgnoreCase) ||
                f.CopertinaPath.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogInformation("Backfill TMDB avviato: {Count} film candidati", films.Count);

        if (films.Count == 0)
        {
            _logger.LogInformation("Backfill TMDB: nessun film da aggiornare");
            return;
        }

        var updated = 0;
        foreach (var film in films)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var searchJson = await tmdbService.SearchMovieAsync(film.Titolo);
            if (string.IsNullOrWhiteSpace(searchJson)) continue;

            using var searchDoc = JsonDocument.Parse(searchJson);
            if (!searchDoc.RootElement.TryGetProperty("results", out var results)) continue;

            var targetYear = film.DataProduzione.Year;
            var best = results.EnumerateArray()
                .Select(r => new
                {
                    Id = r.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var id) ? id : 0,
                    Title = r.TryGetProperty("title", out var tEl) ? tEl.GetString() : null,
                    PosterPath = r.TryGetProperty("poster_path", out var pEl) ? pEl.GetString() : null,
                    Year = r.TryGetProperty("release_date", out var dEl) && DateTime.TryParse(dEl.GetString(), out var d)
                        ? d.Year
                        : 0
                })
                .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.Title))
                .OrderBy(x =>
                {
                    if (targetYear <= 1900 || x.Year == 0) return int.MaxValue;
                    return Math.Abs(x.Year - targetYear);
                })
                .ThenByDescending(x => string.Equals(x.Title!.Trim(), film.Titolo.Trim(), StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (best is null) continue;

            var changed = false;
            if (!film.TmdbId.HasValue || film.TmdbId.Value != best.Id)
            {
                film.TmdbId = best.Id;
                changed = true;
            }

            var posterUrl = tmdbService.GetPosterUrl(best.PosterPath);
            if (!string.IsNullOrWhiteSpace(posterUrl) &&
                !string.Equals(film.CopertinaPath, posterUrl, StringComparison.Ordinal))
            {
                film.CopertinaPath = posterUrl;
                changed = true;
            }

            if (changed) updated++;
        }

        if (updated > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
        }

        _logger.LogInformation("Backfill TMDB completato: {Updated} film aggiornati", updated);
    }

    private static async Task<Regista> GetOrCreateRegistaAsync(FilmDbContext context, string? fullName, string? nazionalita, CancellationToken stoppingToken)
    {
        var (nome, cognome) = SplitDirectorName(fullName);

        var existing = await context.Registi.FirstOrDefaultAsync(r =>
            r.Nome.ToLower() == nome.ToLower() &&
            r.Cognome.ToLower() == cognome.ToLower(),
            stoppingToken);

        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(nazionalita) &&
                (string.IsNullOrWhiteSpace(existing.Nazionalita) || existing.Nazionalita == "Unknown"))
            {
                existing.Nazionalita = nazionalita;
                await context.SaveChangesAsync(stoppingToken);
            }
            return existing;
        }

        var regista = new Regista
        {
            Nome = nome,
            Cognome = cognome,
            Nazionalita = string.IsNullOrWhiteSpace(nazionalita) ? "Unknown" : nazionalita
        };

        context.Registi.Add(regista);
        await context.SaveChangesAsync(stoppingToken);
        return regista;
    }

    private static async Task<string?> ResolveDirectorNationalityAsync(ITMDBService tmdbService, int directorId, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var personJson = await tmdbService.GetPersonDetailAsync(directorId);
        if (string.IsNullOrWhiteSpace(personJson))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(personJson);
        if (doc.RootElement.TryGetProperty("place_of_birth", out var pob))
        {
            var value = pob.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                var nationality = NormalizeNationalityFromPlace(value);
                if (!string.IsNullOrWhiteSpace(nationality))
                {
                    return nationality.Length > 100 ? nationality[..100] : nationality;
                }
            }
        }

        return null;
    }

    private static string NormalizeNationalityFromPlace(string placeOfBirth)
    {
        var country = placeOfBirth.Split(',').Last().Trim();
        if (string.IsNullOrWhiteSpace(country))
        {
            country = placeOfBirth.Trim();
        }

        var key = country.ToLowerInvariant();
        return key switch
        {
            "italy" => "Italia",
            "france" => "Francia",
            "germany" => "Germania",
            "spain" => "Spagna",
            "united kingdom" => "Regno Unito",
            "uk" => "Regno Unito",
            "england" => "Regno Unito",
            "scotland" => "Regno Unito",
            "wales" => "Regno Unito",
            "ireland" => "Irlanda",
            "united states" => "Stati Uniti",
            "usa" => "Stati Uniti",
            "u.s.a." => "Stati Uniti",
            "canada" => "Canada",
            "mexico" => "Messico",
            "brazil" => "Brasile",
            "argentina" => "Argentina",
            "chile" => "Cile",
            "colombia" => "Colombia",
            "japan" => "Giappone",
            "china" => "Cina",
            "south korea" => "Corea del Sud",
            "republic of korea" => "Corea del Sud",
            "north korea" => "Corea del Nord",
            "india" => "India",
            "pakistan" => "Pakistan",
            "turkey" => "Turchia",
            "russia" => "Russia",
            "ukraine" => "Ucraina",
            "poland" => "Polonia",
            "romania" => "Romania",
            "hungary" => "Ungheria",
            "czech republic" => "Repubblica Ceca",
            "slovakia" => "Slovacchia",
            "austria" => "Austria",
            "switzerland" => "Svizzera",
            "belgium" => "Belgio",
            "netherlands" => "Paesi Bassi",
            "norway" => "Norvegia",
            "sweden" => "Svezia",
            "denmark" => "Danimarca",
            "finland" => "Finlandia",
            "iceland" => "Islanda",
            "portugal" => "Portogallo",
            "greece" => "Grecia",
            "croatia" => "Croazia",
            "serbia" => "Serbia",
            "slovenia" => "Slovenia",
            "bosnia and herzegovina" => "Bosnia ed Erzegovina",
            "montenegro" => "Montenegro",
            "albania" => "Albania",
            "bulgaria" => "Bulgaria",
            "estonia" => "Estonia",
            "latvia" => "Lettonia",
            "lithuania" => "Lituania",
            "australia" => "Australia",
            "new zealand" => "Nuova Zelanda",
            "south africa" => "Sudafrica",
            "egypt" => "Egitto",
            "morocco" => "Marocco",
            "tunisia" => "Tunisia",
            "algeria" => "Algeria",
            "nigeria" => "Nigeria",
            "kenya" => "Kenya",
            "israel" => "Israele",
            "iran" => "Iran",
            "iraq" => "Iraq",
            "saudi arabia" => "Arabia Saudita",
            "united arab emirates" => "Emirati Arabi Uniti",
            _ => country
        };
    }

    private static (string Nome, string Cognome) SplitDirectorName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ("Regista", "Sconosciuto");
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return ("Regista", "Sconosciuto");
        }

        if (parts.Length == 1)
        {
            var onlyName = parts[0].Length > 100 ? parts[0][..100] : parts[0];
            return (onlyName, "-");
        }

        var nome = parts[0];
        var cognome = string.Join(' ', parts.Skip(1));

        if (nome.Length > 100)
        {
            nome = nome[..100];
        }

        if (cognome.Length > 100)
        {
            cognome = cognome[..100];
        }

        return (nome, cognome);
    }
}
