// DOC: Service 'PrenotazioneTempCleanupService': implementa logica di business e integrazioni esterne (DB/TMDB/Stripe).
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

// DOC-METHOD: 'PrenotazioneTempCleanupService' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
public class PrenotazioneTempCleanupService(IServiceScopeFactory scopeFactory, ILogger<PrenotazioneTempCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<FilmDbContext>();

                var scaduti = await context.PrenotazioniTemporanee
                    .Where(p => p.Stato == StatoPrenotazioneTemp.ATTIVA && p.DataScadenza < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (scaduti.Count > 0)
                {
                    foreach (var p in scaduti)
                    {
                        p.Stato = StatoPrenotazioneTemp.SCADUTA;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Non bloccare l'intera API se il DB e temporaneamente non raggiungibile.
                logger.LogWarning(ex, "Cleanup prenotazioni temporanee fallito: nuovo tentativo al prossimo ciclo.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

