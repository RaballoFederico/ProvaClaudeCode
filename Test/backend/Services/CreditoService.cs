// DOC: CreditoService - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Service 'CreditoService': implementa logica di business e integrazioni esterne (DB/TMDB/Stripe).
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

// DOC-METHOD: 'CreditoService' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
public class CreditoService(FilmDbContext context) : ICreditoService
{
    // DOC-METHOD: 'GetSaldoAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<decimal> GetSaldoAsync(int utenteId)
    {
        var credito = await EnsureCreditoAsync(utenteId);
        return credito.Saldo;
    }

    // DOC-METHOD: 'RicaricaAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<TransazioneCreditoDTO> RicaricaAsync(int operatoreId, RicaricaCreditoDTO dto)
    {
        if (dto.Importo <= 0) throw new InvalidOperationException("Importo non valido");

        var credito = await EnsureCreditoAsync(dto.UtenteId);
        var saldoPre = credito.Saldo;
        credito.Saldo += dto.Importo;
        credito.DataUltimoAggiornamento = DateTime.UtcNow;

        var tx = new TransazioneCredito
        {
            UtenteId = dto.UtenteId,
            Tipo = TipoTransazione.RICARICA,
            Importo = dto.Importo,
            SaldoPrecedente = saldoPre,
            SaldoSuccessivo = credito.Saldo,
            DataTransazione = DateTime.UtcNow,
            OperatoreId = operatoreId,
            CinemaId = dto.CinemaId,
            Descrizione = dto.Descrizione
        };

        context.TransazioniCredito.Add(tx);
        await context.SaveChangesAsync();

        return ToDto(tx);
    }

    // DOC-METHOD: 'GetStoricoAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<IEnumerable<TransazioneCreditoDTO>> GetStoricoAsync(int utenteId)
    {
        return await context.TransazioniCredito
            .Where(t => t.UtenteId == utenteId)
            .OrderByDescending(t => t.DataTransazione)
            .Select(t => new TransazioneCreditoDTO
            {
                Id = t.Id,
                UtenteId = t.UtenteId,
                Tipo = t.Tipo.ToString(),
                Importo = t.Importo,
                SaldoPrecedente = t.SaldoPrecedente,
                SaldoSuccessivo = t.SaldoSuccessivo,
                DataTransazione = t.DataTransazione,
                OperatoreId = t.OperatoreId,
                CinemaId = t.CinemaId,
                Descrizione = t.Descrizione,
                AcquistoId = t.AcquistoId
            })
            .ToListAsync();
    }

    // DOC-METHOD: 'GetAllTransazioniAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<IEnumerable<TransazioneCreditoDTO>> GetAllTransazioniAsync(TransazioneFilterDTO? filter = null)
    {
        var q = context.TransazioniCredito.AsQueryable();
        if (filter is not null)
        {
            if (filter.UtenteId.HasValue) q = q.Where(t => t.UtenteId == filter.UtenteId.Value);
            if (filter.Tipo.HasValue) q = q.Where(t => (int)t.Tipo == filter.Tipo.Value);
            if (filter.Dal.HasValue) q = q.Where(t => t.DataTransazione >= filter.Dal.Value);
            if (filter.Al.HasValue) q = q.Where(t => t.DataTransazione <= filter.Al.Value);
            if (filter.CinemaId.HasValue) q = q.Where(t => t.CinemaId == filter.CinemaId.Value);
        }

        return await q.OrderByDescending(t => t.DataTransazione)
            .Select(t => new TransazioneCreditoDTO
            {
                Id = t.Id,
                UtenteId = t.UtenteId,
                Tipo = t.Tipo.ToString(),
                Importo = t.Importo,
                SaldoPrecedente = t.SaldoPrecedente,
                SaldoSuccessivo = t.SaldoSuccessivo,
                DataTransazione = t.DataTransazione,
                OperatoreId = t.OperatoreId,
                CinemaId = t.CinemaId,
                Descrizione = t.Descrizione,
                AcquistoId = t.AcquistoId
            })
            .ToListAsync();
    }

    // DOC-METHOD: 'ScalaCreditoAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<bool> ScalaCreditoAsync(int utenteId, decimal importo, int acquistoId)
    {
        if (importo <= 0) return true;
        var credito = await EnsureCreditoAsync(utenteId);
        if (credito.Saldo < importo) return false;

        var saldoPre = credito.Saldo;
        credito.Saldo -= importo;
        credito.DataUltimoAggiornamento = DateTime.UtcNow;

        context.TransazioniCredito.Add(new TransazioneCredito
        {
            UtenteId = utenteId,
            Tipo = TipoTransazione.ACQUISTO,
            Importo = -importo,
            SaldoPrecedente = saldoPre,
            SaldoSuccessivo = credito.Saldo,
            DataTransazione = DateTime.UtcNow,
            AcquistoId = acquistoId,
            Descrizione = "Utilizzo credito per acquisto"
        });

        await context.SaveChangesAsync();
        return true;
    }

    // DOC-METHOD: 'EnsureCreditoAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private async Task<CreditoUtente> EnsureCreditoAsync(int utenteId)
    {
        var credito = await context.CreditiUtente.FirstOrDefaultAsync(c => c.UtenteId == utenteId);
        if (credito is not null) return credito;

        credito = new CreditoUtente { UtenteId = utenteId, Saldo = 0m, DataUltimoAggiornamento = DateTime.UtcNow };
        context.CreditiUtente.Add(credito);
        await context.SaveChangesAsync();
        return credito;
    }

    // DOC-METHOD: 'ToDto' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static TransazioneCreditoDTO ToDto(TransazioneCredito t) => new()
    {
        Id = t.Id,
        UtenteId = t.UtenteId,
        Tipo = t.Tipo.ToString(),
        Importo = t.Importo,
        SaldoPrecedente = t.SaldoPrecedente,
        SaldoSuccessivo = t.SaldoSuccessivo,
        DataTransazione = t.DataTransazione,
        OperatoreId = t.OperatoreId,
        CinemaId = t.CinemaId,
        Descrizione = t.Descrizione,
        AcquistoId = t.AcquistoId
    };
}


