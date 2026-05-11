using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FilmAPI.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_TESTING", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<FilmDbContext>) ||
                d.ServiceType == typeof(FilmDbContext)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<FilmDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync() => await Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task ResetDatabaseAsync(Func<FilmDbContext, Task>? seed = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await SeedAuthDataAsync(db);
        if (seed is not null)
        {
            await seed(db);
            await db.SaveChangesAsync();
        }
    }

    public async Task ResetDatabaseExtendedAsync(Func<FilmDbContext, Task>? seed = null)
    {
        await ResetDatabaseAsync(seed);
    }

    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDTO
        {
            Username = "admin",
            Password = "Admin123!"
        });

        var data = await login.Content.ReadFromJsonAsync<LoginResponseDTO>();
        if (data?.AccessToken != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
        }

        return client;
    }

    public async Task<HttpClient> CreateUserClientAsync(string username = "user", string password = "User123!", string email = "user@test.local")
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
            if (!await db.Utenti.AnyAsync(u => u.Username == username))
            {
                var userRole = await db.Ruoli.FirstAsync(r => r.Nome == "User");
                var user = new Utente
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Attivo = true,
                    DataRegistrazione = DateTime.UtcNow,
                    UtentiRuoli = new List<UtenteRuolo> { new UtenteRuolo { RuoloId = userRole.Id } }
                };
                db.Utenti.Add(user);
                await db.SaveChangesAsync();
            }
        }

        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDTO
        {
            Username = username,
            Password = password
        });
        var data = await login.Content.ReadFromJsonAsync<LoginResponseDTO>();
        if (data?.AccessToken != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.AccessToken);
        }
        return client;
    }

    public async Task<T> WithDbContextAsync<T>(Func<FilmDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        return await action(db);
    }

    private static async Task SeedAuthDataAsync(FilmDbContext db)
    {
        if (!await db.Ruoli.AnyAsync())
        {
            await db.Ruoli.AddRangeAsync(
                new Ruolo { Nome = "Admin", Descrizione = "Amministratore" },
                new Ruolo { Nome = "PowerUser", Descrizione = "Power user" },
                new Ruolo { Nome = "User", Descrizione = "Utente" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Utenti.AnyAsync(u => u.Username == "admin"))
        {
            var adminRuolo = await db.Ruoli.FirstAsync(r => r.Nome == "Admin");
            var admin = new Utente
            {
                Username = "admin",
                Email = "admin@test.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Nome = "Admin",
                Cognome = "Test",
                Attivo = true,
                DataRegistrazione = DateTime.UtcNow,
                UtentiRuoli = new List<UtenteRuolo>
                {
                    new UtenteRuolo { RuoloId = adminRuolo.Id }
                }
            };

            db.Utenti.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
