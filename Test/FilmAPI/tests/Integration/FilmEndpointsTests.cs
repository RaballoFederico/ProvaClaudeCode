using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FilmAPI.DTO;
using FluentAssertions;
using Xunit;

namespace FilmAPI.Tests.Integration;

public class FilmEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FilmEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task F1_GetFilms_EmptyList_ReturnsEmptyArray()
    {
        await _factory.ResetDatabaseAsync();
        
        var response = await _client.GetAsync("/films/");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
    }

    [Fact]
    public async Task F2_PostFilms_ValidData_ReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var request = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista!.Id, 
            Durata = 148,
            CopertinaPath = "/media/inception.jpg",
            FilmatoPath = "/media/inception.mp4"
        };
        var response = await adminClient.PostAsJsonAsync("/films/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<FilmDTO>();
        result.Should().NotBeNull();
        result!.Titolo.Should().Be("Inception");
    }

    [Fact]
    public async Task F3_PostFilms_DefaultCoverPath_ReturnsCreatedWithDefault()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var registaRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan" };
        var registaResponse = await adminClient.PostAsJsonAsync("/registi/", registaRequest);
        var regista = await registaResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var request = new FilmCreateDTO 
        { 
            Titolo = "Interstellar", 
            DataProduzione = DateTime.Parse("2014-11-07"), 
            RegistaId = regista!.Id, 
            Durata = 169
        };
        var response = await adminClient.PostAsJsonAsync("/films/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<FilmDTO>();
        result!.CopertinaPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task F4_PostFilms_InvalidFK_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var request = new FilmCreateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = 99999, 
            Durata = 148
        };
        var response = await adminClient.PostAsJsonAsync("/films/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task F5_GetFilmById_Existing_ReturnsOk()
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
        
        var response = await _client.GetAsync($"/films/{film!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FilmDTO>();
        result!.Titolo.Should().Be("Inception");
    }

    [Fact]
    public async Task F6_PutFilm_ValidData_ReturnsOk()
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
        
        var updateRequest = new FilmUpdateDTO 
        { 
            Titolo = "Inception Updated", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = regista.Id, 
            Durata = 148
        };
        var response = await adminClient.PutAsJsonAsync($"/films/{film!.Id}", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FilmDTO>();
        result!.Titolo.Should().Be("Inception Updated");
    }

    [Fact]
    public async Task F7_PutFilm_InvalidFK_ReturnsBadRequest()
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
        
        var updateRequest = new FilmUpdateDTO 
        { 
            Titolo = "Inception", 
            DataProduzione = DateTime.Parse("2010-07-16"), 
            RegistaId = 99999, 
            Durata = 148
        };
        var response = await adminClient.PutAsJsonAsync($"/films/{film!.Id}", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task F8_DeleteFilm_Existing_ReturnsNoContent()
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
        
        var response = await adminClient.DeleteAsync($"/films/{film!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var verifyResponse = await _client.GetAsync($"/films/{film.Id}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
