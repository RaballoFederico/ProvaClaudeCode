using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IAuthService
{
    Task<(LoginResponseDTO? response, string? error)> LoginAsync(LoginRequestDTO request);
    Task<(LoginResponseDTO? response, string? error)> RefreshAsync(RefreshTokenRequestDTO request);
    Task<(int? utenteId, string? error)> RegisterAsync(RegistrazioneRequestDTO request);
    Task<string?> LogoutAsync(int userId);
    Task<UtenteDTO?> GetMeAsync(int userId);
}
