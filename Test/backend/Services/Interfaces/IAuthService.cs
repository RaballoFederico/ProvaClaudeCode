using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IAuthService
{
    Task<(LoginResponseDTO? response, string? error)> LoginAsync(LoginRequestDTO request);
    Task<(LoginResponseDTO? response, string? error)> RefreshAsync(RefreshTokenRequestDTO request);
    Task<(int? utenteId, string? error)> RegisterAsync(RegistrazioneRequestDTO request);
    Task<(LoginResponseDTO? response, string? error)> LoginOrRegisterExternalAsync(string provider, string providerUserId, string email, string? displayName, string? suggestedUsername);
    Task<string?> LogoutAsync(int userId);
    Task<UtenteDTO?> GetMeAsync(int userId);
}
