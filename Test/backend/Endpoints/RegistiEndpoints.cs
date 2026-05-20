// DOC: Endpoint 'RegistiEndpoints': espone API HTTP e coordina validazione input, accesso dati e risposta.
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class RegistiEndpoints
{
    // DOC-METHOD: 'MapRegistiEndpoints' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static RouteGroupBuilder MapRegistiEndpoints(this RouteGroupBuilder group)
    {
        // GET /registi - Visibile a tutti
        group.MapGet("/", async (FilmDbContext db) =>
        {
            var items = await db.Registi
                .AsNoTracking()
                .Select(r => new
                {
                    r.Id,
                    r.Nome,
                    r.Cognome,
                    r.Nazionalita,
                    FilmsCount = r.Films.Count
                })
                .ToListAsync();

            return DeduplicateRegisti(items.Select(r => new RegistaListItem(
                r.Id,
                r.Nome,
                r.Cognome,
                NormalizeNationality(r.Nazionalita),
                r.FilmsCount)));
        });

        // GET /registi/{id} - Visibile a tutti
        group.MapGet("/{id}", async (int id, FilmDbContext db) =>
        {
            var regista = await db.Registi.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            return regista is null ? Results.NotFound() : Results.Ok(new RegistaDTO
            {
                Id = regista.Id,
                Nome = regista.Nome,
                Cognome = regista.Cognome,
                Nazionalita = NormalizeNationality(regista.Nazionalita)
            });
        });

        // POST /registi - Admin e PowerUser
        group.MapPost("/", [Authorize(Roles = "Admin,PowerUser")] async (RegistaCreateDTO dto, FilmDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
                return Results.BadRequest("Nome and Cognome are required");

            var regista = new Regista
            {
                Nome = dto.Nome,
                Cognome = dto.Cognome,
                Nazionalita = NormalizeNationality(dto.Nazionalita)
            };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();
            return Results.Created($"/registi/{regista.Id}", new RegistaDTO
            {
                Id = regista.Id,
                Nome = regista.Nome,
                Cognome = regista.Cognome,
                Nazionalita = NormalizeNationality(regista.Nazionalita)
            });
        });

        // PUT /registi/{id} - Admin e PowerUser
        group.MapPut("/{id}", [Authorize(Roles = "Admin,PowerUser")] async (int id, RegistaUpdateDTO dto, FilmDbContext db) =>
        {
            var regista = await db.Registi.FindAsync(id);
            if (regista is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cognome))
                return Results.BadRequest("Nome and Cognome are required");

            regista.Nome = dto.Nome;
            regista.Cognome = dto.Cognome;
            regista.Nazionalita = NormalizeNationality(dto.Nazionalita);

            await db.SaveChangesAsync();
            return Results.Ok(new RegistaDTO
            {
                Id = regista.Id,
                Nome = regista.Nome,
                Cognome = regista.Cognome,
                Nazionalita = NormalizeNationality(regista.Nazionalita)
            });
        });

        // DELETE /registi/{id} - Admin e PowerUser
        group.MapDelete("/{id}", [Authorize(Roles = "Admin,PowerUser")] async (int id, FilmDbContext db) =>
        {
            var regista = await db.Registi.FindAsync(id);
            if (regista is null) return Results.NotFound();

            db.Registi.Remove(regista);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }

    private sealed record RegistaListItem(int Id, string Nome, string Cognome, string? Nazionalita, int FilmsCount);

    private static List<RegistaDTO> DeduplicateRegisti(IEnumerable<RegistaListItem> registi)
    {
        var byName = new Dictionary<string, RegistaListItem>(StringComparer.Ordinal);

        foreach (var regista in registi)
        {
            var key = NormalizePersonKey(regista.Nome, regista.Cognome);
            if (string.IsNullOrWhiteSpace(key))
            {
                key = $"id:{regista.Id}";
            }

            if (!byName.TryGetValue(key, out var existing))
            {
                byName[key] = regista;
                continue;
            }

            var existingHasNation = !string.IsNullOrWhiteSpace(existing.Nazionalita) && existing.Nazionalita != "Sconosciuta";
            var candidateHasNation = !string.IsNullOrWhiteSpace(regista.Nazionalita) && regista.Nazionalita != "Sconosciuta";
            if (regista.FilmsCount > existing.FilmsCount ||
                (regista.FilmsCount == existing.FilmsCount && candidateHasNation && !existingHasNation) ||
                (regista.FilmsCount == existing.FilmsCount && candidateHasNation == existingHasNation && regista.Id > existing.Id))
            {
                byName[key] = regista;
            }
        }

        return byName.Values
            .OrderBy(r => r.Cognome)
            .ThenBy(r => r.Nome)
            .Select(r => new RegistaDTO
            {
                Id = r.Id,
                Nome = r.Nome,
                Cognome = r.Cognome,
                Nazionalita = r.Nazionalita
            })
            .ToList();
    }

    private static string NormalizePersonKey(string? nome, string? cognome)
    {
        var value = $"{nome} {cognome}".Normalize(NormalizationForm.FormD);
        Span<char> buffer = stackalloc char[value.Length];
        var idx = 0;

        foreach (var ch in value)
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

    private static string? NormalizeNationality(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var key = NormalizeNationalityKey(trimmed);
        var normalized = key switch
        {
            "usa" => "Stati Uniti",
            "us" => "Stati Uniti",
            "u s a" => "Stati Uniti",
            "united states" => "Stati Uniti",
            "united states of america" => "Stati Uniti",
            "america" => "Stati Uniti",
            "uk" => "Regno Unito",
            "u k" => "Regno Unito",
            "united kingdom" => "Regno Unito",
            "great britain" => "Regno Unito",
            "england" => "Regno Unito",
            "scotland" => "Regno Unito",
            "wales" => "Regno Unito",
            "japan" => "Giappone",
            "mexico" => "Messico",
            "south korea" => "Corea del Sud",
            "korea" => "Corea del Sud",
            "republic of korea" => "Corea del Sud",
            "russia" => "Russia",
            "tailandese" => "Thailandia",
            "thailand" => "Thailandia",
            "thailandia" => "Thailandia",
            "unknown" => "Sconosciuta",
            _ => trimmed
        };

        return normalized.Length > 100 ? normalized[..100] : normalized;
    }

    private static string NormalizeNationalityKey(string value)
    {
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
            else if ((char.IsWhiteSpace(ch) || ch is '.' or '-' or '_') && idx > 0 && buffer[idx - 1] != ' ')
            {
                buffer[idx++] = ' ';
            }
        }

        return new string(buffer[..idx]).Trim();
    }
}

