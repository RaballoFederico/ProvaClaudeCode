using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IExternalAuthService
{
    IReadOnlyList<ExternalAuthProviderDTO> GetEnabledProviders();
    (string? redirectUrl, string? error) CreateAuthorizationUrl(string provider, string? returnUrl, string backendBaseUrl);
    Task<string> HandleCallbackAsync(string provider, string backendBaseUrl, string? code, string? state, string? oauthError);
    Task<(LoginResponseDTO? response, string? error)> CompleteAsync(ExternalAuthCompleteRequestDTO request);
}
