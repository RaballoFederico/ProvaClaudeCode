namespace FilmAPI.DTO;

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

public record UserResponse(string Id, string Email, string? FullName, IEnumerable<string> Roles);
