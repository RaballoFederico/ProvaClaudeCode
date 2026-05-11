using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class AccountActionTokenService(FilmDbContext db) : IAccountActionTokenService
{
    public async Task<string> CreateAsync(string email, AccountActionTokenPurpose purpose, TimeSpan ttl, int? userId = null, string? metadataJson = null)
    {
        var rawToken = CreateRandomUrlSafeToken();
        var tokenHash = HashToken(rawToken);

        var entity = new AccountActionToken
        {
            UtenteId = userId,
            Email = (email ?? string.Empty).Trim().ToLowerInvariant(),
            Purpose = purpose,
            TokenHash = tokenHash,
            MetadataJson = metadataJson,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
            UsedAt = null
        };

        db.AccountActionTokens.Add(entity);
        await db.SaveChangesAsync();
        return rawToken;
    }

    public async Task<AccountActionToken?> ConsumeAsync(string token, AccountActionTokenPurpose purpose)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var tokenHash = HashToken(token);

        var now = DateTime.UtcNow;
        var entity = await db.AccountActionTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash
                                      && t.Purpose == purpose
                                      && t.UsedAt == null
                                      && t.ExpiresAt > now);

        if (entity is null) return null;

        entity.UsedAt = now;
        await db.SaveChangesAsync();
        return entity;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CreateRandomUrlSafeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
