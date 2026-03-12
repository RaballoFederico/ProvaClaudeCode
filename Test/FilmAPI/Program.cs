using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using FilmAPI.Data;
using FilmAPI.Endpoints;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "filmapi_db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";

// Fallback: configure DbContext without AutoDetect to avoid design-time connection attempts.
// When running migrations locally against a real DB, replace with AutoDetect or explicit server version.
builder.Services.AddDbContext<FilmDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("8.0.29-mysql")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // expose the OpenAPI/Swagger UI in development
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGroup("/registi").MapRegistiEndpoints();
app.MapGroup("/films").MapFilmsEndpoints();
app.MapGroup("/cinemas").MapCinemasEndpoints();
app.MapGroup("/proiezioni").MapProiezioniEndpoints();

app.Run();

public partial class Program { }
