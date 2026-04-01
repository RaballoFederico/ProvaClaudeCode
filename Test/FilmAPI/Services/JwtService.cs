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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiryMinutes),
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
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
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
        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var expiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
        return DateTime.UtcNow.AddDays(expiryDays);
    }
}
