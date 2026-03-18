using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FilmAPI.Data;
using FilmAPI.DTO;
using FluentAssertions;
using Xunit;

namespace FilmAPI.Tests.Integration;

public class RegistiEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RegistiEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task R1_GetRegisti_EmptyList_ReturnsEmptyArray()
    {
        await _factory.ResetDatabaseAsync();
        
        var response = await _client.GetAsync("/registi/");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
    }

    [Fact]
    public async Task R2_PostRegisti_ValidData_ReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();
        
        var request = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Britannica" };
        var response = await _client.PostAsJsonAsync("/registi/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RegistaDTO>();
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.Nome.Should().Be("Christopher");
    }

    [Fact]
    public async Task R3_GetRegistiById_Existing_ReturnsOk()
    {
        await _factory.ResetDatabaseAsync();
        
        var createRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Britannica" };
        var createResponse = await _client.PostAsJsonAsync("/registi/", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var response = await _client.GetAsync($"/registi/{created!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegistaDTO>();
        result!.Nome.Should().Be("Christopher");
    }

    [Fact]
    public async Task R4_GetRegistiById_NonExisting_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        
        var response = await _client.GetAsync("/registi/99999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task R5_PutRegisti_Existing_ReturnsOk()
    {
        await _factory.ResetDatabaseAsync();
        
        var createRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Britannica" };
        var createResponse = await _client.PostAsJsonAsync("/registi/", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var updateRequest = new RegistaUpdateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Statunitense" };
        var response = await _client.PutAsJsonAsync($"/registi/{created!.Id}", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegistaDTO>();
        result!.Nazionalita.Should().Be("Statunitense");
    }

    [Fact]
    public async Task R6_PutRegisti_NonExisting_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        
        var updateRequest = new RegistaUpdateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Statunitense" };
        var response = await _client.PutAsJsonAsync("/registi/99999", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task R7_DeleteRegisti_Existing_ReturnsNoContent()
    {
        await _factory.ResetDatabaseAsync();
        
        var createRequest = new RegistaCreateDTO { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Britannica" };
        var createResponse = await _client.PostAsJsonAsync("/registi/", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistaDTO>();
        
        var response = await _client.DeleteAsync($"/registi/{created!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var verifyResponse = await _client.GetAsync($"/registi/{created.Id}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task R8_DeleteRegisti_NonExisting_ReturnsNotFound()
    {
        await _factory.ResetDatabaseAsync();
        
        var response = await _client.DeleteAsync("/registi/99999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task R9_PostRegisti_MissingData_ReturnsBadRequest()
    {
        await _factory.ResetDatabaseAsync();
        
        var request = new RegistaCreateDTO { Nome = "Christopher" };
        var response = await _client.PostAsJsonAsync("/registi/", request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}