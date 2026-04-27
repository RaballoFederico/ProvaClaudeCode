using System.Text.Json;
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

    public static RouteGroupBuilder MapFilmsEndpoints(this RouteGroupBuilder group)
    {
        // GET /films - Visibile a tutti (include categorie)
        group.MapGet("/", async (FilmDbContext db) =>
        await db.Films
            .Include(f => f.FilmsCategorie)
            .ThenInclude(fc => fc.Categoria)
            .Select(f => new FilmDTO
            {
                Id = f.Id,
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
            }).ToListAsync());

        // GET /films/{id} - Visibile a tutti (include categorie)
        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var film = await db.Films
                .Include(f => f.FilmsCategorie)
                .ThenInclude(fc => fc.Categoria)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (film is null) return Results.NotFound();

            return Results.Ok(new FilmDTO
            {
                Id = film.Id,
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
                    Id = fc.Categoria.Id,
                    Nome = fc.Categoria.Nome,
                    Descrizione = fc.Categoria.Descrizione
                }).ToList()
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
                    return Results.BadRequest("Una o più categorie non esistono");
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
                Genere = genere
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
                    return Results.BadRequest("Una o più categorie non esistono");
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
