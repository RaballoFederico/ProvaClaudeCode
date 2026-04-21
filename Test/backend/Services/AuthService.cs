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

    public async Task<(LoginResponseDTO? response, string? error)> LoginAsync(LoginRequestDTO request, string? ipAddress = null, string? userAgent = null)
    {
        var utente = await _db.Utenti
            .Include(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(u => (u.Username == request.Username || u.Email == request.Username) && u.Attivo);

        if (utente == null || string.IsNullOrEmpty(utente.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, utente.PasswordHash))
        {
            return (null, "INVALID_CREDENTIALS");
        }

        var response = await BuildLoginResponseAsync(utente, ipAddress, userAgent);
        return (response, null);
    }

    public async Task<(LoginResponseDTO? response, string? error)> RefreshAsync(RefreshTokenRequestDTO request, string? ipAddress = null, string? userAgent = null)
    {
        var tokenHash = _jwtService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await _db.RefreshTokens
            .Include(rt => rt.Utente)
            .ThenInclude(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (refreshToken == null)
        {
            return (null, "INVALID_REFRESH");
        }

        if (refreshToken.RevokedAt != null)
        {
            await RevokeAllRefreshTokensAsync(refreshToken.UtenteId, ipAddress, userAgent);
            return (null, "INVALID_REFRESH");
        }

        if (refreshToken.ExpiresAt <= DateTime.UtcNow || !refreshToken.Utente.Attivo)
        {
            return (null, "INVALID_REFRESH");
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.RevokedByUserAgent = userAgent;

        var utente = refreshToken.Utente;
        var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
        var accessToken = _jwtService.GenerateAccessToken(utente, ruoli);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();
        var newHash = _jwtService.HashRefreshToken(refreshTokenValue);

        refreshToken.ReplacedByTokenHash = newHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newHash,
            UtenteId = utente.Id,
            ExpiresAt = refreshTokenExpiry,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent
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

    public async Task<(LoginResponseDTO? response, string? error)> LoginOrRegisterExternalAsync(string provider, string providerUserId, string email, string? displayName, string? suggestedUsername, string? ipAddress = null, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerUserId) || string.IsNullOrWhiteSpace(email))
        {
            return (null, "INVALID_EXTERNAL_LOGIN");
        }

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var utente = await _db.Utenti
            .Include(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(u =>
                u.ExternalProvider == normalizedProvider &&
                u.ExternalProviderUserId == providerUserId &&
                u.Attivo);

        if (utente == null)
        {
            utente = await _db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Attivo);

            if (utente != null)
            {
                utente.ExternalProvider = normalizedProvider;
                utente.ExternalProviderUserId = providerUserId;
            }
            else
            {
                var ruoloUser = await _db.Ruoli.FirstOrDefaultAsync(r => r.Nome == "User");
                if (ruoloUser == null)
                {
                    return (null, "Ruolo User non trovato");
                }

                var generatedUsername = await GenerateUniqueUsernameAsync(suggestedUsername, normalizedEmail);
                var (nome, cognome) = SplitDisplayName(displayName);

                utente = new Utente
                {
                    Username = generatedUsername,
                    Email = normalizedEmail,
                    PasswordHash = null,
                    ExternalProvider = normalizedProvider,
                    ExternalProviderUserId = providerUserId,
                    Nome = nome,
                    Cognome = cognome,
                    DataRegistrazione = DateTime.UtcNow,
                    Attivo = true,
                    UtentiRuoli = new List<UtenteRuolo>
                    {
                        new UtenteRuolo { RuoloId = ruoloUser.Id }
                    }
                };

                _db.Utenti.Add(utente);
            }
        }

        var response = await BuildLoginResponseAsync(utente, ipAddress, userAgent);
        return (response, null);
    }

    public async Task<string?> LogoutAsync(int userId, string? ipAddress = null, string? userAgent = null)
    {
        var utente = await _db.Utenti.FindAsync(userId);
        if (utente == null)
        {
            return "NOT_FOUND";
        }

        await RevokeAllRefreshTokensAsync(userId, ipAddress, userAgent);
        return null;
    }

    public async Task<string?> LogoutAllAsync(int userId, string? ipAddress = null, string? userAgent = null)
    {
        var utente = await _db.Utenti.FindAsync(userId);
        if (utente == null)
        {
            return "NOT_FOUND";
        }

        await RevokeAllRefreshTokensAsync(userId, ipAddress, userAgent);
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

    private async Task<LoginResponseDTO> BuildLoginResponseAsync(Utente utente, string? ipAddress = null, string? userAgent = null)
    {
        utente.DataUltimoAccesso = DateTime.UtcNow;
        var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
        var accessToken = _jwtService.GenerateAccessToken(utente, ruoli);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();
        var hash = _jwtService.HashRefreshToken(refreshTokenValue);

        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hash,
            UtenteId = utente.Id,
            ExpiresAt = refreshTokenExpiry,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent
        });

        await _db.SaveChangesAsync();

        return new LoginResponseDTO
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
        };
    }

    public async Task<(bool success, string? error)> ChangePasswordAsync(int userId, ChangePasswordRequestDTO request)
    {
        var utente = await _db.Utenti.FirstOrDefaultAsync(u => u.Id == userId && u.Attivo);
        if (utente == null)
        {
            return (false, "NOT_FOUND");
        }

        if (string.IsNullOrWhiteSpace(utente.PasswordHash))
        {
            return (false, "PASSWORD_NOT_SET");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return (false, "INVALID_PASSWORD");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, utente.PasswordHash))
        {
            return (false, "INVALID_CREDENTIALS");
        }

        if (request.NewPassword.Length < 8)
        {
            return (false, "PASSWORD_TOO_SHORT");
        }

        utente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task RevokeAllRefreshTokensAsync(int userId, string? ipAddress, string? userAgent)
    {
        var tokens = await _db.RefreshTokens
            .Where(rt => rt.UtenteId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevokedByUserAgent = userAgent;
        }

        await _db.SaveChangesAsync();
    }

    private async Task<string> GenerateUniqueUsernameAsync(string? suggestedUsername, string email)
    {
        var baseValue = string.IsNullOrWhiteSpace(suggestedUsername)
            ? email.Split('@')[0]
            : suggestedUsername;

        var sanitized = new string(baseValue
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '-')
            .ToArray())
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "user";
        }

        if (sanitized.Length > 40)
        {
            sanitized = sanitized[..40];
        }

        var candidate = sanitized;
        var suffix = 1;
        while (await _db.Utenti.AnyAsync(u => u.Username == candidate))
        {
            candidate = $"{sanitized}{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static (string? nome, string? cognome) SplitDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return (null, null);
        }

        var parts = displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return (null, null);
        }

        if (parts.Length == 1)
        {
            return (parts[0], null);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
