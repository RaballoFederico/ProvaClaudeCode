using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FilmAPI.Endpoints;

public static class CinemasEndpoints
{
    public static RouteGroupBuilder MapCinemasEndpoints(this RouteGroupBuilder group)
    {
        // GET /cinemas - Visibile a tutti
        group.MapGet("/", async (FilmDbContext db) =>
        await db.Cinemas
            .AsNoTracking()
            .Select(c => new CinemaDTO
        {
            Id = c.Id,
            Nome = c.Nome,
            Indirizzo = c.Indirizzo,
            Citta = c.Citta,
            PostiMassimi = c.PostiMassimi,
            Latitudine = c.Latitudine,
            Longitudine = c.Longitudine,
            CodiceLocale = c.CodiceLocale,
            ImageUrl = c.ImageUrl
        }).ToListAsync());

        // GET /cinemas/{id} - Visibile a tutti
        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var cinema = await db.Cinemas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return cinema is null ? Results.NotFound() : Results.Ok(new CinemaDTO
            {
                Id = cinema.Id,
                Nome = cinema.Nome,
                Indirizzo = cinema.Indirizzo,
                Citta = cinema.Citta,
                PostiMassimi = cinema.PostiMassimi,
                Latitudine = cinema.Latitudine,
                Longitudine = cinema.Longitudine,
                CodiceLocale = cinema.CodiceLocale,
                ImageUrl = cinema.ImageUrl
            });
        });

        // POST /cinemas - Solo Admin (PowerUser può solo leggere)
        group.MapPost("/", [Authorize(Roles = "Admin")] async (CinemaCreateDTO dto, FilmDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
        {
            var cinema = new Cinema
            {
                Nome = dto.Nome,
                Indirizzo = dto.Indirizzo,
                Citta = dto.Citta,
                PostiMassimi = dto.PostiMassimi > 0 ? dto.PostiMassimi : 120,
                Latitudine = dto.Latitudine,
                Longitudine = dto.Longitudine,
                CodiceLocale = dto.CodiceLocale,
                ImageUrl = await ResolveCinemaImageUrlAsync(dto.ImageUrl, dto.Nome, dto.Citta, httpClientFactory, configuration)
            };
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();
            return Results.Created($"/cinemas/{cinema.Id}", new CinemaDTO
            {
                Id = cinema.Id,
                Nome = cinema.Nome,
                Indirizzo = cinema.Indirizzo,
                Citta = cinema.Citta,
                PostiMassimi = cinema.PostiMassimi,
                Latitudine = cinema.Latitudine,
                Longitudine = cinema.Longitudine,
                CodiceLocale = cinema.CodiceLocale,
                ImageUrl = cinema.ImageUrl
            });
        });

        // PUT /cinemas/{id} - Solo Admin
        group.MapPut("/{id}", [Authorize(Roles = "Admin")] async (int id, CinemaUpdateDTO dto, FilmDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
        {
            var cinema = await db.Cinemas.FindAsync(id);
            if (cinema is null) return Results.NotFound();

            cinema.Nome = dto.Nome;
            cinema.Indirizzo = dto.Indirizzo;
            cinema.Citta = dto.Citta;
            cinema.PostiMassimi = dto.PostiMassimi > 0 ? dto.PostiMassimi : cinema.PostiMassimi;
            cinema.Latitudine = dto.Latitudine;
            cinema.Longitudine = dto.Longitudine;
            cinema.CodiceLocale = dto.CodiceLocale;
            cinema.ImageUrl = await ResolveCinemaImageUrlAsync(dto.ImageUrl, dto.Nome, dto.Citta, httpClientFactory, configuration);

            await db.SaveChangesAsync();
            return Results.Ok(new CinemaDTO
            {
                Id = cinema.Id,
                Nome = cinema.Nome,
                Indirizzo = cinema.Indirizzo,
                Citta = cinema.Citta,
                PostiMassimi = cinema.PostiMassimi,
                Latitudine = cinema.Latitudine,
                Longitudine = cinema.Longitudine,
                CodiceLocale = cinema.CodiceLocale,
                ImageUrl = cinema.ImageUrl
            });
        });

        // DELETE /cinemas/{id} - Solo Admin
        group.MapDelete("/{id}", [Authorize(Roles = "Admin")] async (int id, FilmDbContext db) =>
        {
            var cinema = await db.Cinemas.FindAsync(id);
            if (cinema is null) return Results.NotFound();

            db.Cinemas.Remove(cinema);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }

    private static async Task<string> ResolveCinemaImageUrlAsync(
        string? providedUrl,
        string? nomeCinema,
        string? citta,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        var trimmed = (providedUrl ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        var googleApiKey = (Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")
            ?? configuration["GoogleMaps:ApiKey"]
            ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(googleApiKey))
        {
            var fromGoogle = await TryResolveCinemaImageFromGooglePlacesAsync(nomeCinema, citta, googleApiKey, httpClientFactory);
            if (!string.IsNullOrWhiteSpace(fromGoogle))
            {
                return fromGoogle!;
            }
        }

        var seedBase = $"{nomeCinema ?? "cinema"}-{citta ?? "italia"}".ToLowerInvariant();
        var safeSeed = Regex.Replace(seedBase, "[^a-z0-9-]", "-");
        return $"https://picsum.photos/seed/{safeSeed}/1200/800";
    }

    private static async Task<string?> TryResolveCinemaImageFromGooglePlacesAsync(
        string? nomeCinema,
        string? citta,
        string apiKey,
        IHttpClientFactory httpClientFactory)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(8);

            var query = $"{nomeCinema} {citta} cinema".Trim();
            var payload = JsonSerializer.Serialize(new
            {
                textQuery = query,
                languageCode = "it"
            });

            using var searchRequest = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:searchText");
            searchRequest.Headers.Add("X-Goog-Api-Key", apiKey);
            searchRequest.Headers.Add("X-Goog-FieldMask", "places.id,places.photos");
            searchRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var searchResponse = await client.SendAsync(searchRequest);
            if (!searchResponse.IsSuccessStatusCode)
            {
                return null;
            }

            using var searchDoc = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
            if (!searchDoc.RootElement.TryGetProperty("places", out var placesEl) || placesEl.ValueKind != JsonValueKind.Array || placesEl.GetArrayLength() == 0)
            {
                return null;
            }

            var firstPlace = placesEl[0];
            if (!firstPlace.TryGetProperty("photos", out var photosEl) || photosEl.ValueKind != JsonValueKind.Array || photosEl.GetArrayLength() == 0)
            {
                return null;
            }

            var firstPhoto = photosEl[0];
            if (!firstPhoto.TryGetProperty("name", out var nameEl))
            {
                return null;
            }

            var photoName = nameEl.GetString();
            if (string.IsNullOrWhiteSpace(photoName))
            {
                return null;
            }

            var mediaUrl = $"https://places.googleapis.com/v1/{photoName}/media?maxWidthPx=1200&skipHttpRedirect=true";
            using var photoRequest = new HttpRequestMessage(HttpMethod.Get, mediaUrl);
            photoRequest.Headers.Add("X-Goog-Api-Key", apiKey);
            photoRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var photoResponse = await client.SendAsync(photoRequest);
            if (!photoResponse.IsSuccessStatusCode)
            {
                return null;
            }

            using var photoDoc = JsonDocument.Parse(await photoResponse.Content.ReadAsStringAsync());
            if (!photoDoc.RootElement.TryGetProperty("photoUri", out var photoUriEl))
            {
                return null;
            }

            return photoUriEl.GetString();
        }
        catch
        {
            return null;
        }
    }
}
