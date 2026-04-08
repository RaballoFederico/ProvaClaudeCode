using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public static class DbInitializer
{
    public static async Task InitializeAsync(FilmDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // Seed Ruoli
        if (!context.Ruoli.Any())
        {
            var ruoli = new List<Ruolo>
            {
                new Ruolo { Nome = "Admin", Descrizione = "Amministratore con accesso completo" },
                new Ruolo { Nome = "PowerUser", Descrizione = "Utente con privilegi elevati" },
                new Ruolo { Nome = "User", Descrizione = "Utente standard" }
            };

            await context.Ruoli.AddRangeAsync(ruoli);
            await context.SaveChangesAsync();
        }

        // Seed Categorie
        if (!context.Categorie.Any())
        {
            var categorie = new List<Categoria>
            {
                new Categoria { Nome = "Fantasy", Descrizione = "Film di genere fantasy" },
                new Categoria { Nome = "Horror", Descrizione = "Film di genere horror" },
                new Categoria { Nome = "Drammatico", Descrizione = "Film drammatici" },
                new Categoria { Nome = "Commedia", Descrizione = "Film comici" },
                new Categoria { Nome = "Azione", Descrizione = "Film d'azione" },
                new Categoria { Nome = "Thriller", Descrizione = "Film thriller" },
                new Categoria { Nome = "Romantico", Descrizione = "Film romantici" },
                new Categoria { Nome = "Sci-Fi", Descrizione = "Film di fantascienza" },
                new Categoria { Nome = "Animazione", Descrizione = "Film di animazione" },
                new Categoria { Nome = "Documentario", Descrizione = "Film documentari" },
                new Categoria { Nome = "Avventura", Descrizione = "Film di avventura" },
                new Categoria { Nome = "Storico", Descrizione = "Film storici" }
            };

            await context.Categorie.AddRangeAsync(categorie);
            await context.SaveChangesAsync();
        }

        // Seed Utente Admin
        if (!context.Utenti.Any())
        {
            var adminRuolo = await context.Ruoli.FirstOrDefaultAsync(r => r.Nome == "Admin");

            if (adminRuolo != null)
            {
                var admin = new Utente
                {
                    Username = "admin",
                    Email = "admin@filmapi.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Nome = "Admin",
                    Cognome = "System",
                    DataRegistrazione = DateTime.UtcNow,
                    Attivo = true,
                    UtentiRuoli = new List<UtenteRuolo>
                    {
                        new UtenteRuolo { RuoloId = adminRuolo.Id }
                    }
                };

                await context.Utenti.AddAsync(admin);
                await context.SaveChangesAsync();
            }
        }
    }
}
