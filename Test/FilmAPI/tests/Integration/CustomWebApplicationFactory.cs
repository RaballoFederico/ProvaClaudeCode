using System;
using System.Linq;
using FilmAPI.Data;
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
        if (seed is not null)
        {
            await seed(db);
            await db.SaveChangesAsync();
        }
    }
}
