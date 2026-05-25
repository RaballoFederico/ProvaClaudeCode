// DOC: DashboardEndpoints - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Endpoint 'DashboardEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Data;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class DashboardEndpoints
{
    // DOC-METHOD: 'MapDashboardEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard/overview", async (FilmDbContext db, IMemoryCache cache, HttpContext httpContext, int? revenueDays) =>
        {
            var normalizedRevenueDays = Math.Clamp(revenueDays ?? 7, 7, 30);
            var cacheKey = $"dashboard:overview:v3:{normalizedRevenueDays}";
            if (cache.TryGetValue(cacheKey, out object? cached) && cached is not null)
            {
                return Results.Ok(cached);
            }

            var nowLocal = DateTime.Now;
            var today = nowLocal.Date;
            var nowTime = nowLocal.TimeOfDay;

            var filmsCount = await db.Films.AsNoTracking().CountAsync();
            var registiCount = await db.Registi.AsNoTracking().CountAsync();
            var cinemasCount = await db.Cinemas.AsNoTracking().CountAsync();
            var proiezioniCount = await db.Proiezioni.AsNoTracking().CountAsync();

            var featuredFilms = await db.Films
                .AsNoTracking()
                .OrderByDescending(f => f.Featured)
                .ThenByDescending(f => f.DataRilascio ?? f.DataProduzione)
                .ThenByDescending(f => f.Id)
                .Take(24)
                .Select(f => new
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
                    f.DataProduzione,
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
                })
                .ToListAsync();

            var dedupedFeaturedFilms = DeduplicateByTitle(
                featuredFilms,
                item => item.Titolo,
                item => item.DataRilascio ?? item.DataProduzione,
                item => item.Id)
                .Take(6)
                .ToList();

            var normalizedFeaturedFilms = dedupedFeaturedFilms.Select(f => new
            {
                f.Id,
                f.Titolo,
                f.RegistaId,
                f.RegistaNome,
                f.Durata,
                CopertinaPath = NormalizeMediaUrl(f.CopertinaPath, httpContext),
                f.Featured,
                f.DataRilascio,
                f.DataProduzione,
                f.Genere,
                f.Categorie
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

            var revenueDayRange = Enumerable.Range(0, normalizedRevenueDays)
                .Select(offset => today.AddDays(-(normalizedRevenueDays - 1) + offset))
                .ToList();
            var revenueStart = revenueDayRange.First();
            var revenueEnd = today.AddDays(1);

            var paidPurchases = await db.Acquisti
                .AsNoTracking()
                .Where(a =>
                    a.Stato == Model.StatoAcquisto.PAGATO &&
                    a.DataAcquisto >= revenueStart &&
                    a.DataAcquisto < revenueEnd)
                .Select(a => new { a.DataAcquisto, a.ImportoTotale })
                .ToListAsync();

            var revenueSeries = revenueDayRange.Select(day =>
            {
                var amount = paidPurchases
                    .Where(a => a.DataAcquisto.Date == day.Date)
                    .Sum(a => a.ImportoTotale);

                return new
                {
                    date = day.ToString("yyyy-MM-dd"),
                    label = day.ToString("ddd", CultureInfo.GetCultureInfo("it-IT")),
                    amount = decimal.Round(amount, 2)
                };
            }).ToList();

            var currentWeekTotal = decimal.Round(revenueSeries.Sum(x => x.amount), 2);
            var previousWeekStart = revenueStart.AddDays(-normalizedRevenueDays);
            var previousWeekEnd = revenueStart;
            var previousWeekTotal = decimal.Round(await db.Acquisti
                .AsNoTracking()
                .Where(a =>
                    a.Stato == Model.StatoAcquisto.PAGATO &&
                    a.DataAcquisto >= previousWeekStart &&
                    a.DataAcquisto < previousWeekEnd)
                .SumAsync(a => (decimal?)a.ImportoTotale) ?? 0m, 2);
            var delta = currentWeekTotal - previousWeekTotal;

            var payload = new
            {
                stats = new
                {
                    films = filmsCount,
                    registi = registiCount,
                    cinemas = cinemasCount,
                    proiezioni = proiezioniCount
                },
                featuredFilms = normalizedFeaturedFilms,
                upcomingProjections,
                revenue = new
                {
                    days = normalizedRevenueDays,
                    series = revenueSeries,
                    currentWeekTotal,
                    previousWeekTotal,
                    delta
                }
            };

            cache.Set(cacheKey, payload, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20)
            });

            return Results.Ok(payload);
        });

        app.MapPost("/dashboard/cache/invalidate", [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,PowerUser")] (IMemoryCache cache) =>
        {
            cache.Remove("dashboard:overview:v2");
            cache.Remove("dashboard:overview:v3:7");
            cache.Remove("dashboard:overview:v3:30");
            return Results.Ok(new { message = "Dashboard cache invalidata" });
        });

        return app;
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

    private static List<T> DeduplicateByTitle<T>(
        IEnumerable<T> items,
        Func<T, string?> titleSelector,
        Func<T, DateTime?> dateSelector,
        Func<T, int> idSelector)
    {
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

    private static string NormalizeFilmTitleKey(string? value)
    {
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
            }
            else if (char.IsWhiteSpace(ch) && idx > 0 && buffer[idx - 1] != ' ')
            {
                buffer[idx++] = ' ';
            }
        }

        return new string(buffer[..idx]).Trim();
    }
}


