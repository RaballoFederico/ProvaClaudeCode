// DOC: JwtService - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Service 'JwtService': implementa logica di business e integrazioni esterne (DB/TMDB/Stripe).
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FilmAPI.Model;

namespace FilmAPI.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;
    private const int DefaultAccessTokenExpiryMinutes = 15;
    private const int DefaultRefreshTokenExpiryDays = 7;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // DOC-METHOD: 'GenerateAccessToken' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public string GenerateAccessToken(Utente utente, IEnumerable<string> ruoli)
    {
        return GenerateAccessTokenWithExpiry(utente, ruoli).token;
    }

    public (string token, DateTime expiresAt) GenerateAccessTokenWithExpiry(Utente utente, IEnumerable<string> ruoli)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, utente.Id.ToString()),
            new Claim(ClaimTypes.Name, utente.Username),
            new Claim(ClaimTypes.Email, utente.Email),
            new Claim("nome", utente.Nome ?? ""),
            new Claim("cognome", utente.Cognome ?? "")
        };

        foreach (var ruolo in ruoli)
        {
            claims.Add(new Claim(ClaimTypes.Role, ruolo));
        }

        var secretKey = GetJwtValue("Jwt:SecretKey", "JWT_SECRET_KEY", string.Empty);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = GetIntJwtValue("Jwt:AccessTokenExpiryMinutes", "JWT_ACCESS_TOKEN_EXPIRY_MINUTES", DefaultAccessTokenExpiryMinutes);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: GetJwtValue("Jwt:Issuer", "JWT_ISSUER", "FilmAPI"),
            audience: GetJwtValue("Jwt:Audience", "JWT_AUDIENCE", "FilmFrontend"),
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // DOC-METHOD: 'GenerateRefreshToken' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    // DOC-METHOD: 'HashRefreshToken' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public string HashRefreshToken(string refreshToken)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    // DOC-METHOD: 'ValidateToken' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public ClaimsPrincipal? ValidateToken(string token, bool isRefreshToken = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(GetJwtValue("Jwt:SecretKey", "JWT_SECRET_KEY", string.Empty));

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = GetJwtValue("Jwt:Issuer", "JWT_ISSUER", "FilmAPI"),
                ValidateAudience = true,
                ValidAudience = GetJwtValue("Jwt:Audience", "JWT_AUDIENCE", "FilmFrontend"),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    // DOC-METHOD: 'GetAccessTokenExpiry' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public DateTime GetAccessTokenExpiry()
    {
        var expiryMinutes = GetIntJwtValue("Jwt:AccessTokenExpiryMinutes", "JWT_ACCESS_TOKEN_EXPIRY_MINUTES", DefaultAccessTokenExpiryMinutes);
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    // DOC-METHOD: 'GetRefreshTokenExpiry' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public DateTime GetRefreshTokenExpiry()
    {
        var expiryDays = GetIntJwtValue("Jwt:RefreshTokenExpiryDays", "JWT_REFRESH_TOKEN_EXPIRY_DAYS", DefaultRefreshTokenExpiryDays);
        return DateTime.UtcNow.AddDays(expiryDays);
    }

    // DOC-METHOD: 'GetJwtValue' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private string GetJwtValue(string configKey, string envKey, string fallback)
    {
        return Environment.GetEnvironmentVariable(envKey)
            ?? _configuration[configKey]
            ?? fallback;
    }

    // DOC-METHOD: 'GetIntJwtValue' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private int GetIntJwtValue(string configKey, string envKey, int fallback)
    {
        var raw = GetJwtValue(configKey, envKey, fallback.ToString());
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}


