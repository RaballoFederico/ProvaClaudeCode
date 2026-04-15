using System;
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

public class ProiezioneServiceTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilmDbContext _context;
    private readonly SqliteConnection _connection;

    public ProiezioneServiceTests()
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

    public async Task InitializeAsync() => await Task.CompletedTask;

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
        var result = await context.Proiezioni.ToListAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesEntity()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();
        
        var film = new Film { Titolo = "Inception", DataProduzione = DateTime.Parse("2010-07-16"), RegistaId = regista.Id, Durata = 148 };
        context.Films.Add(film);
        await context.SaveChangesAsync();
        
        var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.Parse("2024-12-25"), Ora = TimeSpan.Parse("20:00") };
        context.Proiezioni.Add(proiezione);
        await context.SaveChangesAsync();
        
        var result = await context.Proiezioni.FindAsync(proiezione.Id);
        
        result.Should().NotBeNull();
        result!.Ora.Should().Be(TimeSpan.Parse("20:00"));
    }

    [Fact]
    public async Task CreateAsync_CinemaNotFound_ThrowsException()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        var film = new Film { Titolo = "Inception", DataProduzione = DateTime.Parse("2010-07-16"), RegistaId = regista.Id, Durata = 148 };
        context.Films.Add(film);
        await context.SaveChangesAsync();
        
        var proiezione = new Proiezione { CinemaId = 99999, FilmId = film.Id, Data = DateTime.Parse("2024-12-25"), Ora = TimeSpan.Parse("20:00") };
        context.Proiezioni.Add(proiezione);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CreateAsync_FilmNotFound_ThrowsException()
    {
        var context = GetContext();
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();
        
        var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = 99999, Data = DateTime.Parse("2024-12-25"), Ora = TimeSpan.Parse("20:00") };
        context.Proiezioni.Add(proiezione);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CreateAsync_UniqueViolation_ThrowsException()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();
        
        var film = new Film { Titolo = "Inception", DataProduzione = DateTime.Parse("2010-07-16"), RegistaId = regista.Id, Durata = 148 };
        context.Films.Add(film);
        await context.SaveChangesAsync();
        
        var proiezione1 = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.Parse("2024-12-25"), Ora = TimeSpan.Parse("20:00") };
        context.Proiezioni.Add(proiezione1);
        await context.SaveChangesAsync();
        
        var proiezione2 = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.Parse("2024-12-25"), Ora = TimeSpan.Parse("20:00") };
        context.Proiezioni.Add(proiezione2);
        
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesEntity()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();
        
        var film = new Film { Titolo = "Inception", DataProduzione = DateTime.Parse("2010-07-16"), RegistaId = regista.Id, Durata = 148 };
        context.Films.Add(film);
        await context.SaveChangesAsync();
        
        var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.Parse("2024-12-25"), Ora = TimeSpan.Parse("20:00") };
        context.Proiezioni.Add(proiezione);
        await context.SaveChangesAsync();

        proiezione.Ora = TimeSpan.Parse("21:00");
        await context.SaveChangesAsync();
        
        var result = await context.Proiezioni.FindAsync(proiezione.Id);
        
        result!.Ora.Should().Be(TimeSpan.Parse("21:00"));
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesEntity()
    {
        var context = GetContext();
        var regista = new Regista { Nome = "Christopher", Cognome = "Nolan" };
        context.Registi.Add(regista);
        await context.SaveChangesAsync();
        
        var cinema = new Cinema { Nome = "Cinema Odeon", Indirizzo = "Via Roma 10", Citta = "Milano" };
        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();
        
        var film = new Film { Titolo = "Inception", DataProduzione = DateTime.Parse("2010-07-16"), RegistaId = regista.Id, Durata = 148 };
        context.Films.Add(film);
        await context.SaveChangesAsync();
        
        var proiezione = new Proiezione { CinemaId = cinema.Id, FilmId = film.Id, Data = DateTime.Parse("2024-12-25"), Ora = TimeSpan.Parse("20:00") };
        context.Proiezioni.Add(proiezione);
        await context.SaveChangesAsync();
        
        context.Proiezioni.Remove(proiezione);
        await context.SaveChangesAsync();
        
        var result = await context.Proiezioni.FindAsync(proiezione.Id);
        
        result.Should().BeNull();
    }
}
