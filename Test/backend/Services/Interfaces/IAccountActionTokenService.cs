// DOC: Interfaccia service 'IAccountActionTokenService': contratto della logica applicativa usata dagli endpoint.
using FilmAPI.Model;

namespace FilmAPI.Services.Interfaces;

public interface IAccountActionTokenService
{
    Task<string> CreateAsync(string email, AccountActionTokenPurpose purpose, TimeSpan ttl, int? userId = null, string? metadataJson = null);
    Task<AccountActionToken?> ConsumeAsync(string token, AccountActionTokenPurpose purpose);
}

