using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FilmAPI.DTO;
using FluentAssertions;
using Xunit;

namespace FilmAPI.Tests.Integration;

public class ProiezioneEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProiezioneEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task P1_GetProiezioni_EmptyList_ReturnsEmptyArray()
    {
        await _factory.ResetDatabaseAsync();
        
        var response = await _client.GetAsync("/proiezioni/");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
    }

    [Fact]
    public async Task P2_PostProiezioni_ValidData_ReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var filmRequest = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista!.Id, 
            Durata = 148
        };
        var filmResponse = await adminClient.PostAsJsonAsync("/films/", filmRequest);
        var film = await filmResponse.Content.ReadFromJsonAsync<FilmDTO>();
        
        var cinemaRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var cinemaResponse = await adminClient.PostAsJsonAsync("/cinemas/", cinemaRequest);
        var cinema = await cinemaResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var request = new ProiezioneCreateDTO 
        { 
            CinemaId = cinema!.Id, 
            FilmId = film!.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        var response = await adminClient.PostAsJsonAsync("/proiezioni/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        result.Should().NotBeNull();
        result!.Ora.Should().Be(TimeSpan.Parse("20:00"));
    }

    [Fact]
    public async Task P3_PostProiezioni_InvalidCinemaFK_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var filmRequest = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista!.Id, 
            Durata = 148
        };
        var filmResponse = await adminClient.PostAsJsonAsync("/films/", filmRequest);
        var film = await filmResponse.Content.ReadFromJsonAsync<FilmDTO>();
        
        var request = new ProiezioneCreateDTO 
        { 
            CinemaId = 99999, 
            FilmId = film!.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        var response = await adminClient.PostAsJsonAsync("/proiezioni/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task P4_PostProiezioni_InvalidFilmFK_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var cinemaRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var cinemaResponse = await adminClient.PostAsJsonAsync("/cinemas/", cinemaRequest);
        var cinema = await cinemaResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var request = new ProiezioneCreateDTO 
        { 
            CinemaId = cinema!.Id, 
            FilmId = 99999, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        var response = await adminClient.PostAsJsonAsync("/proiezioni/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task P5_PostProiezioni_Duplicate_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var filmRequest = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista!.Id, 
            Durata = 148
        };
        var filmResponse = await adminClient.PostAsJsonAsync("/films/", filmRequest);
        var film = await filmResponse.Content.ReadFromJsonAsync<FilmDTO>();
        
        var cinemaRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var cinemaResponse = await adminClient.PostAsJsonAsync("/cinemas/", cinemaRequest);
        var cinema = await cinemaResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var request1 = new ProiezioneCreateDTO 
        { 
            CinemaId = cinema!.Id, 
            FilmId = film!.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        await adminClient.PostAsJsonAsync("/proiezioni/", request1);
        
        var request2 = new ProiezioneCreateDTO 
        { 
            CinemaId = cinema.Id, 
            FilmId = film.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        var response = await adminClient.PostAsJsonAsync("/proiezioni/", request2);
        
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task P6_GetProiezioneById_Existing_ReturnsOk()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var filmRequest = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista!.Id, 
            Durata = 148
        };
        var filmResponse = await adminClient.PostAsJsonAsync("/films/", filmRequest);
        var film = await filmResponse.Content.ReadFromJsonAsync<FilmDTO>();
        
        var cinemaRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var cinemaResponse = await adminClient.PostAsJsonAsync("/cinemas/", cinemaRequest);
        var cinema = await cinemaResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var proiezioneRequest = new ProiezioneCreateDTO 
        { 
            CinemaId = cinema!.Id, 
            FilmId = film!.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        var proiezioneResponse = await adminClient.PostAsJsonAsync("/proiezioni/", proiezioneRequest);
        var proiezione = await proiezioneResponse.Content.ReadFromJsonAsync<ProiezioneDTO>();
        
        var response = await _client.GetAsync($"/proiezioni/{proiezione!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        result!.Ora.Should().Be(TimeSpan.Parse("20:00"));
    }

    [Fact]
    public async Task P7_PutProiezione_ValidData_ReturnsOk()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var filmRequest = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista!.Id, 
            Durata = 148
        };
        var filmResponse = await adminClient.PostAsJsonAsync("/films/", filmRequest);
        var film = await filmResponse.Content.ReadFromJsonAsync<FilmDTO>();
        
        var cinemaRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var cinemaResponse = await adminClient.PostAsJsonAsync("/cinemas/", cinemaRequest);
        var cinema = await cinemaResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var proiezioneRequest = new ProiezioneCreateDTO 
        { 
            CinemaId = cinema!.Id, 
            FilmId = film!.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        var proiezioneResponse = await adminClient.PostAsJsonAsync("/proiezioni/", proiezioneRequest);
        var proiezione = await proiezioneResponse.Content.ReadFromJsonAsync<ProiezioneDTO>();
        
        var updateRequest = new ProiezioneUpdateDTO 
        { 
            CinemaId = cinema.Id, 
            FilmId = film.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("21:00") 
        };
        var response = await adminClient.PutAsJsonAsync($"/proiezioni/{proiezione!.Id}", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProiezioneDTO>();
        result!.Ora.Should().Be(TimeSpan.Parse("21:00"));
    }

    [Fact]
    public async Task P8_DeleteProiezione_Existing_ReturnsNoContent()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var filmRequest = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista!.Id, 
            Durata = 148
        };
        var filmResponse = await adminClient.PostAsJsonAsync("/films/", filmRequest);
        var film = await filmResponse.Content.ReadFromJsonAsync<FilmDTO>();
        
        var cinemaRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var cinemaResponse = await adminClient.PostAsJsonAsync("/cinemas/", cinemaRequest);
        var cinema = await cinemaResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var proiezioneRequest = new ProiezioneCreateDTO 
        { 
            CinemaId = cinema!.Id, 
            FilmId = film!.Id, 
            Data = DateTime.Parse("2024-12-25"), 
            Ora = TimeSpan.Parse("20:00") 
        };
        var proiezioneResponse = await adminClient.PostAsJsonAsync("/proiezioni/", proiezioneRequest);
        var proiezione = await proiezioneResponse.Content.ReadFromJsonAsync<ProiezioneDTO>();
        
        var response = await adminClient.DeleteAsync($"/proiezioni/{proiezione!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
