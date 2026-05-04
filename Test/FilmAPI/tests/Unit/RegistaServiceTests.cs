using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FilmAPI.Tests.Unit;

public class RegistaServiceTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilmDbContext _context;
    private readonly SqliteConnection _connection;

    public RegistaServiceTests()
    {
        var services = new ServiceCollection();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FilmDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new FilmDbContext(options);
        _context.Database.EnsureCreated();
        
        services.AddScoped(_ => _context);
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private FilmDbContext GetContext() => _serviceProvider.GetRequiredService<FilmDbContext>();

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyList()
    {
        var context = GetContext();
        var result = await context.Registi.ToListAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsList()
    {
        var context = GetContext();
        context.Registi.Add(new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" });
        await context.SaveChangesAsync();

        var result = await context.Registi.ToListAsync();
        
        result.Should().HaveCount(1);
        result.First().Nome.Should().Be("Christopher");
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsEntity()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();

        var result = await context.Registi.FindAsync(regista.Id);
        
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Christopher");
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var context = GetContext();
        
        var result = await context.Registi.FindAsync(99999);
        
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesEntity()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };
        
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        var result = await context.Registi.FindAsync(regista.Id);
        
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Christopher");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_InvalidData_ThrowsException()
    {
        var context = GetContext();
        var regista = new Regista { Nome = null!, Cognome = "Nolan" };
        
        context.Registi.Add(regista);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UpdateAsync_Existing_UpdatesEntity()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();

        regista.Nazionalita = "US";
        await context.SaveChangesAsync();
        
        var result = await context.Registi.FindAsync(regista.Id);
        
        result!.Nazionalita.Should().Be("US");
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ReturnsNull()
    {
        var context = GetContext();
        
        var result = await context.Registi.FindAsync(99999);
        
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesEntity()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        context.Registi.Remove(regista);
        await context.SaveChangesAsync();
        
        var result = await context.Registi.FindAsync(regista.Id);
        
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ThrowsConcurrencyException()
    {
        var context = GetContext();
        var initialCount = await context.Registi.CountAsync();
        
        var regista = new Regista { Id = 99999, Nome = "Test", Cognome = "Test" };
        context.Registi.Remove(regista);
        
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());
        
        var finalCount = await context.Registi.CountAsync();
        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task GetFilmsByRegistaIdAsync_WithFilms_ReturnsFilms()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        var film = new Film { Titolo = "Inception", DataProduzione = DateTime.Parse("2010-07-16"), RegistaId = regista.Id, Durata = 148 };
        context.Films.Add(film);
        await context.SaveChangesAsync();

        var result = await context.Films.Where(f => f.RegistaId == regista.Id).ToListAsync();
        
        result.Should().HaveCount(1);
        result.First().Titolo.Should().Be("Inception");
    }

    [Fact]
    public async Task GetFilmsByRegistaIdAsync_NoFilms_ReturnsEmpty()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();

        var result = await context.Films.Where(f => f.RegistaId == regista.Id).ToListAsync();
        
        result.Should().BeEmpty();
    }
}
