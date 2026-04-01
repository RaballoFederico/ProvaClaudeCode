using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        // POST /auth/login
        group.MapPost("/login", async (LoginRequestDTO request, FilmDbContext db, JwtService jwtService) =>
        {
            // Cerca utente per username o email
            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .FirstOrDefaultAsync(u =>
                    (u.Username == request.Username || u.Email == request.Username) && u.Attivo);

            if (utente == null)
            {
                return Results.Unauthorized();
            }

            // Verifica password con BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, utente.PasswordHash))
            {
                return Results.Unauthorized();
            }

            // Aggiorna ultimo accesso
            utente.DataUltimoAccesso = DateTime.UtcNow;

            // Genera token
            var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
            var accessToken = jwtService.GenerateAccessToken(utente, ruoli);
            var refreshToken = jwtService.GenerateRefreshToken();

            // Salva refresh token
            utente.RefreshToken = refreshToken;
            utente.RefreshTokenExpiry = jwtService.GetRefreshTokenExpiry();

            await db.SaveChangesAsync();

            var response = new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = jwtService.GetAccessTokenExpiry(),
                Utente = new UtenteDTO
                {
                    Id = utente.Id,
                    Username = utente.Username,
                    Email = utente.Email,
                    Nome = utente.Nome,
                    Cognome = utente.Cognome,
                    Ruoli = ruoli
                }
            };

            return Results.Ok(response);
        });

        // POST /auth/refresh
        group.MapPost("/refresh", async (RefreshTokenRequestDTO request, FilmDbContext db, JwtService jwtService) =>
        {
            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken && u.Attivo);

            if (utente == null || utente.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return Results.Unauthorized();
            }

            // Genera nuovi token
            var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
            var accessToken = jwtService.GenerateAccessToken(utente, ruoli);
            var refreshToken = jwtService.GenerateRefreshToken();

            // Aggiorna refresh token
            utente.RefreshToken = refreshToken;
            utente.RefreshTokenExpiry = jwtService.GetRefreshTokenExpiry();

            await db.SaveChangesAsync();

            var response = new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = jwtService.GetAccessTokenExpiry(),
                Utente = new UtenteDTO
                {
                    Id = utente.Id,
                    Username = utente.Username,
                    Email = utente.Email,
                    Nome = utente.Nome,
                    Cognome = utente.Cognome,
                    Ruoli = ruoli
                }
            };

            return Results.Ok(response);
        });

        // POST /auth/logout
        group.MapPost("/logout", [Authorize] async (HttpContext context, FilmDbContext db) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var utente = await db.Utenti.FindAsync(int.Parse(userId));
                if (utente != null)
                {
                    utente.RefreshToken = null;
                    utente.RefreshTokenExpiry = null;
                    await db.SaveChangesAsync();
                }
            }

            return Results.Ok(new { message = "Logout effettuato con successo" });
        });

        // POST /auth/register
        group.MapPost("/register", async (RegistrazioneRequestDTO request, FilmDbContext db) =>
        {
            // Validazione base
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            {
                return Results.BadRequest(new { message = "Username deve essere di almeno 3 caratteri" });
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            {
                return Results.BadRequest(new { message = "Email non valida" });
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            {
                return Results.BadRequest(new { message = "Password deve essere di almeno 8 caratteri" });
            }

            // Verifica username/email unici
            if (await db.Utenti.AnyAsync(u => u.Username == request.Username))
            {
                return Results.Conflict(new { message = "Username già in uso" });
            }

            if (await db.Utenti.AnyAsync(u => u.Email == request.Email))
            {
                return Results.Conflict(new { message = "Email già in uso" });
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Trova ruolo User
            var ruoloUser = await db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "User");
            if (ruoloUser == null)
            {
                return Results.Problem("Ruolo User non trovato");
            }

            // Crea utente
            var utente = new Utente
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Nome = request.Nome,
                Cognome = request.Cognome,
                Telefono = request.Telefono,
                DataRegistrazione = DateTime.UtcNow,
                Attivo = true,
                UtentiRuoli = new List<UtenteRuolo>
                {
                    new UtenteRuolo { RuoloId = ruoloUser.Id }
                }
            };

            await db.Utenti.AddAsync(utente);
            await db.SaveChangesAsync();

            return Results.Created($"/auth/me", new { message = "Registrazione completata con successo", utenteId = utente.Id });
        });

        // GET /auth/me
        group.MapGet("/me", [Authorize] async (HttpContext context, FilmDbContext db) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var utente = await db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId) && u.Attivo);

            if (utente == null)
            {
                return Results.NotFound();
            }

            var dto = new UtenteDTO
            {
                Id = utente.Id,
                Username = utente.Username,
                Email = utente.Email,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList()
            };

            return Results.Ok(dto);
        });

        return app;
    }
}
