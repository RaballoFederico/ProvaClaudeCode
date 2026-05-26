using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FilmAPI.Model;
using FluentAssertions;
using Xunit;

namespace FilmAPI.Tests.Integration;

public class SubscriptionBenefitsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SubscriptionBenefitsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangingSubscriptionPlan_ChangesCheckoutBenefits()
    {
        await _factory.ResetDatabaseAsync();
        var client = await _factory.CreateUserClientAsync();
        var showId = await SeedShowAsync(TipologiaSala.TRE_D, 12m);

        var baseActivation = await client.PostAsJsonAsync("/abbonamenti/attiva", new { piano = "Base" });
        baseActivation.StatusCode.Should().Be(HttpStatusCode.OK);

        var baseCalc = await client.PostAsJsonAsync("/acquisto/calcola-importo", new
        {
            showId,
            numeroBiglietti = 1,
            usaCredito = false
        });
        baseCalc.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var doc = JsonDocument.Parse(await baseCalc.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("pianoAbbonamento").GetString().Should().Be("Base");
            doc.RootElement.GetProperty("proiezioneCopertaDalPiano").GetBoolean().Should().BeFalse();
            doc.RootElement.GetProperty("scontoAbbonamento").GetDecimal().Should().Be(0m);
            doc.RootElement.GetProperty("subtotale").GetDecimal().Should().Be(12m);
        }

        var plusActivation = await client.PostAsJsonAsync("/abbonamenti/attiva", new { piano = "Plus" });
        plusActivation.StatusCode.Should().Be(HttpStatusCode.OK);

        var plusCalc = await client.PostAsJsonAsync("/acquisto/calcola-importo", new
        {
            showId,
            numeroBiglietti = 2,
            usaCredito = false
        });
        plusCalc.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var doc = JsonDocument.Parse(await plusCalc.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("pianoAbbonamento").GetString().Should().Be("Plus");
            doc.RootElement.GetProperty("proiezioneCopertaDalPiano").GetBoolean().Should().BeTrue();
            doc.RootElement.GetProperty("ingressiAbbonamentoApplicati").GetInt32().Should().Be(2);
            doc.RootElement.GetProperty("scontoAbbonamento").GetDecimal().Should().Be(24m);
            doc.RootElement.GetProperty("subtotale").GetDecimal().Should().Be(0m);
        }
    }

    private async Task<int> SeedShowAsync(TipologiaSala tipologia, decimal prezzo)
    {
        return await _factory.WithDbContextAsync(async db =>
        {
            var regista = new Regista { Nome = "Benefit", Cognome = "Director" };
            db.Registi.Add(regista);
            await db.SaveChangesAsync();

            var film = new Film
            {
                Titolo = $"Benefit Test {Guid.NewGuid():N}",
                DataProduzione = DateTime.UtcNow,
                RegistaId = regista.Id,
                Durata = 100
            };
            var cinema = new Cinema
            {
                Nome = "Cinema Benefit",
                Indirizzo = "Via Test 1",
                Citta = "Test",
                PostiMassimi = 100
            };
            db.Films.Add(film);
            db.Cinemas.Add(cinema);
            await db.SaveChangesAsync();

            var sala = new Sala
            {
                CinemaId = cinema.Id,
                NumeroSala = 1,
                Tipologia = tipologia,
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
                PrezzoBase = prezzo
            };
            db.Shows.Add(show);
            await db.SaveChangesAsync();

            return show.Id;
        });
    }
}
