// DOC: AccountActionTokenService - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Service 'AccountActionTokenService': implementa logica di business e integrazioni esterne (DB/TMDB/Stripe).
using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

// DOC-METHOD: 'AccountActionTokenService' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
public class AccountActionTokenService(FilmDbContext db) : IAccountActionTokenService
{
    // DOC-METHOD: 'CreateAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
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

    // DOC-METHOD: 'ConsumeAsync' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
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

    // DOC-METHOD: 'HashToken' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // DOC-METHOD: 'CreateRandomUrlSafeToken' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string CreateRandomUrlSafeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}


