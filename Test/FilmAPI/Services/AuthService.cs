using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class AuthService : IAuthService
{
    private readonly FilmDbContext _db;
    private readonly JwtService _jwtService;

    public AuthService(FilmDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<(LoginResponseDTO? response, string? error)> LoginAsync(LoginRequestDTO request)
    {
        var utente = await _db.Utenti
            .Include(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(u => (u.Username == request.Username || u.Email == request.Username) && u.Attivo);

        if (utente == null || !BCrypt.Net.BCrypt.Verify(request.Password, utente.PasswordHash))
        {
            return (null, "INVALID_CREDENTIALS");
        }

        utente.DataUltimoAccesso = DateTime.UtcNow;
        var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
        var accessToken = _jwtService.GenerateAccessToken(utente, ruoli);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        utente.RefreshToken = refreshTokenValue;
        utente.RefreshTokenExpiry = refreshTokenExpiry;

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenValue,
            UtenteId = utente.Id,
            ExpiresAt = refreshTokenExpiry
        });

        await _db.SaveChangesAsync();

        return (new LoginResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = _jwtService.GetAccessTokenExpiry(),
            Utente = new UtenteDTO
            {
                Id = utente.Id,
                Username = utente.Username,
                Email = utente.Email,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Ruoli = ruoli
            }
        }, null);
    }

    public async Task<(LoginResponseDTO? response, string? error)> RefreshAsync(RefreshTokenRequestDTO request)
    {
        var refreshToken = await _db.RefreshTokens
            .Include(rt => rt.Utente)
            .ThenInclude(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.RevokedAt == null);

        if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow || !refreshToken.Utente.Attivo)
        {
            return (null, "INVALID_REFRESH");
        }

        refreshToken.RevokedAt = DateTime.UtcNow;

        var utente = refreshToken.Utente;
        var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
        var accessToken = _jwtService.GenerateAccessToken(utente, ruoli);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        utente.RefreshToken = refreshTokenValue;
        utente.RefreshTokenExpiry = refreshTokenExpiry;

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenValue,
            UtenteId = utente.Id,
            ExpiresAt = refreshTokenExpiry
        });

        await _db.SaveChangesAsync();

        return (new LoginResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = _jwtService.GetAccessTokenExpiry(),
            Utente = new UtenteDTO
            {
                Id = utente.Id,
                Username = utente.Username,
                Email = utente.Email,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Ruoli = ruoli
            }
        }, null);
    }

    public async Task<(int? utenteId, string? error)> RegisterAsync(RegistrazioneRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
        {
            return (null, "Username deve essere di almeno 3 caratteri");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return (null, "Email non valida");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return (null, "Password deve essere di almeno 8 caratteri");
        }

        if (await _db.Utenti.AnyAsync(u => u.Username == request.Username))
        {
            return (null, "Username gia' in uso");
        }

        if (await _db.Utenti.AnyAsync(u => u.Email == request.Email))
        {
            return (null, "Email gia' in uso");
        }

        var ruoloUser = await _db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "User");
        if (ruoloUser == null)
        {
            return (null, "Ruolo User non trovato");
        }

        var utente = new Utente
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
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

        _db.Utenti.Add(utente);
        await _db.SaveChangesAsync();

        return (utente.Id, null);
    }

    public async Task<string?> LogoutAsync(int userId)
    {
        var utente = await _db.Utenti.FindAsync(userId);
        if (utente == null)
        {
            return "NOT_FOUND";
        }

        utente.RefreshToken = null;
        utente.RefreshTokenExpiry = null;

        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UtenteId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<UtenteDTO?> GetMeAsync(int userId)
    {
        var utente = await _db.Utenti
            .Include(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Attivo);

        if (utente == null)
        {
            return null;
        }

        return new UtenteDTO
        {
            Id = utente.Id,
            Username = utente.Username,
            Email = utente.Email,
            Nome = utente.Nome,
            Cognome = utente.Cognome,
            Ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList()
        };
    }
}
