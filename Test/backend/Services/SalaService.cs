using System.Text.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class SalaService(FilmDbContext context) : ISalaService
{
    public async Task<IEnumerable<SalaDTO>> GetSaleByCinemaAsync(int cinemaId)
    {
        return await context.Sale
            .Where(s => s.CinemaId == cinemaId)
            .OrderBy(s => s.NumeroSala)
            .Select(s => new SalaDTO
            {
                Id = s.Id,
                CinemaId = s.CinemaId,
                NumeroSala = s.NumeroSala,
                Nome = s.Nome,
                Tipologia = s.Tipologia.ToString(),
                NumeroFile = s.NumeroFile,
                PostiPerFila = s.PostiPerFila,
                PostiTotali = s.PostiTotali,
                ConfigurazionePosti = s.ConfigurazionePosti,
                Attiva = s.Attiva
            })
            .ToListAsync();
    }

    public async Task<SalaDTO?> GetSalaAsync(int id)
    {
        return await context.Sale.Where(s => s.Id == id).Select(s => new SalaDTO
        {
            Id = s.Id,
            CinemaId = s.CinemaId,
            NumeroSala = s.NumeroSala,
            Nome = s.Nome,
            Tipologia = s.Tipologia.ToString(),
            NumeroFile = s.NumeroFile,
            PostiPerFila = s.PostiPerFila,
            PostiTotali = s.PostiTotali,
            ConfigurazionePosti = s.ConfigurazionePosti,
            Attiva = s.Attiva
        }).FirstOrDefaultAsync();
    }

    public async Task<SalaDTO> CreateSalaAsync(int cinemaId, SalaCreateDTO dto)
    {
        var sala = new Sala
        {
            CinemaId = cinemaId,
            NumeroSala = dto.NumeroSala,
            Nome = dto.Nome,
            Tipologia = (TipologiaSala)dto.Tipologia,
            NumeroFile = dto.NumeroFile,
            PostiPerFila = dto.PostiPerFila,
            ConfigurazionePosti = dto.ConfigurazionePosti,
            PostiTotali = CalcolaPostiTotali(dto.NumeroFile, dto.PostiPerFila, dto.ConfigurazionePosti),
            Attiva = true
        };

        context.Sale.Add(sala);
        await context.SaveChangesAsync();
        return ToDtoCompiled(sala);
    }

    public async Task<SalaDTO?> UpdateSalaAsync(int id, SalaUpdateDTO dto)
    {
        var sala = await context.Sale.FindAsync(id);
        if (sala is null) return null;

        sala.Nome = dto.Nome;
        sala.Tipologia = (TipologiaSala)dto.Tipologia;
        sala.NumeroFile = dto.NumeroFile;
        sala.PostiPerFila = dto.PostiPerFila;
        sala.ConfigurazionePosti = dto.ConfigurazionePosti;
        sala.PostiTotali = CalcolaPostiTotali(dto.NumeroFile, dto.PostiPerFila, dto.ConfigurazionePosti);
        sala.Attiva = dto.Attiva;

        await context.SaveChangesAsync();
        return ToDtoCompiled(sala);
    }

    public async Task<bool> DeleteSalaAsync(int id)
    {
        var sala = await context.Sale.FindAsync(id);
        if (sala is null) return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var hasFutureShows = await context.Shows.AnyAsync(s => s.SalaId == id && (s.Data > today || (s.Data == today && s.OraInizio >= now)));
        if (hasFutureShows) return false;

        context.Sale.Remove(sala);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<PiantinaDTO?> GetPiantinaAsync(int salaId)
    {
        var sala = await context.Sale.FindAsync(salaId);
        if (sala is null) return null;

        var file = BuildRows(sala.NumeroFile, sala.PostiPerFila, sala.ConfigurazionePosti);
        return new PiantinaDTO
        {
            SalaId = salaId,
            NumeroFile = sala.NumeroFile,
            File = file.Select(x => new PiantinaFilaDTO { Fila = x.fila, Posti = x.posti }).ToList()
        };
    }

    public async Task<bool> UpdatePiantinaAsync(int salaId, PiantinaUpdateDTO dto)
    {
        var sala = await context.Sale.FindAsync(salaId);
        if (sala is null) return false;

        sala.NumeroFile = dto.NumeroFile;
        sala.PostiPerFila = dto.PostiPerFila;
        sala.ConfigurazionePosti = dto.ConfigurazionePosti;
        sala.PostiTotali = CalcolaPostiTotali(dto.NumeroFile, dto.PostiPerFila, dto.ConfigurazionePosti);

        await context.SaveChangesAsync();
        return true;
    }

    public Task<bool> ValidateConfigurazioneAsync(string configurazioneJson)
    {
        if (string.IsNullOrWhiteSpace(configurazioneJson)) return Task.FromResult(true);
        try
        {
            using var doc = JsonDocument.Parse(configurazioneJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("file", out var rows) || rows.ValueKind != JsonValueKind.Array) return Task.FromResult(false);
            foreach (var row in rows.EnumerateArray())
            {
                if (!row.TryGetProperty("fila", out _) || !row.TryGetProperty("posti", out _)) return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static int CalcolaPostiTotali(int numeroFile, int? postiPerFila, string? configurazione)
    {
        if (!string.IsNullOrWhiteSpace(configurazione))
        {
            try
            {
                return BuildRows(numeroFile, postiPerFila, configurazione).Sum(x => x.posti);
            }
            catch
            {
            }
        }

        return numeroFile * (postiPerFila ?? 10);
    }

    private static List<(int fila, int posti)> BuildRows(int numeroFile, int? postiPerFila, string? configurazione)
    {
        if (!string.IsNullOrWhiteSpace(configurazione))
        {
            using var doc = JsonDocument.Parse(configurazione);
            if (doc.RootElement.TryGetProperty("file", out var rows) && rows.ValueKind == JsonValueKind.Array)
            {
                var result = new List<(int fila, int posti)>();
                foreach (var row in rows.EnumerateArray())
                {
                    var fila = row.GetProperty("fila").GetInt32();
                    var posti = row.GetProperty("posti").GetInt32();
                    result.Add((fila, posti));
                }
                if (result.Count > 0) return result;
            }
        }

        var perFila = postiPerFila ?? 10;
        return Enumerable.Range(1, numeroFile).Select(i => (i, perFila)).ToList();
    }

    private static readonly Func<Sala, SalaDTO> ToDto = s => new SalaDTO
    {
        Id = s.Id,
        CinemaId = s.CinemaId,
        NumeroSala = s.NumeroSala,
        Nome = s.Nome,
        Tipologia = s.Tipologia.ToString(),
        NumeroFile = s.NumeroFile,
        PostiPerFila = s.PostiPerFila,
        PostiTotali = s.PostiTotali,
        ConfigurazionePosti = s.ConfigurazionePosti,
        Attiva = s.Attiva
    };

    private static SalaDTO ToDtoCompiled(Sala s) => ToDto(s);
}
