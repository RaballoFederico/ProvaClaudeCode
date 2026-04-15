using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FilmAPI.DTO;
using FluentAssertions;
using Xunit;

namespace FilmAPI.Tests.Integration;

public class CinemaEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CinemaEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task C1_GetCinemas_EmptyList_ReturnsEmptyArray()
    {
        await _factory.ResetDatabaseAsync();
        
        var response = await _client.GetAsync("/cinemas/");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
    }

    [Fact]
    public async Task C2_PostCinemas_ValidData_ReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var request = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var response = await adminClient.PostAsJsonAsync("/cinemas/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CinemaDTO>();
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Cinema Odeon");
    }

    [Fact]
    public async Task C3_GetCinemaById_Existing_ReturnsOk()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var createRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var createResponse = await adminClient.PostAsJsonAsync("/cinemas/", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var response = await _client.GetAsync($"/cinemas/{created!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CinemaDTO>();
        result!.Nome.Should().Be("Cinema Odeon");
    }

    [Fact]
    public async Task C4_PutCinema_ValidData_ReturnsOk()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var createRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var createResponse = await adminClient.PostAsJsonAsync("/cinemas/", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var updateRequest = new CinemaUpdateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Roma" };
        var response = await adminClient.PutAsJsonAsync($"/cinemas/{created!.Id}", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CinemaDTO>();
        result!.Citta.Should().Be("Roma");
    }

    [Fact]
    public async Task C5_DeleteCinema_Existing_ReturnsNoContent()
    {
        await _factory.ResetDatabaseAsync();
        var adminClient = await _factory.CreateAdminClientAsync();
        
        var createRequest = new CinemaCreateDTO { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        var createResponse = await adminClient.PostAsJsonAsync("/cinemas/", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CinemaDTO>();
        
        var response = await adminClient.DeleteAsync($"/cinemas/{created!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
