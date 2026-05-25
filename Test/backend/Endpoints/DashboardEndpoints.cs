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
        app.MapGet("/dashboard/overview", async (FilmDbContext db, IMemoryCache cache, HttpContext httpContext, int? revenueDays, string? revenueRange) =>
        {
            var range = ResolveRevenueRange(today: DateTime.Now.Date, revenueRange, revenueDays);
            var cacheKey = $"dashboard:overview:v4:{range.Key}";
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

            var revenueStart = range.Start;
            var revenueEnd = range.End;

            var paidPurchases = await db.Acquisti
                .AsNoTracking()
                .Where(a =>
                    a.Stato == Model.StatoAcquisto.PAGATO &&
                    a.DataAcquisto >= revenueStart &&
                    a.DataAcquisto < revenueEnd)
                .Select(a => new
                {
                    a.Id,
                    a.ShowId,
                    a.DataAcquisto,
                    a.ImportoTotale,
                    CinemaId = a.Show.Sala!.CinemaId,
                    CinemaNome = a.Show.Sala.Cinema != null ? a.Show.Sala.Cinema.Nome : "N/D",
                    SalaId = a.Show.SalaId,
                    SalaLabel = $"Sala {a.Show.Sala.NumeroSala} - {a.Show.Sala.Tipologia}"
                })
                .ToListAsync();

            var revenueSeries = range.Buckets.Select(bucket =>
            {
                var amount = paidPurchases
                    .Where(a => a.DataAcquisto >= bucket.Start && a.DataAcquisto < bucket.End)
                    .Sum(a => a.ImportoTotale);

                return new
                {
                    date = bucket.Start.ToString("yyyy-MM-dd"),
                    label = bucket.Label,
                    amount = decimal.Round(amount, 2)
                };
            }).ToList();

            var currentWeekTotal = decimal.Round(revenueSeries.Sum(x => x.amount), 2);
            var previousWeekStart = revenueStart.AddDays(-range.LengthDays);
            var previousWeekEnd = revenueStart;
            var previousWeekTotal = decimal.Round(await db.Acquisti
                .AsNoTracking()
                .Where(a =>
                    a.Stato == Model.StatoAcquisto.PAGATO &&
                    a.DataAcquisto >= previousWeekStart &&
                    a.DataAcquisto < previousWeekEnd)
                .SumAsync(a => (decimal?)a.ImportoTotale) ?? 0m, 2);
            var delta = currentWeekTotal - previousWeekTotal;
            var deltaPercent = previousWeekTotal == 0m
                ? (currentWeekTotal > 0m ? 100m : 0m)
                : decimal.Round((delta / previousWeekTotal) * 100m, 2);

            var currentPurchaseIds = paidPurchases.Select(p => p.Id).ToList();
            var ticketCount = await db.Biglietti
                .AsNoTracking()
                .CountAsync(b => currentPurchaseIds.Contains(b.AcquistoId));
            var paidProjectionCount = paidPurchases.Select(p => p.ShowId).Distinct().Count();
            var ticketsPerPurchase = paidPurchases.Count == 0
                ? 0m
                : decimal.Round((decimal)ticketCount / paidPurchases.Count, 2);
            var revenuePerProjection = paidProjectionCount == 0
                ? 0m
                : decimal.Round(currentWeekTotal / paidProjectionCount, 2);

            var topCinemas = paidPurchases
                .GroupBy(p => new { p.CinemaId, p.CinemaNome })
                .Select(g => new
                {
                    cinemaId = g.Key.CinemaId,
                    nome = g.Key.CinemaNome,
                    revenue = decimal.Round(g.Sum(x => x.ImportoTotale), 2),
                    purchases = g.Count()
                })
                .OrderByDescending(x => x.revenue)
                .ThenBy(x => x.nome)
                .Take(5)
                .ToList();

            var topSale = paidPurchases
                .GroupBy(p => new { p.SalaId, p.SalaLabel, p.CinemaNome })
                .Select(g => new
                {
                    salaId = g.Key.SalaId,
                    label = g.Key.SalaLabel,
                    cinema = g.Key.CinemaNome,
                    revenue = decimal.Round(g.Sum(x => x.ImportoTotale), 2),
                    purchases = g.Count()
                })
                .OrderByDescending(x => x.revenue)
                .ThenBy(x => x.label)
                .Take(5)
                .ToList();

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
                    range = range.Key,
                    label = range.Label,
                    days = range.LengthDays,
                    series = revenueSeries,
                    currentWeekTotal,
                    previousWeekTotal,
                    delta,
                    deltaPercent,
                    kpis = new
                    {
                        purchases = paidPurchases.Count,
                        tickets = ticketCount,
                        ticketsPerPurchase,
                        revenuePerProjection,
                        paidProjectionCount
                    },
                    topCinemas,
                    topSale
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
            cache.Remove("dashboard:overview:v4:7d");
            cache.Remove("dashboard:overview:v4:30d");
            cache.Remove("dashboard:overview:v4:month");
            cache.Remove("dashboard:overview:v4:year");
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

    public static void InvalidateDashboardCache(IMemoryCache cache)
    {
        cache.Remove("dashboard:overview:v2");
        cache.Remove("dashboard:overview:v3:7");
        cache.Remove("dashboard:overview:v3:30");
        cache.Remove("dashboard:overview:v4:7d");
        cache.Remove("dashboard:overview:v4:30d");
        cache.Remove("dashboard:overview:v4:month");
        cache.Remove("dashboard:overview:v4:year");
    }

    private static RevenueRange ResolveRevenueRange(DateTime today, string? revenueRange, int? revenueDays)
    {
        var key = (revenueRange ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key))
        {
            key = revenueDays == 30 ? "30d" : "7d";
        }

        return key switch
        {
            "30" or "30d" or "30days" => BuildDailyRange("30d", "Ultimi 30 giorni", today.AddDays(-29), today.AddDays(1)),
            "month" or "mese" => BuildDailyRange("month", "Mese corrente", new DateTime(today.Year, today.Month, 1), today.AddDays(1)),
            "year" or "anno" => BuildMonthlyRange(today),
            _ => BuildDailyRange("7d", "Ultimi 7 giorni", today.AddDays(-6), today.AddDays(1))
        };
    }

    private static RevenueRange BuildDailyRange(string key, string label, DateTime start, DateTime end)
    {
        var culture = CultureInfo.GetCultureInfo("it-IT");
        var buckets = new List<RevenueBucket>();
        for (var day = start.Date; day < end.Date; day = day.AddDays(1))
        {
            buckets.Add(new RevenueBucket(day, day.AddDays(1), day.ToString("ddd d", culture)));
        }

        return new RevenueRange(key, label, start.Date, end.Date, Math.Max(1, (end.Date - start.Date).Days), buckets);
    }

    private static RevenueRange BuildMonthlyRange(DateTime today)
    {
        var culture = CultureInfo.GetCultureInfo("it-IT");
        var start = new DateTime(today.Year, 1, 1);
        var end = today.AddDays(1);
        var buckets = Enumerable.Range(1, today.Month)
            .Select(month =>
            {
                var bucketStart = new DateTime(today.Year, month, 1);
                var bucketEnd = month == 12 ? new DateTime(today.Year + 1, 1, 1) : new DateTime(today.Year, month + 1, 1);
                if (bucketEnd > end) bucketEnd = end;
                return new RevenueBucket(bucketStart, bucketEnd, bucketStart.ToString("MMM", culture));
            })
            .ToList();

        return new RevenueRange("year", "Anno corrente", start, end, Math.Max(1, (end.Date - start.Date).Days), buckets);
    }

    private sealed record RevenueBucket(DateTime Start, DateTime End, string Label);

    private sealed record RevenueRange(string Key, string Label, DateTime Start, DateTime End, int LengthDays, List<RevenueBucket> Buckets);

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


