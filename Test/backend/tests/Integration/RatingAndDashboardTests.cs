using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FilmAPI.Model;
using FluentAssertions;
using Xunit;

namespace FilmAPI.Tests.Integration;

public class RatingAndDashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RatingAndDashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rating_WithoutPaidPurchase_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync(async db =>
        {
            var regista = new Regista { Nome = "Test", Cognome = "Director" };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();
            db.Films.Add(new Film { Titolo = "No Ticket", DataProduzione = DateTime.UtcNow, RegistaId = regista.Id, Durata = 100 });
        });

        var client = await _factory.CreateUserClientAsync();
        var filmId = await _factory.WithDbContextAsync(async db => await Task.FromResult(db.Films.First().Id));

        var response = await client.PostAsJsonAsync($"/films/{filmId}/ratings", new { valutazione = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rating_WithPaidPurchase_SavesAndSummaryShowsCurrentUserRating()
    {
        await _factory.ResetDatabaseAsync();
        var client = await _factory.CreateUserClientAsync();

        var filmId = await SeedPaidPurchaseForUserAsync("user");

        var saveResponse = await client.PostAsJsonAsync($"/films/{filmId}/ratings", new { valutazione = 4 });
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaryResponse = await client.GetAsync($"/films/{filmId}/ratings/summary");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("currentUserRating").GetInt32().Should().Be(4);
        doc.RootElement.GetProperty("canRate").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DashboardOverview_WithRevenueRange_ReturnsKpisAndTopPerformers()
    {
        await _factory.ResetDatabaseAsync();
        await _factory.CreateUserClientAsync();
        await SeedPaidPurchaseForUserAsync("user");

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/dashboard/overview?revenueRange=month");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var revenue = doc.RootElement.GetProperty("revenue");

        revenue.GetProperty("range").GetString().Should().Be("month");
        revenue.GetProperty("kpis").GetProperty("purchases").GetInt32().Should().BeGreaterThan(0);
        revenue.GetProperty("topCinemas").GetArrayLength().Should().BeGreaterThan(0);
        revenue.GetProperty("topSale").GetArrayLength().Should().BeGreaterThan(0);
    }

    private async Task<int> SeedPaidPurchaseForUserAsync(string username)
    {
        return await _factory.WithDbContextAsync(async db =>
        {
            var user = db.Utenti.First(u => u.Username == username);
            var regista = new Regista { Nome = "Paid", Cognome = "Director" };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();

            var film = new Film
            {
                Titolo = $"Film {Guid.NewGuid():N}",
                DataProduzione = DateTime.UtcNow,
                RegistaId = regista.Id,
                Durata = 120
            };
            var cinema = new Cinema { Nome = "Cinema Test", Indirizzo = "Via Test 1", Citta = "Test", PostiMassimi = 100 };
            db.Films.Add(film);
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();

            var sala = new Sala
            {
                CinemaId = cinema.Id,
                NumeroSala = 1,
                Tipologia = TipologiaSala.DUE_D,
                NumeroFile = 5,
                PostiPerFila = 10,
                PostiTotali = 50
            };
            db.Sale.Add(sala);
            await db.SaveChangesAsync();

            var show = new Show
            {
                FilmId = film.Id,
                SalaId = sala.Id,
                Data = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                OraInizio = new TimeOnly(20, 0),
                OraFine = new TimeOnly(22, 0),
                PrezzoBase = 12m
            };
            db.Shows.Add(show);
            await db.SaveChangesAsync();

            var acquisto = new Acquisto
            {
                UtenteId = user.Id,
                ShowId = show.Id,
                DataAcquisto = DateTime.UtcNow,
                ImportoTotale = 24m,
                CreditoUsato = 0m,
                MetodoPagamentoSalvato = false,
                Stato = StatoAcquisto.PAGATO
            };
            db.Acquisti.Add(acquisto);
            await db.SaveChangesAsync();

            db.Biglietti.Add(new Biglietto
            {
                AcquistoId = acquisto.Id,
                ShowId = show.Id,
                CinemaId = cinema.Id,
                Posto = "Fila 1, Posto 1",
                SalaNumero = 1,
                TipologiaSala = "DUE_D",
                Prezzo = 12m,
                CodiceUnivoco = Guid.NewGuid().ToString("N"),
                CodiceHash = Guid.NewGuid().ToString("N"),
                QRCodeUrl = string.Empty
            });
            await db.SaveChangesAsync();

            return film.Id;
        });
    }
}
