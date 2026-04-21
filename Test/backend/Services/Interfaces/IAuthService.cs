using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IAuthService
{
    Task<(LoginResponseDTO? response, string? error)> LoginAsync(LoginRequestDTO request, string? ipAddress = null, string? userAgent = null);
    Task<(LoginResponseDTO? response, string? error)> RefreshAsync(RefreshTokenRequestDTO request, string? ipAddress = null, string? userAgent = null);
    Task<(int? utenteId, string? error)> RegisterAsync(RegistrazioneRequestDTO request);
    Task<(LoginResponseDTO? response, string? error)> LoginOrRegisterExternalAsync(string provider, string providerUserId, string email, string? displayName, string? suggestedUsername, string? ipAddress = null, string? userAgent = null);
    Task<string?> LogoutAsync(int userId, string? ipAddress = null, string? userAgent = null);
    Task<string?> LogoutAllAsync(int userId, string? ipAddress = null, string? userAgent = null);
    Task<(bool success, string? error)> ChangePasswordAsync(int userId, ChangePasswordRequestDTO request);
    Task<UtenteDTO?> GetMeAsync(int userId);
}
