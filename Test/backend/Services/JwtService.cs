using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FilmAPI.Model;

namespace FilmAPI.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Utente utente, IEnumerable<string> ruoli)
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

        var secretKey = GetJwtValue("Jwt:SecretKey", "JWT_SECRET_KEY", "your-super-secret-key-min-32-characters-for-jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(GetJwtValue("Jwt:AccessTokenExpiryMinutes", "JWT_ACCESS_TOKEN_EXPIRY_MINUTES", "15"));

        var token = new JwtSecurityToken(
            issuer: GetJwtValue("Jwt:Issuer", "JWT_ISSUER", "FilmAPI"),
            audience: GetJwtValue("Jwt:Audience", "JWT_AUDIENCE", "FilmFrontend"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token, bool isRefreshToken = false)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(GetJwtValue("Jwt:SecretKey", "JWT_SECRET_KEY", "your-super-secret-key-min-32-characters-for-jwt"));

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

    public DateTime GetAccessTokenExpiry()
    {
        var expiryMinutes = int.Parse(GetJwtValue("Jwt:AccessTokenExpiryMinutes", "JWT_ACCESS_TOKEN_EXPIRY_MINUTES", "15"));
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var expiryDays = int.Parse(GetJwtValue("Jwt:RefreshTokenExpiryDays", "JWT_REFRESH_TOKEN_EXPIRY_DAYS", "7"));
        return DateTime.UtcNow.AddDays(expiryDays);
    }

    private string GetJwtValue(string configKey, string envKey, string fallback)
    {
        return Environment.GetEnvironmentVariable(envKey)
            ?? _configuration[configKey]
            ?? fallback;
    }
}
