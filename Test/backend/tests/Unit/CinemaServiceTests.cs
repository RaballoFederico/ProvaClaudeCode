using System;
using System.Linq;
using System.Threading.Tasks;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FilmAPI.Tests.Unit;

public class CinemaServiceTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilmDbContext _context;

    public CinemaServiceTests()
    {
        var services = new ServiceCollection();
        
        var options = new DbContextOptionsBuilder<FilmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new FilmDbContext(options);
        
        services.AddScoped(_ => _context);
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync() => await Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    private FilmDbContext GetContext() => _serviceProvider.GetRequiredService<FilmDbContext>();

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmptyList()
    {
        var context = GetContext();
        var result = await context.Cinemas.ToListAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesEntity()
    {
        var context = GetContext();
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();
        
        var result = await context.Cinemas.FindAsync(cinema.Id);
        
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Cinema Odeon");
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsEntity()
    {
        var context = GetContext();
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();

        var result = await context.Cinemas.FindAsync(cinema.Id);
        
        result.Should().NotBeNull();
        result!.Nome.Should().Be("Cinema Odeon");
    }

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesEntity()
    {
        var context = GetContext();
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();

        cinema.Citta = "Roma";
        await context.SaveChangesAsync();
        
        var result = await context.Cinemas.FindAsync(cinema.Id);
        
        result!.Citta.Should().Be("Roma");
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesEntity()
    {
        var context = GetContext();
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();
        
        context.Cinemas.Remove(cinema);
        await context.SaveChangesAsync();
        
        var result = await context.Cinemas.FindAsync(cinema.Id);
        
        result.Should().BeNull();
    }
}