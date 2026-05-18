// DOC: Service 'AuthService': implementa logica di business e integrazioni esterne (DB/TMDB/Stripe).
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FilmAPI.Services;

public class AuthService : IAuthService
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9_.-]{3,40}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(
        "^[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly FilmDbContext _db;
    private readonly JwtService _jwtService;

    public AuthService(FilmDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<(LoginResponseDTO? response, string? error)> LoginAsync(LoginRequestDTO request, string? ipAddress = null, string? userAgent = null)
    {
        var username = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return (null, "INVALID_CREDENTIALS");
        }

        var normalizedUsername = username.ToLowerInvariant();

        var utente = await _db.Utenti
            .Include(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == normalizedUsername &&
                u.Attivo);

        if (utente == null || string.IsNullOrEmpty(utente.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, utente.PasswordHash))
        {
            return (null, "INVALID_CREDENTIALS");
        }

        var response = await BuildLoginResponseAsync(utente);
        return (response, null);
    }

    public async Task<(LoginResponseDTO? response, string? error)> RefreshAsync(RefreshTokenRequestDTO request, string? ipAddress = null, string? userAgent = null)
    {
        var incomingRefreshTokenHash = _jwtService.HashRefreshToken(request.RefreshToken ?? string.Empty);

        var refreshToken = await _db.RefreshTokens
            .Include(rt => rt.Utente)
            .ThenInclude(u => u.UtentiRuoli)
            .ThenInclude(ur => ur.Ruolo)
            .FirstOrDefaultAsync(rt => rt.TokenHash == incomingRefreshTokenHash && rt.RevokedAt == null);

        if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow || !refreshToken.Utente.Attivo)
        {
            return (null, "INVALID_REFRESH");
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.RevokedByUserAgent = userAgent;
        await RevokeActiveRefreshTokensAsync(refreshToken.UtenteId);

        var utente = refreshToken.Utente;
        var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
        var (accessToken, accessTokenExpiry) = _jwtService.GenerateAccessTokenWithExpiry(utente, ruoli);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        var refreshTokenHash = _jwtService.HashRefreshToken(refreshTokenValue);
        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshTokenHash,
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
            ExpiresAt = accessTokenExpiry,
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
        var username = (request.Username ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) || !UsernameRegex.IsMatch(username))
        {
            return (null, "Username non valido: usa 3-40 caratteri (lettere, numeri, _, ., -)");
        }

        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
        {
            return (null, "Email non valida");
        }

        if (!IsStrongPassword(request.Password))
        {
            return (null, "Password non valida: minimo 8 caratteri con almeno una maiuscola, una minuscola, un numero e un simbolo");
        }

        if (await _db.Utenti.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            return (null, "Username gia' in uso");
        }

        if (await _db.Utenti.AnyAsync(u => u.Email.ToLower() == email))
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
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Nome = request.Nome,
            Cognome = request.Cognome,
            Telefono = request.Telefono,
            ConsensoNewsletter = request.ConsensoNewsletter,
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
                u.ExternalProviderUserId == providerUserId.Trim() &&
                u.Attivo);

        if (utente == null)
        {
            utente = await _db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.Attivo);

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
                    ExternalProviderUserId = providerUserId.Trim(),
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

        // Per utenti esterni appena creati serve prima persistere l'utente
        // cosÃ¬ l'ID esiste prima di creare il refresh token.
        if (utente.Id == 0)
        {
            await _db.SaveChangesAsync();

            utente = await _db.Utenti
                .Include(u => u.UtentiRuoli)
                .ThenInclude(ur => ur.Ruolo)
                .FirstAsync(u => u.Id == utente.Id);
        }

        var response = await BuildLoginResponseAsync(utente);
        return (response, null);
    }

    // DOC-METHOD: 'LogoutAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<string?> LogoutAsync(int userId, string? ipAddress = null, string? userAgent = null)
    {
        var utente = await _db.Utenti.FindAsync(userId);
        if (utente == null)
        {
            return "NOT_FOUND";
        }

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

    // DOC-METHOD: 'LogoutAllAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public async Task<string?> LogoutAllAsync(int userId, string? ipAddress = null, string? userAgent = null)
    {
        return await LogoutAsync(userId, ipAddress, userAgent);
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

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, utente.PasswordHash))
        {
            return (false, "INVALID_CURRENT_PASSWORD");
        }

        if (!IsStrongPassword(request.NewPassword))
        {
            return (false, "WEAK_PASSWORD");
        }

        return await SetPasswordAsync(userId, request.NewPassword);
    }

    public async Task<(bool success, string? error)> SetPasswordAsync(int userId, string newPassword)
    {
        var utente = await _db.Utenti.FirstOrDefaultAsync(u => u.Id == userId && u.Attivo);
        if (utente == null)
        {
            return (false, "NOT_FOUND");
        }

        if (!IsStrongPassword(newPassword))
        {
            return (false, "WEAK_PASSWORD");
        }

        utente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await RevokeActiveRefreshTokensAsync(userId);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // DOC-METHOD: 'GetMeAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
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

    // DOC-METHOD: 'BuildLoginResponseAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private async Task<LoginResponseDTO> BuildLoginResponseAsync(Utente utente)
    {
        utente.DataUltimoAccesso = DateTime.UtcNow;
        await RevokeActiveRefreshTokensAsync(utente.Id);
        var ruoli = utente.UtentiRuoli.Select(ur => ur.Ruolo.Nome).ToList();
        var (accessToken, accessTokenExpiry) = _jwtService.GenerateAccessTokenWithExpiry(utente, ruoli);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        var refreshTokenHash = _jwtService.HashRefreshToken(refreshTokenValue);
        _db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshTokenHash,
            UtenteId = utente.Id,
            ExpiresAt = refreshTokenExpiry,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return new LoginResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = accessTokenExpiry,
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

    // DOC-METHOD: 'RevokeActiveRefreshTokensAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private async Task RevokeActiveRefreshTokensAsync(int userId)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UtenteId == userId && rt.RevokedAt == null)
            .ToListAsync();

        if (activeTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }
    }

    // DOC-METHOD: 'GenerateUniqueUsernameAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
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

    // DOC-METHOD: 'IsStrongPassword' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static bool IsStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSymbol;
    }
}

