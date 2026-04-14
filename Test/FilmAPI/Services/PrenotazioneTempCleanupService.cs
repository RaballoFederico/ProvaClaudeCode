using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class PrenotazioneTempCleanupService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
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

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
