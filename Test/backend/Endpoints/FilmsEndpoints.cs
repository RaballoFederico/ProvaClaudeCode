// DOC: Endpoint 'FilmsEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using System.Text.Json;
using System.Globalization;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class FilmsEndpoints
{
    private static readonly string DefaultCoverImagePath =
        Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH") ?? "/media/defaults/cover-default.jpg";

    // DOC-METHOD: 'MapFilmsEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static RouteGroupBuilder MapFilmsEndpoints(this RouteGroupBuilder group)
    {
        // GET /films - Visibile a tutti (supporta versione compatta per liste rapide)
        group.MapGet("/", async (FilmDbContext db, HttpContext httpContext, bool? summary, int? limit, bool? includeCategories) =>
        {
            // Normalizzazione limite per proteggere endpoint e payload size.
            var normalizedLimit = limit.HasValue
                ? Math.Clamp(limit.Value, 1, 500)
                : (int?)null;

            if (summary.GetValueOrDefault())
            {
                // Vista summary: meno campi, ottimizzata per listing pubblici e card veloci.
                var summaryQuery = db.Films
                    .AsNoTracking()
                    .OrderByDescending(f => f.DataRilascio ?? f.DataProduzione)
                    .ThenByDescending(f => f.Id)
                    .Select(f => new FilmSummaryItem
                    {
                        Id = f.Id,
                        TmdbId = f.TmdbId,
                        Titolo = f.Titolo,
                        DataProduzione = f.DataProduzione,
                        DataRilascio = f.DataRilascio,
                        RegistaId = f.RegistaId,
                        RegistaNome = !string.IsNullOrWhiteSpace(f.RegistaNome)
                            ? f.RegistaNome
                            : (f.Regista != null ? $"{f.Regista.Nome} {f.Regista.Cognome}" : null),
                        Durata = f.Durata,
                        CopertinaPath = f.CopertinaPath,
                        Featured = f.Featured,
                        Genere = f.Genere
                    });
                var summaryItems = await summaryQuery.ToListAsync();
                // Deduplica server-side per evitare che duplicati DB contaminino tutte le UI.
                var dedupedSummary = DeduplicateByTitle(
                    summaryItems,
                    item => item.Titolo,
                    item => item.DataRilascio ?? item.DataProduzione,
                    item => item.Id);
                if (normalizedLimit.HasValue)
                {
                    dedupedSummary = dedupedSummary.Take(normalizedLimit.Value).ToList();
                }

                var normalized = dedupedSummary.Select(f => new
                {
                    f.Id,
                    f.TmdbId,
                    f.Titolo,
                    f.DataProduzione,
                    f.DataRilascio,
                    f.RegistaId,
                    f.RegistaNome,
                    f.Durata,
                    CopertinaPath = NormalizeMediaUrl(f.CopertinaPath, httpContext),
                    f.Featured,
                    f.Genere
                });

                return Results.Ok(normalized);
            }

            var withCategories = includeCategories.GetValueOrDefault(true);
            if (withCategories)
            {
                // Vista completa con categorie joinate: usata da pagine gestione/catalogo dettagliato.
                var fullQuery = db.Films
                    .AsNoTracking()
                    .Include(f => f.FilmsCategorie)
                    .ThenInclude(fc => fc.Categoria)
                    .OrderBy(f => f.Titolo)
                    .Select(f => new FilmDTO
                    {
                        Id = f.Id,
                        TmdbId = f.TmdbId,
                        Titolo = f.Titolo,
                        DataProduzione = f.DataProduzione,
                        RegistaId = f.RegistaId,
                        Durata = f.Durata,
                        CopertinaPath = f.CopertinaPath,
                        FilmatoPath = f.FilmatoPath,
                        Descrizione = f.Descrizione,
                        RegistaNome = f.RegistaNome,
                        Cast = f.Cast,
                        Featured = f.Featured,
                        DataRilascio = f.DataRilascio,
                        Genere = f.Genere,
                        CategorieIds = f.FilmsCategorie.Select(fc => fc.CategoriaId).ToList(),
                        Categorie = f.FilmsCategorie.Select(fc => new CategoriaDTO
                        {
                            Id = fc.Categoria.Id,
                            Nome = fc.Categoria.Nome,
                            Descrizione = fc.Categoria.Descrizione
                        }).ToList()
                    });

                var fullItems = await fullQuery.ToListAsync();
                // Anche qui deduplica per garantire coerenza tra endpoint summary/full.
                fullItems = DeduplicateByTitle(
                    fullItems,
                    item => item.Titolo,
                    item => item.DataRilascio ?? item.DataProduzione,
                    item => item.Id);
                if (normalizedLimit.HasValue)
                {
                    fullItems = fullItems.Take(normalizedLimit.Value).ToList();
                }
                foreach (var item in fullItems)
                {
                    item.CopertinaPath = NormalizeMediaUrl(item.CopertinaPath, httpContext);
                }

                return Results.Ok(fullItems);
            }

            var leanQuery = db.Films
                .AsNoTracking()
                .OrderBy(f => f.Titolo)
                .Select(f => new FilmDTO
                {
                    Id = f.Id,
                    TmdbId = f.TmdbId,
                    Titolo = f.Titolo,
                    DataProduzione = f.DataProduzione,
                    RegistaId = f.RegistaId,
                    Durata = f.Durata,
                    CopertinaPath = f.CopertinaPath,
                    FilmatoPath = f.FilmatoPath,
                    Descrizione = f.Descrizione,
                    RegistaNome = f.RegistaNome,
                    Cast = f.Cast,
                    Featured = f.Featured,
                    DataRilascio = f.DataRilascio,
                    Genere = f.Genere,
                    CategorieIds = new List<int>(),
                    Categorie = new List<CategoriaDTO>()
                });

            var leanItems = await leanQuery.ToListAsync();
            // Vista lean senza include categorie: deduplica applicata comunque per consistenza API-wide.
            leanItems = DeduplicateByTitle(
                leanItems,
                item => item.Titolo,
                item => item.DataRilascio ?? item.DataProduzione,
                item => item.Id);
            if (normalizedLimit.HasValue)
            {
                leanItems = leanItems.Take(normalizedLimit.Value).ToList();
            }
            foreach (var item in leanItems)
            {
                item.CopertinaPath = NormalizeMediaUrl(item.CopertinaPath, httpContext);
            }

            return Results.Ok(leanItems);
        });

        // GET /films/{id} - Visibile a tutti (include categorie)
        group.MapGet("/{id}", async (int id, FilmDbContext db, HttpContext httpContext) =>
        {
            var film = await db.Films
                .AsNoTracking()
                .Include(f => f.FilmsCategorie)
                .ThenInclude(fc => fc.Categoria)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (film is null) return Results.NotFound();

            return Results.Ok(new FilmDTO
            {
                Id = film.Id,
                TmdbId = film.TmdbId,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = NormalizeMediaUrl(film.CopertinaPath, httpContext),
                FilmatoPath = film.FilmatoPath,
                Descrizione = film.Descrizione,
                RegistaNome = film.RegistaNome,
                Cast = film.Cast,
                Featured = film.Featured,
                DataRilascio = film.DataRilascio,
                Genere = film.Genere,
                CategorieIds = film.FilmsCategorie.Select(fc => fc.CategoriaId).ToList(),
                Categorie = film.FilmsCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.Categoria.Id,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
            });
        });

        // GET /films/{id}/cast-tmdb - Visibile a tutti
        group.MapGet("/{id}/cast-tmdb", async (int id, FilmDbContext db, ITMDBService tmdbService) =>
        {
            var film = await db.Films.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (film is null) return Results.NotFound("Film non trovato");

            var tmdbId = await ResolveTmdbIdForFilmAsync(film, tmdbService);
            if (tmdbId is null)
                return Results.Ok(Array.Empty<object>());

            var creditsJson = await tmdbService.GetMovieCreditsAsync(tmdbId.Value);
            if (string.IsNullOrWhiteSpace(creditsJson))
                return Results.Ok(Array.Empty<object>());

            using var creditsDoc = JsonDocument.Parse(creditsJson);
            if (!creditsDoc.RootElement.TryGetProperty("cast", out var castArr))
                return Results.Ok(Array.Empty<object>());

            var cast = castArr.EnumerateArray()
                .Select(member => new
                {
                    id = member.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var actorId) ? actorId : 0,
                    nome = member.TryGetProperty("name", out var n) ? n.GetString() : null,
                    personaggio = member.TryGetProperty("character", out var ch) ? ch.GetString() : null,
                    ordine = member.TryGetProperty("order", out var o) && o.TryGetInt32(out var order) ? order : int.MaxValue,
                    profile = tmdbService.GetProfileUrl(member.TryGetProperty("profile_path", out var pp) ? pp.GetString() : null)
                })
                .Where(x => x.id > 0 && !string.IsNullOrWhiteSpace(x.nome))
                .OrderBy(x => x.ordine)
                .Take(12)
                .ToList();

            return Results.Ok(cast);
        });

        // GET /films/{id}/meta-tmdb - Visibile a tutti (fallback descrizione/regista/cast)
        group.MapGet("/{id}/meta-tmdb", async (int id, FilmDbContext db, ITMDBService tmdbService) =>
        {
            var film = await db.Films.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (film is null) return Results.NotFound("Film non trovato");

            var tmdbId = await ResolveTmdbIdForFilmAsync(film, tmdbService);
            if (tmdbId is null)
            {
                return Results.Ok(new
                {
                    descrizione = (string?)null,
                    regista = (string?)null,
                    cast = Array.Empty<string>()
                });
            }

            string? descrizione = null;
            string? regista = null;
            var cast = Array.Empty<string>();

            var detailJson = await tmdbService.GetMovieDetailAsync(tmdbId.Value);
            if (!string.IsNullOrWhiteSpace(detailJson))
            {
                using var detailDoc = JsonDocument.Parse(detailJson);
                if (detailDoc.RootElement.TryGetProperty("overview", out var overview))
                {
                    descrizione = overview.GetString();
                }
            }

            var creditsJson = await tmdbService.GetMovieCreditsAsync(tmdbId.Value);
            if (!string.IsNullOrWhiteSpace(creditsJson))
            {
                using var creditsDoc = JsonDocument.Parse(creditsJson);
                if (creditsDoc.RootElement.TryGetProperty("crew", out var crewArr))
                {
                    regista = crewArr.EnumerateArray()
                        .Where(c =>
                            c.TryGetProperty("job", out var job) &&
                            string.Equals(job.GetString(), "Director", StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.TryGetProperty("name", out var n) ? n.GetString() : null)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                }

                if (creditsDoc.RootElement.TryGetProperty("cast", out var castArr))
                {
                    cast = ExtractCastNames(castArr);
                }
            }

            // Fallback: se cast vuoto, prova corrispondenze alternative per titolo (stesso anno prioritario)
            if (cast.Length == 0)
            {
                var searchJson = await tmdbService.SearchMovieAsync(film.Titolo);
                if (!string.IsNullOrWhiteSpace(searchJson))
                {
                    using var searchDoc = JsonDocument.Parse(searchJson);
                    if (searchDoc.RootElement.TryGetProperty("results", out var results))
                    {
                        var filmYear = film.DataProduzione.Year;
                        var candidates = results.EnumerateArray()
                            .Select(r => new
                            {
                                Id = r.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var rid) ? rid : 0,
                                Title = r.TryGetProperty("title", out var tEl) ? tEl.GetString() : string.Empty,
                                Year = r.TryGetProperty("release_date", out var rdEl) && DateTime.TryParse(rdEl.GetString(), out var rd)
                                    ? rd.Year
                                    : 0
                            })
                            .Where(x => x.Id > 0 && string.Equals(x.Title?.Trim(), film.Titolo.Trim(), StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.Year == filmYear)
                            .ThenBy(x => x.Year == 0 ? int.MaxValue : Math.Abs(x.Year - filmYear))
                            .Take(5)
                            .ToList();

                        foreach (var candidate in candidates)
                        {
                            var cj = await tmdbService.GetMovieCreditsAsync(candidate.Id);
                            if (string.IsNullOrWhiteSpace(cj)) continue;
                            using var cd = JsonDocument.Parse(cj);
                            if (!cd.RootElement.TryGetProperty("cast", out var ca)) continue;
                            var altCast = ExtractCastNames(ca);
                            if (altCast.Length > 0)
                            {
                                cast = altCast;
                                break;
                            }
                        }
                    }
                }
            }

            return Results.Ok(new
            {
                descrizione,
                regista,
                cast
            });
        });

        // GET /films/actors/{actorId}/tmdb - Visibile a tutti
        group.MapGet("/actors/{actorId}/tmdb", async (int actorId, ITMDBService tmdbService, FilmDbContext db) =>
        {
            if (actorId <= 0) return Results.BadRequest("ActorId non valido");

            var personJson = await tmdbService.GetPersonDetailAsync(actorId);
            if (string.IsNullOrWhiteSpace(personJson))
                return Results.NotFound("Attore non trovato su TMDB");

            using var doc = JsonDocument.Parse(personJson);
            var root = doc.RootElement;

            var creditsJson = await tmdbService.GetPersonMovieCreditsAsync(actorId);
            var filmografia = new List<object>();
            if (!string.IsNullOrWhiteSpace(creditsJson))
            {
                using var creditsDoc = JsonDocument.Parse(creditsJson);
                if (creditsDoc.RootElement.TryGetProperty("cast", out var castCredits))
                {
                    var tmdbIds = castCredits.EnumerateArray()
                        .Select(x => x.TryGetProperty("id", out var id) && id.TryGetInt32(out var value) ? value : 0)
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    var localFilmsMap = await db.Films.AsNoTracking()
                        .Where(f => f.TmdbId.HasValue && tmdbIds.Contains(f.TmdbId.Value))
                        .Select(f => new { TmdbId = f.TmdbId!.Value, LocalFilmId = f.Id })
                        .ToDictionaryAsync(x => x.TmdbId, x => x.LocalFilmId);

                    filmografia = castCredits.EnumerateArray()
                        .Select(movie => new
                        {
                            tmdbId = movie.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var tmdbMovieId) ? tmdbMovieId : 0,
                            titolo = movie.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null,
                            personaggio = movie.TryGetProperty("character", out var charEl) ? charEl.GetString() : null,
                            dataRilascio = movie.TryGetProperty("release_date", out var releaseEl) ? releaseEl.GetString() : null,
                            poster = tmdbService.GetPosterUrl(movie.TryGetProperty("poster_path", out var pp) ? pp.GetString() : null, "w342"),
                            popolarita = movie.TryGetProperty("popularity", out var popEl) ? popEl.GetDouble() : 0
                        })
                        .Where(x => x.tmdbId > 0 && !string.IsNullOrWhiteSpace(x.titolo))
                        .OrderByDescending(x => x.popolarita)
                        .Take(12)
                        .Select(x => (object)new
                        {
                            x.tmdbId,
                            x.titolo,
                            x.personaggio,
                            x.dataRilascio,
                            x.poster,
                            localFilmId = localFilmsMap.TryGetValue(x.tmdbId, out var localId) ? localId : (int?)null
                        })
                        .ToList();
                }
            }

            return Results.Ok(new
            {
                id = actorId,
                nome = root.TryGetProperty("name", out var name) ? name.GetString() : null,
                biography = root.TryGetProperty("biography", out var bio) ? bio.GetString() : null,
                compleanno = root.TryGetProperty("birthday", out var birthday) ? birthday.GetString() : null,
                luogoNascita = root.TryGetProperty("place_of_birth", out var pob) ? pob.GetString() : null,
                popolarita = root.TryGetProperty("popularity", out var popularity) ? popularity.GetDouble() : 0,
                profile = tmdbService.GetProfileUrl(root.TryGetProperty("profile_path", out var pp) ? pp.GetString() : null, "h632"),
                filmografia
            });
        });

        // POST /films - Admin e PowerUser
        group.MapPost("/", [Authorize(Roles = "Admin,PowerUser")] async (FilmCreateDTO dto, FilmDbContext db) =>
        {
            var registaExists = await db.Registi.AnyAsync(r => r.Id == dto.RegistaId);
            if (!registaExists)
                return Results.BadRequest("Regista not found");

            if (dto.CategoriaIds.Any())
            {
                var categorieEsistenti = await db.Categorie
                    .Where(c => dto.CategoriaIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (categorieEsistenti.Count != dto.CategoriaIds.Count)
                {
                    return Results.BadRequest("Una o piÃ¹ categorie non esistono");
                }
            }

            var copertinaPath = string.IsNullOrWhiteSpace(dto.CopertinaPath)
                ? DefaultCoverImagePath
                : dto.CopertinaPath;

            var film = new Film
            {
                Titolo = dto.Titolo,
                DataProduzione = dto.DataProduzione,
                RegistaId = dto.RegistaId,
                Durata = dto.Durata,
                CopertinaPath = copertinaPath,
                FilmatoPath = dto.FilmatoPath,
                Descrizione = dto.Descrizione,
                RegistaNome = dto.RegistaNome,
                Cast = dto.Cast,
                Featured = dto.Featured,
                DataRilascio = dto.DataRilascio,
                Genere = dto.Genere,
                FilmsCategorie = dto.CategoriaIds.Select(id => new FilmCategoria { CategoriaId = id }).ToList()
            };

            db.Films.Add(film);
            await db.SaveChangesAsync();

            await db.Entry(film).Collection(f => f.FilmsCategorie).Query().Include(fc => fc.Categoria).LoadAsync();

            return Results.Created($"/films/{film.Id}", new FilmDTO
            {
                Id = film.Id,
                TmdbId = film.TmdbId,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = film.CopertinaPath,
                FilmatoPath = film.FilmatoPath,
                Descrizione = film.Descrizione,
                RegistaNome = film.RegistaNome,
                Cast = film.Cast,
                Featured = film.Featured,
                DataRilascio = film.DataRilascio,
                Genere = film.Genere,
                CategorieIds = film.FilmsCategorie.Select(fc => fc.CategoriaId).ToList(),
                Categorie = film.FilmsCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.CategoriaId,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
            });
        });

        // GET /films/tmdb-search?q=... - Admin e PowerUser
        group.MapGet("/tmdb-search", [Authorize(Roles = "Admin,PowerUser")] async (string q, ITMDBService tmdbService) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("Query richiesta");

            var searchJson = await tmdbService.SearchMovieAsync(q.Trim());
            if (string.IsNullOrWhiteSpace(searchJson))
                return Results.Ok(Array.Empty<object>());

            using var doc = JsonDocument.Parse(searchJson);
            if (!doc.RootElement.TryGetProperty("results", out var results))
                return Results.Ok(Array.Empty<object>());

            var payload = results.EnumerateArray()
                .Take(10)
                .Select(item => new
                {
                    id = item.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    titolo = item.TryGetProperty("title", out var t) ? t.GetString() : null,
                    dataRilascio = item.TryGetProperty("release_date", out var rd) ? rd.GetString() : null,
                    poster = tmdbService.GetPosterUrl(item.TryGetProperty("poster_path", out var pp) ? pp.GetString() : null),
                    voto = item.TryGetProperty("vote_average", out var va) ? va.GetDouble() : 0
                })
                .Where(x => x.id > 0 && !string.IsNullOrWhiteSpace(x.titolo))
                .ToList();

            return Results.Ok(payload);
        });

        // POST /films/import-tmdb - Admin e PowerUser
        group.MapPost("/import-tmdb", [Authorize(Roles = "Admin,PowerUser")] async (TMDBImportRequestDTO dto, FilmDbContext db, ITMDBService tmdbService) =>
        {
            var detailJson = await tmdbService.GetMovieDetailAsync(dto.TmdbMovieId);
            if (string.IsNullOrWhiteSpace(detailJson))
                return Results.BadRequest("TMDB non configurato o film non trovato");

            using var docDetail = JsonDocument.Parse(detailJson);
            var root = docDetail.RootElement;

            var title = root.GetProperty("title").GetString() ?? "Unknown";
            var overview = root.TryGetProperty("overview", out var ov) ? ov.GetString() : null;
            var posterPath = root.TryGetProperty("poster_path", out var pp) ? pp.GetString() : null;
            var backdropPath = root.TryGetProperty("backdrop_path", out var bp) ? bp.GetString() : null;
            var releaseDateStr = root.TryGetProperty("release_date", out var rd) ? rd.GetString() : null;
            var runtime = root.TryGetProperty("runtime", out var rt) ? rt.GetInt32() : 0;
            var voteAvg = root.TryGetProperty("vote_average", out var va) ? va.GetDouble() : 0;

            DateTime dataProduzione;
            if (!string.IsNullOrWhiteSpace(releaseDateStr) && DateTime.TryParse(releaseDateStr, out var parsed))
                dataProduzione = parsed;
            else
                dataProduzione = DateTime.Now;

            var genres = new List<string>();
            if (root.TryGetProperty("genres", out var genresArr))
            {
                foreach (var g in genresArr.EnumerateArray())
                {
                    if (g.TryGetProperty("id", out var gid))
                        genres.Add(ITMDBService.MapTmdbGenreToItalian(gid.GetInt32()));
                }
            }
            var genere = genres.Count > 0 ? string.Join(", ", genres) : null;
            if (!string.IsNullOrWhiteSpace(genere) && genere.Length > 200)
            {
                genere = genere[..200];
            }

            var posterUrl = tmdbService.GetPosterUrl(posterPath);

            var creditsJson = await tmdbService.GetMovieCreditsAsync(dto.TmdbMovieId);
            string? cast = null;
            string? registaNome = null;
            string? registaNazionalita = null;
            if (!string.IsNullOrWhiteSpace(creditsJson))
            {
                using var docCredits = JsonDocument.Parse(creditsJson);
                var crewArr = docCredits.RootElement.GetProperty("crew");
                var director = crewArr.EnumerateArray().FirstOrDefault(c =>
                    c.TryGetProperty("known_for_department", out var dept) &&
                    dept.GetString() == "Directing");
                registaNome = director.TryGetProperty("name", out var dn) ? dn.GetString() : null;

                if (director.TryGetProperty("id", out var directorIdProp) &&
                    directorIdProp.TryGetInt32(out var directorId))
                {
                    registaNazionalita = await ResolveDirectorNationalityAsync(tmdbService, directorId);
                }

                var castList = new List<string>();
                var castArr = docCredits.RootElement.GetProperty("cast");
                var topCast = castArr.EnumerateArray().Take(5);
                foreach (var member in topCast)
                {
                    var name = member.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var character = member.TryGetProperty("character", out var ch) ? ch.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "Unknown", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrWhiteSpace(name))
                        castList.Add(string.IsNullOrWhiteSpace(character) ? name : $"{name} ({character})");
                }
                cast = string.Join(", ", castList);
            }

            var existing = await db.Films.FirstOrDefaultAsync(f =>
                f.Titolo.ToLower() == title.ToLower() && f.DataProduzione.Year == dataProduzione.Year);
            var regista = await GetOrCreateRegistaAsync(db, registaNome, registaNazionalita);

            if (existing != null)
            {
                existing.Descrizione = overview;
                existing.CopertinaPath = !string.IsNullOrWhiteSpace(posterUrl) ? posterUrl : existing.CopertinaPath;
                existing.Durata = runtime > 0 ? runtime : existing.Durata;
                existing.Genere = genere ?? existing.Genere;
                existing.Cast = cast ?? existing.Cast;
                existing.TmdbId = dto.TmdbMovieId;
                existing.RegistaId = regista.Id;
                existing.RegistaNome = string.IsNullOrWhiteSpace(registaNome) ? existing.RegistaNome : registaNome;
                existing.DataRilascio = dataProduzione;
                await db.SaveChangesAsync();
                return Results.Ok(new TMDBImportResultDTO
                {
                    Success = true,
                    Message = "Film aggiornato con dati TMDB",
                    FilmId = existing.Id,
                    ExistingOrNew = "updated"
                });
            }

            var film = new Film
            {
                Titolo = title,
                DataProduzione = dataProduzione,
                RegistaId = regista.Id,
                Durata = runtime,
                CopertinaPath = posterUrl,
                Descrizione = overview,
                RegistaNome = string.IsNullOrWhiteSpace(registaNome) ? "Regista Sconosciuto" : registaNome,
                Cast = cast,
                Featured = false,
                DataRilascio = dataProduzione,
                Genere = genere,
                TmdbId = dto.TmdbMovieId
            };

            db.Films.Add(film);
            await db.SaveChangesAsync();

            return Results.Created($"/films/{film.Id}", new TMDBImportResultDTO
            {
                Success = true,
                Message = "Film importato da TMDB",
                FilmId = film.Id,
                ExistingOrNew = "created"
            });
        });

        // POST /films/sync-tmdb-media - Admin e PowerUser
        // Aggancia i film locali a TMDB (tmdbId + poster URL) quando hanno path locale /media o tmdbId mancante.
        group.MapPost("/sync-tmdb-media", [Authorize(Roles = "Admin,PowerUser")] async (FilmDbContext db, ITMDBService tmdbService) =>
        {
            var films = await db.Films.ToListAsync();
            var updated = 0;
            var scanned = 0;

            foreach (var film in films)
            {
                scanned++;
                var needsSync =
                    !film.TmdbId.HasValue ||
                    string.IsNullOrWhiteSpace(film.CopertinaPath) ||
                    film.CopertinaPath.StartsWith("/media/", StringComparison.OrdinalIgnoreCase) ||
                    film.CopertinaPath.StartsWith("media/", StringComparison.OrdinalIgnoreCase);

                if (!needsSync) continue;

                var searchJson = await tmdbService.SearchMovieAsync(film.Titolo);
                if (string.IsNullOrWhiteSpace(searchJson)) continue;

                using var searchDoc = JsonDocument.Parse(searchJson);
                if (!searchDoc.RootElement.TryGetProperty("results", out var results)) continue;

                var candidates = results.EnumerateArray()
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
                    .OrderByDescending(x => string.Equals(x.Title!.Trim(), film.Titolo.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ThenBy(x =>
                    {
                        var targetYear = film.DataProduzione.Year;
                        return x.Year == 0 ? int.MaxValue : Math.Abs(x.Year - targetYear);
                    })
                    .ToList();

                var best = candidates.FirstOrDefault();
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

                if (changed)
                {
                    updated++;
                }
            }

            if (updated > 0)
            {
                await db.SaveChangesAsync();
            }

            return Results.Ok(new
            {
                message = "Sincronizzazione TMDB completata",
                scanned,
                updated
            });
        });

        // POST /films/{id}/sync-cast-tmdb - Admin e PowerUser
        group.MapPost("/{id}/sync-cast-tmdb", [Authorize(Roles = "Admin,PowerUser")] async (int id, FilmDbContext db, ITMDBService tmdbService) =>
        {
            var film = await db.Films.FirstOrDefaultAsync(f => f.Id == id);
            if (film is null) return Results.NotFound("Film non trovato");

            var tmdbId = film.TmdbId;
            if (tmdbId is null)
            {
                var searchJson = await tmdbService.SearchMovieAsync(film.Titolo);
                if (string.IsNullOrWhiteSpace(searchJson))
                    return Results.BadRequest("TMDB non configurato o risultato non trovato");

                using var searchDoc = JsonDocument.Parse(searchJson);
                if (!searchDoc.RootElement.TryGetProperty("results", out var results))
                    return Results.BadRequest("Risultati TMDB non disponibili");

                int? matchedId = null;
                var filmYear = film.DataProduzione.Year;

                foreach (var result in results.EnumerateArray().Take(10))
                {
                    if (!result.TryGetProperty("id", out var idProp) || !idProp.TryGetInt32(out var resultId))
                        continue;

                    var resultTitle = result.TryGetProperty("title", out var t) ? t.GetString() : string.Empty;
                    if (!string.Equals(resultTitle?.Trim(), film.Titolo.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    var release = result.TryGetProperty("release_date", out var rd) ? rd.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(release) && DateTime.TryParse(release, out var rdParsed))
                    {
                        if (rdParsed.Year == filmYear)
                        {
                            matchedId = resultId;
                            break;
                        }
                    }

                    matchedId ??= resultId;
                }

                tmdbId = matchedId;
                if (tmdbId is null)
                    return Results.BadRequest("Nessuna corrispondenza TMDB trovata per sincronizzare il cast");
            }

            var creditsJson = await tmdbService.GetMovieCreditsAsync(tmdbId.Value);
            if (string.IsNullOrWhiteSpace(creditsJson))
                return Results.BadRequest("Crediti TMDB non disponibili");

            using var creditsDoc = JsonDocument.Parse(creditsJson);
            if (!creditsDoc.RootElement.TryGetProperty("cast", out var castArr))
                return Results.BadRequest("Cast TMDB non disponibile");

            var cast = BuildCastTextFromTmdb(castArr);
            if (string.IsNullOrWhiteSpace(cast))
                return Results.BadRequest("Cast TMDB vuoto");

            film.Cast = cast;
            film.TmdbId = tmdbId;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                success = true,
                message = "Cast aggiornato da TMDB",
                filmId = film.Id,
                tmdbId = film.TmdbId,
                cast = film.Cast
            });
        });

        // PUT /films/{id} - Admin e PowerUser
        group.MapPut("/{id}", [Authorize(Roles = "Admin,PowerUser")] async (int id, FilmUpdateDTO dto, FilmDbContext db) =>
        {
            var film = await db.Films
                .Include(f => f.FilmsCategorie)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (film is null) return Results.NotFound();

            var registaExists = await db.Registi.AnyAsync(r => r.Id == dto.RegistaId);
            if (!registaExists)
                return Results.BadRequest("Regista not found");

            // Verifica che le categorie esistano
            if (dto.CategoriaIds.Any())
            {
                var categorieEsistenti = await db.Categorie
                    .Where(c => dto.CategoriaIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (categorieEsistenti.Count != dto.CategoriaIds.Count)
                {
                    return Results.BadRequest("Una o piÃ¹ categorie non esistono");
                }
            }

            film.Titolo = dto.Titolo;
            film.DataProduzione = dto.DataProduzione;
            film.RegistaId = dto.RegistaId;
            film.Durata = dto.Durata;
            film.CopertinaPath = string.IsNullOrWhiteSpace(dto.CopertinaPath)
                ? DefaultCoverImagePath
                : dto.CopertinaPath;
            film.FilmatoPath = dto.FilmatoPath;
            film.Descrizione = dto.Descrizione;
            film.RegistaNome = dto.RegistaNome;
            film.Cast = dto.Cast;
            film.Featured = dto.Featured;
            film.DataRilascio = dto.DataRilascio;
            film.Genere = dto.Genere;

            // Aggiorna categorie
            film.FilmsCategorie.Clear();
            foreach (var categoriaId in dto.CategoriaIds)
            {
                film.FilmsCategorie.Add(new FilmCategoria { CategoriaId = categoriaId });
            }

            await db.SaveChangesAsync();

            // Ricarica le categorie per il response
            await db.Entry(film).Collection(f => f.FilmsCategorie).Query().Include(fc => fc.Categoria).LoadAsync();

            return Results.Ok(new FilmDTO
            {
                Id = film.Id,
                TmdbId = film.TmdbId,
                Titolo = film.Titolo,
                DataProduzione = film.DataProduzione,
                RegistaId = film.RegistaId,
                Durata = film.Durata,
                CopertinaPath = film.CopertinaPath,
                FilmatoPath = film.FilmatoPath,
                Descrizione = film.Descrizione,
                RegistaNome = film.RegistaNome,
                Cast = film.Cast,
                Featured = film.Featured,
                DataRilascio = film.DataRilascio,
                Genere = film.Genere,
                CategorieIds = film.FilmsCategorie.Select(fc => fc.CategoriaId).ToList(),
                Categorie = film.FilmsCategorie.Select(fc => new CategoriaDTO
                {
                    Id = fc.CategoriaId,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
            });
        });

        // DELETE /films/{id} - Admin e PowerUser
        group.MapDelete("/{id}", [Authorize(Roles = "Admin,PowerUser")] async (int id, FilmDbContext db) =>
        {
            var film = await db.Films.FindAsync(id);
            if (film is null) return Results.NotFound();

            db.Films.Remove(film);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }

    private sealed class FilmSummaryItem
    {
        public int Id { get; init; }
        public int? TmdbId { get; init; }
        public string Titolo { get; init; } = string.Empty;
        public DateTime DataProduzione { get; init; }
        public DateTime? DataRilascio { get; init; }
        public int RegistaId { get; init; }
        public string? RegistaNome { get; init; }
        public int Durata { get; init; }
        public string? CopertinaPath { get; init; }
        public bool Featured { get; init; }
        public string? Genere { get; init; }
    }

    private static List<T> DeduplicateByTitle<T>(
        IEnumerable<T> items,
        Func<T, string?> titleSelector,
        Func<T, DateTime?> dateSelector,
        Func<T, int> idSelector)
    {
        // Algoritmo:
        // - costruisce chiave titolo normalizzato
        // - se collisione, tiene record "migliore" (data più recente, poi id più alto)
        // - restituisce lista già ordinata per recency
        var map = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var key = NormalizeFilmTitleKey(titleSelector(item));
            if (string.IsNullOrWhiteSpace(key))
            {
                key = $"id:{idSelector(item)}";
            }

            if (!map.TryGetValue(key, out var existing))
            {
                map[key] = item;
                continue;
            }

            var existingDate = dateSelector(existing) ?? DateTime.MinValue;
            var candidateDate = dateSelector(item) ?? DateTime.MinValue;
            if (candidateDate > existingDate || (candidateDate == existingDate && idSelector(item) > idSelector(existing)))
            {
                map[key] = item;
            }
        }

        return map.Values
            .OrderByDescending(x => dateSelector(x) ?? DateTime.MinValue)
            .ThenByDescending(idSelector)
            .ToList();
    }

    // DOC-METHOD: 'NormalizeFilmTitleKey' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string NormalizeFilmTitleKey(string? value)
    {
        // Rende confrontabili titoli con varianti grafiche:
        // accenti, punteggiatura e whitespace non devono creare falsi "film diversi".
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        Span<char> buffer = stackalloc char[normalized.Length];
        var idx = 0;
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                buffer[idx++] = char.ToLowerInvariant(ch);
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                buffer[idx++] = ' ';
            }
        }

        return string.Join(' ', new string(buffer[..idx]).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // DOC-METHOD: 'NormalizeMediaUrl' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string NormalizeMediaUrl(string? path, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path ?? string.Empty;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            return path;
        }

        var backendBaseUrl =
            Environment.GetEnvironmentVariable("EXTERNAL_AUTH_BACKEND_BASE_URL") ??
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}";

        return $"{backendBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    // DOC-METHOD: 'GetOrCreateRegistaAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static async Task<Regista> GetOrCreateRegistaAsync(FilmDbContext db, string? fullName, string? nazionalita)
    {
        var (nome, cognome) = SplitDirectorName(fullName);

        var existing = await db.Registi.FirstOrDefaultAsync(r =>
            r.Nome.ToLower() == nome.ToLower() &&
            r.Cognome.ToLower() == cognome.ToLower());

        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(nazionalita) &&
                (string.IsNullOrWhiteSpace(existing.Nazionalita) || existing.Nazionalita == "Unknown"))
            {
                existing.Nazionalita = nazionalita;
                await db.SaveChangesAsync();
            }
            return existing;
        }

        var regista = new Regista
        {
            Nome = nome,
            Cognome = cognome,
            Nazionalita = string.IsNullOrWhiteSpace(nazionalita) ? "Unknown" : nazionalita
        };

        db.Registi.Add(regista);
        await db.SaveChangesAsync();
        return regista;
    }

    // DOC-METHOD: 'ResolveDirectorNationalityAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static async Task<string?> ResolveDirectorNationalityAsync(ITMDBService tmdbService, int directorId)
    {
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
                return nationality.Length > 100 ? nationality[..100] : nationality;
            }
        }

        return null;
    }

    // DOC-METHOD: 'NormalizeNationalityFromPlace' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
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

    // DOC-METHOD: 'BuildCastTextFromTmdb' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string? BuildCastTextFromTmdb(JsonElement castArr)
    {
        var castList = castArr.EnumerateArray()
            .Select(member => new
            {
                Name = member.TryGetProperty("name", out var n) ? n.GetString() : null,
                Order = member.TryGetProperty("order", out var o) && o.TryGetInt32(out var order) ? order : int.MaxValue
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.Equals(x.Name, "Unknown", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Order)
            .Select(x => x.Name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        if (castList.Count == 0)
            return null;

        var cast = string.Join(", ", castList);
        return cast.Length > 1000 ? cast[..1000] : cast;
    }

    // DOC-METHOD: 'ExtractCastNames' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string[] ExtractCastNames(JsonElement castArr)
    {
        return castArr.EnumerateArray()
            .Select(member => new
            {
                Name = member.TryGetProperty("name", out var n) ? n.GetString() : null,
                Order = member.TryGetProperty("order", out var o) && o.TryGetInt32(out var order) ? order : int.MaxValue
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .OrderBy(x => x.Order)
            .Select(x => x.Name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
    }

    // DOC-METHOD: 'ResolveTmdbIdForFilmAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static async Task<int?> ResolveTmdbIdForFilmAsync(Film film, ITMDBService tmdbService)
    {
        if (film.TmdbId.HasValue)
        {
            return film.TmdbId.Value;
        }

        var searchJson = await tmdbService.SearchMovieAsync(film.Titolo);
        if (string.IsNullOrWhiteSpace(searchJson))
        {
            return null;
        }

        using var searchDoc = JsonDocument.Parse(searchJson);
        if (!searchDoc.RootElement.TryGetProperty("results", out var results))
        {
            return null;
        }

        int? matchedId = null;
        var filmYear = film.DataProduzione.Year;

        foreach (var result in results.EnumerateArray().Take(10))
        {
            if (!result.TryGetProperty("id", out var idProp) || !idProp.TryGetInt32(out var resultId))
                continue;

            var resultTitle = result.TryGetProperty("title", out var t) ? t.GetString() : string.Empty;
            if (!string.Equals(resultTitle?.Trim(), film.Titolo.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var release = result.TryGetProperty("release_date", out var rd) ? rd.GetString() : null;
            if (!string.IsNullOrWhiteSpace(release) && DateTime.TryParse(release, out var rdParsed))
            {
                if (rdParsed.Year == filmYear)
                {
                    matchedId = resultId;
                    break;
                }
            }

            matchedId ??= resultId;
        }

        return matchedId;
    }
}

